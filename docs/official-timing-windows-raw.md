# ADOFAI 官方判定窗口文档（原始内容）

> 来源：https://7thbeat.notion.site/Timing-Window-Calculations-a80645e4f14f487b9696a244e1727c57
> 作者：7thbeat (游戏开发者)
> 保存日期：2026-05-28
> 以下为页面原始文本，未做任何修改

---

The timing window calculations for ADOFAI are quite complex! While I think all the information about it is already out there, it seems like a good idea to keep a canonical explanation of it available to prevent any confusion.

At their core, the timing windows are based off of the angle between the planet and the tile when a key is pressed. For non-hold tiles:

d = (planet angle) - (tile angle)

The windows are determined as follows:

- -60° ≤ d < -45°    →  Too Early
- -45° ≤ d < -30°    →  Very Early
- -30° ≤ d ≤ 30°     →  Perfect / Pure Perfect
- 30° < d ≤ 45°      →  Late Perfect
- 45° < d ≤ 60°      →  Very Late
- Otherwise           →  Too Late

As a visualization, this is the where the timing windows line up:

[图片：显示判定区间的饼图/弧形图，标注了 Too Early, Very Early, Early Perfect, Perfect, Late Perfect, Very Late, Too Late 的对应角度范围]

---

## BPM 的影响

With the angle based calculations, it means the faster the BPM the smaller the hit windows. We can convert the angle windows to millisecond windows with the following formula:

angle° × (1 beat / 180°) × (1 / BPM) × (60 s / 1 min) × (1000 ms / 1 s) = (angle° / BPM) × (1000 / 3)

So for example at 180 BPM, we have these timing windows:

- 60° / 180 BPM × 1000/3 ≈ ±111 ms    (Counted)
- 45° / 180 BPM × 1000/3 ≈ ±83 ms     (Perfect)
- 30° / 180 BPM × 1000/3 ≈ ±56 ms     (Pure)

While for a majority of cases this works well, at high BPM these timings windows become way too small. Take for example 700 BPM:

- 60° / 700 BPM × 1000/3 ≈ ±29 ms
- 45° / 700 BPM × 1000/3 ≈ ±21 ms
- 30° / 700 BPM × 1000/3 ≈ ±14 ms

This timing is too strict especially since missing even one tile results in failing! To get around this, we have two additional failsafes.

---

## 保险机制 1：BPM 上限 (BPM Shrinking Limit)

Once a certain BPM is reached, we stop shrinking the hit window for any BPMs higher than it. Initially this was set to 500 BPM, so the calculations came out to be:

- Counted: 60° / 500 BPM × 1000/3 ≈ ±40 ms
- Perfect: 45° / 500 BPM × 1000/3 ≈ ±30 ms
- Pure: 30° / 500 BPM × 1000/3 ≈ ±20 ms

In October 2020, we added a difficulty selector that lets you choose the BPM cap. It's important to note that this does not change how the hit windows work — just changes which failsafes are in place!

- **Strict timing**: hit windows for BPMs above ~310 will scale as if the BPM was at ~310 (the default setting before October 2020).
- **Normal timing**: hit windows for BPMs above ~220 will scale as if the BPM was at ~220.

Note that 310 and 220 aren't exact BPMs since we code the ms values directly. In reality, these values are 65ms and 91ms respectively, which come out to 307.69 BPM and 219.78 BPM respectively. For explanation simplicity though, we use 310 and 220.

In official levels, only Normal and Strict are available right now.

**There would be no difference for levels with BPMs below ~310, between Normal and Strict.** However, we add an additional buffer of 30BPM more before the toggle shows, so that there is a meaningful enough difference between Normal and Strict. So for official levels, the toggle shows only above 340 BPM.

But Normal margins are always the default, so in e.g. XO-X One Forgotten Night (which is 328 BPM), there is a 310 BPM cap being applied even though there is no toggle.

---

## 保险机制 2：25ms 硬下限 (Hard Shrink Cap)

Beyond the BPM limit, we also put a hard cap on the shrinking by forcing at least a 25ms window for all judgments. In reality, this only affects the Pure timing in a majority of cases.

---

## 飚速 (Speed Trials)

Probably the biggest misconceptions about speed trials is that the speed trial multiplier gets applied to the BPM. This is not actually the case! The hit windows are calculated as normal without any multiplier first, then it shrinks the window by a factor of 1/(speed trial multiplier). The reasoning behind this is to provide a harder challenge compared to the standard gameplay at that BPM.

Our 2nd failsafe still comes into play though, which may result in some strange judgment calculations. Take for example 1-X at 150 BPM with a 5x speed trial multiplier.

- 60° / 150 BPM × 1000/3 = ±133 ms → divided by 5x = ±27 ms
- 45° / 150 BPM × 1000/3 = ±100 ms → divided by 5x = ±20 ms → capped at 25 ms!

This is why in some high speed trial multipliers, there are only Very Early/Perfect/Very Late (and no Early Perfect/Late Perfect).

---

## 移动版 (Mobile)

On mobile, the normal BPM shrinking limit isn't used. Instead, we simply limit the hit windows to 90ms / 70ms / 50ms (Counted / Perfect / Pure). Speed trial multipliers are also applied to the BPM directly rather than applied to the calculated hit window.

---

## 判定窗口缩放事件 (Timing Window Scale)

As part of the Neo Cosmos release, we added a "Timing Window Scale" event that lets level creators shrink or expand the hit window. The scaling gets applied to the angle for each judgment calculation. For example with 50% scale:

- 60° × 50% = 30°
- 45° × 50% = 22.5°
- 30° × 50% = 15°

Note that the scaling **does not affect the BPM limit and hard 25ms cap failsafes** described above, meaning if the resulting calculated hit window is smaller than the one calculated by the failsafes, the hit window calculated by the failsafes is used. Take the judgment calculations at 300 BPM and 50% timing window scale as an example:

- 30° / 300 BPM × 1000/3 = ±33 ms → × 50% = ±17 ms → capped at 25ms (hard shrink cap)
- 45° / 300 BPM × 1000/3 = ±50 ms → × 50% = ±25 ms → capped at 25ms (hard shrink cap)
- 60° / 300 BPM × 1000/3 = ±67 ms → × 50% = ±33 ms → may be capped further depending on difficulty

Since the final formula changes depending on a bunch of different factors, I've remade the calculator to make it easy to figure out the hit windows for any BPM, speed trial multiplier, timing window scale, and difficulty.

[链接：ADOFAI Hit Margins Calculator v2]
[链接：原始 hit margins calculator sheet]

---

## 资源链接

- **官方判定计算器 v2**：[ADOFAI Hit Margins Calculator v2]
- **原始计算表格**：[original hit margins calculator sheet]
- **7BG KB 主页**：包含 FAQ、支持、故障排除等信息
