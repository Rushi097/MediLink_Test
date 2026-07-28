package com.medilink.store.web;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
public class StorePortalExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(StorePortalExceptionHandler.class);

    @ExceptionHandler(StorePortalException.class)
    String handleStorePortalException(StorePortalException exception, Model model) {
        model.addAttribute("message", exception.getMessage());
        return "error";
    }

    @ExceptionHandler(DataAccessException.class)
    String handleDatabaseException(DataAccessException exception, Model model) {
        log.error("Store portal database operation failed", exception);
        model.addAttribute("message", "The store portal could not read its database. Check that MySQL is running and restart the portal.");
        return "error";
    }
}
