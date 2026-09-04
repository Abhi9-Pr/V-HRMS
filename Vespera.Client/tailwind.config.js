/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: ['selector', '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: 'var(--vespera-color-primary)',
          hover: 'var(--vespera-color-primary-hover)',
        },
        surface: 'var(--vespera-color-surface)',
        'surface-alt': 'var(--vespera-color-surface-alt)',
        border: 'var(--vespera-color-border)',
        text: {
          DEFAULT: 'var(--vespera-color-text)',
          muted: 'var(--vespera-color-text-muted)',
        },
        danger: 'var(--vespera-color-danger)',
        warning: 'var(--vespera-color-warning)',
        success: 'var(--vespera-color-success)',
      },
      spacing: {
        1: 'var(--vespera-space-1)',
        2: 'var(--vespera-space-2)',
        3: 'var(--vespera-space-3)',
        4: 'var(--vespera-space-4)',
        6: 'var(--vespera-space-6)',
        8: 'var(--vespera-space-8)',
        12: 'var(--vespera-space-12)',
        16: 'var(--vespera-space-16)',
      },
      fontFamily: {
        sans: 'var(--vespera-font-family)',
      },
      boxShadow: {
        1: 'var(--vespera-elevation-1)',
        2: 'var(--vespera-elevation-2)',
        3: 'var(--vespera-elevation-3)',
      },
      borderRadius: {
        sm: 'var(--vespera-radius-sm)',
        md: 'var(--vespera-radius-md)',
        lg: 'var(--vespera-radius-lg)',
      },
      transitionDuration: {
        fast: 'var(--vespera-transition-fast)',
        base: 'var(--vespera-transition-base)',
      },
      maxWidth: {
        content: 'var(--vespera-max-width-content)',
      },
    },
  },
  // Angular Material renders its own markup at runtime — Tailwind's base reset stays on
  // (see docs/CONTRIBUTING-frontend.md), so no corePlugins are disabled here.
  plugins: [],
};
