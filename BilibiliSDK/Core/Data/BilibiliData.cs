using FairyGUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Table;
using static Bilibili.MyJson;

namespace Bilibili
{
    public class BilibiliData : LoadDataBase
    {
        private static BilibiliData _instance;
        public static BilibiliData Instacne
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BilibiliData();                    
                }
                return _instance;
            }
        }
        private int hashcode;
        /// <summary>
        /// 操作队列 （bilibili websoket线程使用）
        /// 注意 里面可能有不使用的数据 比如Auth返回包 心跳包等
        /// </summary>
        private ConcurrentQueue<Protol> _operatQueue;
        /// <summary>
        /// 操作队列最大冗余长度
        /// </summary>
        private const int _operatQueueMaxCount = 1000;
        /// <summary>
        /// 当前操作队列长度
        /// </summary>
        private int _opreatQueueCount = 0;

        //========数据事件===============================================================================================================//

        /// <summary>
        /// 更新直播间排行榜事件
        /// </summary>
        public event Action<LiveRoomRankingData> OnUpdateLiveRoomRanking;

        //========用户数据===============================================================================================================//

        #region 用户数据

        /// <summary>
        /// 直播间ID 数据处理时 抛弃非当前房间ID的数据
        /// </summary>
        Int32 _roomId = 0;
        /// <summary>
        /// 当前主播ID 数据处理时 抛弃非当前主播ID的数据
        /// </summary>
        Int64 _bilibiliLiveAnchorID = 0;

        #endregion

        /// <summary>
        /// 直播间排行榜数据
        /// </summary>
        private LiveRoomRankingData _liveRoomRankingData;

        //========用户数据===============================================================================================================//

        BilibiliData() : base()
        {
            hashcode = Thread.CurrentThread.ManagedThreadId;
            _operatQueue = new ConcurrentQueue<Protol>();
            _liveRoomRankingData = new LiveRoomRankingData();
        }

        internal void AddOperatData(Protol data)
        {
            _operatQueue.Enqueue(data);
            Interlocked.Increment(ref _opreatQueueCount);
            if (_opreatQueueCount > _operatQueueMaxCount)
            {
                Protol p;
                if (_operatQueue.TryDequeue(out p))
                {
                    Interlocked.Decrement(ref _opreatQueueCount);
                    p.RePoolProtol();
                }
            }
        }
        /// <summary>
        /// 尝试获取 操作数据
        /// 使用后需要 RePoolProtol
        /// </summary>
        public Protol TryGetOperatData()
        {
            Protol p = null;
            if (_operatQueue.TryDequeue(out p))
                Interlocked.Decrement(ref _opreatQueueCount);
            return p;
        }

        //========辅助函数===============================================================================================================//

        internal void SetRoomID(int id)
        {
            _roomId = id;
        }
        internal void SetBilibiliLiveAnchorID(long id)
        {
            _bilibiliLiveAnchorID = id;
        }
        internal int GetRoomID()
        {
            return _roomId;
        }
        internal Int64 GetBilibiliLiveAnchorID()
        {
            return _bilibiliLiveAnchorID;
        }

        //========数据表加载函数===============================================================================================================//
        public override void InitLoadDataConfig()
        {
            ConfigList.Add(alphatest.Instance);
        }

        public override void LoadDataComplete()
        {
            Dictionary<string, List<DefaultDataBase>>.Enumerator iter;
            Dictionary<string, List<DefaultDataBase>> dls;
            DefaultDataBase ddb;
            List<DefaultDataBase> ls;
            alphaTestData.Clear();
            for (int i = 0; i < ConfigList.Count; i++)
            {
                ddb = ConfigList[i];
                dls = ddb.getdatas();
                if (dls == null)
                {
                    continue;
                }
                if (ddb is alphatest)
                {
                    iter = dls.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        ls = iter.Current.Value;
                        for (int q = 0; q < ls.Count; q++)
                        {
                            alphaTestData.Add((alphatest)ls[q]);
                        }
                    }
                    iter.Dispose();
                }
            }
            dls = null;
            ddb = null;
        }

        //========辅助函数===============================================================================================================//

        internal void OnHttpResponse_GetAnchorInfo(JsonNode_Object json)
        {
            throw new NotImplementedException();
        }
        internal void OnHttpResponse_GetAnchorLongLiveInfo(JsonNode_Object json)
        {
            throw new NotImplementedException();
        }
        internal void OnHttpResponse_GetUserAndAnchorRelation(JsonNode_Object json)
        {
            throw new NotImplementedException();
        }
        internal void OnHttpResponse_GetAllLiveAnchor(JsonNode_Object json)
        {
            throw new NotImplementedException();
        }
        internal void OnHttpResponse_GetLiveRoomRanking(JsonNode_Object json)
        {
            json = json["data"] as JsonNode_Object;
            _liveRoomRankingData.UpdateRanking(json["gold_rank"].AsList());
            OnUpdateLiveRoomRanking?.Invoke(_liveRoomRankingData);
        }

        //========Test===============================================================================================================//
        //TODO：Test 功能 
        List<alphatest> alphaTestData = new List<alphatest>();
        bool startTest = false;
        int alphaRunIndex = 0;
        float alphaTime = 0;
        public void StartAlphaTest()
        {
            Timers.inst.AddUpdate(E_TimersInterval.Second, StartAlphaTestUpdate);
            startTest = true;
        }

        private void StartAlphaTestUpdate(object param)
        {
            if (startTest && alphaTestData.Count > 0)
            {
                alphaTime += 1;
                if (alphaTime < alphaTestData[alphaRunIndex].Time)
                    return;
                alphaTime = 0;
                ++alphaRunIndex;
                if (alphaRunIndex > alphaTestData.Count - 1)
                {
                    startTest = false;
                    return;
                }
                
                var protol = Protol.GetProtol();
                protol.UnPackByAlphaTest(alphaTestData[alphaRunIndex].Contet);
                AddOperatData(protol);
            }
        }


    }
}
