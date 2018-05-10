using System;
using Share.Sync;
using Share.Utils;

namespace FPSClient
{
    internal class Program
    {
        private static void Main(string[] arg)
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
                    sess.OnMsg = msg => { sess.Trace("msg:{0}", msg); };

                    sess.OnConnect = () => { sess.Trace("connected"); };

                    sess.OnDisconnect = () => { sess.Trace("disconnected"); };


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