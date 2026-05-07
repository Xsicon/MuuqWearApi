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
    public string? ItemsSummary { get; set; }  // for list card view
    public string? FirstName { get; set; }     // for detail panel
    public string? LastName { get; set; }      // for detail panel
    public string? Address { get; set; }       // for detail panel
    public string? City { get; set; }          // for detail panel
    public string? PostalCode { get; set; }    // for detail panel
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

// used by Process button → update status
public class UpdateOrderStatusDTO
{
    public string Status { get; set; } = string.Empty;
}

public static class OrderStatus
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Shipped = "shipped";
    public const string Delivered = "delivered";
    public const string Cancelled = "cancelled";
}