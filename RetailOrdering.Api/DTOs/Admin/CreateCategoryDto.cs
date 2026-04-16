using System.ComponentModel.DataAnnotations;

namespace RetailOrdering.Api.DTOs.Admin;

public class CreateCategoryDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
