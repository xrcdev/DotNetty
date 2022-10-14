using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using SanTint.Message.MessageCenter.Core.NettyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SanTint.Message.MessageCenter.Core.NettyClient.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private Bootstrap bootstrap;
        private IChannel clientChannel;
        private MultithreadEventLoopGroup group;
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            group = new MultithreadEventLoopGroup();
            try
            {
                if (bootstrap == null)
                {
                    bootstrap = new Bootstrap();
                    bootstrap
                       .Group(group)
                       .Channel<TcpSocketChannel>()
                       .Option(ChannelOption.TcpNodelay, true)
                       .Option(ChannelOption.SoKeepalive, true)
                       .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                       {
                           IChannelPipeline pipeline = channel.Pipeline;
                           pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(2080, 1, 4, 0, 0));
                           pipeline.AddLast(new CommonEncoder<NettyCommon.Message>());
                           pipeline.AddLast(new CommonDecoder());
                           pipeline.AddLast(new IdleStateHandler(60, 0, 0));//第一个参数为读，第二个为写，第三个 

                           ClientHandler cHandler = new ClientHandler();
                           cHandler.MessageReceived += CHandler_MessageReceived;
                           pipeline.AddLast(cHandler);

                       }));
                }
                if (clientChannel == null)
                {
                    clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8090));
                }
                //await clientChannel.CloseAsync();
            }
            finally
            {
                //await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// 接收到消息 ,后续处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CHandler_MessageReceived(object sender, MessageEventArgs e)
        {
            Console.WriteLine(e.Msg.ToString());
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            NettyCommon.Message message = new NettyCommon.Message();
            message.Command = COMMAND.Login;
            message.ClientID = "ticket1";
            message.Content = Newtonsoft.Json.JsonConvert.SerializeObject(new BussnissMessage() { Data = "abc" });
            clientChannel.WriteAndFlushAsync(message);
        }

        private void btnOff_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
