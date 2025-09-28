using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public static class SavableTypeCheckOut
    {
        // Savable component types
        public static readonly Dictionary<string, Type> SavableNamesToType = new()
        {
            { nameof(NeedSaveTag), typeof(NeedSaveTag) },
            { nameof(PrefabId), typeof(PrefabId) },
            {nameof(Rnd), typeof(Rnd)},
            // General Component
            { nameof(GlobalSingleId), typeof(GlobalSingleId) },
            { nameof(SubGameplayGeneralAttr), typeof(SubGameplayGeneralAttr) },
            { nameof(UnitAttr), typeof(UnitAttr) },
            { nameof(LocalTransform), typeof(LocalTransform) },
            { nameof(ExpData), typeof(ExpData) },
            { nameof(StatData), typeof(StatData) },
            { nameof(MovableData), typeof(MovableData) },

            // Unit Conditional Component
            { nameof(InGarrison), typeof(InGarrison) },
            { nameof(InArmyGroup), typeof(InArmyGroup) },
            { nameof(AttackAbility), typeof(AttackAbility) },
            { nameof(HealAbility), typeof(HealAbility) },
            { nameof(HarvestAbility), typeof(HarvestAbility) },
            {nameof(FormationTransform), typeof(FormationTransform)},

            // Building Component & Conditional Component
            { nameof(BuildingAttr), typeof(BuildingAttr) },
            { nameof(ConstructingTimer), typeof(ConstructingTimer) },
            {nameof(CrystalDef), typeof(CrystalDef)},

            // Building Buffer
            { nameof(GarrisonEntity), typeof(GarrisonEntity) },
            { nameof(GarrisonTypeData), typeof(GarrisonTypeData) },
            { nameof(ConjuringData), typeof(ConjuringData) },

            // City Component
            { nameof(MainGameplayGeneralAttr), typeof(MainGameplayGeneralAttr) },
            { nameof(HpRegenerateTimer), typeof(HpRegenerateTimer) },
            { nameof(CityAIData), typeof(CityAIData) },
            {nameof(CityGarrisonAttr), typeof(CityGarrisonAttr)},


            // City Buffer
            { nameof(CityGarrisonEntity), typeof(CityGarrisonEntity) },
            { nameof(CityResourceEntry), typeof(CityResourceEntry) },
            { nameof(CityTask), typeof(CityTask) },
            { nameof(CityFutureInvaders), typeof(CityFutureInvaders) },
            { nameof(ExtraArmyGroup), typeof(ExtraArmyGroup) },
            { nameof(AttackArmyGroup), typeof(AttackArmyGroup) },
            { nameof(DefendArmyGroup), typeof(DefendArmyGroup) },
            { nameof(InvadingArmyGroup), typeof(InvadingArmyGroup) },
            { nameof(InvadeTarget), typeof(InvadeTarget) },
            { nameof(ArmyGroupConjureStack), typeof(ArmyGroupConjureStack) },

            // Army Group Component
            { nameof(ArmyGroupAttr), typeof(ArmyGroupAttr) },
            { nameof(ArmyGroupStatData), typeof(ArmyGroupStatData) },
            { nameof(ArmyGroupMovableData), typeof(ArmyGroupMovableData) },
            { nameof(ArmyGroupPathVisualizeData), typeof(ArmyGroupPathVisualizeData) },
            { nameof(LastPassingByCity), typeof(LastPassingByCity) },
            { nameof(ArmyGroupStateData), typeof(ArmyGroupStateData) },
            { nameof(ArmyGroupThreatenData), typeof(ArmyGroupThreatenData) },
            {nameof(ArmyGroupInGarrison), typeof(ArmyGroupInGarrison)},

            // Army Group Buffer
            { nameof(ArmyGroupMovingTarget), typeof(ArmyGroupMovingTarget) },
            { nameof(ArmyGroupFinalWayPoint), typeof(ArmyGroupFinalWayPoint) },
            { nameof(ArmyGroupUnit), typeof(ArmyGroupUnit) },
            { nameof(ArmyGroupUnitTypeData), typeof(ArmyGroupUnitTypeData) },

            // Army Group Conditional Component
            { nameof(EnemyArmyGroupBelongsToCity), typeof(EnemyArmyGroupBelongsToCity) },

            // Singleton Component
            { nameof(PlayerFactionData), typeof(PlayerFactionData) },
            { nameof(PopulationResourceData), typeof(PopulationResourceData) },
            { nameof(WorldTimeData), typeof(WorldTimeData) },
            { nameof(CameraMainGameplayHistory), typeof(CameraMainGameplayHistory) },
            { nameof(GlobalSingIDCounter), typeof(GlobalSingIDCounter) },

            // Singleton Buffer
            { nameof(ResourceData), typeof(ResourceData) },
            { nameof(PopulationConjureTask), typeof(PopulationConjureTask) },
            { nameof(PopulationStorageAddTask), typeof(PopulationStorageAddTask) },
        };
    }

    [Serializable]
    public class SaveArcheTypeInfo
    {
        // Serializable fields
        public SaveArcheType saveArcheType;
        public SaveEntityType saveEntityType;

        [SerializeField, ShowIf(nameof(IsNotSingleton))]
        private List<string> queryWithAll;

        [SerializeField, ShowIf(nameof(IsNotSingleton))]
        private List<string> queryWithNone;

        [SerializeField] private List<string> fixedComponentTypeNames = new();
        [SerializeField] private List<string> fixedBufferTypeNames = new();
        [SerializeField] private List<string> conditionalComponentTypeNames = new();
        [SerializeField] private List<string> conditionalBufferTypeNames = new();

        // Cache
        private List<Type> _types;
        private List<ComponentType> _queryWithAllComponentTypes;
        private List<ComponentType> _queryWithNoneComponentTypes;

        private List<Type> _bufferTypes;
        private List<Type> _conditionalBufferTypes;
        private List<Type> _conditionalComponentTypes;

        // Interfaces
        public List<Type> Types => _types;
        public List<ComponentType> QueryWithAllComponentTypes => _queryWithAllComponentTypes;
        public List<ComponentType> QueryWithNoneComponentTypes => _queryWithNoneComponentTypes;
        public List<Type> BufferTypes => _bufferTypes;
        public List<Type> ConditionalBufferTypes => _conditionalBufferTypes;
        public List<Type> ConditionalComponentTypes => _conditionalComponentTypes;

        public bool IsNotSingleton()
        {
            return saveEntityType == SaveEntityType.Prefab;
        }

        public void RebuildCache()
        {
            if (conditionalBufferTypeNames.Count > 0 || conditionalComponentTypeNames.Count > 0)
            {
                if (saveEntityType == SaveEntityType.Singleton)
                    throw new ArgumentException(
                        "Singleton entity save mode does not support conditional component or buffer");
            }

            if (fixedComponentTypeNames.Count == 0)
                throw new ArgumentException("Fixed component type name list is empty");

            BuildQueryTypeCache();

            BuildSaveTypeCache();
        }

        private void BuildSaveTypeCache()
        {
            _types = fixedComponentTypeNames
                .Select(name =>
                {
                    if (!SavableTypeCheckOut.SavableNamesToType.TryGetValue(name, out var type))
                    {
                        throw new ArgumentException(
                            $"Invalid component type name: {name}, please add to static dictionary in authoring script");
                    }

                    return type;
                })
                .Where(t => t != null)
                .ToList();


            _bufferTypes = fixedBufferTypeNames
                .Select(name =>
                {
                    if (!SavableTypeCheckOut.SavableNamesToType.TryGetValue(name, out var type))
                    {
                        throw new ArgumentException(
                            $"Invalid component type name: {name}, please add to static dictionary in authoring script");
                    }

                    return type;
                })
                .Where(t => t != null)
                .ToList();

            _conditionalComponentTypes = conditionalComponentTypeNames
                .Select(name =>
                {
                    if (!SavableTypeCheckOut.SavableNamesToType.TryGetValue(name, out var type))
                    {
                        throw new ArgumentException(
                            $"Invalid component type name: {name}, please add to static dictionary in authoring script");
                    }

                    return type;
                })
                .Where(t => t != null)
                .ToList();

            _conditionalBufferTypes = conditionalBufferTypeNames
                .Select(name =>
                {
                    if (!SavableTypeCheckOut.SavableNamesToType.TryGetValue(name, out var type))
                    {
                        throw new ArgumentException(
                            $"Invalid component type name: {name}, please add to static dictionary in authoring script");
                    }

                    return type;
                })
                .Where(t => t != null)
                .ToList();
        }

        private void BuildQueryTypeCache()
        {
            _queryWithAllComponentTypes = new List<ComponentType>();
            _queryWithNoneComponentTypes = new List<ComponentType>();
            if (_queryWithAllComponentTypes.Contains(typeof(PrefabId)))
                throw new ArgumentException(
                    "PrefabId is not allowed in types, if it needs prefabId, check the hasPrefabId");

            var list = queryWithAll
                .Select(name =>
                {
                    if (!SavableTypeCheckOut.SavableNamesToType.TryGetValue(name, out var type))
                    {
                        throw new ArgumentException(
                            $"Invalid component type name: {name}, please add to static dictionary in authoring script");
                    }

                    return type;
                })
                .Where(t => t != null)
                .ToList();
            _queryWithAllComponentTypes = list
                .Select(ComponentType.ReadWrite)
                .ToList();
            list.Clear();
            list = queryWithNone
                .Select(name =>
                {
                    if (!SavableTypeCheckOut.SavableNamesToType.TryGetValue(name, out var type))
                    {
                        throw new ArgumentException(
                            $"Invalid component type name: {name}, please add to static dictionary in authoring script");
                    }

                    return type;
                })
                .Where(t => t != null)
                .ToList();
            _queryWithNoneComponentTypes = list
                .Select(ComponentType.ReadWrite)
                .ToList();
        }
    }
}

