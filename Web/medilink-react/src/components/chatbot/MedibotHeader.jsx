import { RiRobot2Line } from "react-icons/ri";

function MediBotHeader({ onClose }) {

    return (

        <div className="medibot-header">

            <div className="medibot-header-left">

                <div className="bot-logo">
                    <RiRobot2Line />
                </div>

                <div>
                    <h3>MediBot</h3>
                    <p>Medicines • Orders • Support</p>
                </div>

            </div>

            <button
                className="close-btn"
                onClick={onClose}
            >
                ✕
            </button>

        </div>

    );
}

export default MediBotHeader;