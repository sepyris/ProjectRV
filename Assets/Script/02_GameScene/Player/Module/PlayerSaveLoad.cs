using UnityEngine;
using UnityEngine.SceneManagement;
using Definitions;
public class PlayerSaveLoad
{
    private Transform playerTransform;

    public PlayerSaveLoad(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
    }

    public void InitializePlayerState(string sceneName)
    {
        if (CharacterSaveManager.Instance == null)
            return;
        string targetSpawnid = CharacterSaveManager.Instance.NextSceneSpawnPointid;
        SubSceneData data = CharacterSaveManager.Instance.LoadSubSceneState();
        data.currentSceneName = sceneName;
        bool hasSavedPos = data.positionX != 0 || data.positionY != 0 || data.positionZ != 0;
        if (!string.IsNullOrEmpty(targetSpawnid)) {

            Vector3 spawn = Vector3.zero;
            foreach (var point in GameObject.FindObjectsOfType<MapSpawnPoint>()) {
                if (point.spawnPointid == targetSpawnid) {
                    spawn = point.transform.position;
                    break;
                }
            }
            if (spawn != Vector3.zero) {
                playerTransform.position = spawn;
                //GameDataManager.Instance.nextSceneSpawnPointid = "";
                data.positionX = spawn.x;
                data.positionY = spawn.y;
                data.positionZ = spawn.z;
            }
        }
        else if (!hasSavedPos) {
            data.positionX = playerTransform.position.x;
            data.positionY = playerTransform.position.y;
            data.positionZ = playerTransform.position.z;
            if (data.health == 0)
                data.health = 100;
        }
        RestoreSubSceneState(data);
    }

    public void SaveStateBeforeDeactivation()
    {
        if (CharacterSaveManager.Instance == null)
            return;
        string sceneName = "";
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name.StartsWith(Def_Name.SCENE_NAME_START_MAP)) {
                sceneName = scene.name;
                break;
            }
        }
        if (string.IsNullOrEmpty(sceneName))
            return;
        SubSceneData data = new SubSceneData {
            currentSceneName = sceneName,
            positionX = playerTransform.position.x,
            positionY = playerTransform.position.y,
            positionZ = playerTransform.position.z,
            health = 99,
            //inventoryItems = new System.Collections.Generic.List<string>()
        };
        CharacterSaveManager.Instance.SaveSubSceneState(data);
    }

    public void RestoreSubSceneState(SubSceneData data) {
        playerTransform.position = new Vector3(data.positionX, data.positionY, data.positionZ);
    }
}