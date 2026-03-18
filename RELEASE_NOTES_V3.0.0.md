Release-3.0.0 CS2-RETAKE

V3 introduces a production-focused retake package built from upstream CS2Retake plus integrated instant-defuse behavior and major gameplay-flow hardening.

Highlights
- Native CS2 buy menu interception with selection-only persistence.
- Unified `!guns` + native buy persistence model.
- AWP rules: threshold gate, one winner per team, preference toggle flow.
- Pistol round helmet removal (kevlar-only behavior).
- Smart CT defuse-kit distribution with configurable mode and pistol override.
- Integrated InstaDefuse logic in-plugin (no external DLL dependency).
- Runtime config refresh consistency on config parse.
- Event payload consistency fixes for synthetic bomb events.

Breaking/Operational notes
- Release line starts at `v3.0.0`.
- If you previously deployed a separate instadefuse plugin, remove/disable it to avoid duplicated behavior.

Upstream acknowledgement
- Based on CS2Retake by LordFetznschaedl: https://github.com/LordFetznschaedl/CS2Retake
- Integrated ideas from cs2-instadefuse by B3none: https://github.com/B3none/cs2-instadefuse
