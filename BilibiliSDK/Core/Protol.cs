using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.IO.Compression;
using static Bilibili.MyJson;
using System.Collections.Concurrent;

namespace Bilibili
{
    /// <summary>
    /// 数据版本
    /// </summary>
    public enum E_ProtolVersion : Int16
    {
        /// <summary>
        /// 实际数据
        /// </summary>
        Data = 0,
        /// <summary>
        /// Zlib压缩数据
        /// </summary>
        ZLib = 2
    }

    /// <summary>
    /// 数据操作类型
    /// </summary>
    public enum E_ProtolOperation : Int32
    {
        /// <summary>
        /// 心跳
        /// </summary>
        Heartbeat = 2,
        /// <summary>
        /// 心跳响应
        /// </summary>
        HeartbeatResponse = 3,
        /// <summary>
        /// 服务器推送消息
        /// </summary>
        PushMessage = 5,
        /// <summary>
        /// 鉴权包
        /// </summary>
        Auth = 7,
        /// <summary>
        /// 鉴权包响应
        /// </summary>
        AuthResponse = 8
    }

    public class Protol
    {
        /// <summary>
        /// 最大包体长度
        /// </summary>
        public const int MaxBodyBufferCount = 2048;
        /// <summary>
        /// 包体头部长度
        /// </summary>
        public const int HeaderLen = 16;
        /// <summary>
        /// 包体长度 Header + Body 4个字节
        /// </summary>
        public Int32 _packetLength = 0;
        /// <summary>
        /// 头部长度 16 2个字节
        /// </summary>
        public Int16 _headerLength = HeaderLen;
        /// <summary>
        /// 版本
        /// </summary>
        public E_ProtolVersion _version = E_ProtolVersion.Data;
        /// <summary>
        /// 操作类型
        /// </summary>
        public E_ProtolOperation _operation = E_ProtolOperation.Heartbeat;
        /// <summary>
        /// 保留字段
        /// </summary>
        public Int32 _sequenceID = 0;
        /// <summary>
        /// 数据
        /// </summary>
        public byte[] _body;
        /// <summary>
        /// 数据Json
        /// </summary>
        public string _jsonBody = string.Empty;
        /// <summary>
        /// 已经解析的json数据
        /// </summary>
        private JsonNode_Object _json;
        /// <summary>
        /// 数据的有效性
        /// </summary>
        private bool _valid;

        private Protol()
        {
        }

        /// <summary>
        /// Bilibili 操作对象池子
        /// </summary>
        static ConcurrentStack<Protol> _protolPool = new ConcurrentStack<Protol>();
        /// <summary>
        /// 获取一个未使用的 操作对象
        /// </summary>
        internal static Protol GetProtol()
        {
            Protol p;
            if (_protolPool.TryPop(out p))
                return p;
            return new Protol();
        }
        /// <summary>
        /// 还原一个使用中的 操作对象
        /// </summary>
        public void RePoolProtol()
        {
            _valid = false;
            _packetLength = 0;
            _headerLength = HeaderLen;
            _version = E_ProtolVersion.Data;
            _operation = E_ProtolOperation.Heartbeat;
            _sequenceID = 0;
            _body = null;
            _jsonBody = string.Empty;
            _json = null;
            _protolPool.Push(this);
        }

        public ArraySegment<byte> Pack_ArraySegment()
        {
            return new ArraySegment<byte>(Pack());
        }

        public byte[] Pack()
        {
            //_jsonBody = "{\"roomid\":0,\"protover\":2,\"uid\":3403215759,\"key\":\"Cx_5zHWbgtGikKOW-93czmVNhrfezclk9YUUqsHXRFKAhA7uhMacvUfxh1wmRKcOv1p33vM6NcdHl0z-ERnb5lnn7UkJQMwVeGSdLTwLTl-cHNyZRWGcU-153QNe6SnrGAEdfkMAIdlT9rtGfI8G\",\"group\":\"open\"}";
            byte[] bodys = Encoding.UTF8.GetBytes(_jsonBody);

            _packetLength = _headerLength + bodys.Length;
            _body = new byte[0];
            IEnumerable<byte> datas = _body.Concat(BitConverter.GetBytes(_packetLength).Reverse());
            datas = datas.Concat(BitConverter.GetBytes(_headerLength).Reverse());
            datas = datas.Concat(BitConverter.GetBytes((Int16)_version).Reverse());
            datas = datas.Concat(BitConverter.GetBytes((Int32)_operation).Reverse());
            datas = datas.Concat(BitConverter.GetBytes(_sequenceID).Reverse());
            _body = datas.Concat(bodys).ToArray();

            return _body;
        }

