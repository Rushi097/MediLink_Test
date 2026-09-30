package com.medilink.store.web;

import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import java.math.BigDecimal;

public class ProductForm {
    @NotBlank
    private String externalMedicineId;
    @NotNull @DecimalMin("0.01")
    private BigDecimal price;
    @NotNull @Min(0)
    private Integer stockQuantity;

    public String getExternalMedicineId(){return externalMedicineId;}
    public void setExternalMedicineId(String v){externalMedicineId=v;}
    public BigDecimal getPrice(){return price;}
    public void setPrice(BigDecimal v){price=v;}
    public Integer getStockQuantity(){return stockQuantity;}
    public void setStockQuantity(Integer v){stockQuantity=v;}
}
