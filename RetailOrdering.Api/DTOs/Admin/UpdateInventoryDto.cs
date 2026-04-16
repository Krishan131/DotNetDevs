using System.ComponentModel.DataAnnotations;

namespace RetailOrdering.Api.DTOs.Admin;

public class UpdateInventoryDto
{
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
}
