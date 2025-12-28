using Definitions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //씬이 로드 되면 나레이션 시작
        //type는 none, data는 기본값인 basic으로 설정
        //mode는 auto로 설정
        if (scene.name == Def_Name.SCENE_NAME_DEFAULT_MAP)
        {
            NarrationManager.Instance.PlayNarration("System");
        }
    }
    private void Update()
    {
        if(!NarrationManager.Instance.IsNarrationCompleted(NarrationConditionType.None, "System"))
        {
            //기본 나레이션
            CheckCondition checkCondition;
            checkCondition.type = NarrationConditionType.None;
            checkCondition.data = "Basic";

            //기본 나레이션 종료 
            CheckCondition nextCondition;
            nextCondition.type = NarrationConditionType.None;
            nextCondition.data = "System_Complete";
            CheckAndNextQuest(checkCondition, nextCondition);

            //이동 튜토리얼
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.Move;
            nextCondition.data = "System_Move";
            CheckAndNextQuest(checkCondition, nextCondition, NarrationMode.Conditional);

            //이동 튜토리얼 끝
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.Move;
            nextCondition.data = "System_Move_Complete";
            CheckAndNextQuest(checkCondition, nextCondition);

            //인벤토리 열기
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenInventory;
            nextCondition.data = "System_OpenInventory";
            CheckAndNextQuest(checkCondition, nextCondition, NarrationMode.Conditional);

            //이벤토리 열기 끝
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenInventory;
            nextCondition.data = "System_OpenInventory_Complete";
            CheckAndNextQuest(checkCondition, nextCondition);

            //장비창 열기
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenEquipment;
            nextCondition.data = "System_OpenEquipment";
            CheckAndNextQuest(checkCondition, nextCondition, NarrationMode.Conditional);

            //장비창 열기 끝
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenEquipment;
            nextCondition.data = "System_OpenEquipment_Complete";
            CheckAndNextQuest(checkCondition, nextCondition);

            //퀘스트창 열기
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenQuest;
            nextCondition.data = "System_OpenQuest";
            CheckAndNextQuest(checkCondition, nextCondition, NarrationMode.Conditional);

            //퀘스트창 열기 끝
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenQuest;
            nextCondition.data = "System_OpenQuest_Complete";
            CheckAndNextQuest(checkCondition, nextCondition);

            //스텟창 열기
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenStat;
            nextCondition.data = "System_OpenStat";
            CheckAndNextQuest(checkCondition, nextCondition, NarrationMode.Conditional);

            //스텟창 열기 끝
            checkCondition = nextCondition;
            nextCondition.type = NarrationConditionType.OpenStat;
            nextCondition.data = "System_OpenStat_Complete";
            CheckAndNextQuest(checkCondition, nextCondition);
        }
        //스텟창 열기가 끝나면
        if (NarrationManager.Instance.IsNarrationCompleted(NarrationConditionType.OpenStat, "System_OpenStat_Complete"))
        {
            NarrationManager.Instance.StopNarration();
        }
    }

    private void CheckAndNextQuest(CheckCondition CheckCondition, CheckCondition NextCondition,NarrationMode mode = NarrationMode.Auto)
    {
        if (NarrationManager.Instance.IsNarrationCompleted(CheckCondition.type, CheckCondition.data))
        {
            var config = new NarrationConfig()
            {
                narrationId = NextCondition.data,
                mode = mode,
                conditionType = NextCondition.type,
                conditionData = NextCondition.data
            };
            //인벤토리 열기 튜토리얼 시작
            NarrationManager.Instance.PlayNarration(NextCondition.data, config);
        }
    }

    public static void CheckLastNarration()
    {
        //마지막 대사가 끝나면
        //플레이어를 마을로 이동시킴
        if (NarrationManager.Instance.IsNarrationCompleted(NarrationConditionType.None, "System_End"))
        {
            string targetMapId = "FOR_001";
            string targetSpawnPointid = "";

            // 맵 ID로부터 실제 씬 이름 생성
            string targetSceneName = MapInfoManager.Instance.GetSceneName(targetMapId);

            // 유효성 검사
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError($"[MapTransition] 맵 ID '{targetMapId}'로부터 씬 이름을 생성할 수 없습니다!");
                return;
            }

            if (string.IsNullOrEmpty(targetSpawnPointid))
            {
                Debug.LogWarning("[MapTransition] 스폰 포인트 id가 설정되지 않았습니다. 기본 위치로 이동합니다.");
            }

            Debug.Log($"[MapTransition] 맵 전환: MapID={targetMapId} → SceneName={targetSceneName} (Spawn: {targetSpawnPointid})");


            // 캐릭터 상태 저장 (선택사항)
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.SaveStateBeforeDeactivation();
                Debug.Log("[MapTransition] 플레이어 상태 저장 완료");
            }

            MapLoadingManager.LoadMap(targetSceneName, targetSpawnPointid);
        }
    }

    struct CheckCondition
    {
        public NarrationConditionType type;
        public string data;
    }
}
