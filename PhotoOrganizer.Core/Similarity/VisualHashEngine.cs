using OpenCvSharp;

namespace PhotoOrganizer.Core.Similarity;

public static class VisualHashEngine
{
    /// <summary>
    /// Generates a perceptual dHash (difference hash) for visual similarity detection.
    /// Produces a 64-bit hash representing structure instead of raw pixels.
    /// </summary>
    public static ulong? GetVisualHash(string file)
    {
        try
        {
            using var img = new Mat(file);

            // Resize to 9x8 grayscale -> compute gradients horizontally
            using var small = img.Resize(new Size(9, 8)).CvtColor(ColorConversionCodes.BGR2GRAY);

            ulong hash = 0;
            int bit = 0;

            // Compare each pixel with its right neighbor
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var left = small.At<byte>(y, x);
                    var right = small.At<byte>(y, x + 1);

                    if (left > right)
                        hash |= 1UL << bit;

                    bit++;
                }
            }

            return hash;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Hamming distance between 2 hashes.
    /// The lower, the more similar. (0 = identical)
    /// Recommended threshold: 0–10
    /// </summary>
    public static int Distance(ulong h1, ulong h2)
    {
        ulong x = h1 ^ h2;
        int count = 0;
        while (x != 0)
        {
            x &= (x - 1);
            count++;
        }
        return count;
    }
}
