using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Pastel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Normalization;
using Color = System.Drawing.Color;

namespace HistogramRepro
{
    /// <summary>
    ///     Reproduces ImageSharp issue where histogram equalisation is non-deterministic between repeated runs.
    /// </summary>
    class Program
    {
        private const string SampleImage = "Resources/IMG_FD_001_IR105_20200912_001006.jpg";
        private const string OutputDirectory = "Output";

        private const int RunCount = 5;

        private static async Task Main(string[] args)
        {
            var saveOutput = args.Length > 0 && args[0] == "-s";
            if (saveOutput) Directory.CreateDirectory(OutputDirectory);

            // Apply histogram equalisation to image
            var equalisedPixels = await EqualiseImages(saveOutput);

            // Calculate and report pixel mismatches between first equalised image and subsequent images
            var mismatches = GetMismatches(equalisedPixels);
            ReportMismatches(mismatches, equalisedPixels);
        }

        /// <summary>
        ///     Equalise <see cref="RunCount"/> instances of <see cref="SampleImage"/>, returning a collection of
        ///     equalised pixel data.
        /// </summary>
        /// <param name="saveOutput">whether to write the output</param>
        /// <returns>pixel data of histogram equalised images</returns>
        private static async Task<List<Rgba32[]>> EqualiseImages(bool saveOutput)
        {
            using var source = await Image.LoadAsync<Rgba32>(SampleImage);
            var targetPixels = new List<Rgba32[]>();

            for (var i = 0; i < RunCount; i++)
            {
                var target = source.Clone();
                target.Mutate(ctx => ctx.HistogramEqualization());

                // Extract all pixel data from image
                targetPixels.Add(GetPixelData(target));

                if (!saveOutput) continue;

                // Save image if required
                var outputFilename = Path.Combine(OutputDirectory, $"Output-{i}.png");
                Console.WriteLine($"Writing histogram equalised output to {outputFilename}");

                await target.SaveAsync(outputFilename);
            }

            return targetPixels;
        }

        /// <summary>
        ///     Compares equalised pixels from each run against the first run.
        /// </summary>
        /// <returns>number of different pixels in consecutive histogram equalisation runs</returns>
        private static List<int> GetMismatches(IReadOnlyList<Rgba32[]> equalisedPixels)
        {
            var reference = equalisedPixels[0];
            var mismatches = new List<int>();

            // Compare each output against the reference
            for (var i = 1; i < equalisedPixels.Count; i++)
            {
                var mismatchCount = 0;
                for (var j = 0; j < equalisedPixels[i].Length; j++)
                {
                    if (equalisedPixels[i][j] != reference[j]) mismatchCount++;
                }

                mismatches.Add(mismatchCount);
            }

            return mismatches;
        }

        /// <summary>
        ///     Write mismatched pixel values to the console.
        /// </summary>
        private static void ReportMismatches(IReadOnlyList<int> mismatches, IReadOnlyList<Rgba32[]> equalisedPixels)
        {
            for (var i = 0; i < mismatches.Count; i++)
            {
                var mismatchCount = mismatches[i];

                Console.Write($"Run #{i + 1}: ".Pastel(Color.CornflowerBlue));
                if (mismatchCount == 0) Console.WriteLine("identical to reference".Pastel(Color.GreenYellow));
                else Console.WriteLine($"{mismatchCount / (float) equalisedPixels[i].Length:P} different pixels to reference".Pastel(Color.Firebrick));
            }
        }

        /// <summary>
        ///     Returns a 1D array of all pixel data in an image.
        /// </summary>
        private static Rgba32[] GetPixelData(Image<Rgba32> image)
        {
            Rgba32[] data = new Rgba32[image.Height * image.Width];

            for (var y = 0; y < image.Height; y++)
            {
                var span = image.GetPixelRowSpan(y);

                for (var x = 0; x < image.Width; x++)
                {
                    data[y * image.Width + x] = span[x];
                }
            }

            return data;
        }
    }
}