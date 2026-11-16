# 卡牌本地化：占位符、条件块与富文本

> 学自 [lf201014/STS2_MarisaMod](https://github.com/lf201014/STS2_MarisaMod) 的 `localization/<语言>/cards.json`：四语同一 key 同语法，可交叉对照。

## 语法一览

| 语法 | 作用 | 例子 key |
|------|------|---------|
| `{Damage:diff()}` | 插入动态变量，按「与基础值的差」着色（6→9 显示绿色 `+3`） | `MARISAMOD-SPARK_STRIKE.description` |
| `{Energy:energyIcons()}` | 插入图标串（能量图标） | `MARISAMOD-BIG_CRUNCH.description` |
| `{CalculatedHits:plural:time\|times}` | 复数：`单数\|复数`，按变量值选一支 | `MARISAMOD-COLLECTING_QUIRK.description` |
| `{IfUpgraded:show: +3\|}` | 条件块：升级显示左侧 ` +3`，未升级显示右侧（此处为空） | `MARISAMOD-ENERGY_RECOIL.description` |
| `{InCombat:\n（…）\|}` | 条件块：只在战斗中显示括号里的动态预览 | `MARISAMOD-ABSOLUTE_MAGNITUDE.description` |
| `[gold]手牌[/gold]` | 富文本着色（BBCode 风格），可嵌在任意文本里 | 遍布全表（另有 `[blue]`） |
| `\n` | JSON 字符串里的换行转义 | 同上 |

原文两条：

```json
"MARISAMOD-ENERGY_RECOIL.description": "获得等同于你[gold]蓄力[/gold]层数{IfUpgraded:show:+3|}的[gold]格挡[/gold]。{InCombat:\n(获得{CalculatedBlock:diff()}格挡)|}",
"MARISAMOD-COLLECTING_QUIRK.description": "…{InCombat:\n(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}"
```

条件块内可再套 `{X:diff()}`、`{X:plural:…}`。`{InCombat:…|}` 见 8 张卡：ABSOLUTE_MAGNITUDE / BIG_CRUNCH / WHITE_DWARF / COLLECTING_QUIRK / ENERGY_RECOIL / GRAVITY_BEAT / OCCULTATION / TABOO_SPARK。

## 括号是展示符号，不是语法

条件块自身不带括号：`{IfUpgraded:show: +3|}` 出来就是裸的 ` +3`。zhs 的 `BIG_CRUNCH` 用半角 `()`、`WHITE_DWARF` 用全角 `（）`——括号纯属排版。

## 为什么动态预览要包进条件块

`CalculatedDamage`/`CalculatedBlock` 的倍率必须读战斗态：`Colorless/TabooSpark.cs:29` 的 `WithMultiplier((_, monster) => monster?.GetPowerAmount<HoukaiPower>() ?? 0)` 取自目标身上的崩坏层数，`AbsoluteMagnitude.cs:22` 取自 `card.Owner.Creature.GetPower<ChargeUpPower>()`。牌组、商店里既无 CombatState 也无目标怪，算出来只能是错数——故预览整块包进 `{InCombat:…|}`，战斗外只留静态基础值。

## 与上表不一致的写法（照实记录）

- zhs `MARISAMOD-METEORIC_SHOWER.description` 写 `至多X{IfUpgraded:show:+1}张牌`，**没有竖线**（其余三语为 `{…:＋1|}`）；缺省「否则」分支是否等价空分支，**未在证据中确认语义**。
- `plural:` 在 cards.json 只见于 eng（6 处 `card|cards`、5 处 `time|times`）；zhs/jpn 的 `powers.json` 把它借作「两种说法二选一」（jpn 有 `{Amount:plural:回|回}`）。
- jpn/kor 缺 `MARISAMOD-TABOO_SPARK.*`、`MARISAMOD-PRISMATIC_NEBULAR.*`（zhs/eng 209 key，jpn/kor 200），这两张卡的条件块只在 zhs/eng 出现。
- zhs `TABOO_SPARK.description` 的 `{InCombat:` 前没写 `\n`（其余 7 张都带）。
- 另有 `{energyPrefix:energyIcons(1)}`（`GRAND_CROSS`、`SINGULARITY`）上表未列；eng `PRISMATIC_NEBULAR` 的否则分支以空格开头（`| same amount`），会原样显示。

## 常见问题

| 现象 | 原因 / 处置 |
|------|------------|
| `{Damage}` 连花括号一起显示 | 变量名与 `CanonicalVars` 里的不一致（`DynamicVars["Div"]` 就得写 `{Div:…}`） |
| 升级后卡面数字没变 | 漏了 `UpgradeValueBy`（`EnergyRecoil.cs:31`） |
| 战斗外显示 0 或错数 | 计算变量没包 `{InCombat:…\|}`；描述里多出括号则是把展示括号写进了语法 |
