using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Proto;
using ProtoBuf;
using Share;
using Share.Network;
using Share.Utils;

namespace FPSServer.Actors
{
    internal class NetworkCmd
    {
        public IExtensible msg;
        public Int64 peerId;
        public bool disconnect = false;
    }

    internal class NetworkActor : IActor
    {
        Share.Network.UDPServer svr = new UDPServer();
        PBTypeDB typeDb = new PBTypeDB(Assembly.GetExecutingAssembly());
        private PID selfPID = null;
        public Task ReceiveAsync(IContext context)
        {
            Util.LogMsg(context);
            switch (context.Message)
            {
                case Proto.Started started:
                    selfPID = context.Self;
                    svr.Listen(1234);
                    svr.OnConnect = onConnect;
                    svr.OnDisconnect = onDisconnect;
                    svr.OnReceive = onReceive;
                    context.Self.Tell("heart");
                    break;
                case Proto.Stopping stopping:
                    break;
                case Proto.Stopped stopped:
                    svr.Dispose();
                    break;
                case NetworkCmd cmd:
                    sendMsg(cmd.peerId, cmd.msg);
                    if (cmd.disconnect)
                    {
                        disconnect(cmd.peerId);
                    }
                    break;
                case string cmd:
                    if (cmd == "heart")
                    {
                        svr.Update();
                        context.Self.Tell("heart");
                    }
                    break;
            }
            return Actor.Done;
        }

        private void onConnect(Int64 peerId)
        {
            System.Diagnostics.Debug.Assert(!peerToCodec.ContainsKey(peerId));

            var data = new PeerData();
            peerToCodec[peerId] = data;
            Global.RoomPID.Tell(new RoomCmdEnter { peerId = peerId });

            data.codec.FrameCb = frame =>
            {
                try
                {
                    var msg = PBSerializer.Deserialize(typeDb, frame);
                    Global.RoomPID.Tell(new RoomCmdMsg { msg = msg, peerId = peerId });
                }
                catch (Exception e)
                {
                    this.Fatal(e.ToString());
                }
            };
        }


        private void sendMsg(Int64 peerId, IExtensible msg)
        {
            var frame = PBSerializer.Serialize(msg);
            var builder = new ByteBuilder();
            builder.Add(frame.Length, Endians.Little);
            builder.Add(frame);
            svr.SendBytes(peerId, builder.ToArrayThenClear());
        }
        
        private void disconnect(Int64 peerId)
        {
            svr.Disconnect(peerId);
        }

        private void onDisconnect(Int64 peerId)
        {
            System.Diagnostics.Debug.Assert(peerToCodec.ContainsKey(peerId));

            var data = peerToCodec[peerId];
            peerToCodec.Remove(peerId);

            Global.RoomPID.Tell(new RoomCmdExit { peerId = peerId });
        }

        private void onReceive(Int64 peerId, byte[] bytes, byte channelId)
        {
            System.Diagnostics.Debug.Assert(peerToCodec.ContainsKey(peerId));

            var data = peerToCodec[peerId];
            data.codec.Feed(bytes);
        }

        private class PeerData
        {
            public FrameCodec codec;

            public PeerData()
            {
                codec = new FrameCodec();
            }
        }
        private Dictionary<Int64, PeerData> peerToCodec = new Dictionary<Int64, PeerData>();
    }
}
