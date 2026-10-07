import os, re, json
sets = json.load(open('/home/admin/.openclaw/workspace/code/sts2-api-sets.json'))
classes, methods = set(sets['classes']), set(sets['methods'])
SKIP = {'Godot','Input','CanvasLayer','Sprite2D','SceneTree','Image','ImageTexture','DisplayServer','Engine','GD','GodotObject','FileAccess','Time','OS','Root','Console','Path','System','String','Math','Mathf','Random','Convert','Activator','AppDomain','Assembly','Type','Task','List','Dictionary','IEnumerable','IReadOnlyList','HashSet','Func','Action','Vector2','Vector3','Color','Texture2D','Material','Control','Node','Node2D','Label','PackedScene','HoverTip','HoverTipFactory','AccessTools','Harmony','HarmonyMethod','HarmonyPatch','MethodInfo','MethodBase','FieldInfo','PropertyInfo','Exception','ArgumentNullException','JsonSerializer','File','Directory','Callable','Rng','PCKPacker','Json','Variant','Callback','Logger','LocString','DynamicVar','DamageVar','BlockVar','PowerVar','ValueProp','CardTag','CardKeyword','PowerType','PowerStackType','TargetType','CardType','CardRarity','RelicRarity','CharacterGender','CombatSide','SerializationCondition','PlayerChoiceContext','CardPlay','Creature','Player','CardModel','RelicModel','PowerModel','MonsterModel','CharacterModel','ModelId','SceneHelper','ImageHelper','ModelDb','ModHelper','PowerCmd','CreatureCmd','PlayerCmd','DamageCmd','CardCmd','CardSelectCmd','SfxCmd','NGame','NCard','NRelic','NPower','NCreatureVisuals','NCharacterSelectButton','NCharacterSelectScreen','NMapMarker','NEnergyCounter','NCombatRoom','NCardLibrary','NCursorManager','NHoverTipSet','EnergyIconsFormatter','ProgressSaveManager','RunState','AncientDialogueSet','AncientDialogue','SavedPropertiesTypeCache','BlockingPlayerChoiceContext','ThrowingPlayerChoiceContext','ICombatState','ITemporaryPower','Interop','Sts2ModExamples','Example','Illaoi','My','ModConfig','NModConfigSubmenu','SubmenuStack','ModPatcher','ContentRegistry','PoolAttributes','EnumInjector','NGameOverScreen','GodotSharp','MegaCrit'}
call_re = re.compile(r'\b([A-Z]\w+)\.(\w+)\s*\(')
block_re = re.compile(r'```(?:csharp|cs)?\n(.*?)```', re.S)
suspects = []
for root, _, files in os.walk('references'):
    for f in files:
        if not f.endswith('.md'): continue
        txt = open(os.path.join(root, f), encoding='utf-8').read()
        for block in block_re.findall(txt):
            for m in call_re.finditer(block):
                c, me = m.group(1), m.group(2)
                if c in SKIP or c.startswith(('My','Example','Illaoi','Test','Mod')) or me.startswith('_'): continue
                if c in classes and me not in methods:
                    suspects.append(f'{c}.{me} <- {os.path.basename(f)}')
print(len(suspects))
for s in sorted(set(suspects)): print(' ', s)
