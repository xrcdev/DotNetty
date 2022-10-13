using DotNetty.Handlers.Flow;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using SanTint.Message.MessageCenter.Core.NettyCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SanTint.Message.MessageCenter.Core.NettyServer
{
    public class MessageChannelHandler : FlowControlHandler
    {
        public event CommandReceiveEvent MessageReceived;

        public event MessageSendEvent MessageSend;

        public event MessageGroupSendEvent MessageGroupSend;

        public event MessageOfflineEvent MessageOffline;

        public static volatile ConcurrentDictionary<string, IChannel> AllClients = new ConcurrentDictionary<string, IChannel>();

        public IChannelHandlerContext _Socket { get; set; }

        #region 重写基类的方法

        /// <summary>
        /// 当收到消息到时触发
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is NettyCommon.Message oo)
            {
                MessageReceived?.Invoke(this, new MessageEventArgs(oo));
            }
        }

        //客户端连接进来时
        public override void HandlerAdded(IChannelHandlerContext context)
        {
            base.HandlerAdded(context);
        }

        //服务器监听到客户端活动时
        public override void ChannelActive(IChannelHandlerContext context)
        {
            _Socket = context;//赋值
            base.ChannelActive(context);
        }

        //捕获 异常，并输出到控制台后断开链接，提示：客户端意外断开链接，也会触发
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("异常: " + exception);
            context.CloseAsync();
        }

        //服务器监听到客户端不活动时
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            //FrmMain.Instance.UpdateTextBox($"客户端{context.Channel.RemoteAddress.ToString()}离线了.");
            base.ChannelInactive(context);
        }

        //客户端下线断线时
        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            base.HandlerRemoved(context);
            if (AllClients.Any(q => q.Value.ToString() == context.Channel.ToString()))
            {
                var key = AllClients.Where(q => q.Value.ToString() == context.Channel.ToString())
               .FirstOrDefault();
                AllClients.TryRemove(key.Key, out IChannel temp);
                MessageOffline?.Invoke(this, key.Key);
            }
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            base.UserEventTriggered(context, evt);
            if (evt is IdleStateEvent)
            {
                var e = evt as IdleStateEvent;
                switch (e.State)
                {
                    //长期没收到服务器推送数据
                    case IdleState.ReaderIdle:
                        {
                            //可以重新连接
                            if (!context.Channel.Active)
                            {
                                var endpoint = context.Channel.RemoteAddress as IPEndPoint;
                                context.ConnectAsync(new IPEndPoint(endpoint.Address, endpoint.Port));
                                //Logger.Write(string.Format(" 检测到不活动连接,UserEventTriggered重新连接:", endpoint.ToString()));
                            }
                        }
                        break;
                    //长期未向服务器发送数据
                    case IdleState.WriterIdle:
                        break;

                    case IdleState.AllIdle:
                        break;
                }
            }
        }

        #endregion 重写基类的方法
    }
}
