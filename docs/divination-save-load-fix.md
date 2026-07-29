# Divination save-load duplicate fix

## Finding

The repeated Act 3 second-boss prophecy is a Diviner state-scoping bug exposed by save/load, not the save loader appending the same record twice.

- The affected save contains one `Boss.Act3.Second` record.
- After loading, the runtime restores that record into the player-scoped record list.
- Forecast candidate filtering was still reading the obsolete global record list, which remains empty for player-scoped runs.
- The already-recorded boss therefore appeared eligible and could be selected again.

The same stale-list reads also affected ancient limits and option selection, relic queue progression, event duplicate checks, and recent-category weighting.

## Fix

- All forecast providers and category weighting now receive the active player's authoritative record list explicitly.
- Boss divinations are filtered by their one-shot category using the restored player records.
- The append boundary independently refuses to add a duplicate one-shot boss category.
- Save loading removes duplicate one-shot boss records from saves already affected by the bug and writes the repaired player data back.
- Leaving or reloading a run now clears only in-memory Diviner caches instead of invoking the destructive record-clearing operation.
- Repeated player tracking reuses the already-loaded player record list rather than replacing it from serialized data.
- Repeatable categories retain their intended rules: ancient options remain capped at three, while relic forecasts continue advancing through their queues.

## Runtime checks

1. Load a run containing an active Act 3 second-boss divination.
2. Divinate repeatedly and confirm that the same second-boss category never appears again.
3. Confirm ancient forecasts still produce distinct options up to their cap.
4. Confirm repeated relic forecasts advance to the next unforecast relic.
5. Save and reload, then repeat checks 2-4.
