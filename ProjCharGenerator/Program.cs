using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace TextGenerator
{
    public class BigramGenerator
    {
        private Dictionary<string, double> bigrams = new Dictionary<string, double>();
        private Random random = new Random();
        private List<string> keys = new List<string>();
        private List<double> cumulativeProbs = new List<double>();

        public BigramGenerator(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"File {filename} not found");

            LoadBigrams(filename);
            PrepareDistribution();
        }

        private void LoadBigrams(string filename)
        {
            try
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split('\t');
                    if (parts.Length >= 3)
                    {
                        string bigram = parts[1].Trim().ToLower();
                        if (double.TryParse(parts[2], out double freq))
                        {
                            bigrams[bigram] = freq;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error loading bigrams", ex);
            }
        }

        private void PrepareDistribution()
        {
            if (bigrams.Count == 0)
                throw new InvalidOperationException("No bigrams loaded");

            double totalFreq = bigrams.Values.Sum();
            if (totalFreq <= 0)
                throw new InvalidOperationException("Invalid frequency sum");

            double cumulative = 0;
            foreach (var pair in bigrams.OrderByDescending(p => p.Value))
            {
                keys.Add(pair.Key);
                cumulative += pair.Value / totalFreq;
                cumulativeProbs.Add(cumulative);
            }
        }

        public string GenerateText(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be positive");

            if (keys.Count == 0)
                throw new InvalidOperationException("Generator not initialized");

            string result = "";
            for (int i = 0; i < length; i++)
            {
                double rand = random.NextDouble();
                int index = cumulativeProbs.FindIndex(p => p >= rand);
                if (index == -1) index = keys.Count - 1;
                result += keys[index];
            }
            return result;
        }

        public Dictionary<string, double> GetExpectedDistribution() =>
            new Dictionary<string, double>(bigrams);
    }

    public class WordGenerator
    {
        private Dictionary<string, double> words = new Dictionary<string, double>();
        private Random random = new Random();
        private List<string> keys = new List<string>();
        private List<double> cumulativeProbs = new List<double>();

        public WordGenerator(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"File {filename} not found");

            LoadWords(filename);
            PrepareDistribution();
        }

        private void LoadWords(string filename)
        {
            try
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        string word = parts[1].Trim().ToLower();
                        if (double.TryParse(parts[2].Replace(",", "."),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double freq))
                        {
                            words[word] = freq;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error loading words", ex);
            }
        }

        private void PrepareDistribution()
        {
            if (words.Count == 0)
                throw new InvalidOperationException("No words loaded");

            double totalFreq = words.Values.Sum();
            if (totalFreq <= 0)
                throw new InvalidOperationException("Invalid frequency sum");

            double cumulative = 0;
            foreach (var pair in words.OrderByDescending(p => p.Value))
            {
                keys.Add(pair.Key);
                cumulative += pair.Value / totalFreq;
                cumulativeProbs.Add(cumulative);
            }
        }

        public string GenerateText(int wordCount)
        {
            if (wordCount <= 0)
                throw new ArgumentException("Word count must be positive");

            if (keys.Count == 0)
                throw new InvalidOperationException("Generator not initialized");

            List<string> result = new List<string>();
            for (int i = 0; i < wordCount; i++)
            {
                double rand = random.NextDouble();
                int index = cumulativeProbs.FindIndex(p => p >= rand);
                if (index == -1) index = keys.Count - 1;
                result.Add(keys[index]);
            }
            return string.Join(" ", result);
        }

        public Dictionary<string, double> GetExpectedDistribution() =>
            new Dictionary<string, double>(words);
    }

    public static class PlotGenerator
    {
        public static void CreateFrequencyPlot(Dictionary<string, double> expected,
                                             Dictionary<string, int> actual,
                                             string title,
                                             string fileName)
        {
            //40
            var topData = expected
                .OrderByDescending(x => x.Value)
                .Take(40)
                .Select(x => new
                {
                    x.Key,
                    Expected = x.Value,
                    Actual = actual.ContainsKey(x.Key) ? actual[x.Key] : 0
                })
                .ToList();

            double[] positions = topData.Select((x, i) => (double)i).ToArray();
            string[] labels = topData.Select(x => x.Key).ToArray();
            double[] expectedValues = topData.Select(x => x.Expected).ToArray();
            double[] actualValues = topData.Select(x => (double)x.Actual).ToArray();

            double maxActual = actualValues.Max();
            double maxExpected = expectedValues.Max();
            double scaleFactor = maxActual / maxExpected;
            double[] scaledExpected = expectedValues.Select(x => x * scaleFactor).ToArray();

            
            DrawDistributionChart(
                expectedValues: scaledExpected,
                actualValues: actualValues,
                labels: labels,
                title: title,
                filename: fileName,
                expectedLabel: "Expected",
                actualLabel: "Actual");
        }

        private static void DrawDistributionChart(
            double[] expectedValues,
            double[] actualValues,
            string[] labels,
            string title,
            string filename,
            string expectedLabel,
            string actualLabel)
        {
            if (expectedValues == null || actualValues == null || labels == null)
                throw new ArgumentNullException("Input arrays cannot be null");

            if (expectedValues.Length == 0)
                throw new InvalidOperationException("No data to plot");

            
            const int width = 3000;
            const int height = 600;
            const int margin = 50;
            const int barWidth = 30;
            const int spacing = 10;
            int chartHeight = height - 2 * margin;
            double maxValue = Math.Max(expectedValues.Max(), actualValues.Max()) * 1.1;

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                
                using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
                {
                    SizeF titleSize = g.MeasureString(title, titleFont);
                    g.DrawString(title, titleFont, Brushes.Black,
                        width / 2 - titleSize.Width / 2, 10);
                }

                
                using (Pen axisPen = new Pen(Color.Black, 2))
                {
                    g.DrawLine(axisPen, margin, height - margin, width - margin, height - margin);
                    g.DrawLine(axisPen, margin, height - margin, margin, margin);
                }

                
                using (Font axisFont = new Font("Arial", 10))
                {
                    g.DrawString("Elements", axisFont, Brushes.Black,
                        width / 2 - 30, height - margin + 20);
                    g.DrawString("Frequency (%)", axisFont, Brushes.Black,
                        10, height / 2 - 50, new StringFormat { FormatFlags = StringFormatFlags.DirectionVertical });
                }

                
                for (int i = 0; i < labels.Length; i++)
                {
                    int x = margin + 50 + i * (barWidth * 2 + spacing);

                    
                    int expectedBarHeight = (int)(expectedValues[i] / maxValue * chartHeight);
                    g.FillRectangle(Brushes.Blue, x, height - margin - expectedBarHeight, barWidth, expectedBarHeight);

                    int actualBarHeight = (int)(actualValues[i] / maxValue * chartHeight);
                    g.FillRectangle(Brushes.Red, x + barWidth, height - margin - actualBarHeight, barWidth, actualBarHeight);

                    
                    using (Font valueFont = new Font("Arial", 8))
                    {
                        g.DrawString($"{expectedValues[i]:F1}", valueFont, Brushes.Blue,
                            x, height - margin - expectedBarHeight - 20);
                        g.DrawString($"{actualValues[i]:F1}", valueFont, Brushes.Red,
                            x + barWidth, height - margin - actualBarHeight - 20);
                        g.DrawString(labels[i], valueFont, Brushes.Black,
                            x - 10, height - margin + 5);
                    }
                }

                
                using (Font legendFont = new Font("Arial", 10))
                {
                    g.FillRectangle(Brushes.Blue, width - 150, 50, 20, 20);
                    g.DrawString(expectedLabel, legendFont, Brushes.Black, width - 125, 50);

                    g.FillRectangle(Brushes.Red, width - 150, 80, 20, 20);
                    g.DrawString(actualLabel, legendFont, Brushes.Black, width - 125, 80);
                }

                
                string filePath = Path.Combine(FileHelper.GetResultsDirectory(), filename);
                bitmap.Save(filePath, ImageFormat.Png);
            }
        }
    }

    public static class FileHelper
    {
        public static string GetResultsDirectory()
        {
            string programDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string resultsDir = Path.Combine(Directory.GetParent(programDir).FullName, "Results");

            if (!Directory.Exists(resultsDir))
            {
                Directory.CreateDirectory(resultsDir);
            }

            return resultsDir;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Directory.CreateDirectory("Results");

                Console.WriteLine("Generating bigrams...");
                var bigramGenerator = new BigramGenerator("bigrams.txt");
                string bigramText = bigramGenerator.GenerateText(1000);
                File.WriteAllText(Path.Combine(FileHelper.GetResultsDirectory(), "gen-1.txt"), bigramText);
                Console.WriteLine("Bigram text saved to Results/gen-1.txt");

                var bigramStats = AnalyzeBigrams(bigramText);
                var expectedBigrams = bigramGenerator.GetExpectedDistribution();
                PlotGenerator.CreateFrequencyPlot(
                    expectedBigrams,
                    bigramStats.ToDictionary(p => p.Key, p => (int)(p.Value * 10)), 
                    "Bigram Frequency Distribution",
                    "gen-1.png");

                Console.WriteLine("\nGenerating words...");
                var wordGenerator = new WordGenerator("words.txt");
                string wordText = wordGenerator.GenerateText(1000);
                File.WriteAllText(Path.Combine(FileHelper.GetResultsDirectory(), "gen-2.txt"), wordText);
                Console.WriteLine("Word text saved to Results/gen-2.txt");

                var wordStats = AnalyzeWords(wordText);
                var expectedWords = wordGenerator.GetExpectedDistribution();
                PlotGenerator.CreateFrequencyPlot(
                    expectedWords,
                    wordStats.ToDictionary(p => p.Key, p => (int)(p.Value * 10)), // Scale for plotting
                    "Word Frequency Distribution",
                    "gen-2.png");

                Console.WriteLine("\nOperation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Details: " + ex.StackTrace);
            }
        }

        static Dictionary<string, double> AnalyzeBigrams(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be empty");

            var stats = new Dictionary<string, int>();
            for (int i = 0; i < text.Length - 1; i++)
            {
                string bigram = text.Substring(i, 2).ToLower();
                if (stats.ContainsKey(bigram))
                    stats[bigram]++;
                else
                    stats[bigram] = 1;
            }

            double total = stats.Values.Sum();
            return stats.ToDictionary(p => p.Key, p => p.Value / total * 100);
        }

        static Dictionary<string, double> AnalyzeWords(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be empty");

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var stats = new Dictionary<string, int>();

            foreach (string word in words)
            {
                if (stats.ContainsKey(word))
                    stats[word]++;
                else
                    stats[word] = 1;
            }

            double total = stats.Values.Sum();
            return stats.ToDictionary(p => p.Key, p => p.Value / total * 100);
        }
    }
}