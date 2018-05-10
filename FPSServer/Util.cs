using Newtonsoft.Json;
using Proto;
using Share.Utils;

namespace FPSServer
{
    internal static class Util
    {
        public static void LogMsg(IContext context)
        {
            var log = true;
            if (context.Message is string scmd)
                if (scmd == "heart")
                    log = false;

            if (log)
                LoggerUtil.Trace(context.Actor.GetType(), "Actor:{0} recv:{1}_{2}", context.Self.ToString(),
                    context.Message.GetType().FullName, JsonConvert.SerializeObject(context.Message));
        }
    }
}