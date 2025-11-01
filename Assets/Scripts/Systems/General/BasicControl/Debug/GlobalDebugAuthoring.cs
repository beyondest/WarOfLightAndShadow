using System;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GlobalDebugAuthoring : MonoBehaviour
    {
        [Title("General Debug Switch")] [GUIColor(1, 0.7f, 0.2f)]
        public bool globalDebugEnable;

        public EnableDebugInitSceneGroup enableEnableDebugInitSceneGroup;
        [FoldoutGroup("Movement Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public MovementDebug movement;

        [FoldoutGroup("Stat Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public StatDebug stat;

        [FoldoutGroup("Interact Ability Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public InteractAbilityDebug interactAbility;

        [FoldoutGroup("Old EnemyAI Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public OldEnemyAIDebug aiDebug;

        [FoldoutGroup("WaveDebug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public WaveDebug waveDebug;

        [FoldoutGroup("Exp Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public ExpDebug expDebug;

        [FoldoutGroup("Camera Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public CameraDebug cameraDebug;

        [FoldoutGroup("Enemy AI Main Gameplay Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public EnemyAIMainGameplayDebug enemyAIMainGameplayDebug;

        [FoldoutGroup("Conjure Speed Debug"),HideLabel][ShowIf(nameof(globalDebugEnable))]
        public ConjureDebug conjureSpeedDebug;
        
        [FoldoutGroup("Resource Debug"), HideLabel][ShowIf(nameof(globalDebugEnable))]
        public ResourceDebug resourceDebug;
        
        private class GlobalDebugAuthoringBaker : Baker<GlobalDebugAuthoring>
        {
            public override void Bake(GlobalDebugAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (authoring.globalDebugEnable)
                {
                    AddComponent<DebugTag>(entity);
                    if (authoring.movement.enabled)
                        AddComponent(entity, authoring.movement);
                    if (authoring.stat.enabled)
                        AddComponent(entity, authoring.stat);
                    if (authoring.interactAbility.enabled)
                        AddComponent(entity, authoring.interactAbility);
                    if (authoring.aiDebug.enabled)
                        AddComponent(entity, authoring.aiDebug);
                    if (authoring.waveDebug.enabled)
                        AddComponent(entity, authoring.waveDebug);
                    if (authoring.expDebug.enabled)
                        AddComponent(entity, authoring.expDebug);
                    if (authoring.cameraDebug.enabled)
                        AddComponent(entity, authoring.cameraDebug);
                    if(authoring.enemyAIMainGameplayDebug.enabled)
                        AddComponent(entity, authoring.enemyAIMainGameplayDebug);
                    if(authoring.conjureSpeedDebug.enabled)
                        AddComponent(entity, authoring.conjureSpeedDebug);
                    if(authoring.resourceDebug.enabled)
                        AddComponent(entity, authoring.resourceDebug);
                    AddComponent(entity, authoring.enableEnableDebugInitSceneGroup);
                }
            }
        }
    }
}