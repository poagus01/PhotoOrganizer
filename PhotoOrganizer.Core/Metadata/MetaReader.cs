using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

public static class MetaReader
{
    public static DateTime? GetDateTaken(string file)
    {
        try
        {
            var dirs = ImageMetadataReader.ReadMetadata(file);
            var exif = dirs.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            return exif?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal);
        }
        catch { return null; }
    }
}
