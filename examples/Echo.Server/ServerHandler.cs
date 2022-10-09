using DotNetty.Handlers.Flow;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using DotNetty.Transport.Channels;
using Echo.Common;
using DotNetty.Handlers.Timeout;

namespace Echo.Server
{
    public class ServerHandler : FlowControlHandler
    {
        #region 重写基类的方法

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

        /// <summary>
        /// 当收到消息到时触发
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is Song oo)
            {
                 base.ChannelRead(context, message);
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
            base.ChannelInactive(context);
        }

        //客户端下线断线时
        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            base.HandlerRemoved(context);
            //if (AllClients.Any(q => q.Value.ToString() == context.Channel.ToString()))
            //{
            //    var key = AllClients.Where(q => q.Value.ToString() == context.Channel.ToString())
            //   .FirstOrDefault();
            //    AllClients.TryRemove(key.Key, out IChannel temp);
            //    //给好友广播下线通知
            //    MessageOffline?.Invoke(this, key.Key);
            //}
        }

        #endregion 重写基类的方法
    }
}
