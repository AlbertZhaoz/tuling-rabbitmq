using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Collections.Immutable;
using System.Text;
using Rabbit.Common.Data.Signals;
using Rabbit.Common.Display;
using System;
using System.Drawing;

namespace Rabbit.Example2.Producer
{
    internal sealed class Program
    {
        private static void Main()
        {
            Console.WriteLine("\nEXAMPLE 1 : WORK QUEUE : PRODUCER");

            var connectionFactory = new ConnectionFactory
            {
                HostName = "139.196.16.210",
                VirtualHost = "fornet",
                Port = 5672,
                UserName = "root",
                Password = "root"
            };

            using var connection = connectionFactory.CreateConnection();

            using var channel = connection.CreateModel();

            const string ExchangeName = "";

            const string QueueName = "example2_signals_queue";

            var queue = channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: ImmutableDictionary<string, object>.Empty);
            

            for (int j = 0; j < 100; j++)
            {
                var signal = Transmitter.Fake(j.ToString()).Transmit();

                // BasicPublish 有三个重载，其中 IBasicProperties 封装了很多属性
                // Expiration:消息过期时间
                // Persistent:是否持久化
                // Correlated:生产者确认机制，异步回调
                // var properties = channel.CreateBasicProperties();
                // properties.Persistent = true;
                // properties.ContentType = "application/json";
                // properties.ContentEncoding = "UTF-8";
                channel.BasicPublish(
                    exchange: ExchangeName, // 如果为空则使用默认交换器
                    routingKey: QueueName, // 推送到哪个队列中
                    body: Encoding.UTF8.GetBytes(signal.ToJson())
                );

                DisplayInfo<Signal>
                    .For(signal)
                    .SetExchange(ExchangeName)
                    .SetQueue(QueueName)
                    .SetRoutingKey(QueueName)
                    .SetVirtualHost(connectionFactory.VirtualHost)
                    .Display(Color.Blue);

                // await Task.Delay(millisecondsDelay: 500);
            }
        }
    }
}
