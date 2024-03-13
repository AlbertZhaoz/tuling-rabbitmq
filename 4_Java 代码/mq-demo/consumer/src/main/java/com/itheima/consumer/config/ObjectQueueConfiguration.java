package com.itheima.consumer.config;

import org.springframework.amqp.core.Queue;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * ClassName:ObjectQueueConfiguration
 * Package:com.itheima.consumer.config
 * Description:
 *
 * @Author AlbertZhao
 * @Create 3/11/2024 4:27 PM
 * @Version 1.0
 */
//@Configuration
public class ObjectQueueConfiguration {
    @Bean
    public Queue directQueue1() {
        return new Queue("object.queue");
    }
}
