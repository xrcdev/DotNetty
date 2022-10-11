using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanTint.DosingExpertCore.NettyCommon;
using System.Net;
using DotNetty.Handlers.Timeout;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;

namespace SanTint.DosingExpertCore.NettyServer
{
    public class ServerHelper
    {
        private ManualResetEvent ManualReset = new ManualResetEvent(false);

        public static ServerHelper Instance = new ServerHelper();

        private ServerHelper()
        {
        }

        #region 服务启动关闭
        public async Task RunServerAsync()
        {
            MultithreadEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
            // 工作线程组，默认为内核数*2的线程数
            MultithreadEventLoopGroup workerGroup = new MultithreadEventLoopGroup();
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 128)
                    .Option(ChannelOption.SoReuseport, true) // 设置端口复用
                    .ChildOption(ChannelOption.SoReuseport, true)
                    .Option(ChannelOption.SoKeepalive, true)

                    //.Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator(1024, 1024, 65536))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        //工作线程连接器 是设置了一个管道，服务端主线程所有接收到的信息都会通过这个管道一层层往下传输
                        //同时所有出栈的消息 也要这个管道的所有处理器进行一步步处理
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(4));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(2048, 0, 4, 0, 0));
                        //pipeline.AddLast(new LoggingHandler());
                        pipeline.AddLast(new CommonEncoder<Message>());
                        pipeline.AddLast(new CommonDecoder());

                        //服务端为读IDLE
                        pipeline.AddLast(new IdleStateHandler(300, 0, 0)); //第一个参数为读，第二个为写，第三个为读写全部

                        //业务handler ，这里是实际处理业务的Handler
                        var serverHandler = new MessageChannelHandler
                        {
                            _Socket = pipeline.FirstContext()
                        };
                        serverHandler.MessageReceived += ServerHandler_MessageReceived;
                        serverHandler.MessageSend += ServerHandler_MessageSend;
                        serverHandler.MessageGroupSend += ServerHandler_MessageGroupSend;

                        pipeline.AddLast(serverHandler);
                    }));

                var boundChannel = await bootstrap.BindAsync(int.Parse("8090"));

                //KeepConnect();
                ManualReset.Reset();
                ManualReset.WaitOne();
            }
            catch (Exception ex)
            {
                string me = ex.Message;
                //Logger.Write(ex, CategoryLog.Error);
            }
            finally
            {
                //释放工作组线程
                await Task.WhenAll(
                   bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                   workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        /// <returns></returns>
        public void StopServerAsync()
        {
            ManualReset.Set();
        }
        #endregion

        /// <summary>
        /// 防止心跳机制不生效
        /// </summary>
        public void KeepConnect()
        {
            var timer = new System.Timers.Timer(TimeSpan.FromMinutes(2).TotalMilliseconds);
            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, x) =>
            {
                try
                {
                    foreach (var item in MessageChannelHandler.AllClients)
                    {
                        if (!item.Value.Active)
                        {
                            var endpoint = item.Value.RemoteAddress as IPEndPoint;
                            item.Value.ConnectAsync(new IPEndPoint(endpoint.Address, endpoint.Port));
                            //Logger.Write(string.Format("Timer检测到不活动连接,UserEventTriggered重新连接:", endpoint.ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Logger.Write(ex, CategoryLog.Error);
                }
            });
            timer.Enabled = true;
            timer.Start();
        }

        #region 注册到Handler上的事件

        private async void ServerHandler_MessageGroupSend(object sender, MessageEventArgs e)
        {
            await Task.Run(() => { BatchSendData(e.CMD); });
        }

        private async void ServerHandler_MessageSend(object sender, MessageEventArgs e)
        {
            await SingleSendData(e.CMD.Ticket, e.CMD);
        }

        private async void ServerHandler_MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                var client = sender as MessageChannelHandler;

                var logTxt = $"收到客户端:{client._Socket.Channel.RemoteAddress.ToString()} 发来的消息: ";

                await Task.Run(() =>
                {
                    switch (e.CMD.Command)
                    {
                        case COMMAND.Login:
                            MessageChannelHandler.AllClients.AddOrUpdate(e.CMD.Ticket, client._Socket.Channel, (k, v) => v);
                            ServerHandler_MessageSend(sender, new MessageEventArgs(new Message
                            {
                                Command = COMMAND.Login,
                                Content = "",
                                Ticket = e.CMD.Ticket
                            }));
                            //FrmMain.Instance.UpdateTextBox(string.Format("User:{0} has Login.", e.CMD.Ticket));
                            return;

                        case COMMAND.Message:
                            //FrmMain.Instance.UpdateTextBox(string.Format("Receive Msg:{0} from User:{1} 。", e.CMD.Content, e.CMD.Ticket));
                            return;

                        default:
                            return;
                    }
                });
            }
            catch (Exception ex)
            {
                //Logger.Write(ex, CategoryLog.Error);
            }
        }

        #endregion 注册到Handler上的事件

        #region 消息通知

        /// <summary>
        /// 给客户端发送消息
        /// </summary>
        /// <param name="msg"></param>
        public void NotifyClient(string ticket, string msg)
        {
            try
            {
                var ms = new Message
                {
                    Command = COMMAND.Message,
                    Content = msg,
                    Ticket = ticket ?? ""
                };

                SendData(ticket, ms);
            }
            catch (Exception ex)
            {
                //FrmMain.Instance.UpdateTextBox("发送消息出现错误:" + ex);
            }
        }

        /// <summary>
        /// 给用户发送信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ticket"></param>
        /// <param name="msgObj"></param>
        private async void SendData<T>(string ticket, T msgObj)
        {
            if (string.IsNullOrWhiteSpace(ticket))
            {
                BatchSendData(msgObj);
            }
            else
            {
                await SingleSendData(ticket, msgObj);
            }
        }

        /// <summary>
        /// 单发数据给客户端
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        private async Task SingleSendData<T>(string ticket, T obj)
        {
            try
            {
                if (MessageChannelHandler.AllClients.TryGetValue(ticket, out IChannel channel))
                {
                    await channel.WriteAndFlushAsync(obj);
                }
            }
            catch (Exception ex)
            {
                //Logger.Write(ex, CategoryLog.Error);
            }
        }

        /// <summary>
        /// 群发数据给客户端
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        private void BatchSendData<T>(T obj)
        {
            try
            {
                MessageChannelHandler.AllClients.Values.ToList().ForEach(async s => await s.WriteAndFlushAsync(obj));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //Logger.Write(ex, CategoryLog.Error);
            }
        }

        /// <summary>
        /// MSMQ消息解析处理
        /// </summary>
        /// <param name="cloudMess"></param>
        //public void MessageParseDeal(CloudMessage cloudMess)
        //{
        //    try
        //    {
        //        PlatformMessage platformMess = new PlatformMessage()
        //        {
        //            MessType = cloudMess.MessType,
        //            Data = cloudMess.Data
        //        };
        //        string sendMsg = JsonDataConvert.Serialize(platformMess);
        //        List<string> notlineticket = new List<string>();
        //        lock (SantintChannelHandler.AllClients)
        //        {
        //            foreach (var token in SantintChannelHandler.AllClients)
        //            {
        //                if (cloudMess.TicketSet.Contains(token.Key))
        //                {
        //                    FrmMain.Instance.UpdateTextBox($"Send Message:{sendMsg} To Ticket:{token.Key}" + token.Key);
        //                    Logger.Write($"Send Message:{sendMsg} To Ticket:{token.Key}" + token.Key);
        //                    try
        //                    {
        //                        //发送信息
        //                        var ms = new CommonLibrary.Message
        //                        {
        //                            Command = COMMAND.Message,
        //                            Content = sendMsg,
        //                        };

        //                        SendData(token.Key, ms);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Logger.Write(string.Format("Send Message: Ticket:{0}  Error:{1}" + token.Key, ex.Message), CategoryLog.Error);
        //                    }
        //                }
        //                else
        //                {
        //                    FrmMain.Instance.UpdateTextBox(string.Format("Ticket: {0} not in MasterServer", token.Key));
        //                    Logger.Write(string.Format("Ticket: {0} not in MasterServer", token.Key), CategoryLog.Info);
        //                }
        //            }
        //            //foreach (string tic in cloudMess.TicketSet)
        //            //{
        //            //    if (!notlineticket.Contains(tic) && !string.IsNullOrWhiteSpace(tic))
        //            //    {
        //            //        isExist += tic + ",";
        //            //    }
        //            //}
        //            //if (!string.IsNullOrWhiteSpace(isExist))
        //            //{
        //            //    FrmMain.Instance.UpdateTextBox(string.Format("Ticket: {0} not online", isExist));
        //            //    Logger.Write(string.Format("Ticket: {0} not online", isExist), CategoryLog.Info);
        //            //}
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Write(ex, CategoryLog.Error);
        //    }
        //}

        #endregion 消息通知

    }
}
