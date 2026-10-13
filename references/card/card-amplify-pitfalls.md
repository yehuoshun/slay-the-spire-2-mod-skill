# 自定义卡牌：增幅常见坑

> 从 [card-amplify.md](card-amplify.md) §5 拆出。增幅（Amplify/Kicker）系统实现时的坑与解法。

| 坑 | 解法 |
|----|------|
| AutoPlay 不经过费用计算 | `OnPlay` 里 `cardPlay.IsAutoPlay` 强制 `AmplifiedInPlay = true` |
| 打出一张后再打下一张，增幅状态残留 | `AfterCardPlayed` 里 `cardPlay.Card == this && cardPlay.IsLastInSeries` 时重置 |
| 增幅费用显示没刷新 | 三个 Patch 缺一不可：费用计算 / 费用标签 / 高亮 |
| `EnergyVar` 只显示一次 | 增幅态总费用手写拼进标签（基础费 + Kicker） |
