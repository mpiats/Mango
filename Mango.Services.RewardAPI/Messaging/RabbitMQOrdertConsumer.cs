using Mango.Services.EmailAPI.Services;
using Mango.Services.RewardAPI.Message;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.RewardAPI.Messaging
{
    public class RabbitMQOrdertConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly RewardService _rewardlService;
        private IConnection _connection;
        private IModel _channel;
        string queueName = "";

        private const string OrderCreated_RewardsUpdateQueue = "RewardsUpdateQueue"; // for direct exchange
        private string ExchangeName = "";

        public RabbitMQOrdertConsumer(IConfiguration configuration, RewardService rewardService)
        {
            _configuration = configuration;
            _rewardlService = rewardService;
            ExchangeName = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Password = "guest",
                UserName = "guest"
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct);

            _channel.QueueDeclare(OrderCreated_RewardsUpdateQueue, false, false, false, null);
            _channel.QueueBind(OrderCreated_RewardsUpdateQueue, ExchangeName, "RewardsUpdate");

            //for fanout
            //_configuration = configuration;
            //_rewardlService = rewardService;
            //ExchangeName = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");
            //var factory = new ConnectionFactory
            //{
            //    HostName = "localhost",
            //    Password = "guest",
            //    UserName = "guest"
            //};
            //_connection = factory.CreateConnection();
            //_channel = _connection.CreateModel();
            //_channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout);
            //queueName = _channel.QueueDeclare().QueueName;
            //_channel.QueueBind(queueName, _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic"), "");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                var rewardsMessage = JsonConvert.DeserializeObject<RewardsMessage>(content);
                HandleMessage(rewardsMessage).GetAwaiter().GetResult();

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(OrderCreated_RewardsUpdateQueue, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(RewardsMessage rewardsMessage)
        {
            _rewardlService.UpdateRewards(rewardsMessage).GetAwaiter().GetResult();
        }
    }
}
