using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Share.Sync;

namespace Share.Game
{
    public class Vector3
    {
        public float x;
        public float y;
        public float z;
    }

    public class Item
    {
        public int type;
        public int num;
    }

    public class GameState : SyncObj
    {
        [Sync(Priority.High)] public Vector3 safePoint;
        [Sync(Priority.High)] public float safeRadius;
    }

    public class Player : SyncObj
    {
        [Sync(Priority.Const)]
        public string name; // 角色名

        [Sync(Priority.High, ENet.PacketFlags.None)]
        public Vector3 pos; // 位置

        [Sync(Priority.High, ENet.PacketFlags.None)]
        public Vector3 dir; // 朝向

        [Sync(Priority.High, ENet.PacketFlags.None)]
        public int state; // 移动状态

        [Sync(Priority.High, ENet.PacketFlags.None)]
        public int hp; // 血

        [Sync(Priority.High, ENet.PacketFlags.None)]
        public SyncAttList<Item> bags; // 携带物品



        [RPC(RPCMode.Self)]
        public Action<int, int, bool> csFire; // 开火( 目标id 子弹数量 是否击中)

        [RPC(RPCMode.Broad, ENet.PacketFlags.None)]
        public Action<int, int> scFire; // 开火( 目标id 子弹数量)

        [RPC(RPCMode.Self)]
        public Action<Vector3, Vector3, int> csMove; // 移动(位置 朝向 状态)

        [RPC(RPCMode.Broad, ENet.PacketFlags.None)]
        public Action scExplode; // 爆炸 广播
    }

    public class Loot : SyncObj
    {
        [Sync(Priority.Const)] public Int64 typeId;
        [Sync(Priority.Low)] public SyncAttList<Item> bags;
    }
}
