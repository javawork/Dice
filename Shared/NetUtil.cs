using Google.Protobuf;

namespace Shared
{
    public static class NetUtil
    {
        private const int OneByteInBits = 8;
        public const int HeaderSize = 4;

        public static int SerializeHeader(short size, ushort code)
        {
            var header = (int)size;
            header <<= OneByteInBits;
            header += (int)code;
            return header;
        }

        public static void DeserializeHeader(int header, out ushort size, out ushort code)
        {
            size = (ushort)(header >> OneByteInBits);
            int tempByte = (int)size << OneByteInBits;
            code = (ushort)(header - tempByte);
        }

        public static byte[] SerializeMessageToBytes(ushort code, IMessage message)
        {
            var bodySize = CodedOutputStream.ComputeGroupSize(message);
            var buffer = new byte[bodySize + HeaderSize];
            var stream = new CodedOutputStream(buffer);
            var header = SerializeHeader((short)bodySize, code);
            stream.WriteSFixed32(header);
            message.WriteTo(stream);
            return buffer;
        }
    }
}
