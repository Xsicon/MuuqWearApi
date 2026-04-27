namespace MuuqWear.Model.DTO.CartDTO;
// full cart returned to frontend
public class CartDTO
{
    // all items in cart
    public List<CartItemDTO> Items { get; set; } = new();

    // computed totals
    // calculated server side
    // no need to store in DB

    // sum of all item totals
    public decimal Subtotal => Items.Sum(i => i.ItemTotal);

    // shipping — free for now 
    public decimal Shipping => 0;

    // 10% tax on subtotal 
    public decimal Tax => Math.Round(Subtotal * 0.10m, 2);

    // total = subtotal + shipping + tax
    public decimal Total => Subtotal + Shipping + Tax;

    // total item count for badge 
    // e.g. 2 jackets + 1 tee = 3
    public int TotalItems => Items.Sum(i => i.Quantity);
}
