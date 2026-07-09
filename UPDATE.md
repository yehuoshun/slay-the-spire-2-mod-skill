# 更新工作流

> 🦞 触发词：「更新 skill」「看看活跃参考有没有新东西」「同步最新版本」

---

## 核心逻辑

```
对每个活跃参考仓库：
  取最新 commit 日期
  ├─ commit 日期 > README 中的 学习 日期 → 学习（克隆→分析→更新 skill 文件）
  └─ commit 日期 ≤ 学习 日期 → 没新东西，跳过，最后只更新 学习 日期
```

---

## 1. 检查活跃参考仓库 + 对比学习日期

```bash
# 先读 README 中的学习日期
grep "学习：" README.md
```

```bash
# 取每个仓库最新 commit 日期
for repo in s1f102500012/sts2mod YuWan886/Sts2-YuWanCard Alchyr/BaseLib-StS2 Alchyr/ModTemplate-StS2 xhyrzldf/ModConfig-STS2; do
  echo "=== $repo ==="
  curl -s "https://api.github.com/repos/$repo/commits?per_page=1" | python3 -c "
import json,sys
commits=json.load(sys.stdin)
if isinstance(commits, list) and commits:
  c=commits[0]
  sha=c['sha'][:7]
  date=c['commit']['committer']['date'][:10]
  msg=c['commit']['message'].split('\n')[0]
  print(f'  latest: {sha} {date} {msg}')
else:
  print('  ERROR:', commits.get('message','unknown'))
"
done
```

**判断**：每个仓库的 `latest date` > README 中该仓库的 `学习` 日期 → 进入步骤 2。否则跳过。

---

## 2. 克隆需要学习的仓库到临时目录

```bash
mkdir -p /tmp/sts-study && cd /tmp/sts-study
# 只克隆有新 commit 的仓库
```

---

## 3. 分析变化

```bash
# 每个仓库看学习日期之后的提交
cd /tmp/sts-study/<repo> && git log --oneline --since="<该仓库的学习日期>" && git diff --stat HEAD~10..HEAD
```

---

## 4. 重点看什么

| 仓库 | 关注点 |
|------|--------|
| **STS2Mod** | 新模组架构、新 API 用法、状态机模式、事件系统 |
| **YuWanCard** | Builder 模式演进、反射注册、多人同步、序列化去重 |
| **BaseLib** | 新工具类、新 Hook 接口、新奖励类型、新目标类型 |
| **ModTemplate** | csproj 变化、.NET 版本、构建脚本 |
| **ModConfig** | 新控件类型、API 变化 |

---

## 5. 更新目标文件

| 发现 | 更新文件 |
|------|---------|
| 新 API | `SKILL.md` 核心 API 速查 + `appendices.md` 附录 A |
| 新写法模式 | `real-code-patterns.md` |
| 新常见坑 | `SKILL.md` 常见坑 + 对应 `modes-*.md` 常见问题 |
| 新架构模式 | 对应 `modes-*.md` 新增章节 |
| 新 Patch 目标 | `modes-harmony.md` 常用 Patch 目标表 |
| 版本号变化 | `README.md` 活跃参考描述 |

---

## 6. 更新学习

`README.md` 活跃参考列表中：
- 有学习过的仓库 → `（学习：YYYY-MM-DD）` 更新为当天日期
- 没新 commit 的仓库 → 也更新为当天日期（表示「检查过了，确认没新东西」）

---

## 7. 提交

```bash
cd /root/.openclaw/workspace/skills/slay-the-spire-2-mod-skill
git add -A
# 隐私检查
git diff --cached | grep -iE '(token|secret|key|password|api_key|webhook)' | grep -v '.example' || true
git commit -m "更新活跃参考：<简述变化>"
git push
```

---

## 8. 清理

```bash
rm -rf /tmp/sts-study
```

---

## 活跃参考仓库列表

| 仓库 | URL | 说明 |
|------|-----|------|
| STS2Mod | `s1f102500012/sts2mod` | 多模组合集 |
| YuWanCard | `YuWan886/Sts2-YuWanCard` | 角色 Mod |
| BaseLib | `Alchyr/BaseLib-StS2` | 基础库 |
| ModTemplate | `Alchyr/ModTemplate-StS2` | 脚手架 |
| ModConfig | `xhyrzldf/ModConfig-STS2` | 设置框架 |

## 上次更新

- **日期**: 2026-07-10
- **版本**: BaseLib v3.3.4, YuWanCard v0.5.10, STS2Mod 海克斯 0.8.5 / 集成战略事件 0.5.0