using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using DelimitedTextHelper;

namespace DelimitedTextParserTest
{
    [TestClass]
    public class DelimitedTextParserTests
    {
        
        /// <summary>
        /// Simple test, no quoted strings that contain delimiter
        /// </summary>
        [TestMethod]
        public void SimpleParserTest()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var reader = new StreamReader(stream))
            using (var parser = new DelimitedTextParser(reader))
            {
                writer.Write("1,2\r\n");
                writer.Write("3,4\r\n");
                writer.Flush();
                stream.Position = 0;

                var row = parser.Read();
                Assert.IsNotNull(row);

                row = parser.Read();
                Assert.IsNotNull(row);

                Assert.IsNull(parser.Read());
            }

        }

        [TestMethod]
        public void RowBlankLinesTest()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var reader = new StreamReader(stream))
            using (var parser = new DelimitedTextParser(reader))
            {
                writer.Write("1,2\r\n");
                writer.Write("\r\n");
                writer.Write("3,4\r\n");
                writer.Write("\r\n");
                writer.Write("5,6\r\n");
                writer.Flush();
                stream.Position = 0;

                var rowCount = 1;
                while (parser.Read() != null)
                {
                    Assert.AreEqual(rowCount, parser.LineNumber);
                    rowCount += 2;
                }
            }
        }

        [TestMethod]
        public void LastLineIsBlankTestSkippingComments()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var reader = new StreamReader(stream))
            using (var parser = new DelimitedTextParser(reader, ',', true))
            {
                writer.Write("1,2\r\n");
                writer.Write("3,4\r\n");
                writer.Write("5,6\r\n");
                writer.Write("");
                writer.Flush();
                stream.Position = 0;

                var rowCount = 1;
                while (parser.Read() != null)
                {
                    Assert.AreEqual(rowCount, parser.LineNumber);
                    rowCount += 1;
                }
            }
        }

        [TestMethod]
        public void ParseNewRecordTest()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("one,two,three");
            writer.WriteLine("four,five,six");
            writer.Flush();
            stream.Position = 0;
            var reader = new StreamReader(stream);

            var parser = new DelimitedTextParser(reader);

            var count = 0;
            while (parser.Read() != null)
            {
                count++;
            }

            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void ParseFieldQuotesTest()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("one,\"two\",three");
            writer.WriteLine("four,\"\"\"five\"\"\",six");
            writer.Flush();
            stream.Position = 0;
            var reader = new StreamReader(stream);

            var parser = new DelimitedTextParser(reader) ;

            var record = parser.Read();
            Assert.AreEqual("one", record[0]);
            Assert.AreEqual("two", record[1]);
            Assert.AreEqual("three", record[2]);

            record = parser.Read();
            Assert.AreEqual("four", record[0]);
            Assert.AreEqual("\"five\"", record[1]);
            Assert.AreEqual("six", record[2]);

            record = parser.Read();
            Assert.IsNull(record);
        }


        [TestMethod]
        public void ParseFieldQuotesWithCommaTest()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("one,\"two,half\",three");
            writer.WriteLine("four,\"\"\"five\"\"\",six");
            writer.Flush();
            stream.Position = 0;
            var reader = new StreamReader(stream);

            var parser = new DelimitedTextParser(reader);

            var record = parser.Read();
            Assert.AreEqual("one", record[0]);
            Assert.AreEqual("two,half", record[1]);
            Assert.AreEqual("three", record[2]);

            record = parser.Read();
            Assert.AreEqual("four", record[0]);
            Assert.AreEqual("\"five\"", record[1]);
            Assert.AreEqual("six", record[2]);

            record = parser.Read();
            Assert.IsNull(record);
        }

        [TestMethod]
        public void ParseSpacesTest()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine(" one , \"two three\" , four ");
            writer.WriteLine(" \" five \"\" six \"\" seven \" ");
            writer.Flush();
            stream.Position = 0;
            var reader = new StreamReader(stream);

            var parser = new DelimitedTextParser(reader);

            var record = parser.Read();
            Assert.AreEqual(" one ", record[0]);
            Assert.AreEqual(" \"two three\" ", record[1]);
            Assert.AreEqual(" four ", record[2]);

            record = parser.Read();
            Assert.AreEqual(" \" five \"\" six \"\" seven \" ", record[0]);

            record = parser.Read();
            Assert.IsNull(record);
        }

        [TestMethod]
        public void CallingReadMultipleTimesAfterDoneReadingTest()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("one,two,three");
            writer.WriteLine("four,five,six");
            writer.Flush();
            stream.Position = 0;
            var reader = new StreamReader(stream);

            var parser = new DelimitedTextParser(reader);

            parser.Read();
            parser.Read();
            parser.Read();
            parser.Read();
        }

        [TestMethod]
        public void ParseEmptyTest()
        {
            using (var memoryStream = new MemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            using (var parser = new DelimitedTextParser(streamReader))
            {
                var record = parser.Read();
                Assert.IsNull(record);
            }
        }

        [TestMethod]
        public void ParseCrOnlyTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new DelimitedTextParser(reader))
            {
                writer.Write("\r");
                writer.Flush();
                stream.Position = 0;

                var record = parser.Read();
                Assert.IsNull(record);
            }
        }

        [TestMethod]
        public void ParseLfOnlyTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new DelimitedTextParser(reader))
            {
                writer.Write("\n");
                writer.Flush();
                stream.Position = 0;

                var record = parser.Read();
                Assert.IsNull(record);
            }
        }

        [TestMethod]
        public void ParseCrLnOnlyTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new DelimitedTextParser(reader))
            {
                writer.Write("\r\n");
                writer.Flush();
                stream.Position = 0;

                var record = parser.Read();
                Assert.IsNull(record);
            }
        }

        [TestMethod]
        public void Parse1RecordWithNoCrlfTest()
        {
            using (var memoryStream = new MemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var parser = new DelimitedTextParser(streamReader))
            {
                streamWriter.Write("one,two,three");
                streamWriter.Flush();
                memoryStream.Position = 0;

                var record = parser.Read();
                Assert.IsNotNull(record);
                Assert.AreEqual(3, record.Length);
                Assert.AreEqual("one", record[0]);
                Assert.AreEqual("two", record[1]);
                Assert.AreEqual("three", record[2]);

                Assert.IsNull(parser.Read());
            }
        }

        [TestMethod]
        public void Parse2RecordsLastWithNoCrlfTest()
        {
            using (var memoryStream = new MemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var parser = new DelimitedTextParser(streamReader))
            {
                streamWriter.WriteLine("one,two,three");
                streamWriter.Write("four,five,six");
                streamWriter.Flush();
                memoryStream.Position = 0;

                parser.Read();
                var record = parser.Read();
                Assert.IsNotNull(record);
                Assert.AreEqual(3, record.Length);
                Assert.AreEqual("four", record[0]);
                Assert.AreEqual("five", record[1]);
                Assert.AreEqual("six", record[2]);

                Assert.IsNull(parser.Read());
            }
        }

        [TestMethod]
        public void ParseFirstFieldIsEmptyQuotedTest()
        {
            using (var memoryStream = new MemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var parser = new DelimitedTextParser(streamReader))
            {
                streamWriter.WriteLine("\"\",\"two\",\"three\"");
                streamWriter.Flush();
                memoryStream.Position = 0;

                var record = parser.Read();
                Assert.IsNotNull(record);
                Assert.AreEqual(3, record.Length);
                Assert.AreEqual("", record[0]);
                Assert.AreEqual("two", record[1]);
                Assert.AreEqual("three", record[2]);
            }
        }

        [TestMethod]
        public void ParseLastFieldIsEmptyQuotedTest()
        {
            using (var memoryStream = new MemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var parser = new DelimitedTextParser(streamReader))
            {
                streamWriter.WriteLine("\"one\",\"two\",\"\"");
                streamWriter.Flush();
                memoryStream.Position = 0;

                var record = parser.Read();
                Assert.IsNotNull(record);
                Assert.AreEqual(3, record.Length);
                Assert.AreEqual("one", record[0]);
                Assert.AreEqual("two", record[1]);
                Assert.AreEqual("", record[2]);
            }
        }

        [TestMethod]
        public void ParseQuoteOnlyQuotedFieldTest()
        {
            using (var memoryStream = new MemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var parser = new DelimitedTextParser(streamReader))
            {
                streamWriter.WriteLine("\"\"\"\",\"two\",\"three\"");
                streamWriter.Flush();
                memoryStream.Position = 0;

                var record = parser.Read();
                Assert.IsNotNull(record);
                Assert.AreEqual(3, record.Length);
                Assert.AreEqual("\"", record[0]);
                Assert.AreEqual("two", record[1]);
                Assert.AreEqual("three", record[2]);
            }
        }
    }
}
