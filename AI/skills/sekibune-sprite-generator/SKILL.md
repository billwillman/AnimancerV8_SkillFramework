---
name: sekibune-sprite-generator
description: This skill generates Uncharted Waters 2 (大航海时代2) style sailing ship sprite sheets. It produces 20 frames of pixel art (5 directions x 4 animation frames each) at 128x128 per frame with fully transparent backgrounds. The ship type is provided by the user. This skill should be used when the user asks to generate ship sprites, sailing ship animation frames, or Uncharted Waters style pixel art ships.
---

# 帆船序列帧生成技能

Generate《大航海时代2》style sailing ship sprite sheets with 5 directions, 4 frames each, transparent backgrounds, and fully deployed sails.

## When To Use

Trigger this skill when the user requests any of the following:
- 生成帆船序列帧 / ship sprite sheets
- 大航海时代风格的船只像素图
- Multi-direction sailing ship animation frames
- Any ship type + sprite/序列帧/animation request

## Execution Workflow

Follow these steps strictly in order. Do not skip any step.

### Step 1: Determine Ship Type

Extract the ship type from the user's request. Examples:
- "西班牙大帆船" → `Spanish Galleon`
- "关船" → `Japanese Sekibune warship`
- "卡拉维尔帆船" → `Caravel`
- "护卫舰" → `Frigate`

If the user provides a Chinese ship name, translate it to English for the prompt. Keep the original Chinese name for file naming.

### Step 2: Generate Sprite Sheet

1. Read the prompt template from `references/prompt_template.md`
2. Replace `[SHIP_TYPE]` with the determined ship type
3. Call `image_gen` with the following parameters:
   - **prompt**: The completed prompt template text
   - **size**: `1024x1024`
4. Record the output file path for the next step

### Step 3: Remove Green Background

Execute the green screen removal script to convert the solid green background to alpha transparency:

```
powershell -ExecutionPolicy Bypass -File "<skill_base_dir>/scripts/remove_green_bg.ps1" -InputFile "<generated_image_path>" -OutputFile "<output_dir>/<ShipName>_Transparent.png"
```

Where:
- `<skill_base_dir>` is the absolute path to this skill's directory
- `<generated_image_path>` is the file from Step 2
- `<output_dir>` is the workspace's `generated-images` directory (or user-specified location)
- `<ShipName>` is a clean name derived from the ship type

The script uses a V3 two-pass algorithm:
1. **Pass 1 (Strict RGB Difference)**: Safely removes the green background while 100% protecting the ship's native colors (like yellow, cyan, and wood).
2. **Pass 2 (Edge Erosion)**: Cleans up any remaining dark or slightly green anti-aliasing artifacts on the edges of the ship.

### Step 4: Verify Transparency

Run the alpha verification script to confirm the output meets quality constraints:

```
powershell -ExecutionPolicy Bypass -File "<skill_base_dir>/scripts/check_alpha.ps1" -InputFile "<transparent_image_path>"
```

Check the output:
- **验证通过**: Proceed to Step 5
- **验证未通过**: 
  - If transparent pixel percentage < 40%: The green screen removal failed. Re-run Step 2 with a new generation, then repeat Steps 3-4
  - If edge dark pixels > 20: Residual black edges remain. Report to the user and suggest manual cleanup or re-generation

### Step 5: Deliver Output

Report the final output file path to the user along with:
- Ship type and direction breakdown (5 directions x 4 frames = 20 total)
- Confirmation that sails are deployed in all frames
- Confirmation that background is alpha transparent
- Verification script results (pass/fail + stats)

## Output Specification

| Property | Value |
|----------|-------|
| Total frames | 20 (5 rows × 4 columns) |
| Frame size | 128×128 pixels |
| Directions | Up, Down, Up-Left, Down-Left, Left |
| Frames per direction | 4 animation frames |
| Background | Alpha transparent (ARGB 32-bit PNG) |
| Sail state | Fully deployed in all frames |
| Water/waves | None |
| Edge artifacts | None (no black cut lines) |

## Resources

### scripts/
- `remove_green_bg.ps1` — V3 algorithm with strict RGB green screen removal and edge artifact erosion. Preserves all ship colors. Accepts `-InputFile` and `-OutputFile` parameters.
- `check_alpha.ps1` — Transparency verification. Accepts `-InputFile` parameter. Returns exit code 0 (pass) or 1 (fail).

### references/
- `prompt_template.md` — Parameterized image generation prompt with `[SHIP_TYPE]` placeholder and all hard constraints.
