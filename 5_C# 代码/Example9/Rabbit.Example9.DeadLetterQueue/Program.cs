using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

var factory = new ConnectionFactory()
{
    HostName = "139.196.16.210",
    VirtualHost = "fornet",
    Port = 5672,
    UserName = "root",
    Password = "root",
};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

var commonQueue = "common.queue";
var dlxExchange = "dlx.exchange";
var dlxQueue = "dlx.queue";

// 声明死信交换机
channel.ExchangeDeclare(dlxExchange, ExchangeType.Fanout, true, false, null);
// 声明队列
channel.QueueDeclare(dlxQueue, true, false, false, null);
// 绑定
channel.QueueBind(dlxQueue, dlxExchange, "");
// 声明 dead-letter-exchange 指向 dlxExchange
var arguments = new Dictionary<string, object>()
{
    { "x-dead-letter-exchange", dlxExchange }
};

// 声明主队列
channel.QueueDeclare(queue: commonQueue,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: arguments);

// 声明过期属性
var messageProperties = channel.CreateBasicProperties();
messageProperties.Expiration = "1000"; // ms

// 发布消息
channel.BasicPublish(
    exchange: "",
    routingKey: commonQueue,
    basicProperties:messageProperties,
    body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize("dead-letter-message"))
);