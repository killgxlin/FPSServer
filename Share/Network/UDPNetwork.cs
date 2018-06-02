using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ENet;

namespace Share.Network
{

    public class Connections
    {
        #region interface
        public Action<Int64> OnConnect;
        public Action<Int64, byte[], byte> OnReceive;
        public Action<Int64> OnDisconnect;
        private HashSet<Int64> disconnecting = new HashSet<long>();
        public bool Dev = false;

        public void Disconnect(Int64 peerId)
        {
            if (disconnecting.Contains(peerId))
                return;

            peers[peerId].DisconnectLater(0);
            disconnecting.Add(peerId);
        }

        public void SendBytes(Int64 peerId, byte[] bytes, byte channel = 0)
        {
            if (disconnecting.Contains(peerId))
                return;

            Peer peer;
            
            if (!peers.TryGetValue(peerId, out peer) || !peer.IsInitialized)
                return;

            peer.Send(0, bytes, PacketFlags.Reliable);
        }

        #endregion


        #region peer
        Dictionary<Int64, Peer> peers = new Dictionary<Int64, Peer>();
        protected unsafe void onConnect(Peer peer)
        {
            var ptr = (IntPtr)peer.NativeData;
            var peerId = ptr.ToInt64();
            peers.Add(peerId, peer);

            if (Dev)
            {
                peer.SetTimeouts(0, 3000000, 3000000);
            }
            
            if (OnConnect != null)
                OnConnect(peerId);
        }

        protected unsafe void onReceive(Peer peer, byte[] bytes, byte channelId)
        {
            var ptr = (IntPtr)peer.NativeData;
            var peerId = ptr.ToInt64();
            if (OnReceive != null)
                OnReceive(peerId, bytes, channelId);
        }

        protected unsafe void onDisconnect(Peer peer)
        {
            var ptr = (IntPtr)peer.NativeData;
            var peerId = ptr.ToInt64();
            peers.Remove(peerId);

            if (OnDisconnect != null)
                OnDisconnect(peerId);

            if (disconnecting.Contains(peerId))
                disconnecting.Remove(peerId);
            
        }
        #endregion

        #region host

        protected Host host = new Host();

        public void Update()
        {
            Event ev;
            if (host.Service(0, out ev))
            {
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
        }
        #endregion

    }
    public class UDPServer : Connections, IDisposable
    {
        public int Port { private set; get; }
        public void Listen(int start, int num = 1)
        {
            for (int i = start; i < start+num; i++)
            {
                try
                {
                    host.InitializeServer(i, 1000);
                    Port = i;
                    return;
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }
        public void Dispose()
        {
            host.Dispose();
        }
    }

    public class UDPClient : Connections, IDisposable
    {
        public void Dispose()
        {
            host.Dispose();
        }

        public UDPClient()
        {
            host.InitializeClient(1);
        }

        public unsafe Int64 Connect(string ip, int port, int channelLimit = 10)
        {
            var peer = host.Connect(ip, port, 4444, channelLimit);
            var ptr = (IntPtr) peer.NativeData;
            return ptr.ToInt64();
        }
    }

    class NetworkTest
    {

        static public void RunServer(object obj)
        {
            var peers = new HashSet<Int64>();
            using (var svr = new UDPServer())
            {
                svr.OnConnect = peerId =>
                {
                    peers.Add(peerId);
                    Console.WriteLine("{0} connected", peerId);
                };
                svr.OnReceive = (peerId, bytes, channelId) =>
                {
                    var msg = Encoding.ASCII.GetString(bytes);
                    Console.WriteLine("recv {0} msg {1}", peerId, msg);

                    bytes = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", peerId, msg));
                    foreach (var peer in peers)
                    {
                        if (peer != peerId)
                            svr.SendBytes(peer, bytes);
                    }
                };
                svr.OnDisconnect = peerId =>
                {
                    peers.Remove(peerId);
                    Console.WriteLine("{0} disconnected", peerId);
                };

                svr.Listen(1234);

                while (true)
                {
                    svr.Update();
                }
            }
        }

        static public void RunClient(object obj)
        {
            using (var cli = new UDPClient())
            {
                cli.OnConnect = peerId =>
                {
                    cli.SendBytes(peerId, Encoding.ASCII.GetBytes("hello"));
                    Console.WriteLine("{0} connected", peerId);
                };

                cli.OnReceive = (peerId, bytes, channelId) =>
                {
                    Console.WriteLine("{0} recved {1}", peerId, Encoding.ASCII.GetString(bytes));
                    cli.Disconnect(peerId);
                };

                cli.OnDisconnect = (peerId) =>
                {
                    Console.WriteLine("{0} disconnected", peerId);
                    cli.Connect("127.0.0.1", 1234);
                };

                cli.Connect("127.0.0.1", 1234);
                while (true)
                {
                    cli.Update();
                }
            }
        }

        static void Main(string[] args)
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
