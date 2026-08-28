# 自定义角色：场景、Spine、注册与本地化

## 六、场景搭建注意事项

### 角色场景（NCreatureVisuals）

根节点必须挂载 `NCreatureVisuals` 脚本：

```csharp
public class CustomCreatureVisuals : NCreatureVisuals { }
```

场景结构：
```
NCreatureVisuals (根节点)
  └─ Visuals (Node2D，唯一名称访问 %)
```

### 能量计数器场景（NEnergyCounter + MegaLabel）

```csharp
public class CustomMegaLabel : MegaLabel { }
```

```
NEnergyCounter (根节点)
  └─ Label (MegaLabel, MinFontSize=32, MaxFontSize=36)
```

### 卡牌拖尾场景（NCardTrailVfx）

```csharp
public class CustomCardTrailVfx : NCardTrailVfx { }
```

```
NCardTrailVfx (根节点)
  └─ NCardTrail (Line2D) × 2
```

---

## 七、Spine 动画导入

1. Mod 根目录创建 `bin/` 文件夹
2. 下载 Godot-Spine 插件 GDExtension 版放入 `bin/`
3. 重启编辑器加载插件
4. Spine JSON 后缀改为 `.spine-json`

---

## 八、注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class AllCharactersPatch
{
    public static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        __result = __result.Append(new MyCharacter());
    }
}
```

> 注意：`AllCharacters` 真实返回 `IEnumerable<CharacterModel>`（不是 `List`），Postfix 需 `ref IEnumerable<CharacterModel>`。
---

## 九、本地化

路径：`res://<模组ID>/localization/<语言代码>/characters.json`

```json
{
  "MY_CHARACTER": {
    "title": "自定义角色",
    "description": "一个来自异世界的战士。"
  }
}
```

> 键与类名 ID 一致（大驼峰 → 大写加下划线），`title` 用于角色名（旧版写 `name` 是错的）。

