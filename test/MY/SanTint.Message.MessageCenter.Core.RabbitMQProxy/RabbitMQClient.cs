using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reflection;
using System.Threading.Channels;

namespace SanTint.MessageCenterCore.RabbitMQProxy
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
        private readonly RabbitMQChannelPool _channelPool;
        /// <summary>
        /// 初始化 RabbitMqClient
        /// </summary>
        /// <param name="configuration">mandatory</param>
        public RabbitMQClient(RabbitMQClientConfiguration configuration)
        {
            _closeToken = _closeTokenSource.Token;
            // load configuration
            _config = configuration;
            _channelPool = new RabbitMQChannelPool(configuration);
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message">字符串消息</param>
        /// <param name="queueName">消息队列名称</param>
        /// <param name="exchangeName">消息队列使用的交换机名称</param>
        /// <param name="queueName">交换机类型</param>
        /// <param name="routeKey">路由键,不传则使用 $"{exchangeType}_{queueName}"</param>
        public void Publish(string message, string queueName, string exchangeName, string exchangeType = "direct", string routeKey = "")
        {
            routeKey = CheckParams(queueName, exchangeName, exchangeType, routeKey);
            _channelPool.SendMessage(_closeToken, message, queueName, exchangeName, exchangeType, routeKey);
            //re.channel.BasicReturn += Channel_BasicReturn;
            //re.channel.CallbackException += Channel_CallbackException;
        }

        /// <summary>
        /// 发布消息(泛型)
        /// </summary>
        /// <param name="message">泛型消息</param>
        /// <param name="queueName">消息队列名称</param>
        /// <param name="exchangeName">消息队列使用的交换机名称</param>
        /// <param name="queueName">交换机类型</param>
        /// <param name="routeKey">路由键,不传则使用 $"{exchangeType}_{queueName}"</param>
        public void Publish<T>(T message, string queueName, string exchangeName, string exchangeType = "direct", string routeKey = "")
        {
            if (message == null)
            {
                throw new ArgumentException($"the '{nameof(message)}' parameter , can't be null");
            }
            var strMessage = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            Publish(strMessage, queueName, exchangeName, exchangeType, routeKey);
        }


        /// <summary>
        ///  创建消费者
        /// </summary>
        ///   例子
        ///   var connect = Consumer( (model, ea) =>
        ///   {
        ///     Console.WriteLine(Encoding.UTF8.GetString(ea.Body.ToArray()));
        ///   },"queue1","exchange1");
        ///   connect.close();
        /// <param name="exec">消息处理事件</param>
        /// <param name="queueName">消息队列名称</param>
        /// <param name="exchangeName">消息队列使用的交换机名称</param>
        /// <param name="queueName">交换机类型</param>
        /// <param name="routeKey">路由键,不传则使用 $"{exchangeType}_{queueName}"</param>
        /// <returns>连接</returns>
        public IConnection Consume(Action<object, BasicDeliverEventArgs> exec, string queueName, string exchangeName, string exchangeType = "direct", string routeKey = "")
        {
            routeKey = CheckParams(queueName, exchangeName, exchangeType, routeKey);
            var connect = _channelPool.GetConnection(_closeToken);
            var channel = connect.CreateModel();

            channel.ExchangeDeclare(queueName, exchangeName, true, false, null);
            channel.QueueDeclare(queueName, true, false, false, null);
            channel.QueueBind(queueName, exchangeName, queueName, null);

            channel.BasicQos(0, 1, false);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                exec?.Invoke(model, ea);
            };
            channel.BasicConsume(queueName, false, consumer);
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


        private void Channel_CallbackException(object? sender, CallbackExceptionEventArgs e)
        {

        }

        private void Channel_BasicReturn(object? sender, BasicReturnEventArgs e)
        {

        }

        private static string CheckParams(string queueName, string exchangeName, string exchangeType, string routeKey)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException($"the '{nameof(queueName)}' parameter , can't be null or empty");
            }
            if (string.IsNullOrWhiteSpace(exchangeName))
            {
                throw new ArgumentException($"the '{nameof(exchangeName)}' parameter , can't be null or empty");
            }
            if (string.IsNullOrWhiteSpace(exchangeType))
            {
                throw new ArgumentException($"the '{nameof(exchangeType)}' parameter , can't be null or empty");
            }
            if (string.IsNullOrWhiteSpace(routeKey))
            {
                routeKey = $"{exchangeType}_{queueName}";
            }

            return routeKey;
        }

    }
}

