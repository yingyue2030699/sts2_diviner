# Rest-site stability hotfix

## Player-facing patch note draft

- Fixed a combat-state cleanup risk that could leave generated cards without a pile when several cards were created at once. Generated cards are now created and placed atomically, and their destination pile is explicitly refreshed.
- Hardened Rewrite the Sign by transforming all eligible Misfortunes in one supported transform operation instead of starting overlapping per-card transform sequences.
- Hardened Marked Deck card rewards by applying its extra choice only in the late reward pass and removing permanent references to prior reward lists.
- Added persistent diagnostics around generated cards, combat cleanup, rest-site option generation, and rest-site room setup. If a rest site still fails, `godot.log` will now identify the failed stage and retain the exception stack trace.

## Investigation record

- The affected Workshop update was manifest `2301698023414714474`; the known-good rollback is manifest `2515444233538704360`.
- The multiplayer state-scoping merge was made after the affected Workshop upload and was not present in that manifest.
- The rest-site PNG, imported rest-site texture, and related PCK entries were identical between the known-good and affected packages.
- Diviner does not override `TryModifyRestSiteOptions`; base-game generation creates Heal and Smith before invoking run hooks.
- The affected DLL introduced a generated-card batch path which registered every card in `CombatState` before adding the first card to a pile. A failure during a later add could therefore leave floating cards behind during room transition.
- The affected DLL also changed Rewrite the Sign to run one awaited transform operation per Misfortune and added permanent static tracking of Marked Deck reward-list objects. Both paths are now bounded to one operation per invocation.

## Diagnostic log markers

- `Diviner generated-card add begin/complete`
- `Diviner Rewrite the Sign transform begin/complete`
- `Diviner Marked Deck modified reward`
- `Diviner combat cleanup begin`
- `Diviner runtime cleared after ...`
- `Diviner rest-site generation begin/complete`
- `Diviner rest-site room ready begin/complete`
- `Diviner rest-site generation threw an exception`
- `Diviner rest-site room setup threw an exception`
