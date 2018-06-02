using System;
using System.Collections.Generic;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading;
using ENet;
using ENet.Native;

namespace Share.Network
{
    public class UDPNetworkMTSvr : IDisposable
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
            }

            public Type type;
            public byte[] bytes;
            public byte channelId = 0;
            public Int64 peerId = 0;

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
        enum ConnState
        {
            Idle,
            Connected,
            Disconnecting,
            Error,
        }

        Dictionary<Int64, ConnState> connStates = new Dictionary<long, ConnState>();
        Host host = new Host();

        public volatile bool Pause = false;
        public bool Dev = false; // for dev mode, program can break for long

        ConnState getConnState(Int64 peerId)
        {
            var state = ConnState.Idle;
            connStates.TryGetValue(peerId, out state);
            return state;
        }

        void setConnState(Int64 peerId, ConnState state)
        {
            var prevState = getConnState(peerId);
            if (state == ConnState.Idle)
            {
                connStates.Remove(peerId);
            }
            else
            {
                connStates[peerId] = state;
            }

            trace(string.Format("{0}-worker {1} to {2}",peerId, prevState, state));
        }

        private void worker(object o)
        {
            while (!quit)
            {
                updateWorkerQueue();

                updateState();
            }
        }

        private Peer peer;

        unsafe void updateWorkerQueue()
        {
            foreach (var evt in getEvents(workerQueue))
            {
                trace(string.Format("{0}-worker {1}", evt.peerId, evt.type));
                var state = getConnState(evt.peerId);

                if (state != ConnState.Connected)
                {
                    continue;
                }

                peer.NativeData = (ENetPeer*) evt.peerId;

                if (!peer.IsInitialized)
                {
                    continue;
                }

                if (evt.type == NetEvent.Type.Send)
                {
                    peer.Send(evt.channelId, evt.bytes, PacketFlags.Reliable);
                }
                else if (evt.type == NetEvent.Type.Disconnect)
                {
                    peer.DisconnectLater(4444);
                }
            }
        }

        unsafe void updateState()
        {
            if (Pause)
            {
                return;
            }

            Event ev;
            Int64 peerId;
            ConnState state;

            if (host.Service(100, out ev))
            {
                do
                {
                    switch (ev.Type)
                    {
                        case EventType.Connect:
                            peerId = (Int64) ev.Peer.NativeData;
                            var addr = ev.Peer.GetRemoteAddress();

                            setConnState(peerId, ConnState.Connected);

                            enqueuSafe(mainQueue, new NetEvent
                            {
                                type = NetEvent.Type.Connected,

                                peerId = peerId,
                                ip = addr.Address.ToString(),
                                port = addr.Port,
                            });

                            if (Dev)
                            {
                                ev.Peer.SetTimeouts(0, 3000000, 3000000);
                            }

                            break;
                        case EventType.Receive:
                            peerId = (Int64) ev.Peer.NativeData;

                            enqueuSafe(mainQueue, new NetEvent
                            {
                                type = NetEvent.Type.Recved,

                                peerId = peerId,
                                bytes = ev.Packet.GetBytes(),
                                channelId = ev.ChannelID,
                            });
                            ev.Packet.Dispose();
                            break;
                        case EventType.Disconnect:
                            peerId = (Int64) ev.Peer.NativeData;
                            setConnState(peerId, ConnState.Idle);
                            enqueuSafe(mainQueue, new NetEvent
                            {
                                type = NetEvent.Type.Disconnected,

                                peerId = peerId,
                            });
                            break;
                    }
                } while (host.CheckEvents(out ev));
            }
        }

        // main -----------------------------------------------------------------------
        public enum NetState
        {
            Idle,
            Connected,
            Disconnecting,
        }

        private NetState state = NetState.Idle;
        
        Dictionary<Int64, NetState> netStates = new Dictionary<long, NetState>();

        public NetState GetPeerState(Int64 peerId)
        {
            var state = NetState.Idle;
            netStates.TryGetValue(peerId, out state);
            return state;
        }

        private void setPeerState(Int64 peerId, NetState state)
        {
            var prevState = GetPeerState(peerId);
            if (state == NetState.Idle)
            {
                netStates.Remove(peerId);
            }
            else
            {
                netStates[peerId] = state;
            }
            
            trace(string.Format("{0}-main {1} to {2}",peerId, prevState, state));
        }

        public Action<Int64> OnConnect;
        public Action<Int64, byte[], byte> OnReceive;
        public Action<Int64> OnDisconnect;

        public volatile bool Trace = false;

        public UDPNetworkMTSvr(int port)
        {
            host.InitializeServer(port, 1000);

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

            host.Dispose();
        }

        public void Update()
        {
            try
            {
                foreach (var evt in getEvents(mainQueue))
                {
                    trace(string.Format("{0}-main {1}", evt.peerId, evt.type));
                    switch (evt.type)
                    {
                        case NetEvent.Type.Disconnected:
                            setPeerState(evt.peerId, NetState.Idle);
                            OnDisconnect?.Invoke(evt.peerId);
                            break;
                        case NetEvent.Type.Recved:
                            OnReceive?.Invoke(evt.peerId, evt.bytes, evt.channelId);
                            break;
                        case NetEvent.Type.Connected:
                            setPeerState(evt.peerId, NetState.Connected);
                            OnConnect?.Invoke(evt.peerId);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public bool Send(Int64 peerId, byte[] bytes)
        {
            if (GetPeerState(peerId) != NetState.Connected)
            {
                return false;
            }

            enqueuSafe(workerQueue, new NetEvent
            {
                type = NetEvent.Type.Send,
                peerId = peerId,
                bytes = bytes,
                channelId = 0,
            });

            return true;
        }

        public bool Disconnect(Int64 peerId)
        {
            if (GetPeerState(peerId) != NetState.Connected)
            {
                return false;
            }

            enqueuSafe(workerQueue, new NetEvent
            {
                type = NetEvent.Type.Disconnect,
                peerId = peerId,
            });

            return true;
        }

        public static void Test()
        {
            using (var svr = new UDPNetworkMTSvr(1234))
            {
                svr.Trace = true;
                svr.OnConnect = peerId => Console.WriteLine("{0} connected", peerId);
                svr.OnDisconnect = peerId => Console.WriteLine("{0} disconnected", peerId);
                svr.OnReceive = (peerId, bytes, arg3) =>
                {
                    var recved = Encoding.ASCII.GetString(bytes);
                    Console.WriteLine("{0} recved {1}", peerId, recved);
                    switch (recved)
                    {
                        case "dc":
                            svr.Disconnect(peerId);
                            break;
                        default:
                            svr.Send(peerId, bytes);
                            break;
                    }
                };

                while (true)
                {
                    svr.Update();
                    Thread.Sleep(500);
                }
            }
        }
    }
}