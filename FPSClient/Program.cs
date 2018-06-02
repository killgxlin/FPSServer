using System;
using System.Linq;
using System.Text;
using Share.Network;
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
                using (var cli = new UDPNetworkMT())
                {
                    cli.Trace = true;
                    cli.OnConnect = () => LoggerUtil.Info(typeof(Program), "connected");
                    cli.OnDisconnect = () => LoggerUtil.Info(typeof(Program), "disconnected");
                    cli.OnReceive = (bytes, channelId) => LoggerUtil.Info(typeof(Program), "recv: " + Encoding.ASCII.GetString(bytes));
                    
                    handler.OnCmd = args =>
                    {
                        switch (args[0])
                        {
                            case "c":
                                cli.Connect("127.0.0.1", 1234);
                                break;
                            case "q":
                                cli.Disconnect();
                                quit = true;
                                break;
                            case "dc":
                                cli.Disconnect();
                                break;
                            case "p":
                                cli.Pause = !cli.Pause;
                                LoggerUtil.Info(typeof(Program), "pause:{0}", cli.Pause);
                                break;
                            default:
                                var cmd = string.Join(" ", args);
                                if (cmd.StartsWith("`"))
                                {
                                    cli.Send(Encoding.ASCII.GetBytes(cmd.Remove(0,1)));
                                }

                                break;
                        }
                    };

                    while (!quit)
                    {
                        handler.Update();

                        cli.Update();                            
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