        public void UnPack(byte[] buffer)
        {
            if (buffer.Length < _headerLength)
            {
                Utils.LogError("包数据不够 包头不够");
                RePoolProtol();
                return;
            }
            byte[] offsets = new byte[4];
            Buffer.BlockCopy(buffer, 0, offsets, 0, 4);
            _packetLength = BitConverter.ToInt32(offsets.Reverse().ToArray(), 0);
            Array.Clear(offsets, 0, 4);
            Buffer.BlockCopy(buffer, 4, offsets, 2, 2);
            _headerLength = BitConverter.ToInt16(offsets.Reverse().ToArray(), 0);
            Array.Clear(offsets, 0, 4);
            Buffer.BlockCopy(buffer, 6, offsets, 2, 2);
            _version = (E_ProtolVersion)BitConverter.ToInt16(offsets.Reverse().ToArray(), 0);
            Array.Clear(offsets, 0, 4);
            Buffer.BlockCopy(buffer, 8, offsets, 0, 4);
            _operation = (E_ProtolOperation)BitConverter.ToInt32(offsets.Reverse().ToArray(), 0);
            Array.Clear(offsets, 0, 4);
            Buffer.BlockCopy(buffer, 12, offsets, 0, 4);
            _sequenceID = BitConverter.ToInt32(offsets.Reverse().ToArray(), 0);
            if (_packetLength <= 0 || _packetLength > MaxBodyBufferCount)
            {
                Utils.LogError("包数据长度不对  PacketLength=" + _packetLength);
                RePoolProtol();
                return;
            }
            if (_headerLength != HeaderLen)
            {
                Utils.LogError("包数据头长度不对  HeaderLength=" + _headerLength);
                RePoolProtol();
                return;
            }
            if (_operation == E_ProtolOperation.HeartbeatResponse)
            {
                RePoolProtol();
                return;
            }
            int bodyLen = _packetLength - _headerLength;
            _body = new byte[bodyLen];
            Buffer.BlockCopy(buffer, _headerLength, _body, 0, bodyLen);
            if (bodyLen <= 0)
            {
                RePoolProtol();
                //没有任何消息
                return;
            }
            if (_version == E_ProtolVersion.Data)
            {
                _jsonBody = Encoding.UTF8.GetString(_body);
                BilibiliData.Instacne.AddOperatData(this);
                _valid = true;
                return;
            }
            else if (_version == E_ProtolVersion.ZLib)
            {
                _body = MicrosoftDecompress(_body);
                bodyLen = _body.Length;
                int cmdSize, offset = 0;
                while (offset < bodyLen)
                {
                    Array.Clear(offsets, 0, 4);
                    Buffer.BlockCopy(_body, offset, offsets, 0, 4);
                    cmdSize = BitConverter.ToInt32(offsets, 0);
                    if (offset + cmdSize > bodyLen)
                        return;
                    Protol pr = GetProtol();
                    byte[] cmdBody = new byte[cmdSize];
                    Buffer.BlockCopy(_body, offset, cmdBody, 0, cmdSize);
                    pr.UnPack(cmdBody);
                    offset += cmdSize;
                }
            }
            RePoolProtol();
        }

        public JsonNode_Object Json()
        {
            if (null == _json && _valid)
                _json = MyJson.Parse(_jsonBody) as JsonNode_Object;
            return _json;
        }
        public LivePlayer Player()
        {
            if (null == Json())
                return null;
            return LivePlayer.GetInstance(Json());
        }

        //TODO:测试方法
        internal void UnPackByAlphaTest(string contet, string id = "111111", string name = "外星修沟")
        {
            _jsonBody = "{\"cmd\":\"LIVE_OPEN_PLATFORM_DM\",\"data\":{\"room_id\":123321,\"fans_medal_level\":22,\"fans_medal_name\":\"夜店牌修沟\",\"fans_medal_wearning_status\":false,\"guard_level\":2,\"uname\":\"" + name + "\",\"uid\":" + id + ",\"uface\":\"www.baidu.com/image\",\"msg\":\"" + contet + "\"}}";
            _valid = true;
        }

        #region Microsoft Zlib
        // 使用System.IO.Compression进行Deflate压缩
        static byte[] MicrosoftCompress(byte[] data)
        {
            MemoryStream uncompressed = new MemoryStream(data);
            MemoryStream compressed = new MemoryStream();
            DeflateStream deflateStream = new DeflateStream(compressed, CompressionMode.Compress);
            uncompressed.CopyTo(deflateStream); // 用 CopyTo 将需要压缩的数据一次性输入；也可以使用Write进行部分输入
            deflateStream.Close();  // 在Close中，会先后执行 Finish 和 Flush 操作。
            byte[] result = compressed.ToArray();
            uncompressed.Dispose();
            deflateStream.Dispose();
            compressed.Dispose();
            return result;
        }
        // 使用System.IO.Compression进行Deflate解压
        static byte[] MicrosoftDecompress(byte[] data)
        {
            MemoryStream compressed = new MemoryStream(data);
            MemoryStream decompressed = new MemoryStream();
            DeflateStream deflateStream = new DeflateStream(compressed, CompressionMode.Decompress);
            deflateStream.CopyTo(decompressed);
            deflateStream.Close();
            byte[] result = decompressed.ToArray();
            decompressed.Dispose();
            deflateStream.Dispose();
            compressed.Dispose();
            return result;
        }
        #endregion

        #region zlib.net
        //static byte[] ZLibDotnetCompress(byte[] data)
        //{
        //    MemoryStream compressed = new MemoryStream();
        //    ZOutputStream outputStream = new ZOutputStream(compressed, 2);
        //    outputStream.Write(data, 0, data.Length); // 这里采用的是用 Write 来写入需要压缩的数据；也可以采用和上面一样的方法
        //    outputStream.Close();
        //    byte[] result = compressed.ToArray();
        //    compressed.Dispose();
        //    outputStream.Dispose();
        //    return result;
        //}

        //static byte[] ZLibDotnetDecompress(byte[] data, int size)
        //{
        //    MemoryStream compressed = new MemoryStream(data);
        //    zlib.ZInputStream inputStream = new zlib.ZInputStream(compressed);
        //    byte[] result = new byte[size];   // 由于ZInputStream 继承的是BinaryReader而不是Stream, 只能提前准备好输出的 buffer 然后用 read 获取定长数据。
        //    inputStream.read(result, 0, result.Length); // 注意这里的 read 首字母是小写            
        //    compressed.Dispose();
        //    inputStream.Dispose();
        //    return result;
        //}
        #endregion
    }
}
