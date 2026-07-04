import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useScrollReveal } from "../hooks/useScrollReveal";
import { api } from "../services/api";
import PriceCard from "./PriceCard";
import styles from "./Pricing.module.css";
import { useAuth } from "../context/AuthContext";

function Pricing() {
  const { isAuthenticated } = useAuth();
  const pricingReveal = useScrollReveal();
  const navigate = useNavigate();
  const [billing, setBilling] = useState("monthly");
  const [plans, setPlans] = useState(FALLBACK_PLANS); // ← start with fallback
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // Only fetch real plans if logged in
    if (!isAuthenticated) return;

    setLoading(true);
    api
      .get("/api/plans")
      .then((data) => {
        const items = Array.isArray(data)
          ? data
          : (data.items ?? data.plans ?? []);
        setPlans(items);
      })
      .catch(() => setPlans(FALLBACK_PLANS))
      .finally(() => setLoading(false));
  }, [isAuthenticated]);

const handleSelectPlan = (plan) => {
  navigate("/login", {
    state: {
      selectedPlan: plan,
      fromPlanSelection: true,
    },
  });
};

  const displayPlans = loading ? FALLBACK_PLANS : plans;

  return (
    <section className={styles.pricing} id="pricing">
      <div className={styles.sectionContainer}>
        <h2 className={styles.sectionTitle}>Pricing Plans</h2>
        <p className={styles.sectionSubtitle}>
          Choose the perfect plan for your organization
        </p>

        {/* Toggle */}
        <div className={styles.pricingToggle}>
          <button
            className={`${styles.toggleBtn} ${billing === "monthly" ? styles.toggleBtnActive : ""}`}
            onClick={() => setBilling("monthly")}
          >
            Monthly
          </button>
          <button
            className={`${styles.toggleBtn} ${billing === "yearly" ? styles.toggleBtnActive : ""}`}
            onClick={() => setBilling("yearly")}
          >
            Yearly
          </button>
          <span className={styles.toggleBadge}>Save 20%</span>
        </div>

        <div className={styles.pricingGrid} ref={pricingReveal}>
          {displayPlans.map((plan, index) => (
            <PriceCard
              key={plan.id ?? index}
              plan={plan}
              billing={billing}
              featured={
                plan.name === "Professional" || plan.type === "Featured"
              }
              onSelect={() => handleSelectPlan(plan)}
              loading={loading}
            />
          ))}
        </div>
      </div>
    </section>
  );
}

// ── Fallback while loading or if API fails ─────────────────────
const FALLBACK_PLANS = [
  {
    id: "aaaa1111-1111-1111-1111-111111111111",
    name: "Free Tier",
    description:
      "Perfect for small teams getting started with honeypot technology.",
    type: "Free",
    pricing: { Monthly: { Amount: 0 }, Annually: { Amount: 0 } },
    features: [
      { Name: "2 Honeypot deployments" },
      { Name: "3 User accounts" },
      { Name: "Real-time alerts" },
      { Name: "Email support" },
    ],
  },
  {
    id: "aaaa2222-2222-2222-2222-222222222222",
    name: "Professional",
    description:
      "For growing security teams needing advanced threat detection.",
    type: "Paid",
    pricing: { Monthly: { Amount: 499 }, Annually: { Amount: 4990 } },
    features: [
      { Name: "10 Honeypot deployments" },
      { Name: "10 User accounts" },
      { Name: "AI Threat Analysis" },
      { Name: "Threat Feeds" },
      { Name: "Priority support" },
    ],
  },
  {
    id: "aaaa3333-3333-3333-3333-333333333333",
    name: "Enterprise",
    description: "Full-featured solution for enterprise security operations.",
    type: "Paid",
    pricing: { Monthly: { Amount: 1999 }, Annually: { Amount: 19990 } },
    features: [
      { Name: "50 Honeypot deployments" },
      { Name: "50 User accounts" },
      { Name: "Predictive Analytics" },
      { Name: "SOC2 Compliance" },
      { Name: "Dedicated account manager" },
      { Name: "SLA guarantee" },
    ],
  },
];

export default Pricing;
