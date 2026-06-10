# 项目构建 & 部署

---

## 完整 .csproj 模板

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <RootNamespace>MyMod</RootNamespace>
    <AssemblyName>MyMod</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="0Harmony">
      <HintPath>../libs/0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="sts2">
      <HintPath>../libs/sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- 构建后复制 DLL 到 build 目录 -->
  <Target Name="CopyDllToBuild" AfterTargets="Build">
    <Copy SourceFiles="$(OutputPath)$(AssemblyName).dll"
          DestinationFolder="../build/" />
  </Target>
</Project>
```

**关键点**：
- `EnableDynamicLoading>true` — 模组 DLL 必须动态加载
- `Private>false` — 不复制依赖 DLL 到输出（游戏本体已提供）
- `CopyLocalLockFileAssemblies>true` — NuGet 包依赖需要复制

---

## 构建脚本（Linux/macOS）

```bash
#!/bin/bash
# tools/build_and_deploy.sh

MOD_ID="MyMod"
GAME_MODS_DIR="$HOME/.local/share/Steam/steamapps/common/SlayTheSpire2/mods"
MEGADOT_DIR="$HOME/Megadot"

echo "=== 1. 编译 DLL ==="
dotnet build src/${MOD_ID}.csproj -c Release

echo "=== 2. 打包 PCK ==="
cd "$MEGADOT_DIR"
./Megadot --headless --path "../${MOD_ID}" --export-pack "Windows" "../${MOD_ID}/build/${MOD_ID}.pck"

echo "=== 3. 部署到游戏 mods/ ==="
cp build/${MOD_ID}.dll "$GAME_MODS_DIR/"
cp build/${MOD_ID}.pck "$GAME_MODS_DIR/"
cp assets/${MOD_ID}.json "$GAME_MODS_DIR/"

