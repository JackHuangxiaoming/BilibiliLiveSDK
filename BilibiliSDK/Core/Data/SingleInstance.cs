using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilibili
{
    /// <summary>
    /// 单例
    /// </summary>
    public abstract class SingleInstance<T> where T : SingleInstance<T>, new()
    {
        private static T instance;

        /// <summary>
        /// 获取一个 对象实例 
        /// </summary>        
        public static T Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new T();
                    instance.Init();
                }
                return instance;
            }
        }

        public abstract void Init();
    }
}
