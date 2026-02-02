export type ToastType = 'success' | 'error' | 'warning' | 'info';

// Toast type constants
export const TOAST_TYPE = {
  SUCCESS: 'success' as const,
  ERROR: 'error' as const,
  WARNING: 'warning' as const,
  INFO: 'info' as const,
};

export const TOAST_DURATION = 5000; // 5 seconds

export const toastStyles: Record<ToastType, { bg: string; icon: string; stroke: string }> = {
  success: {
    bg: 'bg-emerald-500/10',
    icon: 'text-emerald-400',
    stroke: '#22c55e',
  },
  error: {
    bg: 'bg-red-500/10',
    icon: 'text-red-400',
    stroke: '#ef4444',
  },
  warning: {
    bg: 'bg-amber-500/10',
    icon: 'text-amber-400',
    stroke: '#eab308',
  },
  info: {
    bg: 'bg-blue-500/10',
    icon: 'text-blue-400',
    stroke: '#3b82f6',
  },
};

export const styles = {
  container: 'fixed bottom-6 right-6 z-50 flex flex-col gap-3',
  toast: {
    base: 'relative flex items-center gap-3 min-w-[320px] max-w-md rounded-lg px-4 py-3 shadow-xl backdrop-blur-sm animate-in slide-in-from-right-5 fade-in duration-300',
  },
  message: 'flex-1 text-sm text-white font-medium',
  actionButton: 'px-3 py-1.5 bg-white/10 hover:bg-white/20 text-white text-sm font-medium rounded-md transition-colors',
  dismissButton: 'p-1 text-slate-400 hover:text-white transition-colors',
};
