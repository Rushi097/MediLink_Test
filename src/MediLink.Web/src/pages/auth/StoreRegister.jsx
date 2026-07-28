import { Link, useNavigate } from "react-router-dom";
import { useState } from "react";
import axios from "axios";

const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5140/api";

export default function StoreRegister() {
  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    businessLicenseNumber: "",
    storeName: "",
    storeAddress: "",
  });
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const nav = useNavigate();
  const change = (event) =>
    setForm({ ...form, [event.target.name]: event.target.value });
  const submit = async (event) => {
    event.preventDefault();
    setLoading(true);
    setError("");
    try {
      const { data } = await axios.post(
        `${apiUrl}/auth/register/store-owner`,
        form,
      );
      localStorage.setItem("medilink-token", data.token);
      localStorage.setItem("medilink-user", JSON.stringify(data));
      nav("/medical-store");
    } catch (requestError) {
      setError(
        requestError.response?.data?.message ||
          "We could not register your medical store.",
      );
    } finally {
      setLoading(false);
    }
  };
  return (
    <section className="auth-page">
      <form className="auth-card register" onSubmit={submit}>
        <Link className="brand" to="/">
          <span>+</span>MediLink
        </Link>
        <span className="eyebrow">MEDICAL STORE PARTNER</span>
        <h1>Register your pharmacy</h1>
        <p>
          Submit your licensed store details to start managing your medicines.
        </p>
        {error && <div className="form-error">{error}</div>}
        <div className="two">
          <label>
            Owner first name
            <input name="firstName" required onChange={change} />
          </label>
          <label>
            Owner last name
            <input name="lastName" required onChange={change} />
          </label>
        </div>
        <label>
          Work email
          <input name="email" type="email" required onChange={change} />
        </label>
        <label>
          Password
          <input
            name="password"
            type="password"
            minLength="8"
            required
            onChange={change}
          />
        </label>
        <label>
          Store name
          <input name="storeName" required onChange={change} />
        </label>
        <label>
          Drug licence number
          <input name="businessLicenseNumber" required onChange={change} />
        </label>
        <label>
          Store address
          <input name="storeAddress" required onChange={change} />
        </label>
        <button className="primary" disabled={loading}>
          {loading ? "Registering…" : "Register medical store"}
        </button>
        <p className="switch">
          Already registered? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </section>
  );
}
