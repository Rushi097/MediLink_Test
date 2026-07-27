function QuickActionCard({
    icon,
    title,
    description,
    onClick
}) {

    return (

        <div
            className="quick-action-card"
            onClick={onClick}
        >

            <div className="quick-action-icon">
                {icon}
            </div>

            <div className="quick-action-content">

                <h4>{title}</h4>

                <p>{description}</p>

            </div>

        </div>

    );
}

export default QuickActionCard;