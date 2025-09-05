using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public class AnimationStateChnageSystemAuthoring : MonoBehaviour
    {
        public List<UnitTypeToAnimationStateToSpeedScalePair> orderedList;
        private class
            AnimationStateChnageSystemAuthoringBaker : Baker<AnimationStateChnageSystemAuthoring>
        {
            public override void Bake(AnimationStateChnageSystemAuthoring authoring)
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