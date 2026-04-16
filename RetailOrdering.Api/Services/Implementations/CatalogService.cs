using Microsoft.EntityFrameworkCore;
using RetailOrdering.Api.Data;
using RetailOrdering.Api.DTOs.Catalog;
using RetailOrdering.Api.Services.Interfaces;

namespace RetailOrdering.Api.Services.Implementations;

public class CatalogService(RetailOrderingDbContext dbContext) : ICatalogService
{
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await dbContext.categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetBrandsAsync()
    {
        return await dbContext.products
            .AsNoTracking()
            .Where(p => p.Brand != null && p.Brand != string.Empty)
            .Select(p => p.Brand!)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync();
    }

    public async Task<List<ProductDto>> GetProductsAsync(ProductQueryDto query)
    {
        var productsQuery = dbContext.products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.inventory)
            .AsQueryable();

        if (query.CategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Brand))
        {
            var brand = query.Brand.Trim();
            productsQuery = productsQuery.Where(p => p.Brand == brand);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            productsQuery = productsQuery.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        var products = await productsQuery
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(DtoMapper.ToProductDto).ToList();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId)
    {
        var productEntity = await dbContext.products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.inventory)
            .FirstOrDefaultAsync(p => p.Id == productId);

        return productEntity is null ? null : DtoMapper.ToProductDto(productEntity);
    }
}
