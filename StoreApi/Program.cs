using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=store.db"));
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// Create store.db + mock data if it does not exist
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

// helper: cart rows joined with product info
async Task<List<CartLine>> GetCart(AppDb db) =>
    await db.CartItems.Join(db.Products, c => c.ProductId, p => p.ProductId,
        (c, p) => new CartLine(c.CartItemId, p.ProductId, p.ProductName, p.PricePerUnit, c.Quantity))
        .ToListAsync();

// 1) show all products with stock left
app.MapGet("/api/products", (AppDb db) =>
    db.Products.Join(db.Stocks, p => p.ProductId, s => s.ProductId,
        (p, s) => new { p.ProductId, p.ProductName, p.PricePerUnit, s.QuantityLeft })
        .ToListAsync());

// 2) show cart + total
app.MapGet("/api/cart", async (AppDb db) =>
{
    var lines = await GetCart(db);
    return new { items = lines, total = lines.Sum(l => l.LineTotal) };
});

// 3) add to cart (refuse if not enough stock, otherwise cut stock)
app.MapPost("/api/cart", async (AppDb db, AddRequest req) =>
{
    var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductId == req.ProductId);
    if (stock == null) return Results.NotFound("Product not found");
    if (req.Quantity <= 0 || stock.QuantityLeft < req.Quantity)
        return Results.BadRequest($"Not enough stock (only {stock?.QuantityLeft} left)");

    stock.QuantityLeft -= req.Quantity;
    var item = await db.CartItems.FirstOrDefaultAsync(c => c.ProductId == req.ProductId);
    if (item == null) db.CartItems.Add(new CartItem { ProductId = req.ProductId, Quantity = req.Quantity });
    else item.Quantity += req.Quantity;

    await db.SaveChangesAsync();
    return Results.Ok();
});

// 4) change quantity of a cart row (adjust stock by the difference)
app.MapPut("/api/cart/{id}", async (AppDb db, int id, QtyRequest req) =>
{
    var item = await db.CartItems.FindAsync(id);
    if (item == null) return Results.NotFound();
    var stock = await db.Stocks.FirstAsync(s => s.ProductId == item.ProductId);

    var diff = req.Quantity - item.Quantity;          // + = take more, - = give back
    if (req.Quantity < 1 || diff > stock.QuantityLeft)
        return Results.BadRequest("Not enough stock");

    stock.QuantityLeft -= diff;
    item.Quantity = req.Quantity;
    await db.SaveChangesAsync();
    return Results.Ok();
});

// 5) remove one row -> give its quantity back to stock
app.MapDelete("/api/cart/{id}", async (AppDb db, int id) =>
{
    var item = await db.CartItems.FindAsync(id);
    if (item == null) return Results.NotFound();
    var stock = await db.Stocks.FirstAsync(s => s.ProductId == item.ProductId);
    stock.QuantityLeft += item.Quantity;
    db.CartItems.Remove(item);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// 6) delete all -> give everything back to stock
app.MapDelete("/api/cart", async (AppDb db) =>
{
    foreach (var item in await db.CartItems.ToListAsync())
    {
        var stock = await db.Stocks.FirstAsync(s => s.ProductId == item.ProductId);
        stock.QuantityLeft += item.Quantity;
        db.CartItems.Remove(item);
    }
    await db.SaveChangesAsync();
    return Results.Ok();
});

// 7) checkout -> return summary, empty the cart (stock stays cut)
app.MapPost("/api/cart/checkout", async (AppDb db) =>
{
    var lines = await GetCart(db);
    if (lines.Count == 0) return Results.BadRequest("Cart is empty");

    db.CartItems.RemoveRange(db.CartItems);
    await db.SaveChangesAsync();
    return Results.Ok(new
    {
        items = lines,
        totalQuantity = lines.Sum(l => l.Quantity),
        totalSale = lines.Sum(l => l.LineTotal)
    });
});

app.Run("http://localhost:5077");

record AddRequest(int ProductId, int Quantity);
record QtyRequest(int Quantity);
record CartLine(int CartItemId, int ProductId, string ProductName, decimal PricePerUnit, int Quantity)
{
    public decimal LineTotal => PricePerUnit * Quantity;
}