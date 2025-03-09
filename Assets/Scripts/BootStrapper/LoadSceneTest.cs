using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.BootStrapper
{
    public class LoadSceneTest : MonoBehaviour
    {
        private LoadingProgress _progress;
        private void Start()
        {
            SceneController.instance.OnSceneGroupLoaded += (sceneGroup => Debug.Log($"SceneGroup {sceneGroup.groupName} start loading "));
            _progress = new LoadingProgress();
            _progress.ProgressChanged += f => Debug.Log($"SceneGroup progress changed {f}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                SceneController.instance.LoadSceneGroup("0",_progress);
            if(Input.GetKeyDown(KeyCode.U))
                SceneController.instance.UnloadSceneGroup("0");

        }
        
        
    }
}