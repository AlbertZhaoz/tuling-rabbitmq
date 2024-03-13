using Pastel;
using Rabbit.Common.Data.Signals;
using Rabbit.Common.Display;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace Rabbit.Example2.Consumer
{
    internal sealed class Program
    {
        private static void Main()
        {
            Console.WriteLine("\nEXAMPLE 2 : WORK QUEUE : CONSUMER");

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

            // 【能者多劳模式】
            // prefetchSize:表示消费者所能接收未确认消息的总体大小的上限，设置为 0 则表示没有上限。
            // prefetchCount:设置消费者客户端最大能接收的未确认的消息数,能者多劳配置手册，防止一直轮训
            channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);
            
            var queue = channel.QueueDeclare(
                queue: "example2_signals_queue",
                durable: true,
                exclusive: false, // 设置是否排他。如果一个队列被声明为排他队列，该队列仅对首次声明它的连接可见，并在连接断开时自动删除。
                autoDelete: false,
                arguments: ImmutableDictionary<string, object>.Empty);
            
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender, eventArgs) =>
            {
                var messageBody = eventArgs.Body.ToArray();
                var signal = Signal.FromBytes(messageBody);

                DisplayInfo<Signal>
                    .For(signal)
                    .SetExchange(eventArgs.Exchange)
                    .SetQueue(queue)
                    .SetRoutingKey(eventArgs.RoutingKey)
                    .SetVirtualHost(connectionFactory.VirtualHost)
                    .Display(Color.Red);

                DecodeSignal(signal);

                // 如果将 multiple 设为 false，则只确认指定 deliveryTag 的一条消息。
                // 如果将 multiple 设为 true，则会确认所有比指定 deliveryTag 小的并且未被确认的消息。
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            };
            
            channel.BasicConsume(
                queue: queue.QueueName,
                autoAck: false,
                consumer: consumer);
            Console.ReadLine();
        }

        private static void DecodeSignal(Signal signal)
        {
            Console.WriteLine($"\nDECODE STARTED: [ TX: {signal.TransmitterName}, ENCODED DATA: {signal.Data} ]".Pastel(Color.Lime));

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var decodedData = Receiver.DecodeSignal(signal);

            stopwatch.Stop();

            Console.WriteLine($@"DECODE COMPLETE: [ TIME: {stopwatch.Elapsed.Seconds} sec, TX: {signal.TransmitterName}, DECODED DATA: {decodedData} ]".Pastel(Color.Lime));
        }
    }
}
