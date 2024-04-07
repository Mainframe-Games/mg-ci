namespace SocketServer;

internal static class Fragmentation
{
    private const int fragmentSize = 1024 * 50; // 50 KB

    public static List<byte[]> Fragment(byte[] inData)
    {
        var frags = new List<byte[]>();

        // Calculate the number of fragments
        var totalFragments = (int)Math.Ceiling((double)inData.Length / fragmentSize);

        // Send each fragment
        for (int i = 0; i < totalFragments; i++)
        {
            // Calculate the start and end index for the current fragment
            var startIndex = i * fragmentSize;
            var endIndex = Math.Min((i + 1) * fragmentSize, inData.Length);

            // Extract the current fragment from the original data
            var fragment = new byte[endIndex - startIndex];
            Array.Copy(inData, startIndex, fragment, 0, fragment.Length);

            frags.Add(fragment);
        }

        return frags;
    }
}
