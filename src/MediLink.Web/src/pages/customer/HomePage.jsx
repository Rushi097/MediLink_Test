import { useEffect, useState } from "react";
import {
  FaMagnifyingGlass,
  FaPills,
  FaShieldHeart,
  FaTruckMedical,
} from "react-icons/fa6";
import { money } from "../../utils/formatters";
import { apiUrl } from "../../services/api";
import axios from "axios";
import { getRegisteredStores } from "../../services/medicalStoreService";

export default function HomePage({ add, selectedStore, onSelectStore, cartItems = [] }) {
  const [products, setProducts] = useState([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [stores, setStores] = useState([]);
  const [storesLoading, setStoresLoading] = useState(true);
  const [storeError, setStoreError] = useState("");

  useEffect(() => {
    let cancelled = false;
    getRegisteredStores()
      .then(({ data }) => {
        if (cancelled) return;
        const items = data.items || [];
        setStores(items);
        if (selectedStore?.id) {
          const fresh = items.find((store) => store.id === selectedStore.id);
          if (fresh && JSON.stringify(fresh) !== JSON.stringify(selectedStore)) {
            onSelectStore?.(fresh);
          }
        }
      })
      .catch(() => !cancelled && setStoreError("Could not load medical stores."))
      .finally(() => !cancelled && setStoresLoading(false));
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    const timer = setTimeout(async () => {
      setLoading(true);
      setError("");
      try {
        if (!selectedStore?.id) {
          setProducts([]);
          return;
        }
        const { data } = await axios.get(`${apiUrl}/medicines`, {
          params: {
            search: query.trim() || undefined,
            storeId: selectedStore.id,
            pageSize: 24,
          },
        });
        setProducts(data.items || []);
      } catch {
        setError("Could not load the medicine catalogue.");
        setProducts([]);
      } finally {
        setLoading(false);
      }
    }, query.trim() ? 350 : 0);

    return () => clearTimeout(timer);
  }, [query, selectedStore?.id]);

  const categories = [
    ["Fever & pain", "💊"],
    ["Allergy care", "🌿"],
    ["Vitamins", "🍊"],
    ["Wellness", "💧"],
    ["Digestive care", "✨"],
    ["Diabetes care", "🩸"],
  ];

  return (
    <main>
      <section className="hero">
        <div className="hero-copy">
          <span className="eyebrow">TRUSTED LOCAL HEALTHCARE</span>
          <h1>
            Care that comes
            <br />
            <em>closer to you.</em>
          </h1>
          <p>
            Search a standardized medicine catalogue and compare availability
            and prices from registered medical stores.
          </p>

          <div className="search">
            <FaMagnifyingGlass />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Search medicine by name, e.g. paracetamol"
            />
            <button type="button">Search</button>
          </div>

          <div className="trust">
            <span>✓ Standardized medicine data</span>
            <span>✓ Registered pharmacies</span>
            <span>✓ Store-specific prices</span>
          </div>
        </div>

        <div className="hero-art">
          <div className="cross">+</div>
          <div className="art-card card-one">
            💊<small>Medicine catalogue</small>
          </div>
          <div className="art-card card-two">
            🩺<small>Verified information</small>
          </div>
          <div className="art-card card-three">
            🛍️<small>Compare stores</small>
          </div>
        </div>
      </section>

      <section className="store-choice">
        <div>
          <span className="eyebrow">STEP 1 · CHOOSE YOUR PHARMACY</span>
          <h2>Select a medical store before shopping</h2>
          <p>All medicines you add to this cart will be ordered from the selected store. The order notification will be sent only to that store for acceptance.</p>
        </div>
        <div className="store-choice-control">
          <label htmlFor="customer-store">Medical store</label>
          <select
            id="customer-store"
            value={selectedStore?.id || ""}
            disabled={storesLoading || !stores.length}
            onChange={(event) => {
              const store = stores.find((item) => item.id === event.target.value);
              if (store) onSelectStore?.(store);
            }}
          >
            <option value="">{storesLoading ? "Loading stores…" : "Choose a medical store"}</option>
            {stores.map((store) => (
              <option key={store.id} value={store.id}>
                {store.name} — {store.address}
              </option>
            ))}
          </select>
          {selectedStore && (
            <small>Selected: <strong>{selectedStore.name}</strong> · {selectedStore.address}</small>
          )}
          {storeError && <span className="form-error">{storeError}</span>}
        </div>
      </section>

      <section className="benefits">
        <div>
          <FaShieldHeart />
          <b>Reference-backed</b>
          <span>Medicine metadata from trusted public sources</span>
        </div>
        <div>
          <FaTruckMedical />
          <b>Local stores</b>
          <span>See medicines stocked by registered pharmacies</span>
        </div>
        <div>
          <FaPills />
          <b>Live inventory</b>
          <span>Price and stock are controlled by each store</span>
        </div>
      </section>

      <section className="catalog">
        <div className="section-heading">
          <div>
            <span className="eyebrow">QUICK SEARCH</span>
            <h2>Find what you need</h2>
          </div>
        </div>

        <div className="categories">
          {categories.map(([name, icon]) => (
            <button key={name} onClick={() => setQuery(name)}>
              <span>{icon}</span>
              {name}
            </button>
          ))}
        </div>
      </section>

      <section className="catalog" id="medicines">
        <div className="section-heading">
          <div>
            <span className="eyebrow">MEDICINE CATALOGUE</span>
            <h2>{query ? "Search results" : "Available medicines"}</h2>
          {selectedStore && <small className="selected-store-badge">Shopping from: {selectedStore.name}</small>}
          </div>
          {query && (
            <button className="plain-button" onClick={() => setQuery("")}>
              View all
            </button>
          )}
        </div>

        {!selectedStore ? (
          <div className="store-required">
            <strong>Choose a medical store to see its live inventory.</strong>
            <span>Price, stock and the pharmacy receiving your order are all tied to your selected store.</span>
          </div>
        ) : query && (
          <p className="search-note">
            Medicine name, image and description come from the catalogue.
            Store price and stock come from MediLink inventory for the selected pharmacy.
          </p>
        )}

        {error && <p className="form-error">{error}</p>}

        <div className="products">
          {products.map((medicine, index) => {
            const available =
              selectedStore?.id &&
              String(medicine.storeId || "").toLowerCase() === String(selectedStore.id || "").toLowerCase() &&
              medicine.storeId !== "00000000-0000-0000-0000-000000000000" &&
              medicine.stockQuantity > 0;

            return (
              <article
                className={`product ${available ? "" : "unavailable"}`}
                key={`${medicine.externalMedicineId}-${medicine.storeId}-${index}`}
              >
                <div className="product-image">
                  {medicine.imageUrl ? (
                    <img
                      src={medicine.imageUrl}
                      alt={medicine.name}
                      loading="lazy"
                      onError={(event) => {
                        event.currentTarget.onerror = null;
                        event.currentTarget.src = `http://localhost:5201/api/medicines/catalog/placeholder?name=${encodeURIComponent(medicine.name || "Medicine")}`;
                      }}
                    />
                  ) : (
                    <div className="product-placeholder">💊</div>
                  )}
                </div>

                <div className="product-details">
                  <p>{medicine.category}</p>
                  <h3>{medicine.name}</h3>
                  <small>{medicine.description}</small>
                  {medicine.storeName && (
                    <small>
                      <strong>Store:</strong> {medicine.storeName}
                    </small>
                  )}
                </div>

                <div className="price">
                  <b>
                    {available ? money(medicine.price) : "Not stocked locally"}
                  </b>
                  {medicine.storeName && (
                    <span>{medicine.stockQuantity} in stock</span>
                  )}
                  <button type="button" disabled={!available || !selectedStore} onClick={() => add(medicine)}>
                    {available ? "Add +" : selectedStore ? "Unavailable" : "Choose store first"}
                  </button>
                </div>
              </article>
            );
          })}
        </div>

        {loading && <p className="empty">Searching medicine catalogue…</p>}
        {!loading && !products.length && (
          <p className="empty">
            No medicine result found. Try a generic or brand name.
          </p>
        )}
      </section>

      <section className="steps" id="how">
        <span className="eyebrow">HOW MEDILINK WORKS</span>
        <h2>Healthcare in three easy steps</h2>
        <div>
          <article>
            <b>1</b>
            <h3>Search & select</h3>
            <p>Find a medicine by name from the standardized catalogue.</p>
          </article>
          <article>
            <b>2</b>
            <h3>Compare stores</h3>
            <p>See the price and stock supplied by each registered store.</p>
          </article>
          <article>
            <b>3</b>
            <h3>Order securely</h3>
            <p>Checkout reserves stock from the selected medical store.</p>
          </article>
        </div>
      </section>
    </main>
  );
}
