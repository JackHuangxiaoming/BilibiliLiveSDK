using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Bilibili
{
    public delegate void PHPStringHandler(string phpResult);
    public class Utils
    {
        /// <summary>
        /// 当前时区 1970年1月1号凌晨12：00
        /// </summary>
        public static DateTime DateTime1970 = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
        /// <summary>
        /// 打印日志
        /// </summary>
        public static bool CommonDebug = true;
        /// <summary>
        /// 打印错误
        /// </summary>
        public static bool FailedDebug = true;
        /// <summary>
        /// 普通日志
        /// </summary>
        public static void Log(object msg)
        {
            if (CommonDebug)
                Debug.Log(DateTime.Now.ToString("[yyyy-MM-dd-HH:mm:ss:fff]\n") + msg);
        }
        /// <summary>
        /// 错误日志
        /// </summary>
        public static void LogError(object msg)
        {
            if (FailedDebug)
                Debug.LogError(DateTime.Now.ToString("[yyyy-MM-dd-HH:mm:ss:fff]\n") + msg);
        }

        /// <summary>
        /// 包括最小  不包括最大
        /// </summary>        
        public static int Range(int min, int max)
        {
            if (min > max)
            {
                int temp = min;
                min = max;
                max = min;
            }
            return UnityEngine.Random.Range(min, max);
        }

        public static float Range(float min, float max)
        {
            if (min > max)
            {
                float temp = min;
                min = max;
                max = temp;
            }
            return UnityEngine.Random.Range(min, max);
        }

        #region 时间

        /// <summary>
        /// 转换Unix时间戳 到DateTime
        /// 当前时区
        /// </summary>
        /// <param name="second">秒</param>
        public static DateTime ConvertUnixInt2DateTime(long second)
        {
            return DateTime1970.AddSeconds(second);
        }
        /// <summary>
        /// 转换JavaScript时间戳 到DateTime
        /// 当前时区
        /// </summary>
        /// <param name="millisecond">毫秒</param>
        public static DateTime ConvertJavaScriptInt2DateTime(long millisecond)
        {
            return DateTime1970.AddMilliseconds(millisecond);
        }
        /// <summary>
        /// 转换DateTime 到Unix时间戳
        /// </summary>        
        /// <returns>秒</returns>
        public static long ConvertDateTime2UnixInt(DateTime time)
        {
            if (time == default(DateTime))
                time = DateTime.Now;
            return (long)(time - DateTime1970).TotalSeconds; // 相差秒数
        }
        /// <summary>
        /// 转换DateTime 到JavaScript时间戳
        /// </summary>        
        /// <returns>毫秒</returns>
        public static long ConvertDateTime2JavaScriptInt(DateTime time)
        {
            if (time == default(DateTime))
                time = DateTime.Now;
            return (long)(time - DateTime1970).TotalMilliseconds; // 相差秒数
        }


        #endregion

        #region MD5 SHA256
        /// <summary>
        /// MD5
        /// </summary>
        public static string GetMD5HashByString(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
                // To force the hex string to lower-case letters instead of
                // upper-case, use he following line instead:
                // sb.Append(hashBytes[i].ToString("x2")); 
            }
            return sb.ToString();
        }
        /// <summary>
        /// HAMC SHA256(原始)
        /// </summary>
        public static string GetHamcSha256(string message, string secret, Encoding encoding)
        {
            secret = secret ?? "";
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashmessage.Length; i++)
                {
                    builder.Append(hashmessage[i].ToString("x2"));
                }
                return builder.ToString();
                //return Convert.ToBase64String(hashmessage);(Base64)
            }
        }
        /// <summary>
        /// HAMC SHA256(原始)
        /// </summary>
        public static string GetHamcSha256(byte[] message, byte[] secret)
        {
            using (var hmacsha256 = new HMACSHA256(secret))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(message);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashmessage.Length; i++)
                {
                    builder.Append(hashmessage[i].ToString("x2"));
                }
                return builder.ToString();
                //return Convert.ToBase64String(hashmessage);(Base64)
            }
        }
        #endregion

        #region HTTP
        /// <summary>
        /// Http
        /// </summary>
        public static void HttpRequest(string webUrl, string data, PHPStringHandler phpHandler, bool post = true, Dictionary<string, string> header = null)
        {
            CoroutineUtiliy.Instance.StartCoroutine(HttpRequestIEnumerator(webUrl, data, phpHandler, post, header));
        }
        private static IEnumerator HttpRequestIEnumerator(string url, string data, PHPStringHandler phpHandler, bool post = true, Dictionary<string, string> header = null)
        {
            data = data == null ? string.Empty : data;
            UnityWebRequest uwr = null;
            if (post)
            {
                uwr = UnityWebRequest.Post(url, data);
                if (!string.IsNullOrEmpty(data))
                {
                    byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(data);
                    uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(postBytes);
                    uwr.uploadHandler.contentType = "application/json";
                }
            }
            else
                uwr = UnityWebRequest.Get(url + "?" + data);
            uwr.timeout = 15;
            uwr.certificateHandler = new CertHandler();

            if (header != null)
            {
                var iter = header.GetEnumerator();
                while (iter.MoveNext())
                {
                    uwr.SetRequestHeader(iter.Current.Key, iter.Current.Value);
                }
                iter.Dispose();
            }
            yield return uwr.SendWebRequest();
            string str = null;
            if (!string.IsNullOrEmpty(uwr.error))
            {
                LogError(uwr.error);
            }
            else
            {
                str = uwr.downloadHandler.text;
            }
            uwr.Abort();
            if (phpHandler != null)
            {
                phpHandler(str);
            }
        }
        #endregion

        #region Json
        public static string FormatPhpParam(bool isPost, params object[] paramsArr)
        {
            if (isPost)
            {
                return FormatPhpParamsByPost(paramsArr);
            }
            return FormatPhpParams(paramsArr);
        }
        private static string FormatPhpParamsByPost(object[] paramsArr)
        {
            if (paramsArr != null && paramsArr.Length > 1)
            {
                var param = new Dictionary<string, string>();
                for (int i = 0, len = paramsArr.Length; i < len; i += 2)
                {
                    param.Add(paramsArr[i].ToString(), paramsArr[i + 1].ToString());
                }
                return MyJson.SerializeDictionaryToJsonString<string, string>(param);
            }
            return "";
        }
        public static string FormatPhpParams(object[] paramsArr)
        {
            string paramStr = "?";
            if (paramsArr == null || paramsArr.Length < 2)
                paramStr = "";
            else
            {
                for (int i = 0, len = paramsArr.Length; i < len; i += 2)
                {
                    paramStr += string.Format("{0}={1}{2}", paramsArr[i], paramsArr[i + 1], "&");
                }
                if (paramStr.EndsWith("&"))
                    paramStr = paramStr.Remove(paramStr.Length - 1);
            }
            return paramStr;
        }
        #endregion

        #region Coroutine

        /// <summary>
        /// 开启一个协程
        /// </summary>
        /// <param name="enumerator">协程函数 IEnumerator</param>
        /// <returns>Coroutine</returns>
        public static Coroutine StartCoroutine(IEnumerator enumerator)
        {
            return CoroutineUtiliy.Instance.StartDoCoroutine(enumerator);
        }

        /// <summary>
        /// 停止一个协程
        /// </summary>
        /// <param name="coroutine">协程 Coroutine</param>
        public static void StopCoroutine(Coroutine coroutine)
        {
            CoroutineUtiliy.Instance.StopDoCoroutine(coroutine);
        }

        #endregion
    }
    public class CertHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

}
