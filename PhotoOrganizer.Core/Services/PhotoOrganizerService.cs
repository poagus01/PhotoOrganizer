using LiteDB;
using PhotoOrganizer.Core.Database;
using PhotoOrganizer.Core.Models;
using PhotoOrganizer.Core.Similarity;
using System.Drawing;
using System.Text;

namespace PhotoOrganizer.Core.Services;

public class PhotoOrganizerService
{
    private readonly PhotoDb _db;

    private static readonly string[] _supportedImageExt = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".heic", ".heif", ".webp"];
    private readonly string[] _videoExt = { ".mp4", ".mov", ".avi", ".mkv", ".mpg", ".mpeg", ".wmv", ".flv", ".3gp", ".mts", ".m2ts" };


    public PhotoOrganizerService(PhotoDb db)
    {
        _db = db;
        // 🔥 FIX: removed `_hashEngine = new VisualHashEngine();` because class is static
    }

    public void Process(string source, string output)
    {
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(Path.Combine(output, "_Duplicates"));
        Directory.CreateDirectory(Path.Combine(output, "_Unsupported"));
        Directory.CreateDirectory(Path.Combine(output, "_UnknownYear"));

        // var files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)
        //     .Where(f => _supportedImageExt.Contains(Path.GetExtension(f).ToLower()) 
        //              || _videoExt.Contains(Path.GetExtension(f).ToLower()))
        //     .ToList();

        var files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories).ToList();


        Console.WriteLine($"📁 Found {files.Count} media files...");

        foreach (var file in files)
        {
            try
            {
                var ext = Path.GetExtension(file).ToLower();

                // 🔥 Unsupported file routing
                if (!_supportedImageExt.Contains(ext) && !_videoExt.Contains(ext))
                {
                    string unsupportedFolder = Path.Combine(output, "_Unsupported");
                    Directory.CreateDirectory(unsupportedFolder);

                    string dest = Path.Combine(unsupportedFolder, Path.GetFileName(file));
                    SafeMove(file, dest);

                    lock (Console.Out)
                        Console.WriteLine($"🚫 Unsupported → {Path.GetFileName(file)}");

                    continue;
                }

                // ---------------- HASH + DATE EXTRACTION ----------------
                string sha = ComputeSHA256(file);
                ulong? visual = VisualHashEngine.GetVisualHash(file);      // 🔥 FIX: static call

                DateTime? taken = (_supportedImageExt.Contains(ext))
                    ? ExtractTakenDate(file)
                    : File.GetLastWriteTime(file); // videos fallback

                // ---------------- DUP CHECK ----------------
                var existing = _db.FindByHash(sha);
                if (existing != null)
                {
                    string dupFolder = Path.Combine(output, "_Duplicates");
                    Directory.CreateDirectory(dupFolder);

                    string dest = Path.Combine(dupFolder, Path.GetFileName(file));
                    SafeMove(file, dest);

                    Console.WriteLine($"⚠ Duplicate: {Path.GetFileName(file)}");
                    continue;
                }

                // ---------------- YEAR FOLDER ----------------
                int year = taken?.Year ?? -1;

                string targetFolder = (year < 1900 || year > DateTime.Now.Year + 1)
                    ? Path.Combine(output, "_UnknownYear")
                    : Path.Combine(output, year.ToString());

                Directory.CreateDirectory(targetFolder);
                string destPath = Path.Combine(targetFolder, Path.GetFileName(file));
                SafeMove(file, destPath);

                // ---------------- DB INSERT ----------------
                var record = new MediaFileInfo
                {
                    Path = destPath,
                    Sha256Hash = sha,
                    VisualHash = visual,
                    Taken = taken,
                    Year = year
                };

                _db.Insert(record);

                Console.WriteLine($"📦 {Path.GetFileName(file)} → {Path.GetFileName(targetFolder)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❗ Error with {file}: {ex.Message}");
            }
        }

        Console.WriteLine("\n🎉 Done!");
    }

    // ---------------- SHA HASHING ----------------
    private static string ComputeSHA256(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
        catch { return Guid.NewGuid().ToString(); }
    }

    // ---------------- EXIF DATE EXTRACTION ----------------
    private DateTime? ExtractTakenDate(string file)
{
    try
    {
        using var img = Image.FromFile(file);

        // EXIF date tags in order of reliability
        int[] tags = { 0x9003, 0x0132, 0x9004 };

        foreach (var t in tags)
        {
            if (img.PropertyIdList.Contains(t))
            {
                var prop = img.GetPropertyItem(t);
                if (prop?.Value == null) continue;

                string raw = Encoding.ASCII.GetString(prop.Value).Trim('\0');

                // EXIF format example: "2020:05:21 14:32:11"
                // Convert to "2020-05-21 14:32:11"
                if (raw.Count(c => c == ':') >= 2)
                {
                    raw = raw.Replace(raw[..10], raw[..10].Replace(':', '-')); // 🔥 safe conversion
                }

                if (DateTime.TryParse(raw, out var dt))
                    return dt;
            }
        }
    }
    catch { }

    return File.GetLastWriteTime(file);  // fallback
}


    // ---------------- MOVE SAFE ----------------
    private void SafeMove(string src, string dest)
    {
        try
        {
            if (File.Exists(dest))
            {
                string newName = $"{Path.GetFileNameWithoutExtension(dest)}_{Guid.NewGuid().ToString()[..6]}{Path.GetExtension(dest)}";
                dest = Path.Combine(Path.GetDirectoryName(dest)!, newName);
            }
            File.Move(src, dest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Move failed {src}: {ex.Message}");
        }
    }
}
