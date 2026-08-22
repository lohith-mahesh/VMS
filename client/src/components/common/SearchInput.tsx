export function SearchInput() {
  return <label className="flex h-9 w-64 items-center gap-2 border border-[var(--silver)] bg-[var(--surface)] px-3 text-[var(--muted)] focus-within:border-[var(--royal-blue)]"><span aria-hidden="true" className="text-sm">/</span><input aria-label="Search" placeholder="Search RRVMS" className="min-w-0 flex-1 bg-transparent text-sm text-[var(--ink)] outline-none placeholder:text-[var(--muted)]" /></label>
}
