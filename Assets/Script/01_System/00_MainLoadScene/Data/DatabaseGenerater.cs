using Definitions;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
public static class DatabaseGenerater
{
    [Tooltip("CSV파일을 선택해서 메뉴 클릭")]

    [MenuItem("Assets/Create/Game Data/Convert CSV to ScriptableObject", false, 1)]

    public static void ConvertSelectedCSV()
    {
        Object[] selectedObjects = Selection.objects;

        foreach (Object obj in selectedObjects)
        {
            TextAsset csvFile = obj as TextAsset;
            if (csvFile == null) continue;

            string directory = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(csvFile));
            string normalizedPath = directory.Replace('\\', '/');
            string soPath = normalizedPath + "/Database/" + csvFile.name + "Database.asset";
            if (csvFile.name.Contains(Def_CSV.ITEMS))
            {
                ParseItemDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.QUESTS))
            {
                ParseQuestDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.DIALOGUES))
            {
                ParseDialogDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.GATHERABLES))
            {
                ParseGatherableDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.NPCINFO))
            {
                ParseNPCInfoDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.MONSTER))
            {
                ParseMonsterDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.MAPINFO))
            {
                ParseMapInfoDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.SHOP))
            {
                ParseShopDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.DUNGEONS))
            {
                ParseDungeonsDataCSV(csvFile.text, soPath);
            }
            else if (csvFile.name.Contains(Def_CSV.Skill))
            {
                ParseSkillDataCSV(csvFile.text, soPath);
            }
            else
            {
                Debug.LogWarning($"[DatabaseGenerater] 알 수 없는 CSV 파일: {csvFile.name}");
            }

        }

    }

    //Dialogue 데이터 파싱
    private static void ParseDialogDataCSV(string csvText, string soPath)
    {
        DialogueSequenceSO database = AssetDatabase.LoadAssetAtPath<DialogueSequenceSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<DialogueSequenceSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();
        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;
        DialogueSequence currentSequence = null;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            //  CSV 구조: npcId, DialogueType, Questid, Text
            var parts = SplitCSVLine(raw);
            if (parts.Count < 4) continue;

            string npcId = parts[0].Trim();
            string dialogueType = parts[1].Trim();
            string questId = parts[2].Trim();
            string text = parts[3].Trim();

            // 새로운 시퀀스 시작
            if (!string.IsNullOrEmpty(npcId) && !string.IsNullOrEmpty(dialogueType))
            {
                currentSequence = new DialogueSequence
                {
                    npcId = npcId,
                    dialogueType = dialogueType,
                    questId = questId
                };
                database.Items.Add(currentSequence);
            }

            // 대사 추가 (text가 비어있지 않으면)
            //  Speaker 정보는 저장하지 않음 - 런타임에 npcId로 조회 
            if (currentSequence != null && !string.IsNullOrEmpty(text))
            {
                currentSequence.lines.Add(new DialogueLine
                {
                    Text = text
                });
            }
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }

    //gatherable 데이터 파싱
    private static void ParseGatherableDataCSV(string csvText, string soPath)
    {
        GatherableDataSO database = AssetDatabase.LoadAssetAtPath<GatherableDataSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<GatherableDataSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();
        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);

            // CSV 구조: id,이름,설명,채집도구,채집속도,보상아이템테이블
            if (parts.Count < 6) continue;

            GatherableData gatherable = new()
            {
                gatherableid = parts[0].Trim(),
                gatherableName = parts[1].Trim(),
                description = parts[2].Trim(),
                gatherType = ParseGatherType(parts[3].Trim()),
                requiredTool = ParseGatherTool(parts[4].Trim()),
                gatherTime = ParseFloat(parts[5].Trim(), 1.0f)
            };

            // 보상 아이템 테이블 파싱
            if (parts.Count > 6 && !string.IsNullOrWhiteSpace(parts[6]))
            {
                gatherable.dropItems = new List<ItemReward>();
                string rewardsStr = parts[6].Trim();
                if (!string.IsNullOrEmpty(rewardsStr))
                {
                    var rewards = rewardsStr.Split(';');
                    foreach (var r in rewards)
                    {
                        gatherable.dropItems.Add(new ItemReward(r));
                    }
                }
            }
            database.Items.Add(gatherable);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }

    //item 데이터 파싱
    private static void ParseItemDataCSV(string csvText, string soPath)
    {
        ItemDataSO database = AssetDatabase.LoadAssetAtPath<ItemDataSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemDataSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();

        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);

            //  CSV 구조 (변경됨):
            // itemID,itemName,itemType,description,maxStack,buyPrice,sellPrice,iconPath,
            // disposable,consumableEffect,equipSlot,attackBonus,defenseBonus,hpBonus,
            // strBonus,dexBonus,intBonus,lukBonus,tecBonus
            if (parts.Count < 9) continue;

            string itemId = parts[0].Trim();
            string itemName = parts[1].Trim();
            string itemTypeStr = parts[2].Trim();
            string description = parts[3].Trim();
            string maxStackStr = parts[4].Trim();
            string buyPriceStr = parts[5].Trim();
            string sellPriceStr = parts[6].Trim();
            string iconPath = parts[7].Trim();
            string disposableStr = parts[8].Trim();

            ItemData item = new()
            {
                itemId = itemId,
                itemName = itemName,
                itemType = ParseItemType(itemTypeStr),
                description = description,
                maxStack = ParseInt(maxStackStr),
                buyPrice = ParseInt(buyPriceStr),
                sellPrice = ParseInt(sellPriceStr),
                iconPath = iconPath,
                disposable = ParseBool(disposableStr)
            };

            //  소비 아이템 데이터 (9번째 인덱스: consumableEffect)
            if (parts.Count > 9)
            {
                item.consumableEffect = parts[9].Trim();
            }

            // 장비 데이터 (10번째 이후)
            if (parts.Count > 10 && !string.IsNullOrEmpty(parts[10].Trim()))
            {
                item.equipSlot = ParseEquipSlot(parts[10].Trim());
            }
            if (parts.Count > 11)
            {
                int.TryParse(parts[11].Trim(), out item.attackBonus);
            }
            if (parts.Count > 12)
            {
                int.TryParse(parts[12].Trim(), out item.defenseBonus);
            }
            if (parts.Count > 13)
            {
                int.TryParse(parts[13].Trim(), out item.hpBonus);
            }
            if (parts.Count > 14)
            {
                int.TryParse(parts[14].Trim(), out item.strBonus);
            }
            if (parts.Count > 15)
            {
                int.TryParse(parts[15].Trim(), out item.dexBonus);
            }
            if (parts.Count > 16)
            {
                int.TryParse(parts[16].Trim(), out item.intBonus);
            }
            if (parts.Count > 17)
            {
                int.TryParse(parts[17].Trim(), out item.lukBonus);
            }
            if (parts.Count > 18)
            {
                int.TryParse(parts[18].Trim(), out item.tecBonus);
            }
            if (parts.Count > 19)
            {
                item.isCosmetic = ParseBool(parts[19].Trim());
            }

            database.Items.Add(item);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }

    //quest 데이터 파싱
    public static void ParseQuestDataCSV(string csvText, string soPath)
    {
        QuestDataSO database = AssetDatabase.LoadAssetAtPath<QuestDataSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<QuestDataSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();
        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);

            // CSV 구조: QuestID,QuestName,Description,PrerequisiteType,PrerequisiteValue,
            //          PreAcceptReward,QuestHint,Objectives,Rewards,RewardExp,RewardGold
            if (parts.Count < 11) continue;

            QuestData quest = new()
            {
                questId = parts[0].Trim(),
                questName = parts[1].Trim(),
                description = parts[2].Trim().Replace("\\n", "\n"),
                rewardExp = ParseInt(parts[9].Trim(), 0),
                rewardGold = ParseInt(parts[10].Trim(), 0)
            };

            // 선행 조건 파싱
            string prereqType = parts[3].Trim();
            string prereqValue = parts[4].Trim();

            if (!string.IsNullOrEmpty(prereqType) && prereqType != "None")
            {
                if (System.Enum.TryParse(prereqType, out PrerequisiteType pType))
                {
                    quest.prerequisite.type = pType;
                    quest.prerequisite.value = prereqValue;
                }
            }

            // 수락 보상 파싱 (PreAcceptReward)
            // 형식: item:itemid;item:itemid
            string preAcceptRewardStr = parts[5].Trim();
            quest.preAcceptReward = preAcceptRewardStr;

            // 퀘스트 힌트 파싱 (QuestHint)
            // 형식: npc:npcid,item:itemid;item:itemid
            string questHintStr = parts[6].Trim();
            quest.questHint = questHintStr;

            // Objectives 파싱
            quest.objectives = new List<QuestObjective>();
            string objectivesStr = parts[7].Trim();
            if (!string.IsNullOrEmpty(objectivesStr))
            {
                var objectives = objectivesStr.Split(';');
                foreach (var obj in objectives)
                {
                    var seg = obj.Split(':');
                    if (seg.Length < 3) continue;

                    if (System.Enum.TryParse(seg[0].Trim(), out QuestType qType))
                    {
                        quest.objectives.Add(new QuestObjective
                        {
                            type = qType,
                            targetId = seg[1].Trim(),
                            requiredCount = int.Parse(seg[2].Trim()),
                            currentCount = 0
                        });
                    }
                }
            }

            // Reward Items 파싱
            quest.rewards = new List<ItemReward>();
            string rewardsStr = parts[8].Trim();
            if (!string.IsNullOrEmpty(rewardsStr))
            {
                var rewards = rewardsStr.Split(';');
                foreach (var r in rewards)
                {
                    quest.rewards.Add(new ItemReward(r));
                }
            }
            database.Items.Add(quest);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }
    //npcinfo 데이터 파싱
    private static void ParseNPCInfoDataCSV(string csvText, string soPath)
    {
        NPCInfoSO database = AssetDatabase.LoadAssetAtPath<NPCInfoSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<NPCInfoSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();
        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);
            if (parts.Count < 5) continue;

            string npcId = parts[0].Trim();
            if (string.IsNullOrEmpty(npcId)) continue;
            // CSV 구조: npcId    npcName npcTitle    npcDescription  mapId
            Npcs info = new()
            {
                npcId = npcId,
                npcName = parts[1].Trim(),
                npcTitle = parts[2].Trim(),
                npcDescription = parts[3].Trim(),
                mapId = parts[4].Trim()

            };
            database.Items.Add(info);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }
    //monster 데이터 파싱
    private static void ParseMonsterDataCSV(string csvText, string soPath)
    {
        MonsterDataSO database = AssetDatabase.LoadAssetAtPath<MonsterDataSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<MonsterDataSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();
        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);

            // CSV 구조: ID,Name,Description,Level,MonsterType,IsAggressive,IsRanged,AttackSpeed,MoveSpeed,
            //          DetectionRange,strBonus,DexBonus,IntBonus,lukBonus,tecBonus,MaxHealth,AttackPower,
            //          Defense,CriticalRate,CriticalDamage,EvasionRate,Accuracy,DropExp,DropGold,DropItemTable
            if (parts.Count < 18) continue;

            MonsterData monster = new()
            {
                monsterid = parts[0].Trim(),
                monsterName = parts[1].Trim(),
                description = parts[2].Trim(),
                level = ParseInt(parts[3].Trim(), 1),
                monsterType = ParseMonsterType(parts[4].Trim()),
                isAggressive = ParseBool(parts[5].Trim()),
                isRanged = ParseBool(parts[6].Trim()),
                attackSpeed = ParseFloat(parts[7].Trim(), 1.0f),
                moveSpeed = ParseFloat(parts[8].Trim(), 1.0f),
                detectionRange = ParseFloat(parts[9].Trim(), 0f),
                // 간결한 이름의 스탯 보너스
                strBonus = ParseInt(parts[10].Trim(), 0),
                dexBonus = ParseInt(parts[11].Trim(), 0),
                intBonus = ParseInt(parts[12].Trim(), 0),
                lukBonus = ParseInt(parts[13].Trim(), 0),
                tecBonus = ParseInt(parts[14].Trim(), 0),
                dropExp = ParseInt(parts[15].Trim(), 0),
                dropGold = ParseInt(parts[16].Trim(), 0)
            };

            // 드롭 아이템 테이블 파싱
            if (parts.Count > 17 && !string.IsNullOrWhiteSpace(parts[17]))
            {
                monster.dropItems = new List<ItemReward>();
                string rewardsStr = parts[17].Trim();
                if (!string.IsNullOrEmpty(rewardsStr))
                {
                    var rewards = rewardsStr.Split(';');
                    foreach (var r in rewards)
                    {
                        monster.dropItems.Add(new ItemReward(r));
                    }
                }
            }
            database.Items.Add(monster);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }
    //mapinfo 데이터 파싱
    private static void ParseMapInfoDataCSV(string csvText, string soPath)
    {
        MapInfoSO database = AssetDatabase.LoadAssetAtPath<MapInfoSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<MapInfoSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();
        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);
            if (parts.Count < 5) continue;

            string mapId = parts[0].Trim();
            if (string.IsNullOrEmpty(mapId)) continue;
            // CSV 구조: mapId    mapName mapType mapRecommendedLevel parentMapId
            Maps info = new()
            {
                mapId = mapId,
                mapName = parts[1].Trim(),
                mapType = parts[2].Trim().ToLower().FirstCharacterToUpper(),
                mapRecommendedLevel = parts[3].Trim(),
                parentMapId = parts[4].Trim()
            };
            database.Items.Add(info);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }
    //shop 데이터 파싱
    private static void ParseShopDataCSV(string csvText, string soPath)
    {
        ShopDataSO database = AssetDatabase.LoadAssetAtPath<ShopDataSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ShopDataSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();

        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);

            // CSV 구조: Shopid, Items
            if (parts.Count < 2) continue;

            string shopId = parts[0].Trim();
            ShopData shopData = new ShopData(shopId);

            // Items 파싱 (세미콜론으로 구분)
            string itemsStr = parts[1].Trim();
            if (!string.IsNullOrEmpty(itemsStr))
            {
                var itemsArray = itemsStr.Split(';');

                foreach (string itemEntry in itemsArray)
                {
                    string trimmedEntry = itemEntry.Trim();
                    if (string.IsNullOrEmpty(trimmedEntry)) continue;

                    // 콜론으로 구분하여 아이템id와 제한수량 분리
                    if (trimmedEntry.Contains(':'))
                    {
                        var itemParts = trimmedEntry.Split(':');
                        string itemId = itemParts[0].Trim();
                        int limitedStock = ParseInt(itemParts[1].Trim(), -1);
                        shopData.items.Add(new ShopItemData(itemId, limitedStock));
                    }
                    else
                    {
                        // 제한 수량이 없는 경우 (-1은 무제한)
                        shopData.items.Add(new ShopItemData(trimmedEntry, -1));
                    }
                }
            }

            database.Items.Add(shopData);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }

    //dungeon 데이터 파싱
    private static void ParseDungeonsDataCSV(string csvText, string soPath)
    {
        DungeonsDataSO database = AssetDatabase.LoadAssetAtPath<DungeonsDataSO>(soPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<DungeonsDataSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();

        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;

        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);

            // CSV 구조: DungeonID,DungeonName,Description,DungeonImagePath,EntryMapID,RecommendLevel,TimeRestriction,ClearRewardTable
            if (parts.Count < 8) continue;

            DungeonData dungeon = new DungeonData
            {
                dungeonId = parts[0].Trim(),
                dungeonName = parts[1].Trim(),
                description = parts[2].Trim(),
                dungeonImagePath = parts[3].Trim(),
                entryMapId = parts[4].Trim(),
                recommendLevel = ParseInt(parts[5].Trim(), 1),
                timeRestriction = ParseTimeRestriction(parts[6].Trim())
            };

            // 보상 아이템 테이블 파싱 (itemId:quantity:dropRate;itemId:quantity:dropRate)
            if (parts.Count > 7 && !string.IsNullOrWhiteSpace(parts[7]))
            {
                dungeon.clearRewardItems = new List<ItemReward>();
                string rewardsStr = parts[7].Trim();
                if (!string.IsNullOrEmpty(rewardsStr))
                {
                    var rewards = rewardsStr.Split(';');
                    foreach (var r in rewards)
                    {
                        dungeon.clearRewardItems.Add(new ItemReward(r));
                    }
                }
            }

            database.Items.Add(dungeon);
        }

        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }

    //skill 데이터 파싱
    private static void ParseSkillDataCSV(string csvText, string soPath)
    {
        SkillDataSO database = AssetDatabase.LoadAssetAtPath<SkillDataSO>(soPath);
        if(database ==null)
        {
            database = ScriptableObject.CreateInstance<SkillDataSO>();
            AssetDatabase.CreateAsset(database, soPath);
        }
        database.Items.Clear();
        var lines = GetLinesFromCSV(csvText);
        bool skipHeader = true;
        foreach (var raw in lines)
        {
            if (skipHeader) { skipHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("#")) continue;

            var parts = SplitCSVLine(raw);
            //CSV 구조:skillId	skillName	description	skillType	requiredJob	requiredLevel	maxLevel	cooldown	damageRate	levelUpDamageRate	skillIconPath
            if (parts.Count < 9) continue;
            SkillData skill = new SkillData
            {
                skillId = parts[0].Trim(),
                skillName = parts[1].Trim(),
                description = parts[2].Trim(),
                skillType = ParseSkillType(parts[3].Trim()),
                requiredJob = parts[4].Trim(),
                requiredLevel = ParseInt(parts[5].Trim(), 1),
                maxLevel = ParseInt(parts[6].Trim(), 1),
                cooldown = ParseFloat(parts[7].Trim(), 0f),
                damageRate = ParseFloat(parts[8].Trim(), 0f),
                levelUpDamageRate = ParseFloat(parts[9].Trim(), 0f),
                skillIconPath = parts[10].Trim(),
            };
            database.Items.Add(skill);
        }


        if (database != null)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV 파싱 및 ScriptableObject **{soPath}** 생성 완료! ({database.Items.Count}개 데이터)");
        }
        else
        {
            Debug.LogWarning($"변환할 수 없는 CSV 파일입니다");
        }
    }

    // ==========================================
    // TimeRestriction 파싱 헬퍼 메서드 
    // DatabaseGenerater.cs의 ParseMonsterType 함수 다음에 추가하세요
    // ==========================================

    
    /// 시간 제한 타입 파싱
    
    private static TimeRestriction ParseTimeRestriction(string restrictionStr)
    {
        switch (restrictionStr.ToLower())
        {
            case "none":
                return TimeRestriction.None;
            case "night":
                return TimeRestriction.Night;
            case "day":
                return TimeRestriction.Day;
            default:
                Debug.LogWarning($"[DungeonsDataManager] 알 수 없는 시간 제한 타입: {restrictionStr}");
                return TimeRestriction.None;
        }
    }



    // ==========================================
    // 안전한 파싱 헬퍼 메서드
    // ==========================================

    private static float ParseFloat(string str, float defaultValue = 0f)
    {
        if (float.TryParse(str, out float result))
            return result;
        return defaultValue;
    }
    private static int ParseInt(string str, int defaultValue = 0)
    {
        if (int.TryParse(str, out int result))
            return result;
        return defaultValue;
    }

    private static bool ParseBool(string str)
    {
        str = str.ToLower();
        return str == "true";
    }

    private static GatherType ParseGatherType(string toolStr)
    {
        switch (toolStr.ToLower())
        {
            case "gathering":
                return GatherType.Gathering;
            case "mining":
                return GatherType.Mining;
            case "fishing":
                return GatherType.Fishing;
            case "none":
                return GatherType.None;
            default:
                Debug.LogWarning($"[GatherableDataManager] 알 수 없는 채집 도구 타입: {toolStr}");
                return GatherType.None;
        }
    }
    private static SkillType ParseSkillType(string skilltype)
    {
        switch (skilltype.ToLower())
        {
            case "active":
                return SkillType.Active;
            case "buff":
                return SkillType.Active;
            case "passive":
                return SkillType.Passive;
            case "none":
                return SkillType.None;
            default:
                Debug.LogWarning($"[SkillDataManager] 알 수 없는 스킬 타입: {skilltype}");
                return SkillType.None;
        }
    }

    /// 채집 도구 타입 파싱

    private static GatherToolType ParseGatherTool(string toolStr)
    {
        switch (toolStr.ToLower())
        {
            case "pickaxe":
                return GatherToolType.Pickaxe;
            case "sickle":
                return GatherToolType.Sickle;
            case "fishingrod":
                return GatherToolType.FishingRod;
            case "axe":
                return GatherToolType.Axe;
            case "none":
                return GatherToolType.None;
            default:
                Debug.LogWarning($"[GatherableDataManager] 알 수 없는 채집 도구 타입: {toolStr}");
                return GatherToolType.None;
        }
    }

    
    /// 아이템 타입 파싱
    
    private static ItemType ParseItemType(string typeStr)
    {
        switch (typeStr.ToLower())
        {
            case "equipment": return ItemType.Equipment;
            case "consumable": return ItemType.Consumable;
            case "material": return ItemType.Material;
            case "questitem": return ItemType.QuestItem;
            default:
                Debug.LogWarning($"[ItemDataManager] 알 수 없는 아이템 타입: {typeStr}");
                return ItemType.Material;
        }
    }

    
    /// 장비 슬롯 파싱
    
    private static EquipmentSlot ParseEquipSlot(string slotStr)
    {
        return slotStr.ToLower() switch
        {
            "meleeweapon" => EquipmentSlot.MeleeWeapon,      //  근거리 무기
            "rangedweapon" => EquipmentSlot.RangedWeapon,    //  원거리 무기
            "helmet" => EquipmentSlot.Helmet,
            "armor" => EquipmentSlot.Armor,
            "shoes" => EquipmentSlot.Shoes,
            "subweapon" => EquipmentSlot.SubWeapon,
            "ring" => EquipmentSlot.Ring,
            "necklace" => EquipmentSlot.Necklace,
            "bracelet" => EquipmentSlot.Bracelet,
            _ => EquipmentSlot.None,
        };
    }

    
    /// 몬스터 타입 파싱
    
    private static MonsterType ParseMonsterType(string typeStr)
    {
        switch (typeStr.ToLower())
        {
            case "normal":
                return MonsterType.Normal;
            case "elite":
                return MonsterType.Elite;
            case "boss":
                return MonsterType.Boss;
            default:
                Debug.LogWarning($"[MonsterDataManager] 알 수 없는 몬스터 타입: {typeStr}");
                return MonsterType.Normal;
        }
    }



    //  함수를 public static으로 선언해야 다른 파일에서 접근 가능합니다.

    public static List<string> GetLinesFromCSV(string csvText)
    {
        List<string> lines = new();
        string currentLine = "";
        bool inQuotes = false;

        // ... (함수 내용 유지) ...
        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }

            // 줄바꿈 문자 확인
            if (c == '\n')
            {
                // 큰따옴표 밖에 있을 때만 줄바꿈을 레코드의 끝으로 인식합니다.
                if (!inQuotes)
                {
                    // 캐리지 리턴(\r)이 포함되어 있다면 제거
                    lines.Add(currentLine.Trim('\r'));
                    currentLine = "";
                    continue;
                }
            }

            currentLine += c;
        }

        // 마지막 줄 추가
        if (!string.IsNullOrEmpty(currentLine.Trim('\r', '\n')))
        {
            lines.Add(currentLine.Trim('\r'));
        }

        return lines;
    }

    public static List<string> SplitCSVLine(string line)
    {
        List<string> result = new();
        bool inQuotes = false;
        string current = "";

        // ... (함수 내용 유지) ...
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }
}
#endif