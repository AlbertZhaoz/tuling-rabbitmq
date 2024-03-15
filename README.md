# rabbitmq-started
RabbitMQ 从入门到高级，整个项目用两种代码实现，一种是 Java(基于 SpringBoot)，另一种是 C# 实现。整体功能如下：
1. 常规队列的使用
2. WorkQueue 模式实现
3. 四种交换机实现
- Fanout 
- Direct
- Topic
- Headers
- 消息转换器（Java 配置 Jackson）
4. 生产者可靠性实现
- MQ 重试（C# 使用了 Polly)
- 生产者重连重试
- 生产者确认机制 ConfirmCallback ReturnCallback
5. MQ 可靠性实现
- 数据持久化：交换机、队列、消息（发送端参数追加）
- LazyQueue 队列实现
6. 消费者可靠性实现
- 手动 ack：ack none,nack,reject
7. 业务幂等性实现
- 唯一消息 ID
- 业务层面实现
8. 死信队列实现（超时支付场景）
9. 延迟消息实现（超时支付场景）
