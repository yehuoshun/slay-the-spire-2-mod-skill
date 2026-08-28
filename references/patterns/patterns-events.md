# 实战写法模式：事件

## 事件

### 单页事件

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return new List<EventOption>
    {
        new EventOption(this, OnOptionA, "MY_EVENT.pages.INITIAL.options.A"),
        new EventOption(this, OnOptionB, "MY_EVENT.pages.INITIAL.options.B"),
    };
}

private async Task OnOptionA()
{
    SetEventFinished(L10NLookup("MY_EVENT.pages.A.description"));
}
```

### 多页事件

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return new List<EventOption>
    {
        new EventOption(this, OnEnter, "MY_EVENT.pages.INITIAL.options.ENTER"),
    };
}

private async Task OnEnter()
{
    SetEventState(
        L10NLookup("MY_EVENT.pages.PAGE2.description"),
        new List<EventOption>
        {
            new EventOption(this, OnLeave, "MY_EVENT.pages.PAGE2.options.LEAVE"),
        });
}
```

### 事件给卡 / 金币

```csharp
private async Task OnGetCard()
{
    var card = Owner!.RunState.CreateCard<MyCustomCard>(Owner);
    CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    SetEventFinished(L10NLookup("MY_EVENT.pages.GET_CARD.description"));
}

private async Task OnGetGold()
{
    await PlayerCmd.GainGold(50, Owner!);
    SetEventFinished(L10NLookup("MY_EVENT.pages.GET_GOLD.description"));
}
```

