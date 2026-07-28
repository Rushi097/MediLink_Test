package com.medilink.store.web;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public class StoreRegistrationForm {
    @NotBlank @Size(max = 80) private String firstName;
    @NotBlank @Size(max = 80) private String lastName;
    @NotBlank @Email @Size(max = 160) private String email;
    @NotBlank @Size(min = 8, max = 100) private String password;
    @NotBlank @Size(max = 120) private String storeName;
    @NotBlank @Size(max = 200) private String storeAddress;
    @NotBlank @Size(max = 100) private String businessLicenseNumber;
    public String getFirstName(){return firstName;} public void setFirstName(String v){firstName=v;}
    public String getLastName(){return lastName;} public void setLastName(String v){lastName=v;}
    public String getEmail(){return email;} public void setEmail(String v){email=v;}
    public String getPassword(){return password;} public void setPassword(String v){password=v;}
    public String getStoreName(){return storeName;} public void setStoreName(String v){storeName=v;}
    public String getStoreAddress(){return storeAddress;} public void setStoreAddress(String v){storeAddress=v;}
    public String getBusinessLicenseNumber(){return businessLicenseNumber;} public void setBusinessLicenseNumber(String v){businessLicenseNumber=v;}
}
