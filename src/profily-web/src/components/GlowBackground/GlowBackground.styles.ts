// GlowBackground component styles

export const styles = {
  wrapper: 'fixed inset-0 pointer-events-none z-0 overflow-hidden',
  blueGlow: 'absolute w-[700px] h-[700px] rounded-full bg-blue-600/30 blur-[120px] -top-[200px] -left-[150px]',
  purpleGlow: 'absolute w-[600px] h-[600px] rounded-full bg-purple-600/30 blur-[120px] -bottom-[150px] -right-[100px]',
} as const;
