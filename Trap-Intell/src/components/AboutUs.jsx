import { useScrollReveal } from "../hooks/useScrollReveal";
import MissionCard from "./MissionCard";
import ValueCard from "./ValueCard";
import styles from "./AboutUs.module.css";

function AboutUs() {
  const missionsRef = useScrollReveal();
  const valuesRef = useScrollReveal();

  const missions = [
    {
      title: "Our Mission",
      description:
        "To provide enterprise-level security solutions that are both powerful and accessible, enabling organizations of all sizes to defend against modern cyber threats.",
      icon: (
        <svg
          width="22"
          height="22"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
          <polyline points="22 4 12 14.01 9 11.01" />
        </svg>
      ),
    },
    {
      title: "Our Vision",
      description:
        "To become the global standard for security intelligence platforms, trusted by organizations worldwide for protecting their critical digital assets.",
      icon: (
        <svg
          width="22"
          height="22"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
        </svg>
      ),
    },
  ];

  const values = [
    {
      title: "Innovation",
      description: "Constantly evolving our technology",
      icon: (
        <svg
          width="24"
          height="24"
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
      title: "Security First",
      description: "Your protection is our priority",
      icon: (
        <svg
          width="24"
          height="24"
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
      title: "Transparency",
      description: "Clear communication and honest practices",
      icon: (
        <svg
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
      ),
    },
    {
      title: "Excellence",
      description: "Delivering the highest quality solutions",
      icon: (
        <svg
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#4044e4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="12" cy="8" r="6" />
          <path d="M15.477 12.89L17 22l-5-3-5 3 1.523-9.11" />
        </svg>
      ),
    },
  ];

  return (
    <section className={styles.about} id="about">
      <div className={styles.sectionContainer}>
        <h2 className={styles.sectionTitle}>About Us</h2>
        <p className={styles.sectionSubtitle}>
          Building the future of cybersecurity
        </p>
        <p className={styles.aboutBody}>
          ThreatGuard is a cutting-edge security intelligence platform designed
          for enterprises seeking comprehensive threat detection and monitoring.
          We combine advanced authentication systems with real-time threat
          intelligence to protect your digital infrastructure.
        </p>
        <div className={styles.missionVision} ref={missionsRef}>
          {missions.map((value, index) => (
            <MissionCard
              key={index}
              title={value.title}
              description={value.description}
              icon={value.icon}
            />
          ))}
        </div>
        {/* <!-- Values --> */}
        <h3 className={styles.subHeading}>Our Values</h3>
        <div className={styles.valuesGrid} ref={valuesRef}>
          {values.map((value, index) => (
            <ValueCard
              key={index}
              title={value.title}
              description={value.description}
              icon={value.icon}
            />
          ))}
        </div>
      </div>
    </section>
  );
}

export default AboutUs;
