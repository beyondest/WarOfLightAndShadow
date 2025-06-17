using SparFlame.GamePlaySystem.Save;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;


namespace SparFlame.GamePlaySystem.Save
{
    public class SaveManager : MonoBehaviour
    {
        private EntityQuery _entitiesToSave;

        public struct SaveTag : IComponentData
        {
        }

        private void Start()
        {
            // Cache a query that gathers all of the entities that should be saved.
            // NOTE: You don't have to use a special tag component for all entities you want to save. You could instead just
            // save, for example, anything with a Translation component which would exclude things like Singletons entities.
            // It is important to note that prefabs (anything with a Prefab tag component) are automatically excluded from
            // an EntityQuery unless EntityQueryOptions.IncludePrefab is set.
            var savableEntities = new EntityQueryDesc
            {
                Any = new ComponentType[]
                {
                    typeof(SaveTag),
                },
                Options = EntityQueryOptions.Default
            };
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entitiesToSave = entityManager.CreateEntityQuery(savableEntities);
        }

        // Looks for and removes a set of components and then adds a different set of components to the same set
        // of entities. 
        private void ReplaceComponents(
            ComponentType[] typesToRemove,
            ComponentType[] typesToAdd,
            EntityManager entityManager)
        {
            EntityQuery query = entityManager.CreateEntityQuery(
                new EntityQueryDesc { Any = typesToRemove, Options = EntityQueryOptions.Default }
            );
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            foreach (ComponentType removeType in typesToRemove)
            {
                entityManager.RemoveComponent(entities, removeType);
            }

            foreach (ComponentType addType in typesToAdd)
            {
                entityManager.AddComponent(entities, addType);
            }
        }

        public void Save(string filepath)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using (var serializeWorld = new World("Serialization World"))
            {
                EntityManager serializeEntityManager = serializeWorld.EntityManager;
                serializeEntityManager.RemoveComponent<SceneTag>(serializeEntityManager.UniversalQuery);
                serializeEntityManager.RemoveComponent<SceneSection>(serializeEntityManager.UniversalQuery);

                // Save
                using (var writer = new StreamBinaryWriter(filepath))
                {
                    SerializeUtility.SerializeWorld(serializeEntityManager, writer);
                }
            }
        }

        public void Load(string filepath)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            entityManager.DestroyEntity(_entitiesToSave);

            using (var deserializeWorld = new World("Deserialization World"))
            {
                ExclusiveEntityTransaction transaction =
                    deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();

                using (var reader = new StreamBinaryReader(filepath))
                {
                    SerializeUtility.DeserializeWorld(transaction, reader);
                }

                deserializeWorld.EntityManager.EndExclusiveEntityTransaction();

                entityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
            }
        }
    }

    [Savable]
    public struct Health : IComponentData
    {
        
    }
}