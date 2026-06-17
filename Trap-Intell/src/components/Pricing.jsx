import { useScrollReveal } from "../hooks/useScrollReveal";
import PriceCard from "./PriceCard";
import styles from "./Pricing.module.css";

function Pricing() {
  const pricingReveal = useScrollReveal();

  const plans = [
    {
      tier: "Starter",
      description: "Perfect for small teams getting started",
      monthlyPrice: 49,
      yearlyPrice: 39,
      features: [
        "10 User accounts",
        "5 Honeypot deployments",
        "Real-time alerts",
        "Email support",
      ],
      featured: false,
      buttonStyle: "btn-plan-outline",
    },
    {
      tier: "Professional",
      description: "For growing organizations",
      monthlyPrice: 149,
      yearlyPrice: 119,
      features: [
        "50 User accounts",
        "20 Honeypot deployments",
        "Real-time alerts",
        "Priority support",
        "Advanced analytics",
      ],
      featured: true,
      buttonStyle: "btn-plan-white",
    },
    {
      tier: "Enterprise",
      description: "For large-scale operations",
      monthlyPrice: 499,
      yearlyPrice: 399,
      features: [
        "Unlimited User accounts",
        "Unlimited Honeypot deployments",
        "Real-time alerts",
        "Dedicated account manager",
        "Custom integrations",
        "SLA guarantee",
      ],
      featured: false,
      buttonStyle: "btn-plan-outline",
    },
  ];

  return (
    <section className={styles.pricing} id="pricing">
      <div className={styles.sectionContainer}>
        <h2 className={styles.sectionTitle}>Pricing Plans</h2>
        <p className={styles.sectionSubtitle}>
          Choose the perfect plan for your organization
        </p>

        {/* <!-- Toggle --> */}
        <div className={styles.pricingToggle}>
          <button
            className={`${styles.toggleBtn} ${styles.toggleBtnActive}`}
            id="btn-monthly"
            // onClick="setPricing('monthly')"
          >
            Monthly
          </button>
          <button
            className={styles.toggleBtn}
            id="btn-yearly"
            // onclick="setPricing('yearly')"
          >
            Yearly
          </button>
          <span className={styles.toggleBadge}>Save 20%</span>
        </div>

        <div className={styles.pricingGrid} ref={pricingReveal}>
          {plans.map((plan, index) => (
            <PriceCard
              key={index}
              tier={plan.tier}
              description={plan.description}
              monthlyPrice={plan.monthlyPrice}
              yearlyPrice={plan.yearlyPrice}
              features={plan.features}
              featured={plan.featured}
              buttonStyle={plan.buttonStyle}
            />
          ))}
        </div>
      </div>
    </section>
  );
}

export default Pricing;
