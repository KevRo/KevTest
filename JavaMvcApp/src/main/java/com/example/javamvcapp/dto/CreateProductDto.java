package com.example.javamvcapp.dto;

import java.math.BigDecimal;

public record CreateProductDto(String name, BigDecimal price) {
}
