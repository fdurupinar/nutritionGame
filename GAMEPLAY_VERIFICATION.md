# Gameplay verification

Verified in Unity 6000.6.0f1 Play Mode on 2026-09-24, starting directly from `2 Home Page`.

| Check | Result |
| --- | --- |
| Direct Home startup | Loaded saved day 7, cash 22, followers 238, credibility 96; no missing DayManager warning. |
| Same-day posting restriction | Daily Post disabled with HasPostedToday = 1; clicking it did not navigate. |
| End Day without Job Post | Advanced to day 8 and enabled Daily Post. No Job Post interaction was needed. |
| Daily Post navigation | Opened Game; selected Diet Pattern → The Gen-Z Diet → first caption. |
| Incomplete caption | Post/continue control remained disabled until both blanks were filled. |
| Coach warning | Unsuitable word choices opened the warning; Revise returned to the same caption with cleared answers. |
| Valid caption | Friends + energy reached tactic selection. |
| Tactic and publication | Appeal to Emotion + Confirm displayed the completed caption, image, comments and metric animation. |
| Publish completion | Followers 238 → 792; cash 22 → 28; credibility 96 → 95; displayed likes 449. Home appeared after completion. |
| Return Home | Day 8 and final metrics retained; Daily Post disabled again. |
| Save preservation | Stopped Play Mode and restored all changed gameplay preference values to the original snapshot; compared preferences to verify no gameplay differences. |

No C# compilation errors or runtime exceptions were found in the verification log interval. Repeated UI warnings reported `Material Default-Line doesn't have _Stencil property`.

Observed presentation issues: the caption-stage button reads Post although it proceeds to tactic selection; some cat positions still show text placeholders; the published caption appears crowded/clipped between the image and comment area. These did not block the tested flow and were not changed during verification.

Scope: one complete post cycle, coach Revise, direct Home startup and daily gating. Other topics/tactics, coach Continue Anyway, job acceptance/rewards, final-day endings and standalone builds were not exercised.

## Tactic exploration and caption readability (2026-09-25)

Replaced the ambiguous tactic heading with an exploration prompt, kept the composed caption visible, and added a scrollable explanation with reflection questions and authored tactic descriptions. The existing publish action now reads “Publish post” and is disabled without a selected, unlocked card. Browsing does not apply scores or rewrite captions.

Verified in Unity Play Mode: caption-to-tactic flow, card selection and deselection, publish availability, explanation opening, scrolling, and closing. Checked dark caption text on the existing yellow prefab buttons; word-choice text retains its compact size and uses a subtle raised shadow. Final publishing and metric changes were not rerun. Temporary diagnostic helpers were removed.
