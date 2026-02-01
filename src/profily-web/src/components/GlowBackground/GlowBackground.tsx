import { styles } from './GlowBackground.styles';

export function GlowBackground() {
  return (
    <div className={styles.wrapper}>
      <div className={styles.blueGlow} />
      <div className={styles.purpleGlow} />
    </div>
  );
}