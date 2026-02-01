import { useState, useEffect } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { GlowBackground } from '../GlowBackground/GlowBackground';
import { Menu, X, Sparkles, Github, Heart, Twitter, Linkedin } from 'lucide-react';
import { styles, getNavLinkClass, getMobileNavClass } from './Layout.styles';

export function Layout() {
  const [menuOpen, setMenuOpen] = useState(false);
  const location = useLocation();

  // Close menu on route change
  useEffect(() => {
    setMenuOpen(false);
  }, [location.pathname]);

  // Close menu on scroll
  useEffect(() => {
    const handleScroll = () => {
      if (menuOpen) setMenuOpen(false);
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, [menuOpen]);

  // Prevent body scroll when menu is open
  useEffect(() => {
    document.body.style.overflow = menuOpen ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [menuOpen]);

  const closeMenu = () => setMenuOpen(false);

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

          {/* Auth Button */}
          <div className={styles.header.auth.wrapper}>
            <button className={styles.header.auth.login}>
              <Github size={16} />
              Sign In
            </button>
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
            <button className={styles.header.mobileAuth.login} onClick={closeMenu}>
              <Github size={16} />
              Sign In
            </button>
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
