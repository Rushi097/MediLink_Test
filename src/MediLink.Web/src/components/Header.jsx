import { Link, useNavigate } from "react-router-dom";
import { FaCartShopping } from "react-icons/fa6";
import { user } from "../services/api";

export default function Header({ cartCount }) {
  const nav = useNavigate();
  const account = user();

  const logout = () => {
    localStorage.removeItem("medilink-user");
    localStorage.removeItem("medilink-token");
    nav("/");
  };

  return (
    <header className="nav">
      <Link className="brand" to="/">
        <span>+</span>MediLink
      </Link>
      <nav>
        <Link to="/">Medicines</Link>
        <a href="/#how">How it works</a>
        <Link to="/medical-store">Medical store</Link>
        {account && <Link to="/portal">My portal</Link>}
      </nav>
      <div className="nav-actions">
        {account ? (
          <>
            <span className="welcome">
              Hi, {account.fullName?.split(" ")[0]}
            </span>
            <button className="link-btn" onClick={logout}>
              Logout
            </button>
          </>
        ) : (
          <>
            <Link className="login-link" to="/login">
              Login
            </Link>
            <Link className="nav-cta" to="/register">
              Sign up
            </Link>
          </>
        )}
        <Link className="cart" to="/cart">
          <FaCartShopping /> <b>{cartCount}</b>
        </Link>
      </div>
    </header>
  );
}
