# Publisher Confirms

## Overview

[Publisher confirms](https://www.rabbitmq.com/docs/confirms#publisher-confirms) are a RabbitMQ extension to implement reliable publishing. When publisher confirms are enabled on a channel, messages the client publishes are confirmed asynchronously by the broker, meaning they have been taken care of on the server side.

![Snipaste_2024-03-14_16-20-39](https://cdn.jsdelivr.net/gh/AlbertZhaoz/blogpic@master/2024/Snipaste_2024-03-14_16-20-39.4uh4tu4emqa0.webp)

---

## Enabling Publisher Confirms on a Channel

Publisher confirms are a RabbitMQ extension to the AMQP 0.9.1 protocol, so they are not enabled by default. Publisher confirms are enabled at the channel level with the `ConfirmSelect` method:

```C#
var channel = connection.CreateModel();
channel.ConfirmSelect();
```

This method must be called on every channel that you expect to use publisher confirms. Confirms should be enabled just once, not for every message published.

---

## Publishing Messages Individually

Publishing a message and waiting synchronously for its confirmation.

```
while (true)
{
    byte[] body = ...;
    IBasicProperties properties = ...;
    channel.BasicPublish(exchange, queue, properties, body);
    // uses a 5 second timeout
    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
    Task.Delay(1000);
}
```

This technique is very straightforward but also has a major drawback: it  **significantly slows down publishing** , as the confirmation of a message blocks the publishing of all subsequent messages. This approach is not going to deliver throughput of more than a few hundreds of published messages per second. Nevertheless, this can be good enough for some applications.

---

## Publishing Messages in Batches

To improve upon our previous example, we can publish a batch of messages and wait for this whole batch to be confirmed. The following example uses a batch of 100:

```
var batchSize = 100;
var outstandingMessageCount = 0;
while (true)
{
    byte[] body = ...;
    IBasicProperties properties = ...;
    channel.BasicPublish(exchange, queue, properties, body);
    outstandingMessageCount++;

    if (outstandingMessageCount == batchSize)
    {
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        outstandingMessageCount = 0;
    }

    Task.Delay(1000);
}
```

## Handling Publisher Confirms Asynchronously

The broker confirms published messages asynchronously, one just needs to register a callback on the client to be notified of these confirms:

```

// 1.2 消息确认机制开启
channel.ConfirmSelect();

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
};

// 1.3.3 队列不存在
channel.BasicReturn += (sender, eventArgs) =>
{
    Console.WriteLine("队列不存在");
};

```

There are 2 callbacks: one for confirmed messages and one for nack-ed messages (messages that can be considered lost by the broker). Both callbacks have a corresponding `EventArgs` parameter (`ea`) containing a:

* delivery tag: the sequence number identifying the confirmed or nack-ed message. We will see shortly how to correlate it with the published message.
* multiple: this is a boolean value. If false, only one message is confirmed/nack-ed, if true, all messages with a lower or equal sequence number are confirmed/nack-ed.

The sequence number can be obtained with `Channel#NextPublishSeqNo` before publishing:

```
var sequenceNumber = channel.NextPublishSeqNo;
channel.BasicPublish(exchange, queue, properties, body);
```
