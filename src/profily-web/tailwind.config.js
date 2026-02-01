/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        bg: {
          primary: '#0f172a',
          secondary: '#1e293b',
          card: 'rgba(255, 255, 255, 0.08)',
        },
        border: {
          subtle: 'rgba(255, 255, 255, 0.15)',
        },
        accent: {
          blue: '#2563eb',
          'blue-light': '#60a5fa',
          purple: '#7c3aed',
          pink: '#ec4899',
          emerald: '#10b981',
        },
        text: {
          primary: '#ffffff',
          secondary: '#e2e8f0',
          muted: '#94a3b8',
        },
      },
      fontFamily: {
        body: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        heading: ['Space Grotesk', 'Inter', 'sans-serif'],
      },
      backdropBlur: {
        glass: '10px',
      },
    },
  },
  plugins: [],
}
