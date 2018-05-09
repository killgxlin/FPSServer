using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FPSServer.Actors;
using Msg;
using Proto;
using ProtoBuf;
using Share;
using Share.Network;
using Share.Utils;

namespace FPSServer
{
    class Global
    {
        static public PID NetworkPID;
        static public PID RoomPID;
    }
    class Program
    {
        static void Main(string[] arg)
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
