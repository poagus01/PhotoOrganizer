using LiteDB;
using PhotoOrganizer.Core.Models;

namespace PhotoOrganizer.Core.Database;

public class PhotoDb        // <----- changed from PhotoDB
{
    private readonly LiteDatabase _db;
    public ILiteCollection<MediaFileInfo> Files { get; }

    public PhotoDb(string path = "photoindex.db")        // <----- constructor name matches class
    {
        _db = new LiteDatabase(path);
        Files = _db.GetCollection<MediaFileInfo>("files");

        Files.EnsureIndex(x => x.Sha256Hash, unique: false);
        Files.EnsureIndex(x => x.Year);
    }

    public void Insert(MediaFileInfo file) => Files.Insert(file);
    public void Update(MediaFileInfo file) => Files.Update(file);
    public MediaFileInfo? FindByHash(string sha) => Files.FindOne(x => x.Sha256Hash == sha);
}