using Microsoft.EntityFrameworkCore;
using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.Data;
using RetailOrdering.Api.Data.Models;
using RetailOrdering.Api.DTOs.Admin;
using RetailOrdering.Api.DTOs.Catalog;
using RetailOrdering.Api.Services.Interfaces;

namespace RetailOrdering.Api.Services.Implementations;

public class AdminCatalogService(RetailOrderingDbContext dbContext) : IAdminCatalogService
{
    public async Task<ServiceResult<CategoryDto>> CreateCategoryAsync(CreateCategoryDto request)
    {
        var name = request.Name.Trim();
        var exists = await dbContext.categories.AnyAsync(c => c.Name == name);
        if (exists)
        {
            return ServiceResult<CategoryDto>.Fail("Category with same name already exists.");
        }

        var categoryEntity = new category { Name = name };
        dbContext.categories.Add(categoryEntity);
        await dbContext.SaveChangesAsync();

        return ServiceResult<CategoryDto>.Ok(new CategoryDto
        {
            Id = categoryEntity.Id,
            Name = categoryEntity.Name
        }, "Category created.");
    }

    public async Task<ServiceResult<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto request)
    {
        var categoryEntity = await dbContext.categories.FirstOrDefaultAsync(c => c.Id == id);
        if (categoryEntity is null)
        {
            return ServiceResult<CategoryDto>.Fail("Category not found.");
        }

        categoryEntity.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync();

        return ServiceResult<CategoryDto>.Ok(new CategoryDto
        {
            Id = categoryEntity.Id,
            Name = categoryEntity.Name
        }, "Category updated.");
    }

    public async Task<ServiceResult> DeleteCategoryAsync(int id)
    {
        var categoryEntity = await dbContext.categories
            .Include(c => c.products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoryEntity is null)
        {
            return ServiceResult.Fail("Category not found.");
        }

        if (categoryEntity.products.Count > 0)
        {
            return ServiceResult.Fail("Category cannot be deleted while products are assigned to it.");
        }

        dbContext.categories.Remove(categoryEntity);
        await dbContext.SaveChangesAsync();

        return ServiceResult.Ok("Category deleted.");
    }

    public async Task<ServiceResult<ProductDto>> CreateProductAsync(CreateProductDto request)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await dbContext.categories.AnyAsync(c => c.Id == request.CategoryId.Value);
            if (!categoryExists)
            {
                return ServiceResult<ProductDto>.Fail("Invalid category.");
            }
        }

        var productEntity = new product
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            Brand = request.Brand,
            IsAvailable = request.IsAvailable
        };

        dbContext.products.Add(productEntity);
        await dbContext.SaveChangesAsync();

        dbContext.inventories.Add(new inventory
        {
            ProductId = productEntity.Id,
            Quantity = request.InitialQuantity
        });

        await dbContext.SaveChangesAsync();

        var savedProduct = await dbContext.products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.inventory)
            .FirstAsync(p => p.Id == productEntity.Id);

        return ServiceResult<ProductDto>.Ok(DtoMapper.ToProductDto(savedProduct), "Product created.");
    }

    public async Task<ServiceResult<ProductDto>> UpdateProductAsync(int id, UpdateProductDto request)
    {
        var productEntity = await dbContext.products.FirstOrDefaultAsync(p => p.Id == id);
        if (productEntity is null)
        {
            return ServiceResult<ProductDto>.Fail("Product not found.");
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await dbContext.categories.AnyAsync(c => c.Id == request.CategoryId.Value);
            if (!categoryExists)
            {
                return ServiceResult<ProductDto>.Fail("Invalid category.");
            }
        }

        productEntity.Name = request.Name.Trim();
        productEntity.Description = request.Description;
        productEntity.Price = request.Price;
        productEntity.CategoryId = request.CategoryId;
        productEntity.Brand = request.Brand;
        productEntity.IsAvailable = request.IsAvailable;

        await dbContext.SaveChangesAsync();

        var savedProduct = await dbContext.products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.inventory)
            .FirstAsync(p => p.Id == id);

        return ServiceResult<ProductDto>.Ok(DtoMapper.ToProductDto(savedProduct), "Product updated.");
    }

    public async Task<ServiceResult> DeleteProductAsync(int id)
    {
        var productEntity = await dbContext.products
            .Include(p => p.orderitems)
            .Include(p => p.cartitems)
            .Include(p => p.inventory)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (productEntity is null)
        {
            return ServiceResult.Fail("Product not found.");
        }

        if (productEntity.orderitems.Count > 0)
        {
            return ServiceResult.Fail("Product with order history cannot be deleted.");
        }

        if (productEntity.inventory is not null)
        {
            dbContext.inventories.Remove(productEntity.inventory);
        }

        if (productEntity.cartitems.Count > 0)
        {
            dbContext.cartitems.RemoveRange(productEntity.cartitems);
        }

        dbContext.products.Remove(productEntity);
        await dbContext.SaveChangesAsync();

        return ServiceResult.Ok("Product deleted.");
    }

    public async Task<ServiceResult<ProductDto>> UpdateInventoryAsync(int productId, UpdateInventoryDto request)
    {
        var productEntity = await dbContext.products
            .Include(p => p.Category)
            .Include(p => p.inventory)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (productEntity is null)
        {
            return ServiceResult<ProductDto>.Fail("Product not found.");
        }

        var inventoryEntity = productEntity.inventory;
        if (inventoryEntity is null)
        {
            inventoryEntity = new inventory
            {
                ProductId = productEntity.Id,
                Quantity = request.Quantity
            };
            dbContext.inventories.Add(inventoryEntity);
        }
        else
        {
            inventoryEntity.Quantity = request.Quantity;
        }

        await dbContext.SaveChangesAsync();

        productEntity = await dbContext.products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.inventory)
            .FirstAsync(p => p.Id == productId);

        return ServiceResult<ProductDto>.Ok(DtoMapper.ToProductDto(productEntity), "Inventory updated.");
    }
}

