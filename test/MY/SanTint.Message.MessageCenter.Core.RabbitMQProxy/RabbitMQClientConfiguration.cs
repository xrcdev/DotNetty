using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanTint.Message.MessageCenter.Core.RabbitMQProxy
{
    /// <summary>
    /// Configuration class for RabbitMqClient
    /// </summary>
    public class RabbitMQClientConfiguration
    {
        public IList<string> Hostnames { get; } = new List<string>();
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
        /// <summary>
        ///  "direct" "fanout" "headers" "topic";
        /// </summary>
        public string ExchangeType { get; set; } = "direct";
        public string RouteKey { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;

        public RabbitMQDeliveryMode DeliveryMode { get; set; } = RabbitMQDeliveryMode.Durable;
       
        public int Port { get; set; }
        public string VHost { get; set; } = string.Empty;
      
        public SslOption SslOption { get; set; }

        public RabbitMQClientConfiguration From(RabbitMQClientConfiguration config)
        {
            UserName = config.UserName;
            Password = config.Password;
            ExchangeName = config.ExchangeName;
            ExchangeType = config.ExchangeType;
            DeliveryMode = config.DeliveryMode;
            RouteKey = config.RouteKey;
            Port = config.Port;
            VHost = config.VHost;
            SslOption = config.SslOption;

            foreach (string hostName in config.Hostnames)
            {
                Hostnames.Add(hostName);
            }

            return this;
        }
    }


    public enum RabbitMQDeliveryMode : byte
    {
        NonDurable = 1,
        Durable = 2
    }
}
 
