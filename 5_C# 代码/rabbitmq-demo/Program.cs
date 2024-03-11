using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

Publisher();
Consumer();

void Publisher()
{
    var connectionFactory = new ConnectionFactory()
    {
        HostName = "139.196.16.210",
        Port = 5672,
        VirtualHost = "forjava",
        UserName = "root",
        Password = "root"
    };

    using var connection = connectionFactory.CreateConnection();
    using var channel = connection.CreateModel();
    channel.QueueDeclare(queue: "simple.queue",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    const string message = "Hello World!";
    var body = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(exchange: "simple.exchange",
        routingKey: string.Empty,
        basicProperties: null,
        body: body);
    Console.WriteLine($" [x] Sent {message}");

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}

void Consumer()
{
    var factory = new ConnectionFactory
    {
        HostName = "139.196.16.210",
        Port = 5672,
        VirtualHost = "forjava",
        UserName = "root",
        Password = "root"
    };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    channel.QueueDeclare(queue: "simple.queue",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    Console.WriteLine(" [*] Waiting for messages.");

    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($" [x] Received {message}");
    };
    channel.BasicConsume(queue: "simple.queue",
        autoAck: true,
        consumer: consumer);

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}

