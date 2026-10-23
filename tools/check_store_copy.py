#!/usr/bin/env python3
"""Check the store listing copy against the limits both stores enforce,
and confirm every generated asset has the size, colour mode and count the
stores ask for.

Usage:
    python3 tools/check_store_copy.py
"""

from __future__ import annotations

import html.parser
import re
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
STORES = ROOT / "stores"

FAILURES: list[str] = []
CHECKS = 0


def check(condition: bool, message: str) -> None:
    global CHECKS
    CHECKS += 1
    if not condition:
        FAILURES.append(message)


def field(text: str, heading: str) -> str:
    """Return the first fenced block after a '## heading' line."""
    pattern = re.compile(
        r"^##\s+" + re.escape(heading) + r".*?\n```\n(.*?)\n```",
        re.DOTALL | re.MULTILINE,
    )
    match = pattern.search(text)
    if match is None:
        raise SystemExit(f"could not find the '{heading}' section")
    return match.group(1)


def check_copy() -> None:
    apple = (STORES / "apple" / "metadata.md").read_text()
    google = (STORES / "google" / "metadata.md").read_text()

    for heading, limit in [
        ("Name (limit 30)", 30),
        ("Subtitle (limit 30)", 30),
        ("Promotional text (limit 170)", 170),
        ("Keywords (limit 100, comma separated, no spaces)", 100),
        ("Description (limit 4000)", 4000),
        ("What's new (limit 4000)", 4000),
    ]:
        value = field(apple, heading)
        label = heading.split(" (")[0]
        check(len(value) <= limit, f"apple {label}: {len(value)}/{limit} characters")

    for heading, limit in [
        ("App name (limit 30)", 30),
        ("Short description (limit 80)", 80),
        ("Full description (limit 4000)", 4000),
    ]:
        value = field(google, heading)
        label = heading.split(" (")[0]
        check(len(value) <= limit, f"google {label}: {len(value)}/{limit} characters")

    keywords = field(apple, "Keywords (limit 100, comma separated, no spaces)")
    check(", " not in keywords, "apple keywords must not contain spaces after commas")
    check(all(part.strip() for part in keywords.split(",")), "apple keywords contain an empty term")

    check("PAUL F JOHNSON" in apple.upper(), "apple copyright must name Paul F Johnson")
    check("allthejohnsons" not in field(apple, "Description (limit 4000)").lower(), "description leaks the old copyright name")


def check_images() -> None:
    apple_dir = STORES / "apple"
    google_dir = STORES / "google"

    icon = Image.open(apple_dir / "app-icon-1024.png")
    check(icon.size == (1024, 1024), f"apple icon is {icon.size}")
    check(icon.mode == "RGB", f"apple icon mode is {icon.mode} (must be RGB, no alpha)")

    # Apple requires the "iPhone with Dynamic Island (medium display)" slot, which
    # accepts 2556x1179 and 2622x1206 in landscape. The 6.9" 2868x1320 set is now
    # optional and must not be shipped as the required one.
    check(not (apple_dir / "iphone-6.9").exists(), "obsolete iphone-6.9 set is still present")
    iphone = sorted((apple_dir / "iphone-6.3").glob("*.png"))
    check(3 <= len(iphone) <= 10, f"apple iphone count is {len(iphone)}")
    for path in iphone:
        image = Image.open(path)
        check(image.size in {(2622, 1206), (2556, 1179)}, f"{path.name}: {image.size} is not an accepted medium-display size")
        check(image.width / image.height > 2.0, f"{path.name}: not landscape")
        check(image.mode == "RGB", f"{path.name}: mode {image.mode} has alpha")

    ipad = sorted((apple_dir / "ipad-13").glob("*.png"))
    check(3 <= len(ipad) <= 10, f"apple ipad count is {len(ipad)}")
    for path in ipad:
        image = Image.open(path)
        check(image.size == (2752, 2064), f"{path.name}: {image.size} != 2752x2064")
        check(image.mode == "RGB", f"{path.name}: mode {image.mode} has alpha")

    play_icon = Image.open(google_dir / "icon-512.png")
    check(play_icon.size == (512, 512), f"play icon is {play_icon.size}")

    feature = google_dir / "feature-graphic-1024x500.png"
    graphic = Image.open(feature)
    check(graphic.size == (1024, 500), f"feature graphic is {graphic.size}")
    check(graphic.mode == "RGB", f"feature graphic mode is {graphic.mode}")
    check(feature.stat().st_size < 1_000_000, f"feature graphic is {feature.stat().st_size} bytes")

    phone = sorted((google_dir / "phone").glob("*.png"))
    check(len(phone) >= 2, f"play phone screenshot count is {len(phone)}")
    for path in phone:
        image = Image.open(path)
        check(image.size == (1920, 1080), f"{path.name}: {image.size} != 1920x1080")
        check(abs(image.width / image.height - 16 / 9) < 0.01, f"{path.name}: not 16:9")

    tablet = sorted((google_dir / "tablet").glob("*.png"))
    for path in tablet:
        image = Image.open(path)
        check(image.size == (2560, 1440), f"{path.name}: {image.size} != 2560x1440")


