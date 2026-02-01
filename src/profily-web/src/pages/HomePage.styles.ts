// HomePage styles

export const styles = {
  // Page wrapper
  wrapper: 'flex flex-col gap-6 sm:gap-8 pb-6 sm:pb-8',

  // Hero Section
  hero: {
    section: 'text-center pt-6 sm:pt-12 pb-2 sm:pb-4 animate-[fadeInUp_0.8s_ease-out]',
    badge: 'inline-block px-3 sm:px-4 py-1.5 mb-4 sm:mb-6 text-xs sm:text-sm font-medium text-blue-400 bg-blue-600/10 border border-blue-600/30 rounded-full',
    title: 'text-2xl sm:text-3xl md:text-5xl font-extrabold text-white mb-3 sm:mb-4 leading-tight px-2',
    gradient: 'bg-gradient-to-r from-blue-500 via-purple-500 to-pink-500 bg-clip-text text-transparent',
    subtitle: 'text-base sm:text-lg text-slate-400 max-w-xl mx-auto px-4',
  },

  // Workflow Section
  workflow: {
    title: 'text-center text-xl sm:text-2xl font-bold text-white mb-1',
    subtitle: 'text-center text-sm sm:text-base text-slate-400 mb-6 sm:mb-8',
    container: 'flex flex-col lg:flex-row items-center justify-center gap-2 lg:gap-0',
    stepWrapper: 'w-full max-w-[280px] sm:max-w-xs lg:w-64 xl:w-72',
    arrow: 'flex items-center justify-center w-12 h-8 lg:w-12 lg:h-12 shrink-0',
    arrowIcon: 'text-slate-500 rotate-90 lg:rotate-0',
  },

  // Cards
  card: {
    base: 'p-4 sm:p-6 flex flex-col items-center text-center gap-3 sm:gap-4 bg-slate-800/50 border border-white/10 rounded-xl hover:-translate-y-1 transition-transform',
    choose: 'p-3 sm:p-4 flex flex-col items-center gap-2 sm:gap-3 bg-slate-800/30 border border-white/5 rounded-xl',
    chooseLabel: 'text-[10px] sm:text-xs font-semibold text-slate-500 uppercase tracking-wider',
    chooseButtons: 'flex flex-col gap-2 w-full',
    or: 'text-[10px] sm:text-xs text-slate-500 self-center',
  },

  // Icon boxes
  iconBox: {
    base: 'w-10 h-10 sm:w-12 sm:h-12 rounded-lg flex items-center justify-center',
    github: 'bg-white/5 text-white',
    rocket: 'bg-emerald-500/15 text-emerald-400',
    profile: 'w-8 h-8 sm:w-9 sm:h-9 rounded-md flex items-center justify-center bg-blue-500/15 text-blue-400 shrink-0',
    portfolio: 'w-8 h-8 sm:w-9 sm:h-9 rounded-md flex items-center justify-center bg-purple-500/15 text-purple-400 shrink-0',
  },

  // Step buttons
  stepButton: {
    base: 'flex items-center gap-2 sm:gap-3 p-2.5 sm:p-3 bg-slate-800/50 border border-white/10 rounded-lg hover:-translate-y-0.5 transition-all group',
    profile: 'hover:border-blue-500',
    portfolio: 'hover:border-purple-500',
    text: 'text-xs sm:text-sm font-bold text-white flex-1 text-left',
    arrow: 'text-slate-500 group-hover:translate-x-1 transition-transform',
  },

  // Step text
  stepText: {
    title: 'text-sm sm:text-base font-bold text-white',
    description: 'text-xs sm:text-sm text-slate-400',
  },

  // Features Section
  features: {
    section: 'text-center',
    title: 'text-base sm:text-lg font-semibold text-slate-300 mb-3 sm:mb-4',
    badges: 'flex flex-wrap justify-center gap-1.5 sm:gap-2 px-2',
  },
} as const;
