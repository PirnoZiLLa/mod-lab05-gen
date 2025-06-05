using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using Xunit;
using TextGenerator;
using Microsoft.VisualStudio.TestPlatform.TestHost;


namespace TextGenerator.Tests
{
    public class BigramGeneratorTests : IDisposable
    {
        private readonly string tempFilePath;

        public BigramGeneratorTests()
        {
            // ������� ��������� ���� � ��������� ����������
            tempFilePath = Path.GetTempFileName();
            File.WriteAllLines(tempFilePath, new[]
            {
                "1\t��\t0.5",
                "2\t��\t0.3",
                "3\t��\t0.2"
            });
        }
        [Fact]
        
        public void Dispose()
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        [Fact(DisplayName = "����������� ����������� ���������� ��� ���������� �����")]
        public void BigramGenerator_FileNotFound_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => new BigramGenerator("nonexistent.txt"));
        }

        [Fact]
        public void BigramGenerator_ThrowsOnMissingFile()
        {
            Assert.Throws<FileNotFoundException>(() => new BigramGenerator("nonexistent.txt"));
        }


        [Fact(DisplayName = "����������� ����������� ���������� ��� ������ ����� �������")]
        public void BigramGenerator_EmptyFile_ThrowsException()
        {
            // Arrange
            var emptyFilePath = Path.GetTempFileName();
            File.WriteAllText(emptyFilePath, ""); // ������ ����

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new BigramGenerator(emptyFilePath));

            // Cleanup
            File.Delete(emptyFilePath);
        }

        [Fact(DisplayName = "����������� ����������� ���������� ��� ������������ ������� ������ � �����")]
        public void BigramGenerator_InvalidFileFormat_ThrowsException()
        {
            // Arrange
            var invalidFilePath = Path.GetTempFileName();
            File.WriteAllLines(invalidFilePath, new[]
            {
                "1\tth\tinvalid", // ������������ ������ �������
                "2\the\t0.3",
                "3\tin\t0.2"
            });

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new BigramGenerator(invalidFilePath));

            // Cleanup
            File.Delete(invalidFilePath);
        }
    }

    public class WordGeneratorTests : IDisposable
    {
        private readonly string tempFilePath;

        public WordGeneratorTests()
        {
            // ������� ��������� ���� � ��������� �������
            tempFilePath = Path.GetTempFileName();
            File.WriteAllLines(tempFilePath, new[]
            {
                "1\t���\t0.4",
                "2\t�\t0.3",
                "3\t���\t0.3"
            });
        }

        public void Dispose()
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        [Fact(DisplayName = "����������� ����������� ���������� ��� ���������� �����")]
        public void WordGenerator_FileNotFound_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => new WordGenerator("nonexistent.txt"));
        }

        

        [Fact(DisplayName = "��������� ��������� ����� �� �����")]
        public void WordGenerator_LoadWords_CorrectlyLoadsData()
        {
            // Arrange
            var generator = new WordGenerator(tempFilePath);

            // Act
            var distribution = generator.GetExpectedDistribution();

            // Assert
            Assert.Equal(3, distribution.Count);
            Assert.True(distribution.ContainsKey("���"));
            Assert.Equal(0.4, distribution["���"]);
            Assert.True(distribution.ContainsKey("�"));
            Assert.Equal(0.3, distribution["�"]);
            Assert.True(distribution.ContainsKey("���"));
            Assert.Equal(0.3, distribution["���"]);
        }

        [Fact(DisplayName = "���������� ����� � �������� ����������� ����")]
        public void WordGenerator_GenerateText_ReturnsCorrectWordCount()
        {
            // Arrange
            var generator = new WordGenerator(tempFilePath);

            // Act
            string text = generator.GenerateText(5);

            // Assert
            Assert.Equal(5, text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [Fact(DisplayName = "GenerateText ����������� ���������� ��� ��������������� ���������� ����")]
        public void WordGenerator_GenerateText_NonPositiveWordCount_ThrowsException()
        {
            // Arrange
            var generator = new WordGenerator(tempFilePath);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => generator.GenerateText(0));
            Assert.Throws<ArgumentException>(() => generator.GenerateText(-1));
        }
    }

    public class PlotGeneratorTests : IDisposable
    {
        private readonly string resultsDir;

        public PlotGeneratorTests()
        {
            resultsDir = FileHelper.GetResultsDirectory();
        }

        public void Dispose()
        {
            // ������� �������� �����
            string[] files = Directory.GetFiles(resultsDir, "test_plot*.png");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        //[Fact(DisplayName = "CreateFrequencyPlot ������� ���� �����������")]
        //public void PlotGenerator_CreateFrequencyPlot_CreatesImageFile()
        //{
        //    // Arrange
        //    var expected = new Dictionary<string, double>
        //    {
        //        { "��", 0.5 },
        //        { "��", 0.3 },
        //        { "��", 0.2 }
        //    };
        //    var actual = new Dictionary<string, int>
        //    {
        //        { "��", 50 },
        //        { "��", 30 },
        //        { "��", 20 }
        //    };
        //    string fileName = "test_plot.png";
        //    string filePath = Path.Combine(resultsDir, fileName);

        //    // Act
        //    PlotGenerator.CreateFrequencyPlot(expected, actual, "�������� ������", fileName);

        //    // Assert
        //    Assert.True(File.Exists(filePath));
        //}
        


        [Fact(DisplayName = "CreateFrequencyPlot ����������� ���������� ��� ������ ������")]
        public void PlotGenerator_CreateFrequencyPlot_EmptyData_ThrowsException()
        {
            // Arrange
            var emptyExpected = new Dictionary<string, double>();
            var emptyActual = new Dictionary<string, int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                PlotGenerator.CreateFrequencyPlot(emptyExpected, emptyActual, "�������� ������", "test_plot.png"));
        }
    }

    public class FileHelperTests
    {
        [Fact(DisplayName = "GetResultsDirectory ������� � ���������� ���������� ����")]
        public void FileHelper_GetResultsDirectory_CreatesAndReturnsDirectory()
        {
            // Act
            string resultsDir = FileHelper.GetResultsDirectory();

            // Assert
            Assert.True(Directory.Exists(resultsDir));
            Assert.EndsWith("Results", resultsDir);
        }
    }
}