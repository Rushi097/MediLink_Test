import { Link, useNavigate } from "react-router-dom";
import { useState } from "react";
import axios from "axios";
const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5140/api";
export default function Register() {
  const [form, setForm] = useState({
      firstName: "",
      lastName: "",
      email: "",
      password: "",
      phoneNumber: "",
      deliveryAddress: "",
    }),
    [error, setError] = useState(""),
    [loading, setLoading] = useState(false),
    nav = useNavigate();
  const change = (e) => setForm({ ...form, [e.target.name]: e.target.value });
  const submit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");
    try {
      const { data } = await axios.post(
        `${apiUrl}/auth/register/customer`,
        form,
      );
      localStorage.setItem("medilink-token", data.token);
      localStorage.setItem("medilink-user", JSON.stringify(data));
      nav("/");
    } catch (e) {
      setError(
        e.response?.data?.message || "We could not create your account.",
      );
    } finally {
      setLoading(false);
    }
  };
  return (
    <section className="auth-page">
      <form className="auth-card register" onSubmit={submit}>
        <Link className="brand" to="/">
          <span>✚</span> MediLink
        </Link>
        <h1>Create your account</h1>
        <p>Healthcare essentials, just a few clicks away.</p>
        {error && <div className="form-error">{error}</div>}
        <div className="two">
          <label>
            First name
            <input name="firstName" required onChange={change} />
          </label>
          <label>
            Last name
            <input name="lastName" required onChange={change} />
          </label>
        </div>
        <label>
          Email address
          <input name="email" type="email" required onChange={change} />
        </label>
        <label>
          Password
          <input
            name="password"
            type="password"
            minLength="6"
            required
            onChange={change}
          />
        </label>
        <div className="two">
          <label>
            Phone number
            <input name="phoneNumber" required onChange={change} />
          </label>
          <label>
            Delivery address
            <input name="deliveryAddress" required onChange={change} />
          </label>
        </div>
        <button className="primary" disabled={loading}>
          {loading ? "Creating account…" : "Create account"}
        </button>
        <p className="switch">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </section>
  );
}
