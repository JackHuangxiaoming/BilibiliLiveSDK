using System.Collections.Generic;
using static Bilibili.MyJson;

namespace Bilibili
{
    /// <summary>
    /// 房间高能榜玩家
    /// </summary>
    public class RoomRankingPlayer : IComparer<RoomRankingPlayer>
    {
        public RoomRankingPlayer()
        {
        }
        public RoomRankingPlayer(string name, int score, int ranking)
        {
            _uname = name;
            _score = score;
            _ranking = ranking;
        }
        public RoomRankingPlayer(JsonNode_Object json)
        {
            UpdateData(json);
        }

        bool _activety;
        int _uid;
        string _uname;
        string _face;
        int _score;
        int _ranking;

        /// <summary>
        /// 玩家是否活跃
        /// 玩家是否活跃
        /// </summary>
        public bool Activety { get => _activety; set => _activety = value; }
        /// <summary>
        /// 玩家ID
        /// </summary>
        public int Uid { get => _uid; }
        /// <summary>
        /// 玩家昵称
        /// </summary>
        public string Uname { get => _uname; }
        /// <summary>
        /// 玩家头像
        /// </summary>
        public string Face { get => _face; }
        /// <summary>
        /// 玩家分数（贡献）能量
        /// </summary>
        public int Score { get => _score; }
        /// <summary>
        /// 排行名次
        /// </summary>
        public int Ranking { get => _ranking; }

        internal void UpdateData(JsonNode_Object json)
        {
            Activety = false;
            if (null == json)
                return;
            try
            {
                _uid = json["uid"].AsInt();
                _uname = json["uname"].AsString();
                _face = json["face"].AsString();
                _ranking = json["rank"].AsInt();
                _score = json["score"].AsInt();
            }
            catch (System.Exception ex)
            {
                Utils.LogError("房间排行榜玩家数据解析出错：" + ex.Message);
                return;
            }
            Activety = true;
        }

        public int Compare(RoomRankingPlayer x, RoomRankingPlayer y)
        {
            return x.Ranking.CompareTo(y.Ranking);
        }
    }
}