echo "=== 完成 ==="
```

---

## 打包 PCK（Megadot GUI）

1. 打开 Megadot → 打开项目
2. 菜单：项目 → 导出
3. 添加 → Windows Desktop
4. 点击「导出 PCK/ZIP」
5. 保存到 `build/<模组ID>.pck`
6. 取消勾选「使用调试导出」和「导出为补丁」

---

## 本地化 JSON 完整格式

### cards.json

```json
{
  "STRIKE_CARD": {
    "title": "打击",
    "description": "造成 {Damage:diff()} 点伤害。"
  }
}
```

### relics.json

```json
{
  "ENERGY_RELIC": {
    "title": "能量遗物",
    "description": "每回合开始时获得 1 点能量。",
    "flavor": "它散发着微弱的光芒。"
  }
}
```

### powers.json

```json
{
  "THORNS_POWER": {
    "title": "荆棘",
    "description": "受到攻击时，对攻击者造成 {Amount} 点伤害。",
    "smartDescription": "受到攻击时，对攻击者造成 {Amount:diff()} 点伤害。"
  }
}
```

### potions.json

```json
{
  "DAMAGE_POTION": {
    "title": "伤害药水",
    "description": "对所有敌人造成 20 点伤害。"
  }
}
```

### enchantments.json

```json
{
  "SHARP_ENCHANTMENT": {
    "title": "锋利",
    "description": "增加 {Amount} 点伤害和 {Amount} 点格挡。"
  }
}
```

### events.json

```json
{
  "MY_CUSTOM_EVENT": {
    "title": "神秘事件",
    "pages": {
      "INITIAL": {
        "description": "你遇到了一个神秘商人...",
        "options": {
          "OPTION_1": {
            "title": "[交易] 获得一张卡牌"
          },
          "OPTION_2": {
            "title": "[离开] 获得 50 金币"
          }
        }
      },
      "RESULT": {
        "description": "你获得了一张强力卡牌！"
      },
      "RESULT2": {
        "description": "你带着金币离开了。"
      }
    }
  }
}
```

### monsters.json

```json
{
  "GOBLIN_MONSTER": {
    "name": "哥布林",
    "moves": {
      "attack": {
        "title": "攻击"
      },
      "defend": {
        "title": "防御"
      }
    }
  }
}
```

### encounters.json

```json
{
  "GOBLIN_ENCOUNTER": {
    "title": "哥布林遭遇"
  }
}
```

### ancients.json

```json
{
  "MY_ANCIENT": {
    "title": "神秘先古之民",
    "epithet": "远古守护者",
    "talk": {
      "firstVisitEver": {
        "0-0": "你好，旅行者...",
        "0-1": "欢迎来到我的领域。"
      }
    }
  }
}
```

### characters.json

```json
{
  "WATCHER_CHARACTER": {
    "title": "观者",
    "description": "一个来自远方的神秘角色。"
  }
}
```

---

## 动态变量格式化

| 格式 | 效果 |
|------|------|
| `{Damage}` | 显示当前值 |
| `{Damage:diff()}` | 升级后显示变化（如 6→9） |
| `{Damage:upgrade()}` | 仅显示升级后的值 |

---

## 模组清单完整字段

```json
{
  "id": "MyMod",
  "name": "我的模组",
  "version": "1.0.0",
  "author": "作者名",
  "description": "模组描述",
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": true,
  "dependencies": []
}
```

| 字段 | 说明 |
|------|------|
| `id` | 唯一标识，与文件名一致 |
| `has_pck` | 是否有资源包（图片/本地化等） |
| `has_dll` | 是否有代码 |
| `affects_gameplay` | 是否影响游戏玩法（联机校验用） |
| `dependencies` | 依赖的其他模组 ID 列表 |

---

## 图标资源路径规范

### 卡牌

| 资源 | 路径 |
|------|------|
| 卡面大图 | `images/cards/<卡牌ID小写>.png` |
| 裁切纹理 | `images/atlases/card_atlas.sprites/<卡池名>/<卡牌ID小写>.tres` |

### 遗物

| 资源 | 路径 |
|------|------|
| 大图标 | `images/relics/<遗物ID小写>.png` |
| 裁切纹理 | `images/atlases/relic_atlas.sprites/<遗物ID小写>.tres` |
| 描边裁切 | `images/atlases/relic_outline_atlas.sprites/<遗物ID小写>.tres` |

### 能力

| 资源 | 路径 |
|------|------|
| 大图 | `images/powers/<能力ID小写>.png` |
| 裁切纹理 | `images/atlases/power_atlas.sprites/<能力ID小写>.tres` |

### 药水

| 资源 | 路径 |
|------|------|
| 大图 | `images/potions/<药水ID小写>.png` |
| 裁切纹理 | `images/atlases/potion_atlas.sprites/<药水ID小写>.tres` |
| 描边裁切 | `images/atlases/potion_outline_atlas.sprites/<药水ID小写>.tres` |

### 事件

| 资源 | 路径 |
|------|------|
| 背景图 | `images/events/<事件ID小写>.png`（推荐 3440×1613） |

### 附魔

| 资源 | 路径 |
|------|------|
| 图标 | `images/enchantments/<附魔ID小写>.png` |

---

## AtlasTexture 创建步骤

1. 在 Megadot 中右键文件夹 → 新建 → 资源
2. 搜索 `AtlasTexture` → 创建
3. 命名为 `<小写ID>.tres`
4. 双击打开 → 属性检查器 → Atlas 拖入 PNG
5. 设置 Region 的 w/h 为图片尺寸（通常 256×256）

---

## 部署检查清单

- [ ] `build/<模组ID>.dll` 存在
- [ ] `build/<模组ID>.pck` 存在（如有资源）
- [ ] `build/<模组ID>.json` 存在
- [ ] JSON 中 `id` 与文件名一致
- [ ] `has_dll` 和 `has_pck` 与实际一致
- [ ] 三个文件复制到游戏 `mods/` 目录
- [ ] 启动游戏，左下角显示模组加载成功