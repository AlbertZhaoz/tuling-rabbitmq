using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

var queueName = "delay.queue";
            
channel.QueueDeclare(queueName, true, false, false, null);

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (sender, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    
    Console.WriteLine($"Received:{DateTime.Now} {message}");
    channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
    Console.WriteLine("Message acknowledged");
};

channel.BasicConsume(
    queue: queueName,
    autoAck: false,
    consumer: consumer);

Console.ReadLine();