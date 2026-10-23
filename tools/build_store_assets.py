#!/usr/bin/env python3
"""Build the App Store and Google Play artwork from simulator captures.

The captures themselves come from the iOS simulator (see stores/README.md);
this script only crops, resizes, composites, and strips alpha so every file
matches the size and colour-space rules both stores enforce.

Usage:
    python3 tools/build_store_assets.py [capture-dir]

`capture-dir` defaults to /tmp/cap and must contain:

    iphone/<name>.png   2868x1320 iPhone 6.9" captures (landscape, 19.5:9)
    ipad/<name>.png     2752x2064 iPad 13" captures (landscape, 4:3)
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
CAPTURES = Path(sys.argv[1] if len(sys.argv) > 1 else "/tmp/cap")
STORES = ROOT / "stores"
FONTS = ROOT / "tools" / "fonts"
TITLE_ART = ROOT / "src" / "Mutants.Game" / "Content" / "Art" / "title.png"
APP_ICON = ROOT / "src" / "Mutants.iOS" / "Assets.xcassets" / "AppIcon.appiconset" / "AppIcon-1024.png"

INK = (11, 7, 20)
LIME = (182, 242, 107)
BONE = (236, 230, 216)
MUTED = (185, 174, 200)

IPHONE_FRAME = (2868, 1320)
IPAD_FRAME = (2752, 2064)
IPHONE_BAND = (261, 0, 2607, 1320)       # 16:9 picture inside the 19.5:9 frame
IPAD_BAND = (0, 258, 2752, 1806)         # 16:9 picture inside the 4:3 frame

# Marketing order: hook first, then play, then the systems that sell the game.
IPHONE_SHOTS = [
    ("01-title", "01-title"),
    ("02-gameplay", "05-play-early"),
    ("03-difficulty", "02-difficulty"),
    ("04-combat", "06-play-late"),
    ("05-how-to-survive", "03-howto"),
    ("06-hall-of-the-fallen", "04-scores"),
    ("07-the-night-claims-you", "07-gameover"),
]

IPAD_SHOTS = [
    ("01-title", "01-title"),
    ("02-gameplay", "02-play-4s"),
    ("03-exploring", "03-play-9s"),
    ("04-combat", "04-play-15s"),
    ("05-further-in", "05-play-22s"),
]


def load_rgb(path: Path) -> Image.Image:
    image = Image.open(path)
    if image.mode == "RGBA":
        canvas = Image.new("RGBA", image.size, INK + (255,))
        image = Image.alpha_composite(canvas, image)
    return image.convert("RGB")


def fit(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Cover-resize then centre-crop, so the aspect never distorts."""
    width, height = size
    scale = max(width / image.width, height / image.height)
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.LANCZOS,
    )
    left = (resized.width - width) // 2
    top = (resized.height - height) // 2
    return resized.crop((left, top, left + width, top + height))


def save_png(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=True)
    print(f"  {path.relative_to(ROOT)}  {image.width}x{image.height} {image.mode}")


def build_apple() -> None:
    print("Apple App Store")
    for name, source in IPHONE_SHOTS:
        raw = load_rgb(CAPTURES / "iphone" / f"{source}.png")
        assert raw.size == IPHONE_FRAME, f"{source}: expected {IPHONE_FRAME}, got {raw.size}"
        save_png(raw, STORES / "apple" / "iphone-6.9" / f"{name}.png")

    for name, source in IPAD_SHOTS:
        raw = load_rgb(CAPTURES / "ipad" / f"{source}.png")
        assert raw.size == IPAD_FRAME, f"{source}: expected {IPAD_FRAME}, got {raw.size}"
        save_png(raw, STORES / "apple" / "ipad-13" / f"{name}.png")

    icon = load_rgb(APP_ICON).resize((1024, 1024), Image.LANCZOS)
    save_png(icon, STORES / "apple" / "app-icon-1024.png")


