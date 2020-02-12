namespace Dedup
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    class RecordWriter : IDisposable
    {
        private readonly FileStream stream;
        private readonly BinaryFormatter formatter;

        public RecordWriter(string path)
        {
            this.stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            this.formatter = new BinaryFormatter();
        }

        public void Dispose() => this.stream.Close();

        public void Write(Record record) => this.formatter.Serialize(this.stream, record);

        public static void WriteAll(string path, List<Record> records)
        {
            using (var rw = new RecordWriter(path))
            {
                foreach (var r in records)
                {
                    rw.Write(r);
                }
            }
        }
    }
}
