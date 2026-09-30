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

export default function MedicalStoreCatalog({ add, selectedStore: globalSelectedStore, onSelectStore, cartItems = [] }) {
  const account = user();
  const [stores, setStores] = useState([]);
  const [selectedStore, setSelectedStore] = useState(globalSelectedStore || null);
  const [inventory, setInventory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadingInventory, setLoadingInventory] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (account?.role === "StoreOwner") {
      return;
    }

    const loadStores = async () => {
      setLoading(true);
      try {
        const { data } = await getRegisteredStores();
        const items = data.items || [];
        setStores(items);

        if (items.length) {
          const cartStoreId = cartItems[0]?.storeId;
          const preferred = (cartStoreId && items.find((store) => store.id === cartStoreId))
            || (globalSelectedStore && items.find((store) => store.id === globalSelectedStore.id));

          // Never silently choose a pharmacy for a new cart. A customer must
          // explicitly select the medical store before medicines can be added.
          if (preferred) {
            setSelectedStore(preferred);
            const inventoryResponse = await getStoreInventory(preferred.id);
            setInventory(inventoryResponse.data.items || []);
          } else {
            setSelectedStore(null);
            setInventory([]);
          }
        }
      } catch {
        setError("Could not load registered medical stores.");
      } finally {
        setLoading(false);
      }
    };

    loadStores();
  }, [account?.role]);

  const loadInventory = async (storeId) => {
    const match = stores.find((store) => store.id === storeId) || null;
    if (!match) return;
    if (onSelectStore?.(match) === false) return;
    setSelectedStore(match);
    setLoadingInventory(true);
    try {
      const { data } = await getStoreInventory(storeId);
      setInventory(data.items || []);
    } catch {
      setInventory([]);
    } finally {
      setLoadingInventory(false);
    }
  };

  if (account?.role === "StoreOwner") {
    return (
      <main className="page-wrap">
        <section className="portal-panel">
          <span className="eyebrow">STORE OWNER</span>
          <h2>Manage your inventory from My portal</h2>
          <p>
            Use <strong>My portal</strong> to search the medicine catalogue and
            add medicines with your store-specific price and stock.
          </p>
        </section>
      </main>
    );
  }

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
                            <b>
                              {item.stockQuantity > 0
                                ? money(item.price)
                                : "Out of stock"}
                            </b>
                            <span>{item.stockQuantity} in stock</span>
                            <button
                              disabled={item.stockQuantity <= 0}
                              type="button"
                              onClick={() => add?.({
                                ...item,
                                id: item.medicineId || item.id,
                                storeId: item.storeId || selectedStore.id,
                                storeName: selectedStore.name,
                              })}
                            >
                              {item.stockQuantity > 0 ? "Add to cart" : "Unavailable"}
                            </button>
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
