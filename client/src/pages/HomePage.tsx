export function HomePage() {
  return (
    <div className="space-y-8">
      <section className="border border-[var(--silver)] bg-[var(--white)] p-8 shadow-[0_2px_8px_rgba(20,32,48,0.06)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--royal-blue)]">Enterprise visitor management</p>
        <h1 className="display mt-4 max-w-2xl text-4xl font-bold leading-tight text-[var(--royal-blue)] md:text-5xl">A considered welcome, from arrival to departure.</h1>
        <p className="mt-5 max-w-2xl text-base leading-7 text-[var(--muted)]">The RRVMS platform foundation is ready for the next phase of secure visitor workflows.</p>
      </section>
      <section className="grid gap-5 md:grid-cols-3">
        {['Requests', 'Hosts', 'Security'].map((label) => <div key={label} className="border border-[var(--silver)] bg-[var(--white)] p-6"><p className="text-xs uppercase tracking-[0.18em] text-[var(--muted)]">{label}</p><p className="mt-4 text-2xl font-semibold text-[var(--royal-blue)]">Coming soon</p><span className="mt-5 inline-block border border-[var(--silver)] bg-[var(--surface)] px-3 py-1 text-xs font-medium uppercase tracking-wide text-[var(--muted)]">Foundation</span></div>)}
      </section>
    </div>
  )
}
