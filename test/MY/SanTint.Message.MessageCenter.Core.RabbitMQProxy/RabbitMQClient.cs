using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reflection;

namespace SanTint.Message.MessageCenter.Core.RabbitMQProxy
{
    /// <summary>
    /// RabbitMqClient - this class is the engine that lets you send/read messages to/from RabbitMq
    /// </summary>
    public class RabbitMQClient : IDisposable
    {
        // CancellationToken
        private readonly CancellationTokenSource _closeTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _closeToken;

        // configuration member
        private readonly RabbitMQClientConfiguration _config;
        private readonly PublicationAddress _publicationAddress;
        private readonly ChannelPool _channelPool;
        /// <summary>
        /// Constructor for RabbitMqClient
        /// </summary>
        /// <param name="configuration">mandatory</param>
        public RabbitMQClient(RabbitMQClientConfiguration configuration)
        {
            _closeToken = _closeTokenSource.Token;
            // load configuration
            _config = configuration;
            _channelPool = new ChannelPool(configuration);
            _publicationAddress = new PublicationAddress(_config.ExchangeType, _config.ExchangeName, _config.RouteKey);
        }

        /// <summary>
        /// Publishes a message to RabbitMq ExchangeName
        /// </summary>
        /// <param name="message"></param>
        public void Publish(string message)
        {
            var re = _channelPool.GetOrCreateChannel(_closeToken);
            // push message to exchange
            re.channel.BasicPublish(_publicationAddress, re.properties, System.Text.Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Publishes a message to RabbitMq ExchangeName
        /// </summary>
        /// <param name="message"></param>
        public void Publish<T>(T message)
        {
            var strMessage = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            Publish(strMessage);
        }
        /// <summary>
        ///  创建消费者
        ///   var connect = Consumer(  (model, ea) =>
        ///   {
        ///     Console.WriteLine(Encoding.UTF8.GetString(ea.Body.ToArray()));
        ///   });
        ///   block
        ///   connect.close();
        /// </summary>
        /// <param name="exec"></param>
        /// <returns></returns>
        public IConnection Consumer(Action<object, BasicDeliverEventArgs> exec)
        {
            var connect = _channelPool.GetConnection(_closeToken);
            var channel = connect.CreateModel();
            channel.ExchangeDeclare(_config.ExchangeName, _config.ExchangeType, true, false, null);
            channel.QueueDeclare(_config.QueueName, true, false, false, null);
            channel.QueueBind(_config.QueueName, _config.ExchangeName, _config.QueueName, null);

            channel.BasicQos(0, 1, false);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                exec?.Invoke(model, ea);
            };
            channel.BasicConsume(_config.QueueName, false, consumer);
            return connect;
        }
        public void Close()
        {
            IList<Exception> exceptions = new List<Exception>();
            try
            {
                _closeTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
            _channelPool.Close(exceptions);


            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _closeTokenSource.Dispose();
            _channelPool.Dispose();
        }
    }
}

