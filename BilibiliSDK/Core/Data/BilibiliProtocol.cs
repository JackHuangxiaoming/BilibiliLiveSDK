
namespace Bilibili
{
    /// <summary>
    /// 相关协议
    /// </summary>
    public class BilibiliProtocol
    {
        /// <summary>
        /// 直播间弹幕信息
        /// </summary>
        public const string LIVE_DM = "LIVE_OPEN_PLATFORM_DM";
        /// <summary>
        /// 直播间礼物信息
        /// </summary>
        public const string LIVE_GIFT = "LIVE_OPEN_PLATFORM_SEND_GIFT";
        /// <summary>
        /// 直播间开通大航海信息
        /// </summary>
        public const string LIVE_GUARD = "LIVE_OPEN_PLATFORM_GUARD";
        /// <summary>
        /// 直播间醒目留言信息
        /// </summary>
        public const string LIVE_CHAT = "LIVE_OPEN_PLATFORM_SUPER_CHAT";
        /// <summary>
        /// 直播间醒目留言下线信息
        /// </summary>
        public const string LIVE_CHAT_DEL = "LIVE_OPEN_PLATFORM_SUPER_CHAT_DEL";
    }

    /// <summary>
    /// Bilibili 协议类型
    /// 目前有5种类型 使用中 弹幕，礼物，开通大航海
    /// </summary>
    public enum E_BilibiliProtocolType
    {
        /// <summary>
        /// 直播间 推送 弹幕
        /// </summary>
        Live_DM = 0x0,
        /// <summary>
        /// 直播间 赠送 礼物
        /// </summary>
        Live_Gift = 0x1,
        /// <summary>
        /// 直播间 开通 大航海
        /// </summary>
        Live_Guard = 0x10,
        /// <summary>
        /// 直播间 醒目 留言
        /// </summary>
        Live_Chat = 0x100,
        /// <summary>
        /// 直播间 醒目 线下留言
        /// </summary>
        Live_Chat_Del = 0x1000
    }
}
