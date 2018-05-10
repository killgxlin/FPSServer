using System;
using System.Collections.Generic;
using System.Reflection;

namespace Share.Network
{
    public class PBTypeDB
    {
        public PBTypeDB(Assembly assem)
        {
            Type info = null;

            foreach (var definedType in assem.DefinedTypes)
            {
                if (!definedType.FullName.Contains("Msg."))
                    continue;

                if (definedType.GetInterface("ProtoBuf.IExtensible") == null)
                    continue;

                var hashCode = definedType.FullName.GetHashCode();
                if (Types.TryGetValue(hashCode, out info))
                    throw new Exception(string.Format("{0} collide with {1}!!!!!!!!!!!", definedType.FullName,
                        info.FullName));

                Types.Add(hashCode, definedType);
            }
        }

        public Dictionary<int, Type> Types { get; } = new Dictionary<int, Type>();

        public Type FindType(int hashCode)
        {
            Type type = null;
            Types.TryGetValue(hashCode, out type);
            return type;
        }
    }
}