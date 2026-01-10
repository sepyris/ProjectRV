using Definitions;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;


/// 통합 저장 관리자
/// - 캐릭터 슬롯 관리 (all_characters.sav)
/// - 개별 캐릭터 게임 데이터 관리 (character_{id}.sav)
/// - 씬 전환 및 플레이어 복원

public class CharacterSaveManager : MonoBehaviour
{
    public static CharacterSaveManager Instance { get; private set; }

    // 캐릭터 슬롯 관련
    private const string CHARACTERS_SLOT_FILE = "all_characters.sav";
    private string SlotSavePath => Path.Combine(Def_System.SavePath, CHARACTERS_SLOT_FILE);

    private AllCharactersSaveData allCharactersData;
    public CharacterSlotData CurrentCharacter { get; private set; }

    // 게임 세션 관련 (기존 GameDataManager 통합)
    public GlobalSaveData CurrentGlobalData { get; private set; } = new GlobalSaveData();
    public string NextSceneSpawnPointid { get; set; } = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 전체 게임 시작 - 복호화 1차
            LoadAllCharacterSlots();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("[SaveManager] 게임 종료 - 최종 저장 시작");

        //  현재 캐릭터가 있으면 상점 데이터를 먼저 커밋
        if (CurrentCharacter != null && CurrentCharacter.shopStockData != null)
        {
            Debug.Log("[SaveManager] 종료 전 상점 임시 데이터 커밋");
            CurrentCharacter.shopStockData.CommitTempData();
        }

        //현재 캐릭터를 저장시도 - 인게임에서 캐릭터를 바로 종료시
        if (!SaveCurrentCharacterGameData())
        {
            //실패시 모든 캐릭터 슬롯 저장 - 현재 캐릭터가 없음 = 인게임이 아닌 상태에서 종료시
            SaveAllCharacterSlots();
        }
        else
        {
            //  SaveCurrentCharacterGameData()가 성공해도 한 번 더 슬롯 저장
            SaveAllCharacterSlots();
        }

