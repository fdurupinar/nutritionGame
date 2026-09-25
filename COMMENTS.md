# Contextual audience comments

Comments now use the composed post and the same metric preview used for scoring. They do not change scores, call an AI service, or write saved progress.

## Editing

Edit `Assets/Resources/Content/Comments/*.asset` in the Unity Inspector for immediate runtime content changes. Alternatively edit `Assets/Content/CSV/comments.csv` and use **FFT > Import All CSVs**; this also imports the other CSV content files and can overwrite their corresponding asset fields.

| Field | Meaning |
| --- | --- |
| id | Unique comment ID. |
| commenterName / text | Displayed author and reaction. |
| type | Optional tactic category (such as Emotion) or tactic ID (such as emotion). Blank matches all tactics. |
| topicId | Optional JSON topic ID: diet, mom_child, disease, food, supp, weight. |
| subTopicId | Optional JSON subtopic ID. |
| captionTemplateId | Optional exact caption template ID from DailyPostData.json. |
| reaction | Any, ClearCaption, ConfusingWords, or PoorTacticFit. |

All nonblank filters must match. Matching ignores case and surrounding whitespace. ClearCaption means neither word choices nor tactic fit were marked wrong; it is not a claim that the nutrition statement is factual, and it can include partly correct choices. ConfusingWords and PoorTacticFit follow their respective flags in the metric preview. Without formula scoring, only Any reactions are eligible.

Text supports `{topic}`, `{subtopic}`, `{caption}`, and `{tactic}` placeholders. Caption text is the completed post; injected markup is escaped. Use topic/subtopic filters when authoring comments that mention particular claims. One caption-specific example is provided for `diet / carnivore / carnivore_fear_01`; other captions use matching topic and outcome reactions unless more specific comments are authored.

## Selection

The default is four unique comments per post, configurable with Comments Per Post on CommentManager. One matching outcome reaction is reserved first, then caption/subtopic/topic-specific comments are preferred over generic or old tactic-only comments. Equally specific comments are shuffled, so selections can vary. Random selection does not guarantee a different set on every repeated post. If there are fewer eligible comments, all available matches are used. No unrelated topic or mismatched outcome is used just to reach the limit.

25 contextual comments were added alongside the existing 28 comments. Existing files were retained. CSV category aliases supportive and negative now map to positive and skeptic when imported.

## Verification

Unity compiled the change and ran selection checks against all 53 imported assets: four unique results, all six topic filters, caption-specific matching, good/poor-tactic/confusing-word outcomes, legacy scoring fallback, zero limit, escaped placeholders and varied selections passed. CSV rows were checked against the 25 new runtime assets. Tests did not modify saved gameplay. A complete Play Mode publish cycle has not been repeated since this change.
