using System;
using ProtoBuf;
using Share.Game;
using Share.Sync;
using Share.Utils;

namespace FPSServer.Logics
{
    internal class Room
    {
        public Action<long> Kick;


        // ----------------------------------------------
        private readonly SyncScene scene = new SyncScene();
        public Action<long, IExtensible> SendMsg;

        public void Init()
        {
            scene.Init();
            scene.onSendMsg = SendMsg;
            this.Info("Inited");
        }

        public void Destory()
        {
            scene.Destroy();
            this.Info("Destroy");
        }

        public void Update()
        {
            scene.MasterUpdateObjs();
        }

        public void Login(long playerId)
        {
            var player = new Player();
            player.id = playerId;
            player.name = "hello";
            player.pos = new Vector3();
            player.dir = new Vector3();
            player.state = 0;
            player.hp = 0;

            scene.MasterAddObj(player);

            this.Info("Login playerId:{0}", playerId);
        }

        public void Logout(long playerId)
        {
            scene.MasterRemoveObj(playerId);
            this.Info("Logout playerId:{0}", playerId);
        }

        public void RecvMsg(long playerId, IExtensible msg)
        {
            this.Info("RecvMsg playerId:{0}, msg:{1}", playerId, msg.GetType());
            scene.MasterDealMsg(msg);
        }
    }
}