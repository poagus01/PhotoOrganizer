using System.Security.Cryptography;

public static class HashEngine
{
    public static string GetSha256(string file)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(file);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
}
