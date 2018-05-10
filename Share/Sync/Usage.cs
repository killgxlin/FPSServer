using System;
using System.Collections.Generic;
using System.Threading;
using Msg;
using ProtoBuf;
using Share.Game;

namespace Share.Sync
{
    // ------------------------------------------------------------------------------------------------------------------

    // ---------------------------------------------------------
    internal class TestMe
    {
        private readonly Queue<IExtensible> queue = new Queue<IExtensible>();

        public void Server(object obj)
        {
            var scene = new SyncScene();
            scene.Init();

            scene.MasterAddObj(new GameState
            {
                canRecvMsg = false,
                id = 3,
                safePoint = new Vector3(),
                safeRadius = 1000
            });

            scene.onSendMsg = (id, msg) =>
            {
                if (id != 1) return;

                lock (queue)
                {
                    queue.Enqueue(msg);
                }
            };

            scene.onObjAdded = added =>
            {
                switch (added)
                {
                    case Player player:
                        player.csFire = (dst, count, hit) => { };
                        player.csMove = (pos, dir, state) => { };
                        break;
                }
            };
            scene.onObjRefreshed = refreshed => { };
            scene.onObjRemoved = removed => { };


            var p1 = new Player {id = 1};
            var p2 = new Player {id = 2};

            p1.csMove = (pos, dir, state) =>
            {
                p1.pos = pos;
                p1.dir = dir;
                p1.state = state;
            };
            p1.csFire = (targetId, bulletCount, isHit) =>
            {
                p2.hp = 0;
                p2.scExplode();
            };
            p2.scExplode = () => { };


            scene.MasterAddObj(p1);
            scene.MasterAddObj(p2);


            while (true)
            {
                scene.MasterUpdateObjs();
                Thread.Sleep(100);
            }

            scene.MasterRemoveObj(p2.id);


            scene.Destroy();
        }

        public void Client(object obj)
        {
            SyncScene scene = null;
            while (true)
            {
                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        var msg = queue.Dequeue();
                        switch (msg)
                        {
                            case NsSyncSceneEnter sceneInit:
                                scene = new SyncScene();
                                scene.Init();
                                scene.onObjAdded = added =>
                                {
                                    switch (added)
                                    {
                                        case Player player:
                                            player.csFire = (dst, count, hit) => { };
                                            player.csMove = (pos, dir, state) => { };
                                            player.scExplode = () =>
                                            {
                                                /* play effect */
                                            };
                                            if (added.id == 1) player.csFire(2, 5, true);

                                            break;
                                    }
                                };
                                scene.onObjRemoved = removed => { };
                                scene.onObjRefreshed = refreshed => { };
                                break;
                            case NsSyncSceneExit sceneDestroy:
                                scene.Destroy();
                                scene = null;
                                break;
                            case NsSyncObjAdded objAdded:
                                scene.MirrorDealMsg(objAdded);
                                break;
                            case NsSyncObjRefreshed objRefreshed:
                                scene.MirrorDealMsg(objRefreshed);
                                break;
                            case NsSyncObjRemoved objRemoved:
                                scene.MirrorDealMsg(objRemoved);
                                break;
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }

        public void Test()
        {
            ThreadPool.QueueUserWorkItem(Client);
            Console.ReadLine();

            ThreadPool.QueueUserWorkItem(Server);
            Console.ReadLine();
        }
    }
}