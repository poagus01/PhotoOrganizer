using PhotoOrganizer.Core.Database;
using PhotoOrganizer.Core.Services;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  PhotoOrganizer.CLI <sourceFolder> <outputFolder>");
            return;
        }

        string source = args[0];
        string output = args[1];

        Console.WriteLine("📸 PhotoOrganizer");
        Console.WriteLine($"Input:  {source}");
        Console.WriteLine($"Output: {output}");
        Console.WriteLine("----------------------------------------");

        // 🔥 create DB and pass into service as required
        var db = new PhotoDb("photoindex.db");
        var service = new PhotoOrganizerService(db);

        service.Process(source, output);

        Console.WriteLine("\n✔ Finished");
    }
}
