import { styles } from './Card.styles';

export interface CardProps {
    children: React.ReactNode;
    className?: string;
    onClick?: () => void;
}

export function Card({ children, className, onClick }: CardProps) {
    return (
        <div 
            className={`${styles.card} ${className ?? ''}`}
            onClick={onClick}
            role={onClick ? 'button' : undefined}
            tabIndex={onClick ? 0 : undefined}
            onKeyDown={onClick ? (e) => e.key === 'Enter' && onClick() : undefined}
        >
            {children}
        </div>
    )
}