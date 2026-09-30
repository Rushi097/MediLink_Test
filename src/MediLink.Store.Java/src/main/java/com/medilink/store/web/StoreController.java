package com.medilink.store.web;

import com.medilink.store.service.*;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.security.core.Authentication;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.validation.BindingResult;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.client.RestClientResponseException;
import org.springframework.web.multipart.MultipartFile;
import java.io.IOException;
import java.nio.file.*;
import java.util.*;
import java.math.BigDecimal;
@Controller
public class StoreController {
    private final StoreRegistrationService registration;
    private final StoreLookupService storeLookup;
    private final StoreInventoryService storeInventory;
    private final OrderClient orderClient;
    private final String uploadDir;
    private final String publicUploadBaseUrl;

    public StoreController(StoreRegistrationService registration, StoreLookupService storeLookup, StoreInventoryService storeInventory,
                           OrderClient orderClient, @Value("${medilink.upload-dir}") String uploadDir,
                           @Value("${medilink.public-upload-base-url}") String publicUploadBaseUrl) {
        this.registration = registration; this.storeLookup = storeLookup; this.storeInventory = storeInventory;
        this.orderClient = orderClient; this.uploadDir = uploadDir; this.publicUploadBaseUrl = publicUploadBaseUrl;
    }

    private StorePrincipal principal(Authentication auth) {
        if (auth == null || !(auth.getPrincipal() instanceof StorePrincipal p))
            throw new StorePortalException("Please sign in to access the store portal.");
        return p;
    }

    @GetMapping("/login") String login() { return "login"; }

    @GetMapping("/register") String register(Model model) { model.addAttribute("form", new StoreRegistrationForm()); return "register"; }

    @PostMapping("/register")
    String registerStore(@Valid @ModelAttribute("form") StoreRegistrationForm form, BindingResult errors) {
        if (errors.hasErrors()) return "register";
        try { registration.register(form); return "redirect:/login?registered"; }
        catch (Exception ex) { errors.reject("registration.failed", ex.getMessage() == null ? "Registration failed." : ex.getMessage()); return "register"; }
    }

    @GetMapping({"/", "/dashboard"})
    String dashboard(Authentication auth, Model model) {
        StorePrincipal p = principal(auth);
        var store = storeLookup.findCurrentStore(p);
        if (store == null)
            throw new StorePortalException("No store profile is linked to this store-owner account. Please create the store profile first.");

        var products = storeInventory.listProducts(p, store.getId());
        long low = products.stream().filter(x -> x.getStockQuantity() <= 10).count();

        long claimed = 0;
        long pendingOrders = 0;
        String ordersError = null;
        try {
            var storeOrders = orderClient.orders(p.getToken());
            claimed = storeOrders.stream()
                    .filter(x -> x.isClaimed() && store.getId().equals(x.getClaimedByStoreId()))
                    .count();
            pendingOrders = storeOrders.stream()
                    .filter(x -> x.getStatus() == 0 && store.getId().equals(x.getClaimedByStoreId()))
                    .count();
        } catch (Exception ex) {
            ordersError = "Orders service is temporarily unavailable.";
        }

        List<ApiModels.MedicineCatalogItem> catalog = List.of();
        String catalogError = null;
        try {
            catalog = storeInventory.listCatalog(p, 12);
        } catch (Exception ex) {
            catalogError = "Medicine catalogue is temporarily unavailable. You can still open Inventory and retry the search.";
        }

        model.addAttribute("store", store);
        model.addAttribute("inventoryCount", products.size());
        model.addAttribute("lowStock", low);
        model.addAttribute("claimedOrders", claimed);
        model.addAttribute("pendingOrders", pendingOrders);
        model.addAttribute("catalog", catalog);
        model.addAttribute("catalogError", catalogError);
        model.addAttribute("ordersError", ordersError);
        return "dashboard";
    }

