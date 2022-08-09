using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Bilibili.MyJson;

namespace Bilibili
{
    public class BilibiliSDK
    {
        private static BilibiliSDK _instance;
        /// <summary>
        /// 唯一BilibiliSDK 实例
        /// </summary>
        public static BilibiliSDK SDK
        {
            get
            {
                if (null == _instance)
                {

                    _instance = new BilibiliSDK();
                }
                return _instance;
            }
        }
        private BilibiliSDK()
        {
            Connected += OnConnectedWebSocket;
            Disconnect += OnDisconnectWebSocket;
            ConnectException += OnConnectExceptionWebSocket;
            ConnectFailed += OnConnectFailed;
            StateChange += OnStateChange;
        }

        ClientWebSocket cws;//当前cws
        ClientWebSocketOptions cws_option;//当前 cws option
        Uri url;//当前 url
        int reConnectCount = 0;//重连次数
        bool testEnvironment = false;//测试环境
        BilibiliState state = BilibiliState.Disconnect;//连接状态

        public void OnDestroy()
        {
            StateChange?.Invoke(BilibiliState.Disconnect, cws, "关闭游戏");
        }

        ~BilibiliSDK()
        {
            if (null != cws)
                cws.Abort();
            state = BilibiliState.Disconnect;
            cws = null;//当前cws
            cws_option = null;//当前 cws option
            url = null;//当前 url 
            _auth_body = null;
            _hosts = null;
            _ips = null;
            _ws_ports = null;
            _wss_ports = null;
            _tcp_ports = null;
            Connecting = null;
            Connected = null;
            Disconnect = null;
            ConnectSucceed = null;
            ConnectFailed = null;
            ConnectException = null;
            StateChange = null;
        }

        //============事件相关========================================================================================================//

        #region 事件

        /// <summary>
        /// 连接服务器中
        /// </summary>
        public event Action Connecting;

        /// <summary>
        /// 连接到服务器 事件通知 
        /// </summary>
        public event Action Connected;

        /// <summary>
        /// 断开服务器 事件通知
        /// 为什么断开服务器
        /// </summary>
        public event Action<string> Disconnect;

        /// <summary>
        /// 连接上服务 事件通知 还有Auth 不然不会算成功
        /// </summary>
        public event Action ConnectSucceed;

        /// <summary>
        /// 连接服务器失败 未连接服务器 事件通知 
        /// 为什么连接失败
        /// </summary>
        public event Action<string> ConnectFailed;

        /// <summary>
        /// 连接异常 已连接服务器 等待重试
        /// </summary>
        public event Action<ClientWebSocket> ConnectException;

        /// <summary>
        /// 连接服务器状态变化 事件
        /// </summary>
        private event Action<BilibiliState, ClientWebSocket, object> StateChange;

        #endregion

        //============配置相关========================================================================================================//

        #region 连接配置  

