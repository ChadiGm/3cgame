#!/usr/bin/env python3
"""Generate stylized 2D lava tile textures with looping animation."""

from __future__ import annotations

import argparse
import datetime as dt
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

TAU = float(np.pi * 2.0)


@dataclass(frozen=True)
class WaveTerm:
    fx: int
    fy: int
    phase: float
    speed: float
    amplitude: float
    cosine: bool


@dataclass(frozen=True)
class Bubble:
    x: float
    y: float
    radius: float
    phase: float
    speed: float
    strength: float


def smoothstep(edge0: np.ndarray | float, edge1: np.ndarray | float, x: np.ndarray) -> np.ndarray:
    span = np.maximum(np.asarray(edge1) - np.asarray(edge0), 1e-6)
    t = np.clip((x - edge0) / span, 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def build_wave_terms(
    rng: np.random.Generator,
    count: int,
    freq_low: int,
    freq_high: int,
    speed_low: float,
    speed_high: float,
    amp_low: float,
    amp_high: float,
) -> list[WaveTerm]:
    terms: list[WaveTerm] = []
    for _ in range(count):
        fx = int(rng.integers(freq_low, freq_high + 1))
        fy = int(rng.integers(freq_low, freq_high + 1))
        phase = float(rng.uniform(0.0, TAU))
        speed = float(rng.uniform(speed_low, speed_high))
        amplitude = float(rng.uniform(amp_low, amp_high))
        cosine = bool(rng.integers(0, 2))
        terms.append(WaveTerm(fx=fx, fy=fy, phase=phase, speed=speed, amplitude=amplitude, cosine=cosine))
    return terms


def build_bubbles(rng: np.random.Generator, count: int) -> list[Bubble]:
    bubbles: list[Bubble] = []
    for _ in range(count):
        bubbles.append(
            Bubble(
                x=float(rng.uniform(0.0, 1.0)),
                y=float(rng.uniform(0.0, 1.0)),
                radius=float(rng.uniform(0.028, 0.085)),
                phase=float(rng.uniform(0.0, TAU)),
                speed=float(rng.uniform(0.18, 0.55)),
                strength=float(rng.uniform(0.40, 1.00)),
            )
        )
    return bubbles


class LavaAssetGenerator:
    def __init__(self, size: int, frame_count: int, seed: int):
        self.size = size
        self.frame_count = frame_count
        rng = np.random.default_rng(seed)

        lin = np.linspace(0.0, 1.0, size, endpoint=False, dtype=np.float32)
        self.x, self.y = np.meshgrid(lin, lin)

        self.warp_x_terms = build_wave_terms(rng, count=8, freq_low=1, freq_high=4, speed_low=-0.14, speed_high=0.14, amp_low=0.45, amp_high=1.00)
        self.warp_y_terms = build_wave_terms(rng, count=8, freq_low=1, freq_high=4, speed_low=-0.14, speed_high=0.14, amp_low=0.45, amp_high=1.00)
        self.flow_terms = build_wave_terms(rng, count=14, freq_low=1, freq_high=8, speed_low=-0.20, speed_high=0.20, amp_low=0.35, amp_high=1.00)
        self.detail_terms = build_wave_terms(rng, count=16, freq_low=2, freq_high=12, speed_low=-0.24, speed_high=0.24, amp_low=0.20, amp_high=0.70)
        self.bubbles = build_bubbles(rng, count=20)

        self.gradient_stops = np.array([0.00, 0.24, 0.50, 0.74, 0.90, 1.00], dtype=np.float32)
        self.gradient_colors = np.array(
            [
                [34.0, 4.0, 2.0],
                [122.0, 14.0, 6.0],
                [208.0, 32.0, 11.0],
                [255.0, 106.0, 16.0],
                [255.0, 175.0, 34.0],
                [255.0, 232.0, 102.0],
            ],
            dtype=np.float32,
        )

    def _wave_sum(self, u: np.ndarray, v: np.ndarray, t: float, terms: list[WaveTerm]) -> np.ndarray:
        acc = np.zeros_like(u, dtype=np.float32)
        for term in terms:
            theta = TAU * (term.fx * u + term.fy * v + term.speed * t) + term.phase
            signal = np.cos(theta) if term.cosine else np.sin(theta)
            acc += term.amplitude * signal.astype(np.float32)
        return acc

    @staticmethod
    def _gradient(values: np.ndarray, stops: np.ndarray, colors: np.ndarray) -> np.ndarray:
        flat = values.ravel()
        out = np.empty((flat.size, 3), dtype=np.float32)
        for channel in range(3):
            out[:, channel] = np.interp(flat, stops, colors[:, channel])
        return out.reshape(values.shape + (3,))

    def _toroidal_distance(self, bx: float, by: float) -> np.ndarray:
        dx = np.abs(self.x - bx)
        dy = np.abs(self.y - by)
        dx = np.minimum(dx, 1.0 - dx)
        dy = np.minimum(dy, 1.0 - dy)
        return np.sqrt(dx * dx + dy * dy)

    def _base_lava(self, t: float) -> tuple[np.ndarray, np.ndarray]:
        warp_x = self._wave_sum(self.x, self.y, t, self.warp_x_terms)
        warp_y = self._wave_sum(self.x, self.y, t, self.warp_y_terms)

        u = (self.x + 0.046 * warp_x + 0.020 * np.sin(TAU * (1.6 * self.y + 0.13 * t))) % 1.0
        v = (self.y + 0.046 * warp_y + 0.020 * np.cos(TAU * (1.6 * self.x - 0.10 * t))) % 1.0

        macro = self._wave_sum(u, v, t, self.flow_terms)
        detail = self._wave_sum((u * 2.2) % 1.0, (v * 2.2) % 1.0, t * 1.08, self.detail_terms)
        flow = 0.74 * macro + 0.26 * detail
        flow = (flow - flow.min()) / (np.ptp(flow) + 1e-6)

        bubble_hot = np.zeros_like(flow)
        bubble_dark = np.zeros_like(flow)
        for bubble in self.bubbles:
            bx = (bubble.x + 0.018 * np.sin(TAU * (bubble.speed * t) + bubble.phase)) % 1.0
            by = (bubble.y + 0.018 * np.cos(TAU * (bubble.speed * t) + bubble.phase * 0.9)) % 1.0
            dist = self._toroidal_distance(bx, by)

            pulse = 0.5 + 0.5 * np.sin(TAU * (bubble.speed * t) + bubble.phase)
            rim_sigma = bubble.radius * 0.18 + 1e-6
            core_sigma = bubble.radius * 0.56 + 1e-6

            rim = np.exp(-((dist - bubble.radius) ** 2) / (2.0 * rim_sigma * rim_sigma))
            core = np.exp(-(dist * dist) / (2.0 * core_sigma * core_sigma))

            bubble_hot += bubble.strength * pulse * rim
            bubble_dark += bubble.strength * (1.0 - 0.65 * pulse) * core

        bubble_hot = np.clip(bubble_hot, 0.0, 1.2)
        bubble_dark = np.clip(bubble_dark, 0.0, 1.0)
        veins = np.clip((flow - 0.56) * 2.6, 0.0, 1.0)

        heat = np.clip(0.52 * flow + 0.36 * veins + 0.35 * bubble_hot - 0.20 * bubble_dark, 0.0, 1.0)
        striation = 0.5 + 0.5 * np.sin(TAU * (6.2 * u - 3.1 * v + 0.18 * t) + detail * 2.4)
        emissive = np.clip(np.power(np.clip(heat - 0.30, 0.0, 1.0), 1.45) + 0.55 * bubble_hot, 0.0, 1.0)

        rgb = self._gradient(heat, self.gradient_stops, self.gradient_colors)
        rgb += veins[..., None] * np.array([80.0, 24.0, 8.0], dtype=np.float32)
        rgb += bubble_hot[..., None] * np.array([118.0, 68.0, 20.0], dtype=np.float32)
        rgb -= bubble_dark[..., None] * np.array([38.0, 15.0, 6.0], dtype=np.float32)
        rgb += (striation[..., None] - 0.5) * np.array([30.0, 8.0, 0.0], dtype=np.float32)
        rgb = np.clip(rgb, 0.0, 255.0)

        return rgb, emissive

    def _boundary_wave(self, coord: np.ndarray, t: float, phase_seed: float) -> np.ndarray:
        return (
            0.030 * np.sin(TAU * (3.0 * coord + 0.12 * t) + phase_seed)
            + 0.020 * np.sin(TAU * (7.0 * coord - 0.08 * t) + phase_seed * 0.7)
            + 0.010 * np.cos(TAU * (11.0 * coord + 0.05 * t) + phase_seed * 1.3)
        )

    def _mask_edge_top(self, t: float) -> np.ndarray:
        boundary = 0.34 + self._boundary_wave(self.x, t, 0.8)
        return smoothstep(boundary - 0.018, boundary + 0.018, self.y)

    def _mask_edge_bottom(self, t: float) -> np.ndarray:
        boundary = 0.66 + self._boundary_wave(self.x, t, 1.6)
        return 1.0 - smoothstep(boundary - 0.018, boundary + 0.018, self.y)

    def _mask_edge_left(self, t: float) -> np.ndarray:
        boundary = 0.34 + self._boundary_wave(self.y, t, 2.4)
        return smoothstep(boundary - 0.018, boundary + 0.018, self.x)

    def _mask_edge_right(self, t: float) -> np.ndarray:
        boundary = 0.66 + self._boundary_wave(self.y, t, 3.2)
        return 1.0 - smoothstep(boundary - 0.018, boundary + 0.018, self.x)

    def _mask_corner(self, t: float, flip_x: bool, flip_y: bool, phase_seed: float) -> np.ndarray:
        x = (1.0 - self.x) if flip_x else self.x
        y = (1.0 - self.y) if flip_y else self.y

        top_line = 0.34 + self._boundary_wave(x, t, phase_seed)
        left_line = 0.34 + self._boundary_wave(y, t, phase_seed + 0.9)

        top_mask = smoothstep(top_line - 0.018, top_line + 0.018, y)
        left_mask = smoothstep(left_line - 0.018, left_line + 0.018, x)

        radius = 0.49 + 0.030 * np.sin(TAU * (2.2 * (x + y) + 0.10 * t) + phase_seed * 1.1)
        distance = np.sqrt(x * x + y * y)
        round_mask = smoothstep(radius - 0.028, radius + 0.028, distance)
        return np.minimum(np.minimum(top_mask, left_mask), round_mask)

    def _variant_mask(self, name: str, t: float) -> np.ndarray:
        if name == "center":
            return np.ones((self.size, self.size), dtype=np.float32)
        if name == "edge_top":
            return self._mask_edge_top(t)
        if name == "edge_bottom":
            return self._mask_edge_bottom(t)
        if name == "edge_left":
            return self._mask_edge_left(t)
        if name == "edge_right":
            return self._mask_edge_right(t)
        if name == "corner_top_left":
            return self._mask_corner(t, flip_x=False, flip_y=False, phase_seed=4.0)
        if name == "corner_top_right":
            return self._mask_corner(t, flip_x=True, flip_y=False, phase_seed=4.7)
        if name == "corner_bottom_left":
            return self._mask_corner(t, flip_x=False, flip_y=True, phase_seed=5.4)
        if name == "corner_bottom_right":
            return self._mask_corner(t, flip_x=True, flip_y=True, phase_seed=6.1)
        raise ValueError(f"Unknown variant: {name}")

    @staticmethod
    def _enforce_tile_wrap(rgba: np.ndarray) -> np.ndarray:
        left_right = ((rgba[:, 0, :].astype(np.uint16) + rgba[:, -1, :].astype(np.uint16)) // 2).astype(np.uint8)
        rgba[:, 0, :] = left_right
        rgba[:, -1, :] = left_right

        top_bottom = ((rgba[0, :, :].astype(np.uint16) + rgba[-1, :, :].astype(np.uint16)) // 2).astype(np.uint8)
        rgba[0, :, :] = top_bottom
        rgba[-1, :, :] = top_bottom

        corner = (
            (
                rgba[0, 0, :].astype(np.uint16)
                + rgba[0, -1, :].astype(np.uint16)
                + rgba[-1, 0, :].astype(np.uint16)
                + rgba[-1, -1, :].astype(np.uint16)
            )
            // 4
        ).astype(np.uint8)
        rgba[0, 0, :] = corner
        rgba[0, -1, :] = corner
        rgba[-1, 0, :] = corner
        rgba[-1, -1, :] = corner
        return rgba

    def _compose_rgba(self, rgb: np.ndarray, emissive: np.ndarray, mask: np.ndarray) -> np.ndarray:
        mask = np.clip(mask.astype(np.float32), 0.0, 1.0)

        blur_radius = max(6, int(self.size * 0.028))
        mask_image = Image.fromarray((mask * 255.0).astype(np.uint8))
        blurred = mask_image.filter(ImageFilter.GaussianBlur(radius=blur_radius))
        glow = np.array(blurred, dtype=np.float32) / 255.0

        halo = np.clip(glow - mask, 0.0, 1.0)
        alpha = np.clip(mask + 0.72 * halo, 0.0, 1.0)
        visibility = np.clip(mask + 0.60 * halo, 0.0, 1.0)

        out_rgb = rgb.copy()
        out_rgb += emissive[..., None] * np.array([58.0, 30.0, 10.0], dtype=np.float32)
        out_rgb += halo[..., None] * np.array([138.0, 56.0, 18.0], dtype=np.float32)
        out_rgb = np.clip(out_rgb, 0.0, 255.0) * visibility[..., None]

        rgba = np.dstack([out_rgb, alpha * 255.0]).astype(np.uint8)
        return self._enforce_tile_wrap(rgba)

    def generate(self, output_dir: Path) -> None:
        variants = [
            "center",
            "edge_top",
            "edge_bottom",
            "edge_left",
            "edge_right",
            "corner_top_left",
            "corner_top_right",
            "corner_bottom_left",
            "corner_bottom_right",
        ]

        output_dir.mkdir(parents=True, exist_ok=True)
        frames_dir = output_dir / "frames"
        static_dir = output_dir / "static"
        sheets_dir = output_dir / "sheets"
        for path in (frames_dir, static_dir, sheets_dir):
            path.mkdir(parents=True, exist_ok=True)

        frame_images: dict[str, list[Image.Image]] = {name: [] for name in variants}
        static_images: dict[str, Image.Image] = {}

        for frame_index in range(self.frame_count):
            # Motion is intentionally slowed to feel thicker than water.
            t = (frame_index / self.frame_count) * 0.58
            rgb, emissive = self._base_lava(t)

            for variant in variants:
                mask = self._variant_mask(variant, t)
                rgba = self._compose_rgba(rgb, emissive, mask)
                image = Image.fromarray(rgba)

                frame_path = frames_dir / f"lava_{variant}_{frame_index:02d}.png"
                image.save(frame_path, optimize=True)
                frame_images[variant].append(image.copy())

                if frame_index == 0:
                    static_path = static_dir / f"lava_{variant}.png"
                    image.save(static_path, optimize=True)
                    static_images[variant] = image.copy()

        for variant, images in frame_images.items():
            sheet = Image.new("RGBA", (self.size * self.frame_count, self.size), (0, 0, 0, 0))
            for idx, image in enumerate(images):
                sheet.paste(image, (idx * self.size, 0), image)
            sheet.save(sheets_dir / f"lava_{variant}_sheet.png", optimize=True)

        preview_layout = [
            ["corner_top_left", "edge_top", "corner_top_right"],
            ["edge_left", "center", "edge_right"],
            ["corner_bottom_left", "edge_bottom", "corner_bottom_right"],
        ]
        preview = Image.new("RGBA", (self.size * 3, self.size * 3), (0, 0, 0, 0))
        for row_idx, row in enumerate(preview_layout):
            for col_idx, key in enumerate(row):
                tile = static_images[key]
                preview.paste(tile, (col_idx * self.size, row_idx * self.size), tile)
        preview.save(output_dir / "lava_variant_preview.png", optimize=True)

    def write_readme(self, output_dir: Path, seed: int) -> None:
        created_at = dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        text = f"""# Juicy Lava 2D Tile Set

Generated by `Document/tools/generate_lava_assets.py` on {created_at}.

## Specs
- Resolution: {self.size}x{self.size} per tile
- Frames per loop: {self.frame_count}
- Variants: center, 4 edges, 4 corners
- Background: transparent PNG (RGBA)
- Style: cartoon / hand-painted lava with bubbly, viscous flow and emissive glow
- Seed: {seed}

## Output
- `static/`: frame 0 of each variant
- `frames/`: all frame-by-frame PNGs
- `sheets/`: horizontal spritesheet per variant
- `lava_variant_preview.png`: quick visual overview of the 3x3 layout

## Regenerate
```bash
python Document/tools/generate_lava_assets.py --size {self.size} --frames {self.frame_count} --seed {seed}
```
"""
        (output_dir / "README.md").write_text(text, encoding="utf-8")


def default_output_path() -> Path:
    repo_root = Path(__file__).resolve().parents[2]
    return repo_root / "Assets" / "Textures" / "JuicyLava2D"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate stylized seamless lava tile assets.")
    parser.add_argument("--size", type=int, default=512, help="Tile width/height in pixels.")
    parser.add_argument("--frames", type=int, default=8, help="Number of looped animation frames.")
    parser.add_argument("--seed", type=int, default=20260321, help="Seed for deterministic generation.")
    parser.add_argument("--output", type=Path, default=default_output_path(), help="Output directory.")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    generator = LavaAssetGenerator(size=args.size, frame_count=args.frames, seed=args.seed)
    generator.generate(args.output)
    generator.write_readme(args.output, seed=args.seed)
    print(f"Generated lava assets in: {args.output}")


if __name__ == "__main__":
    main()
