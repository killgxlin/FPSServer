using System.Collections.Generic;
using System.Security.Cryptography;
using Msg;
using ProtoBuf;
using Share.Game;
using Share.Sync;

namespace FPSExample
{
    internal class Program
    {
        class Client
        {
            private SyncScene scene;

            List<IExtensible> msgs = new List<IExtensible>();

            public void RecvMsg(IExtensible msg)
            {
                msgs.Add(msg);
            }

            public void Dispatch()
            {
                msgs.ForEach(dispatch);
                msgs.Clear();
            }

            private void dispatch(IExtensible msg)
            {
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

        public static void Test()
        {
            var cli = new Client();
            var scene = new SyncScene();
            scene.Init();
            scene.onSendMsg = (id, msg) =>
            {
                if (id != 1) return;

                cli.RecvMsg(msg);
            };
            scene.MasterAddObj(new GameState
            {
                canRecvMsg = false,
                id = 3,
                safePoint = new Vector3(),
                safeRadius = 1000
            });
            scene.onObjAdded = added =>
            {
                switch (added)
                {
                    case Player player:
                        player.csMove = (pos, dir, state) =>
                        {
                            player.pos = pos;
                            player.dir = dir;
                            player.state = state;
                        };
                        player.csFire = (targetId, bulletCount, isHit) =>
                        {
                            var target = scene.FindObj(targetId) as Player;
                            target.hp = 0;
                            target.scExplode();
                        };
                        player.scExplode = () =>
                        {
                            // pack rpc
                        };
                        break;
                }
            };
            scene.onObjRefreshed = refreshed => { };
            scene.onObjRemoved = removed => { };
            var p1 = new Player {id = 1};
            var p2 = new Player {id = 2};
            scene.MasterAddObj(p1);
            cli.Dispatch();
            scene.MasterAddObj(p2);
            cli.Dispatch();
            scene.MasterRemoveObj(p2.id);
            scene.Destroy();
        }

        public static void Main(string[] args)
        {
            Test();
        }
    }
}