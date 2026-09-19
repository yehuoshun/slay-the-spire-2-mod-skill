# 先古之民事件（Ancient）

> 参考：[杀戮尖塔2模组开发教程06 - 自定义事件 - 哔哩哔哩](https://www.bilibili.com/opus/1180714323922649110)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomAncientModel.cs`

先古之民出现在每一章开头，继承 `AncientEventModel`。相比普通事件，额外要求实现对话系统和选项列表。

导航见 [event.md](event.md)。

## 常见问题

| 问题 | 解决 |
|------|------|
| 先古之民不出现 | 检查 `AllAncients` Patch（ref IEnumerable）+ 5 种纹理资源 |
| 对话不显示 | `DefineDialogues` 对象初始化器正确 |
| 选项不出现 | 检查 `AllPossibleOptions` 返回 |

## 演进路线

- 当前：手动 Patch 注入
- 纯原生自动注册（Attribute 标记 + 反射注入）——已并入 v3「进阶」
