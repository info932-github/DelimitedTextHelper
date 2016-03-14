using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DelimitedTextParserTest
{
    [TestClass]
    public class DelimitedTextReaderTests
    {
        [TestMethod]
        public void TestReaderGetRecord()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var reader = new StreamReader(stream))
            using(var dtReader = new DelimitedTextHelper.DelimitedTextReader(reader))
            {
                writer.Write("Field1,Field2,Field3,Field4,Field5\r\n");
                writer.Write("value1,100,true,\"12/31/2016\", 25.76\r\n");
                writer.Flush();
                stream.Position = 0;

                dtReader.FirstRowIsHeader = true;
                dtReader.Read();
                TestRecord trecord = dtReader.GetRecord<TestRecord>();
                Assert.IsNotNull(trecord);
                Assert.AreEqual("value1", trecord.Field1);
                Assert.AreEqual(100, trecord.Field2);
                Assert.IsTrue(trecord.Field3);
                Assert.AreEqual(DateTime.Parse("12/31/2016").ToShortDateString(), trecord.Field4.ToShortDateString());
                Assert.AreEqual(25.76M, trecord.Field5);
            }
        }

        [TestMethod]
        public void TestReaderPipeDelimitedGetRecord()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var reader = new StreamReader(stream))
            using (var dtReader = new DelimitedTextHelper.DelimitedTextReader(reader, '|'))
            {
                writer.Write("Field1|Field2|Field3|Field4|Field5\r\n");
                writer.Write("value1|100|true|\"12/31/2016\"| 25.76\r\n");
                writer.Flush();
                stream.Position = 0;

                dtReader.FirstRowIsHeader = true;
                dtReader.Read();
                TestRecord trecord = dtReader.GetRecord<TestRecord>();
                Assert.IsNotNull(trecord);
                Assert.AreEqual("value1", trecord.Field1);
                Assert.AreEqual(100, trecord.Field2);
                Assert.IsTrue(trecord.Field3);
                Assert.AreEqual(DateTime.Parse("12/31/2016").ToShortDateString(), trecord.Field4.ToShortDateString());
                Assert.AreEqual(25.76M, trecord.Field5);
            }
        }

        [TestMethod]
        public void GetRecordCaseInsensitiveTest()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var reader = new StreamReader(stream))
            using (var dtReader = new DelimitedTextHelper.DelimitedTextReader(reader))
            {
                writer.Write("field1,field2,Field3,Field4,Field5\r\n");
                writer.Write("value1,100,true,\"12/31/2016\", 25.76\r\n");
                writer.Flush();
                stream.Position = 0;

                dtReader.FirstRowIsHeader = true;
                dtReader.Read();
                TestRecord trecord = dtReader.GetRecord<TestRecord>();
                Assert.IsNotNull(trecord);
                Assert.AreEqual("value1", trecord.Field1);
                Assert.AreEqual(100, trecord.Field2);
                Assert.IsTrue(trecord.Field3);
                Assert.AreEqual(DateTime.Parse("12/31/2016").ToShortDateString(), trecord.Field4.ToShortDateString());
                Assert.AreEqual(25.76M, trecord.Field5);
            }
        }

        private class TestRecord
        {
            public string Field1 { get; set; }
            public int Field2 { get; set; }
            public bool Field3 { get; set; }
            public DateTime Field4 { get; set; }
            public decimal Field5 { get; set; }
        }

    }
}
