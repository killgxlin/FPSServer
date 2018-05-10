using System;
using System.Threading;
using FPSServer.Actors;
using Proto;
using Share.Utils;

namespace FPSServer
{
    internal class Global
    {
        public static PID NetworkPID;
        public static PID RoomPID;
    }

    internal class Program
    {
        private static void Main(string[] arg)
        {
            Global.NetworkPID = Actor.SpawnNamed(Actor.FromProducer(() => new NetworkActor()), "Network");
            Global.RoomPID = Actor.SpawnNamed(Actor.FromProducer(() => new RoomActor()), "Room");
            try
            {
                var quit = false;
                var handler = new ConsoleCmdHandler();
                handler.OnCmd = async args =>
                {
                    switch (args[0])
                    {
                        case "q":
                            quit = true;
                            break;
                    }
                };
                while (!quit)
                {
                    handler.Update();
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                LoggerUtil.Fatal(typeof(Program), e.ToString());
            }

            Global.RoomPID.Stop();
            Global.NetworkPID.Stop();
            LoggerUtil.Info(typeof(Program), "server exited");
        }
    }
}