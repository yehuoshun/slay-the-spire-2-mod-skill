# cardgen 文档核对探针

对**已安装的 AutoAnthony.dll** 做反射 dump，把 20 个公开 API 面的 `ApiVersion` 与全部静态公开成员签名机械化列出来，供人工或 diff 对照 `references/cardgen/*.md`。

## 为什么需要它

`LEARN.md`「测试与验证流程」规定：文档里的 API 必须过 `sts2-mod-examples` 的真编译验证。这条对**主力模块**成立，对 cardgen 不成立：

- `sts2-mod-examples` 的 `deps/` 只有 `sts2.dll` + `0Harmony.dll`，CI 从 `yehuoshun/STS2-ShunMod` 的 release 下载这两颗，**拿不到 AutoAnthony**（第三方工坊 mod，无公开分发渠道）。
- 覆盖矩阵 24 个模块里没有 cardgen（`SKILL.md` 把 cardgen 定为「⚠️ 生态参考，非主路线」）。

所以「在示例仓库补一个 cardgen 示例」编译不了，`dotnet build` 全绿也证明不了 AutoAnthony 的任何签名——**空验证**。cardgen 的等价验收是：对着真 DLL 跑本探针，把输出与文档逐条对。

## 用法

```powershell
cd scripts/cardgen-probe
dotnet run -c Release -- --out dump.txt
# 再和快照比
git diff --no-index snapshot-0.3.165.txt dump.txt
```

路径默认取本地 Steam 安装位置，可用参数或环境变量覆盖：

| 目标 | 参数 | 环境变量 |
|---|---|---|
| 游戏 DLL 目录 | `--game <dir>` | `STS2_DATA_DIR` |
| AutoAnthony 目录 | `--anthony <dir>` | `AUTOANTHONY_DIR` |

找不到 `AutoAnthony.dll` 时退出码 `2`（CI 里就是这个结果，属预期，不代表失败）。

## 快照

`snapshot-0.3.165.txt` = 2026-11-20 对工坊包 `3786611028`（`version 0.3.165`，7,349,248 字节）的实测输出，205 行。
上游 bump 版本后重跑一次、diff，就能一眼看出哪些 API 面涨了版本、哪些成员增删——比重新读 wiki 可靠。

## 已知边界

- 只列 **public static** 面（`ApiVersion` 是 `const int`，出现在 `## ApiVersion matrix`）。实例成员、internal、非公开面不在内。
- 不执行 mod 代码：只 `AssemblyLoadContext.LoadFromAssemblyPath` + 枚举元数据，不触发初始化。
- `CardNameGenerator` 是唯一没有 `ApiVersion` 的老接口（`RegisterExternalParts`，0.3.32 起），矩阵里显示 `none`。