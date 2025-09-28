using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestEntityQueryDispose : MonoBehaviour
    {

        public int createCount = 1;
        public bool disposeBeforeCreate;
        public bool testInStart;
        private EntityQuery query;

        private void Start()
        {
            if(testInStart)
                CreateQuery();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
               CreateQuery();
            }
        }

        private void CreateQuery()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            for (var i = 0; i < createCount; i++)
            {
                if (disposeBeforeCreate)
                {
                    if (query != default)
                    {
                        Debug.Log("Query not default, dispose before create");
                        query.Dispose();
                    }
                    else
                    {
                        Debug.Log("Query is default, no need to dispose");
                    }
                }
                query = em.CreateEntityQuery(typeof(TestTag));
                Debug.Log("Created query");
            }
        }

        private void OnDestroy()
        {
            if (query != default)
            {
                query.Dispose();
                Debug.Log("Disposing query");
            }
            else
            {
                Debug.Log("Query is default, no need to dispose");
            }
        }
    }
}