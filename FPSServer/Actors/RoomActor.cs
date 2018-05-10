using System.Threading.Tasks;
using FPSServer.Logics;
using Proto;
using ProtoBuf;

namespace FPSServer.Actors
{
    internal class RoomCmdEnter
    {
        public long peerId;
        public long playerId;
    }

    internal class RoomCmdExit
    {
        public long peerId;
    }

    internal class RoomCmdMsg
    {
        public IExtensible msg;
        public long peerId;
    }

    internal class RoomActor : IActor
    {
        private readonly Room room = new Room();

        public Task ReceiveAsync(IContext context)
        {
            Util.LogMsg(context);
            switch (context.Message)
            {
                case Started started:
                    room.Kick = kick;
                    room.SendMsg = sendMsg;
                    room.Init();
                    context.Self.Tell("heart");
                    break;
                case Stopping stopping:
                    room.Destory();
                    break;
                case Stopped stopped:
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

        private void sendMsg(long peerId, IExtensible msg)
        {
            Global.NetworkPID.Tell(new NetworkCmd {peerId = peerId, msg = msg});
        }

        private void kick(long peerId)
        {
            Global.NetworkPID.Tell(new NetworkCmd {peerId = peerId, disconnect = true});
        }
    }
}