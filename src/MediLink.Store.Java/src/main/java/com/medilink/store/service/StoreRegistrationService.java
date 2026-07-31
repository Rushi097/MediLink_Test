package com.medilink.store.service;

import com.medilink.store.web.StoreRegistrationForm;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.jdbc.core.JdbcTemplate;
import java.util.UUID;

@Service
public class StoreRegistrationService {
    private final JdbcTemplate jdbc;
    private final PasswordEncoder passwordEncoder;

    public StoreRegistrationService(JdbcTemplate jdbc, PasswordEncoder passwordEncoder) {
        this.jdbc = jdbc;
        this.passwordEncoder = passwordEncoder;
    }

    @Transactional
    public void register(StoreRegistrationForm form) {
        String email = form.getEmail().trim().toLowerCase();
        Integer existing = jdbc.queryForObject("SELECT COUNT(*) FROM Users WHERE LOWER(Email) = ?", Integer.class,
                email);
        if (existing != null && existing > 0)
            throw new IllegalArgumentException("An account already uses this email address.");
        String userId = UUID.randomUUID().toString(), profileId = UUID.randomUUID().toString(),
                storeId = UUID.randomUUID().toString();
        jdbc.update(
                "INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, Role, CreatedAt) VALUES (?, ?, ?, ?, ?, ?, UTC_TIMESTAMP(6))",
                userId, email, passwordEncoder.encode(form.getPassword()), form.getFirstName().trim(),
                form.getLastName().trim(), 1);
        jdbc.update("INSERT INTO StoreOwnerProfiles (Id, UserId, BusinessLicenseNumber) VALUES (?, ?, ?)", profileId,
                userId, form.getBusinessLicenseNumber().trim());
        jdbc.update("INSERT INTO Stores (Id, Name, Address, StoreOwnerProfileId) VALUES (?, ?, ?, ?)", storeId,
                form.getStoreName().trim(), form.getStoreAddress().trim(), profileId);
    }
}
