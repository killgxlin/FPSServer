using System;
using System.Collections.Generic;
using ENet;
using ProtoBuf;

namespace Share.Sync
{
    // ---------------------------------------------------------
    public enum Priority
    {
        Const = 1,  // 常量 只同步一次
        High = 2,   // 高频
        Mid = 3,    // 中频
        Low = 4,    // 低频
    }

    public enum RPCMode
    {
        Self = 1,   // 只发给自己 用作c->s
        Broad = 2,  // 广播 用作s->c
    }

    // ---------------------------------------------------------
    public class SyncAttribute : Attribute
    {
        public SyncAttribute(Priority priority, ENet.PacketFlags flag = PacketFlags.Reliable)
        {

        }
    }

    public class RPCAttribute : Attribute
    {
        public RPCAttribute(RPCMode mode, ENet.PacketFlags flag = PacketFlags.Reliable)
        {

        }
    }



    // ---------------------------------------------------------
    public class SyncAttList<T> : List<T>
    {

    }

    public class SyncAttDict<K, V> : Dictionary<K, V>
    {

    }


    public class SyncObj
    {
        public Int64 id;
        public bool canRecvMsg;
    }


    public class SyncScene
    {
        public Action<SyncObj> onObjAdded;              // 添加对象回调 onObjAdded(objAdded)
        public Action<SyncObj> onObjRemoved;            // 删除对象回调 onObjRemoved(objRemoved)
        public Action<SyncObj> onObjRefreshed;          // 刷新对象回调 onObjRefreshed(objRefreshed)
        public Action<SyncObj, string> onObjAttChange;  // 对象属性变化回调 onObjAttChange(obj, attName)

        public Action<Int64, IExtensible> onSendMsg;      // 发送消息回调，onSendMsg(id, msg)

        public Dictionary<Int64, SyncObj> objects;      // 所有可同步对象

        public void Init()
        {

        }

        public void Destroy()
        {

        }



        public void MasterAddObj(SyncObj obj)
        {

        }

        public void MasterRemoveObj(Int64 id)
        {

        }

        public void MasterUpdateObjs()
        {
        }

        public void MasterDealMsg(IExtensible msg)
        {
            switch (msg)
            {
                case Msg.NcRPC rpc:
                    break;
            }
        }

        public void MirrorDealMsg(IExtensible msg)
        {
            switch (msg)
            {
                case Msg.NsSyncObjAdded added:
                    break;
                case Msg.NsSyncObjRemoved removed:
                    break;
                case Msg.NsSyncObjRefreshed refreshed:
                    break;
                case Msg.NsRPC rpc:
                    break;
            }
        }

    }
}
