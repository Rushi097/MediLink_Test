import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { client, user } from "../../services/api";
import { money } from "../../utils/formatters";

export default function CartPage({ items, setItems }) {
  const account = user();
  const nav = useNavigate();
  const [address, setAddress] = useState("");
  const [message, setMessage] = useState("");

  const total = useMemo(
    () => items.reduce((sum, item) => sum + item.price * item.quantity, 0),
    [items],
  );

  const checkout = async () => {
    if (!account) return nav("/login");
    if (!address.trim())
      return setMessage("Enter a delivery address to continue.");

    try {
      await client().post("/orders", { deliveryAddress: address });
      setItems([]);
      setMessage("Order placed successfully. You can track it in My portal.");
    } catch (error) {
      setMessage(
        error.response?.data?.message ||
          "Checkout failed. Ensure items are in your server cart.",
      );
    }
  };

  return (
    <main className="page-wrap">
      <section className="cart-page">
        <div>
          <span className="eyebrow">YOUR ORDER</span>
          <h1>Shopping cart</h1>

          {items.length ? (
            items.map((item) => (
              <div className="cart-row" key={item.id}>
                <span>💊</span>
                <div>
                  <b>{item.name}</b>
                  <small>{money(item.price)} each</small>
                </div>
                <div className="quantity">
                  <button
                    onClick={() =>
                      setItems(
                        items.map((x) =>
                          x.id === item.id
                            ? { ...x, quantity: Math.max(1, x.quantity - 1) }
                            : x,
                        ),
                      )
                    }
                  >
                    −
                  </button>
                  {item.quantity}
                  <button
                    onClick={() =>
                      setItems(
                        items.map((x) =>
                          x.id === item.id
                            ? { ...x, quantity: x.quantity + 1 }
                            : x,
                        ),
                      )
                    }
                  >
                    +
                  </button>
                </div>
                <button
                  className="remove"
                  onClick={() =>
                    setItems(items.filter((x) => x.id !== item.id))
                  }
                >
                  Remove
                </button>
              </div>
            ))
          ) : (
            <p className="empty">
              Your cart is empty. <Link to="/">Explore medicines</Link>
            </p>
          )}
        </div>

        <aside className="order-summary">
          <h3>Order summary</h3>
          <div>
            <span>Items total</span>
            <b>{money(total)}</b>
          </div>
          <div>
            <span>Delivery</span>
            <b>Calculated at checkout</b>
          </div>
          <hr />
          <div className="total">
            Total <b>{money(total)}</b>
          </div>
          <label>
            Delivery address
            <textarea
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              placeholder="House / street / landmark / PIN code"
            />
          </label>
          {message && <p className="form-message">{message}</p>}
          <button
            className="checkout"
            disabled={!items.length}
            onClick={checkout}
          >
            {account ? "Place secure order" : "Sign in to checkout"}
          </button>
        </aside>
      </section>
    </main>
  );
}
