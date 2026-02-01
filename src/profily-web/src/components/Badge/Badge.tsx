import styles from './Badge.module.css';

export interface BadgeProps {
    children: React.ReactNode;
    icon?: React.ReactNode;
}

export function Badge({ children, icon }: BadgeProps) {
    return (
        <span className={styles.badge}>
            {icon && <span className={styles.icon}>{icon}</span>}
            {children}
        </span>
    )
}