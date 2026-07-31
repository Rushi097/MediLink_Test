package com.medilink.store.service;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.userdetails.*;
import org.springframework.stereotype.Service;
import java.util.List;

@Service
public class StoreUserDetailsService implements UserDetailsService {
    private final JdbcTemplate jdbc;

    public StoreUserDetailsService(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    public UserDetails loadUserByUsername(String email) {
        List<Account> accounts = jdbc.query(
                "SELECT Email, PasswordHash, Role FROM Users WHERE LOWER(Email) = ?",
                (rows, rowNumber) -> new Account(rows.getString("Email"), rows.getString("PasswordHash"),
                        rows.getInt("Role")),
                email.trim().toLowerCase());
        if (accounts.isEmpty())
            throw new UsernameNotFoundException("Invalid store-owner account");
        Account account = accounts.getFirst();
        if (account.role() != 1)
            throw new UsernameNotFoundException("This portal is for medical-store owners only");
        return new User(account.email(), account.passwordHash(),
                List.of(new SimpleGrantedAuthority("ROLE_STORE_OWNER")));
    }

    private record Account(String email, String passwordHash, int role) {
    }
}
