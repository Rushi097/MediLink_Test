import { useEffect, useState } from "react";
import {
  getRegisteredStores,
  getStoreInventory,
} from "../../services/medicalStoreService";

const money = (value) =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(value || 0);

const user = () => JSON.parse(localStorage.getItem("medilink-user") || "null");

export default function MedicalStoreCatalog() {
  const account = user();
  if (account?.role === "StoreOwner") {
    return null;
  }

  const [stores, setStores] = useState([]);
  const [selectedStore, setSelectedStore] = useState(null);
  const [inventory, setInventory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadingInventory, setLoadingInventory] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const loadStores = async () => {
      setLoading(true);
      try {
        const { data } = await getRegisteredStores();
        const items = data.items || [];
        setStores(items);

        if (items.length) {
          setSelectedStore(items[0]);
          const inventoryResponse = await getStoreInventory(items[0].id);
          setInventory(inventoryResponse.data.items || []);
        }
      } catch {
        setError("Could not load registered medical stores.");
      } finally {
        setLoading(false);
      }
    };

    loadStores();
  }, []);

  const loadInventory = async (storeId) => {
    setLoadingInventory(true);
    try {
      const { data } = await getStoreInventory(storeId);
      setInventory(data.items || []);
      const match = stores.find((store) => store.id === storeId) || null;
      setSelectedStore(match);
    } catch {
      setInventory([]);
    } finally {
      setLoadingInventory(false);
    }
  };

  return (
    <main className="page-wrap">
      <section className="catalog" style={{ marginTop: "2rem" }}>
        <div className="section-heading">
          <div>
            <span className="eyebrow">REGISTERED PHARMACIES</span>
            <h2>Medical stores near you</h2>
          </div>
        </div>

        {error && <p className="form-error">{error}</p>}

        {loading ? (
          <p className="empty">Loading registered stores…</p>
        ) : (
          <div
            className="two-column"
            style={{
              display: "grid",
              gridTemplateColumns: "300px 1fr",
              gap: "1.25rem",
            }}
          >
            <aside className="portal-panel">
              {stores.length ? (
                stores.map((store) => (
                  <button
                    key={store.id}
                    className="plain-button"
                    style={{
                      display: "block",
                      width: "100%",
                      textAlign: "left",
                      marginBottom: "0.75rem",
                      padding: "0.9rem 1rem",
                      border:
                        selectedStore?.id === store.id
                          ? "1px solid #3182ce"
                          : "1px solid #d9e7f3",
                      borderRadius: "12px",
                      background:
                        selectedStore?.id === store.id ? "#edf7ff" : "#fff",
                    }}
                    onClick={() => loadInventory(store.id)}
                  >
                    <strong>{store.name}</strong>
                    <div style={{ color: "#60758a", fontSize: "0.9rem" }}>
                      {store.address}
                    </div>
                  </button>
                ))
              ) : (
                <p className="empty">No medical stores have registered yet.</p>
              )}
            </aside>

            <section className="portal-panel">
              {selectedStore ? (
                <>
                  <span className="eyebrow">STORE INVENTORY</span>
                  <h3>{selectedStore.name}</h3>
                  <p>{selectedStore.address}</p>

                  {loadingInventory ? (
                    <p className="empty">Loading inventory…</p>
                  ) : inventory.length ? (
                    <div className="products">
                      {inventory.map((item) => (
                        <article className="product" key={item.id}>
                          <div className="product-icon blue">
                            💊
                            <span
                              className={
                                item.stockQuantity > 0
                                  ? "availability available"
                                  : "availability unavailable"
                              }
                            >
                              {item.stockQuantity > 0
                                ? "Available"
                                : "Unavailable"}
                            </span>
                          </div>
                          <p>{item.category}</p>
                          <h3>{item.name}</h3>
                          <small>{item.description}</small>
                          <div className="price">
                            <b>
                              {item.stockQuantity > 0
                                ? money(item.price)
                                : "Out of stock"}
                            </b>
                            <span>{item.stockQuantity} in stock</span>
                          </div>
                        </article>
                      ))}
                    </div>
                  ) : (
                    <p className="empty">
                      This store has not added any inventory yet.
                    </p>
                  )}
                </>
              ) : (
                <p className="empty">Select a store to view inventory.</p>
              )}
            </section>
          </div>
        )}
      </section>
    </main>
  );
}
