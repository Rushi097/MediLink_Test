package com.medilink.store.web;

import jakarta.validation.constraints.*;
import java.math.BigDecimal;
import org.springframework.web.multipart.MultipartFile;

public class ProductForm {
    @NotBlank
    private String name;
    @NotBlank
    private String category;
    @NotBlank
    private String description;
    @NotNull
    @DecimalMin("0.01")
    private BigDecimal price;
    @NotNull
    @Min(0)
    private Integer stockQuantity;
    private MultipartFile image;

    public String getName() {
        return name;
    }

    public void setName(String v) {
        name = v;
    }

    public String getCategory() {
        return category;
    }

    public void setCategory(String v) {
        category = v;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String v) {
        description = v;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public void setPrice(BigDecimal v) {
        price = v;
    }

    public Integer getStockQuantity() {
        return stockQuantity;
    }

    public void setStockQuantity(Integer v) {
        stockQuantity = v;
    }

    public MultipartFile getImage() {
        return image;
    }

    public void setImage(MultipartFile v) {
        image = v;
    }
}
