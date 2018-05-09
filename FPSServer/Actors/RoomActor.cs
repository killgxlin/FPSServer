using System;
using System.Threading.Tasks;
using FPSServer.Logics;
using Proto;
using ProtoBuf;
using Share;

namespace FPSServer.Actors
{
    internal class RoomCmdEnter
    {
        public Int64 peerId;
        public Int64 playerId;
    }
    internal class RoomCmdExit
    {
        public Int64 peerId;
    }
    internal class RoomCmdMsg
    {
        public Int64 peerId;
        public IExtensible msg;
    }

    internal class RoomActor : IActor
    {
        private Room room = new Room();

        public Task ReceiveAsync(IContext context)
        {
            Util.LogMsg(context);
            switch (context.Message)
            {
                case Proto.Started started:
                    room.Kick = kick;
                    room.SendMsg = sendMsg;
                    room.Init();
                    context.Self.Tell("heart");
                    break;
                case Proto.Stopping stopping:
                    room.Destory();
                    break;
                case Proto.Stopped stopped:
                    break;
                case RoomCmdMsg rMsg:
                    room?.RecvMsg(rMsg.peerId, rMsg.msg);
                    break;
                case RoomCmdEnter enter:
                    room?.Login(enter.peerId);
                    break;
                case RoomCmdExit exit:
                    room?.Logout(exit.peerId);
                    break;
                case string cmd:
                    if (cmd == "heart")
                    {
                        room.Update();
                        context.Self.Tell("heart");
                    }
                    break;
            }
            return Actor.Done;
        }

        private void sendMsg(Int64 peerId, IExtensible msg)
        {
            Global.NetworkPID.Tell(new NetworkCmd { peerId = peerId, msg = msg });
        }

        private void kick(Int64 peerId)
        {
            Global.NetworkPID.Tell(new NetworkCmd { peerId = peerId, disconnect = true });
        }
    }
}
