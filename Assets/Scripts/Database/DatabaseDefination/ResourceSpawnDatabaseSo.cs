using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using SparFlame.GamePlaySystem.Resource;
namespace SparFlame.Database.Database.DatabaseDefinition
{
    [CreateAssetMenu(fileName = "ResourceSpawnDatabase", menuName = "GameData/ResourceSpawnDatabase", order = 0)]
    public class ResourceSpawnDatabaseSo : SerializedScriptableObject
    {
        public Dictionary<ResourceTimePoints, Dictionary<ResourceType, int>> timePoints ;
        

    }

    
}