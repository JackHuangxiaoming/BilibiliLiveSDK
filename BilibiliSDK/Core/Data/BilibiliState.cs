namespace Bilibili
{
    enum BilibiliState
    {
        /// <summary>
        /// 断开连接中
        /// </summary>
        Disconnect = 0,
        /// <summary>
        /// 连接中
        /// </summary>
        Connecting,
        /// <summary>
        /// 连接成功
        /// </summary>
        ConnectSucceed,
        /// <summary>
        /// 连接失败
        /// </summary>
        ConnectFailed,
        /// <summary>
        /// 已连接
        /// </summary>
        Connected,
        /// <summary>
        /// 连接异常
        /// </summary>
        ConnectException

    }
}
