# 更新工作流

> 🦞 触发词：「更新 skill」「看看活跃参考有没有新东西」「同步最新版本」

---

## 1. 检查活跃参考仓库

```bash
for repo in s1f102500012/sts2mod YuWan886/Sts2-YuWanCard Alchyr/BaseLib-StS2 Alchyr/ModTemplate-StS2 xhyrzldf/ModConfig-STS2; do
  echo "=== $repo ==="
  curl -s "https://api.github.com/repos/$repo/commits?per_page=3" | python3 -c "
import json,sys
commits=json.load(sys.stdin)
if isinstance(commits, list):
  for c in commits[:3]:
    sha=c['sha'][:7]
    date=c['commit']['committer']['date'][:10]
    msg=c['commit']['message'].split('\n')[0]
    print(f'  {sha} {date} {msg}')
else:
  print('  ERROR:', commits.get('message','unknown'))
"
done
```

## 2. 克隆到临时目录

```bash
mkdir -p /tmp/sts-study && cd /tmp/sts-study
git clone --depth 50 https://github.com/s1f102500012/sts2mod.git sts2mod
git clone --depth 50 https://github.com/YuWan886/Sts2-YuWanCard.git YuWanCard
git clone --depth 50 https://github.com/Alchyr/BaseLib-StS2.git BaseLib
git clone --depth 50 https://github.com/Alchyr/ModTemplate-StS2.git ModTemplate
```

## 3. 分析变化

```bash
# 每个仓库看最近提交和文件变更
cd /tmp/sts-study/sts2mod && git log --oneline --since="<上次学习日期>" && git diff --stat HEAD~10..HEAD
cd /tmp/sts-study/YuWanCard && git log --oneline --since="<上次学习日期>" && git diff --stat HEAD~10..HEAD
cd /tmp/sts-study/BaseLib && git log --oneline --since="<上次学习日期>" && git diff --stat HEAD~10..HEAD
cd /tmp/sts-study/ModTemplate && git log --oneline --since="<上次学习日期>" && git diff --stat HEAD~10..HEAD
```

## 4. 重点看什么

| 仓库 | 关注点 |
|------|--------|
| **STS2Mod** | 新模组架构、新 API 用法、状态机模式、事件系统 |
| **YuWanCard** | Builder 模式演进、反射注册、多人同步、序列化去重 |
| **BaseLib** | 新工具类、新 Hook 接口、新奖励类型、新目标类型 |
| **ModTemplate** | csproj 变化、.NET 版本、构建脚本 |
| **ModConfig** | 新控件类型、API 变化 |

## 5. 更新目标文件

| 发现 | 更新文件 |
|------|---------|
| 新 API | `SKILL.md` 核心 API 速查 + `appendices.md` 附录 A |
| 新写法模式 | `real-code-patterns.md` |
| 新常见坑 | `SKILL.md` 常见坑 + 对应 `modes-*.md` 常见问题 |
| 新架构模式 | 对应 `modes-*.md` 新增章节 |
| 新 Patch 目标 | `modes-harmony.md` 常用 Patch 目标表 |
| 版本号变化 | `README.md` 活跃参考描述 |

## 6. 更新学习日期

`README.md` 活跃参考列表中的 `（学习：YYYY-MM-DD）` 更新为当天日期。

## 7. 提交

```bash
cd /root/.openclaw/workspace/skills/slay-the-spire-2-mod-skill
git add -A
# 隐私检查
git diff --cached | grep -iE '(token|secret|key|password|api_key|webhook)' | grep -v '.example' || true
git commit -m "更新活跃参考：<简述变化>"
git push
```

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

- **日期**: 2026-06-23
- **版本**: BaseLib v3.3.2, YuWanCard v0.5.8, STS2Mod 海克斯 0.7.6
