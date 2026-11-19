# Asset brief — Stream Star: Idle Tycoon

You are producing the art and audio for an original mobile idle-clicker game about a streamer.
Write files ONLY inside `Assets/Game/Resources/Art/` and `Assets/Game/Resources/Audio/` (create the folders). Do not touch anything else in the repository. Do not create `.meta` files.

## How to produce the files

- If you have an image generation tool, use it for the illustrated assets (background, character, icons) and post-process with Python (Pillow) to get exact sizes and transparent backgrounds.
- If you do not, draw everything procedurally with Python + Pillow (vector-style shapes, gradients, anti-aliasing by supersampling 4x then downscaling). Flat, clean, cute vector style is perfectly fine.
- Audio must be synthesized with Python (`wave` + `math`/`numpy`), 44100 Hz, 16-bit mono WAV.
- Exact file names and pixel sizes below are a hard contract: the game loads them by name.
- All content must be original and family-friendly. Do not imitate any existing game, brand, or real person.

## Style

Cute chibi / kawaii vector style, soft gradients, thick clean outlines (dark purple #2B1B4A rather than black), cozy pastel-neon palette: purple #7C4DFF, pink #FF5FA2, cyan #35D6ED, yellow #FFD23F, mint #4ADE80, dark navy #1B1436.

## Art — `Assets/Game/Resources/Art/` (PNG, RGBA)

### Background
| File | Size | Content |
|---|---|---|
| `bg_room.png` | 1080x1920 | Opaque. Cozy bedroom streaming room at night: back wall with a window (city lights), wooden floor in the lower third, a desk on the lower part. The centre of the image (x 240-840, y 500-1300) must stay calm and uncluttered because the character is drawn on top of it. The top 320 px and bottom 760 px get covered by UI, so keep them simple. |

### Character — "Nova" (original chibi streamer)
All five are the SAME character, same pose and framing: chibi girl, big head, short hair, over-ear headphones with a small mic, waist-up, facing the camera, smiling, one hand waving. Transparent background, character centred, feet/waist touching the bottom edge.

| File | Size | Outfit |
|---|---|---|
| `streamer_casual.png` | 768x1024 | Purple hoodie, plain |
| `streamer_gamer.png` | 768x1024 | Black + cyan esports jersey, cyan headphones glow |
| `streamer_idol.png` | 768x1024 | Pink stage dress with star accessories |
| `streamer_cyber.png` | 768x1024 | Futuristic neon jacket, visor on forehead |
| `streamer_royal.png` | 768x1024 | Gold-and-white regal outfit with a small crown |

### Icons (transparent background, subject centred with ~8% padding)
256x256 each:
`gear_webcam`, `gear_mic`, `gear_headset`, `gear_keyboard`, `gear_lights` (ring light), `gear_chair` (gaming chair), `gear_pc` (gaming PC tower with RGB), `gear_greenscreen`,
`decor_plant`, `decor_poster`, `decor_lamp`, `decor_shelf`, `decor_neon` (neon sign shaped like a heart), `decor_trophy`,
`skill_tap` (pointing finger), `skill_engage` (speech bubbles), `skill_crit` (lightning star), `skill_hype` (flame), `skill_offline` (moon with zzz).

128x128 each:
`icon_coin` (gold coin with a star), `icon_gem` (cyan diamond), `icon_heart` (pink heart), `icon_viewer` (eye), `icon_follower` (person silhouette with plus), `icon_gift` (gift box, pink with yellow ribbon), `icon_boost` (rocket), `icon_star` (yellow star), `icon_lock` (padlock), `icon_check` (white check mark in green circle),
`tab_gear` (camera), `tab_skills` (lightning bolt), `tab_room` (sofa), `tab_outfit` (t-shirt).

### UI pieces
| File | Size | Content |
|---|---|---|
| `ui_panel.png` | 96x96 | Pure white rounded rectangle, corner radius 28 px, fully opaque inside, transparent outside. Used as a 9-slice and tinted in code. |
| `ui_button.png` | 96x96 | White rounded rectangle, radius 28 px, with a slightly darker (light grey #DADADA) 8 px band along the bottom edge for a "3D" feel. 9-slice, tinted in code. |
| `ui_pill.png` | 96x96 | Pure white circle-ended capsule (radius 48). 9-slice for bars. |
| `ui_glow.png` | 256x256 | White radial gradient, alpha 1 in the centre to 0 at the edge. |
| `ui_circle.png` | 128x128 | Pure white anti-aliased filled circle. |
| `ui_sparkle.png` | 128x128 | White four-point star sparkle, soft edges. |
| `ui_live.png` | 160x64 | Red (#FF3B5C) capsule with white dot on the left and white bold text "LIVE". |

## Audio — `Assets/Game/Resources/Audio/` (WAV, 44.1 kHz, 16-bit, mono, normalised to about -3 dBFS, short fade in/out to avoid clicks)

| File | Length | Sound |
|---|---|---|
| `sfx_tap.wav` | ~0.08 s | Soft bubbly pop |
| `sfx_crit.wav` | ~0.25 s | Brighter pop with a sparkle on top |
| `sfx_coin.wav` | ~0.25 s | Two-note coin ding (B5 then E6) |
| `sfx_buy.wav` | ~0.30 s | Satisfying "ka-ching" upward blip |
| `sfx_error.wav` | ~0.18 s | Low dull double thud |
| `sfx_gift.wav` | ~0.6 s | Magical ascending arpeggio |
| `sfx_hype.wav` | ~0.9 s | Rising whoosh into a bright chord |
| `sfx_rankup.wav` | ~1.4 s | Short triumphant fanfare (major arpeggio + held chord) |
| `sfx_tab.wav` | ~0.06 s | Tiny UI tick |
| `bgm_loop.wav` | 16-24 s | Seamless loop: mellow lo-fi chord progression with soft pads and a gentle beat, quiet (about -14 dBFS) |

## Done means

- Every file listed exists with the exact name and size.
- `python` check script printed a table of files with sizes/durations and no missing entries.
- Write a short `docs/asset-report.md` listing what was generated with the image tool vs procedurally, and anything you could not do.
