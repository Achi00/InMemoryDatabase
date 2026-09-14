using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;
using System.Buffers;
using System.Text;

namespace InMemoryDatabase.Resp
{
    public static class RespWriter
    {
        public static void Write(RespValue value, IBufferWriter<byte> writer)
        {
            switch (value.Type)
            {
                case RespValueType.SimpleString:
                    WriteLine(writer, '+', value.TypeString!);
                    break;
                case RespValueType.Error:
                    WriteLine(writer, '-', value.TypeString!);
                    break;
                case RespValueType.Integer:
                    WriteLine(writer, ':', value.TypeInteger.ToString());
                    break;
                case RespValueType.BulkString:
                    WriteBulkString(writer, value.TypeString);
                    break;
                case RespValueType.Null:
                    WriteRaw(writer, "$-1\r\n");
                    break;
                case RespValueType.Array:
                    WriteRaw(writer, $"*{value.TypeArray!.Length}\r\n");
                    foreach (var item in value.TypeArray)
                        Write(item, writer);
                    break;
            }
        }

        private static void WriteLine(IBufferWriter<byte> writer, char prefix, string content) =>
            WriteRaw(writer, $"{prefix}{content}\r\n");

        private static void WriteBulkString(IBufferWriter<byte> writer, string? s)
        {
            if (s is null)
            {
                WriteRaw(writer, "$-1\r\n");
                return;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            WriteRaw(writer, $"${bytes.Length}\r\n");
            Span<byte> span = writer.GetSpan(bytes.Length);
            bytes.CopyTo(span);
            writer.Advance(bytes.Length);
            WriteRaw(writer, "\r\n");
        }

        private static void WriteRaw(IBufferWriter<byte> writer, string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            Span<byte> span = writer.GetSpan(bytes.Length);
            bytes.CopyTo(span);
            writer.Advance(bytes.Length);
        }
    }
}
