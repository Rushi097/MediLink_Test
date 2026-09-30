package com.medilink.store.service;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;

public final class ApiModels {
    private ApiModels() {}

    public static class AuthResponse {
        public UUID userId; public String token; public String email; public String role; public String fullName;
        public UUID getUserId(){return userId;} public String getToken(){return token;} public String getEmail(){return email;} public String getRole(){return role;} public String getFullName(){return fullName;}
    }
    public static class ErrorResponse { public String message; public String getMessage(){return message;} }
    public static class Store {
        public UUID id; public String name; public String address; public UUID storeOwnerProfileId;
        public UUID getId(){return id;} public String getName(){return name;} public String getAddress(){return address;} public UUID getStoreOwnerProfileId(){return storeOwnerProfileId;}
    }
    public static class Envelope<T> { public boolean success; public T item; public List<T> items; public boolean isSuccess(){return success;} public T getItem(){return item;} public List<T> getItems(){return items;} }

    /** Store inventory item returned by GET /api/stores/{storeId}/inventory. */
    public static class Medicine {
        public UUID id; public UUID storeId; public UUID medicineId; public String externalMedicineId;
        public String name; public String category; public String description; public BigDecimal price;
        public int stockQuantity; public String imageUrl; public boolean isActive; public LocalDateTime createdAt;
        public UUID getId(){return id;} public UUID getStoreId(){return storeId;} public UUID getMedicineId(){return medicineId;}
        public String getExternalMedicineId(){return externalMedicineId;} public String getName(){return name;} public String getCategory(){return category;}
        public String getDescription(){return description;} public BigDecimal getPrice(){return price;} public int getStockQuantity(){return stockQuantity;}
        public String getImageUrl(){return imageUrl;} public boolean isActive(){return isActive;}
    }

    /** External medicine catalogue result. Price and stock are deliberately absent. */
    public static class MedicineCatalogItem {
        public String externalId; public String externalSource; public String name; public String displayName;
        public String genericName; public String category; public String description; public String imageUrl;
        public String getExternalId(){return externalId;} public String getExternalSource(){return externalSource;} public String getName(){return name;}
        public String getDisplayName(){return displayName;} public String getGenericName(){return genericName;} public String getCategory(){return category;}
        public String getDescription(){return description;} public String getImageUrl(){return imageUrl;}
    }

    public static class CustomerProfile { public String phoneNumber; public String deliveryAddress; public String getPhoneNumber(){return phoneNumber;} public String getDeliveryAddress(){return deliveryAddress;} }
    public static class Customer { public UUID id; public String email; public String firstName; public String lastName; public String role; public CustomerProfile customerProfile; public UUID getId(){return id;} public String getEmail(){return email;} public String getFirstName(){return firstName;} public String getLastName(){return lastName;} public CustomerProfile getCustomerProfile(){return customerProfile;} }
    public static class Order {
        public UUID id; public UUID userId; public String deliveryAddress; public BigDecimal totalAmount; public int status;
        public LocalDateTime createdAt; public boolean claimed; public UUID claimedByStoreId;
        public Customer customer; public CustomerProfile customerProfile; public Store store; public List<OrderItem> items;
        public UUID getId(){return id;} public UUID getUserId(){return userId;} public String getDeliveryAddress(){return deliveryAddress;}
        public BigDecimal getTotalAmount(){return totalAmount;} public int getStatus(){return status;} public LocalDateTime getCreatedAt(){return createdAt;}
        public boolean isClaimed(){return claimed;} public UUID getClaimedByStoreId(){return claimedByStoreId;}
        public Customer getCustomer(){return customer;} public CustomerProfile getCustomerProfile(){return customerProfile;}
        public Store getStore(){return store;} public List<OrderItem> getItems(){return items;}
        public String getShortId(){return id == null ? "" : id.toString().substring(0, Math.min(8, id.toString().length()));}
        public String getCustomerName(){
            if (customer == null) return userId == null ? "Customer" : userId.toString();
            String first = customer.firstName == null ? "" : customer.firstName.trim();
            String last = customer.lastName == null ? "" : customer.lastName.trim();
            String name = (first + " " + last).trim();
            return name.isEmpty() ? customer.email : name;
        }
        public String getPhone(){
            return customerProfile == null || customerProfile.phoneNumber == null || customerProfile.phoneNumber.isBlank()
                    ? "Not supplied" : customerProfile.phoneNumber;
        }
        public String getStoreName(){ return store == null || store.name == null ? "Medical store" : store.name; }
        public String getStoreAddress(){ return store == null || store.address == null ? "Store address unavailable" : store.address; }
    }
    public static class OrderItem {
        public UUID id; public UUID medicineId; public UUID storeId; public String medicineName; public BigDecimal unitPrice; public int quantity;
        public UUID getId(){return id;} public UUID getMedicineId(){return medicineId;} public UUID getStoreId(){return storeId;}
        public String getMedicineName(){return medicineName;} public BigDecimal getUnitPrice(){return unitPrice;} public int getQuantity(){return quantity;}
        public BigDecimal getLineTotal(){ return (unitPrice == null ? BigDecimal.ZERO : unitPrice).multiply(BigDecimal.valueOf(Math.max(0, quantity))); }
    }
    public static class StoreOrders { public boolean success; public List<Order> items; public List<Order> getItems(){return items;} }
}
