# 先古之民事件：模板与可覆盖成员

## 基础模板

```csharp
public class MyAncient : AncientEventModel
{
    // 对话组：对象初始化器（不是多参构造）
    protected override AncientDialogueSet DefineDialogues()
    {
        return new AncientDialogueSet
        {
            FirstVisitEverDialogue = new AncientDialogue("event:/sfx/npcs/my_ancient/intro"),
            CharacterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>
            {
                [AncientEventModel.CharKey<Ironclad>()] = new List<AncientDialogue>
                {
                    new AncientDialogue("event:/sfx/npcs/my_ancient/talk_0") { VisitIndex = 0 },
                },
            },
            AgnosticDialogues = new List<AncientDialogue>
            {
                new AncientDialogue(""),  // 无音效的单行
            },
        };
    }

    // 所有可能选项（抽象属性）
    public override IEnumerable<EventOption> AllPossibleOptions =>
        new List<EventOption>
        {
            new EventOption(this, OnBless, "MY_ANCIENT.options.BLESS"),
        };

    private async Task OnBless()
    {
        SetEventFinished(L10NLookup("MY_ANCIENT.options.BLESS.description"));
    }
}
```

> `AncientDialogue(params string[] sfxPaths)`：每行一个 SFX 路径，无音效行传 `""`。`VisitIndex` 控制第几次拜访出现，`IsRepeating` 由本地化键 `r` 后缀自动推断。

---

## 可覆盖成员（真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `DefineDialogues()` | `protected abstract AncientDialogueSet` | 定义对话组 |
| `AllPossibleOptions` | `public abstract IEnumerable<EventOption>` | 所有可能出现的选项 |
| `AnyCharacterDialogueBlacklist` | `public virtual IEnumerable<CharacterModel>` | 不显示特定角色对话 |
| `DialogueColor` | `public virtual Color` | 对话颜色 |
| `AmbientBgm` | `public virtual string` | 环境 BGM |

> ⚠️ 旧版写的 `OptionPools`/`IsValidForAct`/`ShouldForceSpawn` 是 **BaseLib `CustomAncientModel`** 的，原生 `AncientEventModel` 不存在。章节有效性由章节配置决定，非本类控制。

