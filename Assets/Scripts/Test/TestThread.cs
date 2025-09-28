// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using UnityEngine;
//
// public class AsyncThreadTest : MonoBehaviour
// {
//     int mainThreadId;
//
//     void Awake()
//     {
//         mainThreadId = Thread.CurrentThread.ManagedThreadId;
//         Debug.Log($"[Awake] mainThreadId = {mainThreadId}");
//     }
//
//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.A))
//         {
//             Debug.Log($"[Update] KeyDown A - ThreadId={Thread.CurrentThread.ManagedThreadId} (main? {IsMainThread()})");
//
//             // 实际结果： DoA=>DoB和DoC=>DoD两条链可以并行，如果DoA中有await，DoC有可能在DoA结束前进行，尽管两者都在主线程
//             _ = DoA().ContinueWith(_ => DoB(), TaskScheduler.Default);
//             _ = DoC().ContinueWith(_ => DoD(), TaskScheduler.Default);
//         }
//     }
//
//     bool IsMainThread() => Thread.CurrentThread.ManagedThreadId == mainThreadId;
//
//     void Log(string who, string stage)
//     {
//         Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] {who} - {stage} - ThreadId={Thread.CurrentThread.ManagedThreadId} (main? {IsMainThread()})");
//     }
//
//     // 示例：DoA 在开始处做同步（主线程），然后 await Task.Delay -> 恢复可能在线程池，
//     // 再通过 Task.Run 做明显在后台的工作
//     private async Task DoA()
//     {
//         Log("DoA", "start (sync before first await)");
//         // 少量同步工作（会在调用线程，也就是 Update 的主线程部分执行）
//         for (int i = 0; i < 1000; i++) Thread.SpinWait(10);
//
//         await Task.Delay(1000); // yield：完成后通常在线程池线程恢复
//         Log("DoA", "after await Task.Delay (resumed)");
//
//         // 模拟 CPU 密集或长耗时，放到线程池（Task.Run）
//         await Task.Run(() =>
//         {
//             Log("DoA", "inside Task.Run start");
//             Thread.Sleep(800);
//             Log("DoA", "inside Task.Run end");
//         });
//
//         Log("DoA", "end");
//     }
//
//     // DoB 会由 ContinueWith 在线程池触发（取决于 ContinueWith 的调度器）
//     private async Task DoB()
//     {
//         Log("DoB", "start");
//         await Task.Run(() =>
//         {
//             Log("DoB", "Task.Run start");
//             Thread.Sleep(1200);
//             Log("DoB", "Task.Run end");
//         });
//         Log("DoB", "end");
//     }
//
//     private async Task DoC()
//     {
//         Log("DoC", "start (sync before first await)");
//         // 比 DoA 更短的 await，观察顺序差异
//         await Task.Delay(500);
//         Log("DoC", "after await Task.Delay (resumed)");
//
//         await Task.Run(() =>
//         {
//             Log("DoC", "Task.Run start");
//             Thread.Sleep(400);
//             Log("DoC", "Task.Run end");
//         });
//
//         Log("DoC", "end");
//     }
//
//     private async Task DoD()
//     {
//         Log("DoD", "start");
//         await Task.Run(() =>
//         {
//             Log("DoD", "Task.Run start");
//             Thread.Sleep(700);
//             Log("DoD", "Task.Run end");
//         });
//         Log("DoD", "end");
//     }
// }
