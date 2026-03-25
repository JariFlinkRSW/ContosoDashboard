using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IHostEnvironment hostEnvironment, IOptions<DocumentStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(options);

        _rootPath = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, options.Value.RootPath));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var combinedPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));

        if (!combinedPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        return combinedPath;
    }
}