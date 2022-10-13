using RabbitMQ.Client;

namespace SanTint.Message.MessageCenter.Core.RabbitMQProxy
{
    /// <summary>
    /// RabbitMqClient - this class is the engine that lets you send/read messages to/from RabbitMq
    /// </summary>
    public class RabbitMQClient : IDisposable
    {
        // synchronization locks
        private readonly CancellationTokenSource _closeTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _closeToken;
        private const int MaxChannelCount = 8;
        private readonly SemaphoreSlim[] _modelLocks = new SemaphoreSlim[MaxChannelCount];
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private int _currentModelIndex = -1;
        // configuration member
        private readonly RabbitMQClientConfiguration _config;
        private readonly PublicationAddress _publicationAddress;

        // endpoint members
        private readonly IConnectionFactory _connectionFactory;
        private volatile IConnection _connection;
        private readonly IModel[] _models = new IModel[MaxChannelCount];
        private readonly IBasicProperties[] _properties = new IBasicProperties[MaxChannelCount];

        /// <summary>
        /// Constructor for RabbitMqClient
        /// </summary>
        /// <param name="configuration">mandatory</param>
        public RabbitMQClient(RabbitMQClientConfiguration configuration)
        {
            _closeToken = _closeTokenSource.Token;

            // RabbitMQ channels are not thread-safe.
            // https://www.rabbitmq.com/dotnet-api-guide.html#model-sharing
            // Create a pool of channels and give each call to Publish one channel.
            for (var i = 0; i < MaxChannelCount; i++)
            {
                _modelLocks[i] = new SemaphoreSlim(1, 1);
            }

            // load configuration
            _config = configuration;
            _publicationAddress = new PublicationAddress(_config.ExchangeType, _config.ExchangeName, _config.RouteKey);

            // initialize
            _connectionFactory = GetConnectionFactory();
        }

        /// <summary>
        /// Configures a new ConnectionFactory, and returns it
        /// </summary>
        /// <returns></returns>
        private IConnectionFactory GetConnectionFactory()
        {
            // prepare connection factory
            var connectionFactory = new ConnectionFactory
            {
                UserName = _config.UserName,
                Password = _config.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(2),
                UseBackgroundThreadsForIO = true
            };

            if (_config.SslOption != null)
            {
                connectionFactory.Ssl.Version = _config.SslOption.Version;
                connectionFactory.Ssl.CertPath = _config.SslOption.CertPath;
                connectionFactory.Ssl.ServerName = _config.SslOption.ServerName;
                connectionFactory.Ssl.Enabled = _config.SslOption.Enabled;
                connectionFactory.Ssl.AcceptablePolicyErrors = _config.SslOption.AcceptablePolicyErrors;
            }

            // only set, if has value, otherwise leave default
            if (_config.Port > 0) connectionFactory.Port = _config.Port;
            if (!string.IsNullOrEmpty(_config.VHost)) connectionFactory.VirtualHost = _config.VHost;

            // return factory
            return connectionFactory;
        }

        /// <summary>
        /// Publishes a message to RabbitMq ExchangeName
        /// </summary>
        /// <param name="message"></param>
        public async Task PublishAsync(string message)
        {
            var currentModelIndex = Interlocked.Increment(ref _currentModelIndex);

            // Interlocked.Increment can overflow and return a negative currentModelIndex.
            // Ensure that currentModelIndex is always in the range of [0, MaxChannelCount) by using this formula.
            // https://stackoverflow.com/a/14997413/263003
            currentModelIndex = (currentModelIndex % MaxChannelCount + MaxChannelCount) % MaxChannelCount;
            var modelLock = _modelLocks[currentModelIndex];
            await modelLock.WaitAsync(_closeToken);
            try
            {
                var model = _models[currentModelIndex];
                var properties = _properties[currentModelIndex];
                if (model == null)
                {
                    var connection = await GetConnectionAsync();
                    model = connection.CreateModel();
                    _models[currentModelIndex] = model;

                    properties = model.CreateBasicProperties();
                    properties.DeliveryMode = (byte)_config.DeliveryMode; // persistence
                    _properties[currentModelIndex] = properties;
                }

                // push message to exchange
                model.BasicPublish(_publicationAddress, properties, System.Text.Encoding.UTF8.GetBytes(message));
            }
            finally
            {
                modelLock.Release();
            }
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

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _closeTokenSource.Dispose();

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

        private async Task<IConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                await _connectionLock.WaitAsync(_closeToken);
                try
                {
                    if (_connection == null)
                    {
                        _connection = _config.Hostnames.Count == 0
                            ? _connectionFactory.CreateConnection()
                            : _connectionFactory.CreateConnection(_config.Hostnames);
                    }
                }
                finally
                {
                    _connectionLock.Release();
                }
            }

            return _connection;
        }
    }
}

