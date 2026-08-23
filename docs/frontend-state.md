# Frontend state management

This is the Angular equivalent of `docs/CONTRIBUTING-slices.md`'s "which port do I use" section:
one clear default, and one narrow, named exception.

## The default: signals + a per-feature facade

Every feature gets one `<feature>/data/<feature>.facade.ts` — a `providedIn: 'root'` service that
wraps the feature's generated API client(s) and exposes plain Angular `signal()`s (never a raw
`Observable` a component has to `| async` and re-subscribe to). See
`Vespera.Client/src/app/features/departments/data/departments.facade.ts` for the reference shape:

```ts
@Injectable({ providedIn: 'root' })
export class DepartmentsFacade {
  private readonly client = inject(DepartmentsClient);
  private readonly departmentsSignal = signal<DepartmentDto[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);

  readonly departments = this.departmentsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  load(query: DataTableQuery): void {
    this.loadingSignal.set(true);
    this.client.list(...).subscribe({ next: ..., error: ... });
  }
}
```

Components inject the facade, read its signals directly in templates, and call its methods on
user action. No actions, no reducers, no effects, no store module — a facade **is** the store,
scoped to one feature. This covers the overwhelming majority of screens: a list, a form, a detail
view, each backed by one facade with a handful of signals.

**Why this is the default, not NgRx-everywhere**: most feature state in Vespera is "the current
page's data plus its loading/error flags" — there's no cross-view derived state, no
undo/replay need, and no multi-step flow spanning several routes. NgRx's ceremony (actions,
reducers, selectors, effects) buys correctness guarantees that state genuinely needs when it gets
that complex — for everything simpler, it's pure overhead that makes a one-screen CRUD feature
take three times as many files to review.

## The exception: NgRx, for Payroll and Attendance specifically

The brief calls for NgRx on the Payroll and Attendance feature slices specifically — neither
exists yet as of this phase ("no HR features" — see AGENTS.md's working agreement), so there is no
NgRx feature state to show here yet. What *is* wired up now, so those two features can add their
own feature state with zero root-level changes when they're built:

- `@ngrx/store`, `@ngrx/effects`, `@ngrx/store-devtools` are installed.
- `app.config.ts` calls `provideStore({})` (empty root reducer map), `provideEffects([])`, and
  `provideStoreDevtools(...)`.
- A feature adds its own slice via `provideState('payroll', payrollReducer)` /
  `provideEffects(PayrollEffects)` in that feature's own route `providers` array (lazy-loaded, so
  the store slice only exists once the route is visited) — not by editing `app.config.ts`.

**Why Payroll/Attendance specifically, once they exist:** both are the two areas of the spec with
real multi-step, cross-view state — a payroll run moves through several stages with data that
several different screens need to read and react to; attendance aggregates a day's punches across
overlapping views. That's exactly the shape NgRx is built for: several consumers reading the same
derived state, mutations coming from more than one place, replay/undo value during a correction
workflow. Departments (and most other CRUD screens) have none of that — one screen, one facade,
done.

## Rule of thumb when starting a new feature

Start with a facade. Reach for NgRx only when you can point at *today's* multi-screen,
multi-source state need — not a hypothetical future one. If a feature is on the Payroll or
Attendance route tree, use NgRx from the start per the brief; anything else, default to a facade
until it demonstrably outgrows one.
