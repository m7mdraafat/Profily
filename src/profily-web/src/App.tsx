import './App.css'
import {
  Routes,
  Route,
  NavLink,
  Outlet
} from 'react-router-dom'
import { HomePage } from './pages/HomePage'
import { PortfolioBuilderPage } from './pages/PortfolioBuilderPage'
import { TemplatesPage } from './pages/TemplatesPage'
import { ProfileBuilderPage } from './pages/ProfileBuilderPage'
import { NotFoundPage } from './pages/NotFoundPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="portfolio" element={<PortfolioBuilderPage />} />
        <Route path="templates" element={<TemplatesPage />} />
        <Route path="profile" element={<ProfileBuilderPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}

function Layout() {
  return (
    <>
      <header>
        <h1>Profily</h1>
        <nav>
          <NavLink to="/" end>Home</NavLink> {" "}
          <NavLink to="/portfolio">Portfolio Builder</NavLink>{ " " }
          <NavLink to="/templates">Templates</NavLink> {" "}
          <NavLink to="/profile">Profile Builder</NavLink>
        </nav>
      </header>

      <main>
        <Outlet />
      </main>
    </>
  )
}
export default App
