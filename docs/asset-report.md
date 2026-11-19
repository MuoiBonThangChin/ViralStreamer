# Stream Star asset report

## Production

- Built-in image generation: original cozy streaming-room background, Nova in five outfits, and all 33 equipment, décor, skill, currency, and navigation icons. The prompt set is recorded in `tools/assetgen/prompts.json`.
- Pillow post-processing: exact PNG sizes, RGBA encoding, preserved generated transparency, centred subjects, approximately 8% icon padding, and character waist alignment at the bottom edge.
- Procedural Pillow: seven UI pieces, with 4× supersampling for clean shape edges. The glow uses a white radial alpha gradient; the LIVE badge uses bold white lettering.
- Procedural numpy/wave: nine original effects and one 20-second lo-fi music loop. All WAVs use uncompressed 44,100 Hz, 16-bit mono PCM. Effects peak at −3 dBFS with short endpoint fades. Music peaks at −14 dBFS; its eight-bar phrase at 96 BPM includes wrapped note tails and matched seam endpoints.

## Locations and reproduction

- Art: `Assets/Game/Resources/Art/`
- Audio: `Assets/Game/Resources/Audio/`
- Helpers, source manifest, prompts, contact sheet, and verification output: `tools/assetgen/`
- Rebuild: `python tools/assetgen/build.py`
- Verify: `python tools/assetgen/verify.py`

The source manifest points to retained image-tool originals outside the repository. Runtime assets are independent of those originals. No `.meta` files are created. The explicit request to keep helpers under `tools/assetgen/` and write this report is followed in addition to the resource-folder destinations in the brief.

## Verification

`python tools/assetgen/verify.py` completed successfully: **46 PNGs + 10 WAVs; 0 failures; no missing entries.** The printed table is retained in `tools/assetgen/verification.txt`.

Checks cover exact filenames and pixel sizes, PNG/RGBA format, opaque room background, transparent sprite backgrounds, icon padding, character bottom alignment, UI white centres and grey button band, WAV encoding, durations, peak levels, effect endpoint fades, and the music seam. The complete labelled contact sheet is `tools/assetgen/contact-sheet.jpg`; the room, character lineup, UI, and icon subjects were visually reviewed. The follower icon was regenerated as a plain person silhouette with a plus badge to match the brief.

All requested assets were produced. No missing deliverables or known production blockers remain. The audio received numerical verification; in-game playback and Unity import settings were outside this asset-only task.
