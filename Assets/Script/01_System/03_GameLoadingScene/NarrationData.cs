using System;
using UnityEngine;

/// <summary>
/// 나레이션 진행 모드
/// </summary>
public enum NarrationMode
{
    Auto,        // 자동 진행 (타이핑 후 자동 넘김)
    Conditional  // 조건부 진행 (플레이어 행동 감지)
}

/// <summary>
/// 조건 타입 정의
/// </summary>
public enum NarrationConditionType
{
    None,              // 조건 없음 (Auto 모드용)
    Move,              // 플레이어 이동 감지
    OpenInventory,     // 인벤토리 열기
    OpenEquipment,     // 장비창 열기
    OpenQuest,         // 퀘스트창 열기
    OpenStat,          // 스텟창 열기
    Custom             // 커스텀 조건 (스크립트에서 직접 호출)
}

/// <summary>
/// 나레이션 설정 데이터
/// </summary>
[Serializable]
public class NarrationConfig
{
    [Header("식별자")]
    [Tooltip("DialogueDataManager에서 가져올 나레이션 ID")]
    public string narrationId;

    [Header("진행 모드")]
    public NarrationMode mode = NarrationMode.Auto;

    [Header("타이핑 효과 설정")]
    [Tooltip("타이핑 효과 사용 여부")]
    public bool useTypingEffect = true;

    [Tooltip("글자당 출력 시간(초) - 기본값 0.03초")]
    public float typingSpeed = 0.03f;

    [Tooltip("타이핑 완료 후 다음 대사까지 대기 시간(초) - 기본값 1.5초")]
    public float delayAfterTyping = 1.5f;

    [Header("조건부 진행 설정 (Conditional 모드)")]
    [Tooltip("다음 대사로 넘어가기 위한 조건")]
    public NarrationConditionType conditionType = NarrationConditionType.None;

    [Tooltip("조건 관련 추가 데이터 (예: 아이템ID, 위치 등)")]
    public string conditionData = "";

    [Header("스킵 설정")]
    [Tooltip("F키(Interact) 홀드로 스킵 가능 여부")]
    public bool canSkip = true;

    [Tooltip("F키를 눌러야 하는 시간(초) - 기본값 1.5초")]
    public float skipHoldDuration = 1.5f;
}