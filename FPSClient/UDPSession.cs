using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Msg;
using Newtonsoft.Json;
using ProtoBuf;
using Share;
using Share.Network;
using Share.Utils;

namespace FPSClient
{
    class UDPSession : IDisposable
    {
        public PBTypeDB typeDb;
        public UDPClient cli;
        public FrameCodec codec;

        private Int64 peerId = 0;

        public bool Connected { private set; get; } = false;
        public bool Connecting { private set; get; } = false;

        public Action<IExtensible> OnMsg;
        public Action OnConnect;
        public Action OnDisconnect;


        public UDPSession()
        {
            codec = new FrameCodec();
            typeDb = new PBTypeDB(Assembly.GetExecutingAssembly());
            cli = new UDPClient();

            cli.OnConnect = peerId =>
            {
                System.Diagnostics.Debug.Assert(this.peerId == 0);

                Connecting = false;
                Connected = true;
                this.peerId = peerId;

                OnConnect?.Invoke();
            };

            cli.OnDisconnect = peerId =>
            {
                System.Diagnostics.Debug.Assert(this.peerId == peerId);


                Connecting = false;
                Connected = false;
                this.peerId = 0;

                OnDisconnect?.Invoke();
            };
            cli.OnReceive = (peerId, bytes, channelId) =>
            {
                System.Diagnostics.Debug.Assert(this.peerId == peerId);

                codec.Feed(bytes);
            };

            codec.FrameCb = frame =>
            {
                try
                {
                    var msg = PBSerializer.Deserialize(typeDb, frame);
                    OnMsg?.Invoke(msg);
                }
                catch (Exception e)
                {
                    this.Fatal("{0}:{1}", peerId, e.ToString());
                }
            };

        }

        public void Connect()
        {
            System.Diagnostics.Debug.Assert(peerId == 0 && Connecting == false && Connected == false);
            cli.Connect("127.0.0.1", 1234);
        }

        public void Disconnect()
        {
            System.Diagnostics.Debug.Assert(peerId != 0);
            if (!Connected)
                return;

            cli.Disconnect(peerId);
        }

        public void Send(IExtensible msg)
        {
            System.Diagnostics.Debug.Assert(peerId != 0);

            var frame = PBSerializer.Serialize(msg);
            var builder = new ByteBuilder();
            builder.Add(frame.Length, Endians.Little);
            builder.Add(frame);
            cli.SendBytes(peerId, builder.ToArrayThenClear());
        }

        public void Dispose()
        {
            cli.Dispose();
            cli = null;
            codec = null;
            typeDb = null;
        }

        public void Update()
        {
            cli.Update();
        }
    }
}

