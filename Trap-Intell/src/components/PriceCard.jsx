import styles from "./Pricing.module.css";

function PriceCard({
  tier,
  description,
  monthlyPrice,
  yearlyPrice,
  features,
  featured,
  buttonStyle,
}) {
  return (
    <div
      className={`${styles.priceCard} ${featured ? styles.priceFeatured : ""}`}
      data-reveal
    >
      {featured && <div className={styles.popularBadge}>Most Popular</div>}
      <h3 className={styles.priceTier}>{tier}</h3>
      <p className={styles.priceDesc}>{description}</p>
      <div className={styles.priceAmount}>
        <span className={styles.priceCurrency}>$</span>
        <span
          className={styles.priceNum}
          data-monthly={monthlyPrice}
          data-yearly={yearlyPrice}
        >
          {monthlyPrice}
        </span>
        <span className={styles.pricePeriod}>/month</span>
      </div>
      <ul className={styles.priceFeatures}>
        {features.map((feature, index) => (
          <li key={index}>
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke={featured ? "#fff" : "#2ace5f"}
              strokeWidth="2.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <polyline points="20 6 9 17 4 12" />
            </svg>
            {" " + feature}
          </li>
        ))}
      </ul>
      <button
        className={`${styles.btnPlan} ${buttonStyle === "btn-plan-outline" ? styles.btnPlanOutline : styles.btnPlanWhite}`}
      >
        Select Plan
      </button>
    </div>
  );
}

export default PriceCard;
