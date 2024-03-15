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

var delayExchange = "delay.exchange";
var delayQueue = "delay.queue";
var routingKey = "delay";

// 声明延迟交换机
// 声明 dead-letter-exchange 指向 dlxExchange
var arguments = new Dictionary<string, object>()
{
    { "x-delayed-type", "direct" } // ms
};

channel.ExchangeDeclare(delayExchange, "x-delayed-message", true, false, arguments);
// 声明队列
channel.QueueDeclare(delayQueue, true, false, false, null);
// 绑定
channel.QueueBind(delayQueue, delayExchange, routingKey);

// 发送带有延迟的消息
var messageProperties = channel.CreateBasicProperties();
messageProperties.Headers = new Dictionary<string, object>
{
    { "x-delay", 5000 } // 延迟5000毫秒（5秒）
};

// 发布消息
Console.WriteLine($"发布消息{DateTime.Now}");
channel.BasicPublish(
    exchange: delayExchange,
    routingKey: routingKey,
    basicProperties:messageProperties,
    body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize("delay-message"))
);