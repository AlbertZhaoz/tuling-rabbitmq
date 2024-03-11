package com.hmall.common.config;

import com.hmall.common.mq.RelyUserInfoMessageProcessor;
import com.hmall.common.utils.UserContext;
import lombok.RequiredArgsConstructor;
import org.springframework.amqp.AmqpException;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessagePostProcessor;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.InitializingBean;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Configuration;

@Configuration
@RequiredArgsConstructor
@ConditionalOnClass(RabbitTemplate.class)
public class MqUserInfoConfig implements InitializingBean {

    private final SimpleRabbitListenerContainerFactory rabbitListenerContainerFactory;

    private final RabbitTemplate rabbitTemplate;

    @Override
    public void afterPropertiesSet() throws Exception {
        // 1.配置消息发送时的处理器
        rabbitTemplate.setBeforePublishPostProcessors(new RelyUserInfoMessageProcessor());
        // 2.配置消息接收后的处理器
        rabbitListenerContainerFactory.setAfterReceivePostProcessors(new MessagePostProcessor() {
            @Override
            public Message postProcessMessage(Message message) throws AmqpException {
                Long userId = message.getMessageProperties().getHeader("user-info");
                if (userId != null) {
                    UserContext.setUser(userId);
                }
                return message;
            }
        });
    }
}
