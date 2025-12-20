using GameData.Common;
using System.Collections.Generic;
using UnityEditor;
using Definitions;
using UnityEngine;

public class QuestDataManager : MonoBehaviour
{
    public static QuestDataManager Instance { get; private set; }

    [Header("SO 파일")]
    public TextAsset csvFile;
    public QuestDataSO questDatabaseSO;

    public Dictionary<string, QuestData> questList = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (questDatabaseSO != null)
            {
#if UNITY_EDITOR
                
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

    void RegisterAll()
    {
        foreach (var quest in questList)
        {
            QuestManager.Instance.RegisterQuest(quest.Value);
        }
    }

    // 수락 가능한 퀘스트 목록 가져오기
    public Dictionary<string, QuestData> GetAvailableQuests()
    {
        return questList;
    }
    /// <summary>
    /// 퀘스트 id로 데이터 가져오기
    /// </summary>
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