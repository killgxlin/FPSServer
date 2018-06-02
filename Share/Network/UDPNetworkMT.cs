using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ENet;

namespace Assets.Script.ClientCore.Network
{
    public class UDPNetworkMT : IDisposable
    {
        // share -----------------------------------------------------------------------
        class NetEvent
        {
            public enum Type
            {
                Invalid,
                Connected,
                Recved,
                Disconnected,
                Send,
                Disconnect,
                Connect,
            }

            public Type type;
            public byte[] bytes;
            public byte channelId = 0;

            public string ip = "";
            public int port = 0;
        }

        Thread thr;
        volatile bool quit = false;

        Queue<NetEvent> workerQueue = new Queue<NetEvent>();
        Queue<NetEvent> mainQueue = new Queue<NetEvent>();

        void enqueuSafe(Queue<NetEvent> queue, NetEvent evt)
        {
            lock (queue)
            {
                queue.Enqueue(evt);
            }
        }

        NetEvent[] getEvents(Queue<NetEvent> queue)
        {
            NetEvent[] events = Array.Empty<NetEvent>();
            lock (queue)
            {
                events = queue.ToArray();
                queue.Clear();
            }

            return events;
        }

        void trace(string p)
        {
            if (Trace)
            {
                Console.WriteLine(p);
            }
        }

        // worker -----------------------------------------------------------------------
        enum WorkerState
        {
            Idle,
            Connecting,
            Disconnecting,
            Connected,
            Error,
        }

        Peer curPeer;
        Host host = new Host();
        
        public volatile bool Pause = false;
        public bool Dev = true;    // for dev mode, program can break for long

        private WorkerState _workerState = WorkerState.Idle;

        private WorkerState workerState
        {
            set
            {
                trace(string.Format("worker {0} to {1}", _workerState, value));
                _workerState = value;
            }
            get { return _workerState; }
        }

        void worker(object o)
        {
            while (!quit)
            {
                updateWorkerQueue();

                updateState();                    
            }
        }

        void updateWorkerQueue()
        {
            foreach (var evt in getEvents(workerQueue))
            {
                trace(string.Format("worker event:{0}", evt.type));
                switch (workerState)
                {
                    case WorkerState.Error:
                    case WorkerState.Idle:
                        switch (evt.type)
                        {
                            case NetEvent.Type.Connect:
                                try
                                {
                                    host.Connect(evt.ip, evt.port, 4444, 10);
                                }
                                catch (Exception e)
                                {
                                    trace(e.ToString());
                                    workerState = WorkerState.Idle;
                                    enqueuSafe(mainQueue, new NetEvent
                                    {
                                        type = NetEvent.Type.Disconnected,
                                    });
                                }

                                workerState = WorkerState.Connecting;
                                break;
                        }

                        break;
                    case WorkerState.Connected:
                        switch (evt.type)
                        {
                            case NetEvent.Type.Send:
                                if (curPeer.IsInitialized)
                                {
                                    curPeer.Send(evt.channelId, evt.bytes, PacketFlags.Reliable);
                                }

                                break;
                            case NetEvent.Type.Disconnect:
                                if (curPeer.IsInitialized)
                                {
                                    curPeer.DisconnectLater(4444);
                                }

                                break;
                        }

                        break;
                }
            }
        }

        void updateState()
        {
            if (Pause)
            {
                return;
            }
            
            switch (workerState)
            {
                case WorkerState.Error:
                    Thread.Sleep(1000);
                    break;
                case WorkerState.Idle:
                    Thread.Sleep(100);
                    break;
                case WorkerState.Connecting:
                case WorkerState.Disconnecting:
                case WorkerState.Connected:
                    Event ev;
                    if (host.Service(100, out ev))
                    {
                        do
                        {
                            switch (ev.Type)
                            {
                                case EventType.Connect:
                                    curPeer = ev.Peer;
                                    enqueuSafe(mainQueue, new NetEvent
                                    {
                                        type = NetEvent.Type.Connected,
                                    });
                                    workerState = WorkerState.Connected;

                                    if (Dev)
                                    {
                                        curPeer.SetTimeouts(0, 3000000, 3000000);
                                    }
                                        
                                    break;
                                case EventType.Receive:
                                    enqueuSafe(mainQueue, new NetEvent
                                    {
                                        type = NetEvent.Type.Recved,

                                        bytes = ev.Packet.GetBytes(),
                                        channelId = ev.ChannelID,
                                    });
                                    ev.Packet.Dispose();
                                    break;
                                case EventType.Disconnect:
                                    enqueuSafe(mainQueue, new NetEvent
                                    {
                                        type = NetEvent.Type.Disconnected,
                                    });
                                    workerState = WorkerState.Idle;
                                    break;
                            }
                        } while (host.CheckEvents(out ev));
                    }

                    break;
            }
        }

