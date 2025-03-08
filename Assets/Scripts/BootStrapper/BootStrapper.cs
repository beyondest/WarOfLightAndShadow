// using UnityEngine;
// using UnityEngine.SceneManagement;
// namespace SparFlame.BootStrapper
// {
//     public class BootStrapper : PersistentSingleton<BootStrapper>
//     {
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//         static async void Init()
//         {
//             Debug.Log("BootStrapper init");
//             await SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
//         }
//     }
// }