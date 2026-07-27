import { useState, useEffect } from "react";
import { RiRobot2Line } from "react-icons/ri";

import MediBotHeader from "./MediBotHeader";
import MediBotBody from "./MediBotBody";

import "./Medibot.css";

function MediBot() {

    const [showWelcome, setShowWelcome] = useState(false);
    const [isOpen, setIsOpen] = useState(false);

    useEffect(() => {

        const showTimer = setTimeout(() => {
            setShowWelcome(true);
        }, 3000);

        const hideTimer = setTimeout(() => {
            setShowWelcome(false);
        }, 8000);

        return () => {
            clearTimeout(showTimer);
            clearTimeout(hideTimer);
        };

    }, []);

    return (
        <>
            {/* Welcome Bubble */}

            {showWelcome && !isOpen && (
                <div className="welcome-bubble">
                    <p>👋 Need any help?</p>
                    <span>Ask MediBot anytime.</span>
                </div>
            )}

            {/* Popup */}

            {isOpen && (
                <div className="medibot-popup">

                    <MediBotHeader
                        onClose={() => setIsOpen(false)}
                    />

                    <MediBotBody />

                </div>
            )}

            {/* Floating Button */}

            <button
                className="medibot-btn"
                onClick={() => setIsOpen(!isOpen)}
            >
                <RiRobot2Line />
            </button>
        </>
    );
}

export default MediBot;