using UnityEngine;
using SparFlame.Utils;
namespace SparFlame.BootStrapper
{
    public class SceneController : MonoBehaviour
    {
        public static SceneController instance;
        [SerializeField] private SceneGroup[] sceneGroups;
        [SerializeField] int awaitInterval = 100;
        
        
        
        private LoadingProgress _awaitProgress;
        public SceneGroupManager SceneGroupManager;


        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
            SceneGroupManager = new SceneGroupManager(awaitInterval);
            _awaitProgress = new LoadingProgress();
            
        }

        private void OnEnable()
        {

            SceneGroupManager.OnSceneUnloaded += reference => Debug.Log("Scene Unloaded: " + reference.Name);
            SceneGroupManager.OnSceneLoaded += reference => Debug.Log("Scene Loaded: " + reference.Name);  
            _awaitProgress.ProgressChanged+= f => Debug.Log("Group 1 loading progress : " + f);
        }

        private void Start()
        {
            Debug.Log("Scene groups 0 start loading");
            var _ = SceneGroupManager.LoadSceneGroupAsync(sceneGroups[0], _awaitProgress);
            // t.wait or t.result will cause deadlock
        }

        
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("All scene groups start unloading");
                var _ =  SceneGroupManager.UnloadSceneGroupAsync(sceneGroups[0]);
            }
        }



    }
}