import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './app/App';
import { initializeAuthentication } from './auth/auth';
import './styles/index.css';

async function start(): Promise<void> {
  await initializeAuthentication();
  const root = document.getElementById('root');
  if (!root) throw new Error('The application root element was not found.');
  createRoot(root).render(<StrictMode><App /></StrictMode>);
}

void start();
