using System;
using System.Collections.Generic;
using System.Reflection;
using ProtoBuf;

namespace Share.Network
{


    class PBDispatcher
    {
        class HandlerCb
        {
            public object handler;
            public Type handlerType;
            public MethodInfo method;
        }
        class TypeInfo
        {
            public Type type;
            public List<HandlerCb> handlers = new List<HandlerCb>();
        }
        private Dictionary<int, TypeInfo> msgTypes = new Dictionary<int, TypeInfo>();

        public Type FindType(int msgId)
        {
            TypeInfo info = null;
            if (!msgTypes.TryGetValue(msgId, out info))
                return null;

            return info.type;
        }

        public PBDispatcher()
        {
            init();
        }
        private void init()
        {
            TypeInfo info = null;
            var a = Assembly.GetAssembly(typeof(PBDispatcher));
            foreach (var definedType in a.DefinedTypes)
            {
                if (!definedType.FullName.Contains("Msg."))
                    continue;

                if (definedType.GetInterface("ProtoBuf.IExtensible") == null)
                    continue;

                var hashCode = definedType.FullName.GetHashCode();
                if (msgTypes.TryGetValue(hashCode, out info))
                    throw new Exception(string.Format("{0} collide with {1}!!!!!!!!!!!", definedType.FullName, info.type.FullName));

                msgTypes.Add(hashCode, new TypeInfo { type = definedType });
            }
        }

        public void Dispatch(IExtensible obj)
        {
            var msgId = PBUtil.MsgId(obj);
            TypeInfo info = null;
            if (!msgTypes.TryGetValue(msgId, out info))
            {
                return;
            }
            
            if (info.handlers.Count == 0)
            {
                return;
            }

            var args = new object[] { obj };
            info.handlers.ForEach(hcb =>
            {
                try
                {
                    hcb.method.Invoke(hcb.handler, args);
                }
                catch (Exception e)
                {
                    throw;
                }
                
            });
        }

        private Type getParaType(MethodInfo methodInfo, bool needStatic)
        {
            if (needStatic)
            {
                if (!methodInfo.IsStatic)
                    return null;
            }
            else
            {
                if (methodInfo.IsStatic)
                    return null;
            }
                

            if (!methodInfo.Name.Contains("On"))
                return null;

            if (methodInfo.Name != "On")
                return null;

            var paras = methodInfo.GetParameters();
            if (paras.Length != 1)
                return null;

            var paraType = paras[0].ParameterType;
            if (!paraType.FullName.StartsWith("Msg."))
                return null;

            return paraType;
        }

        private List<object> handlers = new List<object>();

        public void AddHandler(object handler)
        {
            if (handlers.Contains(handler))
                return;

            var methods = handler.GetType().GetRuntimeMethods();
            foreach (var methodInfo in methods)
            {
                var paraType = getParaType(methodInfo, false);
                if (paraType == null)
                    continue;

                var hashCode = paraType.FullName.GetHashCode();
                msgTypes[hashCode].handlers.Add(new HandlerCb { handler = handler, method = methodInfo });
            }

            handlers.Add(handler);
        }

        public void RemoveHandler(object handler)
        {
            if (!handlers.Contains(handler))
                return;

            var methods = handler.GetType().GetRuntimeMethods();
            foreach (var methodInfo in methods)
            {
                var paraType = getParaType(methodInfo, false);
                if (paraType == null)
                    continue;

                var hashCode = paraType.FullName.GetHashCode();
                msgTypes[hashCode].handlers.RemoveAll(hcb => hcb.handler == handler);
            }

            handlers.Remove(handler);
        }

        private List<Type> typeHandlers = new List<Type>();

        public void AddHandler(Type handler)
        {
            if (typeHandlers.Contains(handler))
                return;

            var methods = handler.GetRuntimeMethods();
            foreach (var methodInfo in methods)
            {
                var paraType = getParaType(methodInfo, true);
                if (paraType == null)
                    continue;

                var hashCode = paraType.FullName.GetHashCode();
                msgTypes[hashCode].handlers.Add(new HandlerCb { handlerType = handler, method = methodInfo });
            }

            typeHandlers.Add(handler);
        }

        public void RemoveHandler(Type handler)
        {
            if (!typeHandlers.Contains(handler))
                return;

            var methods = handler.GetRuntimeMethods();
            foreach (var methodInfo in methods)
            {
                var paraType = getParaType(methodInfo, true);
                if (paraType == null)
                    continue;

                var hashCode = paraType.FullName.GetHashCode();
                msgTypes[hashCode].handlers.RemoveAll(hcb => hcb.handlerType == handler);
            }

            typeHandlers.Remove(handler);
        }
        
        static public void Test(string[] args)
        {
            var d = new PBDispatcher();
            d.init();
            //d.AddHandler(h1);
            //d.AddHandler(h2);
            //d.TestDispatch(new NsScriptTest {Id = 123, Name = "hello"});
            //d.TestDispatch(new PbVec3 {X=0.0f,Y=1.0f,Z=2.0f});
            //d.RemoveHandler(h2);
            //d.RemoveHandler(h1);
            //d.TestDispatch(new NsScriptTest { Id = 123, Name = "hello" });
            //d.TestDispatch(new PbVec3 { X = 0.0f, Y = 1.0f, Z = 2.0f });

            return;

        }
    }

}
