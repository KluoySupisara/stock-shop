"use client";

import { useEffect, useState } from "react";

const API = "http://localhost:5077/api";

type Product = { productId: number; productName: string; pricePerUnit: number; quantityLeft: number };
type Line = { cartItemId: number; productName: string; pricePerUnit: number; quantity: number; lineTotal: number };
type Summary = { items: Line[]; totalQuantity: number; totalSale: number };

const money = (n: number) => "$" + n.toFixed(2);

// one helper for every API call
async function call(method: string, path: string, body?: object) {
  const res = await fetch(API + path, {
    method,
    headers: { "Content-Type": "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(await res.text());
  const text = await res.text();
  return text ? JSON.parse(text) : true; // true = success with no data
}

export default function Home() {
  const [products, setProducts] = useState<Product[]>([]);
  const [cart, setCart] = useState<{ items: Line[]; total: number }>({ items: [], total: 0 });
  const [summary, setSummary] = useState<Summary | null>(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  // reload both lists from the database
  async function load() {
    setProducts(await call("GET", "/products"));
    setCart(await call("GET", "/cart"));
  }

  useEffect(() => {
    load();
  }, []);

  // run an API call, show the error if any, then refresh. Returns undefined on failure.
  async function act(method: string, path: string, body?: object) {
    setError("");
    setMessage("");
    try {
      return await call(method, path, body);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      load();
    }
  }

  async function addToCart(p: Product) {
    const ok = await act("POST", "/cart", { productId: p.productId, quantity: 1 });
    if (ok) setMessage(`Added ${p.productName} to cart`);
  }

  async function checkout() {
    const result = await act("POST", "/cart/checkout");
    if (result) setSummary(result);
  }

  // after checkout: show the summary
  if (summary) {
    return (
      <>
        <div className="header">Stock Shop</div>
        <div className="layout" style={{ gridTemplateColumns: "1fr" }}>
          <div className="card">
            <h2>Order summary</h2>
            <table>
              <thead>
                <tr><th>Product</th><th>Price</th><th>Qty</th><th>Total</th></tr>
              </thead>
              <tbody>
                {summary.items.map((i) => (
                  <tr key={i.cartItemId}>
                    <td>{i.productName}</td>
                    <td>{money(i.pricePerUnit)}</td>
                    <td>{i.quantity}</td>
                    <td>{money(i.lineTotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="total"><span>Items sold: {summary.totalQuantity}</span><span>Total sale: {money(summary.totalSale)}</span></div>
            <div className="actions"><button className="primary" onClick={() => setSummary(null)}>OK</button></div>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <div className="header">Stock Shop</div>
      <div className="layout">
        {/* LEFT: products */}
        <div className="card">
          <h2>Products</h2>
          {error && <div className="msg error">{error}</div>}
          {message && <div className="msg success">{message}</div>}
          <table>
            <thead>
              <tr><th>ID</th><th>Name</th><th>Price/unit</th><th>Stock left</th><th></th></tr>
            </thead>
            <tbody>
              {products.map((p) => (
                <tr key={p.productId}>
                  <td>{p.productId}</td>
                  <td>{p.productName}</td>
                  <td>{money(p.pricePerUnit)}</td>
                  <td>{p.quantityLeft > 0 ? p.quantityLeft : <span className="badge">Out of stock</span>}</td>
                  <td>
                    <button className="primary" disabled={p.quantityLeft === 0} onClick={() => addToCart(p)}>
                      Add to cart
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* RIGHT: cart */}
        <div className="card">
          <h2>Cart</h2>
          {cart.items.length === 0 ? (
            <p className="empty">Your cart is empty</p>
          ) : (
            <>
              <table>
                <thead>
                  <tr><th>Product</th><th>Qty</th><th>Total</th><th></th></tr>
                </thead>
                <tbody>
                  {cart.items.map((i) => (
                    <tr key={i.cartItemId}>
                      <td>{i.productName}</td>
                      <td>
                        <button onClick={() => act("PUT", `/cart/${i.cartItemId}`, { quantity: i.quantity - 1 })}>-</button>
                        {" "}{i.quantity}{" "}
                        <button onClick={() => act("PUT", `/cart/${i.cartItemId}`, { quantity: i.quantity + 1 })}>+</button>
                      </td>
                      <td>{money(i.lineTotal)}</td>
                      <td><button className="danger" onClick={() => act("DELETE", `/cart/${i.cartItemId}`)}>✕</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <div className="total"><span>Total</span><span>{money(cart.total)}</span></div>
              <div className="actions">
                <button className="danger" onClick={() => act("DELETE", "/cart")}>Delete all</button>
                <button className="primary" onClick={checkout}>Check out</button>
              </div>
            </>
          )}
        </div>
      </div>
    </>
  );
}