# 卡牌肖像图尺寸规格（学自 ModTemplate-StS2 `CustomCardModel`）

| 用途 | 分辨率 | 路径（`card_portraits/`） |
|------|--------|---------------------------|
| 常规卡面图 | **1000x760**（500x380 也行，会被缩放） | `images/card_portraits/<id>.png` |
| 全幅卡面图 | **606x852** | `images/card_portraits/big/<id>.png` |
| 全幅小图（高效变体） | 250x350 | 同上 `big/` |
| 常规小图（高效变体） | 250x190 | `images/card_portraits/<id>.png`（替换大图） |
| 测试卡面（Beta） | 同常规 | `images/card_portraits/beta/<id>.png` |

> 模板基类映射：`CustomPortraitPath`（大图）→ `BigCardImagePath()`；`PortraitPath`（常规）→ `CardImagePath()`；`BetaPortraitPath` → `beta/` 子目录。找不到图自动回退 `card.png`（见 design-patterns-assets.md）。
