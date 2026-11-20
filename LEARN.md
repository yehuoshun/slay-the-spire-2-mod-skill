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
   - 超过 4000 字符时按章节拆分为 `xx-*.md` 子文件，主文件保留导航表（字符数按 `wc -m` 统计，含空格换行；中文 1 字 = 1 字符，勿用 `wc -c` 字节数误判）
4. **标注演进路线** — 当前方案如果还有更优方案（如硬注册 → 一键注册），在末尾用 ## 演进路线 注明，方便后期替换
5. **更新 SKILL.md** — 参考资料表加新行
6. **更新 README.md** — 状态列表勾选 + 鸣谢追加
7. **更新** — 新学内容直接写入 `references/<模块>/<模块>.md` 或对应子文件，审查通过后更新导航页为最新版
8. **commit + push**
9. **测试验证** — 文档改动必须过 [sts2-mod-examples](https://github.com/yehuoshun/sts2-mod-examples) 的编译验证；**cardgen 例外**，本地装了 AutoAnthony 才参与编译，CI 上靠反射探针兜底（见下「测试与验证流程」例外段）
10. **同步备份仓库** — 主仓库 commit 后，把 `references/` 增量同步到 [slay-the-spire-2-mod-skill-archive](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive)（见下）

---

## 测试与验证流程

> **闭环**：skill 文档（唯一创作源）→ sts2-mod-examples 可编译示例 → CI 真编译 → 报错回流修文档。
> 文档里的任何 API/签名，示例仓库编译不过 = 文档有问题（2026-10-02 起强制执行，当天靠此抓到 10+ 处编造 API）。

1. **改文档** — 新增/修改 references 模块内容
2. **同步示例** — 在 `sts2-mod-examples/Sts2ModExamplesCode/<模块>/` 补/改对应示例：
   - 新 API 必须有示例类覆盖（类型/方法逐条对照 `sts2-res/src/`）
   - 修改的 API 更新示例调用
3. **本地静态自检** — `python3 scripts/doc-api-check.py`（API 白名单，防幻觉）通过
4. **push → CI** — push 后 GitHub Actions 自动跑：dotnet build（真编译）+ ModAnalyzers + 白名单 + 本地化 JSON 校验
5. **CI 全绿才算完** — 编译报错 → 按错误修正示例（或修正文档）→ 重推，直到绿色
6. **示例里踩的坑必须回流** — 真编译暴露的文档 bug/签名差异，当天修进 references + SKILL.md/README 同步

> 覆盖矩阵见 sts2-mod-examples README「覆盖内容」表。新增模块示例时同步更新该表。

> **cardgen（生态参考模块）的双轨验证。** `sts2-mod-examples` 的 `deps/` 只有 `sts2.dll` + `0Harmony.dll`（CI 从 `yehuoshun/STS2-ShunMod` release 下载这两颗），拿不到 AutoAnthony（第三方工坊 mod，无公开分发渠道）——所以 cardgen 示例按**条件编译**接进示例仓库：
>
> - `Sts2ModExamplesCode/Cardgen/` 两个文件（`CardgenTinkeringFlow.cs` 覆盖 `CardTinkeringApi` v6 + `AutoAnthonyEditorApi` v2；`CardgenPackageRegistration.cs` 覆盖组件包注册七步 + 运行时/展示/关键词路由 + 外部角色 + 原生分解 + freeform 物化 + 设置/平衡监听）
> - `Sts2ModExamples.csproj`：`AutoAnthonyDir` 从实际找到游戏的 Steam 库根反推 `../../workshop/content/2868840/3786611028`（退回 `SteamLibraryPath`/`AutoSteamPath`）；找到 `AutoAnthony.dll` 就 `<Reference Include="AutoAnthony" Private="false">` 参与编译，找不到就 `<Compile Remove="Sts2ModExamplesCode/Cardgen/**/*.cs" />`
> - **CI 仍全绿**：构建日志里会打印 `AutoAnthony.dll 未找到 → 跳过 Cardgen 编译`，不会把第三方 DLL 拖进产物
> - 本机装了 mod：`dotnet build` 就是 cardgen 文档的**真编译验证**——文档写错参数名/类型/重载/大小写，这里直接编不过
> - 任何机器（含 CI）都能跑的独立验收：`scripts/cardgen-probe/`（`dotnet run -c Release -- --out dump.txt` 后与 `snapshot-<版本>.txt` diff），把 20 个 API 面与 `references/cardgen/*.md` 的签名/版本矩阵逐条对
>
> 2026-11-20 首跑探针抓到文档真错：`CardTinkeringApi.AnalyzeAtX(GeneratedCard, int resolvedX, bool balancedValues = true)` 被写成第三参 `CardTinkeringEvaluationOptions? options = null`。同批核对确认 `cardgen-api.md` 的 20 面版本矩阵与 DLL 实测**全部一致**。
>
> 2026-11-20 接上条件编译后，真编译又抓到三处只有编译器才认得的差异：① `CardTinkeringApi.Valid`（源码 `:49` 的静态只读「校验通过」结果）**没进 0.3.165 包**——源码快照 ≠ 已发布包，以已装 DLL 为准；② record 的位置参数在元数据里是 PascalCase（`GeneratedCard(Cost:, Type:, Operations:)`），普通类/结构是 camelCase（`ComponentProfileRequest(UnlockComponentRoles:)`），写反编译期报 `CS1739`；③ `AutoAnthonyEditorApi` 的命名空间是 `AutoAnthony`，不是它所在目录名 `ChaosMode`。
>
> **教训：`dotnet build` 全绿但没把 cardgen 编进去 = 空验证。** 先看构建日志里那条 `AutoAnthony.dll 已找到 / 未找到`，确认这次到底验没验；文档改动必须落在「装了 mod 的机器上编译通过」这一条上。

---

## 备份仓库同步流程

> 备份仓库 = 主仓库 `references/` 的只读镜像 + 旧版归档（v1/v2/v3/v4 目录）。主仓库是唯一创作源，archive 只做同步。**只同步 `references/` + `SKILL.md`，其余文件（LEARN.md/LEARNED.md/README.md 等）一律不同步**。

```bash
# 1. 克隆两个仓库（若本地没有）
git clone git@github.com:yehuoshun/slay-the-spire-2-mod-skill.git
cd slay-the-spire-2-mod-skill-archive
# 2. 整目录同步 references（缺的复制、内容变化的覆盖；⚠️ 必须排除 v*/ 归档目录，否则 --delete 会误删 v1-v4 旧版，历史教训：9dbbda8 误删）
rsync -a --delete --exclude='v*/' ../slay-the-spire-2-mod-skill/references/ references/
# 3. SKILL.md 同步（参考资料表/硬规则变化要跟上；LEARN.md/LEARNED.md 不同步）
cp ../slay-the-spire-2-mod-skill/SKILL.md .
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
- 沙箱无 rsync 时用 python 脚本对比复制（缺的复制、差异覆盖）→ 已固化 `scripts/sync_archive.py`（自动跳过 v*/ 归档目录）

## 重要

- 当前学的方案可能不是最优解，后面学到更好的要在 reference 末尾加「演进路线」说明
- 不要为了追求完美跳过当前学习，先学再优化

---