class _TagBalance(html.parser.HTMLParser):
    """Catch a page that renders wrong because a tag was left open."""

    VOID = {"area", "base", "br", "col", "embed", "hr", "img", "input",
            "link", "meta", "source", "track", "wbr"}

    def __init__(self) -> None:
        super().__init__()
        self.stack: list[str] = []
        self.errors: list[str] = []
        self.ids: set[str] = set()
        self.fragments: list[str] = []

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        if "id" in attributes:
            self.ids.add(attributes["id"])
        href = attributes.get("href", "")
        if href.startswith("#"):
            self.fragments.append(href[1:])
        if tag not in self.VOID:
            self.stack.append(tag)

    def handle_endtag(self, tag):
        if not self.stack or self.stack[-1] != tag:
            self.errors.append(f"unbalanced </{tag}>")
        else:
            self.stack.pop()


def check_pages() -> None:
    """The support and privacy pages are what the stores link to."""
    for name in ["support.html", "privacy.html"]:
        path = STORES / name
        check(path.is_file(), f"stores/{name} is missing")
        if not path.is_file():
            continue
        text = path.read_text()
        parser = _TagBalance()
        parser.feed(text)
        parser.close()
        check(text.lstrip().lower().startswith("<!doctype html>"), f"{name} has no doctype")
        check(not parser.stack, f"{name} leaves <{parser.stack[-1] if parser.stack else ''}> open")
        check(not parser.errors, f"{name} has unbalanced tags: {parser.errors[:2]}")
        missing = [f for f in parser.fragments if f not in parser.ids]
        check(not missing, f"{name} links to missing anchors: {missing}")
        check(not re.search(r'(?:src|href)="https?://', text), f"{name} loads something from the network")
        check("__ICON" not in text, f"{name} still contains a placeholder")
        check("Paul F Johnson" in text, f"{name} does not carry the copyright line")
        check(len(text) > 5000, f"{name} looks empty")

    support = (STORES / "support.html").read_text()
    privacy = (STORES / "privacy.html").read_text()
    check('href="privacy.html"' in support, "support.html does not link the privacy policy")
    check('href="support.html"' in privacy, "privacy.html does not link support")
    check("collects no data" in support or "collects nothing" in support,
          "support.html does not state the privacy answer")


def check_submission_text() -> None:
    """The console answer sheets must exist and stay free of placeholders."""
    apple = STORES / "apple" / "app-store-connect.md"
    google = STORES / "google" / "play-console.md"
    for path in [apple, google]:
        check(path.is_file(), f"{path.relative_to(ROOT)} is missing")
        if not path.is_file():
            continue
        text = path.read_text()
        check("TODO" not in text, f"{path.name} still has a TODO")
        check("<your" not in text, f"{path.name} still has a placeholder name")
        check("Data" in text, f"{path.name} does not answer the data questions")

    # A stale TODO in any listing document means a field has no answer yet.
    for path in sorted(STORES.rglob("*.md")):
        check("TODO" not in path.read_text(), f"{path.relative_to(STORES)} still has a TODO")

    apple_text = apple.read_text() if apple.is_file() else ""
    google_text = google.read_text() if google.is_file() else ""
    for label, text in [("App Store Connect", apple_text), ("Play Console", google_text)]:
        check("support.html" in text or "privacy.html" in text,
              f"{label} answers do not name the hosted pages")
        check("privacy.html" in text, f"{label} answers have no privacy policy URL")


def check_movie() -> None:
    movie = ROOT / "Movies" / "Mutants-iPad-gameplay.mp4"
    check(movie.is_file(), "Movies/Mutants-iPad-gameplay.mp4 is missing")
    if movie.is_file():
        check(movie.stat().st_size > 1_000_000, "gameplay video looks empty")
        check((ROOT / "Movies" / "Mutants-iPad-gameplay-poster.png").is_file(), "poster frame is missing")


def main() -> int:
    check_copy()
    check_images()
    check_pages()
    check_submission_text()
    check_movie()

    for failure in FAILURES:
        print(f"FAIL  {failure}")
    print(f"{CHECKS - len(FAILURES)}/{CHECKS} checks passed")
    return 1 if FAILURES else 0


if __name__ == "__main__":
    sys.exit(main())
