using System.Net.Mime;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public class DocumentService : IDocumentService
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly HashSet<string> PreviewableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly DocumentStorageOptions _storageOptions;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IOptions<DocumentStorageOptions> storageOptions,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task<DocumentQueryResult> GetMyDocumentsAsync(int requestingUserId, DocumentQueryOptions options, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.Tags)
            .Include(d => d.UploadedByUser)
            .Where(d => d.UploadedByUserId == requestingUserId);

        if (!string.IsNullOrWhiteSpace(options.SearchTerm))
        {
            var term = options.SearchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.Title.ToLower().Contains(term) ||
                (d.Description != null && d.Description.ToLower().Contains(term)) ||
                d.Tags.Any(t => t.TagValue.ToLower().Contains(term)) ||
                d.UploadedByUser.DisplayName.ToLower().Contains(term) ||
                (d.Project != null && d.Project.Name.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(options.Category))
        {
            query = query.Where(d => d.Category == options.Category);
        }

        if (options.ProjectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == options.ProjectId.Value);
        }

        query = ApplySort(query, options);

        var documentEntities = await query.ToListAsync(cancellationToken);

        var documents = documentEntities
            .Select(d => new DocumentListItem
            {
                DocumentId = d.DocumentId,
                Title = d.Title,
                Description = d.Description,
                Category = d.Category,
                OriginalFileName = d.OriginalFileName,
                FileType = d.FileType,
                FileSizeBytes = d.FileSizeBytes,
                FileSizeDisplay = d.FileSizeBytes < 1024
                    ? d.FileSizeBytes + " B"
                    : d.FileSizeBytes < 1024 * 1024
                        ? (d.FileSizeBytes / 1024d).ToString("F1") + " KB"
                        : (d.FileSizeBytes / 1024d / 1024d).ToString("F1") + " MB",
                CreatedDateUtc = d.CreatedDateUtc,
                UpdatedDateUtc = d.UpdatedDateUtc,
                UploadedByUserId = d.UploadedByUserId,
                UploadedByDisplayName = d.UploadedByUser.DisplayName,
                ProjectId = d.ProjectId,
                ProjectName = d.Project?.Name,
                Tags = d.Tags.OrderBy(t => t.TagValue).Select(t => t.TagValue).ToList(),
                IsPreviewable = PreviewableExtensions.Contains(Path.GetExtension(d.OriginalFileName))
            })
            .ToList();

        var categories = await _context.Documents
            .AsNoTracking()
            .Where(d => d.UploadedByUserId == requestingUserId)
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);

        return new DocumentQueryResult
        {
            Documents = documents,
            AvailableCategories = categories
        };
    }

    public async Task<IReadOnlyList<DocumentUploadResult>> UploadDocumentsAsync(int requestingUserId, DocumentUploadCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || string.IsNullOrWhiteSpace(command.Category))
        {
            return command.Files.Select(file => new DocumentUploadResult
            {
                FileName = file.FileName,
                Success = false,
                Message = "Title and category are required."
            }).ToList();
        }

        await EnsureProjectAssociationIsAllowedAsync(command.ProjectId, command.TaskId, requestingUserId, cancellationToken);

        var normalizedTags = NormalizeTags(command.TagValues);
        var results = new List<DocumentUploadResult>();

        foreach (var file in command.Files)
        {
            var validationError = ValidateFile(file);
            if (validationError != null)
            {
                _logger.LogWarning("Document upload validation failed for user {UserId} and file {FileName}: {Reason}", requestingUserId, file.FileName, validationError);
                results.Add(new DocumentUploadResult
                {
                    FileName = file.FileName,
                    Success = false,
                    Message = validationError
                });
                continue;
            }

            var extension = Path.GetExtension(file.FileName);
            var storageKey = BuildStorageKey(requestingUserId, command.ProjectId, extension);

            try
            {
                await using var fileStream = await file.OpenReadStreamAsync(cancellationToken);
                await _fileStorageService.SaveAsync(storageKey, fileStream, cancellationToken);

                var now = DateTime.UtcNow;
                var document = new Document
                {
                    Title = command.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
                    Category = command.Category.Trim(),
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StorageKey = storageKey,
                    FileType = ResolveContentType(file.FileName, file.ContentType),
                    FileSizeBytes = file.Size,
                    UploadedByUserId = requestingUserId,
                    ProjectId = command.ProjectId,
                    TaskId = command.TaskId,
                    CreatedDateUtc = now,
                    UpdatedDateUtc = now,
                    Tags = normalizedTags.Select(tag => new DocumentTag { TagValue = tag }).ToList(),
                    ActivityRecords = new List<DocumentActivityRecord>
                    {
                        new()
                        {
                            ActorUserId = requestingUserId,
                            ActivityType = DocumentActivityType.Upload,
                            OccurredAtUtc = now,
                            Details = $"Uploaded {Path.GetFileName(file.FileName)}"
                        }
                    }
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync(cancellationToken);

                results.Add(new DocumentUploadResult
                {
                    FileName = file.FileName,
                    Success = true,
                    DocumentId = document.DocumentId,
                    Message = "Upload succeeded."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document upload failed for user {UserId} and file {FileName}", requestingUserId, file.FileName);

                try
                {
                    await _fileStorageService.DeleteAsync(storageKey, cancellationToken);
                    _logger.LogInformation("Compensating cleanup completed for failed upload of {FileName}", file.FileName);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Compensating cleanup failed for storage key {StorageKey}", storageKey);
                }

                results.Add(new DocumentUploadResult
                {
                    FileName = file.FileName,
                    Success = false,
                    Message = "Upload failed. Please try again."
                });
            }
        }

        return results;
    }

    public async Task<DocumentOperationResult> UpdateDocumentMetadataAsync(int requestingUserId, UpdateDocumentMetadataCommand command, CancellationToken cancellationToken = default)
    {
        var document = await LoadManagedDocumentAsync(command.DocumentId, requestingUserId, cancellationToken);
        if (document == null)
        {
            return new DocumentOperationResult { Success = false, Message = "Document not found." };
        }

        if (string.IsNullOrWhiteSpace(command.Title) || string.IsNullOrWhiteSpace(command.Category))
        {
            return new DocumentOperationResult { Success = false, Message = "Title and category are required." };
        }

        document.Title = command.Title.Trim();
        document.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        document.Category = command.Category.Trim();
        document.UpdatedDateUtc = DateTime.UtcNow;

        var normalizedTags = NormalizeTags(command.TagValues);
        document.Tags.Clear();
        foreach (var tag in normalizedTags)
        {
            document.Tags.Add(new DocumentTag { TagValue = tag });
        }

        document.ActivityRecords.Add(new DocumentActivityRecord
        {
            ActorUserId = requestingUserId,
            ActivityType = DocumentActivityType.MetadataUpdate,
            OccurredAtUtc = DateTime.UtcNow,
            Details = "Updated document metadata"
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new DocumentOperationResult
        {
            Success = true,
            DocumentId = document.DocumentId,
            Message = "Document details updated."
        };
    }

    public async Task<DocumentOperationResult> ReplaceDocumentFileAsync(int requestingUserId, ReplaceDocumentFileCommand command, CancellationToken cancellationToken = default)
    {
        var document = await LoadManagedDocumentAsync(command.DocumentId, requestingUserId, cancellationToken);
        if (document == null)
        {
            return new DocumentOperationResult { Success = false, Message = "Document not found." };
        }

        var validationError = ValidateFile(command.ReplacementFile);
        if (validationError != null)
        {
            _logger.LogWarning("Document replacement validation failed for user {UserId} and document {DocumentId}: {Reason}", requestingUserId, command.DocumentId, validationError);
            return new DocumentOperationResult { Success = false, Message = validationError };
        }

        var previousStorageKey = document.StorageKey;
        var previousFileName = document.OriginalFileName;
        var newStorageKey = BuildStorageKey(requestingUserId, document.ProjectId, Path.GetExtension(command.ReplacementFile.FileName));

        try
        {
            await using var fileStream = await command.ReplacementFile.OpenReadStreamAsync(cancellationToken);
            await _fileStorageService.SaveAsync(newStorageKey, fileStream, cancellationToken);

            document.StorageKey = newStorageKey;
            document.OriginalFileName = Path.GetFileName(command.ReplacementFile.FileName);
            document.FileType = ResolveContentType(command.ReplacementFile.FileName, command.ReplacementFile.ContentType);
            document.FileSizeBytes = command.ReplacementFile.Size;
            document.UpdatedDateUtc = DateTime.UtcNow;
            document.ActivityRecords.Add(new DocumentActivityRecord
            {
                ActorUserId = requestingUserId,
                ActivityType = DocumentActivityType.Replace,
                OccurredAtUtc = DateTime.UtcNow,
                Details = $"Replaced {previousFileName} with {document.OriginalFileName}"
            });

            await _context.SaveChangesAsync(cancellationToken);
            await _fileStorageService.DeleteAsync(previousStorageKey, cancellationToken);

            return new DocumentOperationResult
            {
                Success = true,
                DocumentId = document.DocumentId,
                Message = "Document file replaced."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document replacement failed for document {DocumentId}", command.DocumentId);

            try
            {
                await _fileStorageService.DeleteAsync(newStorageKey, cancellationToken);
                _logger.LogInformation("Compensating cleanup completed for failed replacement of document {DocumentId}", command.DocumentId);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Compensating cleanup failed for replacement storage key {StorageKey}", newStorageKey);
            }

            return new DocumentOperationResult { Success = false, Message = "Replacement failed. The original file was kept." };
        }
    }

    public async Task<DocumentOperationResult> DeleteDocumentAsync(int requestingUserId, DeleteDocumentCommand command, CancellationToken cancellationToken = default)
    {
        if (!command.Confirmation)
        {
            return new DocumentOperationResult { Success = false, Message = "Delete confirmation is required." };
        }

        var document = await LoadManagedDocumentAsync(command.DocumentId, requestingUserId, cancellationToken);
        if (document == null)
        {
            return new DocumentOperationResult { Success = false, Message = "Document not found." };
        }

        var storageKey = document.StorageKey;
        var originalFileName = document.OriginalFileName;

        _context.DocumentActivityRecords.Add(new DocumentActivityRecord
        {
            ActorUserId = requestingUserId,
            ActivityType = DocumentActivityType.Delete,
            OccurredAtUtc = DateTime.UtcNow,
            Details = $"Deleted document {originalFileName}",
            DocumentId = document.DocumentId
        });

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        await _fileStorageService.DeleteAsync(storageKey, cancellationToken);

        return new DocumentOperationResult
        {
            Success = true,
            DocumentId = command.DocumentId,
            Message = "Document deleted."
        };
    }

    public async Task<DocumentFileAccessResult?> OpenDocumentForDownloadAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await LoadAccessibleDocumentAsync(documentId, requestingUserId, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Download denied or document missing for user {UserId} and document {DocumentId}", requestingUserId, documentId);
            return null;
        }

        var stream = await _fileStorageService.OpenReadAsync(document.StorageKey, cancellationToken);
        document.ActivityRecords.Add(new DocumentActivityRecord
        {
            ActorUserId = requestingUserId,
            ActivityType = DocumentActivityType.Download,
            OccurredAtUtc = DateTime.UtcNow,
            Details = $"Downloaded {document.OriginalFileName}"
        });
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} downloaded by user {UserId}", documentId, requestingUserId);

        return new DocumentFileAccessResult
        {
            Content = stream,
            FileName = document.OriginalFileName,
            ContentType = document.FileType
        };
    }

    public async Task<DocumentFileAccessResult?> OpenDocumentForPreviewAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await LoadAccessibleDocumentAsync(documentId, requestingUserId, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Preview denied or document missing for user {UserId} and document {DocumentId}", requestingUserId, documentId);
            return null;
        }

        if (!PreviewableExtensions.Contains(Path.GetExtension(document.OriginalFileName)))
        {
            return new DocumentFileAccessResult
            {
                Content = Stream.Null,
                ContentType = string.Empty,
                FileName = string.Empty
            };
        }

        var stream = await _fileStorageService.OpenReadAsync(document.StorageKey, cancellationToken);
        document.ActivityRecords.Add(new DocumentActivityRecord
        {
            ActorUserId = requestingUserId,
            ActivityType = DocumentActivityType.Preview,
            OccurredAtUtc = DateTime.UtcNow,
            Details = $"Previewed {document.OriginalFileName}"
        });
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} previewed by user {UserId}", documentId, requestingUserId);

        return new DocumentFileAccessResult
        {
            Content = stream,
            FileName = document.OriginalFileName,
            ContentType = document.FileType
        };
    }

    private IQueryable<Document> ApplySort(IQueryable<Document> query, DocumentQueryOptions options)
    {
        return (options.SortBy, options.Descending) switch
        {
            (DocumentSortField.Title, false) => query.OrderBy(d => d.Title),
            (DocumentSortField.Title, true) => query.OrderByDescending(d => d.Title),
            (DocumentSortField.Category, false) => query.OrderBy(d => d.Category),
            (DocumentSortField.Category, true) => query.OrderByDescending(d => d.Category),
            (DocumentSortField.FileSize, false) => query.OrderBy(d => d.FileSizeBytes),
            (DocumentSortField.FileSize, true) => query.OrderByDescending(d => d.FileSizeBytes),
            (DocumentSortField.UploadDate, false) => query.OrderBy(d => d.CreatedDateUtc),
            _ => query.OrderByDescending(d => d.CreatedDateUtc)
        };
    }

    private async Task<Document?> LoadManagedDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .Include(d => d.Tags)
            .Include(d => d.Shares)
            .Include(d => d.ActivityRecords)
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.UploadedByUser)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document == null)
        {
            return null;
        }

        var requestingUser = await _context.Users.FindAsync(new object[] { requestingUserId }, cancellationToken);
        if (requestingUser == null)
        {
            return null;
        }

        if (document.UploadedByUserId == requestingUserId || requestingUser.Role == UserRole.Administrator)
        {
            return document;
        }

        if (requestingUser.Role == UserRole.TeamLead &&
            !string.IsNullOrWhiteSpace(requestingUser.Department) &&
            string.Equals(requestingUser.Department, document.UploadedByUser.Department, StringComparison.OrdinalIgnoreCase))
        {
            return document;
        }

        if (requestingUser.Role == UserRole.ProjectManager && document.Project?.ProjectManagerId == requestingUserId)
        {
            return document;
        }

        return null;
    }

    private async Task<Document?> LoadAccessibleDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .Include(d => d.Shares)
            .Include(d => d.ActivityRecords)
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.UploadedByUser)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document == null)
        {
            return null;
        }

        var requestingUser = await _context.Users.FindAsync(new object[] { requestingUserId }, cancellationToken);
        if (requestingUser == null)
        {
            return null;
        }

        if (document.UploadedByUserId == requestingUserId || requestingUser.Role == UserRole.Administrator)
        {
            return document;
        }

        if (document.Project != null)
        {
            var isProjectManager = document.Project.ProjectManagerId == requestingUserId;
            var isProjectMember = document.Project.ProjectMembers.Any(member => member.UserId == requestingUserId);
            if (isProjectManager || isProjectMember)
            {
                return document;
            }
        }

        if (document.Shares.Any(share => share.SharedWithUserId == requestingUserId))
        {
            return document;
        }

        if (!string.IsNullOrWhiteSpace(requestingUser.Department) &&
            document.Shares.Any(share => share.SharedWithDepartment == requestingUser.Department))
        {
            return document;
        }

        if (requestingUser.Role == UserRole.TeamLead &&
            !string.IsNullOrWhiteSpace(requestingUser.Department) &&
            string.Equals(requestingUser.Department, document.UploadedByUser.Department, StringComparison.OrdinalIgnoreCase))
        {
            return document;
        }

        return null;
    }

    private async Task EnsureProjectAssociationIsAllowedAsync(int? projectId, int? taskId, int requestingUserId, CancellationToken cancellationToken)
    {
        if (!projectId.HasValue && !taskId.HasValue)
        {
            return;
        }

        TaskItem? task = null;
        if (taskId.HasValue)
        {
            task = await _context.Tasks
                .Include(t => t.Project)
                .ThenInclude(p => p!.ProjectMembers)
                .FirstOrDefaultAsync(t => t.TaskId == taskId.Value, cancellationToken)
                ?? throw new InvalidOperationException("The selected task does not exist.");

            if (task.ProjectId.HasValue)
            {
                projectId ??= task.ProjectId;
            }
        }

        if (!projectId.HasValue)
        {
            return;
        }

        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId.Value, cancellationToken)
            ?? throw new InvalidOperationException("The selected project does not exist.");

        var isProjectManager = project.ProjectManagerId == requestingUserId;
        var isProjectMember = project.ProjectMembers.Any(member => member.UserId == requestingUserId);

        if (!isProjectManager && !isProjectMember)
        {
            throw new InvalidOperationException("You are not authorized to associate documents with that project.");
        }

        if (task?.ProjectId.HasValue == true && task.ProjectId != projectId)
        {
            throw new InvalidOperationException("Task and project associations must match.");
        }
    }

    private string? ValidateFile(DocumentUploadFile file)
    {
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            return "A file name is required.";
        }

        if (file.Size <= 0)
        {
            return $"{file.FileName} is empty.";
        }

        if (file.Size > _storageOptions.MaxFileSizeBytes)
        {
            return $"{file.FileName} exceeds the 25 MB limit.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !_storageOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return $"{file.FileName} is not a supported file type.";
        }

        return null;
    }

    private static List<string> NormalizeTags(IEnumerable<string> tagValues)
    {
        return tagValues
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag)
            .ToList();
    }

    private static string ResolveContentType(string fileName, string providedContentType)
    {
        if (!string.IsNullOrWhiteSpace(providedContentType) && !string.Equals(providedContentType, MediaTypeNames.Application.Octet, StringComparison.OrdinalIgnoreCase))
        {
            return providedContentType;
        }

        if (ContentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            return contentType;
        }

        return MediaTypeNames.Application.Octet;
    }

    private static string BuildStorageKey(int requestingUserId, int? projectId, string extension)
    {
        var scopeSegment = projectId.HasValue ? $"project-{projectId.Value}" : "personal";
        var normalizedExtension = string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.ToLowerInvariant();
        var now = DateTime.UtcNow;
        return $"documents/{requestingUserId}/{scopeSegment}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{normalizedExtension}";
    }
}