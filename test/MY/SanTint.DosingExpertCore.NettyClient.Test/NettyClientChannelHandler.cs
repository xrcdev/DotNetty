using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using SanTint.MessageCenterCore.NettyCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SanTint.MessageCenterCore.NettyClient 
{
    public class NettyClientChannelHandler : ChannelHandlerAdapter, IDisposable
    {
        public event CommandReceiveEvent MessageReceived;
        public NettyClientChannelHandler()
        {
        }

        public IChannelHandlerContext _Socket { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="temp"></param>
        //public void AddUser(Users temp)
        //{
        //    try
        //    {
        //        AllClients.AddOrUpdate(temp.ClientID, _Socket.Channel, (k, v) => v);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Write(ex, CategoryLog.Error);
        //    }
        //}

        internal static void SendMsgToServer(NettyCommon.Message ms)
        {
            if (AllClients.Any())
            {
                AllClients.First().Value.WriteAndFlushAsync(ms);
            }
        }


        #region 发送消息


        /// <summary>
        /// 给用户发送信息
        /// </summary>
        /// <typeparam name="T">List<int> lgus</typeparam>
        /// <param name="lgus"></param>
        /// <param name="obj"></param>
        public async void SendMessageToUsers<T>(string s, T obj)
        {
            try
            {
                if (AllClients.ContainsKey(s))
                {
                    await AllClients[s].WriteAndFlushAsync(obj);
                }
            }
            catch (Exception ex)
            {
            }
        }

        ///// <summary>
        ///// 单发数据
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="obj"></param>
        //public async void SendData<T>(T obj)
        //{
        //    try
        //    {
        //        await _Socket.WriteAndFlushAsync(obj);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Write(ex, CategoryLog.Error);
        //    }
        //}

        /// <summary>
        /// 群发数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public void GroupSendData<T>(T obj)
        {
            try
            {
                AllClients.Values.ToList().ForEach(async s => await s.WriteAndFlushAsync(obj));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion


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
                }
            });
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is NettyCommon.Message oo)
            {
                //FrmMain.Instance.UpdateInputTextContent("收到服务端发来的消息:" + oo.Content);
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
            try
            {
                //Console.WriteLine($"客户端{context}下线.");
                base.HandlerRemoved(context);

                var key = AllClients.Where(q => q.Value == context.Channel).SingleOrDefault();//.Select(q => q.Key);  //get all keys
                AllClients.TryRemove(key.Key, out IChannel temp);

                //给好友广播下线通知
                //OnMessageOffline(key.Key);
            }
            catch (Exception ex)
            {
            }
        }

        //服务器监听到客户端活动时
        public override void ChannelActive(IChannelHandlerContext context)
        {
            _Socket = context;//赋值
            base.ChannelActive(context);
            try
            {
                Interlocked.Increment(ref clientConnCount);
                context.WriteAndFlushAsync(new NettyCommon.Message()
                {
                    Command = COMMAND.Login,
                    Content = $"login",
                    ClientID = $"clientId{clientConnCount.ToString()}"
                }
                );
                AllClients.AddOrUpdate($"clientId{clientConnCount.ToString()}", context.Channel, (k, v) => v);

            }
            catch (Exception ex)
            {
            }
            string clienData = string.Format("{0}:{1} online.\r\n", DateTime.Now.ToString(), context.Channel.RemoteAddress);
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

        public async void Dispose()
        {
            await _Socket.DisconnectAsync();
            await _Socket.CloseAsync();
        }
        #endregion

    }

}
