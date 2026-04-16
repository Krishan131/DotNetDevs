using RetailOrdering.Api.DTOs.Catalog;

namespace RetailOrdering.Api.Services.Interfaces;

public interface ICatalogService
{
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<string>> GetBrandsAsync();
    Task<List<ProductDto>> GetProductsAsync(ProductQueryDto query);
    Task<ProductDto?> GetProductByIdAsync(int productId);
}
