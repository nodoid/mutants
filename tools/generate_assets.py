#!/usr/bin/env python3
"""Generate the game's brand artwork and bitmap-font atlases.

All artwork is original and produced procedurally. The bundled fonts are
licensed under the SIL Open Font License (see tools/fonts/*.txt).
"""
from __future__ import annotations

import json
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[1]
FONT_DIR = ROOT / "tools" / "fonts"
GAME_CONTENT = ROOT / "src" / "Mutants.Game" / "Content"
ANDROID_RES = ROOT / "src" / "Mutants.Android" / "Resources"
IOS_ASSETS = ROOT / "src" / "Mutants.iOS" / "Assets.xcassets"

INK = (11, 7, 20)
NIGHT = (24, 16, 44)
BONE = (240, 231, 204)
LIME = (182, 242, 107)
TOXIC = (118, 205, 72)
EMBER = (255, 158, 61)
BLOOD = (210, 65, 80)
VIOLET = (122, 92, 255)
PALE_MOON = (241, 232, 201)


def hex_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4))


def ensure(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def font(path: Path, size: int, weight: int | None = None) -> ImageFont.FreeTypeFont:
    face = ImageFont.truetype(str(path), size)
    if weight is not None:
        try:
            face.set_variation_by_axes([weight])
        except Exception:
            pass
    return face


def build_font_atlas(
    ttf: Path,
    out_png: Path,
    out_json: Path,
    name: str,
    size: int,
    weight: int | None = None,
    atlas_width: int = 1024,
) -> None:
    face = font(ttf, size, weight)
    ascent, descent = face.getmetrics()
    line_height = ascent + descent
    pad = 3
    chars = [chr(c) for c in range(32, 127)]

    glyphs: dict[str, dict[str, int]] = {}
    placements = []

    for ch in chars:
        if ch == " ":
            glyphs[ch] = {
                "x": 0, "y": 0, "w": 0, "h": 0,
                "ox": 0, "oy": 0, "adv": int(round(face.getlength(ch))),
            }
            continue

        bbox = face.getbbox(ch)
        x0, y0, x1, y1 = bbox
        w = max(1, x1 - x0)
        h = max(1, y1 - y0)
        placements.append((ch, x0, y0, w, h, int(round(face.getlength(ch)))))

    placements.sort(key=lambda item: (-item[4], item[1]))
    cursor_x = pad
    cursor_y = pad
    row_height = 0
    coords = {}

    for ch, x0, y0, w, h, adv in placements:
        if cursor_x + w + pad > atlas_width:
            cursor_x = pad
            cursor_y += row_height + pad
            row_height = 0

        coords[ch] = (cursor_x, cursor_y, w, h, x0, y0, adv)
        cursor_x += w + pad
        row_height = max(row_height, h)

    atlas_height = 1
    while atlas_height < cursor_y + row_height + pad:
        atlas_height *= 2

    atlas = Image.new("RGBA", (atlas_width, atlas_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(atlas)

    for ch, (px, py, w, h, x0, y0, adv) in coords.items():
        mask = Image.new("L", (w, h), 0)
        mask_draw = ImageDraw.Draw(mask)
        mask_draw.text((-x0, -y0), ch, font=face, fill=255)
        patch = Image.new("RGBA", (w, h), (255, 255, 255, 0))
        patch.putalpha(mask)
        atlas.alpha_composite(patch, (px, py))

        glyphs[ch] = {
            "x": px,
            "y": py,
            "w": w,
            "h": h,
            "ox": x0,
            "oy": y0,
            "adv": adv,
        }

    ensure(out_png.parent)
    atlas.save(out_png)

    payload = {
        "name": name,
        "size": size,
        "lineHeight": line_height,
        "ascent": ascent,
        "descent": descent,
        "atlasWidth": atlas_width,
        "atlasHeight": atlas_height,
        "glyphs": glyphs,
    }
    out_json.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"font atlas: {out_png.relative_to(ROOT)} ({atlas_width}x{atlas_height})")


def draw_emblem(size: int, with_hills: bool = False) -> Image.Image:
    scale = 3
    w = size * scale
    im = Image.new("RGBA", (w, w), (0, 0, 0, 0))
    d = ImageDraw.Draw(im, "RGBA")

    # Moon behind the figure.
    moon_cx, moon_cy, moon_r = int(w * 0.63), int(w * 0.31), int(w * 0.29)
    d.ellipse(
        (moon_cx - moon_r, moon_cy - moon_r, moon_cx + moon_r, moon_cy + moon_r),
        fill=PALE_MOON + (255,),
    )
    for cx, cy, r, alpha in (
        (0.54, 0.20, 0.045, 26),
        (0.71, 0.36, 0.060, 20),
        (0.60, 0.43, 0.032, 24),
        (0.76, 0.20, 0.026, 30),
    ):
        d.ellipse(
            (
                int(w * cx - w * r), int(w * cy - w * r),
                int(w * cx + w * r), int(w * cy + w * r),
            ),
            fill=(190, 179, 148, alpha),
        )

    # Distant hills.
    if with_hills:
        d.polygon(
            [
                (0, int(w * 0.83)), (int(w * 0.18), int(w * 0.72)),
                (int(w * 0.38), int(w * 0.84)), (int(w * 0.57), int(w * 0.70)),
                (int(w * 0.78), int(w * 0.82)), (w, int(w * 0.73)),
                (w, w), (0, w),
            ],
            fill=(16, 10, 28, 255),
        )

    # Gravestones.
    stone = (35, 25, 52, 255)
    stone_light = (58, 44, 82, 255)
    for cx, base, sw, sh in (
        (0.18, 0.94, 0.10, 0.20),
        (0.31, 0.95, 0.07, 0.13),
        (0.83, 0.95, 0.11, 0.18),
    ):
        left = int(w * (cx - sw / 2))
        right = int(w * (cx + sw / 2))
        top = int(w * (base - sh))
        bottom = int(w * base)
        d.rounded_rectangle((left, top, right, bottom), radius=int(w * 0.035), fill=stone)
        d.rectangle((left, top + int(w * 0.02), left + int(w * 0.012), bottom), fill=stone_light)
        d.line((left + int(w * 0.02), top + int(w * 0.035), right - int(w * 0.02), top + int(w * 0.035)),
               fill=(20, 14, 32, 255), width=int(w * 0.006))

    # Cloaked figure.
    cloak = (21, 15, 36, 255)
    cloak_edge = (40, 28, 64, 255)
    cx = int(w * 0.50)
    d.polygon(
        [
            (int(w * 0.24), int(w * 1.02)),
            (int(w * 0.33), int(w * 0.62)),
            (int(w * 0.40), int(w * 0.47)),
            (int(w * 0.60), int(w * 0.47)),
            (int(w * 0.68), int(w * 0.63)),
            (int(w * 0.78), int(w * 1.02)),
        ],
        fill=cloak,
    )
    d.line(
        [(int(w * 0.34), int(w * 0.60)), (int(w * 0.42), int(w * 0.48))],
        fill=cloak_edge, width=int(w * 0.012),
    )
    d.line(
        [(int(w * 0.66), int(w * 0.60)), (int(w * 0.58), int(w * 0.48))],
        fill=cloak_edge, width=int(w * 0.012),
    )

    # Hood and face void.
    d.ellipse(
        (int(w * 0.36), int(w * 0.33), int(w * 0.64), int(w * 0.60)),
        fill=(30, 21, 48, 255),
    )
    d.ellipse(
        (int(w * 0.40), int(w * 0.40), int(w * 0.60), int(w * 0.57)),
        fill=(4, 3, 9, 255),
    )

    # Eyes with layered glow.
    for x, y, r in ((0.465, 0.485, 0.020), (0.545, 0.485, 0.020)):
        for grow, alpha in ((3.8, 26), (2.6, 48), (1.7, 80), (1.0, 255)):
            rr = w * r * grow
            d.ellipse(
                (int(w * x - rr), int(w * y - rr), int(w * x + rr), int(w * y + rr)),
                fill=LIME + (alpha,),
            )
        d.ellipse(
            (int(w * (x - 0.006)), int(w * (y - 0.007)),
             int(w * (x + 0.006)), int(w * (y + 0.004))),
            fill=(245, 255, 226, 255),
        )

    # Scarf.
    d.line(
        [(int(w * 0.40), int(w * 0.585)), (int(w * 0.60), int(w * 0.585))],
        fill=LIME + (255,), width=int(w * 0.020),
    )

    # Lantern.
    lx, ly = int(w * 0.745), int(w * 0.69)
    d.line(
        [(int(w * 0.70), int(w * 0.665)), (lx, ly - int(w * 0.055))],
        fill=(63, 47, 88, 255), width=int(w * 0.010),
    )
    d.rounded_rectangle(
        (lx - int(w * 0.042), ly - int(w * 0.055), lx + int(w * 0.042), ly + int(w * 0.055)),
        radius=int(w * 0.018),
        fill=(50, 36, 66, 255),
        outline=(96, 72, 118, 255),
        width=int(w * 0.007),
    )
    d.rounded_rectangle(
        (lx - int(w * 0.026), ly - int(w * 0.036), lx + int(w * 0.026), ly + int(w * 0.032)),
        radius=int(w * 0.012),
        fill=(255, 196, 102, 255),
    )
    d.ellipse(
        (lx - int(w * 0.022), ly - int(w * 0.022), lx + int(w * 0.022), ly + int(w * 0.022)),
        fill=(255, 244, 196, 255),
    )
    for grow, alpha in ((4.2, 24), (2.6, 48), (1.6, 70)):
        rr = w * 0.055 * grow
        d.ellipse((lx - rr, ly - rr, lx + rr, ly + rr), fill=EMBER + (alpha,))

    return im.resize((size, size), Image.LANCZOS)


def background(size: int, inner: tuple[int, int, int], outer: tuple[int, int, int]) -> Image.Image:
    gradient = Image.radial_gradient("L").resize((size, size))
    return ImageOps.colorize(gradient, black=outer, white=inner).convert("RGBA")


def build_icon(size: int = 1024) -> Image.Image:
    big = background(size * 2, (41, 24, 74), (7, 4, 13))
    emblem = draw_emblem(size, with_hills=True)
    big.alpha_composite(emblem.resize((size * 2, size * 2), Image.LANCZOS))
    d = ImageDraw.Draw(big, "RGBA")

    # Stars.
    rng = random.Random(7800)
    for _ in range(90):
        x = rng.randrange(int(size * 0.05), int(size * 0.95))
        y = rng.randrange(int(size * 0.05), int(size * 0.65))
        r = rng.choice((1, 1, 2, 2, 3))
        d.ellipse((x - r, y - r, x + r, y + r), fill=BONE + (rng.randrange(40, 150),))

    # Framing line and vignette.
    d.rounded_rectangle(
        (int(size * 0.055), int(size * 0.055), int(size * 1.945), int(size * 1.945)),
        radius=int(size * 0.15),
        outline=(143, 117, 180, 80),
        width=int(size * 0.012),
    )
    vignette = Image.new("L", (size * 2, size * 2), 0)
    ImageDraw.Draw(vignette).ellipse(
        (-int(size * 0.35), -int(size * 0.35), int(size * 2.35), int(size * 2.35)), fill=140
    )
    vignette = vignette.filter(ImageFilter.GaussianBlur(size // 8))
    black = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    black.putalpha(ImageOps.invert(vignette).point(lambda v: int(v * 0.35)))
    big.alpha_composite(black)

    return big.resize((size, size), Image.LANCZOS).convert("RGB")


def build_wordmark(width: int = 1400, height: int = 460) -> Image.Image:
    im = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    display = font(FONT_DIR / "cinzel.ttf", 220, weight=900)
    tagline_face = font(FONT_DIR / "alegreya-bold.ttf", 50)

    title = "MUTANTS"
    tracking = 14
    widths = [display.getlength(ch) for ch in title]
    total = sum(widths) + tracking * (len(title) - 1)
    x = (width - total) / 2
    baseline_y = 245

    # Glow layers.
    glow = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gx = x
    for ch, cw in zip(title, widths):
        gd.text((gx, baseline_y), ch, font=display, fill=VIOLET + (180,), anchor="ls",
                stroke_width=6, stroke_fill=VIOLET + (160,))
        gx += cw + tracking
    glow = glow.filter(ImageFilter.GaussianBlur(22))
    im.alpha_composite(glow)

    # Gradient fill clipped to glyph mask.
    mask = Image.new("L", (width, height), 0)
    md = ImageDraw.Draw(mask)
    gx = x
    for ch, cw in zip(title, widths):
        md.text((gx, baseline_y), ch, font=display, fill=255, anchor="ls")
        gx += cw + tracking

    gradient = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    for y in range(40, 300):
        t = max(0.0, min(1.0, (y - 60) / 220))
        col = (
            int(246 - 26 * t),
            int(236 - 60 * t),
            int(204 - 86 * t),
            255,
        )
        ImageDraw.Draw(gradient).line((0, y, width, y), fill=col)
    im.paste(gradient, (0, 0), mask)

    # Outline and shadow around the lettering.
    outline = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    od = ImageDraw.Draw(outline)
    gx = x
    for ch, cw in zip(title, widths):
        od.text((gx, baseline_y + 7), ch, font=display, fill=(3, 2, 8, 220), anchor="ls",
                stroke_width=7, stroke_fill=(3, 2, 8, 220))
        gx += cw + tracking
    im.alpha_composite(outline)
    im.paste(gradient, (0, 0), mask)
    edge = ImageDraw.Draw(im)
    gx = x
    for ch, cw in zip(title, widths):
        edge.text((gx, baseline_y), ch, font=display, fill=None, anchor="ls",
                  stroke_width=2, stroke_fill=(19, 12, 32, 200))
        gx += cw + tracking

    # Tagline.
    tag = "THE HOLLOW HOUR"
    tag_tracking = 12
    tag_widths = [tagline_face.getlength(ch) for ch in tag]
    tag_total = sum(tag_widths) + tag_tracking * (len(tag) - 1)
    tx = (width - tag_total) / 2
    ty = 330
    td = ImageDraw.Draw(im)
    for ch, cw in zip(tag, tag_widths):
        td.text((tx, ty), ch, font=tagline_face, fill=LIME + (255,), anchor="ls")
        tx += cw + tag_tracking

    # Divider.
    td.line((width * 0.5 - 240, 355, width * 0.5 - 30, 355), fill=BONE + (80,), width=2)
    td.line((width * 0.5 + 30, 355, width * 0.5 + 240, 355), fill=BONE + (80,), width=2)
    for dx in (-18, 0, 18):
        td.regular_polygon((int(width * 0.5 + dx), 355, 4), n_sides=4, rotation=45, fill=LIME + (200,))
    return im


def build_launch_art(size: int = 900) -> Image.Image:
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    emblem = draw_emblem(int(size * 0.72), with_hills=True)
    im.alpha_composite(emblem, (int(size * 0.14), int(size * 0.02)))
    face = font(FONT_DIR / "cinzel.ttf", int(size * 0.085), weight=900)
    text = "MUTANTS"
    w = face.getlength(text)
    d = ImageDraw.Draw(im)
    d.text(((size - w) / 2, int(size * 0.86)), text, font=face, fill=BONE + (255,), anchor="ls")
    return im


def write_ios_catalog() -> None:
    ensure(IOS_ASSETS)
    (IOS_ASSETS / "Contents.json").write_text(json.dumps({"info": {"author": "xcode", "version": 1}}, indent=2))
    icon_dir = IOS_ASSETS / "AppIcon.appiconset"
    ensure(icon_dir)
    contents = {
        "images": [
            {
                "filename": "AppIcon-1024.png",
                "idiom": "universal",
                "platform": "ios",
                "size": "1024x1024",
            }
        ],
        "info": {"author": "xcode", "version": 1},
    }
    (icon_dir / "Contents.json").write_text(json.dumps(contents, indent=2))

    launch_dir = IOS_ASSETS / "LaunchImage.imageset"
    ensure(launch_dir)
    contents = {
        "images": [
            {"filename": "LaunchImage.png", "idiom": "universal", "scale": "1x"},
            {"filename": "LaunchImage@2x.png", "idiom": "universal", "scale": "2x"},
            {"filename": "LaunchImage@3x.png", "idiom": "universal", "scale": "3x"},
        ],
        "info": {"author": "xcode", "version": 1},
    }
    (launch_dir / "Contents.json").write_text(json.dumps(contents, indent=2))

    colors_dir = IOS_ASSETS / "LaunchBackground.colorset"
    ensure(colors_dir)
    colors = {
        "colors": [
            {
                "color": {
                    "color-space": "srgb",
                    "components": {"alpha": "1.000", "blue": "0.078", "green": "0.027", "red": "0.043"},
                },
                "idiom": "universal",
            }
        ],
        "info": {"author": "xcode", "version": 1},
    }
    (colors_dir / "Contents.json").write_text(json.dumps(colors, indent=2))


def main() -> None:
    ensure(GAME_CONTENT / "Fonts")
    ensure(GAME_CONTENT / "Art")
    ensure(ANDROID_RES)
    write_ios_catalog()

    build_font_atlas(
        FONT_DIR / "cinzel.ttf",
        GAME_CONTENT / "Fonts" / "display.png",
        GAME_CONTENT / "Fonts" / "display.json",
        "Cinzel Black",
        96,
        weight=900,
    )
    build_font_atlas(
        FONT_DIR / "alegreya-bold.ttf",
        GAME_CONTENT / "Fonts" / "body.png",
        GAME_CONTENT / "Fonts" / "body.json",
        "Alegreya Sans SC Bold",
        76,
    )

    wordmark = build_wordmark()
    wordmark.save(GAME_CONTENT / "Art" / "title.png")

    icon = build_icon(1024)
    ensure(ROOT / "art")
    icon.save(ROOT / "art" / "icon-1024.png")
    icon.save(IOS_ASSETS / "AppIcon.appiconset" / "AppIcon-1024.png")

    launch = build_launch_art(900)
    launch.resize((300, 300), Image.LANCZOS).save(IOS_ASSETS / "LaunchImage.imageset" / "LaunchImage.png")
    launch.resize((600, 600), Image.LANCZOS).save(IOS_ASSETS / "LaunchImage.imageset" / "LaunchImage@2x.png")
    launch.resize((900, 900), Image.LANCZOS).save(IOS_ASSETS / "LaunchImage.imageset" / "LaunchImage@3x.png")

    # Legacy Android launcher icons.
    densities = {"mdpi": 48, "hdpi": 72, "xhdpi": 96, "xxhdpi": 144, "xxxhdpi": 192}
    for name, px in densities.items():
        folder = ANDROID_RES / f"mipmap-{name}"
        ensure(folder)
        icon.resize((px, px), Image.LANCZOS).save(folder / "ic_launcher.png")

        round_icon = Image.new("RGBA", (px * 4, px * 4), (0, 0, 0, 0))
        mask = Image.new("L", (px * 4, px * 4), 0)
        ImageDraw.Draw(mask).ellipse((0, 0, px * 4 - 1, px * 4 - 1), fill=255)
        round_icon.paste(icon.resize((px * 4, px * 4), Image.LANCZOS), (0, 0))
        round_icon.putalpha(mask)
        round_icon.resize((px, px), Image.LANCZOS).save(folder / "ic_launcher_round.png")

    # Adaptive icon foreground (safe-zone artwork on transparency).
    emblem = draw_emblem(1024, with_hills=True)
    for name, px in {"mdpi": 108, "hdpi": 162, "xhdpi": 216, "xxhdpi": 324, "xxxhdpi": 432}.items():
        folder = ANDROID_RES / f"mipmap-{name}"
        ensure(folder)
        fg = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
        art = emblem.resize((int(1024 * 0.66), int(1024 * 0.66)), Image.LANCZOS)
        fg.alpha_composite(art, ((1024 - art.width) // 2, (1024 - art.height) // 2))
        fg.resize((px, px), Image.LANCZOS).save(folder / "ic_launcher_foreground.png")

    # Android 12 splash icon and old-API splash tile.
    splash_icon = draw_emblem(512, with_hills=False)
    ensure(ANDROID_RES / "drawable-nodpi")
    splash_icon.save(ANDROID_RES / "drawable-nodpi" / "splash_emblem.png")
    brand = build_wordmark(800, 264)
    brand.save(ANDROID_RES / "drawable-nodpi" / "splash_brand.png")

    print("assets generated.")


if __name__ == "__main__":
    main()
