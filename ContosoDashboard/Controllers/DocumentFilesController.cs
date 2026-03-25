using System.Security.Claims;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Controllers;

[Authorize]
[ApiController]
[Route("document-files")]
public class DocumentFilesController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentFilesController> _logger;

    public DocumentFilesController(IDocumentService documentService, ILogger<DocumentFilesController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpGet("{documentId:int}/download")]
    public async Task<IActionResult> Download(int documentId, CancellationToken cancellationToken)
    {
        var requestingUserId = GetRequestingUserId();
        if (requestingUserId == 0)
        {
            return NotFound();
        }

        var fileResult = await _documentService.OpenDocumentForDownloadAsync(documentId, requestingUserId, cancellationToken);
        if (fileResult == null)
        {
            return NotFound();
        }

        return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
    }

    [HttpGet("{documentId:int}/preview")]
    public async Task<IActionResult> Preview(int documentId, CancellationToken cancellationToken)
    {
        var requestingUserId = GetRequestingUserId();
        if (requestingUserId == 0)
        {
            return NotFound();
        }

        var fileResult = await _documentService.OpenDocumentForPreviewAsync(documentId, requestingUserId, cancellationToken);
        if (fileResult == null)
        {
            return NotFound();
        }

        if (fileResult.Content == Stream.Null || string.IsNullOrWhiteSpace(fileResult.ContentType))
        {
            return BadRequest("This file type cannot be previewed inline.");
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{fileResult.FileName}\"";
        _logger.LogInformation("Streaming inline preview for document {DocumentId} to user {UserId}", documentId, requestingUserId);
        return File(fileResult.Content, fileResult.ContentType);
    }

    private int GetRequestingUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : 0;
    }
}