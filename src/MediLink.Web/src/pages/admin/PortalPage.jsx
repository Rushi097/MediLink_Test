import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { client, user } from "../../services/api";
import { money } from "../../utils/formatters";
import RoleLanding from "../../components/RoleLanding";

function InventoryManager() {
  const [form, setForm] = useState({
    name: "",
    category: "",
    description: "",
    price: "",
    stockQuantity: "",
  });
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);

  const submit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setMessage("");

    try {
      await client().post("/medicines", {
        ...form,
        price: Number(form.price),
        stockQuantity: Number(form.stockQuantity),
      });
      setForm({
        name: "",
        category: "",
        description: "",
        price: "",
        stockQuantity: "",
      });
      setMessage(
        "Medicine saved in MySQL and is now searchable in the customer catalogue.",
      );
    } catch (requestError) {
      setMessage(
        requestError.response?.data?.message ||
          "Could not save medicine. Check every field.",
      );
    } finally {
      setSaving(false);
    }
  };

  const change = (event) =>
    setForm({ ...form, [event.target.name]: event.target.value });

  return (
    <section className="portal-panel inventory-form">
      <h2>Add medicine to catalogue</h2>
      <p>
        New products are stored in MySQL and immediately appear in customer
        search.
      </p>
      <form onSubmit={submit}>
        <div className="two">
          <label>
            Medicine name
            <input name="name" value={form.name} required onChange={change} />
          </label>
          <label>
            Category
            <input
              name="category"
              value={form.category}
              required
              onChange={change}
              placeholder="e.g. Vitamins"
            />
          </label>
        </div>
        <label>
          Description
          <input
            name="description"
            value={form.description}
            required
            onChange={change}
          />
        </label>
        <div className="two">
          <label>
            Price (₹)
            <input
              name="price"
              type="number"
              step="0.01"
              min="0.01"
              value={form.price}
              required
              onChange={change}
            />
          </label>
          <label>
            Stock quantity
            <input
              name="stockQuantity"
              type="number"
              min="0"
              value={form.stockQuantity}
              required
              onChange={change}
            />
          </label>
        </div>
        <button className="primary inline" disabled={saving}>
          {saving ? "Saving…" : "Add medicine"}
        </button>
        {message && <p className="form-message">{message}</p>}
      </form>
    </section>
  );
}

export default function PortalPage({ requiredRole }) {
  const account = user();
  const [data, setData] = useState(null);
  const [orders, setOrders] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!account) return;
    const role = account.role?.toLowerCase().replace("storeowner", "store");

    Promise.all([
      client().get(`/dashboard/${role}`),
      account.role === "Customer"
        ? client().get("/orders")
        : Promise.resolve({ data: { items: [] } }),
    ])
      .then(([dashboard, orderData]) => {
        setData(dashboard.data);
        setOrders(orderData.data.items || []);
      })
      .catch(() =>
        setError("We could not load your portal. Please sign in again."),
      );
  }, [account?.role]);

  if (!account) return <RoleLanding role={requiredRole} />;

  if (requiredRole && account.role !== requiredRole) {
    return (
      <main className="page-wrap">
        <h1>Access restricted</h1>
        <p>
          This workspace requires a{" "}
          {requiredRole === "StoreOwner"
            ? "medical-store owner"
            : "platform administrator"}{" "}
          account.
        </p>
        <Link className="primary inline" to="/portal">
          Open my portal
        </Link>
      </main>
    );
  }

  const roleName =
    account.role === "StoreOwner" ? "Medical store" : account.role;

  const metrics =
    data && account.role === "Customer"
      ? [
          ["Orders", data.orderCount],
          ["Active deliveries", data.activeOrders],
        ]
      : data && account.role === "StoreOwner"
        ? [
            ["Active products", data.activeProducts],
            ["Low stock", data.lowStock],
            ["Inventory value", money(data.inventoryValue)],
          ]
        : data
          ? [
              ["Customers", data.customers],
              ["Pharmacy owners", data.pharmacyOwners],
              ["Medicines", data.medicines],
              ["Pending orders", data.pendingOrders],
              ["Delivered revenue", money(data.deliveredRevenue)],
            ]
          : [];

  return (
    <main className="page-wrap portal">
      <span className="eyebrow">{roleName.toUpperCase()} PORTAL</span>
      <h1>Welcome back, {account.fullName?.split(" ")[0]}</h1>
      <p>Manage your MediLink activity from one place.</p>
      {error && <p className="form-error">{error}</p>}

      <section className="metric-grid">
        {metrics.map(([label, value]) => (
          <article key={label}>
            <span>{label}</span>
            <b>{value}</b>
          </article>
        ))}
      </section>

      {account.role === "Customer" && (
        <section className="portal-panel">
          <h2>Recent orders</h2>
          {orders.length ? (
            <div className="orders">
              {orders.map((order) => (
                <div key={order.id}>
                  <span>#{order.id.slice(0, 8)}</span>
                  <b>{money(order.totalAmount)}</b>
                  <em className={`status ${order.status.toLowerCase()}`}>
                    {order.status}
                  </em>
                </div>
              ))}
            </div>
          ) : (
            <p>
              No orders yet. <Link to="/">Start shopping</Link>.
            </p>
          )}
        </section>
      )}

      {account.role === "StoreOwner" && <InventoryManager />}

      {account.role === "Admin" && (
        <section className="portal-panel">
          <h2>Administration</h2>
          <p>
            Platform statistics are calculated from the MySQL database. Product
            management is available through the secured medicine APIs.
          </p>
          <a
            className="primary inline"
            href="http://localhost:5140/swagger"
            target="_blank"
          >
            Open API console
          </a>
        </section>
      )}
    </main>
  );
}
