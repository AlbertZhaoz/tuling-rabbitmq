package com.hmall.cart.listeners;

import com.hmall.cart.service.ICartService;
import com.hmall.common.constants.MqConstants;
import lombok.RequiredArgsConstructor;
import org.springframework.amqp.core.ExchangeTypes;
import org.springframework.amqp.rabbit.annotation.Exchange;
import org.springframework.amqp.rabbit.annotation.Queue;
import org.springframework.amqp.rabbit.annotation.QueueBinding;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@RequiredArgsConstructor
public class OrderStatusListener {

    private final ICartService cartService;

    @RabbitListener(bindings = @QueueBinding(
            value = @Queue(name = "cart.clear.queue"),
            exchange = @Exchange(name = MqConstants.TRADE_EXCHANGE_NAME, type = ExchangeTypes.TOPIC),
            key = MqConstants.ORDER_CREATE_KEY
    ))
    public void listenOrderCreate(List<Long> itemIds){
        cartService.removeByItemIds(itemIds);
    }
}
