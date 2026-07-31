package com.medilink.store.config;

import com.medilink.store.service.StoreUserDetailsService;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
public class SecurityConfig {
    @Bean
    PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }

    @Bean
    SecurityFilterChain securityFilterChain(HttpSecurity http, StoreUserDetailsService users) throws Exception {
        return http.userDetailsService(users).authorizeHttpRequests(auth -> auth
                .requestMatchers("/css/**", "/uploads/**", "/login", "/register", "/error").permitAll().anyRequest()
                .hasRole("STORE_OWNER"))
                .formLogin(login -> login.loginPage("/login").defaultSuccessUrl("/dashboard", true).permitAll())
                .logout(logout -> logout.logoutSuccessUrl("/login?logout").permitAll()).build();
    }
}
