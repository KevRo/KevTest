namespace KevTest.Core.Dtos;

public record ProductDto(int Id, string Name, decimal Price);

public record CreateProductDto(string Name, decimal Price);
