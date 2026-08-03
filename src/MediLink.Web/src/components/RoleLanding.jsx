import { Link } from "react-router-dom";

export default function RoleLanding({ role }) {
  const store = role === "StoreOwner";

  return (
    <main className="page-wrap role-landing">
      <span className="eyebrow">
        {store ? "MEDICAL STORE PARTNER" : "MEDILINK ADMINISTRATION"}
      </span>
      <h1>
        {store
          ? "Run your pharmacy online."
          : "Operate MediLink with confidence."}
      </h1>
      <p>
        {store
          ? "Register your pharmacy, manage your catalogue and monitor low-stock inventory from the store workspace."
          : "Monitor users, pharmacies, catalogue health and delivered revenue from the secured admin dashboard."}
      </p>
      <div className="role-actions">
        {store && (
          <Link className="primary inline" to="/register-store">
            Register medical store
          </Link>
        )}
        <Link className="secondary inline" to="/login">
          {store ? "Store owner login" : "Admin login"}
        </Link>
      </div>
      {!store && (
        <p className="role-note">
          Admin accounts are provisioned by the platform team; they cannot be
          created through the public website.
        </p>
      )}
    </main>
  );
}
