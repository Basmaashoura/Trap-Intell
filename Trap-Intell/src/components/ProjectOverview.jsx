// import "../assets/styles/homepage.css";
import styles from "./ProjectOverview.module.css";
import StatusCard from "./StatusCard";
import { useScrollReveal } from "../hooks/useScrollReveal";

function ProjectOverview() {
  const scrollReveal = useScrollReveal();

  const stats = [
    { number: "9", label: "Auth Systems" },
    { number: "6", label: "User Roles" },
    { number: "8+", label: "Other Features" },
    { number: "15+", label: "Domain Events" },
  ];

  return (
    <section className={styles.overview} id="overview" ref={scrollReveal}>
      <div className={styles.sectionContainer}>
        <h2 className={styles.sectionTitle}>Project Overview</h2>
        <p className={styles.sectionSubtitle}>
          TRAP-INTELL is a comprehensive cybersecurity threat intelligence
          platform designed for enterprise security operations centers (SOC).
          The platform provides real-time threat monitoring, honeypot
          management, attack analysis, and threat actor tracking capabilities.
        </p>
        <div className={styles.statsGrid}>
          {stats.map((stat, index) => (
            <StatusCard key={index} number={stat.number} label={stat.label} />
          ))}
        </div>
      </div>
    </section>
  );
}

export default ProjectOverview;
