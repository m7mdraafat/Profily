// Button component styles

export const styles = {
  base: 'px-4 py-2 rounded-lg text-base font-medium cursor-pointer transition-all border-none disabled:opacity-50 disabled:cursor-not-allowed',
  variants: {
    primary: 'bg-gradient-to-r from-blue-600 to-purple-600 text-white hover:opacity-90 shadow-lg shadow-blue-500/25',
    secondary: 'bg-transparent text-white border border-white/20 hover:bg-white/10',
  },
} as const;

// Helper to build button className
export const getButtonClass = (variant: 'primary' | 'secondary') => {
  return `${styles.base} ${styles.variants[variant]}`;
};
