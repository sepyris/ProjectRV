using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


/// Canvas와 UI 버튼의 클릭 영역 문제를 진단하고 수정하는 스크립트
/// Canvas 오브젝트에 붙여서 사용하세요.

public class CanvasClickFixDiagnostic : MonoBehaviour
{
    [Header("진단 설정")]
    [SerializeField] private bool showDebugOverlay = true;
    [SerializeField] private bool autoFixOnStart = true;

    [Header("Canvas Scaler 권장 설정")]
    [SerializeField] private CanvasScaler.ScaleMode recommendedScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private RectTransform canvasRect;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        canvasScaler = GetComponent<CanvasScaler>();
        canvasRect = GetComponent<RectTransform>();

        DiagnoseCanvas();

        if (autoFixOnStart)
        {
            FixCanvas();
        }
    }

    [ContextMenu("Canvas 진단")]
    public void DiagnoseCanvas()
    {
        Debug.Log("=== Canvas 클릭 영역 문제 진단 ===");

        if (canvas == null)
        {
            Debug.LogError(" Canvas 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        // 1. Canvas 설정 확인
        Debug.Log($"\n[Canvas 설정]");
        Debug.Log($"  Render Mode: {canvas.renderMode}");
        Debug.Log($"  Pixel Perfect: {canvas.pixelPerfect}");
        Debug.Log($"  Sort Order: {canvas.sortingOrder}");

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Debug.Log($"  Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "없음!")}");
            if (canvas.worldCamera == null)
            {
                Debug.LogWarning(" Render Mode가 Camera인데 카메라가 할당되지 않았습니다!");
            }
        }

        // 2. Canvas Scaler 확인
        if (canvasScaler == null)
        {
            Debug.LogWarning(" CanvasScaler가 없습니다! 이것이 문제의 원인일 수 있습니다.");
        }
        else
        {
            Debug.Log($"\n[Canvas Scaler 설정]");
            Debug.Log($"  UI Scale Mode: {canvasScaler.uiScaleMode}");

            if (canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                Debug.Log($"  Reference Resolution: {canvasScaler.referenceResolution}");
                Debug.Log($"  Screen Match Mode: {canvasScaler.screenMatchMode}");
                Debug.Log($"  Match: {canvasScaler.matchWidthOrHeight}");

                // 현재 화면 해상도와 비교
                float currentAspect = (float)Screen.width / Screen.height;
                float referenceAspect = canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y;
                Debug.Log($"  현재 화면 비율: {currentAspect:F2} (해상도: {Screen.width}x{Screen.height})");
                Debug.Log($"  레퍼런스 비율: {referenceAspect:F2}");

                if (Mathf.Abs(currentAspect - referenceAspect) > 0.2f)
                {
                    Debug.LogWarning(" 화면 비율과 레퍼런스 비율이 크게 다릅니다! 이것이 문제의 원인일 수 있습니다.");
                }
            }
            else if (canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                Debug.Log($"  Scale Factor: {canvasScaler.scaleFactor}");
            }
        }

        // 3. RectTransform 확인
        if (canvasRect != null)
        {
            Debug.Log($"\n[Canvas RectTransform]");
            Debug.Log($"  Size: {canvasRect.rect.width} x {canvasRect.rect.height}");
            Debug.Log($"  Scale: {canvasRect.localScale}");
            Debug.Log($"  Position: {canvasRect.localPosition}");

            if (canvasRect.localScale != Vector3.one)
            {
                Debug.LogWarning(" Canvas의 Scale이 (1,1,1)이 아닙니다! 클릭 영역 문제의 원인일 수 있습니다.");
            }
        }

        // 4. GraphicRaycaster 확인
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError(" GraphicRaycaster가 없습니다! UI 클릭이 불가능합니다.");
        }
        else
        {
            Debug.Log($"\n[GraphicRaycaster]");
            Debug.Log($"  활성화: {raycaster.enabled}");
            Debug.Log($"  Ignore Reversed Graphics: {raycaster.ignoreReversedGraphics}");
            Debug.Log($"  Blocking Objects: {raycaster.blockingObjects}");
        }

        // 5. 버튼 샘플 체크
        CheckButtonAlignment();
    }

    private void CheckButtonAlignment()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        Debug.Log($"\n[버튼 정렬 확인] 총 {buttons.Length}개");

        int misalignedCount = 0;
        foreach (Button button in buttons)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null) continue;

            // 버튼의 Scale 확인
            if (buttonRect.localScale != Vector3.one)
            {
                Debug.LogWarning($"   {button.gameObject.name}: Scale이 비정상 ({buttonRect.localScale})");
                misalignedCount++;
            }

            // 버튼의 Pivot 확인
            if (buttonRect.pivot != new Vector2(0.5f, 0.5f))
            {
                Debug.Log($"   {button.gameObject.name}: Pivot이 중앙이 아님 ({buttonRect.pivot})");
            }

            // Image raycastTarget 확인
            Image image = button.GetComponent<Image>();
            if (image != null && !image.raycastTarget)
            {
                Debug.LogWarning($"   {button.gameObject.name}: Image의 Raycast Target이 꺼져있음");
                misalignedCount++;
            }
        }

        if (misalignedCount > 0)
        {
            Debug.LogWarning($" {misalignedCount}개의 버튼에 문제가 발견되었습니다.");
        }
        else
        {
            Debug.Log(" 모든 버튼이 정상입니다.");
        }
    }

    [ContextMenu("Canvas 자동 수정")]
    public void FixCanvas()
    {
        Debug.Log("=== Canvas 자동 수정 시작 ===");

        // 1. CanvasScaler 추가 또는 수정
        if (canvasScaler == null)
        {
            Debug.Log("CanvasScaler 추가 중...");
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }

        // 권장 설정 적용
        canvasScaler.uiScaleMode = recommendedScaleMode;

        if (recommendedScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
            Debug.Log($" Canvas Scaler 설정: Scale With Screen Size ({referenceResolution.x}x{referenceResolution.y})");
        }

        // 2. Canvas RectTransform 수정
        if (canvasRect != null)
        {
            if (canvasRect.localScale != Vector3.one)
            {
                Debug.Log("Canvas Scale을 (1,1,1)로 수정 중...");
                canvasRect.localScale = Vector3.one;
            }

            if (canvasRect.localPosition != Vector3.zero)
            {
                Debug.Log("Canvas Position을 (0,0,0)로 수정 중...");
                canvasRect.localPosition = Vector3.zero;
            }
        }

        // 3. GraphicRaycaster 확인
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.Log("GraphicRaycaster 추가 중...");
            gameObject.AddComponent<GraphicRaycaster>();
        }
        else if (!raycaster.enabled)
        {
            raycaster.enabled = true;
            Debug.Log("GraphicRaycaster 활성화");
        }

        // 4. 모든 버튼 수정
        FixAllButtons();

        Debug.Log(" Canvas 자동 수정 완료!");

        // 수정 후 다시 진단
        DiagnoseCanvas();
    }

    private void FixAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        Debug.Log($"버튼 수정 중... (총 {buttons.Length}개)");

        int fixedCount = 0;
        foreach (Button button in buttons)
        {
            bool wasFixed = false;

            // RectTransform Scale 수정
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null && buttonRect.localScale != Vector3.one)
            {
                buttonRect.localScale = Vector3.one;
                wasFixed = true;
            }

            // Image raycastTarget 수정
            Image image = button.GetComponent<Image>();
            if (image != null && !image.raycastTarget)
            {
                image.raycastTarget = true;
                wasFixed = true;
            }

            // Button interactable 확인
            if (!button.interactable)
            {
                Debug.LogWarning($"   {button.gameObject.name}: Interactable이 꺼져있습니다 (수동 확인 필요)");
            }

            if (wasFixed)
            {
                fixedCount++;
                Debug.Log($"   {button.gameObject.name} 수정 완료");
            }
        }

        Debug.Log($" {fixedCount}개의 버튼 수정 완료!");
    }

    void OnGUI()
    {
        if (!showDebugOverlay) return;

        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 400));
        GUILayout.Box("=== Canvas 클릭 진단 ===");

        if (canvas != null)
        {
            GUILayout.Label($"Canvas: {canvas.gameObject.name}");
            GUILayout.Label($"Render Mode: {canvas.renderMode}");
        }

        if (canvasScaler != null)
        {
            GUILayout.Label($"\nCanvas Scaler:");
            GUILayout.Label($"  Mode: {canvasScaler.uiScaleMode}");
            if (canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                GUILayout.Label($"  Ref: {canvasScaler.referenceResolution}");
            }
        }

        GUILayout.Label($"\n화면: {Screen.width}x{Screen.height}");
        GUILayout.Label($"마우스: {Input.mousePosition}");

        // 마우스 위치의 UI 확인
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = Input.mousePosition;

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);

            GUILayout.Label($"\n호버 중: {raycastResults.Count}개");
            if (raycastResults.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(3, raycastResults.Count); i++)
                {
                    GUILayout.Label($"  {raycastResults[i].gameObject.name}");
                }
            }
        }

        if (GUILayout.Button("Canvas 진단"))
        {
            DiagnoseCanvas();
        }

        if (GUILayout.Button("Canvas 자동 수정"))
        {
            FixCanvas();
        }

        GUILayout.EndArea();
    }

    // 특정 버튼의 클릭 영역을 시각화
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (!button.gameObject.activeInHierarchy) continue;

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null) continue;

            // 버튼의 월드 공간 코너들
            Vector3[] corners = new Vector3[4];
            buttonRect.GetWorldCorners(corners);

            // 클릭 영역 시각화 (녹색 박스)
            Gizmos.color = Color.green;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
        }
    }
}