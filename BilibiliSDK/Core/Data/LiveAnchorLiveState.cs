namespace Bilibili
{
    /// <summary>
    /// 直播开播状态
    /// </summary>
    public enum E_LiveAnchorLiveState : byte
    {
        /// <summary>
        /// 未开播
        /// </summary>
        Close = 0,
        /// <summary>
        /// 直播中
        /// </summary>
        Open = 1,
        /// <summary>
        /// 轮播中
        /// </summary>
        OpenLoop = 2
    }
}
