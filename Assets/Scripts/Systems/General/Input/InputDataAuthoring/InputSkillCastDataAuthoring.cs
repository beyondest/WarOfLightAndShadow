using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public class InputSkillCastDataAuthoring : MonoBehaviour
    {
        private class InputSkillCastDataAuthoringBaker : Baker<InputSkillCastDataAuthoring>
        {
            public override void Bake(InputSkillCastDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InputCastSkillData());
            }
        }
    }
}