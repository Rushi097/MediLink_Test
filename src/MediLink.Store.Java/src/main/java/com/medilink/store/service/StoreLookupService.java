package com.medilink.store.service;

import com.medilink.store.entity.Store;
import com.medilink.store.web.StorePortalException;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.UUID;

/**
 * Reads the existing EF Core-owned account tables using their MySQL char(36)
 * UUID representation. This avoids Hibernate binding UUID parameters as binary
 * values, which prevented valid store-owner accounts from reaching the dashboard.
 */
@Service
public class StoreLookupService {
    private final JdbcTemplate jdbc;

    public StoreLookupService(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    public Store findStoreForEmail(String email) {
        List<String> userIds = jdbc.query(
                "SELECT Id FROM Users WHERE LOWER(Email) = ? AND Role = 1",
                (rows, rowNumber) -> rows.getString("Id"),
                email.trim().toLowerCase());
        if (userIds.isEmpty()) {
            throw new StorePortalException("The signed-in store account could not be found.");
        }

        List<String> profileIds = jdbc.query(
                "SELECT Id FROM StoreOwnerProfiles WHERE UserId = ?",
                (rows, rowNumber) -> rows.getString("Id"),
                userIds.getFirst());
        if (profileIds.isEmpty()) {
            throw new StorePortalException("This account does not have a medical-store owner profile. Create a store account before signing in.");
        }

        List<Store> stores = jdbc.query(
                "SELECT Id, Name, Address, StoreOwnerProfileId FROM Stores WHERE StoreOwnerProfileId = ? LIMIT 1",
                (rows, rowNumber) -> new Store(
                        UUID.fromString(rows.getString("Id")),
                        rows.getString("Name"),
                        rows.getString("Address"),
                        UUID.fromString(rows.getString("StoreOwnerProfileId"))),
                profileIds.getFirst());
        if (stores.isEmpty()) {
            throw new StorePortalException("This store-owner account has no store profile yet. Please register the store again with a new email address.");
        }
        return stores.getFirst();
    }
}
