import styles from "./Pricing.module.css";

function PriceCard({ plan, billing, featured, onSelect, loading }) {
  // ── Parse price from DB pricing JSON ──────────────────────────
  const pricing = plan.pricing ?? {};
  const monthlyAmount = pricing.Monthly?.Amount ?? pricing.monthly?.amount ?? 0;
  const yearlyAmount =
    pricing.Annually?.Amount ?? pricing.annually?.amount ?? 0;

  const displayPrice =
    billing === "monthly" ? monthlyAmount : Math.round(yearlyAmount / 12);
  const isFree = monthlyAmount === 0;

  // ── Parse features ─────────────────────────────────────────────
  // API returns features as array of objects with Name property
  // or as plain strings
  const featureList = (plan.features ?? [])
    .map((f) => (typeof f === "string" ? f : (f.Name ?? f.name ?? "")))
    .filter(Boolean);

  return (
    <div
      className={`${styles.priceCard} ${featured ? styles.priceFeatured : ""}`}
      data-reveal
      data-visible="true"
    >
      {featured && <div className={styles.popularBadge}>Most Popular</div>}

      <h3 className={styles.priceTier}>{plan.name}</h3>
      <p className={styles.priceDesc}>{plan.description}</p>

      {/* Price */}
      <div className={styles.priceAmount}>
        {!isFree && <span className={styles.priceCurrency}>$</span>}
        <span className={styles.priceNum}>
          {isFree ? "Free" : displayPrice}
        </span>
        {!isFree && (
          <span className={styles.pricePeriod}>
            {billing === "yearly" ? "/mo · billed annually" : "/month"}
          </span>
        )}
      </div>

      {/* Features */}
      <ul className={styles.priceFeatures}>
        {featureList.map((feature, i) => (
          <li key={i}>
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

      {/* CTA */}
      <button
        className={`${styles.btnPlan} ${
          featured ? styles.btnPlanWhite : styles.btnPlanOutline
        }`}
        onClick={onSelect}
        disabled={loading}
      >
        {isFree ? "Get Started Free" : "Select Plan"}
      </button>
    </div>
  );
}

export default PriceCard;
