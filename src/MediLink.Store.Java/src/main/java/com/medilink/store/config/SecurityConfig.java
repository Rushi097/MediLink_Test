package com.medilink.store.config;

import com.medilink.store.service.StoreAuthenticationProvider;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.authentication.builders.AuthenticationManagerBuilder;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
public class SecurityConfig {
    private final StoreAuthenticationProvider provider;
    public SecurityConfig(StoreAuthenticationProvider provider){this.provider=provider;}

    @Bean
    SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        http.authenticationProvider(provider)
            .authorizeHttpRequests(auth -> auth.requestMatchers("/css/**","/uploads/**","/login","/register","/error").permitAll().anyRequest().hasRole("STORE_OWNER"))
            .formLogin(login -> login.loginPage("/login").defaultSuccessUrl("/dashboard", true).permitAll())
            .logout(logout -> logout.logoutSuccessUrl("/login?logout").permitAll());
        return http.build();
    }
}
