using UnityEngine;


/// 데미지 텍스트 생성을 위한 헬퍼 클래스
/// 캐릭터나 몬스터 컴포넌트에서 사용

public class DamageTextSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject damageTextPrefab; // FloatingDamageText 프리팹

    [Header("Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(0f, 2f, 0f); // 오브젝트 위쪽 오프셋
    public Transform spawnPoint; // 커스텀 스폰 위치 (선택사항)

    [Header("Damage Colors")]
    public Color normalDamageColor = Color.white;
    public Color criticalDamageColor = Color.yellow;
    public Color healColor = Color.green;
    public Color missColor = Color.gray;

    
    /// 일반 데미지 표시
    
    public void ShowDamage(int damage)
    {
        ShowDamage(damage, false);
    }

    
    /// 데미지 표시 (크리티컬 여부 포함)
    
    public void ShowDamage(int damage, bool isCritical)
    {
        Color color = isCritical ? criticalDamageColor : normalDamageColor;
        SpawnDamageText(damage, color, isCritical);
    }

    
    /// 힐 표시
    
    public void ShowHeal(int healAmount)
    {
        SpawnTextMessage($"+{healAmount}", healColor);
    }

    
    /// 회피 표시
    
    public void ShowMiss()
    {
        SpawnTextMessage("MISS", missColor);
    }

    
    /// 커스텀 텍스트 표시
    
    public void ShowCustomText(string text, Color color)
    {
        SpawnTextMessage(text, color);
    }

    
    /// 데미지 텍스트 생성
    
    private void SpawnDamageText(int damage, Color color, bool isCritical)
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("[DamageTextSpawner] 데미지 텍스트 프리팹이 설정되지 않았습니다!");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition();
        GameObject textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);

        FloatingDamageText damageText = textObj.GetComponent<FloatingDamageText>();
        if (damageText != null)
        {
            damageText.Show(damage, color, isCritical);
        }
    }

    
    /// 텍스트 메시지 생성
    
    private void SpawnTextMessage(string text, Color color)
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("[DamageTextSpawner] 데미지 텍스트 프리팹이 설정되지 않았습니다!");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition();
        GameObject textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);

        FloatingDamageText damageText = textObj.GetComponent<FloatingDamageText>();
        if (damageText != null)
        {
            damageText.Show(text, color);
        }
    }

    
    /// 스폰 위치 계산
    
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        else
        {
            return transform.position + spawnOffset;
        }
    }
}