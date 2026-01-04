using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance {  get; private set; }

    [SerializeField] private GameObject narration_Panel;
    [SerializeField] private Button narration_Close_Button;
    [SerializeField] private TextMeshProUGUI narration_Text;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        narration_Panel.SetActive(false);
        if (narration_Close_Button != null)
            narration_Close_Button.onClick.AddListener(CloseNarrationUi);
    }

    public void CloseNarrationUi()
    {
        narration_Panel.SetActive(false);
    }

    public void ShowNarrationUi(string quest_id)
    {
        if (quest_id == null) return;

        bool show_Panel = false;
        narration_Text.text = "";
        if (quest_id == "Quest_002")
        {
            show_Panel = true;
            narration_Text.text = "I(i)키를 눌러서 아이템창을 열수 있습니다.\nQ(q)키를 눌러서 퀘스트창을 열수 있습니다.";
        }
        if (quest_id == "Quest_004")
        {
            show_Panel = true;
            narration_Text.text = "채집물에 가까이 가면 상호작용키가 표시 됩니다.\n상호작용키를 눌러서 채집을 할수 있습니다.";
        }
        if (quest_id == "Quest_006")
        {
            show_Panel = true;
            narration_Text.text = "E(e)키를 눌러서 장비창을 열수 있습니다.\n아이템창에서 장비를 드래그 하거나 더블클릭하여 장착 할수 있습니다.";
        }

        if(show_Panel)
        {
            narration_Panel.SetActive(true);
        }
    }

}
