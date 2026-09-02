package com.medilink.store.service;

import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;
import org.springframework.web.client.HttpClientErrorException;

@Component
public class StoreAuthenticationProvider implements AuthenticationProvider {
    private final AuthClient auth;
    public StoreAuthenticationProvider(AuthClient auth){this.auth=auth;}
    @Override public Authentication authenticate(Authentication authentication) throws AuthenticationException {
        try {
            var response=auth.login(authentication.getName(), String.valueOf(authentication.getCredentials()));
            if(response==null || !"StoreOwner".equals(response.role)) throw new BadCredentialsException("This portal is for medical-store owners only.");
            return UsernamePasswordAuthenticationToken.authenticated(new StorePrincipal(response.userId,response.email,response.fullName,response.token), null, new StorePrincipal(response.userId,response.email,response.fullName,response.token).getAuthorities());
        } catch (HttpClientErrorException ex) { throw new BadCredentialsException("Invalid email or password."); }
        catch (BadCredentialsException ex) { throw ex; }
        catch (Exception ex) { throw new BadCredentialsException("Authentication service is unavailable.", ex); }
    }
    @Override public boolean supports(Class<?> authentication){return UsernamePasswordAuthenticationToken.class.isAssignableFrom(authentication);}
}
