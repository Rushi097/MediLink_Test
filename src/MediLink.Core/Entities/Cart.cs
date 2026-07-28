namespace MediLink.Core.Entities;

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;
    public int Quantity { get; set; }
}