        /// <summary>
        /// B站Host
        /// </summary>
        const string BilibiliHost = "live-open.biliapi.com";
        /// <summary>
        /// WebSocket连接模板
        /// </summary>
        const string BilibiliWS_Str = "ws://{0}:{1}/sub";
        /// <summary>
        /// 请求主播信息 POST https://live-open.biliapi.com/v1/mcnuser/anchor_info
        /// </summary>
        const string BilibiliHttps_GetAnchorInfo = "https://live-open.biliapi.com/v1/mcnuser/anchor_info";
        /// <summary>
        /// 请求主播开播数据（天维度） POST https://live-open.biliapi.com/v1/mcnuser/anchor_live_data
        /// </summary>
        const string BilibiliHttps_GetAnchorLongLiveInfo = "https://live-open.biliapi.com/v1/mcnuser/anchor_live_data";
        /// <summary>
        /// 请求用户和主播大航海/粉丝勋章关系 POST https://live-open.biliapi.com/v1/mcnuser/relation
        /// </summary>
        const string BilibiliHttps_GetUserAndAnchorRelation = "https://live-open.biliapi.com/v1/mcnuser/anchor_live_data";
        /// <summary>
        /// 请求公会下正在直播的主播 POST https://live-open.biliapi.com/v1/mcnuser/guild_living_anchor
        /// </summary>
        const string BilibiliHttps_GetAllLiveAnchor = "https://live-open.biliapi.com/v1/mcnuser/anchor_live_data";
        /// <summary>
        /// 请求直播间高能榜信息 POST https://live-open.biliapi.com/v1/mcnuser/anchor_gold_rank
        /// </summary>
        const string BilibiliHttps_GetLiveRoomRanking = "https://live-open.biliapi.com/v1/mcnuser/anchor_live_data";
        /// <summary>
        /// B站Host测试环境
        /// </summary>
        string BilibiliHost_Test = "test-live-open.biliapi.net";
        /// <summary>
        /// 获取WebSocket 连接信息URL
        /// </summary>
        string BilibiliGetWebSocketUrl => string.Format("https://{0}/v1/common/websocketInfo", testEnvironment ? BilibiliHost_Test : BilibiliHost);
        /// <summary>
        /// auth 内容
        /// </summary>
        string _auth_body;
        List<string> _hosts = new List<string>();
        List<string> _ips = new List<string>();
        /// <summary>
        /// websocket主机端口
        /// </summary>
        List<string> _ws_ports = new List<string>();
        List<string> _wss_ports = new List<string>();
        List<string> _tcp_ports = new List<string>();

        #endregion

        #region 用户配置

        /// <summary>
        /// 公司唯一key
        /// </summary>
        string CompanyKey = "你的access_key";
        /// <summary>
        /// 公司唯一秘钥
        /// </summary>
        string CompanySecret = "你的access_secret";

        #endregion

        //============辅助相关========================================================================================================//

        #region 辅助函数

