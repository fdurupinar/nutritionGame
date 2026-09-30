# Healthcare content review

Caption export refreshed from the game on 2026-09-29. Existing specialist review columns were preserved.

- captions_and_blanks.csv: 54 captions, 108 blank rows. Each row shows the caption, intended word, available choices, scoring lists, and tactic matches. Rows with the same Caption ID belong to the same post.
- tactics.csv: 16 tactic assets referenced by the game scene's day configuration, including examples, debunking explanations, and explorer hints.

Fill in the specialist decision, suggested correction, clinical rationale / evidence URL, and reviewer notes columns. Keep the IDs unchanged so corrections can be matched back to the game. Suggested decisions: Keep, Revise, Remove.

## Meaning of the data

The game includes deliberately misleading health claims as teaching examples. Full-credit/correct words describe intended caption completion, not clinical truth. The example caption removes square brackets from the supplied template; it is not an endorsed health statement. Assess both the misleading examples and the accuracy and clarity of their explanations.

Players choose from the caption's shared word bank to fill its ordered blanks. Options may be shuffled. The blank number identifies the relevant scoring rule. Lists inside cells are separated with |. Tactic categories link captions to tactics through the Matching category column; several cards can share a category. Ideal/neutral/bad describe the game's tactic fit, not whether the health claim is medically valid.

The reviewed captions use full credit (fits), partial credit (partly fits), and no credit (does not fit). Legacy neutral/wrong columns are now empty. The old internal Nonsense category remains for compatibility; player feedback describes caption fit and specific wording problems. Full credit is not a statement of medical accuracy. Global word overrides are empty in the current formula profile. Matching category is the value used by the scoring code (tacticId, falling back to type). Source type label is exported separately because some assets have a matching category of emotion even when their type label is different; this is an existing classification issue to review. Source categories and text are preserved here, including any replacement characters flagged in tactics.csv.

These are review copies. Editing them does not automatically update Unity. Return the edited CSVs for changes to be applied to the JSON and tactic assets. The older tactics.csv in the game's Content/CSV folder is not the basis of this export.
