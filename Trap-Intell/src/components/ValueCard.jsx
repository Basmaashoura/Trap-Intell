import styles from "./ValueCard.module.css";

function ValueCard({ icon, title, description }) {
  return (
    <div className={styles.valueCard} data-reveal>
      <div className={styles.valueIcon}>{icon}</div>
      <h4>{title}</h4>
      <p>{description}</p>
    </div>
  );
}

export default ValueCard;
