// Layout component styles
// Extracted for cleaner component code

export const styles = {
  // Navigation links
  nav: {
    base: 'px-4 py-2.5 text-sm rounded-lg transition-all duration-200',
    active: 'font-semibold text-white bg-gradient-to-r from-blue-600 to-purple-600 shadow-lg shadow-blue-500/25',
    inactive: 'font-medium text-slate-400 hover:text-white hover:bg-white/10',
  },

  // Header
  header: {
    wrapper: 'sticky top-0 z-50 mx-0 sm:mx-2 md:mx-auto mt-0 sm:mt-2 md:mt-4 flex items-center justify-between md:justify-start gap-4 px-4 py-3 bg-slate-900/95 backdrop-blur-xl border-b sm:border border-white/10 sm:rounded-xl',
    logo: {
      link: 'flex items-center gap-2 group',
      icon: 'w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shadow-lg shadow-blue-500/25 group-hover:shadow-blue-500/40 transition-shadow',
      text: 'text-xl sm:text-2xl font-bold bg-gradient-to-r from-white via-blue-100 to-blue-300 bg-clip-text text-transparent',
    },
    mobileToggle: 'md:hidden p-2.5 text-white hover:bg-white/10 rounded-lg transition-colors',
    desktopNav: 'hidden md:flex gap-1 ml-auto',
    auth: {
      wrapper: 'hidden md:flex items-center ml-4 pl-4 border-l border-white/10',
      login: 'flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-slate-800 border border-white/20 rounded-lg hover:bg-slate-700 hover:border-white/30 transition-all duration-200',
    },
    mobileAuth: {
      wrapper: 'flex flex-col gap-2 mt-3 pt-3 border-t border-white/10',
      login: 'flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-semibold text-white bg-slate-800 border border-white/20 rounded-lg',
    },
  },

  // Mobile menu
  mobile: {
    overlay: 'fixed inset-0 z-40 bg-black/60 backdrop-blur-sm md:hidden',
    nav: {
      base: 'fixed top-[57px] left-0 right-0 z-50 mx-2 p-4 flex flex-col gap-1 bg-slate-900/98 backdrop-blur-xl border border-white/10 rounded-xl shadow-2xl transform transition-all duration-300 md:hidden',
      open: 'opacity-100 translate-y-0',
      closed: 'opacity-0 -translate-y-4 pointer-events-none',
    },
  },

  // Main content
  main: 'flex-1 px-3 sm:px-4 md:px-8 py-6 sm:py-8 max-w-6xl w-full mx-auto',

  // Footer
  footer: {
    wrapper: 'mt-auto border-t border-white/10 bg-slate-900/80 backdrop-blur-sm',
    container: 'max-w-6xl mx-auto px-4 sm:px-6 py-8 sm:py-12',
    grid: 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8 mb-8',
    brand: {
      wrapper: 'sm:col-span-2 lg:col-span-1',
      logo: 'flex items-center gap-2 group mb-4',
      logoIcon: 'w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shadow-lg shadow-blue-500/25',
      logoText: 'text-xl font-bold bg-gradient-to-r from-white via-blue-100 to-blue-300 bg-clip-text text-transparent',
      description: 'text-slate-400 text-sm leading-relaxed',
    },
    section: {
      title: 'text-white font-semibold mb-4',
      list: 'space-y-2',
      link: 'text-slate-400 hover:text-white text-sm transition-colors',
    },
    social: {
      wrapper: 'flex gap-3',
      icon: 'w-10 h-10 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center text-slate-400 hover:text-white hover:bg-white/10 hover:border-white/20 transition-all',
    },
    bottom: {
      wrapper: 'border-t border-white/10 pt-6',
      content: 'flex flex-col sm:flex-row items-center justify-between gap-4',
      copyright: 'text-slate-500 text-sm',
      madeWith: 'text-slate-500 text-sm flex items-center gap-1',
    },
  },

  // Layout wrapper
  layout: {
    wrapper: 'relative z-10 min-h-screen flex flex-col',
  },
} as const;

// Helper to get nav link class based on active state
export const getNavLinkClass = ({ isActive }: { isActive: boolean }) => {
  const { base, active, inactive } = styles.nav;
  return `${base} ${isActive ? active : inactive}`;
};

// Helper to get mobile nav class based on open state
export const getMobileNavClass = (isOpen: boolean) => {
  const { base, open, closed } = styles.mobile.nav;
  return `${base} ${isOpen ? open : closed}`;
};
