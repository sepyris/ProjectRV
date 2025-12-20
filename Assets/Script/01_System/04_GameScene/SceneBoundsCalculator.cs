using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBoundsCalculator : MonoBehaviour
{
    void Start()
    {
        // 현재 씬의 경계 계산 및 출력
        Bounds sceneBounds = CalculateSceneBounds();
    }

    public Bounds CalculateSceneBounds()
    {
        // 현재 활성화된 씬을 가져옴
        Scene currentScene = SceneManager.GetActiveScene();

        // 초기 경계는 무한대로 설정
        Bounds bounds = new Bounds();
        bool firstRenderer = true;

        // 씬의 모든 루트 게임 오브젝트를 순회
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            // 자식 오브젝트를 포함한 모든 Renderer 컴포넌트를 가져옴
            Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // 첫 번째 렌더러의 경계로 초기화
                if (firstRenderer)
                {
                    bounds = renderer.bounds;
                    firstRenderer = false;
                }
                // 이후 렌더러의 경계를 현재 경계에 포함시킴
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return bounds;
    }
}