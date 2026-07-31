package com.medilink.store;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;

@SpringBootApplication
@EnableJpaRepositories(basePackages = "com.medilink.store.repository", considerNestedRepositories = true)
public class StorePortalApplication {
    public static void main(String[] args) {
        SpringApplication.run(StorePortalApplication.class, args);
    }
}
