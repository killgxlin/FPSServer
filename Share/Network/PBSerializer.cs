using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Share.Network
{
    public class PBSerializer
    {
        public static IExtensible Deserialize(PBTypeDB db, byte[] bytes)
        {
            var hashCode = BitConverter.ToInt32(bytes, 0);
            var type = db.FindType(hashCode);
            if (type == null) throw new Exception(string.Format("MsgId:{0} not exist", hashCode));

            var inst = Activator.CreateInstance(type);
            using (var mem = new MemoryStream(bytes.Skip(4).ToArray()))
            {
                return RuntimeTypeModel.Default.Deserialize(mem, inst, type) as IExtensible;
            }
        }

        private static IExtensible deserialize(Type type, byte[] bytes)
        {
            var hashCode = BitConverter.ToInt32(bytes.Take(4).ToArray(), 0);
            Debug.Assert(hashCode == type.FullName.GetHashCode());
            using (var mem = new MemoryStream(bytes.Skip(4).ToArray()))
            {
                var inst = Activator.CreateInstance(type);
                return RuntimeTypeModel.Default.Deserialize(mem, inst, type) as IExtensible;
            }
        }

        public static byte[] Serialize(IExtensible msg)
        {
            var msgId = msg.MsgId();
            var head = BitConverter.GetBytes(msgId);
            using (var mem = new MemoryStream())
            {
                mem.Write(head, 0, 4);
                RuntimeTypeModel.Default.Serialize(mem, msg);
                var len = (int) mem.Length;
                return mem.GetBuffer().Take(len).ToArray();
            }
        }

        public static IExtensible CheckSerialize(IExtensible obj)
        {
            var bytes = Serialize(obj);
            var msg = deserialize(obj.GetType(), bytes);
            return msg;
        }

        public static void Test()
        {
            var msgs = new IExtensible[]
            {
/*
                new NsScriptTest { Id = 123, Name = "hello" },
                new PbVec3 { X = 0.0f, Y = 1.0f, Z = 2.0f },
*/
            };
            foreach (var extensible in msgs) CheckSerialize(extensible);
        }
    }
}