using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Proto;
using ProtoBuf;
using Share.Network;
using Share.Utils;

namespace FPSServer.Actors
{
    internal class NetworkCmd
    {
        public bool disconnect = false;
        public IExtensible msg;
        public long peerId;
    }

    internal class NetworkActor : IActor
    {
        private readonly Dictionary<long, PeerData> peerToCodec = new Dictionary<long, PeerData>();
        private PID selfPID;
        private readonly UDPServer svr = new UDPServer();
        private readonly PBTypeDB typeDb = new PBTypeDB(Assembly.GetExecutingAssembly());

        public Task ReceiveAsync(IContext context)
        {
            Util.LogMsg(context);
            switch (context.Message)
            {
                case Started started:
                    selfPID = context.Self;
                    svr.Listen(1234);
                    svr.OnConnect = onConnect;
                    svr.OnDisconnect = onDisconnect;
                    svr.OnReceive = onReceive;
                    context.Self.Tell("heart");
                    break;
                case Stopping stopping:
                    break;
                case Stopped stopped:
                    svr.Dispose();
                    break;
                case NetworkCmd cmd:
                    sendMsg(cmd.peerId, cmd.msg);
                    if (cmd.disconnect) disconnect(cmd.peerId);
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

        private void onConnect(long peerId)
        {
            Debug.Assert(!peerToCodec.ContainsKey(peerId));

            var data = new PeerData();
            peerToCodec[peerId] = data;
            Global.RoomPID.Tell(new RoomCmdEnter {peerId = peerId});

            data.codec.FrameCb = frame =>
            {
                try
                {
                    var msg = PBSerializer.Deserialize(typeDb, frame);
                    Global.RoomPID.Tell(new RoomCmdMsg {msg = msg, peerId = peerId});
                }
                catch (Exception e)
                {
                    this.Fatal(e.ToString());
                }
            };
        }


        private void sendMsg(long peerId, IExtensible msg)
        {
            var frame = PBSerializer.Serialize(msg);
            var builder = new ByteBuilder();
            builder.Add(frame.Length, Endians.Little);
            builder.Add(frame);
            svr.SendBytes(peerId, builder.ToArrayThenClear());
        }

        private void disconnect(long peerId)
        {
            svr.Disconnect(peerId);
        }

        private void onDisconnect(long peerId)
        {
            Debug.Assert(peerToCodec.ContainsKey(peerId));

            var data = peerToCodec[peerId];
            peerToCodec.Remove(peerId);

            Global.RoomPID.Tell(new RoomCmdExit {peerId = peerId});
        }

        private void onReceive(long peerId, byte[] bytes, byte channelId)
        {
            Debug.Assert(peerToCodec.ContainsKey(peerId));

            var data = peerToCodec[peerId];
            data.codec.Feed(bytes);
        }

        private class PeerData
        {
            public readonly FrameCodec codec;

            public PeerData()
            {
                codec = new FrameCodec();
            }
        }
    }
}