        // main -----------------------------------------------------------------------
        public enum NetState
        {
            Idle,
            Connecting,
            Connected,
            Disconnecting,
        }

        private NetState state = NetState.Idle;
        public NetState State
        {
            private set
            {
                trace(string.Format("main {0} to {1}", state, value));
                state = value;
            }
            get { return state; }
        }

        public Action OnConnect;
        public Action<byte[], byte> OnReceive;
        public Action OnDisconnect;

        public string Ip { get; set; } = "";
        public int Port { get; set; } = 0;

        public volatile bool Trace = false;

        public UDPNetworkMT()
        {
            host.InitializeClient(10);

            thr = new Thread(worker);
            thr.Start();
        }

        public void Dispose()
        {
            if (thr != null)
            {
                quit = true;
                thr.Join();
                thr = null;
            }
        }

        public void Update()
        {
            try
            {
                foreach (var evt in getEvents(mainQueue))
                {
                    trace(string.Format("main event:{0}", evt.type));
                    switch (evt.type)
                    {
                        case NetEvent.Type.Disconnected:
                            State = NetState.Idle;
                            OnDisconnect?.Invoke();
                            break;
                        case NetEvent.Type.Recved:
                            OnReceive?.Invoke(evt.bytes, evt.channelId);
                            break;
                        case NetEvent.Type.Connected:
                            State = NetState.Connected;
                            OnConnect?.Invoke();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Connect(string ip, int port)
        {
            if (State != NetState.Idle)
            {
                return;
            }

            Ip = ip;
            Port = port;


            State = NetState.Connecting;
            enqueuSafe(workerQueue, new NetEvent
            {
                type = NetEvent.Type.Connect,
                ip = Ip,
                port = Port,
            });
        }

        public void Send(byte[] bytes)
        {
            if (State != NetState.Connected)
            {
                return;
            }

            enqueuSafe(workerQueue, new NetEvent
            {
                type = NetEvent.Type.Send,
                bytes = bytes,
                channelId = 0,
            });
        }

        public void Disconnect()
        {
            if (State != NetState.Connected)
            {
                return;
            }

            State = NetState.Disconnecting;
            enqueuSafe(workerQueue, new NetEvent
            {
                type = NetEvent.Type.Disconnect,
            });
        }

        public static void Test()
        {
            using (var cli = new UDPNetworkMT())
            {
                cli.Trace = true;
                cli.OnConnect = () =>
                {
                    Console.WriteLine(cli.State);
                };
                cli.OnDisconnect = () =>
                {
                    Console.WriteLine(cli.State);
                };
                cli.OnReceive = (bytes, arg3) =>
                {
                    Console.WriteLine(Encoding.ASCII.GetString(bytes));
                };

                var i = 0;

                cli.Connect("127.0.0.1", 1234);

                while (true)
                {
                    switch (cli.State)
                    {
                        case NetState.Idle:
                            cli.Connect("127.0.0.1", 1234);
                            break;
                        case NetState.Connected:
                            cli.Send(Encoding.ASCII.GetBytes(i.ToString()));
                            i++;
                            if (i >= 100000)
                            {
                                cli.Disconnect();
                                i = 0;
                            }
                            break;
                    }

                    cli.Update();
                    Thread.Sleep(500);
                }
            }
        }
    }
}