import { useState, useEffect } from "react";
import "./Medibot.css";
import { RiRobot2Line } from "react-icons/ri";

function MediBot() {

    const [showWelcome, setShowWelcome] = useState(false);

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
            {showWelcome && (
                <div className="welcome-bubble">
                    <p>👋 Need any help?</p>
                    <span>Ask MediBot anytime.</span>
                </div>
            )}
            <button className="medibot-btn">
                <RiRobot2Line />
            </button>
        </>
    );
}

export default MediBot;