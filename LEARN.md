# 学习流程

当老板说"学习"或发来教程链接时，按以下流程执行：

---

## 学习步骤

0. **查 LEARNED.md** — 目标仓库是否已学？已学只跟进增量（git fetch + diff），未学才完整学习（详见 LEARNED.md 前置步骤）

1. **读内容** — 读老板发的链接/文档/代码
2. **提炼关键知识** — 提取可复用的代码模板、API 用法、常见坑
3. **写 references** — 创建 `references/<模块>/<模块>.md`，结构：
   - 概述
   - 代码模板（标注引用来源）
   - API 速查
   - 常见问题
   - 超过 4000 字符时按章节拆分为 `xx-*.md` 子文件，主文件保留导航表
4. **标注演进路线** — 当前方案如果还有更优方案（如硬注册 → 一键注册），在末尾用 ## 演进路线 注明，方便后期替换
5. **更新 SKILL.md** — 参考资料表加新行
6. **更新 README.md** — 状态列表勾选 + 鸣谢追加
7. **更新** — 新学内容直接写入 `references/<模块>/<模块>.md` 或对应子文件，审查通过后更新导航页为最新版
8. **commit + push**
9. **测试验证** — 文档改动必须过 [sts2-mod-examples](https://github.com/yehuoshun/sts2-mod-examples) 的编译验证（见下「测试与验证流程」）
10. **同步备份仓库** — 主仓库 commit 后，把 `references/` 增量同步到 [slay-the-spire-2-mod-skill-archive](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive)（见下）

---

## 测试与验证流程

> **闭环**：skill 文档（唯一创作源）→ sts2-mod-examples 可编译示例 → CI 真编译 → 报错回流修文档。
> 文档里的任何 API/签名，示例仓库编译不过 = 文档有问题（2026-10-02 起强制执行，当天靠此抓到 10+ 处编造 API）。

1. **改文档** — 新增/修改 references 模块内容
2. **同步示例** — 在 `sts2-mod-examples/Sts2ModExamplesCode/<模块>/` 补/改对应示例：
   - 新 API 必须有示例类覆盖（类型/方法逐条对照 `sts2-res/src/`）
   - 修改的 API 更新示例调用
3. **本地静态自检** — `python3 scripts/check-api.py`（API 白名单，防幻觉）通过
4. **push → CI** — push 后 GitHub Actions 自动跑：dotnet build（真编译）+ ModAnalyzers + 白名单 + 本地化 JSON 校验
5. **CI 全绿才算完** — 编译报错 → 按错误修正示例（或修正文档）→ 重推，直到绿色
6. **示例里踩的坑必须回流** — 真编译暴露的文档 bug/签名差异，当天修进 references + SKILL.md/README 同步

> 覆盖矩阵（24 模块 ↔ 示例文件）见 sts2-mod-examples README「覆盖内容」表。新增模块示例时同步更新该表。

---

## 备份仓库同步流程

> 备份仓库 = 主仓库 `references/` 的只读镜像 + 旧版归档（v1/v2/v3/v4 目录）。主仓库是唯一创作源，archive 只做同步。

```bash
# 1. 克隆两个仓库（若本地没有）
git clone git@github.com:yehuoshun/slay-the-spire-2-mod-skill.git
cd slay-the-spire-2-mod-skill-archive
# 2. 整目录同步 references（缺的复制、内容变化的覆盖；⚠️ 必须排除 v*/ 归档目录，否则 --delete 会误删 v1-v4 旧版，历史教训：9dbbda8 误删）
rsync -a --delete --exclude='v*/' ../slay-the-spire-2-mod-skill/references/ references/
# 3. 流程文档同步（SKILL.md + LEARN.md + LEARNED.md，参考资料表/流程变化要跟上；LEARN.md 含同步流程本身，必须同步否则 archive 里是旧流程）
cp ../slay-the-spire-2-mod-skill/SKILL.md ../slay-the-spire-2-mod-skill/LEARN.md ../slay-the-spire-2-mod-skill/LEARNED.md .
# 4. 不动 README.md —— archive 版是定制存档说明，不能覆盖
# 5. commit + push
git add -A
git commit -m "feat: 同步主仓库 <commit> — <改动概要>"
git push origin main
```

### 注意

- archive 的 `references/<模块>/v1|v2|v3|v4/` 是旧版本归档，**保留不动**（`rsync --delete` 会删多余文件，但 v1-v4 与主仓库不重复，不受影响；主仓库无对应目录时删掉 archive 中已废弃的模块文件即可）
- **大改前先留档（硬规则）**：模块内容结构性大改（API 校正 / 方案转正 / 文件拆分）前，先把 archive 中该模块当前版复制为 `v<N+1>`（全库统一递增：当前 v4 → 下次 v5），再同步新内容；小改动靠 git 提交 diff 记录即可（“备份旧的”二选一：vX 留档 或 git diff）
- 主仓库 push 后**必须**同步 archive，两个仓库才算完事（历次都是先主仓后备份）
- README.md / CHANGELOG.md 只在 archive 维护，主仓库的版本不同，禁止互相覆盖
- 沙箱无 rsync 时用 python 脚本对比复制（缺的复制、差异覆盖）

## 重要

- 当前学的方案可能不是最优解，后面学到更好的要在 reference 末尾加「演进路线」说明
- 不要为了追求完美跳过当前学习，先学再优化

---