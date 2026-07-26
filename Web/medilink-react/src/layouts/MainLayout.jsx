import "./MainLayout.css";
import { useState } from "react";

import Sidebar from "../components/layout/Sidebar";
import Navbar from "../components/layout/Navbar";
import Footer from "../components/layout/Footer";
import MediBot from "../components/chatbot/MediBot";

function MainLayout({ children }) {

    const [sidebarOpen, setSidebarOpen] = useState(false);

    const toggleSidebar = () => {
        setSidebarOpen(prev => !prev);
    };

    const closeSidebar = () => {
        setSidebarOpen(false);
    };

    return (
        <div className="app-container">

            <Navbar toggleSidebar={toggleSidebar} sidebarOpen={sidebarOpen} />

            <Sidebar
                sidebarOpen={sidebarOpen}
                closeSidebar={closeSidebar}
            />

            {/* Overlay */}
            {sidebarOpen && (
                <div
                    className="drawer-overlay"
                    onClick={closeSidebar}
                ></div>
            )}

            <main className="content-body">
                {children}
            </main>

            <MediBot />

            <Footer />

        </div>
    );
}

export default MainLayout;