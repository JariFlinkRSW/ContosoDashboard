using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

builder.Services.Configure<DocumentStorageOptions>(builder.Configuration.GetSection(DocumentStorageOptions.SectionName));

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (NeedsLegacyDatabaseReset(context))
        {
            logger.LogWarning("A legacy training database without migration history was detected. Recreating the database so migrations can be applied cleanly.");
            context.Database.CloseConnection();
            SqlConnection.ClearAllPools();
            context.Database.EnsureDeleted();
        }

        try
        {
            context.Database.Migrate();
        }
        catch (SqlException ex) when (ex.Message.Contains("already an object named", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(ex, "A legacy pre-migrations training database blocked schema creation. Recreating it and retrying migrations.");
            context.Database.CloseConnection();
            SqlConnection.ClearAllPools();
            context.Database.EnsureDeleted();
            context.Database.Migrate();
        }

        var storageOptions = services.GetRequiredService<IOptions<DocumentStorageOptions>>().Value;
        var contentRoot = services.GetRequiredService<IHostEnvironment>().ContentRootPath;
        var storageRoot = Path.GetFullPath(Path.Combine(contentRoot, storageOptions.RootPath));
        Directory.CreateDirectory(storageRoot);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static bool NeedsLegacyDatabaseReset(ApplicationDbContext context)
{
    if (!context.Database.CanConnect())
    {
        return false;
    }

    var connection = context.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;

    try
    {
        if (shouldClose)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        var tableCount = Convert.ToInt32(command.ExecuteScalar() ?? 0);

        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory'";
        var historyTableCount = Convert.ToInt32(command.ExecuteScalar() ?? 0);

        return tableCount > 0 && historyTableCount == 0;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}
