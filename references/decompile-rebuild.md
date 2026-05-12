# 反编译重构建流程

基于 dnSpy/ILSpy + Harmony 的开发工作流。

## 工具

| 工具 | 用途 |
|------|------|
| dnSpy / ILSpy | 反编译 `sts2.dll`，查看游戏源码 |
| Harmony AccessTools | 反射访问私有/内部成员 |
| RuntimeTypeResolver | 运行时查找类型（处理 DLL 重命名） |

## 标准流程

```
1. dnSpy 打开 sts2.dll
2. Ctrl+F 搜索目标类/方法（如 "Enchant"、"CardModel"）
3. 右键 → Analyze 看调用链
4. 记录方法签名 + 参数类型
5. 确认是 public/internal（可 Patch）还是 compiler-generated（不可）
6. 确定 Patch 策略（Postfix/Prefix/Transpiler）
7. 编写补丁 → dotnet build → 测试
```

## 实战：找附魔系统

```
搜索 "Enchant"：
  CardCmd.Enchant(EnchantmentModel, CardModel, decimal)
  EnchantmentModel.CanEnchant(CardModel) : bool
  CardModel.Enchantment : EnchantmentModel（属性，单槽位）
  CardCmd.ClearEnchantment(CardModel)

分析 Enchant 方法逻辑：
  if (card.Enchantment == null)       ← 首次附魔
  else if (类型不同) throw             ← 类型冲突
  else Amount +=                      ← 同类叠层

Patch 决策：
  方案A: Postfix CanEnchant → 永远 true
  方案B: Prefix Enchant → 跳过类型检查
  方案C: Transpiler → 删除 throw 指令
```

## 构建

```bash
# STS2Plus 方式
dotnet build -c Release -p:STS2GameDir="D:\Steam\...\Slay the Spire 2"

# STS2-ShunMod 方式  
dotnet build STS2-ShunMod.csproj -c Release -p:Sts2DataDir="$(realpath deps)"
```

## 常见陷阱

| 陷阱 | 处理 |
|------|------|
| 反编译代码有编译错误 | 只用作 API 参考，不直接复制 |
| 方法有多重重载 | Harmony 显式指定参数类型 |
| GodotPlugins.dll 有同名类型 | RuntimeTypeResolver 动态查找 |
| async 方法 | AsyncTaskMethodBuilder，Patch 需特殊处理 |
| compiler-generated struct | 找原始方法，不 Patch 生成的 |
