---
name: sekibune-sprite-generator
description: 生成《大航海时代2》风格的帆船序列帧，包含上、下、左上、左下、左共5个方向，每个方向4帧，总计20张128x128的透明背景像素图。
---

# 帆船序列帧生成技能

本技能旨在帮助用户快速生成《大航海时代2》风格、特定方向的帆船序列帧。

## 何时使用
当用户需要生成一组《大航海时代2》风格的帆船像素动画，且要求包含特定方向、开帆状态、透明背景时。

## 使用说明
1. **输入参数**：
   - 帆船样式（由外部输入）
   - 要求：
     - 包含5个方向：上、下、左上、左下、左
     - 每个方向4帧，总计20帧
     - 单帧尺寸：128x128
     - 背景：Alpha透明
     - 状态：所有帆均为“开帆”状态
     - 效果：无海水/海浪，无边缘黑线
2. **执行流程**：
   - 调用 `image_gen` 工具，在 prompt 中明确要求：“Uncharted Waters 2 style pixel art, no water, no waves, pure Alpha transparent background, sails fully deployed, 20 frames (5 rows x 4 frames), directions: Up, Down, Up-Left, Down-Left, Left, 128x128 per frame”。
   - 若背景未透明，调用后续处理脚本进行绿幕/背景清理。
   - 验证边缘透明度，确保没有黑色的切割线。
