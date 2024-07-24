using System.Runtime.InteropServices;
using FastLZMA2Net;
using Compressor = FastLZMA2Net.Compressor;
namespace Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Compressor compressor = new Compressor();
            var parameters = FL2.GetPresetLevelParameters(10, 0);
            parameters = FL2.GetPresetLevelParameters(10, 1);
            compressor.CompressLevel = 10;
            compressor.HighCompressLevel = 1;
            compressor.DictionarySize = 1 << 30;
            byte[] src = File.ReadAllBytes("D:\\game.fnt");
            //PrintHex(src);

            byte[] compressed = compressor.Compress(src, 10);
            //PrintHex(compressed);
            Console.WriteLine("Compressed Length: " + compressed.Length);
            Console.WriteLine($"ratio: {(float)compressed.Length / (float)src.Length:P}");
            nuint decompressedSize = FL2.FindDecompressedSize(compressed);

            Console.WriteLine($"Max Compression Level: {FL2.MaxCompressionLevel}");
            Console.WriteLine($"Max High Compression Level: {FL2.MaxHighCompressionLevel}");
        }

        private static void PrintHex(byte[] byteArray)
        {
            foreach (byte b in byteArray)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine(Environment.NewLine);
        }
    }
}