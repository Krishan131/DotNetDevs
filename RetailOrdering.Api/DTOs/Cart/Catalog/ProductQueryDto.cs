namespace RetailOrdering.Api.DTOs.Catalog;

public class ProductQueryDto
{
    public int? CategoryId { get; set; }
    public string? Brand { get; set; }
    public string? Search { get; set; }
}
