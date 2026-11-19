# Stream Star asset generation

`build.py` reads `sources.json`, resizes the original image-tool illustrations into the game resource folder, draws supersampled UI pieces, and synthesizes deterministic audio with numpy and wave.

Run from the repository root:

```powershell
python tools/assetgen/build.py
python tools/assetgen/verify.py
```

The illustration source paths refer to the image tool's retained original files. `prompts.json` records the exact prompt set. Final game assets are self-contained and do not depend on those source files at runtime.

`verify.py` checks the explicit asset inventory rather than discovering files, so missing files fail verification. It also writes `verification.txt` and a labelled `contact-sheet.jpg` for review. No Unity `.meta` files are generated.
