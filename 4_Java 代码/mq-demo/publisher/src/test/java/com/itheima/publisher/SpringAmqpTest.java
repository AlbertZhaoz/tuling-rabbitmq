package com.itheima.publisher;

import lombok.extern.slf4j.Slf4j;
import org.junit.jupiter.api.Test;
import org.springframework.amqp.AmqpException;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageBuilder;
import org.springframework.amqp.core.MessageDeliveryMode;
import org.springframework.amqp.core.MessagePostProcessor;
import org.springframework.amqp.rabbit.connection.CorrelationData;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.util.concurrent.ListenableFutureCallback;

import javax.print.Doc;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

@Slf4j
@SpringBootTest
public class SpringAmqpTest {

    @Autowired
    private RabbitTemplate rabbitTemplate;

    @Test
    void testSendMessage2Queue() {
        String queueName = "simple.queue";
        String msg = "hello, amqp!";
        rabbitTemplate.convertAndSend(queueName, msg);
        System.out.println("msg = " + msg);
    }

    @Test
    void testWorkQueue() throws InterruptedException {
        String queueName = "work.queue";

        for (int i = 1; i <= 50; i++) {
            String msg = "hello, worker, message_" + i;
            rabbitTemplate.convertAndSend(queueName, msg);
            Thread.sleep(20);
        }
    }

    @Test
    void testSendFanout() {
        String exchangeName = "fanout.exchange";
        String msg = "hello, fanout!";
        rabbitTemplate.convertAndSend(exchangeName, null, msg);
    }

    @Test
    void testSendDirect() {
        String exchangeName = "direct.exchange";
        String msg = "红色通知，警报警报";
        rabbitTemplate.convertAndSend(exchangeName, "red", msg);
    }

    @Test
    void testSendTopic() {
        String exchangeName = "topic.exchange";
        String msg = "今天天气挺不错，我的心情的挺好的";
        rabbitTemplate.convertAndSend(exchangeName, "china.weather", msg);
        rabbitTemplate.convertAndSend(exchangeName, "china.news", msg);
    }

    @Test
    void testSendObject() {
        Map<String, Object> msg = new HashMap<>(2);
        msg.put("name", "jack");
        msg.put("age", 21);
        rabbitTemplate.convertAndSend("object.queue", msg);
    }

    @Test
    void testConfirmCallback() throws InterruptedException {
        // 1.创建 cd
        CorrelationData cd = new CorrelationData();
        // 2.添加 ConfirmCallback
        cd.getFuture().addCallback(new ListenableFutureCallback<CorrelationData.Confirm>() {
            @Override
            public void onFailure(Throwable ex) {
                log.error("消息回调失败", ex);
            }

            @Override
            public void onSuccess(CorrelationData.Confirm result) {
                log.debug("收到confirm callback回执");

                if (result.isAck()) {
                    // 消息发送成功
                    log.debug("消息发送成功，收到ack");
                } else {
                    // 消息发送失败
                    log.error("消息发送失败，收到nack， 原因：{}", result.getReason());
                }
            }
        });

        //rabbitTemplate.convertAndSend("hmall.direct123", "red", "hello", cd);
        // 路由失败
        rabbitTemplate.convertAndSend("hmall.direct123", "red2", "hello", cd);

        // 需要休眠才能看到结果
        Thread.sleep(2000);
    }

    /**
     * 发送 100 万，测试消息的持久化
     * 这边建议关闭：生产者确认机制 return+ack，不然会影响性能 3000条/s
     */
    @Test
    void testPageOut() {
        // 创建的消息默认是持久化的，这边单独设置为非持久化
        Message message = MessageBuilder
                .withBody("hello".getBytes(StandardCharsets.UTF_8))
                .setDeliveryMode(MessageDeliveryMode.NON_PERSISTENT)
                .build();

        //Message message = MessageBuilder
        //        .withBody("hello".getBytes(StandardCharsets.UTF_8))
        //        .setDeliveryMode(MessageDeliveryMode.PERSISTENT)
        //        .build();

        for (int i = 0; i < 1000000; i++) {
            rabbitTemplate.convertAndSend("lazy.queue", message);
        }
    }

    @Test
    void testSendTTLMessage() {
        rabbitTemplate.convertAndSend("simple.direct", "hi", "hello",
                new MessagePostProcessor() {
            @Override
            public Message postProcessMessage(Message message) throws AmqpException {
                message.getMessageProperties().setExpiration("10000");
                return message;
            }
        });
        log.info("消息发送成功！");
    }

    /*
    * 测试死信队列
     */
    @Test
    void testSendDelayMessage() {
        rabbitTemplate.convertAndSend("delay.direct", "hi", "hello", new MessagePostProcessor() {
            @Override
            public Message postProcessMessage(Message message) throws AmqpException {
                message.getMessageProperties().setDelay(10000);
                return message;
            }
        });
        log.info("消息发送成功！");
    }
}
