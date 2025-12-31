
using System.Collections.Generic;
using UnityEditor;
using Definitions;
using UnityEngine;


/// 퀘스트 데이터 관리 싱글톤

public class QuestDataManager : MonoBehaviour
{
    public static QuestDataManager Instance { get; private set; }

    [Header("SO 파일")]
    public TextAsset csvFile;
    public QuestDataSO questDatabaseSO;

    public Dictionary<string, QuestData> questList = new();
    // ==========================================
    // 초기화 메서드
    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (questDatabaseSO != null)
            {
#if UNITY_EDITOR
                // Editor 모드에서는 퀘스트 상태 초기화를 위해 CSV 파일을 다시 빌드


                Debug.LogWarning("[QuestDataManager] Editor 모드에서는 CSV 파일을 다시 빌드합니다.");
                if (csvFile == null) return;

                string directory = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(csvFile));
                string normalizedPath = directory.Replace('\\', '/');
                string soPath = normalizedPath + "/Database/" + csvFile.name + "Database.asset";
                QuestDataSO database = AssetDatabase.LoadAssetAtPath<QuestDataSO>(soPath);
                DatabaseGenerater.ParseQuestDataCSV(csvFile.text, soPath);
#endif
                BuildDictionary(questDatabaseSO);
            }
            else
            {
                Debug.LogError("[QuestDataManager] CSV 파일이 할당되지 않았습니다.");
            }

            RegisterAll();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    /// 데이터베이스 초기화 및 재구축
    
    void BuildDictionary(QuestDataSO database)
    {
        questList.Clear();
        foreach (var item in database.Items)
        {
            if (!questList.ContainsKey(item.questId))
            {
                questList.Add(item.questId, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDataManager] 중복 id 발견 (SO): {item.questId}");
            }
        }
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {questList.Count}개의 아이템 로드 완료");
    }

    
    /// 퀘스트매니저에 모든 퀘스트 등록
    
    void RegisterAll()
    {
        foreach (var quest in questList)
        {
            QuestManager.Instance.RegisterQuest(quest.Value);
        }
    }
    // ==========================================
    // 조회 메서드
    // ==========================================

    
    /// 수락 가능한 퀘스트 목록 가져오기
    
    public Dictionary<string, QuestData> GetAvailableQuests()
    {
        return questList;
    }
    
    /// 퀘스트 id로 데이터 가져오기
    
    public QuestData GetGatherableData(string questid)
    {
        if (questList.TryGetValue(questid, out QuestData data))
        {
            return data;
        }

        Debug.LogWarning($"[QuestDataManager] 퀘스트를 찾을 수 없음: {questid}");
        return null;
    }
}