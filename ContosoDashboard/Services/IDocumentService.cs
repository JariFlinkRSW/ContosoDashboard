using System.Net.Mime;
using Microsoft.AspNetCore.Http;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<DocumentQueryResult> GetMyDocumentsAsync(int requestingUserId, DocumentQueryOptions options, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentUploadResult>> UploadDocumentsAsync(int requestingUserId, DocumentUploadCommand command, CancellationToken cancellationToken = default);
    Task<DocumentOperationResult> UpdateDocumentMetadataAsync(int requestingUserId, UpdateDocumentMetadataCommand command, CancellationToken cancellationToken = default);
    Task<DocumentOperationResult> ReplaceDocumentFileAsync(int requestingUserId, ReplaceDocumentFileCommand command, CancellationToken cancellationToken = default);
    Task<DocumentOperationResult> DeleteDocumentAsync(int requestingUserId, DeleteDocumentCommand command, CancellationToken cancellationToken = default);
    Task<DocumentFileAccessResult?> OpenDocumentForDownloadAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<DocumentFileAccessResult?> OpenDocumentForPreviewAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
}

public class DocumentQueryOptions
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DocumentSortField SortBy { get; set; } = DocumentSortField.UploadDate;
    public bool Descending { get; set; } = true;
}

public enum DocumentSortField
{
    Title,
    UploadDate,
    Category,
    FileSize
}

public class DocumentQueryResult
{
    public IReadOnlyList<DocumentListItem> Documents { get; init; } = Array.Empty<DocumentListItem>();
    public IReadOnlyList<string> AvailableCategories { get; init; } = Array.Empty<string>();
}

public class DocumentListItem
{
    public int DocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string FileSizeDisplay { get; init; } = string.Empty;
    public DateTime CreatedDateUtc { get; init; }
    public DateTime UpdatedDateUtc { get; init; }
    public int UploadedByUserId { get; init; }
    public string UploadedByDisplayName { get; init; } = string.Empty;
    public int? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public bool IsPreviewable { get; init; }
}

public class DocumentUploadCommand
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> TagValues { get; set; } = new();
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public IReadOnlyList<DocumentUploadFile> Files { get; set; } = Array.Empty<DocumentUploadFile>();
}

public class DocumentUploadFile
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public Func<CancellationToken, Task<Stream>> OpenReadStreamAsync { get; init; } = _ => Task.FromResult<Stream>(Stream.Null);
}

public class UpdateDocumentMetadataCommand
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> TagValues { get; set; } = new();
}

public class ReplaceDocumentFileCommand
{
    public int DocumentId { get; set; }
    public DocumentUploadFile ReplacementFile { get; set; } = new();
}

public class DeleteDocumentCommand
{
    public int DocumentId { get; set; }
    public bool Confirmation { get; set; }
}

public class DocumentOperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? DocumentId { get; init; }
}

public class DocumentUploadResult : DocumentOperationResult
{
    public string FileName { get; init; } = string.Empty;
}

public class DocumentFileAccessResult
{
    public Stream Content { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = MediaTypeNames.Application.Octet;
}