        /// <summary>
        /// 设置为测试环境
        /// </summary>
        public void SetTest(string host = "test-live-open.biliapi.net")
        {
            BilibiliHost_Test = host;
            testEnvironment = true;
        }
        /// <summary>
        /// 设置CompanyKey / Secret
        /// </summary>
        public void SetCompanyKeySecret(string key, string secret)
        {
            CompanyKey = key;
            CompanySecret = secret;
        }
        /// <summary>
        /// 设置RoomID
        /// </summary>
        public void SetRoomID(int id)
        {
            BilibiliData.Instacne.SetRoomID(id);
        }
        /// <summary>
        /// 设置主播ID
        /// </summary>
        public void SetBilibiliLiveAnchorID(int id)
        {
            BilibiliData.Instacne.SetBilibiliLiveAnchorID(id);
        }
        /// <summary>
        /// Http返回码验证
        /// </summary>
        bool HttpCodeVerify(int code, out string message)
        {
            message = "未知错误";
            switch (code)
            {
                case 0:
                    message = "ok";
                    return true;
                case 4000:
                    message = "参数错误,请检查必填参数，参数大小限制";
                    break;
                case 4001:
                    message = "应用无效,请检查header的x-bili-accesskeyid是否为空，或者有效";
                    break;
                case 4002:
                    message = "签名异常,请检查header的Authorization";
                    break;
                case 4003:
                    message = "请求过期,请检查header的x-bili-timestamp";
                    break;
                case 4004:
                    message = "重复请求,请检查header的x-bili-nonce";
                    break;
                case 4005:
                    message = "签名method异常,请检查header的x-bili-signature-method";
                    break;
                case 4006:
                    message = "版本异常,请检查header的x-bili-version";
                    break;
                case 4007:
                    message = "IP白名单限制,请确认请求服务器是否在报备的白名单内";
                    break;
                case 4008:
                    message = "权限异常,请确认接口权限";
                    break;
                case 4009:
                    message = "频率限制,请检查请求频率";
                    break;
                case 4010:
                    message = "接口不存在,请确认请求接口url";
                    break;
                case 4011:
                    message = "Content-Type不为application/json,请检查header的Content-Type";
                    break;
                case 4012:
                    message = "MD5校验失败,请检查header的x-bili-content-md5";
                    break;
                case 4013:
                    message = "Accept不为application/json,请检查header的Accept";
                    break;
                case 5000:
                    message = "服务异常,请联系B站对接同学";
                    break;
                case 5001:
                    message = "请求超时,请求超时";
                    break;
                case 5002:
                    message = "内部错误,请联系B站对接同学";
                    break;
                case 5003:
                    message = "配置错误,请联系B站对接同学";
                    break;
                case 5004:
                    message = "房间白名单限制,请联系B站对接同学";
                    break;
                case 5005:
                    message = "房间黑名单限制,请联系B站对接同学";
                    break;
                case 6000:
                    message = "验证码错误,验证码校验失败";
                    break;
                case 6001:
                    message = "手机号码错误,检查手机号码";
                    break;
                case 6002:
                    message = "验证码已过期,验证码超过规定有效期";
                    break;
                case 6003:
                    message = "验证码频率限制,检查获取验证码的频率";
                    break;
                case 10000:
                    message = "公会权限校验失败,请检查当前主播是否加入当前公会";
                    break;
                case 10001:
                    message = "房管封禁失败 ,请检查是否有房管权限，具体失败原因请联系B站对接同学";
                    break;
            }
            message = string.Format("[{0}]:{1}", code, message);
            return false;
        }
        /// <summary>
        /// 保存请求的WebSocketInfo 信息
        /// </summary>
        void SaveWebSocketInfo(JsonNode_Object json)
        {
            Utils.Log("保存长连接连接信息");
            json = json["data"] as JsonNode_Object;
            _auth_body = json["auth_body"].AsString();
            //Utils.LogError("AuthBody:" + _auth_body);            
            IList<IJsonNode> jsArray = json["host"].AsList();
            for (int i = 0; i < jsArray.Count; i++)
            {
                _hosts.Add(jsArray[0].AsString());
            }
            jsArray = json["ip"].AsList();
            for (int i = 0; i < jsArray.Count; i++)
            {
                _ips.Add(jsArray[0].AsString());
            }
            jsArray = json["tcp_port"].AsList();
            for (int i = 0; i < jsArray.Count; i++)
            {
                _tcp_ports.Add(jsArray[0].AsString());
            }
            jsArray = json["ws_port"].AsList();
            for (int i = 0; i < jsArray.Count; i++)
            {
                _ws_ports.Add(jsArray[0].AsString());
            }
            jsArray = json["wss_port"].AsList();
            for (int i = 0; i < jsArray.Count; i++)
            {
                _wss_ports.Add(jsArray[0].AsString());
            }
        }
        /// <summary>
        /// Http的签名       
        /// </summary>
        Dictionary<string, string> SignHttp(string param = "{}")
        {
            Utils.Log("获取了一次Http签名");
            List<string> x_bili_sha256 = new List<string>();
            x_bili_sha256.Add("x-bili-timestamp");
            x_bili_sha256.Add("x-bili-signature-method");
            x_bili_sha256.Add("x-bili-signature-nonce");
            x_bili_sha256.Add("x-bili-accesskeyid");
            x_bili_sha256.Add("x-bili-signature-version");
            x_bili_sha256.Add("x-bili-content-md5");
            x_bili_sha256.Sort();

            int time = (int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("x-bili-timestamp", time.ToString());
            dic.Add("x-bili-signature-method", "HMAC-SHA256");
            dic.Add("x-bili-signature-nonce", (new Random().Next(1, 100000) + time).ToString());
            dic.Add("x-bili-accesskeyid", CompanyKey);
            dic.Add("x-bili-signature-version", "1.0");
            dic.Add("x-bili-content-md5", Utils.GetMD5HashByString(param));

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < x_bili_sha256.Count; i++)
            {
                if (dic.ContainsKey(x_bili_sha256[i]))
                {
                    sb.Append(x_bili_sha256[i]);
                    sb.Append(":");
                    sb.Append(dic[x_bili_sha256[i]]);
                    sb.Append('\n');
                }
            }
            string signatureStr = sb.ToString().TrimEnd('\n');
            signatureStr = Utils.GetHamcSha256(Encoding.UTF8.GetBytes(signatureStr), Encoding.UTF8.GetBytes(CompanySecret));

            dic.Add("Authorization", signatureStr);
            dic.Add("Content-Type", "application/json");
            dic.Add("Accept", "application/json");
            return dic;
        }

