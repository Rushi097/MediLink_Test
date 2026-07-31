package com.medilink.store.entity;

import jakarta.persistence.*;
import org.hibernate.annotations.JdbcTypeCode;
import java.math.BigDecimal;
import java.sql.Types;
import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Table(name = "Medicines")
public class Medicine {
    @Id
    @JdbcTypeCode(Types.VARCHAR)
    @Column(name = "Id", columnDefinition = "char(36)")
    private UUID id;
    @Column(name = "Name")
    private String name;
    @Column(name = "Description")
    private String description;
    @Column(name = "Category")
    private String category;
    @Column(name = "Price")
    private BigDecimal price;
    @Column(name = "StockQuantity")
    private int stockQuantity;
    @Column(name = "ImageUrl")
    private String imageUrl;
    @Column(name = "IsActive")
    private boolean active;
    @Column(name = "CreatedAt")
    private LocalDateTime createdAt;

    public Medicine() {
    }

    public Medicine(String name, String category, String description, BigDecimal price, int stock, String imageUrl) {
        this(UUID.randomUUID(), name, category, description, price, stock, imageUrl, true, LocalDateTime.now());
    }

    public Medicine(UUID id, String name, String category, String description, BigDecimal price, int stock,
            String imageUrl, boolean active, LocalDateTime createdAt) {
        this.id = id;
        this.name = name;
        this.category = category;
        this.description = description;
        this.price = price;
        this.stockQuantity = stock;
        this.imageUrl = imageUrl;
        this.active = active;
        this.createdAt = createdAt;
    }

    public UUID getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public String getCategory() {
        return category;
    }

    public String getDescription() {
        return description;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public int getStockQuantity() {
        return stockQuantity;
    }

    public String getImageUrl() {
        return imageUrl;
    }

    public boolean isActive() {
        return active;
    }

    public void update(String name, String category, String description, BigDecimal price, int stock, String imageUrl) {
        this.name = name;
        this.category = category;
        this.description = description;
        this.price = price;
        stockQuantity = stock;
        if (imageUrl != null)
            this.imageUrl = imageUrl;
    }
}
