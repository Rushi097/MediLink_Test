import "./Navbar.css";

function Navbar({ toggleSidebar, sidebarOpen }) {

    return (

        <header className="navbar-custom">

            {/* Left Section */}

            <div className="navbar-left">

                {!sidebarOpen && (
                    <button
                        className="hamburger-btn"
                        onClick={toggleSidebar}
                        aria-label="Toggle Sidebar"
                    >
                        <i className="bi bi-list"></i>
                    </button>
                )}
            </div>

            {/* Center Section */}

            <div className="navbar-center">

                <div className="search-wrapper">

                    <i className="bi bi-search"></i>

                    <input
                        type="text"
                        placeholder="Search medicines..."
                        className="search-box"
                    />

                </div>

            </div>

            {/* Right Section */}

            <div className="navbar-right">

                <div className="nav-icon">
                    <i className="bi bi-heart"></i>
                </div>

                <div className="nav-icon">
                    <i className="bi bi-cart3"></i>
                </div>

                <div className="nav-icon">
                    <i className="bi bi-bell"></i>
                </div>

                <div className="user-info">
                    <i className="bi bi-person-circle"></i>
                    <span>Login</span>
                </div>

            </div>

        </header>

    );

}

export default Navbar;