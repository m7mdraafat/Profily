import './App.css'
import { Routes, Route } from 'react-router-dom'
import { HomePage } from './pages/HomePage'
import { PortfolioBuilderPage } from './pages/PortfolioBuilderPage'
import { TemplatesPage } from './pages/TemplatesPage'
import { ProfileBuilderPage } from './pages/ProfileBuilderPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { Layout } from './components';

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

export default App