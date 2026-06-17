import styles from "./StatusCard.module.css";

function StatusCard({ number, label }) {
  return (
    <div className={styles.statCard} data-reveal>
      <span className={styles.statNum}>{number}</span>
      <span className={styles.statLabel}>{label}</span>
    </div>
  );
}

export default StatusCard;
