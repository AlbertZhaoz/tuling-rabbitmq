using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.CSharp.RuntimeBinder;
using RabbitMQ.Client;

Console.WriteLine("EXAMPLE 7 : 生产者可靠性保证-异步发送消息确认 : PRODUCER");

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
var queue = channel.QueueDeclare(
    queue: QueueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

// 1.2 消息确认机制开启
channel.ConfirmSelect();

// TestWaitMessageConfirm(channel);
TestBasicAcks(channel);

// 同步确认
static void TestWaitMessageConfirm(IModel channel)
{
    while (true)
    {
        var textJson = JsonSerializer.Serialize("hello");
    
        channel.BasicPublish(
            exchange: ExchangeName, // 如果为空则使用默认交换器
            routingKey: QueueName, // 推送到哪个队列中
            body: Encoding.UTF8.GetBytes(textJson)
        );
            
        // 同步等待，及时没有消息确认如果超时 5s 也会返回
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        
        Task.Delay(1000);
    }
}

// 批量确认
static void TestWaitMessageConfirmBatch(IModel channel)
{
    var batchSize = 100;
    var outstandingMessageCount = 0;
    
    while (true)
    {
        var textJson = JsonSerializer.Serialize("hello");
        
        channel.BasicPublish(
            exchange: ExchangeName, // 如果为空则使用默认交换器
            routingKey: QueueName, // 推送到哪个队列中
            body: Encoding.UTF8.GetBytes(textJson)
        );
            
        outstandingMessageCount++;
        
        if (outstandingMessageCount == batchSize)
        {
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            outstandingMessageCount = 0;
        }
        
        Task.Delay(1000);
    }
}

/// 基础确认
static void TestBasicAcks(IModel channel)
{
    try
    {
        // 1.3 异步发送消息回调
        // 1.3.1 消息正常投递 ack
        channel.BasicAcks += (sender, eventArgs) =>
        {
            Console.WriteLine($"消息正常投递{eventArgs.DeliveryTag}");
        };

        // 1.3.2 抛出业务异常,消息重试投递 noack
        channel.BasicNacks += (sender, eventArgs) =>
        {
            Console.WriteLine("抛出业务异常,消息重试投递");
            
            // It can be tempting to re-publish a nack-ed message from the corresponding callback but this should be avoided, as confirm callbacks are dispatched in an I/O thread where channels are not supposed to do operations. A better solution consists in enqueuing the message in an in-memory queue which is polled by a publishing thread. A class like ConcurrentQueue would be a good candidate to transmit messages between the confirm callbacks and a publishing thread.
            // 从相应的回调中重新发布 nack-ed 消息可能很诱人，但应该避免这种情况，因为确认回调是在通道不应该执行操作的 I/O 线程中调度的。更好的解决方案是将消息放入内存队列中，由发布线程轮询。像 ConcurrentQueue 这样的类是在确认回调和发布线程之间传输消息的良好候选者。
        };

        // 1.3.3 队列不存在
        channel.BasicReturn += (sender, eventArgs) =>
        {
            Console.WriteLine("队列不存在");
        };
        
        
        Console.WriteLine($"消息即将被投递{channel.NextPublishSeqNo}");
        
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
}

/// 最终未完成的确认
static void TestBasicNacks(IModel channel)
{
    try
    {
        var outstandingConfirms = new ConcurrentDictionary<ulong, string>();

        void CleanOutstandingConfirms(ulong sequenceNumber, bool multiple)
        {
            if (multiple)
            {
                var confirmed = outstandingConfirms.Where(k => k.Key <= sequenceNumber);
                foreach (var entry in confirmed)
                {
                    outstandingConfirms.TryRemove(entry.Key, out _);
                }
            }
            else
            {
                outstandingConfirms.TryRemove(sequenceNumber, out _);
            }
        }

        channel.BasicAcks += (sender, ea) => CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
        channel.BasicNacks += (sender, ea) =>
        {
            outstandingConfirms.TryGetValue(ea.DeliveryTag, out string body);
            Console.WriteLine($"Message with body {body} has been nack-ed. Sequence number: {ea.DeliveryTag}, multiple: {ea.Multiple}");
            CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
        };
    }
    catch (Exception e)
    {
        Console.WriteLine("业务异常");
    }
}

static void TestBasicReturn(IModel channel)
{
    try
    {
        var textJson = JsonSerializer.Serialize("hello");
    
        channel.BasicPublish(
            exchange: ExchangeName, // 如果为空则使用默认交换器
            routingKey: QueueName, // 推送到哪个队列中
            body: Encoding.UTF8.GetBytes(textJson)
        );

        throw new RuntimeBinderException();
    }
    catch (Exception e)
    {
        Console.WriteLine("业务异常");
    }
}

