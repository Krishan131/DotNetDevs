using System.ComponentModel.DataAnnotations;

namespace RetailOrdering.Api.DTOs.Order;

public class UpdateOrderStatusDto
{
    [Required, MaxLength(50)]
    public string Status { get; set; } = string.Empty;
}