/*#region CheckOut

public static class AllComponentTypes
{
    public static readonly List<Type> Types = new()
    {
        typeof(GlobalSingleId),
        typeof(SubGameplayGeneralAttr),
        typeof(UnitAttr),
        typeof(LocalTransform),
        typeof(PhysicsMass),
        typeof(ExpData),
        typeof(StatData),
        typeof(MovableData),

        // Unit 条件组件
        typeof(InGarrison),
        typeof(InArmyGroup),
        typeof(AttackAbility),
        typeof(HealAbility),
        typeof(HarvestAbility),

        // Building 组件 & 条件组件
        typeof(BuildingAttr),
        typeof(ConstructingTimer),

        // Building Buffer
        typeof(GarrisonEntity),
        typeof(GarrisonTypeData),
        typeof(ConjuringData),

        // City 组件
        typeof(MainGameplayGeneralAttr),
        typeof(HpRegenerateTimer),
        typeof(CityAIData),

        // City Buffer
        typeof(CityGarrisonEntity),
        typeof(CityResourceEntry),
        typeof(CityTask),
        typeof(CityFutureInvaders),
        typeof(ExtraArmyGroup),
        typeof(AttackArmyGroup),
        typeof(DefendArmyGroup),
        typeof(InvadingArmyGroup),
        typeof(InvadeTarget),
        typeof(ArmyGroupConjureStack),

        // Army Group 组件
        typeof(ArmyGroupAttr),
        typeof(ArmyGroupStatData),
        typeof(ArmyGroupMovableData),
        typeof(ArmyGroupPathVisualizeData),
        typeof(LastPassingByCity),
        typeof(ArmyGroupStateData),
        typeof(ArmyGroupThreatenData),

        // Army Group Buffer
        typeof(ArmyGroupMovingTarget),
        typeof(ArmyGroupFinalWayPoint),
        typeof(ArmyGroupUnit),
        typeof(ArmyGroupUnitTypeData),

        // Army Group 条件组件
        typeof(EnemyArmyGroupBelongsToCity),

        // 单例组件
        typeof(PlayerFactionData),
        typeof(PopulationResourceData),
        typeof(WorldTimeData),
        typeof(CameraMainGameplayHistory),
        typeof(GlobalSingIDCounter),

        // 单例 Buffer
        typeof(ResourceData),
        typeof(PopulationConjureTask),
        typeof(PopulationStorageAddTask),
    };
}

public static class ArcheTypeCheckOut
{
    // ------------------------------- Unit archetype -----------------------------//
    private static readonly List<Type> UnitArcheTypeFixedComponentTypes = new()
    {
        typeof(GlobalSingleId),
        typeof(SubGameplayGeneralAttr),
        typeof(UnitAttr),
        typeof(LocalTransform),
        typeof(PhysicsMass),
        typeof(ExpData),
        typeof(StatData),
        typeof(MovableData),
    };

    private static readonly List<Type> UnitArcheTypeConditionalComponentTypes = new()
    {
        typeof(InGarrison),
        typeof(InArmyGroup),
        typeof(AttackAbility),
        typeof(HealAbility),
        typeof(HarvestAbility)
    };

    // ------------------------------ Building archetype ----------------------------//
    private static readonly List<Type> BuildingArcheTypeFixedComponentTypes = new()
    {
        typeof(GlobalSingleId),
        typeof(SubGameplayGeneralAttr),
        typeof(BuildingAttr),
        typeof(LocalTransform),
        typeof(StatData)
    };

    private static readonly List<Type> BuildingArcheTypeConditionalComponentTypes = new()
    {
        typeof(ConstructingTimer)
    };

    private static readonly List<Type> BuildingArcheTypeConditionalBufferTypes = new()
    {
        typeof(GarrisonEntity),
        typeof(GarrisonTypeData),
        typeof(ConjuringData)
    };

    // ---------------------------------City archetype-------------------------------------//

    public static readonly List<Type> CityArcheTypeFixedComponentTypes = new()
    {
        typeof(GlobalSingleId),
        typeof(MainGameplayGeneralAttr),
        typeof(HpRegenerateTimer),
        typeof(LocalTransform),
        typeof(CityAIData),
    };

    public static readonly List<Type> CityArcheTypeFixedBufferTypes = new()
    {
        typeof(CityGarrisonEntity),
        typeof(CityResourceEntry),
        typeof(CityTask),
        typeof(CityFutureInvaders),
        typeof(ExtraArmyGroup),
        typeof(AttackArmyGroup),
        typeof(DefendArmyGroup),
        typeof(InvadingArmyGroup),
        typeof(InvadeTarget),
        typeof(ArmyGroupConjureStack),
    };

    // ------------------------------- Army group archetype -----------------------------//
    public static readonly List<Type> ArmyGroupArcheTypeFixedComponentTypes = new()
    {
        typeof(GlobalSingleId),
        typeof(MainGameplayGeneralAttr),
        typeof(ArmyGroupAttr),

        typeof(ArmyGroupStatData),
        typeof(HpRegenerateTimer),

        typeof(ArmyGroupMovableData),
        typeof(ArmyGroupPathVisualizeData),
        typeof(LastPassingByCity),

        typeof(ArmyGroupStateData),
        typeof(ArmyGroupThreatenData),
    };

    public static readonly List<Type> ArmyGroupArcheTypeFixedBufferTypes = new()
    {
        typeof(ArmyGroupMovingTarget),
        typeof(ArmyGroupFinalWayPoint),
        typeof(ArmyGroupUnit),
        typeof(ArmyGroupUnitTypeData),
    };

    public static readonly List<Type> ArmyGroupArcheTypeConditionalComponentTypes = new()
    {
        typeof(EnemyArmyGroupBelongsToCity)
    };

    // ----------------------- Singleton Data ---------------------------//
    public static readonly List<Type> SingletonComponentTypes = new()
    {
        typeof(PlayerFactionData),
        typeof(PopulationResourceData),
        typeof(WorldTimeData),
        typeof(CameraMainGameplayHistory),
        typeof(GlobalSingIDCounter)
    };

    public static readonly List<Type> SingletonBufferTypes = new()
    {
        typeof(ResourceData),
        typeof(PopulationConjureTask),
        typeof(PopulationStorageAddTask),
    };
}

#endregion*/