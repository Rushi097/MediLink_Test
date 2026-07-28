import { Link, useNavigate } from "react-router-dom";
import { useState } from "react";
import axios from "axios";

const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5140/api";

export default function Login() {
  const [form, setForm] = useState({ email: "", password: "" });
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const nav = useNavigate();
  const submit = async (event) => {
    event.preventDefault();
    setLoading(true);
    setError("");
    try {
      const { data } = await axios.post(`${apiUrl}/auth/login`, form);
      localStorage.setItem("medilink-token", data.token);
      localStorage.setItem("medilink-user", JSON.stringify(data));
      nav(
        data.role === "Admin"
          ? "/admin"
          : data.role === "StoreOwner"
            ? "/medical-store"
            : "/portal",
      );
    } catch (requestError) {
      setError(
        requestError.response?.data?.message ||
          "Cannot reach the API. Ensure it is running at http://localhost:5140.",
      );
    } finally {
      setLoading(false);
    }
  };
  return (
    <section className="auth-page">
      <form className="auth-card" onSubmit={submit}>
        <Link className="brand" to="/">
          <span>+</span>MediLink
        </Link>
        <h1>Welcome back</h1>
        <p>Sign in to manage your healthcare orders or workspace.</p>
        {error && <div className="form-error">{error}</div>}
        <label>
          Email address
          <input
            type="email"
            required
            value={form.email}
            onChange={(event) =>
              setForm({ ...form, email: event.target.value })
            }
            placeholder="you@example.com"
          />
        </label>
        <label>
          Password
          <input
            type="password"
            required
            value={form.password}
            onChange={(event) =>
              setForm({ ...form, password: event.target.value })
            }
            placeholder="Your password"
          />
        </label>
        <button className="primary" disabled={loading}>
          {loading ? "Signing in…" : "Sign in"}
        </button>
        <p className="switch">
          New customer? <Link to="/register">Create an account</Link>
        </p>
        <p className="switch">
          Medical-store partner?{" "}
          <Link to="/register-store">Register your store</Link>
        </p>
      </form>
    </section>
  );
}
