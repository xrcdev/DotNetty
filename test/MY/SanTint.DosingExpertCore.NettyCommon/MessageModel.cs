using MessagePack;
using System.ComponentModel.DataAnnotations;
using KeyAttribute = MessagePack.KeyAttribute;

namespace SanTint.DosingExpertCore.NettyCommon
{
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

    public interface IMessage
    {
        public COMMAND Command { set; get; }
        public string Content { get; set; }
        public string Ticket { get; set; }
        /// <summary>
        /// 客户端IP
        /// </summary>
        public string ClientIP { get; set; }

        /// <summary>
        /// 客户端ID/标志
        /// </summary>
        public string ClientID { get; set; }
    }

    [MessagePackObject]
    public class Message : IMessage
    {
        [Key(0)]
        public COMMAND Command { get; set; }

        /// <summary>
        /// 内容为 BussnissMessage 序列化后的结果
        /// </summary>
        [Key(1)]
        public string Content { get; set; }
        [Key(2)]
        public string Ticket { get; set; }

        [Key(3)]
        public string ClientIP { get; set; }
        [Key(4)]
        public string ClientID { get; set; }
        [Key(5)]
        public DateTime CreateTime { get; set; }
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



        /// <summary>
        /// 消息
        /// </summary>
        public class Message
        {
            /// <summary>
            /// 消息类型
            /// </summary>
            public COMMAND Command { get; set; }
            /// <summary>
            /// 消息内容
            /// </summary>
            public string Content { get; set; }
            /// <summary>
            /// 客户端IP
            /// </summary>
            public string ClientIP { get; set; }
            /// <summary>
            /// 客户端ID/标志
            /// </summary>
            public string ClientID { get; set; }
            /// <summary>
            /// 消息创建时间
            /// </summary>
            public DateTime CreateTime { get; set; }
        }
    }



}