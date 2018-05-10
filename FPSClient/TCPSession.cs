using System;
using System.Diagnostics;
using System.Text;
using Share.Network;
using Share.Utils;

namespace FPSClient
{
    internal class TCPSession : IDisposable
    {
        public TCPClient cli;
        public Action OnConnect;
        public Action OnDisconnect;

        public Action<string> OnMsg;

        private long peerId;


        public TCPSession()
        {
            cli = new TCPClient();

            cli.OnConnect = peerId =>
            {
                Debug.Assert(this.peerId == 0);

                Connecting = false;
                Connected = true;
                this.peerId = peerId;

                OnConnect?.Invoke();
            };

            cli.OnDisconnect = peerId =>
            {
                Debug.Assert(this.peerId == peerId);


                Connecting = false;
                Connected = false;
                this.peerId = 0;

                OnDisconnect?.Invoke();
            };
            cli.OnReceive = (peerId, bytes, channelId) =>
            {
                Debug.Assert(this.peerId == peerId);

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

        public bool Connected { private set; get; }
        public bool Connecting { private set; get; }

        public void Dispose()
        {
            cli.Dispose();
            cli = null;
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

        public void Update()
        {
            cli.Update();
        }
    }
}