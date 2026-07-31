package com.medilink.store.entity;

import jakarta.persistence.*;
import org.hibernate.annotations.JdbcTypeCode;
import java.sql.Types;
import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Table(name = "Users")
public class UserAccount {
    @Id
    @JdbcTypeCode(Types.VARCHAR)
    @Column(name = "Id", columnDefinition = "char(36)")
    private UUID id;
    @Column(name = "Email")
    private String email;
    @Column(name = "PasswordHash")
    private String passwordHash;
    @Column(name = "FirstName")
    private String firstName;
    @Column(name = "LastName")
    private String lastName;
    @Column(name = "Role")
    private int role;
    @Column(name = "CreatedAt")
    private LocalDateTime createdAt;

    public UserAccount() {
    }

    public UserAccount(String email, String passwordHash, String firstName, String lastName) {
        this.id = UUID.randomUUID();
        this.email = email;
        this.passwordHash = passwordHash;
        this.firstName = firstName;
        this.lastName = lastName;
        this.role = 1;
        this.createdAt = LocalDateTime.now();
    }

    public UUID getId() {
        return id;
    }

    public String getEmail() {
        return email;
    }

    public String getPasswordHash() {
        return passwordHash;
    }

    public String getFirstName() {
        return firstName;
    }

    public String getLastName() {
        return lastName;
    }

    public int getRole() {
        return role;
    }
}
