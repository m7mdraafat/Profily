import { createContext, useContext, useState, useCallback, useEffect, useRef, type ReactNode } from 'react';
import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from 'lucide-react';
import { toastStyles, styles, type ToastType, TOAST_DURATION } from './Toast.styles';

interface Toast {
  id: string;
  type: ToastType;
  message: string;
  duration: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

interface ToastContextValue {
  toasts: Toast[];
  showToast: (type: ToastType, message: string, action?: Toast['action'], duration?: number) => void;
  dismissToast: (id: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

const ToastIcon = ({ type }: { type: ToastType }) => {
  const iconClass = toastStyles[type].icon;
  const size = 20;
  
  switch (type) {
    case 'success':
      return <CheckCircle size={size} className={iconClass} />;
    case 'error':
      return <AlertCircle size={size} className={iconClass} />;
    case 'warning':
      return <AlertTriangle size={size} className={iconClass} />;
    case 'info':
      return <Info size={size} className={iconClass} />;
  }
};

interface ToastItemProps {
  toast: Toast;
  onDismiss: () => void;
}

function ToastItem({ toast, onDismiss }: ToastItemProps) {
  const [progress, setProgress] = useState(100);
  const [dimensions, setDimensions] = useState({ width: 0, height: 0, perimeter: 0 });
  const containerRef = useRef<HTMLDivElement>(null);
  const style = toastStyles[toast.type];
  
  // Measure toast dimensions on mount
  useEffect(() => {
    if (containerRef.current) {
      const { width, height } = containerRef.current.getBoundingClientRect();
      const perimeter = 2 * (width + height);
      setDimensions({ width, height, perimeter });
    }
  }, []);
  
  // Animate progress
  useEffect(() => {
    const startTime = Date.now();
    const duration = toast.duration;
    
    const updateProgress = () => {
      const elapsed = Date.now() - startTime;
      const remaining = Math.max(0, 100 - (elapsed / duration) * 100);
      setProgress(remaining);
      
      if (remaining > 0) {
        requestAnimationFrame(updateProgress);
      } else {
        onDismiss();
      }
    };
    
    const animationId = requestAnimationFrame(updateProgress);
    return () => cancelAnimationFrame(animationId);
  }, [toast.duration, onDismiss]);

  // Calculate stroke dash offset (how much of border is hidden)
  const strokeDashoffset = dimensions.perimeter * (1 - progress / 100);
  
  return (
    <div ref={containerRef} className={`relative ${styles.toast.base} ${style.bg}`}>
      {/* Animated border using SVG overlay */}
      {dimensions.perimeter > 0 && (
        <svg
          className="absolute inset-0 w-full h-full pointer-events-none"
          style={{ overflow: 'visible' }}
        >
          <rect
            x="2"
            y="0.5"
            width={dimensions.width - 3}
            height={dimensions.height - 3}
            rx="13"
            ry="13"
            fill="none"
            stroke={style.stroke}
            strokeWidth="2"
            strokeDasharray={dimensions.perimeter}
            strokeDashoffset={strokeDashoffset}
            style={{ transition: 'none' }}
          />
        </svg>
      )}
      
      <div className="flex items-center gap-3 flex-1">
        <ToastIcon type={toast.type} />
        <p className={styles.message}>{toast.message}</p>
      </div>
      
      {toast.action && (
        <button
          onClick={() => {
            toast.action?.onClick();
            onDismiss();
          }}
          className={styles.actionButton}
        >
          {toast.action.label}
        </button>
      )}
      
      <button onClick={onDismiss} className={styles.dismissButton}>
        <X size={16} />
      </button>
    </div>
  );
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = useCallback((
    type: ToastType, 
    message: string, 
    action?: Toast['action'],
    duration: number = TOAST_DURATION
  ) => {
    const id = Math.random().toString(36).slice(2);
    // Toasts with actions get longer duration
    const finalDuration = action ? duration * 2 : duration;
    const toast: Toast = { id, type, message, action, duration: finalDuration };
    
    setToasts(prev => [...prev, toast]);
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  return (
    <ToastContext.Provider value={{ toasts, showToast, dismissToast }}>
      {children}
      
      {/* Toast Container */}
      <div className={styles.container}>
        {toasts.map(toast => (
          <ToastItem
            key={toast.id}
            toast={toast}
            onDismiss={() => dismissToast(toast.id)}
          />
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return context;
}
