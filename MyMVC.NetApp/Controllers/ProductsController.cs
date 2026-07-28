using KevTest.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MyMVC.NetApp.Services;

namespace MyMVC.NetApp.Controllers;

public class ProductsController : Controller
{
    private readonly IProductsApiClient _productsApiClient;
    private readonly ILogger<ProductsController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ProductsController(
        IProductsApiClient productsApiClient,
        ILogger<ProductsController> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _productsApiClient = productsApiClient;
        _logger = logger;
        _localizer = localizer;
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
            ViewData["ApiError"] = true;
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
            ModelState.AddModelError(nameof(request.Name), _localizer["Products_Create_NameRequired"]);
        }

        if (request.Price <= 0)
        {
            ModelState.AddModelError(nameof(request.Price), _localizer["Products_Create_PriceInvalid"]);
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
