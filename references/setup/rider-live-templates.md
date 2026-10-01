# Rider Live Templates（学自 ModTemplate-StS2 `.sln.DotSettings`）

> 官方模板自带 4 个右键快速模板：**Custom Card / Custom Relic / Custom Power**（+ Potion），在文件夹右键 → 新建文件即可生成对应基类文件。枚举值可直接抄。

## Custom Card 字段枚举

| 字段 | 可选值 |
|------|--------|
| `TYPE` | `Attack, Skill, Power, Status, Curse, Quest` |
| `RARITY` | `Basic, Common, Uncommon, Rare, Ancient, Event, Token, Status, Curse, Quest` |
| `TARGET` | `Self, AnyEnemy, AllEnemies, RandomEnemy, AnyPlayer, AnyAlly, AllAllies, Osty` |
| `COST` | 数字或空 |
| `CLASS` | 自动取文件名（`getAlphaNumericFileNameWithoutExtension()`） |
| `NAMESPACE` | 自动取文件所在命名空间（`fileDefaultNamespace()`） |

> 生成的文件继承模组自己的基类（如 `CharModCard`），`[Pool]` 由基类注解自动继承，无需每个子类重复标。
> 自建模板可照抄这套结构：`/Default/PatternsAndTemplates/LiveTemplates/Template/=...` 键存于 `.sln.DotSettings`。
