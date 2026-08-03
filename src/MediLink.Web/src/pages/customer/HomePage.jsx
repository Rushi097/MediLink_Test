import { useEffect, useMemo, useState } from "react";
import {
  FaMagnifyingGlass,
  FaPills,
  FaShieldHeart,
  FaTruckMedical,
} from "react-icons/fa6";
import { money } from "../../utils/formatters";
import { apiUrl } from "../../services/api";
import axios from "axios";

const medicineReference = [
  ["Dolo 650", "Fever & pain", "Paracetamol tablet for fever and mild pain."],
  ["Crocin", "Fever & pain", "Paracetamol medicine for fever and pain relief."],
  [
    "Combiflam",
    "Fever & pain",
    "Ibuprofen and paracetamol pain-relief medicine.",
  ],
  ["Cetirizine", "Allergy care", "Antihistamine for allergy symptoms."],
  ["Allegra", "Allergy care", "Antihistamine for seasonal allergies."],
  [
    "Azithromycin",
    "Prescription medicine",
    "Antibiotic; use only with a clinician prescription.",
  ],
  [
    "Amoxicillin",
    "Prescription medicine",
    "Antibiotic; use only with a clinician prescription.",
  ],
  [
    "Pantoprazole",
    "Digestive care",
    "Acid-reducing medicine for gastric symptoms.",
  ],
  ["Gelusil", "Digestive care", "Antacid for acidity and indigestion."],
  ["Benadryl", "Cough & cold", "Cough and cold relief product."],
  ["ORS", "Wellness", "Oral rehydration salts for hydration support."],
  ["Vitamin D3", "Vitamins", "Vitamin D nutritional supplement."],
  ["Becosules", "Vitamins", "Vitamin B-complex nutritional supplement."],
  [
    "Metformin",
    "Diabetes care",
    "Prescription medicine used in diabetes management.",
  ],
  [
    "Amlodipine",
    "Heart care",
    "Prescription medicine used for blood-pressure management.",
  ],
  ["Volini", "Personal care", "Topical pain-relief product."],
].map(([name, category, description]) => ({ name, category, description }));

export default function HomePage({ add }) {
  const [inventory, setInventory] = useState([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const timer = setTimeout(async () => {
      setLoading(true);
      try {
        const { data } = await axios.get(`${apiUrl}/medicines`, {
          params: { pageSize: 50 },
        });
        setInventory(data.items || []);
      } finally {
        setLoading(false);
      }
    }, 220);

    return () => clearTimeout(timer);
  }, []);

  const products = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    const terms = normalized.split(/\s+/).filter(Boolean);
    const matches = (item) =>
      !terms.length ||
      terms.every((term) =>
        `${item.name} ${item.category} ${item.description}`
          .toLowerCase()
          .includes(term),
      );

    const inventoryByName = new Map(
      inventory.map((item) => [item.name.trim().toLowerCase(), item]),
    );

    const referenceMatches = normalized
      ? medicineReference.filter(matches)
      : [];
    const inventoryMatches = inventory.filter(matches);
    const unavailable = referenceMatches
      .filter((item) => !inventoryByName.has(item.name.toLowerCase()))
      .map((item) => ({
        ...item,
        id: `reference-${item.name}`,
        stockQuantity: 0,
        unavailable: true,
      }));

    const results = [...inventoryMatches, ...unavailable];

    return results.length || !normalized
      ? results
      : [
          {
            id: `reference-${normalized}`,
            name: query.trim(),
            category: "Medicine search",
            description:
              "This medicine is not currently listed in the MediLink reference catalogue or local inventory.",
            stockQuantity: 0,
            unavailable: true,
          },
        ];
  }, [inventory, query]);

  const categories = [
    "Fever & pain",
    "Allergy care",
    "Vitamins",
    "Wellness",
    "Personal care",
    "Diabetes care",
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
            Discover genuine medicines and wellness essentials from licensed
            pharmacies, delivered to your doorstep.
          </p>
          <div className="search">
            <FaMagnifyingGlass />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Search medicines, brands or health products"
            />
            <button>Search</button>
          </div>
          <div className="trust">
            <span>✓ Genuine products</span>
            <span>✓ Licensed pharmacies</span>
            <span>✓ Secure ordering</span>
          </div>
        </div>

        <div className="hero-art">
          <div className="cross">+</div>
          <div className="art-card card-one">
            💊<small>Verified medicines</small>
          </div>
          <div className="art-card card-two">
            🩺<small>Expert care</small>
          </div>
          <div className="art-card card-three">
            🛍️<small>Easy ordering</small>
          </div>
        </div>
      </section>

      <section className="benefits">
        <div>
          <FaShieldHeart />
          <b>100% genuine</b>
          <span>Sourced from verified sellers</span>
        </div>
        <div>
          <FaTruckMedical />
          <b>Fast delivery</b>
          <span>At your doorstep, on time</span>
        </div>
        <div>
          <FaPills />
          <b>Healthcare range</b>
          <span>Everyday needs in one place</span>
        </div>
      </section>

      <section className="catalog">
        <div className="section-heading">
          <div>
            <span className="eyebrow">SHOP BY CATEGORY</span>
            <h2>Find what you need</h2>
          </div>
        </div>
        <div className="categories">
          {categories.map((x, i) => (
            <button key={x} onClick={() => setQuery(x)}>
              <span>{["💊", "🌿", "🍊", "💧", "✨", "🩸"][i]}</span>
              {x}
            </button>
          ))}
        </div>
      </section>

      <section className="catalog" id="medicines">
        <div className="section-heading">
          <div>
            <span className="eyebrow">MEDICINE CATALOGUE</span>
            <h2>
              {query ? "Medicine search results" : "Healthcare essentials"}
            </h2>
          </div>
          <button className="plain-button" onClick={() => setQuery("")}>
            View all
          </button>
        </div>

        {query && (
          <p className="search-note">
            Searching MediLink inventory and the medicine reference catalogue.
            Only medicines marked <b>Available</b> can be ordered.
          </p>
        )}

        <div className="products">
          {products.map((m) => {
            const available = m.stockQuantity > 0;
            return (
              <article
                className={`product ${available ? "" : "unavailable"}`}
                key={m.id}
              >
                <div className="product-icon blue">
                  💊
                  <span
                    className={
                      available
                        ? "availability available"
                        : "availability unavailable"
                    }
                  >
                    {available ? "Available" : "Unavailable"}
                  </span>
                </div>
                <p>{m.category}</p>
                <h3>{m.name}</h3>
                <small>{m.description}</small>
                <div className="price">
                  <b>{available ? money(m.price) : "Not stocked"}</b>
                  <button disabled={!available} onClick={() => add(m)}>
                    {available ? "Add +" : "Unavailable"}
                  </button>
                </div>
              </article>
            );
          })}
        </div>

        {loading && <p className="empty">Loading medicines…</p>}
        {!loading && !products.length && (
          <p className="empty">
            No matching medicine was found in the catalogue.
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
            <p>Find products from a trusted catalogue.</p>
          </article>
          <article>
            <b>2</b>
            <h3>Upload prescription</h3>
            <p>Provide a valid prescription where it is required.</p>
          </article>
          <article>
            <b>3</b>
            <h3>Track delivery</h3>
            <p>Follow your order from confirmation to doorstep.</p>
          </article>
        </div>
      </section>
    </main>
  );
}
