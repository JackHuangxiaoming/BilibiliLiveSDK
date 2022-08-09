using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Bilibili
{
    public class CoroutineUtiliy : MonoBehaviour
    {
        private static CoroutineUtiliy _instance;
        public static CoroutineUtiliy Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    _instance = obj.AddComponent<CoroutineUtiliy>();
                    obj.hideFlags = HideFlags.HideInHierarchy;
                    GameObject.DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        /// <summary>
        /// 开启一个协程
        /// </summary>
        /// <param name="coroutine"></param>
        public Coroutine StartDoCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }
        /// <summary>
        /// 停止一个协成
        /// </summary>
        /// <param name="coroutine"></param>
        public void StopDoCoroutine(Coroutine coroutine)
        {
            StopCoroutine(coroutine);
        }

       
    }
}