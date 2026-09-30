import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { client, user } from "../../services/api";
import { money } from "../../utils/formatters";

const isCustomer = (account) => String(account?.role || "").toLowerCase() === "customer";
const sameId = (a, b) => String(a || "").toLowerCase() === String(b || "").toLowerCase();

const normalize = (serverItems, previous = []) =>
  (serverItems || []).map((item) => {
    const old = previous.find(
      (x) => x.id === item.medicineId && x.storeId === item.storeId,
    );
    return {
      ...item,
      id: item.medicineId,
      cartItemId: item.id,
      price: Number(item.price || 0),
      storeName: item.storeName || old?.storeName || "Selected medical store",
    };
  });

export default function CartPage({ items, setItems, selectedStore, onStartNewOrder }) {
  const account = user();
  const nav = useNavigate();
  const [address, setAddress] = useState(account?.deliveryAddress || "");
  const [message, setMessage] = useState("");
  const [busyId, setBusyId] = useState("");
  const [checkingOut, setCheckingOut] = useState(false);

  useEffect(() => {
    if (account?.deliveryAddress) setAddress(account.deliveryAddress);
  }, [account?.userId, account?.deliveryAddress]);

  const total = useMemo(
    () => items.reduce((sum, item) => sum + Number(item.price || 0) * Number(item.quantity || 0), 0),
    [items],
  );
  const storeName = items[0]?.storeName || "Selected medical store";

  const changeQuantity = async (item, quantity) => {
    const next = Math.max(1, quantity);
    if (!account) {
      setItems((current) =>
        current.map((x) =>
          x.id === item.id && x.storeId === item.storeId
            ? { ...x, quantity: next }
            : x,
        ),
      );
      return;
    }

    setBusyId(item.cartItemId || item.id);
    try {
      const { data } = await client().put(
        `/cart/items/${item.cartItemId || item.id}`,
        {
          medicineId: item.id,
          storeId: item.storeId,
          quantity: next,
        },
      );
      setItems((current) => normalize(data.cart?.items || [], current));
      setMessage("");
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not update quantity.");
    } finally {
      setBusyId("");
    }
  };

  const remove = async (item) => {
    if (!account) {
      setItems((current) =>
        current.filter(
          (x) => !(x.id === item.id && x.storeId === item.storeId),
        ),
      );
      return;
    }

    setBusyId(item.cartItemId || item.id);
    try {
      const { data } = await client().delete(
        `/cart/items/${item.cartItemId || item.id}`,
      );
      // DELETE intentionally returns the authoritative cart as well, so the
      // UI never guesses what the server contains.
      if (data.cart) {
        setItems((current) => normalize(data.cart.items || [], current));
      } else {
        setItems((current) =>
          current.filter((x) => x.cartItemId !== item.cartItemId),
        );
      }
      setMessage("");
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not remove the item.");
    } finally {
      setBusyId("");
    }
  };

  const checkout = async () => {
    if (!account) return nav("/login");
    if (!isCustomer(account)) {
      return setMessage("Only customer accounts can place orders.");
    }
    if (!address.trim()) return setMessage("Enter a delivery address to continue.");
    if (!items.length) return;
    const storeIds = [...new Set(items.map((x) => x.storeId))];
    if (storeIds.length !== 1) {
      return setMessage("Checkout supports one medical store at a time.");
    }
    if (!selectedStore?.id || !sameId(selectedStore.id, storeIds[0])) {
      return setMessage("Please select the same medical store before placing this order.");
    }

    setCheckingOut(true);
    setMessage("");
    try {
      await client().post("/orders/checkout", {
        deliveryAddress: address.trim(),
        paymentMethod: "CashOnDelivery",
        storeId: selectedStore.id,
      });

      // Checkout consumes the current cart. Start a fresh empty cart while
      // keeping the authenticated cart owner and selected medical store.
      // The server Cart row remains owned by this customer; only its items
      // were consumed by checkout.
      setItems([]);
      localStorage.setItem("medilink-cart", "[]");
      onStartNewOrder?.();
      setMessage("Order placed. The pharmacy has received it for acceptance.");
      window.setTimeout(() => nav("/orders"), 500);
    } catch (error) {
      setMessage(
        error.response?.data?.message ||
          "Checkout failed. Your cart was not cleared.",
      );
    } finally {
      setCheckingOut(false);
    }
  };

  return (
    <main className="page-wrap">
      <section className="cart-page">
        <div>
          <span className="eyebrow">YOUR ORDER</span>
          <h1>Shopping cart</h1>
          {items.length ? (
            <>
              <div className="portal-panel" style={{ marginBottom: "18px" }}>
                <strong>Medical store</strong>
                <p>{storeName}</p>
                {selectedStore?.address && <small className="selected-store-address">{selectedStore.address}</small>}
                <small>
                  One checkout is limited to this store. Stock is not reserved
                  until you place the order.
                </small>
              </div>
              {items.map((item) => {
                const busy = busyId === (item.cartItemId || item.id);
                return (
                  <div className="cart-row" key={`${item.id}-${item.storeId}`}>
                    <span>💊</span>
                    <div>
                      <b>{item.name}</b>
                      <small>{money(item.price)} each</small>
                    </div>
                    <div className="quantity">
                      <button
                        type="button"
                        disabled={busy || item.quantity <= 1}
                        onClick={() => changeQuantity(item, item.quantity - 1)}
                      >
                        −
                      </button>
                      {item.quantity}
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => changeQuantity(item, item.quantity + 1)}
                      >
                        +
                      </button>
                    </div>
                    <button
                      type="button"
                      className="remove"
                      disabled={busy}
                      onClick={() => remove(item)}
                    >
                      Remove
                    </button>
                  </div>
                );
              })}
            </>
          ) : (
            <p className="empty">
              Your cart is empty. <Link to="/medical-store">Continue shopping</Link>
            </p>
          )}
        </div>

        <aside className="order-summary">
          <h3>Checkout</h3>
          <div>
            <span>Items total</span>
            <b>{money(total)}</b>
          </div>
          <div>
            <span>Delivery</span>
            <b>Confirmed by store</b>
          </div>
          <hr />
          <div className="total">
            Total <b>{money(total)}</b>
          </div>
          <label>
            Customer name
            <input
              value={account?.fullName || ""}
              readOnly
              placeholder="Sign in to continue"
            />
          </label>
          <label>
            Phone
            <input
              value={account?.phoneNumber || ""}
              readOnly
              placeholder="Saved account phone"
            />
          </label>
          <label>
            Delivery address
            <textarea
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              placeholder="House / street / landmark / PIN code"
            />
          </label>
          <label>
            Payment method
            <select value="CashOnDelivery" disabled>
              <option value="CashOnDelivery">Cash on Delivery</option>
            </select>
          </label>
          <small>
            Payment is collected when the medicine is delivered. Online payment
            can be added later without changing the order flow.
          </small>
          {message && <p className="form-message">{message}</p>}
          <button
            type="button"
            className="checkout"
            disabled={!items.length || checkingOut}
            onClick={checkout}
          >
            {checkingOut
              ? "Placing order…"
              : account
                ? "Place order"
                : "Sign in to checkout"}
          </button>
        </aside>
      </section>
    </main>
  );
}
