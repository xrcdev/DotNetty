using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DotNetty.Transport.Channels;
using DotNetty.Handlers.Timeout;
using Echo.Common;
using DotNetty.Buffers;
using Newtonsoft.Json;

namespace Echo.Client
{
    internal class ClientHandler : ChannelHandlerAdapter
    {
        #region 重写基类的方法
        private int lossConnectCount = 0;
        private volatile static int clientConnCount = 0;

        public override async void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            await Task.Run(() =>
            {
                try
                {
                    //已经15秒未收到客户端的消息了！
                    if (evt is IdleStateEvent eventState)
                    {
                        if (eventState.State == IdleState.ReaderIdle)
                        {
                            lossConnectCount++;
                            if (lossConnectCount > 2)
                            {
                                //("关闭这个不活跃通道！");
                                context.CloseAsync();
                            }
                        }
                    }
                    else
                    {
                        base.UserEventTriggered(context, evt);
                    }
                }
                catch (Exception ex)
                {
                    //Logger.Write(ex, CategoryLog.Error);
                }
            });
        }
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is Song oo)
            {
            }
        }

        public static volatile ConcurrentDictionary<string, IChannel> AllClients = new ConcurrentDictionary<string, IChannel>();

        //客户端连接进来时
        public override void HandlerAdded(IChannelHandlerContext context)
        {
            //Console.WriteLine($"客户端{context}上线.");
            base.HandlerAdded(context);


            //AllClients.AddOrUpdate((context.Channel.RemoteAddress as IPEndPoint).Port.ToString(), context.Channel, (k, v) => v);
        }

        //客户端下线断线时
        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            //try
            //{
            //    //Console.WriteLine($"客户端{context}下线.");
            //    base.HandlerRemoved(context);

            //    var key = AllClients.Where(q => q.Value == context.Channel).SingleOrDefault();//.Select(q => q.Key);  //get all keys
            //    AllClients.TryRemove(key.Key, out IChannel temp);

            //    //给好友广播下线通知
            //    //OnMessageOffline(key.Key);
            //}
            //catch (Exception ex)
            //{
            //    Logger.Write(ex, CategoryLog.Error);
            //}
        }

        //服务器监听到客户端活动时
        public override void ChannelActive(IChannelHandlerContext context)
        {
            var song = new Song() { Singer = "xrc00001" };
            var str = JsonConvert.SerializeObject(song);
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(str);

            var initialMessage = Unpooled.Buffer(messageBytes.Length);
            initialMessage.WriteBytes(messageBytes);
            context.WriteAndFlushAsync(initialMessage);

            //_Socket = context;//赋值
            //base.ChannelActive(context);
            //try
            //{
            //    Interlocked.Increment(ref clientConnCount);
            //    context.WriteAndFlushAsync(new Message()
            //    {
            //        Command = COMMAND.Login,
            //        Content = $"login now {DateTime.Now.ToString()}",
            //        Ticket = $"clientId{clientConnCount.ToString()}"
            //    }
            //    );
            //    AllClients.AddOrUpdate($"clientId{clientConnCount.ToString()}", context.Channel, (k, v) => v);

            //}
            //catch (Exception ex)
            //{
            //    Logger.Write(ex, CategoryLog.Error);
            //}
            //string clienData = string.Format("{0}:{1} online.\r\n", DateTime.Now.ToString(), context.Channel.RemoteAddress);
            //Logger.Write(clienData, CategoryLog.Error);
        }

        //服务器监听到客户端不活动时
        //public override void ChannelInactive(IChannelHandlerContext context)
        //{
        //    base.ChannelInactive(context);
        //}

        // 输出到客户端，也可以在上面的方法中直接调用WriteAndFlushAsync方法直接输出
        //public override void ChannelReadComplete(IChannelHandlerContext context)
        //{
        //    context.Flush();
        //}

        //捕获 异常，并输出到控制台后断开链接，提示：客户端意外断开链接，也会触发
        //public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        //{
        //    context.CloseAsync();
        //}


        #endregion
    }
}
