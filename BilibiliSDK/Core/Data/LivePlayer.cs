using System;
using System.Collections.Generic;
using static Bilibili.MyJson;

namespace Bilibili
{
    /// <summary>
    /// 直播间玩家信息
    /// 针对自己房间的玩家
    /// </summary>
    public class LivePlayer : SinglePool<LivePlayer>
    {
        const int ActivityValue = 30;//玩家不活跃最大时间
        int uid;//用户id
        string uname;//用户昵称
        string uface;//用户头像
        int fans_level;//粉丝等级
        string fans_name;//粉丝牌名字
        bool fans_state;//粉丝佩戴状态
        int guard_level;//大航海等级
        int room_id;//所在房间的id        
        DateTime lastTime;//最后活跃时间
        E_BilibiliProtocolType type;//消息类型

        //按命令类型 会有不同数据
        //弹幕
        string contendStr;//消息内容

        //礼物
        int gift_id;//礼物id
        string gift_name;//礼物名字
        int gift_num;//礼物数量
        int gift_price;//礼物单价

        //开通大航海
        int guard_num;//开通大航海时间
        string guard_unit;//开通大航海单位        

        /// <summary>
        /// 玩家ID
        /// </summary>
        public int Uid { get => uid; }
        /// <summary>
        /// 玩家昵称
        /// </summary>
        public string Uname { get => uname; }
        /// <summary>
        /// 玩家头像
        /// </summary>
        public string Uface { get => uface; }
        /// <summary>
        /// 玩家粉丝牌 等级
        /// </summary>
        public int Fans_level { get => fans_level; }
        /// <summary>
        /// 玩家粉丝牌 名字
        /// </summary>
        public string Fans_name { get => fans_name; }
        /// <summary>
        /// 玩家粉丝牌 佩戴状态
        /// </summary>
        public bool Fans_state { get => fans_state; }
        /// <summary>
        /// 玩家大航海 等级 0 未开通 1 总督 2 提督 3 舰长
        /// </summary>
        public int Guard_level { get => guard_level; }
        /// <summary>
        /// RoomID
        /// </summary>
        public int Room_id { get => room_id; }
        /// <summary>
        /// 最后活跃时间
        /// </summary>
        public DateTime LastTime { get => lastTime; }
        /// <summary>
        /// 是否是活跃状态
        /// </summary>
        public bool Activity { get => (LastTime - DateTime.Now).TotalSeconds > ActivityValue; }
        /// <summary>
        /// 消息类型
        /// </summary>
        public E_BilibiliProtocolType Type { get => type; }
        /// <summary>
        /// 消息内容
        /// </summary>
        public string ContendStr { get => contendStr; }
        /// <summary>
        /// 礼物ID
        /// </summary>
        public int Gift_id { get => gift_id; }
        /// <summary>
        /// 礼物名字
        /// </summary>
        public string Gift_name { get => gift_name; }
        /// <summary>
        /// 礼物数量
        /// </summary>
        public int Gift_num { get => gift_num; }
        /// <summary>
        /// 礼物单价
        /// </summary>
        public int Gift_price { get => gift_price; }
        /// <summary>
        /// 开通大航海 数量
        /// </summary>
        public int Guard_num { get => guard_num; }
        /// <summary>
        /// 开通大航海数量 的单位
        /// </summary>
        public string Guard_unit { get => guard_unit; }

        public LivePlayer()
        {
        }

        public LivePlayer(string name, string message, int level = 0)
        {
            uname = name;
            contendStr = message;
            guard_level = level;
            valid = true;
        }

        public override void UpdateInfo(JsonNode_Object json)
        {
            valid = false;
            bool isRechargeGuard = false;//是否是直播间充值航海信息
            if (json == null)
                return;
            try
            {
                string cmd = json["cmd"].AsString();
                json = json["data"] as JsonNode_Object;
                switch (cmd)
                {
                    case BilibiliProtocol.LIVE_DM:
                        type = E_BilibiliProtocolType.Live_DM;
                        contendStr = json["msg"].AsString();
                        break;
                    case BilibiliProtocol.LIVE_GIFT:
                        type = E_BilibiliProtocolType.Live_Gift;
                        gift_id = json["gift_id"].AsInt();
                        gift_name = json["gift_name"].AsString();
                        gift_num = json["gift_num"].AsInt();
                        gift_price = json["gift_price"].AsInt();
                        break;
                    case BilibiliProtocol.LIVE_GUARD:
                        type = E_BilibiliProtocolType.Live_Guard;
                        guard_num = json["guard_num"].AsInt();
                        guard_unit = json["guard_unit"].AsString();
                        isRechargeGuard = true;
                        break;
                    default:
                        return;
                }

                room_id = json["room_id"].AsInt();
                fans_level = json["fans_medal_level"].AsInt();
                fans_name = json["fans_medal_name"].AsString();
                fans_state = json["fans_medal_wearning_status"].AsBool();
                guard_level = json["guard_level"].AsInt();
                if (json.ContainsKey("timestamp"))
                    lastTime = Utils.ConvertJavaScriptInt2DateTime(json["timestamp"].AsInt());
                else
                    lastTime = DateTime.Now;
                if (isRechargeGuard)
                    json = json["user_info"] as JsonNode_Object;
                uid = json["uid"].AsInt();
                uname = json["uname"].AsString();
                uface = json["uface"].AsString();
                valid = true;
            }
            catch (Exception ex)
            {
                valid = false;
                Utils.Log("LivePlayer UpdateInfo Error:" + ex.ToString() + json.ToString());
            }
        }
    }
}
