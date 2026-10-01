# 手牌布局修正（HandPosHelper）

> 从 pile-hand-size.md 拆出。手牌超过 10 张时，原生布局会溢出，用 Prefix 修正位置/角度/缩放。

```csharp
[HarmonyPatch(typeof(HandPosHelper), nameof(HandPosHelper.GetPosition))]
public static class HandPosFix
{
    [HarmonyPrefix]
    static bool Prefix(int handSize, int cardIndex, ref Vector2 __result)
    {
        if (handSize <= 10) return true;
        var halfSpread = Mathf.Lerp(610f, 690f,
            Mathf.Clamp((handSize - 10) / 4f, 0f, 1f));
        var u = (2f * cardIndex / (handSize - 1f)) - 1f;
        __result = new Vector2(halfSpread * u,
            Math.Min(18f, -64f + (88f - (handSize - 10) * 1.5f) * u * u));
        return false;
    }
}
```

> 配合手牌上限修改（[pile-hand-size.md](pile-hand-size.md)）一起注册。`return true` = 手牌 ≤10 走原生布局，`return false` = 超限用自定义位置。
