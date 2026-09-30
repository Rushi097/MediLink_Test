package com.medilink.store.service;

import com.medilink.store.web.StoreRegistrationForm;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.*;
import org.springframework.stereotype.Service;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestClient;

@Service
public class AuthClient {
    private final RestClient client;
    public AuthClient(@Value("${medilink.auth-url}") String url, RestClient.Builder builder){client=builder.baseUrl(url).build();}
    public ApiModels.AuthResponse login(String email, String password){
        return client.post().uri("/api/auth/login").contentType(MediaType.APPLICATION_JSON).body(new LoginBody(email,password)).retrieve().body(ApiModels.AuthResponse.class);
    }
    public void register(StoreRegistrationForm form){
        client.post().uri("/api/auth/register/store-owner").contentType(MediaType.APPLICATION_JSON).body(new RegisterBody(form)).retrieve().toBodilessEntity();
    }
    private record LoginBody(String email,String password){}
    private record RegisterBody(String email,String password,String firstName,String lastName,String businessLicenseNumber,String storeName,String storeAddress){
        RegisterBody(StoreRegistrationForm f){this(f.getEmail(),f.getPassword(),f.getFirstName(),f.getLastName(),f.getBusinessLicenseNumber(),f.getStoreName(),f.getStoreAddress());}
    }
}
