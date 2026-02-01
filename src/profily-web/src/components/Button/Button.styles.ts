// Button component styles

export const styles = {
  base: 'px-4 py-2 rounded-lg text-base font-medium cursor-pointer transition-all duration-200 border-none disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-slate-900',
  variants: {
    primary: 'bg-gradient-to-r from-blue-600 to-purple-600 text-white shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 hover:scale-105 focus:ring-blue-500',
    secondary: 'bg-white/5 text-white border border-white/20 hover:bg-white/10 hover:border-white/30 focus:ring-white/50',
  },
} as const;

// Helper to build button className
export const getButtonClass = (variant: 'primary' | 'secondary') => {
  return `${styles.base} ${styles.variants[variant]}`;
};
