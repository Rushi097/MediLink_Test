package com.medilink.store.service;

import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.userdetails.UserDetails;
import java.util.Collection;
import java.util.List;
import java.util.UUID;

public class StorePrincipal implements UserDetails {
    private final UUID userId; private final String email; private final String fullName; private final String token;
    public StorePrincipal(UUID userId, String email, String fullName, String token){this.userId=userId;this.email=email;this.fullName=fullName;this.token=token;}
    public UUID getUserId(){return userId;} public String getFullName(){return fullName;} public String getToken(){return token;}
    @Override public Collection<? extends GrantedAuthority> getAuthorities(){return List.of(() -> "ROLE_STORE_OWNER");}
    @Override public String getPassword(){return "";} @Override public String getUsername(){return email;}
    @Override public boolean isAccountNonExpired(){return true;} @Override public boolean isAccountNonLocked(){return true;} @Override public boolean isCredentialsNonExpired(){return true;} @Override public boolean isEnabled(){return true;}
}
