import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { useEffect, useState } from "react";
import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";
import StoreRegister from "./pages/auth/StoreRegister";
import AdminLogin from "./pages/auth/AdminLogin";
import Header from "./components/Header";
import Footer from "./components/Footer";
import HomePage from "./pages/customer/HomePage";
import CartPage from "./pages/customer/CartPage";
import PortalPage from "./pages/admin/PortalPage";
import MedicalStoreCatalog from "./pages/seller/MedicalStoreCatalog";
import { client, user } from "./services/api";
import "./styles.css";
import "./medicine-search.css";

function Shell() {
  const [cart, setCart] = useState(() =>
    JSON.parse(localStorage.getItem("medilink-cart") || "[]"),
  );
  const [notice, setNotice] = useState("");
  useEffect(
    () => localStorage.setItem("medilink-cart", JSON.stringify(cart)),
    [cart],
  );
  const add = async (medicine) => {
    setCart((items) => {
      const found = items.find((x) => x.id === medicine.id);
      return found
        ? items.map((x) =>
            x.id === medicine.id ? { ...x, quantity: x.quantity + 1 } : x,
          )
        : [...items, { ...medicine, quantity: 1 }];
    });
    if (user()) {
      try {
        await client().post("/cart/items", {
          medicineId: medicine.id,
          quantity: 1,
        });
      } catch {
        setNotice("Added locally. Sign in again to sync your cart.");
      }
    }
    setNotice(`${medicine.name} added to cart`);
    setTimeout(() => setNotice(""), 2400);
  };
  return (
    <>
      <Header cartCount={cart.reduce((sum, item) => sum + item.quantity, 0)} />
      {notice && <div className="toast">✓ {notice}</div>}
      <Routes>
        <Route path="/" element={<HomePage add={add} />} />
        <Route
          path="/cart"
          element={<CartPage items={cart} setItems={setCart} />}
        />
        <Route path="/portal" element={<PortalPage />} />
        <Route path="/medical-store" element={<MedicalStoreCatalog />} />
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
