using UnityEngine;
public class CharacterStateSaver : MonoBehaviour
{
    // 캐릭터 컴포넌트 참조 (Movement, Inventory 등)
    // ...

    // ------------------------------------------------------------------
    // A. 서브씬 비활성화 전 호출 (씬 껐다 켤 때)
    // ------------------------------------------------------------------
    public void SaveStateBeforeDeactivation()
    {
        SubSceneData dataToSave = new SubSceneData
        {
            positionX = transform.position.x,
            positionY = transform.position.y,
            positionZ = transform.position.z,
            health = 99, // 예시
            // ... 다른 상태 저장
        };

        CharacterSaveManager.Instance.SaveSubSceneState(dataToSave);
    }

    // ------------------------------------------------------------------
    // B. 씬 활성화 후 호출 (씬 껐다 켤 때, 또는 전체 로드 시)
    // ------------------------------------------------------------------
    private void Start()
    {
        // 씬 활성화 시 상태 로드 시작 (로딩 화면 로직이 별도로 없다면 Start에서 처리)
        // 서브씬 임시 데이터 로드
        SubSceneData savedData = CharacterSaveManager.Instance.LoadSubSceneState();
        RestoreSubSceneState(savedData);
    }

    public void RestoreSubSceneState(SubSceneData data)
    {
        // SubSceneData로 캐릭터 상태 복원
        transform.position = new Vector3(data.positionX, data.positionY, data.positionZ);
        // ... 체력, 인벤토리 등 복원
    }
}