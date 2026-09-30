package com.medilink.store.service;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;
import java.util.List;
import java.util.UUID;

@Service
public class OrderClient {
    private final RestClient client;
    public OrderClient(@Value("${medilink.order-url}") String url, RestClient.Builder builder){client=builder.baseUrl(url).build();}
    public List<ApiModels.Order> orders(String token){
        ApiModels.StoreOrders body=client.get().uri("/api/store-orders").header("Authorization","Bearer "+token).retrieve().body(ApiModels.StoreOrders.class);
        return body==null||body.items==null?List.of():body.items;
    }
    public void claim(String token, UUID id){client.post().uri("/api/store-orders/{id}/claim",id).header("Authorization","Bearer "+token).retrieve().toBodilessEntity();}
    public void accept(String token, UUID id){client.post().uri("/api/store-orders/{id}/accept",id).header("Authorization","Bearer "+token).retrieve().toBodilessEntity();}
    public void reject(String token, UUID id){client.post().uri("/api/store-orders/{id}/reject",id).header("Authorization","Bearer "+token).retrieve().toBodilessEntity();}
    public void status(String token, UUID id, int status){client.put().uri("/api/store-orders/{id}/status",id).header("Authorization","Bearer "+token).contentType(MediaType.APPLICATION_JSON).body(new StatusBody(status)).retrieve().toBodilessEntity();}
    private record StatusBody(int status){}
}
