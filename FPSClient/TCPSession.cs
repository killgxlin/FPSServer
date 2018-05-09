using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Share.Network;
using Share.Utils;

namespace FPSClient
{
    class TCPSession : IDisposable
    {
        public TCPClient cli;

        private Int64 peerId = 0;

        public bool Connected { private set; get; } = false;
        public bool Connecting { private set; get; } = false;

        public Action<string> OnMsg;
        public Action OnConnect;
        public Action OnDisconnect;


        public TCPSession()
        {
            cli = new TCPClient();

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

                try
                {
                    var msg = Encoding.ASCII.GetString(bytes);
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
            if (peerId != 0 || Connected)
            {
                this.Warn("already connected");
                return;
            }

            if (Connecting)
            {
                this.Warn("connecting");
                return;
            }

            cli.Connect("10.235.157.230", 8010);
        }

        public void Disconnect()
        {
            if (peerId == 0)
            {
                this.Warn("not conect yet");
                return;
            }

            if (!Connected)
                return;

            cli.Disconnect(peerId);
        }

        public void Send(string msg)
        {
            if (peerId == 0)
            {
                this.Warn("not conect yet");
                return;
            }

            var frame = Encoding.ASCII.GetBytes(msg);
            var builder = new ByteBuilder();
            builder.Add(frame);
            cli.SendBytes(peerId, builder.ToArrayThenClear());
        }

        public void Dispose()
        {
            cli.Dispose();
            cli = null;
        }

        public void Update()
        {
            cli.Update();
        }
    }
}
