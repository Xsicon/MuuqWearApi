namespace MuuqWear.API.DTO;

public class OrderDTO
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Shipping { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? CreatedAt { get; set; }
    public List<OrderItemDTO> Items { get; set; } = new();
}

public class OrderItemDTO
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal ItemTotal { get; set; }
}

// used when placing order 
public class PlaceOrderDTO
{
    // contact info
    public string? Email { get; set; }

    // shipping address
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}