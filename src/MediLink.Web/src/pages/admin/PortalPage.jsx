import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { client, user } from "../../services/api";
import { money } from "../../utils/formatters";
import RoleLanding from "../../components/RoleLanding";

function InventoryManager() {
  const [store, setStore] = useState(null);
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [selected, setSelected] = useState(null);
  const [price, setPrice] = useState("");
  const [stockQuantity, setStockQuantity] = useState("");
  const [inventory, setInventory] = useState([]);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [loadingSearch, setLoadingSearch] = useState(false);
  const [saving, setSaving] = useState(false);

  const loadStore = async () => {
    const { data } = await client().get("/stores/owner/me");
    setStore(data.item);
    const inventoryResponse = await client().get(
      `/stores/${data.item.id}/inventory`,
    );
    setInventory(inventoryResponse.data.items || []);
  };

  // The effect intentionally synchronizes initial server state into this portal.
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    loadStore().catch(() =>
      setError("Could not load your medical store. Please sign in again."),
    );
  }, []);
  /* eslint-enable react-hooks/set-state-in-effect */

  const search = async () => {
    if (query.trim().length < 2) {
      setError("Enter at least 2 characters to search.");
      return;
    }

    setLoadingSearch(true);
    setError("");
    setMessage("");
    try {
      const { data } = await client().get("/medicines/catalog/search", {
        params: { name: query.trim() },
      });
      setResults(data.items || []);
      if (!data.items?.length) setMessage("No medicine was found.");
    } catch (requestError) {
      setResults([]);
      setError(
        requestError.response?.data?.message ||
          "Could not search the medicine catalogue.",
      );
    } finally {
      setLoadingSearch(false);
    }
  };

  const selectMedicine = (medicine) => {
    setSelected(medicine);
    setPrice("");
    setStockQuantity("");
    setMessage("");
    setError("");
  };

  const addToInventory = async (event) => {
    event.preventDefault();
    if (!store || !selected) return;

    setSaving(true);
    setError("");
    setMessage("");

    try {
      await client().post(`/stores/${store.id}/inventory`, {
        externalMedicineId: selected.externalId,
        price: Number(price),
        stockQuantity: Number(stockQuantity),
      });

      setMessage(
        `${selected.displayName} was added. You control only this store's price and stock.`,
      );
      setSelected(null);
      setPrice("");
      setStockQuantity("");
      await loadStore();
    } catch (requestError) {
      setError(
        requestError.response?.data?.message ||
          "Could not add the medicine to your inventory.",
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="portal-panel inventory-form">
      <span className="eyebrow">STORE INVENTORY</span>
      <h2>Add medicine to your store</h2>
      <p>
        Search the MediLink medicine catalogue by name. Medicine name,
        description and image are supplied automatically. You enter only your
        store's price and stock.
      </p>

      {error && <p className="form-error">{error}</p>}
      {message && <p className="form-message">{message}</p>}

      <div className="search" style={{ margin: "18px 0" }}>
        <span>💊</span>
        <input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => event.key === "Enter" && search()}
          placeholder="Search medicine name, e.g. paracetamol"
        />
        <button type="button" onClick={search} disabled={loadingSearch}>
          {loadingSearch ? "Searching…" : "Search"}
        </button>
      </div>

      {results.length > 0 && (
        <div className="portal-panel" style={{ marginBottom: "18px" }}>
          <h3>Select a medicine</h3>
          <div className="products">
            {results.map((medicine) => (
              <button
                type="button"
                key={medicine.externalId}
                onClick={() => selectMedicine(medicine)}
                style={{
                  textAlign: "left",
                  border:
                    selected?.externalId === medicine.externalId
                      ? "2px solid #168f87"
                      : "1px solid #d9e7f3",
                  borderRadius: "12px",
                  padding: "12px",
                  background: "#fff",
                  cursor: "pointer",
                }}
              >
                <div className="product-image">
                  {medicine.imageUrl ? (
                    <img src={medicine.imageUrl} alt={medicine.displayName} />
                  ) : (
                    <div className="product-placeholder">💊</div>
                  )}
                </div>
                <strong>{medicine.displayName}</strong>
                <small style={{ display: "block", marginTop: "6px" }}>
                  {medicine.description}
                </small>
              </button>
            ))}
          </div>
        </div>
      )}

      {selected && (
        <form onSubmit={addToInventory}>
          <div className="portal-panel">
            <div style={{ display: "flex", gap: "16px", alignItems: "center" }}>
              <div className="product-image" style={{ width: "100px" }}>
                {selected.imageUrl ? (
                  <img src={selected.imageUrl} alt={selected.displayName} />
                ) : (
                  <div className="product-placeholder">💊</div>
                )}
              </div>
              <div>
                <span className="eyebrow">SELECTED MEDICINE</span>
                <h3>{selected.displayName}</h3>
                <p>{selected.description}</p>
              </div>
            </div>

            <div className="two">
              <label>
                Your store price (₹)
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={price}
                  required
                  onChange={(event) => setPrice(event.target.value)}
                />
              </label>
              <label>
                Stock quantity
                <input
                  type="number"
                  min="0"
                  value={stockQuantity}
                  required
                  onChange={(event) => setStockQuantity(event.target.value)}
                />
              </label>
            </div>

            <button className="primary inline" disabled={saving}>
              {saving ? "Adding…" : "Add to my inventory"}
            </button>
          </div>
        </form>
      )}

      <div style={{ marginTop: "30px" }}>
        <h3>My current inventory</h3>
        {inventory.length ? (
          <div className="products">
            {inventory.map((item) => (
              <article className="product" key={item.id}>
                <div className="product-image">
                  {item.imageUrl ? (
                    <img src={item.imageUrl} alt={item.name} />
                  ) : (
                    <div className="product-placeholder">💊</div>
                  )}
                </div>
                <div className="product-details">
                  <p>{item.category}</p>
                  <h3>{item.name}</h3>
                  <small>{item.description}</small>
                </div>
                <div className="price">
                  <b>{money(item.price)}</b>
                  <span>{item.stockQuantity} in stock</span>
                </div>
              </article>
            ))}
          </div>
        ) : (
          <p className="empty">
            Your inventory is empty. Search for a medicine above to add one.
          </p>
        )}
      </div>
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
  /* eslint-disable react-hooks/exhaustive-deps */
  }, [account?.role]);
  /* eslint-enable react-hooks/exhaustive-deps */

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
          <div className="section-heading"><h2>Recent orders</h2><Link className="plain-button" to="/orders">View all orders</Link></div>
          {orders.length ? (
            <div className="orders">
              {orders.map((order) => (
                <div key={order.id}>
                  <span>#{order.id.slice(0, 8)}</span>
                  <b>{money(order.totalAmount)}</b>
                  <em className={`status status-${order.status}`}>
                    {{
                    0: "Waiting for pharmacy", 1: "Accepted", 2: "Preparing", 3: "Delivered", 4: "Cancelled", 5: "Ready for delivery", 6: "Out for delivery", 7: "Rejected"
                  }[order.status] || order.status}
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

      {account.role === "StoreOwner" && (
        <>
          <div style={{ margin: "12px 0 20px" }}>
            <a
              className="primary inline"
              href="http://localhost:8081/login"
              target="_blank"
              rel="noreferrer"
            >
              Open Java Store Portal
            </a>
          </div>
          <InventoryManager />
        </>
      )}

      {account.role === "Admin" && (
        <section className="portal-panel">
          <h2>Administration</h2>
          <p>
            Medicine master data is now supplied by the external catalogue.
            Store-specific price and stock remain inside the Inventory service.
          </p>
          <a
            className="primary inline"
            href="http://localhost:5140/swagger"
            target="_blank"
            rel="noreferrer"
          >
            Open API console
          </a>
        </section>
      )}
    </main>
  );
}
