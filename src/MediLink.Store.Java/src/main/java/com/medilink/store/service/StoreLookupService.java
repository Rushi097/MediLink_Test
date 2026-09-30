package com.medilink.store.service;

import org.springframework.stereotype.Service;

@Service
public class StoreLookupService {
    private final InventoryClient inventory;
    public StoreLookupService(InventoryClient inventory){this.inventory=inventory;}
    public ApiModels.Store findCurrentStore(StorePrincipal principal){
        var store=inventory.myStore(principal.getToken());
        if(store==null) throw new com.medilink.store.web.StorePortalException("The signed-in store account does not have a registered store.");
        return store;
    }
}
