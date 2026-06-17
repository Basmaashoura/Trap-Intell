import styles from "./CoreFeatures.module.css";

function FeaturesCard({ icon, title, description }) {
  return (
    <div className={styles.featureCard} data-reveal>
      <div className={styles.featureIcon}>{icon}</div>
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  );
}
export default FeaturesCard;
