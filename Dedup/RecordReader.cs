namespace Dedup
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    class RecordReader : IDisposable
    {
        private readonly FileStream stream;
        private readonly BinaryFormatter formatter;

        public RecordReader(string path)
        {
            this.stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            this.formatter = new BinaryFormatter();
        }

        public void Dispose() => this.stream.Close();

        public bool HasNext() => this.stream.Position != this.stream.Length;

        public Record Next() => (Record)this.formatter.Deserialize(this.stream);

        public static List<Record> ReadAll(string path)
        {
            var list = new List<Record>();
            using (var rr = new RecordReader(path))
            {
                while (rr.HasNext())
                {
                    list.Add(rr.Next());
                }
            }

            return list;
        }
    }
}
