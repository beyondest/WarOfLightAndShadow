using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.State
{
    public class InteractAnimationEventSystemAuthoring : MonoBehaviour
    {
        [TableList]
        public List<AnimationEventInfoInspector> infos; 
        private class Baker : Baker<InteractAnimationEventSystemAuthoring>
        {
            public override void Bake(InteractAnimationEventSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<AnimationEventInfo>(entity);
                var dict = new Dictionary<string, List<AnimationEventInfo>>();
                
                foreach (var info in authoring.infos)
                {
                    if (info.sendVfxName == VFXName.None &&
                        info.animationInteractType == AnimationInteractType.VfxChangeStat)
                        throw new ArgumentException(
                            $"Interact animation event system config wrong, you set {info.eventName} {info.eventIndex}" +
                            $" to use vfx change stat, but you do not assign a vfx name");
                    if (!dict.ContainsKey(info.eventName))
                    {
                        dict.Add(info.eventName, new List<AnimationEventInfo>());
                    }

                    var list = dict[info.eventName];
                    list.Add(new AnimationEventInfo
                    {
                        eventName = info.eventName,
                        eventIndex = info.eventIndex,
                        animationInteractType = info.animationInteractType,
                        amountMultiplier = info.amountMultiplier,
                        sendVfxName = info.sendVfxName,
                        sendStatChangeRequestDelay = info.sendStatChangeRequestDelay,
                    });
                }

                foreach (var key in dict.Keys.ToList())
                {
                    dict[key].Sort((a, b) => a.eventIndex.CompareTo(b.eventIndex));
                }

                foreach (var pair in dict)
                {
                    foreach (var info in pair.Value)
                    {
                        buffer.Add(info);
                    }
                }
            }
        }
        
        
        
    }
    [Serializable]
    public struct AnimationEventInfoInspector
    {
        public string eventName;
        [Tooltip("Index 1 means the first event during a circle of attack animation")]
        public int eventIndex;
        [Tooltip("If none, then no vfx, only send stat change request")]
        public VFXName sendVfxName;
        [Tooltip("This multiplier used as different action cause a little different damage, if action raise attack events multiple times")]
        public float amountMultiplier;
        
        public AnimationInteractType animationInteractType ;
            
        [ShowIf(nameof(IsAoe)),Tooltip("This is for some ranged attack, wait for splash reach target")]
        public float sendStatChangeRequestDelay;

        private bool IsAoe()
        {
            return animationInteractType == AnimationInteractType.AoeBuffChangeStat;
        }
    }
    [Serializable]
    public struct AnimationEventInfo : IBufferElementData
    {
        public FixedString64Bytes eventName;
        [Tooltip("Index 1 means the first event during a circle of attack animation")]
        public int eventIndex;
        [Tooltip("If none, then no vfx, only send stat change request")]
        public VFXName sendVfxName;
        [Tooltip("This multiplier used as different action cause a little different damage, if action raise attack events multiple times")]
        public float amountMultiplier;
        
        [InfoBox("Notice that only projectile vfx can dealt damage")]
        public AnimationInteractType animationInteractType ;
            
        [ShowIf("@animationInteractType == AoeBuffChangeStat"),Tooltip("This is for some ranged attack, wait for splash reach target")]
        public float sendStatChangeRequestDelay;
    }

    public enum AnimationInteractType
    {
        VfxChangeStat = 0,
        AoeBuffChangeStat = 1,
        DirectlyChangeStat = 2
    }
}