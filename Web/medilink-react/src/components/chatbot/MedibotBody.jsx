import QuickActionCard from "./QuickActionCard";

function MediBotBody() {

    return (
        <div className="welcome-heading">
            <h2>Welcome to MediLink!</h2>

            <p className="welcome-text">
                Choose a service below or type your question.
            </p>

        <div className="hero-section">

                <img
                    src="https://media1.giphy.com/media/v1.Y2lkPTc5MGI3NjExZm41N3ZhYXhmM2V0N2syNW5qczczcTV3NnMzNHZpbHBxNDYyajJwZyZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/y5OS4cGeLMQsU/giphy.gif"
                    alt="MediBot"
                    className="hero-gif"
                />

                <h2>Welcome to MediLink!</h2>

                <p className="welcome-text">
                    Choose a service below or type your question.
                </p>

            </div>

            <QuickActionCard
                icon="💊"
                title="Check Medicine"
                description="Find medicine availability"
            />

            <QuickActionCard
                icon="📦"
                title="Track My Order"
                description="Check live order status"
            />

            <QuickActionCard
                icon="📞"
                title="Contact Details"
                description="Pharmacies & Hospitals"
            />

        </div>
    );

}

export default MediBotBody;