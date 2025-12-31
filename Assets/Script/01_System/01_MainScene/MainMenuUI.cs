using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// 메인 메뉴 UI 관리

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;

    [Header("Scene Names")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelectScene";

    void Start()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnStartGameClicked()
    {
        Debug.Log("[MainMenu] 게임 시작 → 캐릭터 선택 화면으로 이동");
        SceneManager.LoadScene(characterSelectSceneName);
    }

    private void OnOptionsClicked()
    {
        Debug.Log("[MainMenu] 옵션 버튼 (추후 구현)");
    }

    private void OnExitClicked()
    {
        Debug.Log("[MainMenu] 게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}