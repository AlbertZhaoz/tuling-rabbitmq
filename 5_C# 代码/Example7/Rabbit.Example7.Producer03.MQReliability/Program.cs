using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

Console.WriteLine("EXAMPLE 7 : MQ 可靠性保证- 惰性队列：x-queue-mode:lazy 消息持久化：basicProperties.Persistent : PRODUCER");

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

using var connection  = connectionFactory
    .CreateConnection();
    
using var channel = connection?.CreateModel();

// 1.1 声明队列
const string ExchangeName = "";
const string QueueName = "example7_signals_queue";

// 是否实现 LazyQueue 惰性队列,"x-queue-mode","lazy"
// 1.接收到消息后直接存入磁盘而非内存（内存中只保留最近的消息，默认 2048 条）
// 2.消费者要消费消息时才会从磁盘中读取并加载到内存（也就是懒加载）
// 3.支持数百万条的消息存储
var dictionary = new Dictionary<string,object>();

if (true)
{
    dictionary.Add("x-queue-mode","lazy");
}

var queue = channel.QueueDeclare(
    queue: QueueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: dictionary);
    
var textJson = JsonSerializer.Serialize("hello");

// 创建消息属性-消息持久化
var basicProperties = channel.CreateBasicProperties();
// 默认是持久化消息，如果不是持久化则会出现大量 PagedOut
// 因为如果持续在内存中则会阻塞，持久化就会批量落盘，很少 PagedOut
basicProperties.Persistent = true;

for (int i = 0; i < 1000000; i++)
{
    channel.BasicPublish(
        exchange: ExchangeName, // 如果为空则使用默认交换器
        routingKey: QueueName, // 推送到哪个队列中
        basicProperties:basicProperties,
        body: Encoding.UTF8.GetBytes(textJson)
    );
}