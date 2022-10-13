using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using DotNetty.Handlers.Timeout;
using System.Net;
using SanTint.Message.MessageCenter.Core.NettyCommon;

namespace SanTint.Message.MessageCenter.Core.NettyServer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        /// <summary>
        /// ���Ʒ���ر�
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
            await ServerHelper.Instance.RunServerAsync();

        }

    }
}