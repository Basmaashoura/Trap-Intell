import styles from "./Services.module.css";

function ServiceCard({ icon, title, description }) {
  return (
    <div className={styles.serviceCard} data-reveal>
      <div className={styles.serviceIcon}>{icon}</div>
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  );
}

export default ServiceCard;
