using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Share.Network
{
    public class FrameCodec
    {
        private readonly byte[] buffer = new byte[1024 * 1024];
        private int curLen;
        public Action<byte[]> FrameCb;


        public void Feed(byte[] bytes)
        {
            if (bytes.Length + curLen > buffer.Length) throw new Exception("msgcodec buffer full");
            Array.Copy(bytes, 0, buffer, curLen, bytes.Length);
            curLen += bytes.Length;

            for (var frame = frameChunk(); frame != null; frame = frameChunk()) FrameCb(frame);
        }

        private byte[] frameChunk()
        {
            if (curLen < 4)
                return null;

            var size = ByteConverter.ToInt32(buffer, 0, Endians.Little);

            if (curLen < size + 4)
                return null;

            var m = buffer.Skip(4).Take(size).ToArray().Clone();

            var copyLen = Math.Max(curLen - size - 4, 0);
            if (copyLen < 0 || copyLen > buffer.Length) return null;
            Buffer.BlockCopy(buffer, size + 4, buffer, 0, copyLen);
            curLen = curLen - size - 4;

            return m as byte[];
        }

        public static void Test()
        {
            var strings = new[]
            {
                "",
                "hello",
                "worldasdf asdf sdfasdf ",
                "worldasdf asdf  ",
                "sdfasdf ",
                "",
                "woasdf sdfasdf "
            };

            var index = 0;
            var c = new FrameCodec();
            c.FrameCb = frame => { Debug.Assert(Encoding.ASCII.GetString(frame) == strings[index++]); };

            foreach (var s in strings)
            {
                var bytes = Encoding.ASCII.GetBytes(s);

                c.Feed(ByteConverter.ToBytes(bytes.Length, Endians.Little));
                c.Feed(bytes.Take(3).ToArray());
                c.Feed(bytes.Skip(3).ToArray());
            }
        }
    }
}