        #endregion

        //============功能相关========================================================================================================//

        #region 功能函数

        /// <summary>
        /// 开启Http服务器
        /// </summary>
        public void StartHttpServer()
        {
            BilibiliHttpServer.Instance.StartHttpServer();
        }

        public void StopHttpServer()
        {
            BilibiliHttpServer.Instance.StopHttpServer();
        }

        /// <summary>
        /// Http获取WebSocket Info
        /// </summary>
        public void Start()
        {
            Utils.Log("开始尝试获取长连接信息");
            Utils.HttpRequest(BilibiliGetWebSocketUrl, "{}", GetWebSocketInfoResponse, true, SignHttp());
        }
        /// <summary>
        /// Http获取WebSocket Info 返回
        /// </summary>
        void GetWebSocketInfoResponse(string phpResult)
        {
            Utils.Log("GetWebSocketInfo 获取长连接 信息返回：" + phpResult);
            string message, defaultErrorMessage = "";
            if (!string.IsNullOrEmpty(phpResult))
            {
                JsonNode_Object json = MyJson.Parse(phpResult) as JsonNode_Object;
                message = json["message"].AsString();
                if (HttpCodeVerify(json["code"].AsInt(), out defaultErrorMessage))
                {
                    Utils.Log("获取长连接信息成功");
                    SaveWebSocketInfo(json);
                    StartConnectWebSocket();
                    return;
                }
            }
            Utils.LogError("GetWebSocketInfo 获取长连接 失败:" + defaultErrorMessage);
            StateChange?.Invoke(BilibiliState.ConnectFailed, cws, "Http获取长连接信息 失败 【数据：" + phpResult);
        }

        #region WebSocket

        /// <summary>
        /// 开始连接WS
        /// </summary>
        async void StartConnectWebSocket()
        {
            Utils.Log("开始WebSocket 长连接");
            try
            {
                cws = new ClientWebSocket();
                cws_option = cws.Options;
                cws_option.UseDefaultCredentials = true;
                var iter = SignHttp().GetEnumerator();
                while (iter.MoveNext())
                {
                    cws_option.SetRequestHeader(iter.Current.Key, iter.Current.Value);
                }
                iter.Dispose();
                //cws_ct = new CancellationToken();
                url = new Uri(string.Format(BilibiliWS_Str, _hosts[0], _ws_ports[0]));
                Utils.LogError("URL:" + url.ToString());
                StateChange?.Invoke(BilibiliState.Connecting, cws, null);
                await cws.ConnectAsync(url, CancellationToken.None);
                Utils.Log("WebSocket 连接上服务器 还需要Auth");
                StateChange?.Invoke(BilibiliState.ConnectSucceed, cws, null);
                await SendAuth();
            }
            catch (Exception ex)
            {
                Utils.LogError("连接错误： " + ex);
                StateChange?.Invoke(BilibiliState.ConnectException, cws, null);
            }
        }

