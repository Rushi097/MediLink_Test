import { BrowserRouter, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";
import StoreRegister from "./pages/auth/StoreRegister";
import AdminLogin from "./pages/auth/AdminLogin";
import Header from "./components/Header";
import Footer from "./components/Footer";
import HomePage from "./pages/customer/HomePage";
import CartPage from "./pages/customer/CartPage";
import OrdersPage from "./pages/customer/OrdersPage";
import PortalPage from "./pages/admin/PortalPage";
import MedicalStoreCatalog from "./pages/seller/MedicalStoreCatalog";
import { client, user } from "./services/api";
import "./styles.css";
import "./medicine-search.css";

const CART_KEY = "medilink-cart";
const CART_OWNER_KEY = "medilink-cart-owner";
const SELECTED_STORE_KEY = "medilink-selected-store";

const accountUserId = (account) => account?.userId || account?.id || "";
const isCustomer = (account) => String(account?.role || "").toLowerCase() === "customer";
const sameId = (a, b) => String(a || "").toLowerCase() === String(b || "").toLowerCase();

function normalizeServerCart(serverItems, previousItems = []) {
  return (serverItems || []).map((item) => {
    const old = previousItems.find(
      (x) => x.id === item.medicineId && x.storeId === item.storeId,
    );
    return {
      id: item.medicineId,
      cartItemId: item.id,
      storeId: item.storeId,
      name: item.name,
      price: Number(item.price || 0),
      imageUrl: item.imageUrl,
      quantity: item.quantity,
      storeName: item.storeName || old?.storeName || "Selected medical store",
      subtotal: item.subtotal,
    };
  });
}

function Shell() {
  const navigate = useNavigate();
  const [account, setAccount] = useState(() => user());
  const [cart, setCart] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem(CART_KEY) || "[]");
    } catch {
      return [];
    }
  });
  const [notice, setNotice] = useState("");
  const [selectedStore, setSelectedStore] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem(SELECTED_STORE_KEY) || "null");
    } catch {
      return null;
    }
  });

  const notify = (text, duration = 3000) => {
    setNotice(text);
    window.clearTimeout(window.__medilinkNoticeTimer);
    window.__medilinkNoticeTimer = window.setTimeout(() => setNotice(""), duration);
  };

  const endInvalidSession = () => {
    // A restarted service or a changed signing key invalidates existing JWTs.
    // Remove the unusable credentials immediately so cart actions do not keep
    // failing with a silent-looking 401 response.
    localStorage.removeItem("medilink-token");
    localStorage.removeItem("medilink-user");
    localStorage.removeItem(CART_OWNER_KEY);
    setAccount(null);
    window.dispatchEvent(new Event("medilink-auth-changed"));
    notify("Your session has expired. Please sign in again to use your cart.", 6000);
    navigate("/login");
  };

  // Keep React auth state in sync with login/register/logout in the same tab.
  useEffect(() => {
    const refreshAccount = () => setAccount(user());
    window.addEventListener("medilink-auth-changed", refreshAccount);
    window.addEventListener("storage", refreshAccount);
    return () => {
      window.removeEventListener("medilink-auth-changed", refreshAccount);
      window.removeEventListener("storage", refreshAccount);
    };
  }, []);

  // localStorage is only a guest/mirror cache. For authenticated customers,
  // the Order service is always the source of truth.
  useEffect(() => {
    localStorage.setItem(CART_KEY, JSON.stringify(cart));
  }, [cart]);

  useEffect(() => {
    let cancelled = false;

    const syncAuthenticatedCart = async () => {
      if (!account || !isCustomer(account)) return;

      try {
        const owner = localStorage.getItem(CART_OWNER_KEY);
        const guestItems = owner ? [] : cart;

        // Merge a genuine guest cart exactly once after a customer signs in.
        // Guest checkout is deliberately one-store-only, so a mixed cart is
        // never silently split across pharmacies.
        if (!owner && guestItems.length) {
          const storeIds = [...new Set(guestItems.map((item) => item.storeId))];
          if (storeIds.length !== 1) {
            notify("Your guest cart contains multiple stores. Please keep one store per checkout.");
            return;
          }

          for (const item of guestItems) {
            await client().post("/cart/items", {
              medicineId: item.id,
              storeId: item.storeId,
              quantity: item.quantity,
            });
          }
        } else if (owner && owner !== accountUserId(account)) {
          // Never expose another customer's server-cart mirror to this account.
          setCart([]);
        }

        // Repair legacy cart rows before the authoritative read. New carts are
        // simply created by this endpoint; existing carts keep all valid items.
        await client().post("/cart/repair");
        const { data } = await client().get("/cart");
        if (cancelled) return;

        const serverItems = data.cart?.items || [];
        setCart((previous) => normalizeServerCart(serverItems, previous));
        localStorage.setItem(CART_OWNER_KEY, accountUserId(account));
      } catch (error) {
        if (!cancelled) {
          if (error.response?.status === 401 || error.response?.status === 403) {
            endInvalidSession();
            return;
          }
          notify(
            error.response?.data?.message ||
              "We could not load your cart. Your server cart was not changed.",
          );
        }
      }
    };

    syncAuthenticatedCart();
    return () => {
      cancelled = true;
    };
    // cart is intentionally not a dependency: adding/removing items already
    // receives the authoritative server response and updates the state.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accountUserId(account), String(account?.role || "").toLowerCase()]);

  const selectStore = (store) => {
    if (!store?.id) return;
    if (cart.length && cart.some((item) => !sameId(item.storeId, store.id))) {
      notify("Your cart contains medicines from another medical store. Empty the cart before changing stores.");
      return false;
    }
    setSelectedStore(store);
    localStorage.setItem(SELECTED_STORE_KEY, JSON.stringify(store));
    notify(`${store.name} selected for your order`, 2200);
    return true;
  };

  const add = async (medicine) => {
    const medicineId = medicine.id || medicine.medicineId;
    const storeId = medicine.storeId;

    if (!selectedStore?.id) {
      notify("Choose a medical store before adding a medicine to your cart.");
      return;
    }

    if (!sameId(storeId, selectedStore.id)) {
      notify("This medicine is not from your selected medical store.");
      return;
    }

    if (!medicineId || !storeId || storeId === "00000000-0000-0000-0000-000000000000") {
      notify("This medicine is not linked to a medical-store inventory item.");
      return;
    }

    if (account && !isCustomer(account)) {
      notify("Only customer accounts can add medicines to a shopping cart.");
      return;
    }

    const normalizedMedicine = { ...medicine, id: medicineId, storeId };

    if (cart.length && cart.some((item) => !sameId(item.storeId, storeId))) {
      notify(
        "One checkout can contain medicines from one medical store. Empty your cart before switching stores.",
      );
      return;
    }

    if (isCustomer(account)) {
      try {
        const payload = {
          medicineId,
          storeId,
          externalMedicineId: medicine.externalMedicineId || undefined,
          quantity: 1,
        };
        let data;
        try {
          ({ data } = await client().post("/cart/items", payload));
        } catch (firstError) {
          // Existing customers may have a server cart created by an older
          // MediLink release. Instead of leaving the Add button apparently
          // dead, rebuild the server cart from the current browser cart plus
          // the item the customer just selected. The backend revalidates every
          // item against the selected store and live inventory before replacing
          // the old cart rows.
          const status = firstError.response?.status;
          if ([400, 404, 409, 500].includes(status)) {
            const existing = cart.map((item) => ({
              medicineId: item.id,
              storeId: item.storeId,
              externalMedicineId: item.externalMedicineId || undefined,
              quantity: Number(item.quantity || 1),
            }));
            const merged = [...existing];
            const same = merged.find((item) =>
              sameId(item.medicineId, medicineId) && sameId(item.storeId, storeId)
            );
            if (same) same.quantity += 1;
            else merged.push(payload);
            ({ data } = await client().post("/cart/reconcile", { items: merged }));
          } else {
            throw firstError;
          }
        }
        const serverItems = data.cart?.items || [];
        setCart((previous) => normalizeServerCart(serverItems, [
          ...previous,
          normalizedMedicine,
        ]));
        localStorage.setItem(CART_OWNER_KEY, accountUserId(account));
        notify(`${normalizedMedicine.name} added to cart`, 2400);
      } catch (error) {
        const status = error.response?.status;
        if (status === 401 || status === 403) {
          endInvalidSession();
        } else {
          const code = error.response?.data?.code;
          const detail = code ? ` [${code}]` : "";
          notify(
            `${error.response?.data?.message || "Could not add this medicine to your cart."}${detail}`,
            5000,
          );
          console.error("MediLink Add-to-Cart failed", {
            status,
            code,
            response: error.response?.data,
            medicineId,
            storeId,
            externalMedicineId: medicine.externalMedicineId,
          });
        }
      }
      return;
    }

    // Guest cart: local only. No inventory is reserved at this stage.
    setCart((items) => {
      const found = items.find(
        (x) => x.id === medicineId && x.storeId === storeId,
      );
      if (found) {
        const nextQuantity = found.quantity + 1;
        if (nextQuantity > Number(medicine.stockQuantity || 0)) {
          notify("You have reached the available quantity for this medicine.");
          return items;
        }
        return items.map((x) =>
          x.id === medicineId && x.storeId === storeId
            ? { ...x, quantity: nextQuantity }
            : x,
        );
      }
      return [...items, { ...normalizedMedicine, quantity: 1 }];
    });
    notify(`${normalizedMedicine.name} added to cart`, 2400);
  };

  const handleLogout = () => {
    setCart([]);
    localStorage.removeItem(CART_OWNER_KEY);
    localStorage.removeItem(CART_KEY);
    setAccount(null);
    window.dispatchEvent(new Event("medilink-auth-changed"));
  };

  const cartCount = cart.reduce((sum, item) => sum + Number(item.quantity || 0), 0);

  return (
    <>
      <Header cartCount={cartCount} onLogout={handleLogout} />
      {notice && <div className="toast">✓ {notice}</div>}
      <Routes>
        <Route path="/" element={<HomePage add={add} selectedStore={selectedStore} onSelectStore={selectStore} cartItems={cart} />} />
        <Route path="/cart" element={<CartPage items={cart} setItems={setCart} selectedStore={selectedStore} onStartNewOrder={() => { setCart([]); localStorage.setItem(CART_OWNER_KEY, accountUserId(account)); localStorage.setItem(CART_KEY, "[]"); }} />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/portal" element={<PortalPage />} />
        <Route path="/medical-store" element={<MedicalStoreCatalog add={add} selectedStore={selectedStore} onSelectStore={selectStore} cartItems={cart} />} />
        <Route path="/admin" element={<PortalPage requiredRole="Admin" />} />
        <Route path="/admin-login" element={<AdminLogin />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/register-store" element={<StoreRegister />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      <Footer />
    </>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Shell />
    </BrowserRouter>
  );
}
