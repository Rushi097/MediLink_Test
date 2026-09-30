package com.medilink.store.service;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;
import java.math.BigDecimal;
import java.util.List;
import java.util.UUID;

@Service
public class InventoryClient {
    private final RestClient client;
    private final String publicUploadBaseUrl;

    public InventoryClient(@Value("${medilink.inventory-url}") String url,
                           @Value("${medilink.public-upload-base-url}") String publicUploadBaseUrl,
                           RestClient.Builder builder) {
        client = builder.baseUrl(url).build();
        this.publicUploadBaseUrl = publicUploadBaseUrl;
    }

    public ApiModels.Store myStore(String token) {
        StoreEnvelope env = client.get().uri("/api/stores/owner/me")
                .header("Authorization", "Bearer " + token).retrieve().body(StoreEnvelope.class);
        return env == null ? null : env.item;
    }

    public List<ApiModels.Medicine> products(String token, UUID storeId) {
        InventoryEnvelope env = client.get().uri("/api/stores/{id}/inventory", storeId)
                .header("Authorization", "Bearer " + token).retrieve().body(InventoryEnvelope.class);
        return env == null || env.items == null ? List.of() : env.items;
    }

    public List<ApiModels.MedicineCatalogItem> listCatalog(String token, int pageSize) {
        CatalogEnvelope env = client.get().uri(uriBuilder -> uriBuilder
                        .path("/api/medicines/catalog/list")
                        .queryParam("pageSize", pageSize)
                        .build())
                .header("Authorization", "Bearer " + token)
                .retrieve().body(CatalogEnvelope.class);
        return env == null || env.items == null ? List.of() : env.items;
    }

    public List<ApiModels.MedicineCatalogItem> searchCatalog(String token, String name) {
        CatalogEnvelope env = client.get().uri(uriBuilder -> uriBuilder
                        .path("/api/medicines/catalog/search")
                        .queryParam("name", name)
                        .build())
                .header("Authorization", "Bearer " + token).retrieve().body(CatalogEnvelope.class);
        return env == null || env.items == null ? List.of() : env.items;
    }

    public ApiModels.MedicineCatalogItem catalogItem(String token, String externalId) {
        CatalogItemEnvelope env = client.get().uri("/api/medicines/catalog/{id}", externalId)
                .header("Authorization", "Bearer " + token).retrieve().body(CatalogItemEnvelope.class);
        return env == null ? null : env.item;
    }

    public void add(String token, UUID storeId, String externalMedicineId, BigDecimal price, int stockQuantity) {
        client.post().uri("/api/stores/{id}/inventory", storeId)
                .header("Authorization", "Bearer " + token)
                .contentType(MediaType.APPLICATION_JSON)
                .body(new MedicineRequest(externalMedicineId, price, stockQuantity))
                .retrieve().toBodilessEntity();
    }

    public void update(String token, UUID storeId, UUID medicineId, BigDecimal price, int stockQuantity) {
        client.put().uri("/api/stores/{storeId}/inventory/{medicineId}", storeId, medicineId)
                .header("Authorization", "Bearer " + token)
                .contentType(MediaType.APPLICATION_JSON)
                .body(new MedicineUpdateRequest(price, stockQuantity))
                .retrieve().toBodilessEntity();
    }

    public String publicImageUrl(String filename){return publicUploadBaseUrl + "/uploads/" + filename;}

    private record StoreEnvelope(boolean success, ApiModels.Store item) {}
    private record InventoryEnvelope(boolean success, List<ApiModels.Medicine> items) {}
    private record CatalogEnvelope(boolean success, List<ApiModels.MedicineCatalogItem> items) {}
    private record CatalogItemEnvelope(boolean success, ApiModels.MedicineCatalogItem item) {}
    private record MedicineRequest(String externalMedicineId, BigDecimal price, int stockQuantity) {}
    private record MedicineUpdateRequest(BigDecimal price, int stockQuantity) {}
}
