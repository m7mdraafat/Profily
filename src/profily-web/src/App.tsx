import { useState } from 'react'
import './App.css'

function App() {
  const [health, setHealth] = useState<string>('')
  const [error, setError] = useState<string>('')

  async function loadHealth() {
    setError('')
    setHealth('loading...')
    try {
      const res = await fetch('/api/health')
      const data = await res.json()
      setHealth(JSON.stringify(data, null, 2))
    } catch (e) {
      setHealth('')
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  return (
    <div style={{ maxWidth: 900, margin: '0 auto', padding: 24, textAlign: 'left' }}>
      <h1 style={{ marginBottom: 6 }}>Profily</h1>
      <p style={{ marginTop: 0, opacity: 0.8 }}>
        Local UI scaffold (Vite + React + TS) with API proxy.
      </p>

      <section style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
        <button onClick={loadHealth}>Check API health</button>
      </section>

      {error ? (
        <p style={{ color: 'crimson' }}>{error}</p>
      ) : null}

      <section style={{ marginTop: 18 }}>
        <h3 style={{ marginBottom: 6 }}>/api/health</h3>
        <pre style={{ background: '#111', color: '#ddd', padding: 12, borderRadius: 8, overflow: 'auto' }}>
          {health}
        </pre>
      </section>
    </div>
  )
}

export default App
