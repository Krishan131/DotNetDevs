using System.ComponentModel.DataAnnotations;

namespace RetailOrdering.Api.DTOs.Admin;

public class UpdateProductDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0.01, 999999)]
    public decimal Price { get; set; }

    public int? CategoryId { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    public bool IsAvailable { get; set; }
}
