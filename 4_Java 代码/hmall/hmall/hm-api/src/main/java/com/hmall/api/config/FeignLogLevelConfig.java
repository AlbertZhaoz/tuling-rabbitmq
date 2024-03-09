package com.hmall.api.config;

import com.hmall.api.client.fallback.CartClientFallbackFactory;
import com.hmall.api.client.fallback.ItemClientFallbackFactory;
import com.hmall.api.client.fallback.TradeClientFallbackFactory;
import com.hmall.api.client.fallback.UserClientFallbackFactory;
import com.hmall.api.interceptors.UserInfoInterceptor;
import feign.Logger;
import feign.RequestInterceptor;
import org.springframework.context.annotation.Bean;

public class FeignLogLevelConfig {

    @Bean
    public Logger.Level feignLogLevel(){
        return Logger.Level.FULL;
    }

    @Bean
    public RequestInterceptor userInfoInterceptor(){
        return new UserInfoInterceptor();
    }

    @Bean
    public ItemClientFallbackFactory itemClientFallbackFactory(){
        return new ItemClientFallbackFactory();
    }
    @Bean
    public CartClientFallbackFactory cartClientFallbackFactory(){
        return new CartClientFallbackFactory();
    }
    @Bean
    public UserClientFallbackFactory userClientFallbackFactory(){
        return new UserClientFallbackFactory();
    }
    @Bean
    public TradeClientFallbackFactory tradeClientFallbackFactory(){
        return new TradeClientFallbackFactory();
    }
}
