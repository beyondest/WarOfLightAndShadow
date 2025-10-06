using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public class AnimationStateChangeSystemAuthoring : MonoBehaviour
    {
        [TableList]
        public List<UnitTypeToAnimationStateToSpeedScalePair> orderedList;
        private class
            AnimationStateChangeSystemAuthoringBaker : Baker<AnimationStateChangeSystemAuthoring>
        {
            public override void Bake(AnimationStateChangeSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var buffer = AddBuffer<UnitTypeToAnimationStateToSpeedScalePair>(entity);
                var dict = new Dictionary<UnitType, List<UnitTypeToAnimationStateToSpeedScalePair>>();
                foreach (var pair in authoring.orderedList)
                {
                    if(!dict.ContainsKey(pair.unitType))
                        dict.Add(pair.unitType, new List<UnitTypeToAnimationStateToSpeedScalePair>());
                    var list = dict[pair.unitType];
                    list.Add(pair);
                }

                foreach (var list in dict.Values)
                {
                    for (var index = 0; index < list.Count; index++)
                    {
                        var pair = list[index];
                        if ((int)pair.state != index)
                            throw new ArgumentException(
                                "Animation State Speed Scale Config Wrong, animation state must be ordered ");
                    }
                }
                
                
                foreach (var pair in authoring.orderedList)
                {
                    buffer.Add(pair);
                }
            }
        }
        
        
    }

    [Serializable]
    public struct UnitTypeToAnimationStateToSpeedScalePair : IBufferElementData
    {
        public UnitType unitType;
        public UnitAnimationState state;
        public float speedScale;
        
    }
}