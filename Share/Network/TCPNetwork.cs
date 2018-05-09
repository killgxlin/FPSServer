using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using SocketMessaging;
using SocketMessaging.Server;

namespace Share.Network
{
    public class TCPConnections : IDisposable
    {
        public class EventData
        {
            public Connection conn;
            public byte[] msg;
            public int typ;
        }

        Queue<EventData> queue = new Queue<EventData>();

        protected void sendEd(Connection conn, int typ)
        {
            var ed = new TCPConnections.EventData
            {
                conn = conn,
                typ = typ,
            };
            switch (typ)
            {
                case 0:
                case 1:
                    ed.msg = conn.ReceiveMessage();
                    break;
            }

            lock (queue)
            {
                queue.Enqueue(ed);
            }
        }

        public Action<Int64> OnConnect;
        public Action<Int64, byte[], byte> OnReceive;
        public Action<Int64> OnDisconnect;
        private Dictionary<Int64, Connection> conns = new Dictionary<long, Connection>();
        public void Update()
        {
            lock (queue)
            {
                while (queue.Count > 0)
                {
                    var ed = queue.Dequeue();
                    switch (ed.typ)
                    {
                        case 0:
                            conns.Add(ed.conn.Id+1, ed.conn);
                            OnConnect?.Invoke(ed.conn.Id + 1);
                            break;
                        case 1:
                            OnReceive?.Invoke(ed.conn.Id + 1, ed.msg, 0);
                            break;
                        case 2:
                            OnDisconnect?.Invoke(ed.conn.Id + 1);
                            conns.Remove(ed.conn.Id + 1);
                            break;
                    }
                }
            }
        }

        public virtual void Dispose()
        {
            queue.Clear();
        }

        public void SendBytes(Int64 peerId, byte[] bytes)
        {
            conns[peerId].Send(bytes);
        }
        public void Disconnect(Int64 peerId)
        {
            conns[peerId].Close();
        }
    }
    public class TCPServer : TCPConnections
    {
        TcpServer svr = new TcpServer();

        public void Listen(int port)
        {
            svr.Connected += (sender, args) =>
            {
                var conn = args.Connection;
                conn.SetMode(MessageMode.PrefixedLength);
                conn.Disconnected += (o, eventArgs) => sendEd(o as Connection, 2);
                conn.ReceivedMessage += (o, eventArgs) => sendEd(o as Connection, 1);
                sendEd(conn, 0);
            };
            svr.Start(port);
        }

        public override void Dispose()
        {
            svr.Stop();
            base.Dispose();
        }
    }

    public class TCPClient : TCPConnections
    {
        public void Connect(string ip, int port)
        {
            var cli = SocketMessaging.TcpClient.Connect(IPAddress.Parse(ip), port);
            cli.SetMode(MessageMode.PrefixedLength);
            cli.Disconnected += (o, eventArgs) => sendEd(o as Connection, 2);
            cli.ReceivedMessage += (o, eventArgs) => sendEd(o as Connection, 1);
            sendEd(cli, 0);
        }
    }


    class TCPNetworkTest
    {

        static public void RunServer(object obj)
        {
            var peers = new HashSet<Int64>();
            using (var svr = new TCPServer())
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
            using (var cli = new TCPClient())
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

        public static void Test(string[] args)
        {

            ThreadPool.QueueUserWorkItem(RunServer);
            Thread.Sleep(100);

            ThreadPool.QueueUserWorkItem(RunClient);

            Thread.Sleep(100);
            Console.ReadLine();
        }
    }
}
