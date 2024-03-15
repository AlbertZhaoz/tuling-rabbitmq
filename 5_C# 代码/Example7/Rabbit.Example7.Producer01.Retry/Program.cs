using System.Collections.Immutable;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.Retry;
using Rabbit.Common.Data.Signals;
using Rabbit.Common.Display;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

Console.WriteLine("EXAMPLE 7 : 生产者可靠性保证-重试机制 : PRODUCER");


var retryCount = 3;
var policy = RetryPolicy.Handle<SocketException>()
    // 当 ConnectionFactory 期间无法打开连接时抛出。CreateConnection 尝试。
    .Or<BrokerUnreachableException>()
    .WaitAndRetry(retryCount,
        retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
        {
            Console.WriteLine("RabbitMQ Client could not connect after {time.TotalSeconds:n1}s ({ex.Message})");
        });

var connectionFactory = new ConnectionFactory
{
    HostName = "139.196.16.210",
    VirtualHost = "fornet",
    Port = 5672,
    UserName = "root",
    Password = "root",
    // 默认是 true，自动连接恢复
    // 这种自动连接是在 ConnectionFactory.CreateConnection() 连接创建成功后才生效，
    // 并不能在 RabbitMQ 一开始宕机就进行重试连接，
    // 如果想要一开始就重试连接，我们直接使用 Polly 重试
    AutomaticRecoveryEnabled = true, 
};

IConnection connection = null;

policy.Execute(() =>
{
    connection  = connectionFactory
        .CreateConnection();
});

if (connection == null && !connection.IsOpen)
{
    return;
}

using var channel = connection?.CreateModel();

const string ExchangeName = "";

const string QueueName = "example7_signals_queue";

var queue = channel.QueueDeclare(
    queue: QueueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

while (true)
{
    try
    {
        var textJson = JsonSerializer.Serialize("hello");
    
        channel.BasicPublish(
            exchange: ExchangeName, // 如果为空则使用默认交换器
            routingKey: QueueName, // 推送到哪个队列中
            body: Encoding.UTF8.GetBytes(textJson)
        );

        Console.WriteLine("发送成功");
    }
    catch (Exception e)
    {
        Console.WriteLine("发生异常正在重试");
    }
    
    await Task.Delay(3000);
}