namespace Bilibili
{
    /// <summary>
    /// 主播信息
    /// </summary>
    public class LiveAnchor
    {
        string user_name;//昵称
        string user_avatar;//头像
        int up_level;//up主等级
        int room_id;//房间ID
        E_LiveAnchorLiveState live_status;//开播状态
        int fans_count;//粉丝数量
        int guard_count;//大航海数量
        //List<grard_list> guard_list;//粉丝开通大航海 数据
    }
}
