namespace Dedup
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    class Program
    {
        private static readonly Comparer<Record> InfoComparer = Comparer<Record>.Create((r1, r2) => r1.Info.CompareTo(r2.Info));
        private static readonly Comparer<Record> IndexComparer = Comparer<Record>.Create((r1, r2) => r1.Index.CompareTo(r2.Index));

        static int Main(string[] args)
        {
            if (args.Length == 2)
            {
                Run(args[0], args[1]);
            }
            else if (args.Length == 3)
            {
                Run(args[0], args[1], int.Parse(args[2]));
            }
            else
            {
                Console.WriteLine("Usage: Dedup <InputFile> <OutputFile> [<RecordsPerFile>]");
                return 1;
            }

            return 0;
        }

        private static void Run(string inputFile, string outputFile, int recordsPerFile = 100)
        {
            // Encode the input lines with the line position in the original document. This step converts the input
            // lines to Record objects. A Record is an abstract representation of an indexed line. The data is
            // processed in stream in order to handle scalable input files.
            Encode(inputFile, out var indexedFile);

            // Segment the indexed file into smaller segments, so that each segment can be sort in-memory.
            Segment(indexedFile, out var dupSegments, recordsPerFile);

            // Sort lines in each segmented files. Lines are sorted by their contents in a lexigraphic order.
            Parallel.ForEach(dupSegments, (file) => Sort(file, InfoComparer));

            // Merge sorted segments into one file. The data is processed in stream.
            Merge(dupSegments, out var mergedFile, InfoComparer);

            // Dedup the content by removing the adjacent identical lines. The data is processed in stream.
            Dedup(mergedFile, out var dedupedFile);

            // Segment the deduped records into segments.
            Segment(dedupedFile, out var dedupedSegments, recordsPerFile);

            // Sort lines in each segmented files by their position in the original document.
            Parallel.ForEach(dedupedSegments, (file) => Sort(file, IndexComparer));

            // Merge the segments into one file.
            Merge(dedupedSegments, out var dedupedMergedFile, IndexComparer);

            // Decode the content into a text file.
            Decode(dedupedMergedFile, outputFile);
        }

        #region Procedures

        private static void Encode(string inputPath, out string outputPath)
        {
            outputPath = GetTempFileName();

            using(var reader = new StreamReader(inputPath))
            using(var writer = new RecordWriter(outputPath))
            {
                string line;
                var idx = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    writer.Write(new Record(line, idx++));
                }
            }
        }
        private static void Decode(string inputPath, string outputPath)
        {
            using(var reader = new RecordReader(inputPath))
            using(var writer = new StreamWriter(outputPath))
            {
                while (reader.HasNext())
                {
                    writer.Write(reader.Next().Info + "\n");
                }
            }
        }

        private static void Segment(string inputFile, out List<string> outputFiles, int recordsPerFile = 3)
        {
            outputFiles = new List<string>();

            using (RecordReader reader = new RecordReader(inputFile))
            {
                var outputFile = GetTempFileName();
                outputFiles.Add(outputFile);
                var writer = new RecordWriter(outputFile);

                while (reader.HasNext())
                {
                    var record = reader.Next();

                    if (record.Index > 0 && record.Index % recordsPerFile == 0)
                    {
                        writer.Dispose();
                        outputFile = GetTempFileName();
                        outputFiles.Add(outputFile);
                        writer = new RecordWriter(outputFile);
                    }

                    writer.Write(record);
                }

                writer.Dispose();
            }
        }

        private static void Sort(string inputFile, string outputFile, IComparer<Record> comparer)
        {
            var records = RecordReader.ReadAll(inputFile);
            records.Sort(comparer);
            RecordWriter.WriteAll(outputFile, records);
        }

        private static void Sort(string inputFile, IComparer<Record> comparer)
        {
            Sort(inputFile, inputFile, comparer);
        }

        private static void Merge(string inputFile1, string inputFile2, out string outputFile, IComparer<Record> comparer)
        {
            outputFile = GetTempFileName();

            using (var reader1 = new RecordReader(inputFile1))
            using (var reader2 = new RecordReader(inputFile2))
            using (var writer = new RecordWriter(outputFile))
            {
                var record1 = reader1.Next();
                var record2 = reader2.Next();

                while (record1 != null && record2 != null)
                {
                    if (comparer.Compare(record1, record2) <= 0)
                    {
                        writer.Write(record1);
                        record1 = reader1.HasNext() ? reader1.Next() : null;
                    }
                    else
                    {
                        writer.Write(record2);
                        record2 = reader2.HasNext() ? reader2.Next() : null;
                    }
                }

                while (record1 != null)
                {
                    writer.Write(record1);
                    record1 = reader1.HasNext() ? reader1.Next() : null;
                }

                while (record2 != null)
                {
                    writer.Write(record2);
                    record2 = reader2.HasNext() ? reader2.Next() : null;
                }
            }
        }

        private static void Merge(List<string> inputFiles, out string outputFile, IComparer<Record> comparer)
        {
            if (inputFiles.Count == 1)
            {
                outputFile = inputFiles[0];
            }
            else if (inputFiles.Count == 2)
            {
                Merge(inputFiles[0], inputFiles[1], out outputFile, comparer);
            }
            else
            {
                Merge(inputFiles.GetRange(0, inputFiles.Count / 2), out var mergedFile1, comparer);
                Merge(inputFiles.GetRange(inputFiles.Count / 2, inputFiles.Count - inputFiles.Count / 2), out var mergedFile2, comparer);
                Merge(mergedFile1, mergedFile2, out outputFile, comparer);
            }
        }

        private static void Dedup(string inputFile, out string outputFile)
        {
            outputFile = GetTempFileName();

            using (var reader = new RecordReader(inputFile))
            using (var writer = new RecordWriter(outputFile))
            {
                Record prev = null;
                while (reader.HasNext())
                {
                    var curr = reader.Next();
                    if (prev == null || !curr.Info.Equals(prev.Info))
                    {
                        writer.Write(curr);
                    }

                    prev = curr;
                }
            }
        }

        #endregion

        #region Helper methods

        private static string GetTempFileName()
        {
            return string.Format("temp-{0}.tmp", Guid.NewGuid().ToString().Substring(0, 4));
        }

        private static void PrintFile(string path)
        {
            Console.WriteLine("  Print file: " + path);
            using (var rr = new RecordReader(path))
            {
                while (rr.HasNext())
                {
                    Console.WriteLine("  " + rr.Next().Info);
                }
            }
        }

        #endregion
    }
}
