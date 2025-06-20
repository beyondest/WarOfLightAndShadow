using UnityEngine;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Reflection;
using System.Collections.Generic;
using SparFlame.UI.General;

namespace Editor
{
 
    public class EventTriggerMigrator : MonoBehaviour
    {
        [MenuItem("Tools/Migrate EventTrigger.OnPointerEnter To CursorScreenSideTrigger")]
        public static void MigrateOnPointerEnter()
        {
            int migratedCount = 0;

            foreach (GameObject go in Selection.gameObjects)
            {
                var trigger = go.GetComponent<EventTrigger>();
                var cursorTrigger = go.GetComponent<LeftRightTrigger>();
                if (trigger == null || cursorTrigger == null)
                {
                    Debug.LogWarning($"GameObject '{go.name}' missing EventTrigger or CursorScreenSideTrigger");
                    continue;
                }

                foreach (var entry in trigger.triggers)
                {
                    if (entry.eventID != EventTriggerType.PointerEnter)
                        continue;

                    UnityEventBase sourceEvent = entry.callback;
                    UnityEvent targetEvent = cursorTrigger.onCursorLeftSide;

                    // 利用反射复制所有 persistent calls
                    int count = sourceEvent.GetPersistentEventCount();
                    for (int i = 0; i < count; i++)
                    {
                        Object target = sourceEvent.GetPersistentTarget(i);
                        string methodName = sourceEvent.GetPersistentMethodName(i);

                        if (string.IsNullOrEmpty(methodName) || target == null)
                            continue;

                        UnityAction action =
                            System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName) as UnityAction;
                        if (action != null)
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