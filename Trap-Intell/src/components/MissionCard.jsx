import styles from "./MissionCard.module.css";

function MissionCard({ icon, title, description }) {
  return (
    <div className={styles.mvCard} data-reveal>
      <div className={styles.mvIcon}>{icon}</div>
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  );
}

export default MissionCard;
