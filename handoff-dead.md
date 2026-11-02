# Handoff — Dead (Completed / Archived) Tasks

**Status:** VERIFY-01 completed — objective-centered verification of all 12 figure requirements against the study objectives (RQ1–RQ5).  
**Archived item:** VERIFY-01 — Verified each figure: EXISTS/PARTIAL/MISSING, CORRECT/INCORRECT/UNCERTAIN, with file-path + line evidence and calibration needed. See `FIGURE_OBJECTIVE_VERIFICATION.md`.  
**Archived item:** F-01 - Session JWT attached by `forecastApi.ts` and `manpowerApi.ts` when available.
**Partial item:** F-02 - Stale API fallbacks corrected; explicit reporting when `NEXT_PUBLIC_API_URL` is unset remains incomplete.
**Archived item:** F-03 / F-04 — Frontend forecast model identity fix + localStorage fallback removal (see espasyo/ handoff-dead.md for details)  
**Archived item:** F-05 — The backend endpoint is consumed by a typed frontend client, authenticated lookup page, and incident-entry validation. Verified coordinates are submitted on create. Frontend ESLint and production build pass; no backend changes were required.
**Archived item:** F-06 — Frontend time-of-day distribution chart (see espasyo/ handoff-dead.md for details)  
**Archived item:** F-08 — Temporal preprocessing visualization now matches the backend monthly aggregation and zero-filled series preview (see espasyo/ handoff-dead.md for details).
**Archived item:** F-09 — Dedicated hotspot risk summary and prevention recommendations screen; empty predictions return a neutral summary. Fixed source-cluster lookup null reference by matching forecasts to source incidents by precinct/crime type. Hotspot prediction and empty-result regression tests pass; full-suite failures are outside F-09 (see espasyo/ handoff-dead.md for details).
**Archived item:** F-10 backend — Statistical forecast rows receive composite risk scores from configured crime-type severity, precinct risk, and heinous multipliers; summary averages/maxima are populated. Monthly projected volume by existing forecast risk label is aggregated in the backend for generation and save responses; a saved-run query recomputes the summary from persisted forecast rows, without schema changes. The frontend displays these values in the optional review section and saved forecast overview, with no client-side aggregation or new risk thresholds. Focused forecast-handler/aggregation tests and the Web API build pass; frontend validation is recorded in `espasyo/handoff-dead.md`.
**Archived item:** F-07 — Preprocessing Analysis Records includes the temporal fields for current analysis clusters, case/precinct/crime-type search, time/month/year filters, pagination, and CSV export of all filtered rows. Removed the redundant Dataset tab. Frontend lint/build/diff checks pass.
**Convention:** When an active task is complete, its entry is moved here from `handoff-active.md`, and `handoff-active.md` is reset to "none" (or the next task). TODO.md status is updated to "done".  
**Mirrored at:** workspace root, `espasyo/`, `nin-architecture/`