        Debug.Log("[SaveManager] 전체 게임 종료 - 저장 완료");
    }

    // ==================== 캐릭터 슬롯 관리 (암호화 1차) ====================

    
    /// 전체 게임 시작 시 - 캐릭터 슬롯 로드 (복호화 1차)
    
    public void LoadAllCharacterSlots()
    {
        allCharactersData = LoadEncryptedSlotData();

        if (allCharactersData == null)
        {
            allCharactersData = new AllCharactersSaveData();
            Debug.Log("[SaveManager] 새로운 캐릭터 슬롯 데이터 생성");
        }
        else
        {
            Debug.Log($"[SaveManager] 캐릭터 슬롯 로드 (복호화 1차): {allCharactersData.CharacterCount}개");
        }
    }

    
    /// 전체 게임 종료 시 - 캐릭터 슬롯 저장 (암호화)
    
    public void SaveAllCharacterSlots()
    {
        //  상점 데이터 저장 여부 확인
        if (allCharactersData != null && allCharactersData.CharacterCount > 0)
        {
            foreach (var character in allCharactersData.characterSlots)
            {
                if (character.shopStockData != null)
                {
                    int purchasedCount = character.shopStockData.purchasedItems?.Count ?? 0;
                    int rebuyCount = character.shopStockData.rebuyItems?.Count ?? 0;
                    Debug.Log($"[SaveManager] {character.stats.characterName} 상점 데이터: 구매 {purchasedCount}개, 재매입 {rebuyCount}개");
                }
            }
        }

        SaveEncryptedSlotData(allCharactersData);
        Debug.Log($"[SaveManager] 캐릭터 슬롯 저장 (암호화): {allCharactersData.CharacterCount}개");
    }

    private void SaveEncryptedSlotData(AllCharactersSaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            byte[] encrypted = SimpleEncrypt(bytes);
            File.WriteAllBytes(SlotSavePath, encrypted);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 슬롯 저장 실패: {e.Message}");
        }
    }

    private AllCharactersSaveData LoadEncryptedSlotData()
    {
        if (!File.Exists(SlotSavePath))
            return null;

        try
        {
            byte[] encrypted = File.ReadAllBytes(SlotSavePath);
            byte[] decrypted = SimpleDecrypt(encrypted);
            string json = System.Text.Encoding.UTF8.GetString(decrypted);
            return JsonUtility.FromJson<AllCharactersSaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 슬롯 로드 실패: {e.Message}");
            return null;
        }
    }

    private byte[] SimpleEncrypt(byte[] data)
    {
        byte[] key = System.Text.Encoding.UTF8.GetBytes("MySecretKey12345");
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        return result;
    }

    private byte[] SimpleDecrypt(byte[] data)
    {
        return SimpleEncrypt(data);
    }

    // ==================== 캐릭터 생성/삭제 ====================

    
    /// 새 캐릭터 생성
    
    public CharacterSlotData CreateCharacter(string characterName, int slotIndex)
    {
        if (allCharactersData.IsFull)
        {
            Debug.LogWarning("[SaveManager] 캐릭터 슬롯이 가득 찼습니다!");
            return null;
        }

        Debug.Log($"[SaveManager] === 새 캐릭터 생성 시작: {characterName} ===");

        //  CurrentCharacter를 null로 설정 (중요!)
        CurrentCharacter = null;
        CurrentGlobalData = new GlobalSaveData();

        // 매니저 완전 초기화
        if (InventoryManager.Instance != null)
        {
            Debug.Log("[SaveManager] 인벤토리 완전 초기화");
            InventoryManager.Instance.ClearInventory();
        }

        if (QuestManager.Instance != null)
        {
            Debug.Log("[SaveManager] 퀘스트 완전 초기화");
            QuestManager.Instance.ResetAllQuests();
        }

        //  장비 매니저 초기화 추가
        if (EquipmentManager.Instance != null)
        {
            Debug.Log("[SaveManager] 장비 매니저 초기화");
            EquipmentManager.Instance.ClearAllEquipment();
        }

        if (SkillManager.Instance != null)
        {
            Debug.Log("[SaveManager] 스킬 매니저 초기화");
            SkillManager.Instance.ClearAllSkills();
        }

        // 새 캐릭터 슬롯 데이터 생성
        CharacterSlotData newCharacter = CharacterSlotData.CreateNew(characterName, slotIndex);
        allCharactersData.AddCharacter(newCharacter);

        SaveAllCharacterSlots();
        CreateInitialGameData(newCharacter);

        Debug.Log($"[SaveManager] === 새 캐릭터 생성 완료: {characterName} (슬롯 {slotIndex}) ===");
        return newCharacter;
    }

    
    /// 캐릭터 삭제
    
    public void DeleteCharacter(string characterid)
    {
        allCharactersData.RemoveCharacter(characterid);
        SaveAllCharacterSlots();
        DeleteGameData(characterid);

        Debug.Log($"[SaveManager] 캐릭터 삭제: {characterid}");
    }

    // ==================== 캐릭터별 게임 시작/종료 ====================

    
    /// 캐릭터 게임 시작 (데이터 준비만)
    
    public void StartCharacterGame(string characterid)
    {
        Debug.Log($"[SaveManager] === StartCharacterGame: {characterid} ===");

        //  이전 캐릭터가 있고 다른 캐릭터로 전환하는 경우
        if (CurrentCharacter != null && CurrentCharacter.characterid != characterid)
        {
            Debug.Log($"[SaveManager] 캐릭터 전환 감지 - 이전 캐릭터 저장: {CurrentCharacter.stats.characterName}");
            SaveCurrentCharacterBeforeSwitch();
        }

        //  CurrentCharacter 먼저 설정 (PlayerStatsComponent가 이걸 참조함)
        CurrentCharacter = allCharactersData.GetCharacter(characterid);
        if (CurrentCharacter != null)
        {
            allCharactersData.lastSelectedCharacterid = characterid;
            CurrentCharacter.UpdateLastPlayed();
            SaveAllCharacterSlots();

            Debug.Log($"[SaveManager] 캐릭터 선택 완료: {CurrentCharacter.stats.characterName}");
            Debug.Log($"  - 레벨: {CurrentCharacter.stats.level}");
            Debug.Log($"  - HP: {CurrentCharacter.stats.currentHP}/{CurrentCharacter.stats.maxHP}");

            //  상점 데이터 로드 로그
            if (CurrentCharacter.shopStockData != null)
            {
                int purchasedCount = CurrentCharacter.shopStockData.purchasedItems?.Count ?? 0;
                int rebuyCount = CurrentCharacter.shopStockData.rebuyItems?.Count ?? 0;
                Debug.Log($"  - 상점 데이터 로드: 구매 {purchasedCount}개, 재매입 {rebuyCount}개");
            }
        }
        else
        {
            Debug.LogError($"[SaveManager] 캐릭터를 찾을 수 없음: {characterid}");
        }
    }

    
    /// 캐릭터 전환 전 저장
    
    private void SaveCurrentCharacterBeforeSwitch()
    {
        if (CurrentCharacter == null) return;

        // 인벤토리 저장
        if (InventoryManager.Instance != null)
        {
            CurrentCharacter.inventoryData = InventoryManager.Instance.ToSaveData();
            CurrentGlobalData.inventoryData = CurrentCharacter.inventoryData;
            Debug.Log($"[SaveManager] 인벤토리 저장: {CurrentCharacter.inventoryData.items?.Count ?? 0}개 아이템");
        }

        // 퀘스트 저장
        if (QuestManager.Instance != null)
        {
            CurrentGlobalData.questData = QuestManager.Instance.ToSaveData();
            Debug.Log($"[SaveManager] 퀘스트 저장: {CurrentGlobalData.questData.quests?.Count ?? 0}개");
        }

        // 파일 저장
        string saveFileName = $"character_{CurrentCharacter.characterid}.sav";
        string savePath = Path.Combine(Def_System.SavePath, saveFileName);
        SecureSaveLoad.SaveData(savePath, CurrentGlobalData);

        Debug.Log($"[SaveManager] 캐릭터 데이터 저장 완료: {CurrentCharacter.stats.characterName}");
    }

    
    /// 캐릭터 게임 종료 (캐릭터 선택 화면으로)
    /// 세이브 포인트에서만 저장하므로 여기서는 초기화만 수행
    
    public void EndCharacterGame()
    {
        if (CurrentCharacter == null)
        {
            Debug.LogWarning("[SaveManager] 종료할 캐릭터가 없습니다.");
            return;
        }

        Debug.Log($"[SaveManager] === 캐릭터 게임 종료: {CurrentCharacter.stats.characterName} ===");

        //  상점 임시 데이터 롤백 (저장 안함)
        if (CurrentCharacter.shopStockData != null)
        {
            CurrentCharacter.shopStockData.RollbackTempData();
            Debug.Log($"[SaveManager] 상점 임시 데이터 롤백 (저장하지 않은 변경사항 취소됨)");
        }

        //  나가기 전 현재 상태 저장 
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene.StartsWith(Def_Name.SCENE_NAME_START_MAP))
        {
            // 플레이어 위치/스탯 저장
            if (PlayerController.Instance != null)
            {
                var statsComp = PlayerController.Instance.GetComponent<PlayerStatsComponent>();
                if (statsComp != null && statsComp.Stats != null)
                {
                    CurrentCharacter.stats = statsComp.Stats.ToSaveData();
                }
                CurrentCharacter.position = PlayerController.Instance.transform.position;
                CurrentCharacter.currentScene = currentScene;

            }

            // 인벤토리 저장
            if (InventoryManager.Instance != null)
            {
                CurrentCharacter.inventoryData = InventoryManager.Instance.ToSaveData();
                CurrentGlobalData.inventoryData = CurrentCharacter.inventoryData;
            }

            // 퀘스트 저장
            if (QuestManager.Instance != null)
            {
                CurrentGlobalData.questData = QuestManager.Instance.ToSaveData();
            }

            // 파일 저장
            CurrentGlobalData.currentSceneName = CurrentCharacter.currentScene;
            CurrentGlobalData.playerPosition = CurrentCharacter.position;

            string saveFileName = $"character_{CurrentCharacter.characterid}.sav";
            string savePath = Path.Combine(Def_System.SavePath, saveFileName);
            SecureSaveLoad.SaveData(savePath, CurrentGlobalData);

            Debug.Log($"[SaveManager] 게임 종료 전 자동 저장 완료");
        }

        // 초기화
        CurrentCharacter = null;
        CurrentGlobalData = new GlobalSaveData();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ClearInventory();
        if (QuestManager.Instance != null)
            QuestManager.Instance.ResetAllQuests();

        Debug.Log($"[SaveManager] === 캐릭터 게임 종료 완료 ===");

        SceneManager.LoadScene("CharacterSelectScene");
    }

    // ==================== 게임 데이터 관리 (암호화 2차) ====================

    
    /// 초기 게임 데이터 생성
    
    private void CreateInitialGameData(CharacterSlotData character)
    {
        Debug.Log($"[SaveManager] 초기 게임 데이터 생성 시작: {character.stats.characterName}");

        GlobalSaveData newGameData = new GlobalSaveData();
        newGameData.currentSceneName = Def_Name.SCENE_NAME_DEFAULT_MAP;

        // 인벤토리 초기화 (빈 상태로 시작)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
            newGameData.inventoryData = InventoryManager.Instance.ToSaveData();
            Debug.Log("[SaveManager] 인벤토리 초기화 (빈 상태)");
        }

        // 퀘스트 초기화
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ResetAllQuests();
            newGameData.questData = QuestManager.Instance.ToSaveData();
            Debug.Log("[SaveManager] 퀘스트 초기화");
        }

        // 파일 저장
        string saveFileName = $"character_{character.characterid}.sav";
        string savePath = Path.Combine(Def_System.SavePath, saveFileName);
        SecureSaveLoad.SaveData(savePath, newGameData);

        Debug.Log($"[SaveManager] 초기 게임 데이터 생성 완료: {saveFileName}");
    }

    
    /// 게임 데이터 로드 (GameLoadingScene에서 호출)
    
    public void LoadCurrentCharacterGameData()
    {
        if (CurrentCharacter == null)
        {
            Debug.LogError("[SaveManager] 선택된 캐릭터가 없습니다!");
            return;
        }

        Debug.Log($"[SaveManager] ====================================");
        Debug.Log($"[SaveManager] 캐릭터 데이터 로드 시작");
        Debug.Log($"[SaveManager] ====================================");
        Debug.Log($"  캐릭터: {CurrentCharacter.stats.characterName}");
        Debug.Log($"  ID: {CurrentCharacter.characterid}");
        Debug.Log($"  레벨: {CurrentCharacter.stats.level}");

        string saveFileName = $"character_{CurrentCharacter.characterid}.sav";
        string savePath = Path.Combine(Def_System.SavePath, saveFileName);

        Debug.Log($"[SaveManager] 파일 경로: {savePath}");

        if (!System.IO.File.Exists(savePath))
        {
            Debug.LogWarning($"[SaveManager]  저장 파일이 존재하지 않습니다!");
            Debug.LogWarning($"[SaveManager] 초기 데이터로 시작합니다.");

            CurrentGlobalData = new GlobalSaveData();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ClearInventory();
            }
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ResetAllQuests();
            }
            return;
        }

        Debug.Log($"[SaveManager]  저장 파일 발견!");

        // 파일 로드
        GlobalSaveData gameData = SecureSaveLoad.LoadData(savePath);

        if (gameData == null)
        {
            Debug.LogError($"[SaveManager]  파일 로드 실패 (복호화 실패?)");
            CurrentGlobalData = new GlobalSaveData();

            if (InventoryManager.Instance != null) InventoryManager.Instance.ClearInventory();
            if (QuestManager.Instance != null) QuestManager.Instance.ResetAllQuests();
            return;
        }

        Debug.Log($"[SaveManager]  파일 로드 성공!");

        CurrentGlobalData = gameData;
        NextSceneSpawnPointid = "Main_SpawnPoint";

        // ===== 인벤토리 로드 =====
        Debug.Log($"[SaveManager] --- 인벤토리 로드 ---");

        if (InventoryManager.Instance != null)
        {
            // 1. 완전 초기화
            InventoryManager.Instance.ClearInventory();
            Debug.Log($"[SaveManager] 인벤토리 초기화 완료");

            // 2. 데이터 확인
            if (gameData.inventoryData != null && gameData.inventoryData.items != null)
            {
                Debug.Log($"[SaveManager] 저장된 아이템 발견: {gameData.inventoryData.items.Count}개");

                // 3. 아이템 내역 출력
                foreach (var item in gameData.inventoryData.items)
                {
                    Debug.Log($"  📦 {item.itemid} {item.quantity}");
                }

                // 4. 로드
                InventoryManager.Instance.LoadFromData(gameData.inventoryData);

                // 5. 로드 후 확인
                var loadedData = InventoryManager.Instance.ToSaveData();
                Debug.Log($"[SaveManager] 로드 후 인벤토리 아이템 수: {loadedData.items?.Count ?? 0}개");
            }
            else
            {
                Debug.Log($"[SaveManager]  저장된 인벤토리 데이터 없음");
            }
        }

        // ===== 퀘스트 로드 =====
        Debug.Log($"[SaveManager] --- 퀘스트 로드 ---");

        if (QuestManager.Instance != null)
        {
            // 1. 완전 초기화
            QuestManager.Instance.ResetAllQuests();
            Debug.Log($"[SaveManager] 퀘스트 초기화 완료");

            // 2. 데이터 확인
            if (gameData.questData != null && gameData.questData.quests != null)
            {
                Debug.Log($"[SaveManager] 저장된 퀘스트 발견: {gameData.questData.quests.Count}개");

                // 3. 퀘스트 내역 출력
                foreach (var quest in gameData.questData.quests)
                {
                    Debug.Log($"   {quest.questId}: {quest.status}");
                }

                // 4. 로드
                QuestManager.Instance.LoadFromData(gameData.questData);

                // 5. 로드 후 확인
                var loadedData = QuestManager.Instance.ToSaveData();
                Debug.Log($"[SaveManager] 로드 후 퀘스트 수: {loadedData.quests?.Count ?? 0}개");
            }
            else
            {
                Debug.Log($"[SaveManager]  저장된 퀘스트 데이터 없음");
            }
        }

        // ===== 퀵슬롯 로드 =====
        if (QuickSlotManager.Instance != null && CurrentCharacter.quickSlots != null)
        {
            QuickSlotManager.Instance.LoadFromSaveData(CurrentCharacter.quickSlots);
            Debug.Log($"[SaveManager] 퀵슬롯 로드: {CurrentCharacter.quickSlots.Count}개");

            // UI 새로고침
            if (QuickSlotUIManager.Instance != null)
            {
                QuickSlotUIManager.Instance.RefreshAllSlots();
            }
        }

        //  장비 복원 추가 (인벤토리 로드 후에 호출해야 함!)
        if (EquipmentManager.Instance != null && CurrentCharacter.equipmentData != null)
        {
            EquipmentManager.Instance.LoadFromSaveData(CurrentCharacter.equipmentData);
            Debug.Log($"[SaveManager] 장비 복원: {CurrentCharacter.equipmentData.equippedItems.Count}개 장비, {CurrentCharacter.equipmentData.cosmeticItems.Count}개 치장");
        }

        //  상점 재고 데이터 로드 후 임시 데이터 초기화
        if (CurrentCharacter.shopStockData != null)
        {
            CurrentCharacter.shopStockData.OnDataLoaded();
            Debug.Log($"[SaveManager] 상점 재고 데이터 로드 완료 (임시 데이터 초기화됨)");
        }
        // 스킬 로드
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.LoadFromData(CurrentCharacter.skillData);
            Debug.Log($"[SaveManager] 스킬 로드: {CurrentCharacter.skillData?.skills?.Count ?? 0}개");
        }


        Debug.Log($"[SaveManager] ====================================");
        Debug.Log($"[SaveManager] 캐릭터 데이터 로드 완료");
        Debug.Log($"[SaveManager] ====================================");
    }

    public bool SaveCurrentCharacterGameData()
    {
        if (CurrentCharacter == null)
        {
            Debug.LogWarning("[SaveManager] 저장할 캐릭터가 없습니다.");
            return false;
        }

        // 현재 플레이어 상태 수집
        if (PlayerController.Instance != null)
        {
            var statsComp = PlayerController.Instance.GetComponent<PlayerStatsComponent>();
            if (statsComp != null && statsComp.Stats != null)
            {
                CurrentCharacter.stats = statsComp.Stats.ToSaveData();
                Debug.Log($"[SaveManager] 스텟 저장: Lv.{CurrentCharacter.stats.level}");
            }

            CurrentCharacter.position = PlayerController.Instance.transform.position;
            CurrentCharacter.currentScene = SceneManager.GetActiveScene().name;
        }

        // 인벤토리 저장
        if (InventoryManager.Instance != null)
        {
            var inventoryData = InventoryManager.Instance.ToSaveData();
            CurrentCharacter.inventoryData = inventoryData;
            CurrentGlobalData.inventoryData = inventoryData;

            Debug.Log($"[SaveManager] 인벤토리 저장: {inventoryData.items?.Count ?? 0}개 아이템");

            // 아이템 내역 출력
            if (inventoryData.items != null)
            {
                foreach (var item in inventoryData.items)
                {
                    Debug.Log($"{item.itemid} {item.quantity}");
                }
            }
        }

        // 퀘스트 저장
        if (QuestManager.Instance != null)
        {
            var questData = QuestManager.Instance.ToSaveData();
            CurrentGlobalData.questData = questData;

            Debug.Log($"[SaveManager] 퀘스트 저장: {questData.quests?.Count ?? 0}개");
        }

        // 퀵슬롯 저장
        if (QuickSlotManager.Instance != null)
        {
            CurrentCharacter.quickSlots = QuickSlotManager.Instance.GetSaveData();
            Debug.Log($"[SaveManager] 퀵슬롯 저장: {CurrentCharacter?.quickSlots.Count ?? 0}개");
        }
        //스킬 저장
        if (SkillManager.Instance != null)
        {
            CurrentCharacter.skillData = SkillManager.Instance.ToSaveData();
            Debug.Log($"[SaveManager] 스킬 저장: {CurrentCharacter.skillData?.skills?.Count ?? 0}개");
        }

        //  장비 저장 추가
        if (EquipmentManager.Instance != null)
        {
            CurrentCharacter.equipmentData = EquipmentManager.Instance.ToSaveData();
            Debug.Log($"[SaveManager] 장비 저장: {CurrentCharacter.equipmentData.equippedItems?.Count ?? 0}개 장비, {CurrentCharacter.equipmentData.cosmeticItems?.Count ?? 0}개 치장");
        }

        //  상점 임시 데이터 커밋 (실제 데이터에 반영)
        if (CurrentCharacter.shopStockData != null)
        {
            CurrentCharacter.shopStockData.CommitTempData();
            Debug.Log($"[SaveManager] 상점 재고 임시 데이터 커밋 완료");
        }

        // 현재 씬 정보 저장
        if (CurrentCharacter != null)
        {
            CurrentGlobalData.currentSceneName = CurrentCharacter.currentScene;
            CurrentGlobalData.playerPosition = CurrentCharacter.position;
        }

        // 파일 저장
        string saveFileName = $"character_{CurrentCharacter.characterid}.sav";
        string savePath = Path.Combine(Def_System.SavePath, saveFileName);

        Debug.Log($"[SaveManager] 저장 경로: {savePath}");
        SecureSaveLoad.SaveData(savePath, CurrentGlobalData);

        SaveAllCharacterSlots();
        return true;
    }

    
    /// 게임 데이터 파일 삭제
    
    private void DeleteGameData(string characterid)
    {
        string saveFileName = $"character_{characterid}.sav";
        string savePath = Path.Combine(Def_System.SavePath, saveFileName);
        SecureSaveLoad.DeleteSaveData(savePath);
    }

    // ==================== 씬 로드 및 전환 (기존 GameDataManager 기능) ====================

    
    /// 씬 로드 (이름으로)
    
    public void LoadSceneByName(string sceneName, string spawnPointid = "")
    {
        CurrentGlobalData.currentSceneName = sceneName;
        if (!string.IsNullOrEmpty(spawnPointid))
        {
            NextSceneSpawnPointid = spawnPointid;
        }
        StartCoroutine(LoadSceneAndRestore(sceneName));
    }

    
    /// 씬 로드 및 플레이어 복원
    
    private IEnumerator LoadSceneAndRestore(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = Def_Name.SCENE_NAME_DEFAULT_MAP;

        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowGlobalLoading();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!asyncLoad.isDone) yield return null;

        Debug.Log($"[SaveManager] 씬 '{sceneName}' 로드 완료");

        if (MiniMapManager.Instance != null)
            MiniMapManager.Instance.ReInitialize();

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.CompareTag("TmpCamera"))
            Destroy(mainCam.gameObject);

        var cameraCtrl = FindGameCameraController();
        if (cameraCtrl != null)
            cameraCtrl.ReInitialize();

        //카메라 초기화 이후에 캐릭터의 위치 조절
        yield return StartCoroutine(HandlePlayerSpawn());

        yield return new WaitForSeconds(1.0f);

        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.HideGlobalLoading();

        Debug.Log($"[SaveManager] 씬 '{sceneName}' 로드 및 복원 완료");
    }

    
    /// 플레이어 스폰 처리
    
    private IEnumerator HandlePlayerSpawn()
    {
        PlayerController player = FindObjectOfType<PlayerController>();

        if (player == null)
        {
            Debug.LogError("[SaveManager] PlayerController를 찾을 수 없음!");
            yield break;
        }

        if (!string.IsNullOrEmpty(NextSceneSpawnPointid))
        {
            MapSpawnPoint[] allPoints = FindObjectsOfType<MapSpawnPoint>();
            foreach (var point in allPoints)
            {
                if (point.spawnPointid == NextSceneSpawnPointid)
                {
                    //로딩후 세이브 데이터를 확인 하여 스폰위치를 정하기 때문에 이동될 위치를 세이브 데이터에 저장
                    CurrentCharacter.position = point.transform.position;
                    player.transform.position = point.transform.position;
                    Debug.Log($"[SaveManager] 플레이어를 '{NextSceneSpawnPointid}'로 이동");
                    break;
                }
            }
            NextSceneSpawnPointid = "";
        }
        else if (CurrentCharacter != null)
        {
            player.transform.position = CurrentCharacter.position;
            Debug.Log($"[SaveManager] 플레이어 위치 복원: {CurrentCharacter.position}");
        }

        yield return null;
    }

    private CameraController FindGameCameraController()
    {
        foreach (var cam in FindObjectsOfType<Camera>(true))
        {
            if (cam.CompareTag("GameCamera"))
                return cam.GetComponent<CameraController>();
        }
        return null;
    }

    // ==================== Public API ====================

    public AllCharactersSaveData GetSaveData()
    {
        return allCharactersData;
    }

    
    /// 현재 플레이 중인 캐릭터 이름
    
    public string GetCurrentCharacterName()
    {
        return CurrentCharacter?.stats.characterName ?? "알 수 없음";
    }


    
    /// 현재 씬 이름
    
    public string GetCurrentSceneName()
    {
        return CurrentGlobalData.currentSceneName;
    }

    // ==================== 호환성 메서드 (기존 GameDataManager 메서드) ====================

    
    /// 게임 로드 (호환성용)
    /// 새 코드에서는 LoadSceneByName 사용 권장
    
    public void LoadGame(string slotName = "")
    {
        string sceneName = CurrentGlobalData.currentSceneName;
        if (string.IsNullOrEmpty(sceneName))
            sceneName = Def_Name.SCENE_NAME_DEFAULT_MAP;

        LoadSceneByName(sceneName);
    }

    
    /// 서브씬 상태 저장 (호환성용)
    /// 새 코드에서는 CurrentGlobalData.subSceneState에 직접 접근 권장
    
    public void SaveSubSceneState(SubSceneData data)
    {
        CurrentGlobalData.subSceneState = data;
    }

    
    /// 서브씬 상태 로드 (호환성용)
    /// 새 코드에서는 CurrentGlobalData.subSceneState에 직접 접근 권장
    
    public SubSceneData LoadSubSceneState()
    {
        return CurrentGlobalData.subSceneState;
    }
}

// ==================== 데이터 클래스 정의 ====================


/// 글로벌 저장 데이터 (개별 캐릭터 게임 데이터)

[System.Serializable]
public class GlobalSaveData
{
    public string currentSceneName = "";
    public Vector3 playerPosition = Vector3.zero;
    public int playerHealth = 100;
    public SubSceneData subSceneState = new SubSceneData();
    public InventorySaveData inventoryData = new InventorySaveData();
    public AllQuestsSaveData questData = new AllQuestsSaveData();
    public string integrityHash = "";
    public string lastSafeZoneScene = "";
    public string lastSafeZoneSpawnPoint = "";
    public Vector3 lastSafeZonePosition = Vector3.zero;
}


/// 서브씬 저장 데이터

[System.Serializable]
public struct SubSceneData
{
    public string currentSceneName;
    public float positionX;
    public float positionY;
    public float positionZ;
    public int health;

    public static SubSceneData Default() => new SubSceneData { health = 100 };
}