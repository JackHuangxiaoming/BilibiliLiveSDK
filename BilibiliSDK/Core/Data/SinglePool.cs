
using System;
using System.Collections.Generic;
using static Bilibili.MyJson;

namespace Bilibili
{
    /// <summary>
    /// 单一 池子对象
    /// </summary>
    public abstract class SinglePool<T> where T : SinglePool<T>, new()
    {
        private static T lp;
        private static Queue<T> Pool = new Queue<T>();

        /// <summary>
        /// 获取一个 对象实例 
        /// 用完必须 Unuse
        /// </summary>        
        public static T GetInstance(JsonNode_Object json = null)
        {
            if (Pool.Count > 0)
                lp = Pool.Dequeue();
            else
                lp = new T();
            lp.UpdateInfo(json);
            return lp;
        }
        /// <summary>
        /// 获取一个 对象实例 
        /// </summary>   
        public static T GetInstance()
        {
            if (Pool.Count > 0)
                lp = Pool.Dequeue();
            else
                lp = new T();
            return lp;
        }

        protected bool valid;//是否有效
        /// <summary>
        /// 否是是有效玩家
        /// </summary>
        public bool Valid { get => valid; }
        public virtual void UpdateInfo(JsonNode_Object json)
        {
        }
        public virtual void UpdateInfo()
        {

        }
        public virtual void Unuse()
        {
            valid = false;
            Pool.Enqueue((T)this);
        }
    }
}
