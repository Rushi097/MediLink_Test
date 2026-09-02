package com.medilink.store.web;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.client.RestClientException;

@ControllerAdvice
public class StorePortalExceptionHandler {
    private static final Logger log=LoggerFactory.getLogger(StorePortalExceptionHandler.class);
    @ExceptionHandler(Exception.class)
    String handle(Exception exception, Model model){
        log.error("Store portal operation failed", exception);
        model.addAttribute("message", exception.getMessage() == null ? "The requested operation could not be completed." : exception.getMessage());
        return "error";
    }
}
