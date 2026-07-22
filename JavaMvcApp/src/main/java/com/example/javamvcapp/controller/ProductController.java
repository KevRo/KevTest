package com.example.javamvcapp.controller;

import com.example.javamvcapp.client.ProductApiClient;
import com.example.javamvcapp.dto.CreateProductDto;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.client.RestClientException;

import java.math.BigDecimal;
import java.util.List;

@Controller
@RequestMapping("/products")
public class ProductController {

    private static final Logger log = LoggerFactory.getLogger(ProductController.class);

    private final ProductApiClient productApiClient;

    public ProductController(ProductApiClient productApiClient) {
        this.productApiClient = productApiClient;
    }

    @GetMapping
    public String index(Model model) {
        try {
            model.addAttribute("products", productApiClient.getAll());
        } catch (RestClientException ex) {
            log.warn("Could not reach the Products API", ex);
            model.addAttribute("products", List.of());
            model.addAttribute("apiError", "Could not reach the Products API. Make sure KevTest.Api is running.");
        }
        return "products/index";
    }

    @GetMapping("/new")
    public String createForm(Model model) {
        model.addAttribute("name", "");
        model.addAttribute("price", null);
        return "products/create";
    }

    @PostMapping
    public String create(@RequestParam String name, @RequestParam(required = false) BigDecimal price, Model model) {
        if (name == null || name.isBlank() || price == null || price.signum() <= 0) {
            model.addAttribute("name", name);
            model.addAttribute("price", price);
            model.addAttribute("error", "Name is required and price must be greater than zero.");
            return "products/create";
        }

        productApiClient.create(new CreateProductDto(name, price));
        return "redirect:/products";
    }

    @PostMapping("/{id}/delete")
    public String delete(@PathVariable int id) {
        productApiClient.delete(id);
        return "redirect:/products";
    }
}
