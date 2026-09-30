import { type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { sentence } from '../lib/format';

export function Button({ variant = 'primary', className = '', ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'text' | 'danger' }) {
  return <button className={`button button--${variant} ${className}`} {...props} />;
}

export function PageTitle({ eyebrow, title, description, actions }: { eyebrow?: string; title: string; description?: string; actions?: ReactNode }) {
  return <header className="page-title">
    <div>
      {eyebrow && <p className="eyebrow">{eyebrow}</p>}
      <h1>{title}</h1>
      {description && <p className="page-title__description">{description}</p>}
    </div>
    {actions && <div className="button-set">{actions}</div>}
  </header>;
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <section className={`card ${className}`}>{children}</section>;
}

export function StatusTag({ value }: { value: string }) {
  const normalized = value.toLowerCase();
  const tone = normalized.includes('reject') || normalized.includes('cancel') || normalized.includes('late')
    ? 'danger'
    : normalized.includes('pending') || normalized.includes('correction') || normalized.includes('early')
      ? 'warning'
      : normalized.includes('approve') || normalized.includes('complete') || normalized.includes('on time') || normalized.includes('ontime')
        ? 'success'
        : 'neutral';
  return <span className={`tag tag--${tone}`}>{sentence(value)}</span>;
}

export function Field({ label, error, hint, required, children }: { label: string; error?: string; hint?: string; required?: boolean; children: ReactNode }) {
  return <label className="field">
    <span className="field__label">{label}{required && <span aria-hidden="true"> *</span>}</span>
    <span className="field__control">{children}</span>
    {hint && !error && <span className="field__hint">{hint}</span>}
    {error && <span className="field__error" role="alert">{error}</span>}
  </label>;
}

export function TextInput(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input className="input" {...props} />;
}

export function EmptyState({ title, message, action }: { title: string; message: string; action?: ReactNode }) {
  return <div className="empty-state">
    <div className="empty-state__shape" aria-hidden="true">◇</div>
    <h2>{title}</h2>
    <p>{message}</p>
    {action}
  </div>;
}

export function Alert({ tone = 'info', title, children }: { tone?: 'info' | 'success' | 'warning' | 'danger'; title: string; children?: ReactNode }) {
  return <div className={`alert alert--${tone}`} role={tone === 'danger' ? 'alert' : 'status'}>
    <strong>{title}</strong>
    {children && <div>{children}</div>}
  </div>;
}

export function Loading({ label = 'Loading' }: { label?: string }) {
  return <div className="loading" role="status"><span className="spinner" aria-hidden="true" /><span>{label}</span></div>;
}

export function Breadcrumbs({ items }: { items: Array<{ label: string; to?: string }> }) {
  return <nav aria-label="Breadcrumb"><ol className="breadcrumbs">{items.map((item, index) => <li key={`${item.label}-${index}`}>{item.to ? <Link to={item.to}>{item.label}</Link> : <span aria-current="page">{item.label}</span>}</li>)}</ol></nav>;
}

export function Modal({ title, children, onClose, actions }: { title: string; children: ReactNode; onClose: () => void; actions: ReactNode }) {
  return <div className="modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}>
    <section className="modal" role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <header><h2 id="modal-title">{title}</h2><button className="icon-button" type="button" onClick={onClose} aria-label="Close">×</button></header>
      <div className="modal__body">{children}</div>
      <footer className="button-set">{actions}</footer>
    </section>
  </div>;
}

export function Pagination({ page, pageSize, total, onChange }: { page: number; pageSize: number; total: number; onChange: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  return <div className="pagination"><span>Page {page} of {pages} · {total} records</span><div className="button-set"><Button variant="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>Previous</Button><Button variant="secondary" disabled={page >= pages} onClick={() => onChange(page + 1)}>Next</Button></div></div>;
}
