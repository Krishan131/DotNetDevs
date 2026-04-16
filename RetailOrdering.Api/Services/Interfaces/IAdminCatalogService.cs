using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.DTOs.Admin;
using RetailOrdering.Api.DTOs.Catalog;

namespace RetailOrdering.Api.Services.Interfaces;

public interface IAdminCatalogService
{
    Task<ServiceResult<CategoryDto>> CreateCategoryAsync(CreateCategoryDto request);
    Task<ServiceResult<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto request);
    Task<ServiceResult> DeleteCategoryAsync(int id);

    Task<ServiceResult<ProductDto>> CreateProductAsync(CreateProductDto request);
    Task<ServiceResult<ProductDto>> UpdateProductAsync(int id, UpdateProductDto request);
    Task<ServiceResult> DeleteProductAsync(int id);
    Task<ServiceResult<ProductDto>> UpdateInventoryAsync(int productId, UpdateInventoryDto request);
}

