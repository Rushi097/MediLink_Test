package com.medilink.store.service;

import com.medilink.store.web.StoreRegistrationForm;
import org.springframework.stereotype.Service;

@Service
public class StoreRegistrationService {
    private final AuthClient auth;
    public StoreRegistrationService(AuthClient auth){this.auth=auth;}
    public void register(StoreRegistrationForm form){auth.register(form);}
}
