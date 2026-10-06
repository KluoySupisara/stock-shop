using Microsoft.EntityFrameworkCore;

public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal PricePerUnit { get; set; }
}

public class Stock
{
    public int StockId { get; set; }
    public int ProductId { get; set; }
    public int QuantityLeft { get; set; }
}

public class CartItem
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>().HasData(
            new Product { ProductId = 1, ProductName = "Keyboard", PricePerUnit = 49.90m },
            new Product { ProductId = 2, ProductName = "Mouse", PricePerUnit = 19.90m },
            new Product { ProductId = 3, ProductName = "Monitor", PricePerUnit = 199m },
            new Product { ProductId = 4, ProductName = "USB Hub", PricePerUnit = 34.50m });
        b.Entity<Stock>().HasData(
            new Stock { StockId = 1, ProductId = 1, QuantityLeft = 10 },
            new Stock { StockId = 2, ProductId = 2, QuantityLeft = 25 },
            new Stock { StockId = 3, ProductId = 3, QuantityLeft = 5 },
            new Stock { StockId = 4, ProductId = 4, QuantityLeft = 0 });
    }
}