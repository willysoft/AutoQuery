using AutoQuery.Abstractions;
using AutoQuery.AspNetCore;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoQueryApiDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private List<Product> products = new List<Product>
    {
        new Product(1, "PRD-001", "Laptop", 1299.99m),
        new Product(2, "PRD-002", "Mouse", 29.99m),
        new Product(3, "PRD-003", "Keyboard", 79.99m),
        new Product(4, "PRD-004", "Monitor", 399.99m),
        new Product(5, "PRD-005", "Webcam", 89.99m),
        new Product(6, "PRD-006", "Headphones", 149.99m),
        new Product(7, "PRD-007", "Desk Chair", 299.99m),
        new Product(8, "PRD-008", "USB Hub", 39.99m),
    };

    private readonly IQueryProcessor _queryProcessor;

    public ProductsController(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
    }

    [HttpGet]
    [EnableFieldProjection]
    public IActionResult Get(ProductQueryOptions queryOptions)
    {
        var result = products.AsQueryable()
                              .ApplyQueryPagedResult(_queryProcessor, queryOptions);
        return Ok(result);
    }
}
