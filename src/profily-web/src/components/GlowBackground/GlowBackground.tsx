import styles from './GlowBackground.module.css';

export function GlowBackground() {
  return (
    <div className={styles.container}>
      <div className={`${styles.blob} ${styles.blobBlue}`} />
      <div className={`${styles.blob} ${styles.blobPurple}`} />
    </div>
  );
}