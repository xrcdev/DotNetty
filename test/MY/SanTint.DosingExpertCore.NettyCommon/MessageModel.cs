using MessagePack;
using System.ComponentModel.DataAnnotations;
using KeyAttribute = MessagePack.KeyAttribute;

namespace SanTint.MessageCenterCore.NettyCommon
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public enum COMMAND
    {
        /// <summary>
        /// 未知
        /// </summary>
        UnKnown = 0,
        /// <summary>
        /// 业务消息
        /// </summary>
        Message = 1,
        /// <summary>
        /// 登录/上线
        /// </summary>
        Login = 2,
        /// <summary>
        /// 用于健康检查
        /// </summary>
        Ping = 3,
        /// <summary>
        /// 下线通知
        /// </summary>
        ConnectLost = 4,
    }

    
    [MessagePackObject]
    public class Message
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        [Key(0)]
        public COMMAND Command { get; set; }

        /// <summary>
        /// 内容为 BussnissMessage 序列化后的结果
        /// </summary>
        [Key(1)]
        public string Content { get; set; }

        /// <summary>
        /// 客户端标志
        /// </summary>
        [Key(2)]
        public string ClientID { get; set; }

        /// <summary>
        /// 消息创建时间
        /// </summary>
        [Key(3)]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 是否需要缓存,用户客户端不在线时
        /// </summary>
        [Key(4)]
        public bool IsRequireCache { get; set; }
    }

    public class BussnissMessage
    {
        /// <summary>
        /// 业务编码
        /// </summary>
        public string BussnissCode { get; set; }

        /// <summary>
        /// 业务数据 ,Json序列化后的字符串
        /// </summary>
        public string Data { get; set; }
         
    }

}