    @GetMapping("/products")
    String products(Authentication auth,
                    @RequestParam(required = false) String search,
                    @RequestParam(required = false) String selectedMedicine,
                    Model model) {
        StorePrincipal p = principal(auth);
        var store = storeLookup.findCurrentStore(p);
        model.addAttribute("store", store);
        model.addAttribute("products", storeInventory.listProducts(p, store.getId()));
        model.addAttribute("form", new ProductForm());

        if (search != null && !search.isBlank()) {
            model.addAttribute("catalogResults", storeInventory.searchCatalog(p, search.trim()));
            model.addAttribute("catalogSearch", search.trim());
        }
        if (selectedMedicine != null && !selectedMedicine.isBlank()) {
            var selected = storeInventory.getCatalogItem(p, selectedMedicine.trim());
            model.addAttribute("selectedMedicine", selected);
            if (selected != null) {
                var form = new ProductForm();
                form.setExternalMedicineId(selected.getExternalId());
                model.addAttribute("form", form);
            }
        }
        return "products";
    }

    @PostMapping("/products")
    String addProduct(Authentication auth,
                      @Valid @ModelAttribute("form") ProductForm form,
                      BindingResult errors,
                      Model model) {
        if (errors.hasErrors()) return products(auth, null, form.getExternalMedicineId(), model);
        StorePrincipal p = principal(auth);
        var store = storeLookup.findCurrentStore(p);
        storeInventory.addProduct(p, store.getId(), form.getExternalMedicineId().trim(), form.getPrice(), form.getStockQuantity());
        return "redirect:/products?created";
    }

    @PostMapping("/products/{id}")
    String updateProduct(Authentication auth,
                         @PathVariable UUID id,
                         @RequestParam BigDecimal price,
                         @RequestParam int stockQuantity) {
        StorePrincipal p = principal(auth);
        var store = storeLookup.findCurrentStore(p);
        storeInventory.updateProduct(p, store.getId(), id, price, stockQuantity);
        return "redirect:/products?updated";
    }

    @GetMapping("/orders")
    String orderList(Authentication auth, Model model){
        StorePrincipal p = principal(auth);
        var store = storeLookup.findCurrentStore(p);
        if (store == null)
            throw new StorePortalException("No store profile is linked to this store-owner account.");

        List<ApiModels.Order> orders = List.of();
        String ordersError = null;
        try {
            orders = orderClient.orders(p.getToken());
        } catch (Exception ex) {
            ordersError = "Orders are temporarily unavailable. Please refresh in a moment.";
        }

        long pendingOrders = orders.stream()
                .filter(x -> x.getStatus() == 0 && store.getId().equals(x.getClaimedByStoreId()))
                .count();

        model.addAttribute("store", store);
        model.addAttribute("orders", orders);
        model.addAttribute("pendingOrders", pendingOrders);
        model.addAttribute("ordersError", ordersError);
        return "orders";
    }

    @PostMapping("/orders/{id}/claim") String claim(Authentication auth,@PathVariable UUID id){orderClient.claim(principal(auth).getToken(),id);return "redirect:/orders";}
    @PostMapping("/orders/{id}/accept") String accept(Authentication auth,@PathVariable UUID id){orderClient.accept(principal(auth).getToken(),id);return "redirect:/orders";}
    @PostMapping("/orders/{id}/reject") String reject(Authentication auth,@PathVariable UUID id){orderClient.reject(principal(auth).getToken(),id);return "redirect:/orders";}
    @PostMapping("/orders/{id}/status")
    String status(Authentication auth, @PathVariable UUID id, @RequestParam int status) {
        try {
            orderClient.status(principal(auth).getToken(), id, status);
            return "redirect:/orders?updated";
        } catch (RestClientResponseException ex) {
            String message = ex.getResponseBodyAsString();
            return "redirect:/orders?error=" + java.net.URLEncoder.encode(message, java.nio.charset.StandardCharsets.UTF_8);
        }
    }

    private String saveImage(MultipartFile image){
        if(image==null||image.isEmpty()) return null;
        String type=image.getContentType(); if(type==null||!type.startsWith("image/")) throw new IllegalArgumentException("Product image must be an image file.");
        try { String extension=Optional.ofNullable(image.getOriginalFilename()).filter(n->n.contains(".")).map(n->n.substring(n.lastIndexOf('.'))).orElse(".jpg"); Path folder=Paths.get(uploadDir).toAbsolutePath().normalize(); Files.createDirectories(folder); String name=UUID.randomUUID()+extension.toLowerCase(Locale.ROOT); image.transferTo(folder.resolve(name)); return publicUploadBaseUrl+"/uploads/"+name; }
        catch(IOException ex){throw new IllegalStateException("Unable to save the product image.",ex);}
    }
}
