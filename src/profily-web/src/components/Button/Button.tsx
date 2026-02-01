import styles from './Button.module.css';

export interface ButtonProps {
    variant: 'primary' | 'secondary';
    onClick: () => void;
    disabled?: boolean;
    children: React.ReactNode;
}

export function Button({ variant, onClick, disabled, children }: ButtonProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`${styles.button} ${styles[variant]}`}
    >
      {children}
    </button>
  );
}