namespace Dedup
{
    using System;

    [Serializable]
    class Record: IComparable<Record>
    {
        public Record(string info, int index)
        {
            this.Info = info;
            this.Index = index;
        }

        public string Info { get; }
        public int Index { get; }

        public int CompareTo(Record other) => this.Info.CompareTo(other.Info);
    }
}
