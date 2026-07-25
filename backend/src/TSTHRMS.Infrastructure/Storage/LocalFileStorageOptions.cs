namespace TSTHRMS.Infrastructure.Storage;

public class LocalFileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "storage/uploads";
}
