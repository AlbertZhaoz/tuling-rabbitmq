# Delay Message

## Overview

![死信队列](https://cdn.jsdelivr.net/gh/AlbertZhaoz/blogpic@master/2024/死信队列.5cyge7lq2d00.webp)
![DelayExchange-插件](https://cdn.jsdelivr.net/gh/AlbertZhaoz/blogpic@master/2024/DelayExchange-插件.2jl6ki2qw6u0.webp)
---

## Dead Letter Exchange

```C#
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
```
## DelayExchange Plugin
1. Plugin download address: GitHub-rabbitmq /rabbitmq-delayed-message-exchange: Delayed Messaging for RabbitMQ. Since the MQ we installed is version 3.8, download version 3.8.17 here. If you are a rabbitmq 3.7 user, you can use the 3.8 plugin directly.
2. `docker exec -it rabbitmq rabbitmq-plugins enable rabbitmq_delayed_message_exchange`
3. Publisher Code
```
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
```
4. Consumer Code
```
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
```