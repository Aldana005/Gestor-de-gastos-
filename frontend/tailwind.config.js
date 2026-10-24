/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          900: 'var(--brand-900)',
          800: 'var(--brand-800)',
          700: 'var(--brand-700)',
          600: 'var(--brand-600)',
          500: 'var(--brand-500)',
          300: 'var(--brand-300)',
          100: 'var(--brand-100)',
        },
        income: {
          DEFAULT: 'var(--color-income)',
          bg: 'var(--color-income-bg)',
          border: 'var(--color-income-border)',
        },
        expense: {
          DEFAULT: 'var(--color-expense)',
          bg: 'var(--color-expense-bg)',
          border: 'var(--color-expense-border)',
        },
        warning: {
          DEFAULT: 'var(--color-warning)',
          bg: 'var(--color-warning-bg)',
          border: 'var(--color-warning-border)',
        },
        glass: {
          bg: 'var(--glass-card-bg)',
          hover: 'var(--glass-card-bg-hover)',
          border: 'var(--glass-card-border)',
          'border-active': 'var(--glass-card-border-active)',
          inner: 'var(--glass-card-inner)',
        },
        text: {
          primary: 'var(--text-primary)',
          secondary: 'var(--text-secondary)',
          muted: 'var(--text-muted)',
          inverse: 'var(--text-inverse)',
        },
      },
      borderRadius: {
        'card-sm': 'var(--radius-sm)',
        'card': 'var(--radius-card)',
        'card-lg': 'var(--radius-card-lg)',
        'pill': 'var(--radius-pill)',
      },
      minHeight: {
        'touch': '44px',
      },
      minWidth: {
        'touch': '44px',
      },
      boxShadow: {
        'glass': 'var(--shadow-soft)',
        'glow': 'var(--shadow-glow)',
        'floating': 'var(--shadow-floating)',
      },
      backdropBlur: {
        'glass': '16px',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
    },
  },
  plugins: [],
}

