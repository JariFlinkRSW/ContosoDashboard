using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Models;

namespace ContosoDashboard.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TaskItem> Tasks { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<TaskComment> TaskComments { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<Announcement> Announcements { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentShare> DocumentShares { get; set; } = null!;
    public DbSet<DocumentActivityRecord> DocumentActivityRecords { get; set; } = null!;
    public DbSet<DocumentTag> DocumentTags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.AssignedTasks)
            .WithOne(t => t.AssignedUser)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.CreatedTasks)
            .WithOne(t => t.CreatedByUser)
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ManagedProjects)
            .WithOne(p => p.ProjectManager)
            .HasForeignKey(p => p.ProjectManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.UploadedDocuments)
            .WithOne(d => d.UploadedByUser)
            .HasForeignKey(d => d.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.CreatedDocumentShares)
            .WithOne(ds => ds.SharedByUser)
            .HasForeignKey(ds => ds.SharedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ReceivedDocumentShares)
            .WithOne(ds => ds.SharedWithUser)
            .HasForeignKey(ds => ds.SharedWithUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.DocumentActivityRecords)
            .WithOne(ar => ar.ActorUser)
            .HasForeignKey(ar => ar.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Documents)
            .WithOne(d => d.Project)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskItem>()
            .HasMany(t => t.Documents)
            .WithOne(d => d.Task)
            .HasForeignKey(d => d.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasMany(d => d.Tags)
            .WithOne(t => t.Document)
            .HasForeignKey(t => t.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasMany(d => d.Shares)
            .WithOne(s => s.Document)
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasMany(d => d.ActivityRecords)
            .WithOne(a => a.Document)
            .HasForeignKey(a => a.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DocumentShare>()
            .ToTable(t => t.HasCheckConstraint("CK_DocumentShare_OneTarget", "(([SharedWithUserId] IS NOT NULL AND [SharedWithDepartment] IS NULL) OR ([SharedWithUserId] IS NULL AND [SharedWithDepartment] IS NOT NULL))"));

        modelBuilder.Entity<Document>()
            .Property(d => d.FileSizeBytes)
            .HasDefaultValue(0L);

        // Configure indexes for performance
        modelBuilder.Entity<TaskItem>()
            .HasIndex(t => t.AssignedUserId);

        modelBuilder.Entity<TaskItem>()
            .HasIndex(t => t.Status);

        modelBuilder.Entity<TaskItem>()
            .HasIndex(t => t.DueDate);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.ProjectManagerId);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.UploadedByUserId);

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.ProjectId);

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.TaskId);

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.Category);

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.CreatedDateUtc);

        modelBuilder.Entity<DocumentTag>()
            .HasIndex(t => new { t.DocumentId, t.TagValue })
            .IsUnique();

        modelBuilder.Entity<DocumentShare>()
            .HasIndex(s => new { s.DocumentId, s.SharedWithUserId, s.SharedWithDepartment });

        modelBuilder.Entity<DocumentActivityRecord>()
            .Property(a => a.DocumentId)
            .IsRequired(false);

        modelBuilder.Entity<DocumentActivityRecord>()
            .HasIndex(a => new { a.DocumentId, a.OccurredAtUtc });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var seedNow = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var projectStartDate = seedNow.AddDays(-30);
        var projectTargetDate = seedNow.AddDays(60);
        var taskOneDueDate = seedNow.AddDays(-20);
        var taskTwoDueDate = seedNow.AddDays(5);
        var taskThreeDueDate = seedNow.AddDays(10);

        // Seed an admin user
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Email = "admin@contoso.com",
                DisplayName = "System Administrator",
                Department = "IT",
                JobTitle = "Administrator",
                Role = UserRole.Administrator,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = seedNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            },
            new User
            {
                UserId = 2,
                Email = "camille.nicole@contoso.com",
                DisplayName = "Camille Nicole",
                Department = "Engineering",
                JobTitle = "Project Manager",
                Role = UserRole.ProjectManager,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = seedNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            },
            new User
            {
                UserId = 3,
                Email = "floris.kregel@contoso.com",
                DisplayName = "Floris Kregel",
                Department = "Engineering",
                JobTitle = "Team Lead",
                Role = UserRole.TeamLead,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = seedNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            },
            new User
            {
                UserId = 4,
                Email = "ni.kang@contoso.com",
                DisplayName = "Ni Kang",
                Department = "Engineering",
                JobTitle = "Software Engineer",
                Role = UserRole.Employee,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = seedNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            }
        );

        // Seed a sample project
        modelBuilder.Entity<Project>().HasData(
            new Project
            {
                ProjectId = 1,
                Name = "ContosoDashboard Development",
                Description = "Internal employee productivity dashboard",
                ProjectManagerId = 2,
                StartDate = projectStartDate,
                TargetCompletionDate = projectTargetDate,
                Status = ProjectStatus.Active,
                CreatedDate = projectStartDate,
                UpdatedDate = seedNow
            }
        );

        // Seed sample tasks
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem
            {
                TaskId = 1,
                Title = "Design database schema",
                Description = "Create entity relationship diagram and database design",
                Priority = TaskPriority.High,
                Status = Models.TaskStatus.Completed,
                DueDate = taskOneDueDate,
                AssignedUserId = 4,
                CreatedByUserId = 2,
                ProjectId = 1,
                CreatedDate = projectStartDate,
                UpdatedDate = taskOneDueDate
            },
            new TaskItem
            {
                TaskId = 2,
                Title = "Implement authentication",
                Description = "Set up Microsoft Entra ID authentication",
                Priority = TaskPriority.Critical,
                Status = Models.TaskStatus.InProgress,
                DueDate = taskTwoDueDate,
                AssignedUserId = 4,
                CreatedByUserId = 2,
                ProjectId = 1,
                CreatedDate = seedNow.AddDays(-25),
                UpdatedDate = seedNow
            },
            new TaskItem
            {
                TaskId = 3,
                Title = "Create UI mockups",
                Description = "Design user interface mockups for all main pages",
                Priority = TaskPriority.Medium,
                Status = Models.TaskStatus.NotStarted,
                DueDate = taskThreeDueDate,
                AssignedUserId = 4,
                CreatedByUserId = 2,
                ProjectId = 1,
                CreatedDate = seedNow.AddDays(-20),
                UpdatedDate = seedNow.AddDays(-20)
            }
        );

        // Seed project members
        modelBuilder.Entity<ProjectMember>().HasData(
            new ProjectMember
            {
                ProjectMemberId = 1,
                ProjectId = 1,
                UserId = 3,
                Role = "TeamLead",
                AssignedDate = projectStartDate
            },
            new ProjectMember
            {
                ProjectMemberId = 2,
                ProjectId = 1,
                UserId = 4,
                Role = "Developer",
                AssignedDate = projectStartDate
            }
        );

        // Seed announcement
        modelBuilder.Entity<Announcement>().HasData(
            new Announcement
            {
                AnnouncementId = 1,
                Title = "Welcome to ContosoDashboard",
                Content = "Welcome to the new ContosoDashboard application. This platform will help you manage your tasks and projects more efficiently.",
                CreatedByUserId = 1,
                PublishDate = seedNow,
                ExpiryDate = seedNow.AddDays(30),
                IsActive = true
            }
        );
    }
}
