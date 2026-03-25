using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivityRecord
{
    [Key]
    public int DocumentActivityRecordId { get; set; }

    public int? DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required]
    public DocumentActivityType ActivityType { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Details { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document? Document { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public virtual User ActorUser { get; set; } = null!;
}

public enum DocumentActivityType
{
    Upload,
    Preview,
    Download,
    MetadataUpdate,
    Replace,
    Delete,
    Share
}