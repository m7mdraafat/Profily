import { styles } from './Badge.styles';

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