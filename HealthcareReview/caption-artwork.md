# Caption artwork

Images are organized under `Assets/Food for Thought Images/Captions/<topic ID>/<subtopic ID>/<caption template ID>.png`.

The gallery `caption-artwork.html` pairs each image with its caption. `caption-image-manifest.json` records the topic names, caption text, paths, and full generation prompts. Images use the built-in image generation tool with the approved strawberry illustration as a style reference. The strawberry image is reused from the approved sample.

In Unity, Tools > Food for Thought > Link All Caption Images imports the images as sprites and updates the Game scene's TacticManager caption image links. The original image folders are preserved.

Existing artwork is preserved. The strawberry caption links directly to its original approved file. Nine older Food Safety illustrations remain in their original folders and appear in the gallery; their older caption template is not in the current 54-caption data. The linking tool preserves existing caption links to original artwork and fills missing slots with generated images.
