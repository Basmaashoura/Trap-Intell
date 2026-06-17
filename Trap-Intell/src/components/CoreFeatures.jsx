import { useScrollReveal } from "../hooks/useScrollReveal";
import FeaturesCard from "./FeaturesCard";
import styles from "./CoreFeatures.module.css";

function CoreFeatures() {
  const featuresReveal = useScrollReveal();

  const features = [
    {
      title: "Secure Authentication",
      description:
        "Multi-factor authentication with SSO support and advanced security features.",
      icon: (
        <svg
          width="26"
          height="26"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <rect x="3" y="11" width="18" height="11" rx="2" />
          <path d="M7 11V7a5 5 0 0 1 10 0v4" />
        </svg>
      ),
    },
    {
      title: "Organization Management",
      description:
        "Complete organization onboarding with approval workflows and role-based access control.",
      icon: (
        <svg
          width="26"
          height="26"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
          <path d="M16 3.13a4 4 0 0 1 0 7.75" />
        </svg>
      ),
    },
    {
      title: "Real-time Monitoring",
      description:
        "Live dashboard with WebSocket updates for attack monitoring and threat detection.",
      icon: (
        <svg
          width="26"
          height="26"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
        </svg>
      ),
    },
    {
      title: "Honeypot Deployment",
      description:
        "Deploy and monitor honeypots with automated threat intelligence gathering.",
      icon: (
        <svg
          width="26"
          height="26"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
        </svg>
      ),
    },
    {
      title: "Smart Alerts",
      description:
        "Customizable alert rules with severity levels and real-time notifications.",
      icon: (
        <svg
          width="26"
          height="26"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M22 17H2a3 3 0 0 0 3-3V9a7 7 0 0 1 14 0v5a3 3 0 0 0 3 3zm-8.27 4a2 2 0 0 1-3.46 0" />
        </svg>
      ),
    },
    {
      title: "Geographic Intelligence",
      description:
        "Interactive maps showing threat origins and geographic distribution.",
      icon: (
        <svg
          width="26"
          height="26"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="12" cy="12" r="10" />
          <line x1="2" y1="12" x2="22" y2="12" />
          <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
        </svg>
      ),
    },
  ];

  return (
    <section className={styles.features} id="features">
      <div className={styles.sectionContainer}>
        <h2 className={styles.sectionTitle}>Core Features</h2>
        <p className={styles.sectionSubtitle}>
          Comprehensive security platform with enterprise-grade authentication
          and threat intelligence
        </p>
        <div className={styles.featuresGrid} ref={featuresReveal}>
          {features.map((feature, index) => (
            <FeaturesCard
              key={index}
              title={feature.title}
              description={feature.description}
              icon={feature.icon}
            />
          ))}
        </div>
      </div>
    </section>
  );
}

export default CoreFeatures;
