using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Bilibili.MyJson;

namespace Bilibili
{
    internal class BilibiliHttpServer
    {
        private static BilibiliHttpServer _instance;
        internal static BilibiliHttpServer Instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new BilibiliHttpServer();
                }
                return _instance;
            }
        }

        HttpListener listener;
        /// <summary>
        /// 开启Http服务器
        /// </summary>
        internal void StartHttpServer()
        {
            listener = new HttpListener();
            try
            {
                listener.Prefixes.Add("http://+:23322/");
                listener.Start();
                listener.BeginGetContext(ListenerCallback, listener);
                Utils.LogError($"Http服务端初始化完毕，正在等待客户端请求,时间：{DateTime.Now.ToString()}\r\n");
            }
            catch (Exception ex)
            {
                if (listener != null)
                {
                    listener.Close();
                    listener = null;
                }
                Utils.LogError("Http 服务器异常错误：" + ex.Message);
            }
        }

        void ListenerCallback(IAsyncResult result)
        {
            try
            {
                Utils.Log($"接到新的请求时间：{DateTime.Now.ToString()}");
                //继续异步监听
                HttpListener listener = (HttpListener)result.AsyncState;
                listener.BeginGetContext(ListenerCallback, listener);

                HttpListenerContext context = listener.EndGetContext(result);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                ////如果是js的ajax请求，还可以设置跨域的ip地址与参数
                //context.Response.AppendHeader("Access-Control-Allow-Origin", "*");//后台跨域请求，通常设置为配置文件
                //context.Response.AppendHeader("Access-Control-Allow-Headers", "ID,PW");//后台跨域参数设置，通常设置为配置文件
                //context.Response.AppendHeader("Access-Control-Allow-Method", "post");//后台跨域请求设置，通常设置为配置文件
                //context.Response.ContentType = "text/plain;charset=UTF-8";//告诉客户端返回的ContentType类型为纯文本格式，编码为UTF-8
                //context.Response.AddHeader("Content-type", "text/plain");//添加响应头信息                
                byte[] buffer = Encoding.UTF8.GetBytes(HandleRequest(request, response));
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                Utils.Log($"请求处理完成时间：{ DateTime.Now.ToString()}\r\n");
            }
            catch (Exception ex)
            {
                if (listener != null)
                {
                    listener.Close();
                    listener = null;
                }
                Utils.LogError("Http 服务器异常错误：" + ex.Message);
            }
        }

        string HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            string data = null;
            try
            {
                if (request.HttpMethod == "POST" && request.InputStream != null)
                {
                    var byteList = new List<byte>();
                    var byteArr = new byte[2048];
                    int readLen = 0;
                    int len = 0;
                    //接收客户端传过来的数据并转成字符串类型
                    do
                    {
                        readLen = request.InputStream.Read(byteArr, 0, byteArr.Length);
                        len += readLen;
                        byteList.AddRange(byteArr);
                    } while (readLen != 0);
                    data = Encoding.UTF8.GetString(byteList.ToArray(), 0, len);
                    //获取得到数据data可以进行其他操作
                    //做点什么
                    response.StatusDescription = "200";//获取或设置返回给客户端的 HTTP 状态代码的文本说明。
                    response.StatusCode = 200;// 获取或设置返回给客户端的 HTTP 状态代码。
                    Utils.Log($"接收数据完成:{data.Trim()},时间：{DateTime.Now.ToString()}");
                    return OnHandleResponse(data);
                }
                else
                {
                    return "<HTML><BODY> Hello world!</BODY></HTML>";
                }
            }
            catch (Exception ex)
            {
                response.StatusDescription = "404";
                response.StatusCode = 404;
                Utils.LogError($"在接收数据时发生错误:{ex.Message}");
                return "<HTML><BODY> 404!</BODY></HTML>";
            }
        }

        private string OnHandleResponse(string data)
        {
            JsonNode_Object js = MyJson.Parse(data) as JsonNode_Object;
            return "";
        }

        internal void StopHttpServer()
        {
            if (listener != null)
            {
                listener.Close();
                listener.Abort();
                listener = null;
            }
            Utils.LogError($"Http服务端停止服务,时间：{DateTime.Now.ToString()}\r\n");
        }
    }
}
