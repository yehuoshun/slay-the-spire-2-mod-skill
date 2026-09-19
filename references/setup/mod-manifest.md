# 模组清单（Mod Manifest JSON）

> 学自 [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 的 `ModTemplate.json` / `CharMod.json`。纯原生方案，零第三方依赖。

---

## 文件位置

放在项目根目录，与 `csproj` 同级：

```
MyMod/
├── MyModCode/
├── MyMod/
├── MyMod.csproj
├── MyMod.json          ← 这里
├── project.godot
└── Sts2PathDiscovery.props
```

> 文件名必须与 `csproj` 的 `AssemblyName` 一致（即 `.csproj` 文件名去掉扩展名）。

---

## 字段说明

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 模组唯一 ID，建议与项目名一致 |
| `name` | string | ✅ | 显示名称 |
| `author` | string | ✅ | 作者名 |
| `description` | string | ✅ | 模组描述 |
| `version` | string | ✅ | 语义版本号，建议 `"vX.Y.Z"` |
| `min_game_version` | string | ✅ | 兼容的最低游戏版本（当前 `"0.107.0"`） |
| `has_pck` | bool | ✅ | 是否有资源包（通常是 `true`） |
| `has_dll` | bool | ✅ | 是否有 C# DLL（通常是 `true`） |
| `dependencies` | array | ❌ | 依赖列表，每项 `{ "id", "min_version" }` |
| `affects_gameplay` | bool | ❌ | 是否影响游戏玩法（纯 UI/音频模组才设 `false`） |

---

## 纯原生模组的清单 JSON

```json
{
  "id": "MyMod",
  "name": "MyMod",
  "author": "YourName",
  "description": "A pure native Slay the Spire 2 mod.",
  "version": "v0.0.1",
  "min_game_version": "0.107.0",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [],
  "affects_gameplay": true
}
```

---

## 常见问题

**Q: `min_game_version` 填什么？**
A: 填当前游戏版本。当前 STS2 为 `"0.107.0"`，可从 Steam 库 → 游戏属性查看。

**Q: `dependencies` 留空会有问题吗？**
A: 不会。纯原生模组只依赖 `0Harmony.dll` + `sts2.dll`（在 csproj 中通过 `<Reference>` 引用），不需要在清单中声明依赖。

**Q: `has_pck` / `has_dll` 怎么判断？**
A: 有图片/场景等 Godot 资源 → `has_pck: true`；有 C# 源文件编译出 DLL → `has_dll: true`。通常两个都是 `true`。