def build_google() -> None:
    print("Google Play")
    for name, source in IPHONE_SHOTS[:6]:
        band = load_rgb(CAPTURES / "iphone" / f"{source}.png").crop(IPHONE_BAND)
        save_png(band.resize((1920, 1080), Image.LANCZOS), STORES / "google" / "phone" / f"{name}.png")

    for name, source in IPAD_SHOTS[:4]:
        band = load_rgb(CAPTURES / "ipad" / f"{source}.png").crop(IPAD_BAND)
        save_png(band.resize((2560, 1440), Image.LANCZOS), STORES / "google" / "tablet" / f"{name}.png")

    icon = Image.open(APP_ICON).convert("RGBA").resize((512, 512), Image.LANCZOS)
    save_png(icon, STORES / "google" / "icon-512.png")

    save_png(feature_graphic(), STORES / "google" / "feature-graphic-1024x500.png")


def feature_graphic() -> Image.Image:
    """1024x500 banner: wordmark on the left, faded playfield on the right."""
    width, height = 1024, 500
    canvas = Image.new("RGB", (width, height), INK)

    # Vertical gradient plus a lime glow behind the wordmark.
    top = (10, 7, 18)
    bottom = (26, 14, 33)
    for y in range(height):
        mix = y / (height - 1)
        row = tuple(round(top[i] + (bottom[i] - top[i]) * mix) for i in range(3))
        ImageDraw.Draw(canvas).line([(0, y), (width, y)], fill=row)

    glow = Image.new("RGB", (width, height), (0, 0, 0))
    ImageDraw.Draw(glow).ellipse((-160, -80, 620, 560), fill=(24, 46, 20))
    canvas = Image.blend(canvas, glow, 0.55)

    # Playfield strip on the right, melted into the background.
    play = fit(load_rgb(CAPTURES / "ipad" / "03-play-9s.png").crop(IPAD_BAND), (width, height))
    play = play.point(lambda v: min(255, round(v * 1.35 + 12)))
    mask = Image.new("L", (width, height), 0)
    ramp_start, ramp_end = 470, 660
    pixels = []
    for x in range(width):
        if x <= ramp_start:
            alpha = 0
        elif x >= ramp_end:
            alpha = 235
        else:
            alpha = round(235 * (x - ramp_start) / (ramp_end - ramp_start))
        pixels.append(alpha)
    mask.putdata([pixels[x] for _ in range(height) for x in range(width)])
    canvas.paste(play, (0, 0), mask)

    # Darken the far right so the banner survives small thumbnails.
    shade = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    shade_pixels = []
    for y in range(height):
        for x in range(width):
            alpha = 0 if x < 760 else round(70 * (x - 760) / (width - 760))
            shade_pixels.append((5, 3, 9, alpha))
    shade.putdata(shade_pixels)
    canvas = Image.alpha_composite(canvas.convert("RGBA"), shade).convert("RGB")

    display = ImageFont.truetype(str(FONTS / "cinzel.ttf"), 34)
    body = ImageFont.truetype(str(FONTS / "alegreya-bold.ttf"), 21)
    small = ImageFont.truetype(str(FONTS / "alegreya-bold.ttf"), 16)
    draw = ImageDraw.Draw(canvas)

    title = Image.open(TITLE_ART).convert("RGBA")
    title_scale = 214 / title.height
    title = title.resize((round(title.width * title_scale), 214), Image.LANCZOS)
    canvas.paste(title, (40, 54), title)

    draw.line([(48, 300), (268, 300)], fill=LIME, width=4)
    draw.text((48, 318), "AN ORIGINAL NIGHT-TERROR ADVENTURE", font=body, fill=MUTED)
    draw.text((48, 348), "FIVE DISTRICTS - THREE KEYS", font=small, fill=(146, 137, 162))
    draw.text((48, 368), "ONE HOLLOW KING", font=small, fill=(146, 137, 162))
    draw.text((48, 402), "OUT NOW ON iOS AND ANDROID", font=display, fill=LIME)
    draw.line([(48, 462), (1000, 462)], fill=(58, 44, 74), width=2)

    return canvas.filter(ImageFilter.SMOOTH)


def main() -> int:
    if not (CAPTURES / "iphone").is_dir() or not (CAPTURES / "ipad").is_dir():
        print(f"captures missing under {CAPTURES}", file=sys.stderr)
        return 1

    build_apple()
    build_google()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
