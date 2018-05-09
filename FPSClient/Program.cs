using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Msg;
using Share.Network;
using ProtoBuf;
using Share;
using Newtonsoft.Json;
using System.Diagnostics;
using Share.Sync;
using Share.Utils;

namespace FPSClient
{
    class Program
    {
        
        static void Main(string[] arg)
        {

            try
            {
                SyncScene scene = null;
                var quit = false;
                var handler = new ConsoleCmdHandler();
                LoggerUtil.Info(typeof(Program), "client inited");
                using (var sess = new TCPSession())
                {
                    sess.Connect();
                    sess.OnMsg = (msg) =>
                    {
                        sess.Trace("msg:{0}", msg);
                    };

                    sess.OnConnect = () => 
                    {
                        sess.Trace("connected");
                    };

                    sess.OnDisconnect = () =>
                    {
                        sess.Trace("disconnected");
                    };


                    handler.OnCmd = args =>
                    {
                        switch (args[0])
                        {
                            case "c":
                                sess.Send(args[1]);
                                break;
                            case "q":
                                sess.Disconnect();
                                quit = true;
                                break;
                            case "dconn":
                                sess.Disconnect();
                                break;
                            case "conn":
                                sess.Connect();
                                break;
                        }
                    };

                    while (!quit)
                    {
                        handler.Update();
                        sess.Update();
                    }
                }

            }
            catch (Exception e)
            {
                LoggerUtil.Fatal(typeof(Program), e.ToString());
            }

            LoggerUtil.Info(typeof(Program), "client exited");
        }
    }
}
