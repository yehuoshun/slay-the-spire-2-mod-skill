# 怪物/角色：动画状态机（CreatureAnimator + AnimState）

> 实战验证（STS2_MarisaMod 2026-10-05 学习，API 已对照 sts2-res）。游戏动画用「状态机」而非动画名直调：`CreatureAnimator` 持有主状态 + 任意触发分支，`AnimState` 定义单个动画与后继状态。

## 1. 核心类（sts2-res 验证）

```csharp
// 单个动画状态：名字 + 是否循环 + 播完去哪
var idle = new AnimState("idle_loop", isLooping: true);
var attack = new AnimState("attack");
attack.NextState = idle;                 // 攻击播完回 idle

// 状态机：主状态 + 触发分支
var animator = new CreatureAnimator(idle, controller);   // controller = MegaSprite
animator.AddAnyState("Idle", idle);
animator.AddAnyState("Attack", attack);
animator.AddAnyState("Dead", die, () => 条件);            // 第三个参数 = 条件 lambda
```

- `CreatureAnimator.AddAnyState(string trigger, AnimState state, Func<bool>? condition = null)` — 任意时刻可触发
- `AnimState.AddBranch(string trigger, AnimState state, Func<bool>? condition = null)` — 仅该状态内可触发
- 常用 trigger 名（游戏内部约定）：`Idle` / `Attack` / `Cast` / `Hit` / `Dead` / `BlockStart` / `BlockEnd` / `StunTrigger` / `WakeUpTrigger` / `AttackMulti` / `Spark` / `Relaxed`
- 同一 trigger 可注册多个 state + 条件 lambda 区分（如 Hit 时按能力分支）

## 2. 自定义角色：覆写 GenerateAnimator

```csharp
public class MyCharacter : CharacterModel
{
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle_loop", isLooping: true);
        var cast = new AnimState("cast");
        var attack = new AnimState("attack");
        var hurt = new AnimState("hurt");
        var die = new AnimState("die");
        var relaxed = new AnimState("relaxed_loop", isLooping: true);
        cast.NextState = idle; attack.NextState = idle; hurt.NextState = idle;
        relaxed.AddBranch("Idle", idle);                 // relaxed 内 Idle 触发回 idle

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }
}
```

> `GenerateAnimator` 是 `CharacterModel` 原生虚方法（sts2-res 已验证），纯原生角色直接覆写即可，动画名要与角色骨架（Spine）里的动画名一致。

## 3. 怪物：Patch GenerateAnimator 重定义

```csharp
[HarmonyPatch(typeof(PhantasmalGardener), nameof(PhantasmalGardener.GenerateAnimator))]
static class GardenerAnimatorPatch
{
    static bool Prefix(PhantasmalGardener __instance, ref CreatureAnimator __result, MegaSprite controller)
    {
        var idle = new AnimState("idle_loop", isLooping: true);
        var buff = new AnimState("buff");
        var attack = new AnimState("attack");
        var hurt = new AnimState("hurt");
        var die = new AnimState("die");
        buff.NextState = idle; attack.NextState = idle; hurt.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Cast", buff);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Hit", hurt, () => !__instance.Creature.HasPower<SkittishPower>());  // 条件分支
        animator.AddAnyState("Dead", die);
        __result = animator;
        return false;
    }
}
```

条件分支实战：同一 `Hit` 触发注册两个 state，lambda 按能力/状态切换（如「本回合有格挡 → 播放 hurt_extended」），比改动画名直调更稳。

## 4. 常见坑

| 坑 | 解法 |
|----|------|
| 动画不循环 | 常驻状态（idle/relaxed/stunned_loop）必须 `isLooping: true` |
| 播完卡死 | 每个一次性状态都要设 `NextState`（回 idle 或其它常驻态） |
| 怪物 Patch 后原动画丢 | Prefix 里必须手动 `__result = animator; return false;` 完整重建 |
| 条件分支永远不触发 | lambda 引用 `__instance` 捕获的是 Patch 实例，条件里用 `__instance.Creature` 判能力 |
| 角色动画对不上 | 动画名必须匹配 Spine 骨架实际动画名（spine_godot_extension 导入） |
