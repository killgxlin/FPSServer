using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;
using Share;
using Newtonsoft.Json;
using Share.Utils;

namespace FPSServer
{
    static class Util
    {
        public static void LogMsg(IContext context)
        {
            var log = true;
            if (context.Message is string scmd)
            {
                if (scmd == "heart")
                {
                    log = false;
                }
            }

            if (log)
            {
                LoggerUtil.Trace(context.Actor.GetType(), "Actor:{0} recv:{1}_{2}", context.Self.ToString(), context.Message.GetType().FullName, JsonConvert.SerializeObject(context.Message));
            }

        }
    }
}
