using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Asap42ToXml
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var asapFilePath = args[0];
            var xmlFilePath = args[1];
            var includeEmptyAttributes = args.Length > 2 && args[2].StartsWith("/i", StringComparison.CurrentCultureIgnoreCase);

            using (var inFile = File.OpenRead(asapFilePath))
            using (var outFile = XmlWriter.Create(
                File.Open(xmlFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None),
                new XmlWriterSettings() { Async = true, Indent = true }))
            {
                await CopyAsapToXmlAsync(inFile, outFile, includeEmptyAttributes);
            }
        }

        private static async Task CopyAsapToXmlAsync(FileStream inFile, XmlWriter outFile, bool includeEmptyAttributes)
        {
            await outFile.WriteStartElementAsync(null, "Asap4File", null);

            var pipe = new Pipe();
            var readStreamPromise = ReadFileToPipe(inFile, pipe.Writer);
            var writeStreamPromise = ReadPipeToXml(pipe.Reader, outFile, includeEmptyAttributes);
            await Task.WhenAll(readStreamPromise, writeStreamPromise);

            await outFile.WriteEndElementAsync();
        }

        private static async Task ReadFileToPipe(FileStream inFile, PipeWriter pipe)
        {
            while (true)
            {
                var memory = pipe.GetMemory(4096);
                int bytesRead = await inFile.ReadAsync(memory);
                if (bytesRead == 0)
                {
                    break;
                }
                pipe.Advance(bytesRead);

                await pipe.FlushAsync();
            }

            pipe.Complete();
        }

        private static async Task ReadPipeToXml(PipeReader pipe, XmlWriter outFile, bool includeEmptyAttributes)
        {
            while (true)
            {
                var result = await pipe.ReadAsync();

                var buffer = result.Buffer;
                SequencePosition? position = null;

                bool ignoreNextIfEmpty = false;
                do
                {
                    position = buffer.PositionOf((byte)'\\');

                    if (position != null)
                    {
                        ignoreNextIfEmpty = await ProcessLineAsync(buffer.Slice(0, position.Value), outFile, includeEmptyAttributes, ignoreNextIfEmpty);
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);

                pipe.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            pipe.Complete();
        }

        private static async Task<bool> ProcessLineAsync(ReadOnlySequence<byte> lineSequence, XmlWriter outFile, bool includeEmptyAttributes, bool ignoreNextIfEmpty)
        {
            var pos = lineSequence;
            var segmentName = ReadSegment(ref pos);
            if (string.IsNullOrWhiteSpace(segmentName))
            {
                if (ignoreNextIfEmpty)
                {
                    return false;
                }

                segmentName = "UNK";
            }

            await outFile.WriteStartElementAsync(null, segmentName, null);
            int segmentNo = 1;
            while (!pos.IsEmpty)
            {
                var value = ReadSegment(ref pos);
                var segment = $"{segmentName}{segmentNo++:00}";
                if (!string.IsNullOrEmpty(value) || includeEmptyAttributes)
                {
                    await outFile.WriteAttributeStringAsync(null, segment, null, value);
                }
            }
            await outFile.WriteEndElementAsync();

            // ignoreNextEmpty if TH since Header row has special processing in format
            return segmentName == "TH";
        }

        private static string ReadSegment(ref ReadOnlySequence<byte> pos)
        {
            if (pos.PositionOf((byte)'*') is SequencePosition limit)
            {
                var str = GetAsciiString(pos.Slice(0, limit));
                pos = pos.Slice(pos.GetPosition(1, limit));
                return str;
            }
            else
            {
                var str = GetAsciiString(pos);
                pos = pos.Slice(pos.End);
                return str;
            }
        }

        private static string GetAsciiString(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return Encoding.ASCII.GetString(buffer.First.Span);
            }

            return string.Create((int)buffer.Length, buffer, (span, sequence) =>
            {
                foreach (var segment in sequence)
                {
                    Encoding.ASCII.GetChars(segment.Span, span);

                    span = span.Slice(segment.Length);
                }
            });
        }
    }
}
