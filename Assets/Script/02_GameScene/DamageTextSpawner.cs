using UnityEngine;

/// <summary>
/// 데미지 텍스트 생성을 위한 헬퍼 클래스
/// 캐릭터나 몬스터 컴포넌트에서 사용
/// </summary>
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

    /// <summary>
    /// 일반 데미지 표시
    /// </summary>
    public void ShowDamage(int damage)
    {
        ShowDamage(damage, false);
    }

    /// <summary>
    /// 데미지 표시 (크리티컬 여부 포함)
    /// </summary>
    public void ShowDamage(int damage, bool isCritical)
    {
        Color color = isCritical ? criticalDamageColor : normalDamageColor;
        SpawnDamageText(damage, color, isCritical);
    }

    /// <summary>
    /// 힐 표시
    /// </summary>
    public void ShowHeal(int healAmount)
    {
        SpawnTextMessage($"+{healAmount}", healColor);
    }

    /// <summary>
    /// 회피 표시
    /// </summary>
    public void ShowMiss()
    {
        SpawnTextMessage("MISS", missColor);
    }

    /// <summary>
    /// 커스텀 텍스트 표시
    /// </summary>
    public void ShowCustomText(string text, Color color)
    {
        SpawnTextMessage(text, color);
    }

    /// <summary>
    /// 데미지 텍스트 생성
    /// </summary>
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

    /// <summary>
    /// 텍스트 메시지 생성
    /// </summary>
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

    /// <summary>
    /// 스폰 위치 계산
    /// </summary>
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