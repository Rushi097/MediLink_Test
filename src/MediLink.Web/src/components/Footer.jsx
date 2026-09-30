export default function Footer() {
  return (
    <footer>
      <div className="brand">
        <span>+</span>MediLink
      </div>
      <p>Hyperlocal healthcare, delivered responsibly.</p>
      <small>
        © 2026 MediLink · Prescription medicines are supplied only after
        pharmacist verification.
      </small>
      <small style={{ display: "block", marginTop: "8px" }}>
        Medicine reference data uses publicly available data from the U.S.
        National Library of Medicine (NLM), NIH, HHS. NLM is not responsible
        for MediLink and does not endorse or recommend it.
      </small>
    </footer>
  );
}
