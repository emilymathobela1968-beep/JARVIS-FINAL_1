import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  envPrefix: ['VITE_', 'REACT_APP_'],
  server: {
    host: '0.0.0.0',
    port: 3000,
    allowedHosts: true,
  },
});
