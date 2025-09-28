using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using SparFlame.UI.General;

namespace Editor
{
 
    public class EventTriggerMigrator : MonoBehaviour
    {
        [MenuItem("Tools/Migrate EventTrigger.OnPointerEnter To CursorScreenSideTrigger")]
        public static void MigrateOnPointerEnter()
        {
            var migratedCount = 0;

            foreach (var go in Selection.gameObjects)
            {
                var trigger = go.GetComponent<EventTrigger>();
                var cursorTrigger = go.GetComponent<LeftRightTrigger>();
                if (!trigger || !cursorTrigger)
                {
                    Debug.LogWarning($"GameObject '{go.name}' missing EventTrigger or CursorScreenSideTrigger");
                    continue;
                }

                foreach (var entry in trigger.triggers)
                {
                    if (entry.eventID != EventTriggerType.PointerEnter)
                        continue;

                    UnityEventBase sourceEvent = entry.callback;
                    var targetEvent = cursorTrigger.onCursorInLeftSide;

                    // 利用反射复制所有 persistent calls
                    var count = sourceEvent.GetPersistentEventCount();
                    for (var i = 0; i < count; i++)
                    {
                        var target = sourceEvent.GetPersistentTarget(i);
                        var methodName = sourceEvent.GetPersistentMethodName(i);

                        if (string.IsNullOrEmpty(methodName) || !target)
                            continue;

                        if (System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName) is UnityAction action)
                        {
                            UnityEditor.Events.UnityEventTools.AddPersistentListener(targetEvent, action);
                            migratedCount++;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"Failed to create delegate for method '{methodName}' on object '{target.name}'");
                        }
                    }
                }

                // 可以选择移除旧的 EventTrigger
                // DestroyImmediate(trigger, true);
            }

            Debug.Log(
                $"✅ Migrated {migratedCount} event(s) from EventTrigger.OnPointerEnter to CursorScreenSideTrigger.onCursorLeftSide.");
        }
    }
}