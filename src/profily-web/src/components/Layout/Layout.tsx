import { useState, useEffect } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { GlowBackground } from '../GlowBackground/GlowBackground';
import { Menu, X, Sparkles, Github, Heart, Twitter, Linkedin, Loader2, LogOut } from 'lucide-react';
import { styles, getNavLinkClass, getMobileNavClass } from './Layout.styles';
import { useAuth } from '../../contexts/AuthContext';

export function Layout() {
  const [menuOpen, setMenuOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false); 
  const location = useLocation();
  const { user, isAuthenticated, isLoading, login, logout} = useAuth();

  // Close menu on route change
  useEffect(() => {
    setMenuOpen(false);
    setUserMenuOpen(false);
  }, [location.pathname]);

  // Close menu on scroll
  useEffect(() => {
    const handleScroll = () => {
      if (menuOpen) setMenuOpen(false);
      if (userMenuOpen) setUserMenuOpen(false);
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, [menuOpen, userMenuOpen]);

  // Prevent body scroll when menu is open
  useEffect(() => {
    document.body.style.overflow = menuOpen ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [menuOpen]);

  // Close user menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (userMenuOpen && !(event.target as Element).closest('[data-user-menu')) {
        setUserMenuOpen(false);
      }
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [userMenuOpen]);

  const closeMenu = () => setMenuOpen(false);
  
  const handleLogout = async () => {
    setUserMenuOpen(false);
    await logout();
  }

  return (
    <>
      <GlowBackground />
      <div className={styles.layout.wrapper}>
        {/* Header */}
        <header className={styles.header.wrapper}>
          <NavLink to="/" className={styles.header.logo.link}>
            <div className={styles.header.logo.icon}>
              <Sparkles size={18} className="text-white" />
            </div>
            <span className={styles.header.logo.text}>Profily</span>
          </NavLink>

          <button
            className={styles.header.mobileToggle}
            onClick={() => setMenuOpen(!menuOpen)}
            aria-label="Toggle menu"
            aria-expanded={menuOpen}
          >
            {menuOpen ? <X size={22} /> : <Menu size={22} />}
          </button>

          <nav className={styles.header.desktopNav}>
            <NavLink to="/" end className={getNavLinkClass}>Home</NavLink>
            <NavLink to="/portfolio" className={getNavLinkClass}>Portfolio Builder</NavLink>
            <NavLink to="/profile" className={getNavLinkClass}>Profile Builder</NavLink>
            <NavLink to="/templates" className={getNavLinkClass}>Templates</NavLink>
          </nav>

          {/* Auth Button / User Menu */}
          <div className={styles.header.auth.wrapper} data-user-menu>
            {isLoading ? (
              <div className="flex item-center justify-center w-10 h-10">
                <Loader2 size={20} className="animate-spin text-slate-400" />
              </div>
            ) : isAuthenticated && user ? (
              <div className="relative">
                <button
                  onClick={() => setUserMenuOpen(!userMenuOpen)}
                  className="flex items-center gap-2 p-1 rounded-full hover:bg-white/10 transition-colors"
                  aria-expanded={userMenuOpen}
                  aria-haspopup="true"
                >
                  <img
                    src={user.avatarUrl  || `https://github/${user.gitHubUsername}.png`}
                    alt={user.gitHubUsername}
                    className="w-8 h-8 rounded-full border border-white/20"
                  />
                </button>

                {/* Dropdown Menu */}
                {userMenuOpen && (
                  <div className="absolute right-0 mt-2 w-48 bg-slate-800 border border-white/10 rounded-lg shadow-xl py-1 z-50">
                    <div className="px-4 py-2 border-b border-white/10">
                      <p className="text-sm font-medium text-white truncate">
                        {user.gitHubUsername}
                      </p>
                      {user.email && (
                        <p className="text-xs text-slate-400 truncate">{user.email}</p>
                      )}
                    </div>
                    <button
                      onClick={handleLogout}
                      className="w-full flex items-center gap-2 px-4 py-2 text-sm text-slate-300 hover:bg-white/5 hover:text-white transition-colors"
                    >
                      <LogOut size={16} />
                      Sign Out
                    </button>
                  </div>
                )}
              </div>
            ): (
              <button onClick={login} className={styles.header.auth.login}>
                <Github size={16} />
                Sign In
              </button>
            )}
          </div>
        </header>

        {/* Mobile Menu Overlay */}
        {menuOpen && (
          <div className={styles.mobile.overlay} onClick={closeMenu} />
        )}

        {/* Mobile Nav */}
        <nav className={getMobileNavClass(menuOpen)}>
          <NavLink to="/" end className={getNavLinkClass} onClick={closeMenu}>Home</NavLink>
          <NavLink to="/portfolio" className={getNavLinkClass} onClick={closeMenu}>Portfolio Builder</NavLink>
          <NavLink to="/templates" className={getNavLinkClass} onClick={closeMenu}>Templates</NavLink>
          <NavLink to="/profile" className={getNavLinkClass} onClick={closeMenu}>Profile Builder</NavLink>
          
          {/* Mobile Auth */}
          <div className={styles.header.mobileAuth.wrapper}>
            {isLoading ? (
              <div className="flex items-center justify-center py-2">
                <Loader2 size={20} className="animate-spin text-slate-400" />
              </div>
            ) : isAuthenticated && user ? (
              <div className="flex flex-col gap-2">
                <div className="flex items-center gap-3 px-3 py-2">
                  <img
                    src={user.avatarUrl || `https://github.com/${user.gitHubUsername}.png`}
                    alt={user.gitHubUsername}
                    className="w-8 h-8 rounded-full border border-white/20"
                  />
                  <span className="text-sm font-medium text-white">{user.gitHubUsername}</span>
                </div>
                <button
                  onClick={async () => {closeMenu(); await logout(); }}
                  className="flex items-center gap-2 px-3 py-2 text-sm text-slate-300 hover:text-white transition-colors"
                >
                  <LogOut size={16} />
                  SignOut 
                </button>
              </div>
            ) : (
              <button onClick={() => { closeMenu(); login(); }} className={styles.header.mobileAuth.login}>
                <Github size={16} />
                Sign In
              </button>
            )}
          </div>
        </nav>

        {/* Main Content */}
        <main className={styles.main}>
          <Outlet />
        </main>

        {/* Footer */}
        <footer className={styles.footer.wrapper}>
          <div className={styles.footer.container}>
            <div className={styles.footer.grid}>
              {/* Brand */}
              <div className={styles.footer.brand.wrapper}>
                <NavLink to="/" className={styles.footer.brand.logo}>
                  <div className={styles.footer.brand.logoIcon}>
                    <Sparkles size={18} className="text-white" />
                  </div>
                  <span className={styles.footer.brand.logoText}>Profily</span>
                </NavLink>
                <p className={styles.footer.brand.description}>
                  Build stunning developer portfolios and profiles with ease. 
                  Stand out from the crowd with professional templates.
                </p>
              </div>

              {/* Quick Links */}
              <div>
                <h4 className={styles.footer.section.title}>Quick Links</h4>
                <ul className={styles.footer.section.list}>
                  <li><NavLink to="/" className={styles.footer.section.link}>Home</NavLink></li>
                  <li><NavLink to="/portfolio" className={styles.footer.section.link}>Portfolio Builder</NavLink></li>
                  <li><NavLink to="/templates" className={styles.footer.section.link}>Templates</NavLink></li>
                  <li><NavLink to="/profile" className={styles.footer.section.link}>Profile Builder</NavLink></li>
                </ul>
              </div>

              {/* Resources */}
              <div>
                <h4 className={styles.footer.section.title}>Resources</h4>
                <ul className={styles.footer.section.list}>
                  <li><a href="#" className={styles.footer.section.link}>Documentation</a></li>
                  <li><a href="#" className={styles.footer.section.link}>API Reference</a></li>
                  <li><a href="#" className={styles.footer.section.link}>Examples</a></li>
                </ul>
              </div>

              {/* Connect */}
              <div>
                <h4 className={styles.footer.section.title}>Connect</h4>
                <div className={styles.footer.social.wrapper}>
                  <a href="https://github.com/m7mdraafat/Profily" target="_blank" rel="noopener noreferrer" className={styles.footer.social.icon} aria-label="GitHub">
                    <Github size={18} />
                  </a>
                  <a href="https://twitter.com" target="_blank" rel="noopener noreferrer" className={styles.footer.social.icon} aria-label="Twitter">
                    <Twitter size={18} />
                  </a>
                  <a href="https://linkedin.com" target="_blank" rel="noopener noreferrer" className={styles.footer.social.icon} aria-label="LinkedIn">
                    <Linkedin size={18} />
                  </a>
                </div>
              </div>
            </div>

            {/* Bottom */}
            <div className={styles.footer.bottom.wrapper}>
              <div className={styles.footer.bottom.content}>
                <p className={styles.footer.bottom.copyright}>
                  © {new Date().getFullYear()} Profily. All rights reserved.
                </p>
                <p className={styles.footer.bottom.madeWith}>
                  Made with <Heart size={14} className="text-red-500 fill-red-500" /> for developers
                </p>
              </div>
            </div>
          </div>
        </footer>
      </div>
    </>
  );
}
