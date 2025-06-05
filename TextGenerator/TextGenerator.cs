using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using TextGenerator;

namespace TextGenerator.Tests
{
    public class BigramGeneratorTests : IDisposable
    {
        private readonly string tempFilePath;

        public BigramGeneratorTests()
        {
            tempFilePath = Path.GetTempFileName();
            File.WriteAllLines(tempFilePath, new[]
            {
                "1\tии\t0.5",
                "2\tаа\t0.3",
                "3\tда\t0.2"
            });
        }

        public void Dispose()
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        [Fact(DisplayName = "BigramGenerator выбрасывает исключение при отсутствии файла")]
        public void BigramGenerator_FileNotFound_ThrowsException()
        {
            Assert.Throws<FileNotFoundException>(() => new BigramGenerator("nonexistent.txt"));
        }

        [Fact(DisplayName = "BigramGenerator выбрасывает исключение при пустом файле")]
        public void BigramGenerator_EmptyFile_ThrowsException()
        {
            var emptyFilePath = Path.GetTempFileName();
            File.WriteAllText(emptyFilePath, "");

            Assert.Throws<InvalidOperationException>(() => new BigramGenerator(emptyFilePath));

            File.Delete(emptyFilePath);
        }

        [Fact(DisplayName = "BigramGenerator выбрасывает исключение при неправильном формате")]
        public void BigramGenerator_InvalidFileFormat_ThrowsException()
        {
            var invalidFilePath = Path.GetTempFileName();
            File.WriteAllLines(invalidFilePath, new[]
            {
                "1\tth\tinvalid",
                "2\the\t0.3",
                "3\tin\t0.2"
            });

            Assert.Throws<InvalidOperationException>(() => new BigramGenerator(invalidFilePath));

            File.Delete(invalidFilePath);
        }
    }

    public class WordGeneratorTests : IDisposable
    {
        private readonly string tempFilePath;

        public WordGeneratorTests()
        {
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

        [Fact(DisplayName = "WordGenerator выбрасывает исключение при отсутствии файла")]
        public void WordGenerator_FileNotFound_ThrowsException()
        {
            Assert.Throws<FileNotFoundException>(() => new WordGenerator("nonexistent.txt"));
        }

        [Fact(DisplayName = "Корректно загружает слова из файла")]
        public void WordGenerator_LoadWords_CorrectlyLoadsData()
        {
            var generator = new WordGenerator(tempFilePath);

            var distribution = generator.GetExpectedDistribution();

            Assert.Equal(3, distribution.Count);
            Assert.True(distribution.ContainsKey("или"));
            Assert.Equal(0.4, distribution["или"], 3);
            Assert.True(distribution.ContainsKey("и"));
            Assert.Equal(0.3, distribution["и"], 3);
            Assert.True(distribution.ContainsKey("там"));
            Assert.Equal(0.3, distribution["там"], 3);
        }

        [Fact(DisplayName = "Генерирует текст с заданным числом слов")]
        public void WordGenerator_GenerateText_ReturnsCorrectWordCount()
        {
            var generator = new WordGenerator(tempFilePath);
            string text = generator.GenerateText(5);
            Assert.Equal(5, text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [Fact(DisplayName = "GenerateText выбрасывает исключение при неположительном числе слов")]
        public void WordGenerator_GenerateText_NonPositiveWordCount_ThrowsException()
        {
            var generator = new WordGenerator(tempFilePath);
            Assert.Throws<ArgumentException>(() => generator.GenerateText(0));
            Assert.Throws<ArgumentException>(() => generator.GenerateText(-1));
        }
    }
}
