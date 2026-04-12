# 帆船序列帧生成 Prompt 模板

将 `[SHIP_TYPE]` 替换为用户提供的帆船类型名称后使用。

## Prompt

```
Uncharted Waters 2 style pixel art of a [SHIP_TYPE].
CRITICAL: NO WATER, NO WAVES, NO SEA, NO OCEAN. Render ONLY the ship itself on a pure, flat solid green background (#00FF00). Nothing else in the background.
CRITICAL: The sails MUST be fully deployed, open, and billowing in all directions and all frames. Every single frame must show open sails.
CRITICAL: Sprite sheet with exactly 20 frames arranged in a strict grid of 5 rows and 4 columns.
Each row represents one sailing direction:
  Row 1 (top): Sailing UP (away from viewer)
  Row 2: Sailing DOWN (toward viewer)  
  Row 3: Sailing UP-LEFT (diagonal away-left)
  Row 4: Sailing DOWN-LEFT (diagonal toward-left)
  Row 5 (bottom): Sailing LEFT (side view)
Each column is a successive animation frame showing slight movement variation.
16-bit retro pixel art aesthetic, high contrast, clean pixel lines, NO anti-aliasing blur.
Each frame is exactly 128x128 pixels, uniform frame size, perfectly aligned grid with NO gaps and NO black borders.
The ship must be centered within each 128x128 cell. No part of the ship should be cut off at cell edges.
```

## 参数说明

| 参数 | 说明 | 示例 |
|------|------|------|
| `[SHIP_TYPE]` | 帆船的具体类型，中文或英文均可 | `Spanish Galleon`, `Japanese Sekibune`, `Caravel`, `Frigate` |

## image_gen 调用参数

- **size**: `1024x1024`（20帧 = 5行x4列，每帧 128x128，正好适配 512x640，但 1024x1024 给 AI 更多空间生成细节）
