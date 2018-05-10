using ProtoBuf;

namespace Share.Network
{
    public static class PBUtil
    {
        public const int MAX_SIZE = 1024 * 1024;
        private const int HEADER_SIZE = 4;

        public static int MsgId(this IExtensible msg)
        {
            return msg.GetType().FullName.GetHashCode();
        }
    }
}