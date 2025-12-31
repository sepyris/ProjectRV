// Definitions.cs
// 프로젝트의 모든 상수, 태그, 메시지를 통합 관리
using UnityEngine;
using System;
using System.IO;

namespace Definitions
{
    public static class Def_System
    {
        public const bool USING_STEAM_CLOUD = false; // 스팀 클라우드 사용 여부
        public const string GameName = "ProjectRV";
        //저장경로 -> 개발시에는 프로젝트 폴더내에 저장, 빌드시에는 문서폴더내에 게임이름 폴더로 저장
#if UNITY_EDITOR
        private static readonly string savePath = Path.Combine(Application.dataPath, "..", "SaveData");
#else
        private static readonly string savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            GameName
        );
#endif
        public static string SavePath => savePath;
    }
    // ===== 이름, 태그, 레이어 =====
    public static class Def_Name
    {
        // 태그
        public const string PLAYER_TAG = "Player";
        public const string WORLD_BORDER_TAG = "WorldBorder";
        public const string GAME_CAMERA = "GameCamera";
        public const string MONSTER_TAG = "Monster";
        public const string NPC_TAG = "NPC";
        public const string TMPCAMERA_TAG = "TmpCamera";


        // 씬 이름
        public const string SCENE_NAME_INITIAL_LOADING_SCENE = "00_MainLoading";
        public const string SCENE_NAME_MAIN_SCREEN = "01_MainScreen";
        public const string SCENE_NAME_CHARACTER_SELECT_SCENE = "02_CharacterSelectWindow";
        public const string SCENE_NAME_GAME_LOADING_SCENE = "03_GameStartLoadingScene";
        public const string SCENE_NAME_MAP_LOADING_SCENE = "MapLoadingScene";

        // 카테고리별 접두 (사용 예: Map_Town_Village01)
        public const string SCENE_NAME_START_MAP = "Map_";
        public const string SCENE_PREFIX_TOWN = "Map_Town_";
        public const string SCENE_PREFIX_FIELD = "Map_Field_";
        public const string SCENE_PREFIX_DUNGEON = "Map_Dungeon_";
        public const string SCENE_NAME_DEFAULT_MAP = "Map_Town_Tutorial01";

        // 입력 축
        public const string HORIZONTAL = "Horizontal";
        public const string VERTICAL = "Vertical";

        // 레이어
        public const string LAYER_MONSTER = "Monster";
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_NPC = "NPC";
        public const string LAYER_GATHERING = "Gathering";

        // 폰트 리소스
        public const string FONT_KOR = "Font_Korean";
        public const string FONT_ENG = "Font_English";
        public const string FONT_JPN = "Font_Japanese";
        public const string FONT_CNA = "Font_Chinese";
        public const string FONT_KOR_SDF = "Font_Korean_SDF";
        public const string FONT_ENG_SDF = "Font_English_SDF";
        public const string FONT_JPN_SDF = "Font_Japanese_SDF";
        public const string FONT_CNA_SDF = "Font_Chinese_SDF";

        // 리소스 경로
        public const string RESOURCE_PATH_GAME_DATA_MANAGER = "GameDataManager";
    }

    // ===== 코드 내 하드코딩된 값 (인스펙터 제외) =====
    public static class Def_Values
    {
        // 파일 시스템
        public const string KEY_FILE_NAME = "user_encryption_key.dat";
        public const string SUB_SCENE_SAVE_KEY = "SubSceneTempData";
        public const int ENCRYPTION_KEY_SIZE = 16;

        // 플레이어
        public const float PLAYER_PROJECTILE_LIFETIME = 3f;
        public const int PLAYER_DEFAULT_HEALTH = 100; // 데이터 초기화용
        public const float PLAYER_MIN_MOVE_MAGNITUDE = 0.01f;

        // 몬스터
        public const float MONSTER_CHASE_SPEED_MULTIPLIER = 1.5f;
        public const float MONSTER_CHASE_MAX_SPEED_MULTIPLIER = 2.0f;
        public const float MONSTER_CHASE_DETECTION_MULTIPLIER = 1.2f;
        public const float MONSTER_DEATH_DESTROY_DELAY = 0.5f;
        public const float MONSTER_POSITION_TOLERANCE = 0.2f;
        public const float MONSTER_MIN_STOP_DISTANCE = 0.1f;

        // 몬스터 스폰
        public const float SPAWN_AI_ROUTINE_WAIT = 0.5f;
        public const float SPAWN_DEFAULT_BOX_SIZE = 5f; // 콜라이더 기본값

        // 로딩
        public const int LOADING_WARNING_FRAME_INTERVAL = 60;

        // 디버그
        public const float DEBUG_RAY_DURATION = 0.2f;

        // 씬 관리
        public const string MANAGER_INSTANCE_CHECK_NULL = "null";
        public const string MANAGER_INSTANCE_CHECK_PRESENT = "present";
    }



    public static class Def_Layer_Mask_Values
    {
        // 기본 레이어 마스크 값
        public const int LAYER_MASK_DEFAULT = 1 << 0; // Default
        public const int LAYER_MASK_TRANSPARENTFX = 1 << 1; // TransparentFX
        public const int LAYER_MASK_IGNORE_RAYCAST = 1 << 2; // Ignore Raycast
        public const int LAYER_MASK_WATER = 1 << 4; // Water
        public const int LAYER_MASK_UI = 1 << 5; // UI

        // 커스텀 레이어 마스크 값
        public const int LAYER_MASK_RAWIMAGE = 1 << 6; // Monster
        public const int LAYER_MASK_MONSTER = 1 << 7; // Monster
        public const int LAYER_MASK_PLAYER = 1 << 8;  // Player
        public const int LAYER_MASK_NPC = 1 << 9;     // NPC
        public const int LAYER_MASK_MINIMAP_OBJECT = 1 << 10;     // MinimapObject            
        public const int LAYER_MASK_BLOCKING_OBJECT = 1 << 11;     // BlockingObject
        public const int LAYER_MASK_PORTAL_OBJECT = 1 << 12;     // Portal
        public const int LAYER_MASK_GATHERING = 1 << 13;    //gathring
        public const int LAYER_MASK_INTERACT_KEY = 1 << 14;    //interactkey
        public const int LAYER_MASK_WALL = 1 << 15;    //wall

    }
    public static class Def_CSV
    {
        // === CSV 파일 이름 (확장자 제외, Resources 바로 아래) ===
        public const string LOCALIZATION = "localization";
        public const string DIALOGUES = "Dialogue";
        public const string GATHERABLES = "Gatherable";
        public const string ITEMS = "items";
        public const string MAPINFO = "Maps";
        public const string MONSTER = "Monster";
        public const string NPCINFO = "Npcs";
        public const string QUESTS = "Quest";
        public const string SHOP = "Shop";
        public const string DUNGEONS = "Dungeons";
        public const string Skill = "Skill";


    }
    public static class Def_Dialogue
    {
        // 기본 대화 타입
        public const string TYPE_NORMAL = "Normal";
        public const string TYPE_DAILY = "Daily";

        // 퀘스트 관련 대화 타입
        public const string TYPE_QUEST_OFFER = "Quest_Offer";
        public const string TYPE_QUEST_ACCEPT = "Quest_Accept";
        public const string TYPE_QUEST_DECLINE = "Quest_Decline";
        public const string TYPE_QUEST_PROGRESS = "Quest_Progress";
        public const string TYPE_QUEST_COMPLETE = "Quest_Complete";
        public const string TYPE_QUEST_REWARD = "Quest_Reward";

        // 상점 관련 대화 타입
        public const string TYPE_SHOP_GREETING = "Shop_Greeting";
        public const string TYPE_SHOP_BUY = "Shop_Buy";
        public const string TYPE_SHOP_SELL = "Shop_Sell";
        public const string TYPE_SHOP_FAREWELL = "Shop_Farewell";

        // 특수 상황 대화 타입
        public const string TYPE_FIRST_MEET = "First_Meet";
        public const string TYPE_REPEAT_VISIT = "Repeat_Visit";
        public const string TYPE_NIGHT_TIME = "Night_Time";
        public const string TYPE_EVENT = "Event";


        // 나레이션 타입 (튜토리얼, 힌트, 안내 등)
        public const string TYPE_NARRATION = "Narration";
        // 디버그 메시지
        public const string DIALOGUE_NOT_FOUND = "[DialogueUIManager] 대화를 찾을 수 없습니다.";
        public const string DIALOGUE_SEQUENCE_START = "[DialogueUIManager] 대화 시퀀스 시작:";
        public const string DIALOGUE_SEQUENCE_END = "[DialogueUIManager] 대화 시퀀스 종료.";
    }
    // ===== 퀘스트 UI 관련 =====
    public static class Def_QuestUI
    {
        // 탭 이름
        public const string TAB_AVAILABLE = "시작 가능";
        public const string TAB_IN_PROGRESS = "진행중";
        public const string TAB_COMPLETED = "완료";

        // 빈 상태 메시지
        public const string EMPTY_AVAILABLE = "시작 가능한 퀘스트가 없습니다.";
        public const string EMPTY_IN_PROGRESS = "진행중인 퀘스트가 없습니다.";
        public const string EMPTY_COMPLETED = "완료한 퀘스트가 없습니다.";

        // 디버그 메시지
        public const string QUEST_UI_OPENED = "[QuestUI] 퀘스트 창 열림";
        public const string QUEST_UI_CLOSED = "[QuestUI] 퀘스트 창 닫힘";
        public const string QUEST_DETAIL_SHOWN = "[QuestUI] 퀘스트 상세 정보 표시:";
        public const string QUEST_NOT_FOUND = "[QuestUI] 퀘스트를 찾을 수 없음:";
    }


    // ===== CSV 관련 디버그 메시지 =====
    public static class Def_CSV_Messages
    {
        // 로드 성공
        public const string CSV_LOAD_SUCCESS = "[CSV] '{0}' 로드 완료: {1}개 항목";

        // 로드 실패
        public const string CSV_FILE_NOT_FOUND = "[CSV] 파일을 찾을 수 없습니다: Resources/{0}.csv";
        public const string CSV_FILE_EMPTY = "[CSV] CSV 파일이 비어있습니다: {0}";
        public const string CSV_PARSE_ERROR = "[CSV] 파싱 오류 (Line {0}): {1}";

        // 자동 생성
        public const string CSV_AUTO_GENERATE = "[CSV] 기본 CSV 파일 자동 생성: {0}";
        public const string CSV_GENERATE_SUCCESS = "[CSV] 파일 생성 완료: {0}";
        public const string CSV_GENERATE_FAIL = "[CSV] 파일 생성 실패: {0}";

        // 리로드
        public const string CSV_RELOAD_SUCCESS = "[CSV] '{0}' 리로드 완료";

        // 무결성 검사
        public const string CSV_HEADER_INVALid = "[CSV] 헤더가 올바르지 않습니다: {0}";
        public const string CSV_DUPLICATE_id = "[CSV] 중복된 id 발견: {0} (무시됨)";
    }
    // ===== UI 메시지 =====
    public static class Def_UI
    {
        // LocalizationManager 관련
        public const string LOCALIZATION_CSV_NOT_FOUND = "[Localization] CSV 파일을 찾을 수 없습니다: Resources/{0}.csv";
        public const string LOCALIZATION_CSV_EMPTY = "[Localization] CSV 파일이 비어있거나 헤더만 있습니다.";
        public const string LOCALIZATION_HEADER_INVALid = "[Localization] CSV 헤더가 올바르지 않습니다. 최소 KEY, LANG 필요.";
        public const string LOCALIZATION_DUPLICATE_KEY = "[Localization] 중복된 키 발견: {0} (무시됨)";
        public const string LOCALIZATION_LOADED = "[Localization] {0}개 키 로드 완료. 기본 언어: {1}";
        public const string LOCALIZATION_NO_DATA = "[Localization] 다국어 데이터가 로드되지 않았습니다!";
        public const string LOCALIZATION_FALLBACK_WARNING = "[Localization] '{0}'의 {1} 번역이 없어 기본 언어({2}) 사용";
        public const string LOCALIZATION_KEY_NOT_FOUND = "[Localization] 키를 찾을 수 없습니다: {0}";
        public const string LOCALIZATION_ALREADY_SET = "[Localization] 이미 {0} 언어가 설정되어 있습니다.";
        public const string LOCALIZATION_LANG_CHANGED = "[Localization] 언어 변경: {0}";

        // LocalizedText 관련
        public const string LOCALIZEDTEXT_NO_COMPONENT = "[LocalizedText] '{0}'에 Text 또는 TextMeshProUGUI 컴포넌트가 없습니다!";
        public const string LOCALIZEDTEXT_EMPTY_KEY = "[LocalizedText] '{0}'의 localizationKey가 비어있습니다!";
        public const string LOCALIZEDTEXT_MANAGER_NOT_INIT = "[LocalizedText] LocalizationManager가 초기화되지 않았습니다!";

        // Player 관련 디버그/로그
        public const string PLAYER_FORCE_STOP_LOADING = "[Player] F1 키: 강제로 로딩 해제!";
        public const string PLAYER_LOADING_WARNING = "[Player] 로딩 중... 이동 불가. (F1 키로 강제 해제 가능)";
        public const string PLAYER_MELEE_HIT = "[Player] 근거리 공격 성공! 대상: {0}";
        public const string PLAYER_SAVED_SCENE = "[Player] 게임씬 '{0}' 상태 저장 완료.";
        public const string PLAYER_SAVE_INVALid_SCENE = "[Player] 저장할 게임씬 이름이 유효하지 않음 → 저장 스킵.";
        public const string PLAYER_RESTORE_STATE = "플레이어 상태 복원: Scene={0}, Pos=({1:F2},{2:F2})";
        public const string PLAYER_SCENE_NOT_GAME = "[Player] '{0}'은 게임 컨텐츠 씬이 아님.";
        public const string PLAYER_SCENE_LOADED = "[Player] 씬 '{0}' 로드 완료 → 플레이어 초기화.";
        public const string PLAYER_SPAWN_APPLIED = "[Player] 맵 이동 스폰 id '{0}' 위치 적용: {1}";
        public const string PLAYER_SPAWN_NOT_FOUND = "[Player] 맵 이동 스폰 id '{0}'를 찾을 수 없습니다!";
        public const string PLAYER_CURRENT_SCENE_SAVED = "[Player] 현재 게임씬 이름 저장: {0}";
        public const string PLAYER_SAVED_POSITION_RESTORING = "[Player] 저장된 SubSceneData 위치를 복원합니다.";
        public const string PLAYER_SAVED_POSITION_NOT_FOUND = "[Player] 저장된 위치가 없어 씬 초기 위치로 설정합니다.";

        // NPC / Dialogue / Quest / Shop 관련
        public const string INTERACT_KEY_LABEL = "E";
        public const string NPC_INTERACT_AVAILABLE = "[NPC] 상호작용 가능: {0}키";
        public const string NPC_CONSOLE_HEADER = "[NPC] DialogueUIManager가 없음. 콘솔로 대화 출력:";
        public const string UI_INTERACT_HINT = "[UI] 상호작용 힌트: {0}";
        public const string DIALOGUE_ALREADY_INTERACTING = "[DialogueUI] 이미 상호작용 중입니다.";
        public const string DIALOGUE_PREFIX = "[Dialogue] ";
        public const string QUEST_OFFER_PREFIX = "[Quest Offer] ";
        public const string QUEST_NO_UI = "[Quest] 선택 UI 없음 - 자동 거부 처리";
        public const string SHOP_OPEN_PREFIX = "[Shop] 상점 열기: ";
        public const string QUEST_ACCEPTED_PREFIX = "[Quest Accepted] ";
        public const string QUEST_DECLINED_PREFIX = "[Quest Declined] ";

        // Steam / Cloud 관련
        public const string STEAMCLOUD_NO_DATA = "[SteamCloud] 저장할 데이터가 없습니다.";
        public const string STEAMCLOUD_NOT_INIT = "[SteamCloud] Steam이 초기화되지 않아 클라우드 저장을 건너뜁니다.";
        public const string STEAMCLOUD_UPLOAD_REQUEST = "[SteamCloud] 클라우드 업로드 요청: {0} ({1} bytes)";
        public const string STEAMCLOUD_SUCCESS = "클라우드 동기화 성공!";
        public const string STEAMCLOUD_FAIL = "클라우드 동기화 실패: Result: {0}";

        // Monster 관련
        public const string MONSTER_ATTACK = "[Monster] 플레이어 공격! 데미지: {0}";
        public const string MONSTER_TAKEDAMAGE = "[Monster] 데미지 받음: {0}, 남은 체력: {1}";
        public const string MONSTER_DIE = "[Monster] 사망";

        // 기타 공통 디버그 포맷
        public const string FORMAT_POS = "Pos=({0:F2},{1:F2})";
    }

    // ===== 디버그 로그 메시지 =====
    public static class Def_Debug
    {
        // CameraController
        public const string CAMERA_PLAYER_TARGET_SET = "[CameraController] 플레이어 타겟 설정 완료.";
        public const string CAMERA_PLAYER_NOT_FOUND = "[CameraController] 플레이어를 찾을 수 없습니다.";
        public const string CAMERA_REINIT_COMPLETE = "[CameraController] 씬 로드 후 카메라 초기 위치 재설정 및 바운드 적용 완료.";
        public const string CAMERA_REINIT_NO_TARGET = "[CameraController] ReInitialize 시 플레이어 타겟을 찾을 수 없습니다.";
        public const string CAMERA_WORLD_BOUNDS_SET = "[CameraController] 월드 경계 설정 완료: Min({0}), Max({1})";
        public const string CAMERA_NO_WORLD_BORDER = "[CameraController] 'WorldBorder' 태그를 가진 오브젝트를 찾을 수 없습니다! 카메라 경계 제한 비활성화.";
        public const string CAMERA_WORLD_BORDER_NO_COLLidER = "월드 보더 오브젝트에 Collider2D 컴포넌트가 없습니다!";

        // MapTransition
        public const string MAP_TRANSITION_TRIGGER = "[MapTransition] Trigger entered by '{0}', tag='{1}'";
        public const string MAP_TRANSITION_GO_SCENE = "[MapTransition] GoToNewScene called. targetScene='{0}', targetSpawnid='{1}', GameDataManager={2}";
        public const string MAP_TRANSITION_NO_MANAGER = "[MapTransition] GameDataManager.Instance가 null입니다. Resources에서 prefab 로드 시도.";
        public const string MAP_TRANSITION_PREFAB_LOADED = "[MapTransition] GameDataManager prefab 인스턴스화 완료.";
        public const string MAP_TRANSITION_NO_PREFAB = "[MapTransition] Resources/GameDataManager.prefab을 찾을 수 없습니다.";
        public const string MAP_TRANSITION_NO_TARGET = "[MapTransition] 이동할 씬 이름 또는 스폰 id가 설정되지 않았습니다!";
        public const string MAP_TRANSITION_SPAWN_SAVED = "[MapTransition] 다음 씬({0}) 스폰 id 저장: {1}";
        public const string MAP_TRANSITION_DIRECT_LOAD = "[MapTransition] GameDataManager.Instance가 null입니다. SceneManager로 직접 로드 시도.";

        // GameDataManager
        public const string GAME_DATA_SAVE_SKIP = "[GameDataManager] 저장할 게임씬 이름이 유효하지 않음 → 저장 스킵.";
        public const string GAME_DATA_SCENE_UNLOAD_START = "[GameDataManager] 기존 게임 씬 '{0}' 언로드 시작.";
        public const string GAME_DATA_SCENE_UNLOAD_COMPLETE = "[GameDataManager] 기존 게임 씬 '{0}' 언로드 완료.";
        public const string GAME_DATA_SCENE_UNLOAD_FAIL = "[GameDataManager] 씬 '{0}' 언로드 요청 실패 (AsyncOperation == null). 계속 진행합니다.";
        public const string GAME_DATA_SCENE_UNLOAD_ERROR = "[GameDataManager] 씬 언로드 시 예외: {0}";
        public const string GAME_DATA_SPAWN_FOUND = "[GameDataManager] 스폰 id '{0}' 발견. 위치: {1}";
        public const string GAME_DATA_SPAWN_MOVED = "[GameDataManager] 플레이어를 {0}로 이동.";
        public const string GAME_DATA_SPAWN_NOT_FOUND = "[GameDataManager] 스폰 id '{0}'를 찾을 수 없음.";
        public const string GAME_DATA_CAMERA_REINIT = "[GameDataManager] CameraController ReInitialize 완료.";

        // PlayerController
        public const string PLAYER_NOT_GAME_SCENE = "[Player] '{0}'은 게임 컨텐츠 씬이 아님.";
        public const string PLAYER_SCENE_LOADED = "[Player] 씬 '{0}' 로드 완료 → 플레이어 초기화.";
        public const string PLAYER_SCENE_NAME_SAVED = "[Player] 현재 게임씬 이름 저장: {0}";
        public const string PLAYER_MAP_SPAWN_APPLIED = "맵 이동 스폰 id '{0}' 위치 적용: {1}";
        public const string PLAYER_MAP_SPAWN_NOT_FOUND = "맵 이동 스폰 id '{0}'를 찾을 수 없습니다!";
        public const string PLAYER_RESTORE_SAVED_POS = "저장된 SubSceneData 위치를 복원합니다.";
        public const string PLAYER_NO_SAVED_POS = "저장된 위치가 없어 씬 초기 위치로 설정합니다.";
        public const string PLAYER_STATE_RESTORED = "플레이어 상태 복원: Scene={0}, Pos=({1:F2},{2:F2})";
        public const string PLAYER_STATE_SAVED = "[Player] 게임씬 '{0}' 상태 저장 완료.";
        public const string PLAYER_F1_FORCE_STOP = "[Player] F1 키: 강제로 로딩 해제!";
        public const string PLAYER_LOADING_BLOCKED = "[Player] 로딩 중... 이동 불가. (F1 키로 강제 해제 가능)";
        public const string PLAYER_ATTACK = "공격";
        public const string PLAYER_MELEE_SUCCESS = "[Player] 근거리 공격 성공! 대상: {0}";
        public const string PLAYER_NO_PROJECTILE = "[Player] 투사체 프리팹이 설정되지 않았습니다!";

        // MonsterController
        public const string MONSTER_DAMAGE = "[Monster] 데미지 받음: {0}, 남은 체력: {1}";
        public const string MONSTER_ATTACK_PLAYER = "[Monster] 플레이어 공격! 데미지: {0}";
        public const string MONSTER_DIED = "[Monster] 사망";

        // MonsterSpawnArea
        public const string SPAWN_AREA_NO_PREFAB = "[SpawnArea] 몬스터 프리팹이 설정되지 않았습니다!";
        public const string SPAWN_AREA_MAX_REACHED = "[SpawnArea] 최대 몬스터 수({0}) 도달. 스폰 중단.";
        public const string SPAWN_AREA_SPAWN_COMPLETE = "[SpawnArea] 몬스터 스폰 완료: {0}";
        public const string SPAWN_AREA_SHORTAGE = "[SpawnArea] 몬스터 부족 감지: {0}/{1}. {2}마리 스폰.";
        public const string SPAWN_AREA_MONSTER_DIED = "[SpawnArea] 몬스터 사망 알림 받음. 남은 몬스터: {0}/{1}";
        public const string SPAWN_AREA_ALL_CLEARED = "[SpawnArea] 모든 몬스터 제거 완료.";

        // LoadingScreenManager
        public const string LOADING_SHOW = "[Loading] 전역 로딩 화면 표시.";
        public const string LOADING_HidE = "[Loading] 전역 로딩 화면 숨김.";
        public const string LOADING_AUTO_HidE = "[Loading] {0}초 경과. 강제로 로딩 화면 숨김.";
        public const string LOADING_FORCE_STOP = "[Loading] 강제로 로딩 상태 해제!";

        // SecureSaveLoad
        public const string SECURE_KEY_GENERATED = "[SecureSave] 새로운 고유 암호화 키를 생성하고 스팀 클라우드에 저장했습니다.";
        public const string SECURE_KEY_SAVE_FAIL = "[SecureSave] 고유 암호화 키를 스팀 클라우드에 저장하는데 실패했습니다.";
        public const string SECURE_KEY_LOADED = "[SecureSave] 스팀 클라우드에서 고유 암호화 키를 성공적으로 로드했습니다.";
        public const string SECURE_KEY_LOAD_FAIL = "[SecureSave] 스팀 클라우드에서 고유 암호화 키를 읽는데 실패했습니다.";
        public const string SECURE_NO_KEY = "[SecureSave] 암호화 키가 없습니다. 암호화/복호화 실패.";
        public const string SECURE_SAVE_NO_KEY = "[SecureSave] 세이브 데이터를 저장할 수 없습니다. 암호화 키가 없습니다.";
        public const string SECURE_SAVE_COMPLETE = "[SecureSave] 로컬 저장 완료: {0}";
        public const string SECURE_SAVE_FAIL = "[SecureSave] 로컬 저장 실패: {0}";
        public const string SECURE_LOAD_LOCAL = "[SecureSave] 로컬 파일 로드: {0}";
        public const string SECURE_CLOUD_DOWNLOAD = "[SecureSave] Steam Cloud에서 파일 다운로드 성공: {0}";
        public const string SECURE_CLOUD_DOWNLOAD_FAIL = "[SecureSave] Steam Cloud 다운로드 실패: {0}";
        public const string SECURE_CACHE_COMPLETE = "[SecureSave] 로컬 캐시 저장 완료: {0}";
        public const string SECURE_CACHE_FAIL = "[SecureSave] 로컬 캐시 저장 실패 (로드는 계속): {0}";
        public const string SECURE_NO_FILE = "[SecureSave] 저장된 파일이 없습니다: {0}";
        public const string SECURE_LOAD_NO_KEY = "[SecureSave] 세이브 데이터를 로드할 수 없습니다. 암호화 키가 없습니다.";
        public const string SECURE_JSON_FAIL = "[SecureSave] JSON 파싱 실패: {0}";
        public const string SECURE_INTEGRITY_FAIL = "[SecureSave] 파일 무결성 검증 실패! 파일이 변조되었거나 교체되었습니다.";
        public const string SECURE_LOAD_SUCCESS = "[SecureSave] 파일 로드 및 무결성 검증 성공!";
        public const string SECURE_DELETE_LOCAL = "[SecureSave] 로컬 파일 삭제: {0}";
        public const string SECURE_DELETE_CLOUD = "[SecureSave] Steam Cloud 파일 삭제: {0}";

        // SteamCloudManager
        public const string STEAM_CLOUD_SUCCESS = "클라우드 동기화 성공!";
        public const string STEAM_CLOUD_FAIL = "클라우드 동기화 실패: Result: {0}";
        public const string STEAM_CLOUD_NO_DATA = "[SteamCloud] 저장할 데이터가 없습니다.";
        public const string STEAM_CLOUD_NOT_INIT = "[SteamCloud] Steam이 초기화되지 않아 클라우드 저장을 건너뜁니다.";
        public const string STEAM_CLOUD_UPLOAD = "[SteamCloud] 클라우드 업로드 요청: {0} ({1} bytes)";

        // LocalizationManager
        public const string LOC_FONT_LOAD_FAIL = "[Localization] Resources에서 Font '{0}'을(를) 찾을 수 없습니다.";
        public const string LOC_FONT_EXCEPTION = "[Localization] Font 로드 중 예외: {0}";
        public const string LOC_TMP_LOAD_FAIL = "[Localization] Resources에서 TMP_FontAsset '{0}'을(를) 찾을 수 없습니다.";
        public const string LOC_TMP_EXCEPTION = "[Localization] TMP_FontAsset 로드 중 예외: {0}";
    }

    // ===== 씬 카테고리 및 헬퍼 =====
    public enum SceneCategory
    {
        Unknown,
        Town,
        Field,
        Dungeon
    }
}