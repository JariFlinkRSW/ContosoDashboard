namespace ContosoDashboard.Services;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; set; } = "AppData/Documents";
    public int MaxFileSizeMb { get; set; } = 25;
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;
}