        /// <summary>
        /// 发送Auth包
        /// </summary>
        /// <returns></returns>
        async Task SendAuth()
        {
            Utils.Log("WebSocket 开始Auth 包");
            if (cws == null /*|| cws_ct == null*/)
            {
                Utils.LogError("异常 发送Auth包的时候 没有找到CWS 或 CWS_Token");
                state = BilibiliState.ConnectException;
                ConnectException?.Invoke(cws);
                return;
            }
            try
            {
                var authP = Protol.GetProtol();
                authP._jsonBody = _auth_body;
                authP._operation = E_ProtolOperation.Auth;
                await cws.SendAsync(authP.Pack_ArraySegment(), WebSocketMessageType.Binary, true, CancellationToken.None); //发送数据
                authP.RePoolProtol();
                var result = new byte[Protol.MaxBodyBufferCount];
                await cws.ReceiveAsync(new ArraySegment<byte>(result), CancellationToken.None);//接受Auth数据                
                authP = Protol.GetProtol();
                authP.UnPack(result);
                JsonNode_Object json = authP.Json();
                if (null == json || json["code"].AsInt() != 0)
                    throw new Exception("Auth 失败：" + authP._jsonBody);
                Utils.Log("WebSocket 开始Auth 成功");
                StateChange?.Invoke(BilibiliState.Connected, cws, null);
            }
            catch (Exception ex)
            {
                Utils.LogError("长连接Auth 发送异常： " + ex);
                StateChange?.Invoke(BilibiliState.ConnectException, cws, "长连接Auth 发送异常： " + ex);
            }
        }
        /// <summary>
        /// 开始心跳包
        /// </summary>
        async void StartHearBeat()
        {
            Utils.Log("WebSocket 开始发送心跳包");
            try
            {
                while (true)
                {
                    if (state != BilibiliState.Connected)
                        return;
                    var authP = Protol.GetProtol();
                    authP._operation = E_ProtolOperation.Heartbeat;
                    await cws.SendAsync(authP.Pack_ArraySegment(), WebSocketMessageType.Binary, true, CancellationToken.None); //发送数据
                    authP.RePoolProtol();
                    await Task.Delay(20000);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError("连接异常： " + ex);
                StateChange?.Invoke(BilibiliState.ConnectException, cws, "连接异常： " + ex);
            }
        }

        /// <summary>
        /// 开始数据等待
        /// </summary>
        async void StartRecvLoop()
        {
            Utils.Log("WebSocket 开始等待接受推送数据");
            try
            {
                while (true)
                {
                    if (state != BilibiliState.Connected)
                        return;
                    var result = new byte[Protol.MaxBodyBufferCount];
                    await cws.ReceiveAsync(new ArraySegment<byte>(result), new CancellationToken());//接受数据
                    Protol recvP = Protol.GetProtol();
                    recvP.UnPack(result);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError("连接异常： " + ex);
                StateChange?.Invoke(BilibiliState.ConnectException, cws, "连接异常： " + ex);
            }
        }

        /// <summary>
        /// 重连服务器
        /// </summary>
        async void ReConnectWebSocket()
        {
            await Task.Delay(reConnectCount * 2000);
            StartConnectWebSocket();
        }

        #endregion

        #region Http

        /// <summary>
        /// 开始HttpLoop
        /// </summary>
        async void StartHttpLoop()
        {
            while (true)
            {
                if (state != BilibiliState.Connected)
                    return;
                HttpGetLiveRoomRanking();
                await Task.Delay(10000);
            }
        }
        /// <summary>
        /// 获取主播信息
        /// </summary>
        public void HttpGetBilibiliLiveAnchorInfo()
        {
            Hashtable hash = new Hashtable();
            hash["user_id"] = BilibiliData.Instacne.GetBilibiliLiveAnchorID();
            string data = JsonConvert.SerializeObject(hash);
            Utils.HttpRequest(BilibiliHttps_GetAnchorInfo, data, GetHttpGetBilibiliLiveAnchorResponse, true, SignHttp(data));
        }
        /// <summary>
        /// 获取主播信息响应
        /// </summary>
        void GetHttpGetBilibiliLiveAnchorResponse(string phpResult)
        {
            Utils.Log("GetHttpGetBilibiliLiveAnchorResponse 获取主播信息响应 信息返回：" + phpResult);
            string message, defaultErrorMessage = "";
            if (!string.IsNullOrEmpty(phpResult))
            {
                JsonNode_Object json = MyJson.Parse(phpResult) as JsonNode_Object;
                message = json["message"].AsString();
                if (HttpCodeVerify(json["code"].AsInt(), out defaultErrorMessage))
                {
                    BilibiliData.Instacne.OnHttpResponse_GetAnchorInfo(json["data"] as JsonNode_Object);
                    return;
                }
                Utils.LogError("GetHttpGetBilibiliLiveAnchorResponse 获取主播信息响应 错误[" + defaultErrorMessage + "]:" + message);
            }
        }
        /// <summary>
        /// 获取主播开播数据信息（多天）
        /// </summary>
        public void HttpGetAnchorLongLiveInfo()
        {
            Hashtable hash = new Hashtable();
            hash["anchor_id"] = BilibiliData.Instacne.GetBilibiliLiveAnchorID();
            hash["time_range"] = 1;//7天 30天
            string data = JsonConvert.SerializeObject(hash);
            Utils.HttpRequest(BilibiliHttps_GetAnchorLongLiveInfo, data, GetHttpGetAnchorLongLiveInfoResponse, true, SignHttp(data));
        }
        /// <summary>
        /// 获取主播开播数据信息（多天）响应
        /// </summary>
        void GetHttpGetAnchorLongLiveInfoResponse(string phpResult)
        {
            Utils.Log("GetHttpGetAnchorLongLiveInfoResponse 获取主播开播数据信息（多天） 信息返回：" + phpResult);
            string message, defaultErrorMessage = "";
            if (!string.IsNullOrEmpty(phpResult))
            {
                JsonNode_Object json = MyJson.Parse(phpResult) as JsonNode_Object;
                message = json["message"].AsString();
                if (HttpCodeVerify(json["code"].AsInt(), out defaultErrorMessage))
                {
                    BilibiliData.Instacne.OnHttpResponse_GetAnchorLongLiveInfo(json["data"] as JsonNode_Object);
                    return;
                }
                Utils.LogError("GetHttpGetAnchorLongLiveInfoResponse 获取主播开播数据信息（多天） 错误[" + defaultErrorMessage + "]:" + message);
            }
        }
        /// <summary>
        /// 获取用户和主播的粉丝关系
        /// </summary>
        public void HttpGetUserAndAnchorRelation(long userid)
        {
            Hashtable hash = new Hashtable();
            hash["user_id"] = userid;
            hash["anchor_id"] = BilibiliData.Instacne.GetBilibiliLiveAnchorID();
            string data = JsonConvert.SerializeObject(hash);
            Utils.HttpRequest(BilibiliHttps_GetUserAndAnchorRelation, data, GetHttpGetUserAndAnchorRelationResponse, true, SignHttp(data));
        }
        /// <summary>
        /// 获取用户和主播的粉丝关系 响应
        /// </summary>
        void GetHttpGetUserAndAnchorRelationResponse(string phpResult)
        {
            Utils.Log("GetHttpGetUserAndAnchorRelationResponse 获取用户和主播粉丝关系 信息返回：" + phpResult);
            string message, defaultErrorMessage = "";
            if (!string.IsNullOrEmpty(phpResult))
            {
                JsonNode_Object json = MyJson.Parse(phpResult) as JsonNode_Object;
                message = json["message"].AsString();
                if (HttpCodeVerify(json["code"].AsInt(), out defaultErrorMessage))
                {
                    BilibiliData.Instacne.OnHttpResponse_GetUserAndAnchorRelation(json["data"] as JsonNode_Object);
                    return;
                }
                Utils.LogError("GetHttpGetUserAndAnchorRelationResponse 获取用户和主播粉丝关系 错误[" + defaultErrorMessage + "]:" + message);
            }
        }
        /// <summary>
        /// 获取公会下所有主播
        /// </summary>
        public void HttpGetAllLiveAnchor()
        {
            string data = "{}";
            Utils.HttpRequest(BilibiliHttps_GetAllLiveAnchor, data, HttpGetAllLiveAnchorResponse, true, SignHttp(data));
        }
        /// <summary>
        /// 获取公会下所有主播 响应
        /// </summary>
        void HttpGetAllLiveAnchorResponse(string phpResult)
        {
            Utils.Log("HttpGetAllLiveAnchorResponse 获取公会下所有主播 信息返回：" + phpResult);
            string message, defaultErrorMessage = "";
            if (!string.IsNullOrEmpty(phpResult))
            {
                JsonNode_Object json = MyJson.Parse(phpResult) as JsonNode_Object;
                message = json["message"].AsString();
                if (HttpCodeVerify(json["code"].AsInt(), out defaultErrorMessage))
                {
                    BilibiliData.Instacne.OnHttpResponse_GetAllLiveAnchor(json["data"] as JsonNode_Object);
                    return;
                }
                Utils.LogError("HttpGetAllLiveAnchorResponse 获取公会下所有主播 错误[" + defaultErrorMessage + "]:" + message);
            }
        }
        /// <summary>
        /// 获取直播间高能榜
        /// </summary>
        public void HttpGetLiveRoomRanking()
        {
            Hashtable hash = new Hashtable();
            hash["page"] = 1;
            hash["page_size"] = 10;
            hash["anchor_id"] = BilibiliData.Instacne.GetBilibiliLiveAnchorID();
            string data = JsonConvert.SerializeObject(hash);
            Utils.HttpRequest(BilibiliHttps_GetLiveRoomRanking, data, HttpGetLiveRoomRankingResponse, true, SignHttp(data));
        }
        /// <summary>
        /// 获取直播间高能榜 响应
        /// </summary>
        void HttpGetLiveRoomRankingResponse(string phpResult)
        {
            Utils.Log("HttpGetLiveRoomRankingResponse 获取直播间高能榜 信息返回：" + phpResult);
            string message, defaultErrorMessage = "";
            if (!string.IsNullOrEmpty(phpResult))
            {
                JsonNode_Object json = MyJson.Parse(phpResult) as JsonNode_Object;
                message = json["message"].AsString();
                if (HttpCodeVerify(json["code"].AsInt(), out defaultErrorMessage))
                {
                    BilibiliData.Instacne.OnHttpResponse_GetLiveRoomRanking(json["data"] as JsonNode_Object);
                    return;
                }
                Utils.LogError("HttpGetLiveRoomRankingResponse 获取直播间高能榜 错误[" + defaultErrorMessage + "]:" + message);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 连接成功到WebSocket 开启数据接收 开启心跳包
        /// </summary>
        void OnConnectedWebSocket()
        {
            reConnectCount = 0;
            StartRecvLoop();
            StartHearBeat();
            StartHttpLoop();
        }
        /// <summary>
        /// 断开WebSocket 清空数据
        /// </summary>
        void OnDisconnectWebSocket(string error)
        {
            Utils.LogError("OnDisconnectWebSocket");
            if (null != cws)
                cws.Abort();
            cws = null;//当前cws
            cws_option = null;//当前 cws option
            url = null;//当前 url 
            _auth_body = null;
            _hosts.Clear();
            _ips.Clear();
            _ws_ports.Clear();
            _wss_ports.Clear();
            _tcp_ports.Clear();
            _instance = null;
        }
        /// <summary>
        /// WebSocket 连接异常
        /// </summary>
        void OnConnectExceptionWebSocket(ClientWebSocket errorCws = null)
        {
            if (state == BilibiliState.Disconnect)
                return;
            if (null != cws && cws != errorCws)
                return;
            if (null != cws)
            {
                cws.Abort();
                cws.Dispose();
            }
            cws = null;
            ++reConnectCount;
            if (reConnectCount > 3)
            {
                ConnectFailed?.Invoke("重连3次都失败了");
                return;
            }
            ReConnectWebSocket();
        }
        /// <summary>
        /// 连接错误
        /// </summary>
        void OnConnectFailed(string error)
        {
            StateChange?.Invoke(BilibiliState.Disconnect, cws, error);
        }
        /// <summary>
        /// 连接状态变化
        /// </summary>
        private void OnStateChange(BilibiliState sta, ClientWebSocket ws, object param)
        {
            if (cws != ws)
                return;
            if (state == BilibiliState.Disconnect)
            {
                switch (sta)
                {
                    case BilibiliState.Disconnect:
                    case BilibiliState.ConnectSucceed:
                    case BilibiliState.Connected:
                    case BilibiliState.ConnectException:
                        return;
                }
            }
            state = sta;
            switch (state)
            {
                case BilibiliState.Connected:
                    Connected?.Invoke();
                    break;
                case BilibiliState.Disconnect:
                    Disconnect?.Invoke(param.ToString());
                    break;
                case BilibiliState.Connecting:
                    Connecting?.Invoke();
                    break;
                case BilibiliState.ConnectSucceed:
                    ConnectSucceed?.Invoke();
                    break;
                case BilibiliState.ConnectFailed:
                    ConnectFailed?.Invoke(param.ToString());
                    break;
                case BilibiliState.ConnectException:
                    ConnectException?.Invoke(cws);
                    break;
            }
        }     
    }
}
