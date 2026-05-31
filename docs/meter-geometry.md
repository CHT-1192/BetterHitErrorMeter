# Error Meter 几何参数

## Straight Meter（直线型）

| 元素 | x | y | 宽 | 高 | 颜色 |
|------|---|---|----|----|------|
| 黑底 | 35 | 68 | 330 | 64 | #000000 opacity=0.333 |
| 绿色 | 43 | 94 | 314 | 12 | #5FFF4E |
| 黄色 L | 88 | 94 | 37 | 12 | #FCFF4D |
| 黄色 R | 275 | 94 | 37 | 12 | #FCFF4D |
| 橙色 L | 50 | 94 | 38 | 12 | #FF6F4D |
| 橙色 R | 312 | 94 | 38 | 12 | #FF6F4D |
| 红色 L | 43 | 94 | 7 | 12 | #FF0000 |
| 红色 R | 350 | 94 | 7 | 12 | #FF0000 |
| 指针 | 198 | 90 | 4 | 20 | #FFFFFF |

全 `<rect>`，无 rx，无 `<path>`。

## Curved Meter（曲线型）

### 同心圆参数

| 环 | 内径 | 外径 | 中点 r | 线宽 | 颜色 |
|----|------|------|--------|------|------|
| 灰底 | 118 | 183 | 150.5 | 65 | #AAAAAA |
| 色环 | 146.5 | 159 | 152.75 | 12.5 | — |

圆心：(200, 200)，即 400×200 viewBox 底边中点。

### 角度（从水平线向上测）

| 带 | 夹角 | SVG 弧范围 | 总弧 |
|----|------|-----------|------|
| 黑（灰底） | 24° | 204° ~ 336° | 132° |
| 红（TooEarly/Late） | 27° | 207° ~ 333° | 126° |
| 橙（VeryEarly/Late） | 30° | 210° ~ 330° | 120° |
| 黄（Early/LatePerfect） | 45° | 225° ~ 315° | 90° |
| 绿（Perfect） | 60° | 240° ~ 300° | 60° |

弧心在 270°（正上方）。
左 = CCW（减角）= Early hit，右 = CW（增角）= Late hit。

### dashoffset 公式

SVG 圆笔画起点为 3 点钟方向（0°），顺时针前进。dashoffset 正向为逆时针偏移。

```
dashoffset = (360 - start_angle) / 360 × circumference
```

### 颜色标准

| 颜色 | Hex | 判定 |
|------|-----|------|
| 绿 | #5FFF4E | Perfect |
| 黄 | #FCFF4D | EarlyPerfect / LatePerfect |
| 橙 | #FF6F4D | VeryEarly / VeryLate |
| 红 | #FF0000 | TooEarly / TooLate |

### SVG 实现约束

- 零 `<path>`，只用 `<circle>` + `stroke-dasharray`（曲线）或 `<rect>` + `<line>`（直线）
- 无 rx 圆角
- 注释不得跨行
