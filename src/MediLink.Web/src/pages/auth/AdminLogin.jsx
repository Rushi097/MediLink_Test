import { Link, useNavigate } from "react-router-dom";
import { useState } from "react";
import axios from "axios";

const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5140/api";

export default function AdminLogin() {
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

      if (data.role !== "Admin") {
        setError("This login page is only for platform administrators.");
        setLoading(false);
        return;
      }

      localStorage.setItem("medilink-token", data.token);
      localStorage.setItem("medilink-user", JSON.stringify(data));
      nav("/admin");
    } catch (requestError) {
      setError(
        requestError.response?.data?.message ||
          "Cannot reach the API. Ensure the backend is running at http://localhost:5140.",
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
        <h1>Admin access</h1>
        <p>Secure sign in for MediLink platform administrators.</p>
        {error && <div className="form-error">{error}</div>}

        <label>
          Admin email
          <input
            type="email"
            required
            value={form.email}
            onChange={(event) =>
              setForm({ ...form, email: event.target.value })
            }
            placeholder="admin@medilink.com"
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
            placeholder="Enter admin password"
          />
        </label>

        <button className="primary" disabled={loading}>
          {loading ? "Signing in…" : "Admin login"}
        </button>

        <p className="switch">
          Need customer login? <Link to="/login">Open customer login</Link>
        </p>
      </form>
    </section>
  );
}
