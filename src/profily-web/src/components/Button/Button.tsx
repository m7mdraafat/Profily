import { getButtonClass } from './Button.styles';

export interface ButtonProps {
    variant: 'primary' | 'secondary';
    onClick: (e: React.MouseEvent<HTMLButtonElement>) => void;
    disabled?: boolean;
    children: React.ReactNode;
}

export function Button({ variant, onClick, disabled, children }: ButtonProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={getButtonClass(variant)}
    >
      {children}
    </button>
  );
}