using System;
using System.Collections.Generic;
using static Bilibili.MyJson;

namespace Bilibili
{
    /// <summary>
    /// 直播间 高能榜（礼物榜）数据
    /// </summary>
    public class LiveRoomRankingData
    {
        public LiveRoomRankingData()
        {
            InactivatePool = new Queue<RoomRankingPlayer>();
            ActivatePool = new Queue<RoomRankingPlayer>();
            CurRankingPlayers = new List<RoomRankingPlayer>(10);
        }

        public LiveRoomRankingData(RoomRankingPlayer[] players)
        {
            InactivatePool = new Queue<RoomRankingPlayer>();
            ActivatePool = new Queue<RoomRankingPlayer>();
            CurRankingPlayers = new List<RoomRankingPlayer>(10);            
            CurRankingPlayers.AddRange(players);
        }
        /// <summary>
        /// 不活跃排行榜玩家池子
        /// </summary>
        Queue<RoomRankingPlayer> InactivatePool;
        /// <summary>
        /// 活跃排行榜玩家池子
        /// </summary>
        Queue<RoomRankingPlayer> ActivatePool;
        /// <summary>
        /// 排行榜全部玩家数量
        /// </summary>
        public int CurRankingTotalPlayerNumber = 0;
        /// <summary>
        /// 排行榜当前页数
        /// </summary>
        public int CurPage = 1;
        /// <summary>
        /// 当前排行榜玩家
        /// </summary>
        public List<RoomRankingPlayer> CurRankingPlayers;

        /// <summary>
        /// 更新房间排行榜
        /// </summary>
        internal void UpdateRanking(IList<IJsonNode> list)
        {
            CurRankingPlayers.Clear();
            RoomRankingPlayer player;
            while (ActivatePool.Count > 0)
            {
                player = ActivatePool.Dequeue();
                player.Activety = false;
                InactivatePool.Enqueue(player);
            }
            if (null == list || list.Count == 0 || list.Count > 10)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (InactivatePool.Count > 0)
                    {
                        player = InactivatePool.Dequeue();
                        player.UpdateData(list[i] as JsonNode_Object);
                    }
                    else
                        player = new RoomRankingPlayer(list[i] as JsonNode_Object);
                    CurRankingPlayers.Add(player);
                }
            }
            CurRankingPlayers.Sort();
        }
    }
}