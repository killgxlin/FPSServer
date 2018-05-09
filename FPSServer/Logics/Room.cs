using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPSServer.Logics.Sync;
using Msg;
using Share;
using Share.Game;
using Share.Sync;
using Share.Utils;

namespace FPSServer.Logics
{
    class Room
    {
        public Action<Int64, ProtoBuf.IExtensible> SendMsg;
        public Action<Int64> Kick;

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

        public void Login(Int64 playerId)
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
        public void Logout(Int64 playerId)
        {
            scene.MasterRemoveObj(playerId);
            this.Info("Logout playerId:{0}", playerId);
        }
        public void RecvMsg(Int64 playerId, ProtoBuf.IExtensible msg)
        {
            this.Info("RecvMsg playerId:{0}, msg:{1}", playerId, msg.GetType());
            scene.MasterDealMsg(msg);
        }


        // ----------------------------------------------
        SyncScene scene = new SyncScene();
    }
}
