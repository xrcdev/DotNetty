using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using DotNetty.Handlers.Timeout;
using System.Net;
using SanTint.MessageCenterCore.NettyCommon;
using System.Text;

namespace SanTint.MessageCenterCore.NettyServer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        /// <summary>
        /// 控制服务关闭
        /// </summary>
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            //RabbitMQProxy.RabbitMQClientConfiguration rabbitMQClientConfiguration = new RabbitMQProxy.RabbitMQClientConfiguration();
            //rabbitMQClientConfiguration.Hostnames = new List<string>() { "127.0.0.1" };
            //rabbitMQClientConfiguration.QueueName = "SanTint.DosingExpert.NotificationQueue";
            //rabbitMQClientConfiguration.ExchangeName = "SanTint.DosingExpert.NotificationExchange";
            //rabbitMQClientConfiguration.RouteKey = rabbitMQClientConfiguration.QueueName;
            //rabbitMQClientConfiguration.UserName = "newadmin";
            //rabbitMQClientConfiguration.Password = "newpassword";


            //RabbitMQProxy.RabbitMQClient rabbitMQClient = new RabbitMQProxy.RabbitMQClient(rabbitMQClientConfiguration);
            //rabbitMQClient.Consume((obj, e) =>
            //{
            //    System.Diagnostics.Debug.WriteLine(Encoding.UTF8.GetString(e.Body.ToArray()));
            //});
            //rabbitMQClient.Publish("abc");

            await NettyServerHelper.Instance.RunServerAsync();


        }

    }
}