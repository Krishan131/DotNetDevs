using System.ComponentModel.DataAnnotations;

namespace RetailOrdering.Api.DTOs.Cart;

public class AddCartItemDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }
}
