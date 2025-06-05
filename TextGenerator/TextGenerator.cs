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
            // Создаем временный файл с тестовыми биграммами
            tempFilePath = Path.GetTempFileName();
            File.WriteAllLines(tempFilePath, new[]
            {
                "1\tии\t0.5",
                "2\tаа\t0.3",
                "3\tда\t0.2"
            });
        }
        [Fact]
        
        public void Dispose()
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        [Fact(DisplayName = "Конструктор выбрасывает исключение при отсутствии файла")]
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


        [Fact(DisplayName = "Конструктор выбрасывает исключение при пустом файле биграмм")]
        public void BigramGenerator_EmptyFile_ThrowsException()
        {
            // Arrange
            var emptyFilePath = Path.GetTempFileName();
            File.WriteAllText(emptyFilePath, ""); // Пустой файл

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new BigramGenerator(emptyFilePath));

            // Cleanup
            File.Delete(emptyFilePath);
        }

        [Fact(DisplayName = "Конструктор выбрасывает исключение при некорректном формате данных в файле")]
        public void BigramGenerator_InvalidFileFormat_ThrowsException()
        {
            // Arrange
            var invalidFilePath = Path.GetTempFileName();
            File.WriteAllLines(invalidFilePath, new[]
            {
                "1\tth\tinvalid", // Некорректный формат частоты
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
            // Создаем временный файл с тестовыми словами
            tempFilePath = Path.GetTempFileName();
            File.WriteAllLines(tempFilePath, new[]
            {
                "1\tили\t0.4",
                "2\tи\t0.3",
                "3\tтам\t0.3"
            });
        }

        public void Dispose()
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        [Fact(DisplayName = "Конструктор выбрасывает исключение при отсутствии файла")]
        public void WordGenerator_FileNotFound_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => new WordGenerator("nonexistent.txt"));
        }

        

        [Fact(DisplayName = "Корректно загружает слова из файла")]
        public void WordGenerator_LoadWords_CorrectlyLoadsData()
        {
            // Arrange
            var generator = new WordGenerator(tempFilePath);

            // Act
            var distribution = generator.GetExpectedDistribution();

            // Assert
            Assert.Equal(3, distribution.Count);
            Assert.True(distribution.ContainsKey("или"));
            Assert.Equal(0.4, distribution["или"]);
            Assert.True(distribution.ContainsKey("и"));
            Assert.Equal(0.3, distribution["и"]);
            Assert.True(distribution.ContainsKey("там"));
            Assert.Equal(0.3, distribution["там"]);
        }

        [Fact(DisplayName = "Генерирует текст с заданным количеством слов")]
        public void WordGenerator_GenerateText_ReturnsCorrectWordCount()
        {
            // Arrange
            var generator = new WordGenerator(tempFilePath);

            // Act
            string text = generator.GenerateText(5);

            // Assert
            Assert.Equal(5, text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [Fact(DisplayName = "GenerateText выбрасывает исключение при неположительном количестве слов")]
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
            // Удаляем тестовые файлы
            string[] files = Directory.GetFiles(resultsDir, "test_plot*.png");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        //[Fact(DisplayName = "CreateFrequencyPlot создает файл изображения")]
        //public void PlotGenerator_CreateFrequencyPlot_CreatesImageFile()
        //{
        //    // Arrange
        //    var expected = new Dictionary<string, double>
        //    {
        //        { "аа", 0.5 },
        //        { "аб", 0.3 },
        //        { "ав", 0.2 }
        //    };
        //    var actual = new Dictionary<string, int>
        //    {
        //        { "аа", 50 },
        //        { "аб", 30 },
        //        { "ав", 20 }
        //    };
        //    string fileName = "test_plot.png";
        //    string filePath = Path.Combine(resultsDir, fileName);

        //    // Act
        //    PlotGenerator.CreateFrequencyPlot(expected, actual, "Тестовый график", fileName);

        //    // Assert
        //    Assert.True(File.Exists(filePath));
        //}
        


        [Fact(DisplayName = "CreateFrequencyPlot выбрасывает исключение при пустых данных")]
        public void PlotGenerator_CreateFrequencyPlot_EmptyData_ThrowsException()
        {
            // Arrange
            var emptyExpected = new Dictionary<string, double>();
            var emptyActual = new Dictionary<string, int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                PlotGenerator.CreateFrequencyPlot(emptyExpected, emptyActual, "Тестовый график", "test_plot.png"));
        }
    }

    public class FileHelperTests
    {
        [Fact(DisplayName = "GetResultsDirectory создает и возвращает корректный путь")]
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