package com.medilink.store.service;

import org.springframework.stereotype.Service;
import java.math.BigDecimal;
import java.util.List;
import java.util.UUID;

@Service
public class StoreInventoryService {
    private final InventoryClient inventory;
    public StoreInventoryService(InventoryClient inventory){this.inventory=inventory;}

    public List<ApiModels.Medicine> listProducts(StorePrincipal principal, UUID storeId){
        return inventory.products(principal.getToken(), storeId);
    }

    public List<ApiModels.MedicineCatalogItem> listCatalog(StorePrincipal principal, int pageSize){
        return inventory.listCatalog(principal.getToken(), pageSize);
    }

    public List<ApiModels.MedicineCatalogItem> searchCatalog(StorePrincipal principal, String name){
        return inventory.searchCatalog(principal.getToken(), name);
    }

    public ApiModels.MedicineCatalogItem getCatalogItem(StorePrincipal principal, String externalId){
        return inventory.catalogItem(principal.getToken(), externalId);
    }

    public void addProduct(StorePrincipal principal, UUID storeId, String externalMedicineId, BigDecimal price, int stock){
        inventory.add(principal.getToken(), storeId, externalMedicineId, price, stock);
    }

    public void updateProduct(StorePrincipal principal, UUID storeId, UUID medicineId, BigDecimal price, int stock){
        inventory.update(principal.getToken(), storeId, medicineId, price, stock);
    }
}
