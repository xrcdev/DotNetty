using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SanTint.MessageCenterCore.RabbitMQProxy
{
    internal class RabbitMQChannelPool
    {
        private readonly SemaphoreSlim[] _modelLocks = new SemaphoreSlim[MaxChannelCount];
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        // endpoint members
        private static readonly int MaxChannelCount = 8;
        private int _currentModelIndex = -1;
        private readonly IConnectionFactory _connectionFactory;
        private volatile IConnection _connection;
        private readonly IModel[] _models = new IModel[MaxChannelCount];
        private readonly IBasicProperties[] _properties = new IBasicProperties[MaxChannelCount];
        RabbitMQClientConfiguration _rabbitMQClientConfiguration;
        public RabbitMQChannelPool(RabbitMQClientConfiguration configuration)
        {
            // RabbitMQ channels are not thread-safe.
            // https://www.rabbitmq.com/dotnet-api-guide.html#model-sharing
            _rabbitMQClientConfiguration = configuration;
            for (var i = 0; i < MaxChannelCount; i++)
            {
                _modelLocks[i] = new SemaphoreSlim(1, 1);
            }
            // initialize
            _connectionFactory = GetConnectionFactory(configuration);
        }
        /// <summary>
        /// Configures a new ConnectionFactory, and returns it
        /// </summary>
        /// <returns></returns>
        private IConnectionFactory GetConnectionFactory(RabbitMQClientConfiguration config)
        {
            // prepare connection factory
            var connectionFactory = new ConnectionFactory
            {
                UserName = config.UserName,
                Password = config.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(2),
                UseBackgroundThreadsForIO = true
            };

            if (config.SslOption != null)
            {
                connectionFactory.Ssl.Version = config.SslOption.Version;
                connectionFactory.Ssl.CertPath = config.SslOption.CertPath;
                connectionFactory.Ssl.ServerName = config.SslOption.ServerName;
                connectionFactory.Ssl.Enabled = config.SslOption.Enabled;
                connectionFactory.Ssl.AcceptablePolicyErrors = config.SslOption.AcceptablePolicyErrors;
            }

            // only set, if has value, otherwise leave default
            if (config.Port > 0) connectionFactory.Port = config.Port;
            if (!string.IsNullOrEmpty(config.VHost)) connectionFactory.VirtualHost = config.VHost;
            // return factory
            return connectionFactory;
        }


        internal IConnection GetConnection(CancellationToken cancellationToken)
        {
            if (_connection == null)
            {
                _connectionLock.Wait(cancellationToken);
                try
                {
                    if (_connection == null)
                    {
                        _connection = _connectionFactory.CreateConnection(_rabbitMQClientConfiguration.Hostnames);
                    }
                }
                finally
                {
                    _connectionLock.Release();
                }
            }

            return _connection;
        }

        internal (int channelNumber, SemaphoreSlim semaphoreSlim) GetChannelLock()
        {
            var currentModelIndex = Interlocked.Increment(ref _currentModelIndex);

            // Interlocked.Increment can overflow and return a negative currentModelIndex.
            // Ensure that currentModelIndex is always in the range of [0, MaxChannelCount) by using this formula.
            // https://stackoverflow.com/a/14997413/263003
            currentModelIndex = (currentModelIndex % MaxChannelCount + MaxChannelCount) % MaxChannelCount;
            var modelLock = _modelLocks[currentModelIndex];
            return (currentModelIndex, modelLock);
        }
        private static void SendMessage(string message, string exchangeName, string exchangeType, string routeKey, (IModel channel, IBasicProperties properties) re)
        {
            // push message to exchange
            PublicationAddress publicationAddress = new PublicationAddress(exchangeType, exchangeName, routeKey);
            re.channel.BasicPublish(publicationAddress, re.properties, System.Text.Encoding.UTF8.GetBytes(message));
        }
        internal void GetOrCreateChannel(CancellationToken cancellationToken, Action action)
        {

        }

        internal void Close(IList<Exception> exceptions)
        {
            // Disposing channel and connection objects is not enough, they must be explicitly closed with the API methods.
            // https://www.rabbitmq.com/dotnet-api-guide.html#disconnecting
            for (var i = 0; i < _models.Length; i++)
            {
                try
                {
                    _modelLocks[i].Wait(10);
                    _models[i]?.Close();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            try
            {
                _connectionLock.Wait(10);
                _connection?.Close();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        internal void Dispose()
        {
            _connectionLock.Dispose();
            foreach (var modelLock in _modelLocks)
            {
                modelLock.Dispose();
            }

            foreach (var model in _models)
            {
                model?.Dispose();
            }

            _connection?.Dispose();
        }

        internal void SendMessage(CancellationToken closeToken, string message, string queueName, string exchangeName, string exchangeType, string routeKey)
        {
            var currentModelIndex = Interlocked.Increment(ref _currentModelIndex);

            // Interlocked.Increment can overflow and return a negative currentModelIndex.
            // Ensure that currentModelIndex is always in the range of [0, MaxChannelCount) by using this formula.
            // https://stackoverflow.com/a/14997413/263003
            currentModelIndex = (currentModelIndex % MaxChannelCount + MaxChannelCount) % MaxChannelCount;
            var modelLock = _modelLocks[currentModelIndex];
            modelLock.Wait(closeToken);
            try
            {
                var model = _models[currentModelIndex];
                var properties = _properties[currentModelIndex];

                if (model == null)
                {
                    var connection = GetConnection(closeToken);
                    model = connection.CreateModel();

                    _models[currentModelIndex] = model;

                    properties = model.CreateBasicProperties();
                    properties.DeliveryMode = (byte)_rabbitMQClientConfiguration.DeliveryMode; // persistence
                    _properties[currentModelIndex] = properties;
                }

                // push message to exchange
                PublicationAddress publicationAddress = new PublicationAddress(exchangeType, exchangeName, routeKey);
                model.BasicPublish(publicationAddress, properties, System.Text.Encoding.UTF8.GetBytes(message));
            }
            finally
            {
                modelLock.Release();
            }


        }
    }
}
