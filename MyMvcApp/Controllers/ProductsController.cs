using KevTest.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Services;

namespace MyMvcApp.Controllers;

public class ProductsController : Controller
{
    private readonly IProductsApiClient _productsApiClient;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductsApiClient productsApiClient, ILogger<ProductsController> logger)
    {
        _productsApiClient = productsApiClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var products = await _productsApiClient.GetAllAsync(cancellationToken);
            return View(products);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach the Products API");
            ViewData["ApiError"] = "Could not reach the Products API. Make sure KevTest.Api is running.";
            return View(Array.Empty<ProductDto>());
        }
    }

    public IActionResult Create() => View(new CreateProductDto(string.Empty, 0));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name is required.");
        }

        if (request.Price <= 0)
        {
            ModelState.AddModelError(nameof(request.Price), "Price must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        await _productsApiClient.CreateAsync(request, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productsApiClient.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
