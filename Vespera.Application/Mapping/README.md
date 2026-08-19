# Mapping convention: Mapster

Vespera uses **Mapster**, not AutoMapper and not hand-written mapping methods, for every
Domain-entity-to-DTO projection.

## Why

- **Assembly-scanned, zero central registry.** Every feature owns exactly one `IRegister`
  class colocated with its DTOs (e.g. `Features/Departments/DepartmentMappingConfig.cs`).
  `TypeAdapterConfig.GlobalSettings.Scan(assembly)` finds all of them. It runs from a
  `[ModuleInitializer]` (`Mapping/MapsterModuleInitializer.cs`), not from
  `ApplicationServiceCollectionExtensions.AddApplication` — mapping has to work in unit tests
  too, which call handlers directly and never run DI registration. There is no growing
  "MappingProfile god file" to merge-conflict over as bounded contexts are added.
- **`ProjectToType<T>()` on `IQueryable`.** For read-heavy or mobile-shaped list endpoints,
  Mapster can push the projection into the query itself (via `IVesperaDbContext`) so only the
  columns the DTO needs are selected — this matters for `CursorPagedResult<T>` / delta-sync
  responses over a mobile connection. AutoMapper's `ProjectTo` does the same thing, but with
  materially more configuration ceremony per profile; hand-written mapping methods can't do it
  at all without duplicating query logic.
- **No runtime reflection cost per call.** Mapster compiles adapters once; explicit
  hand-written `ToDto()` methods would avoid that cost too, but at the price of every
  aggregate — and Vespera has ~30 of them — needing its own boilerplate method that has to be
  kept in sync by hand.
- Single dependency, MIT-licensed, actively maintained.

## Rules

1. One `IRegister` class per feature folder. Never a shared/global mapping file.
2. Full DTOs are named `<Entity>Dto`. Where a mobile list response needs a lighter payload,
   add a separate `<Entity>SummaryDto` (ID + display-critical fields only) with its own
   explicit map — do not reuse the full DTO's map and ignore fields on the way out.
3. **Typed IDs must be mapped explicitly.** Every aggregate ID is a
   `readonly record struct FooId(Guid Value)`, not a bare `Guid`. Mapster's convention-based
   member matching does not unwrap these — every `IRegister` must include an explicit
   `.Map(dest => dest.Id, src => src.Id.Value)` (and the same for any nullable foreign-key-typed
   ID) or the mapped DTO will end up with the ID silently left at `default`.
4. Commands never return entities. A create/update handler returns `Result<Guid>` (or another
   primitive/simple `Result<T>`) built by hand — there is nothing worth mapping on the write
   side.
5. Query handlers reading a small/bounded set go through `IReadRepository<T>` +
   `ISpecification<T>`, materializing entities and then calling `.Adapt<TDto>()` — see
   `Features/Departments/GetDepartmentsQueryHandler.cs`. Reserve `IVesperaDbContext` +
   `ProjectToType<TDto>()` for slices that are read-heavy enough that pushing column
   selection into the query matters.
