# Sunny Yellow

Applied to all three scenes, runtime tactic cards, and the caption/word-choice prefabs on 2026-09-25.

| Role | Color |
| --- | --- |
| Screen backgrounds | Warm ivory #FFFAF2 |
| Content surfaces | White #FFFFFF |
| Primary actions and selected tactic cards | Sunny yellow #F5C84C |
| Secondary buttons and unselected cards | Neutral sand #EEE8DF |
| Coach and explanation panels | Neutral sand #EEE8DF |
| Body text | Navy #242C45 |
| Text on yellow actions | Navy #242C45 |
| Day and money badges | Yellow #FFE477 |
| Text on yellow and title artwork | Dark purple #292338 |

Role-based colors are centralized in `Assets/Scripts/GamePalette.cs`. `Card.cs` switches surface and text together when a tactic is selected. Existing card locking is preserved.

Use **Tools > Food for Thought > Apply Sunny Yellow Palette** to reapply authored colors. Stop Play Mode first. The command saves open scenes before applying the theme and also updates caption prefab lettering. **Improve Caption Readability** preserves the yellow button artwork and applies dark letters with a subtle raised shadow. Those prefabs retain their existing scale and compact font sizes.

Text-material copies are under `Assets/UI/Theme/`; the original font assets remain intact. Cat, persona, food artwork, and phone frame are preserved. The tactic explorer's layout and interactions are retained.

Validation: Unity compiled and applied the theme with no console errors. Main Page, Home, and topic selection were visually checked. Scene and card layout values, script references, and button bindings match snapshots taken before this palette change. Full gameplay was not rerun for this color-only change.
