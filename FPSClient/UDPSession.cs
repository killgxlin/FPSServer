﻿using System;
using System.Diagnostics;
using System.Reflection;
using ProtoBuf;
using Share.Network;
using Share.Utils;

namespace FPSClient
{
    internal class UDPSession : IDisposable
    {
        public UDPClient cli;
        public FrameCodec codec;
        public Action OnConnect;
        public Action OnDisconnect;

        public Action<IExtensible> OnMsg;

        private long peerId;
        public PBTypeDB typeDb;


        public UDPSession()
        {
            codec = new FrameCodec();
            typeDb = new PBTypeDB(Assembly.GetExecutingAssembly());
            cli = new UDPClient();

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

        public bool Connected { private set; get; }
        public bool Connecting { private set; get; }

        public void Dispose()
        {
            cli.Dispose();
            cli = null;
            codec = null;
            typeDb = null;
        }

        public void Connect()
        {
            Debug.Assert(peerId == 0 && Connecting == false && Connected == false);
            cli.Connect("127.0.0.1", 1234);
        }

        public void Disconnect()
        {
            Debug.Assert(peerId != 0);
            if (!Connected)
                return;

            cli.Disconnect(peerId);
        }

        public void Send(IExtensible msg)
        {
            Debug.Assert(peerId != 0);

            var frame = PBSerializer.Serialize(msg);
            var builder = new ByteBuilder();
            builder.Add(frame.Length, Endians.Little);
            builder.Add(frame);
            cli.SendBytes(peerId, builder.ToArrayThenClear());
        }

        public void Update()
        {
            cli.Update();
        }
    }
}