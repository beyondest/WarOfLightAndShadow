using UnityEngine;

namespace SparFlame.Test
{
    [CreateAssetMenu(fileName = "GameData", menuName = "TestDatabase", order = 0)]
    public class TestDatabaseSo : ScriptableObject
    {
        public GameObject prefab;   
    }
}