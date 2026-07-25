import "./Sidebar.css";

function Sidebar({ sidebarOpen, closeSidebar }) {

    return (

        <aside className={`sidebar ${sidebarOpen ? "open" : ""}`}>

            {/* Sidebar Header */}
            <div className="sidebar-header">

                <div className="sidebar-brand">

                    <div className="logo-icon">
                        <i className="bi bi-capsule-pill"></i>
                    </div>

                    <div>
                        <h2>MediLink</h2>
                        <p>Smart Pharmacy</p>
                    </div>

                </div>

            </div>

            {/* Sidebar Menu */}
            <nav className="sidebar-menu">

                <a href="/" onClick={closeSidebar}>
                    <i className="bi bi-speedometer2"></i>
                    <span>Dashboard</span>
                </a>

                <a href="/medicines" onClick={closeSidebar}>
                    <i className="bi bi-capsule"></i>
                    <span>Medicines</span>
                </a>

                <a href="/pharmacies" onClick={closeSidebar}>
                    <i className="bi bi-hospital"></i>
                    <span>Pharmacies</span>
                </a>

                <a href="/categories" onClick={closeSidebar}>
                    <i className="bi bi-tags"></i>
                    <span>Categories</span>
                </a>

                <a href="/upload-prescription" onClick={closeSidebar}>
                    <i className="bi bi-file-earmark-medical"></i>
                    <span>Upload Prescription</span>
                </a>

                <a href="/orders" onClick={closeSidebar}>
                    <i className="bi bi-bag-check"></i>
                    <span>My Orders</span>
                </a>

                <a href="/track-order" onClick={closeSidebar}>
                    <i className="bi bi-geo-alt"></i>
                    <span>Track Order</span>
                </a>

            </nav>

            {/* Bottom Section */}
            <div className="sidebar-bottom">

                <p className="sidebar-title">My Account</p>

                <a href="/profile" onClick={closeSidebar}>
                    <i className="bi bi-person-circle"></i>
                    <span>Profile</span>
                </a>

                <a href="/settings" onClick={closeSidebar}>
                    <i className="bi bi-gear"></i>
                    <span>Settings</span>
                </a>

                <a href="/login" onClick={closeSidebar}>
                    <i className="bi bi-box-arrow-in-right"></i>
                    <span>Login</span>
                </a>

            </div>

        </aside>

    );

}

export default Sidebar;