# 自定义能量图标：资源准备与常见问题

## 图标资源准备

### 大图标

放在 `res://<模组ID>/images/energy/` 下，推荐 32x32 或 64x64 PNG，Godot 会自动处理。

如果希望用 `.tres` 纹理资源（支持着色/动画），参照游戏默认 `ui_atlas.sprites/card/energy_red.tres`：

```bash
# 简单的 PNG 即可工作，注意路径与 BigIconPath 一致
images/energy/my_energy_icon.png
```

### 文本图标

内联图标在 BBCode 中显示，尺寸受文本行高限制，推荐 16x16 或 20x20 PNG。

### 打包

所有图标资源必须随模组打包进 PCK（`Publish` 而非 `Build`）。

## 常见问题

| 问题 | 解决 |
|------|------|
| GetPath Patch 不触发 | 确认 `prefix` 参数类型是 `string`；检查 `PatchAll()` 是否执行 |
| 分隔符冲突 | 普通 `EnergyColorName` 确保不包含 `∴`（该字符在 C# 中几乎不会出现） |
| 文本图标不替换 | Transpiler 匹配点可能变了，反编译 `TryEvaluateFormat` 确认 IL 结构 |
| 其他池也被影响了 | Patch 里有 `DecodePool` 判断，不是你的编码就放行 |
| 多人模式 | 客户端和服务端都需要有相同资源，图标路径依赖 PCK 打包 |

## 演进路线

- 当前：手动 Patch，插两个点实现接口
- 更优：如果 BaseLib 被设为依赖，直接让池模型继承 `CustomCardPoolModel` 即可，自动获得自定义图标支持
- 纯原生优点：完全可控，轻量，不背依赖

## 参见

- [energy-custom-icon-core.md](energy-custom-icon-core.md) — 接口定义与用法
- [energy-custom-icon-patches.md](energy-custom-icon-patches.md) — Patch 实现