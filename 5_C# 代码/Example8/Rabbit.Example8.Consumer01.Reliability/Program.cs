using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

Console.WriteLine("EXAMPLE 8 : 消费者可靠性保证-手动 ack- PRODUCER");

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
const string QueueName = "example8_signals_queue";

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    try
    {
        Console.WriteLine($"Received: {message}");
        // 模拟消息处理
        Thread.Sleep(1000); // 假设这里是消息处理的代码

        // 如果消息处理成功，则手动发送ACK
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        Console.WriteLine("Message acknowledged");
    }
    catch (Exception ex)
    {
        //● ack：成功处理消息，RabbitMQ 从队列中删除该消息
        //● nack：消息处理失败，RabbitMQ 需要再次投递消息
        //● reject：消息处理失败并拒绝该消息，RabbitMQ 从队列中删除该消息
        
        // 处理消息失败的情况，不发送ACK，让消息重新入队
        Console.WriteLine($"Error processing message: {ex.Message}");
        // BasicNack 提供了比 BasicReject 更大的灵活性和批量处理能力。如果你需要拒绝单个消息，两者都可以使用，但如果你需要批量拒绝消息，那么 BasicNack 是更合适的选择。
        // 拒绝消息再次投递，不重新入队
        channel.BasicReject(ea.DeliveryTag, false);
        // 返回 nack，不重新入队
        channel.BasicNack(ea.DeliveryTag,false,false);
    }
};

// 开始消费消息，并设置autoAck参数为false开启手动确认
channel.BasicConsume(queue: QueueName,
    autoAck: false,
    consumer: consumer);

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();

static void ConsumerAckAndTryCount()
{
    var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            var dlxExchange = "dlx_exchange";
            var dlxQueue = "dlx_queue";
            var queueName = "lazy_queue";
            var retryLimit = 3;

            // 声明死信交换和队列
            channel.ExchangeDeclare(dlxExchange, "direct");
            channel.QueueDeclare(dlxQueue, true, false, false, null);
            channel.QueueBind(dlxQueue, dlxExchange, "");

            var arguments = new Dictionary<string, object>()
            {
                {"x-dead-letter-exchange", dlxExchange}
            };

            // 声明主队列
            channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: arguments);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var retryCount = 0;

                // 检查消息头部以获取重试次数
                if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-retry-count"))
                {
                    retryCount = (int)ea.BasicProperties.Headers["x-retry-count"];
                }

                try
                {
                    Console.WriteLine($"Received: {message}");
                    // 模拟消息处理
                    Thread.Sleep(1000); // 假设这里是消息处理的代码

                    // 成功处理后确认消息
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    Console.WriteLine("Message acknowledged");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");

                    if (retryCount >= retryLimit)
                    {
                        // 超过重试次数，确认消息以避免重复投递
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        Console.WriteLine("Message discarded after maximum retries");
                    }
                    else
                    {
                        // 重新投递消息，并增加重试次数
                        var properties = channel.CreateBasicProperties();
                        properties.Headers = new Dictionary<string, object> {{"x-retry-count", retryCount + 1}};
                        channel.BasicPublish("", queueName, properties, body);
                        // 拒绝消息并不重新入队
                        channel.BasicReject(ea.DeliveryTag, false);
                    }
                }
            };

            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }
}