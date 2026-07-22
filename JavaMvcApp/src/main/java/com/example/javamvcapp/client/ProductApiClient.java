package com.example.javamvcapp.client;

import com.example.javamvcapp.dto.CreateProductDto;
import com.example.javamvcapp.dto.ProductDto;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;

import java.util.List;

@Component
public class ProductApiClient {

    private final RestClient restClient;

    public ProductApiClient(@Value("${products.api.base-url}") String baseUrl) {
        this.restClient = RestClient.builder().baseUrl(baseUrl).build();
    }

    public List<ProductDto> getAll() {
        List<ProductDto> products = restClient.get()
                .uri("/api/products")
                .retrieve()
                .body(new ParameterizedTypeReference<List<ProductDto>>() {
                });
        return products != null ? products : List.of();
    }

    public ProductDto create(CreateProductDto request) {
        return restClient.post()
                .uri("/api/products")
                .body(request)
                .retrieve()
                .body(ProductDto.class);
    }

    public void delete(int id) {
        restClient.delete()
                .uri("/api/products/{id}", id)
                .retrieve()
                .toBodilessEntity();
    }
}
