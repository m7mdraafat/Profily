import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { GlowBackground } from '../GlowBackground/GlowBackground';
import styles from './Layout.module.css';

export function Layout() {
  const [menuOpen, setMenuOpen] = useState(false);

  const closeMenu = () => setMenuOpen(false);

  return (
    <>
      <GlowBackground />
      <div className={styles.wrapper}>
        <header className={styles.header}>
          <h1 className={styles.logo}>Profily</h1>
          
          <button 
            className={styles.menuToggle} 
            onClick={() => setMenuOpen(!menuOpen)}
            aria-label="Toggle menu"
            aria-expanded={menuOpen}
          >
            <span className={`${styles.hamburger} ${menuOpen ? styles.hamburgerOpen : ''}`} />
          </button>

          <nav className={`${styles.nav} ${menuOpen ? styles.navOpen : ''}`}>
            <NavLink 
              to="/" 
              end 
              className={({ isActive }) => isActive ? styles.navLinkActive : styles.navLink}
              onClick={closeMenu}
            >
              Home
            </NavLink>
            <NavLink 
              to="/portfolio" 
              className={({ isActive }) => isActive ? styles.navLinkActive : styles.navLink}
              onClick={closeMenu}
            >
              Portfolio Builder
            </NavLink>
            <NavLink 
              to="/templates" 
              className={({ isActive }) => isActive ? styles.navLinkActive : styles.navLink}
              onClick={closeMenu}
            >
              Templates
            </NavLink>
            <NavLink 
              to="/profile" 
              className={({ isActive }) => isActive ? styles.navLinkActive : styles.navLink}
              onClick={closeMenu}
            >
              Profile Builder
            </NavLink>
          </nav>
        </header>

        <main className={styles.main}>
          <Outlet />
        </main>
      </div>
    </>
  );
}