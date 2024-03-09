package com.hmall.common.constants;

public interface MqConstants {
    String TRADE_EXCHANGE_NAME = "trade.topic";
    String ORDER_CREATE_KEY = "order.create";

    String DELAY_EXCHANGE = "trade.delay.topic";
    String DELAY_ORDER_QUEUE = "trade.order.delay.queue";
    String DELAY_ORDER_ROUTING_KEY = "order.query";
}
