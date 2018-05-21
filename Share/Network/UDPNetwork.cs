using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ENet;
using Share.Utils;

namespace Share.Network
{
    public class UDPConnections
    {
        #region interface

        public Action<long> OnConnect;
        public Action<long, byte[], byte> OnReceive;
        public Action<long> OnDisconnect;

        public void Disconnect(long peerId)
        {
            peers[peerId].DisconnectLater(0);
        }

        public void SendBytes(long peerId, byte[] bytes, byte channel = 0)
        {
            Peer peer;
            if (!peers.TryGetValue(peerId, out peer)) return;

            peer.Send(0, bytes, PacketFlags.Reliable);
        }

        #endregion


        #region peer

        private readonly Dictionary<long, Peer> peers = new Dictionary<long, Peer>();

        protected unsafe void onConnect(Peer peer)
        {
            try
            {
                peer.SetTimeouts(0, 3000000, 3000000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            var ptr = (IntPtr) peer.NativeData;
            var peerId = ptr.ToInt64();
            peers.Add(peerId, peer);

            if (OnConnect != null)
                OnConnect(peerId);
        }

        protected unsafe void onReceive(Peer peer, byte[] bytes, byte channelId)
        {
            var ptr = (IntPtr) peer.NativeData;
            var peerId = ptr.ToInt64();
            if (OnReceive != null)
                OnReceive(peerId, bytes, channelId);
        }

        protected unsafe void onDisconnect(Peer peer)
        {
            var ptr = (IntPtr) peer.NativeData;
            var peerId = ptr.ToInt64();
            peers.Remove(peerId);
            if (OnDisconnect != null)
                OnDisconnect(peerId);
        }

        #endregion

        #region host

        protected Host host = new Host();

        public void Update()
        {
            Event ev;
            if (host.Service(0, out ev))
                do
                {
                    switch (ev.Type)
                    {
                        case EventType.Connect:
                            onConnect(ev.Peer);
                            break;
                        case EventType.Receive:
                            var packet = ev.Packet;
                            onReceive(ev.Peer, packet.GetBytes(), ev.ChannelID);
                            packet.Dispose();
                            break;
                        case EventType.Disconnect:
                            onDisconnect(ev.Peer);
                            break;
                    }
                } while (host.CheckEvents(out ev));
        }

        #endregion
    }

    public class UDPServer : UDPConnections, IDisposable
    {
        public void Dispose()
        {
            host.Dispose();
        }

        public void Listen(int port)
        {
            host.InitializeServer(port, 1000);
        }
    }

    public class UDPClient : UDPConnections, IDisposable
    {
        public UDPClient()
        {
            host.InitializeClient(1);
        }

        public void Dispose()
        {
            host.Dispose();
        }

        public unsafe long Connect(string ip, int port, int channelLimit = 10)
        {
            var peer = host.Connect(ip, port, 4444, channelLimit);
            var ptr = (IntPtr) peer.NativeData;
            return ptr.ToInt64();
        }
    }

    internal class UDPNetworkTest
    {
        public static void RunServer(object obj)
        {
            var peers = new HashSet<long>();
            using (var svr = new UDPServer())
            {
                svr.OnConnect = peerId =>
                {
                    peers.Add(peerId);
                    LoggerUtil.Info(typeof(UDPNetworkTest), "{0} connected", peerId);
                };
                svr.OnReceive = (peerId, bytes, channelId) =>
                {
                    var msg = Encoding.ASCII.GetString(bytes);
                    LoggerUtil.Info(typeof(UDPNetworkTest), "recv {0} msg {1}", peerId, msg);

                    bytes = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", peerId, msg));
                    foreach (var peer in peers)
                        if (peer != peerId)
                            svr.SendBytes(peer, bytes);
                };
                svr.OnDisconnect = peerId =>
                {
                    peers.Remove(peerId);
                    LoggerUtil.Info(typeof(UDPNetworkTest), "{0} disconnected", peerId);
                };

                svr.Listen(1234);

                while (true) svr.Update();
            }
        }

        public static void RunClient(object obj)
        {
            using (var cli = new UDPClient())
            {
                cli.OnConnect = peerId =>
                {
                    cli.SendBytes(peerId, Encoding.ASCII.GetBytes("hello"));
                    LoggerUtil.Info(typeof(UDPNetworkTest), "{0} connected", peerId);
                };

                cli.OnReceive = (peerId, bytes, channelId) =>
                {
                    LoggerUtil.Info(typeof(UDPNetworkTest), "{0} recved {1}", peerId, Encoding.ASCII.GetString(bytes));
                    cli.Disconnect(peerId);
                };

                cli.OnDisconnect = peerId =>
                {
                    LoggerUtil.Info(typeof(UDPNetworkTest), "{0} disconnected", peerId);
                    cli.Connect("127.0.0.1", 1234);
                };

                cli.Connect("127.0.0.1", 1234);
                while (true) cli.Update();
            }
        }

        private static void Test(string[] args)
        {
            if (args.Length != 0)
                ThreadPool.QueueUserWorkItem(RunServer);
            else
                ThreadPool.QueueUserWorkItem(RunClient);

            Thread.Sleep(100);
            Console.ReadLine();
        }
    }
}