using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    public long FileSizeBytes { get; set; }

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDateUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [ForeignKey(nameof(TaskId))]
    public virtual TaskItem? Task { get; set; }

    public virtual ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentActivityRecord> ActivityRecords { get; set; } = new List<DocumentActivityRecord>();

    [NotMapped]
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024d:F1} KB",
        _ => $"{FileSizeBytes / 1024d / 1024d:F1} MB"
    };
}