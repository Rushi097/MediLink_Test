import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { client, user } from "../../services/api";
import { money } from "../../utils/formatters";

const labels = {
  PendingStoreAcceptance: "Waiting for pharmacy",
  Accepted: "Pharmacy accepted",
  Preparing: "Preparing your medicines",
  ReadyForDelivery: "Ready for delivery",
  OutForDelivery: "Out for delivery",
  Delivered: "Delivered",
  Rejected: "Rejected",
  Cancelled: "Cancelled",
};

const statusSteps = [
  "PendingStoreAcceptance",
  "Accepted",
  "Preparing",
  "ReadyForDelivery",
  "OutForDelivery",
  "Delivered",
];

const statusName = (status) => {
  if (typeof status === "string") return status;
  return [
    "PendingStoreAcceptance",
    "Accepted",
    "Preparing",
    "Delivered",
    "Cancelled",
    "ReadyForDelivery",
    "OutForDelivery",
    "Rejected",
  ][status] || "Unknown";
};

export default function OrdersPage() {
  const account = user();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (!account) return;
    client().get("/orders")
      .then(({ data }) => setOrders(data.items || []))
      .catch((error) => setMessage(error.response?.data?.message || "Could not load your orders."))
      .finally(() => setLoading(false));
  }, [account?.userId]);

  const cancel = async (id) => {
    try {
      const { data } = await client().put(`/orders/${id}/cancel`);
      setOrders((items) => items.map((order) => order.id === id ? data.item : order));
      setMessage("Order cancelled and reserved stock was restored.");
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not cancel this order.");
    }
  };

  if (!account) {
    return <main className="page-wrap"><section className="portal-panel"><h2>Sign in to view your orders</h2><Link className="primary inline" to="/login">Sign in</Link></section></main>;
  }

  return (
    <main className="page-wrap">
      <section className="catalog" style={{ marginTop: "2rem" }}>
        <div className="section-heading">
          <div><span className="eyebrow">ORDER TRACKING</span><h2>My orders</h2></div>
          <Link className="plain-button" to="/medical-store">Continue shopping</Link>
        </div>
        {message && <p className="form-message">{message}</p>}
        {loading ? <p className="empty">Loading your orders…</p> : !orders.length ? (
          <section className="portal-panel"><p>No orders yet. <Link to="/medical-store">Start shopping</Link>.</p></section>
        ) : orders.map((order) => {
          const status = statusName(order.status);
          const currentIndex = statusSteps.indexOf(status);
          return (
            <article className="portal-panel order-card" key={order.id} style={{ marginBottom: "18px" }}>
              <div className="order-card-head">
                <div><span className="eyebrow">ORDER #{order.id.slice(0, 8).toUpperCase()}</span><small>{new Date(order.createdAt).toLocaleString("en-IN")}</small></div>
                <strong>{money(order.totalAmount)}</strong>
              </div>
              <div className="customer-order-store">
                <div><strong>Medical store</strong><span>{order.store?.name || "Medical store"}</span></div>
                <div><strong>Store address</strong><span>{order.store?.address || "Store details unavailable"}</span></div>
              </div>
              <p><strong>Delivery:</strong> {order.deliveryAddress}</p>
              <div className="order-items-list">
                {order.items?.map((item) => <div key={item.id}><span>{item.medicineName} × {item.quantity}</span><b>{money(item.unitPrice * item.quantity)}</b></div>)}
              </div>
              {status === "Rejected" || status === "Cancelled" ? (
                <div className={`order-status-banner ${status.toLowerCase()}`}><b>{labels[status]}</b>{status === "Rejected" && <span>The store rejected the order and reserved inventory was restored.</span>}</div>
              ) : (
                <>
                  <div className="order-status-banner"><b>{labels[status]}</b><span>Your order status is updated by the medical store.</span></div>
                  <div className="status-track">
                    {statusSteps.map((step, index) => <div className={index <= currentIndex ? "active" : ""} key={step}><span>{index + 1}</span><small>{labels[step]}</small></div>)}
                  </div>
                </>
              )}
              {(status === "PendingStoreAcceptance" || status === "Accepted") && <button className="link-btn" onClick={() => cancel(order.id)}>Cancel order</button>}
            </article>
          );
        })}
      </section>
    </main>
  );
}
