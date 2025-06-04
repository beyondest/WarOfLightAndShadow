using System;
using System.Collections.Generic;
using System.Linq;
using SparFlame.GamePlaySystem.Animation;
using SparFlame.GamePlaySystem.State;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestChangeState : MonoBehaviour
    {
        public List<namepair> pairs2;
        public Animator _a;
        private EntityQuery _query;
        private EntityManager _em;


        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _query = _em.CreateEntityQuery(typeof(AnimationStateData));
        }

        public void ChangeState(string name)
        {
            var tmp =            pairs2.Find(namepair => namepair.name == name);

            pairs2.Remove(tmp);

            if (_a != null)
            {
                _a.SetBool(name, true);
                foreach (var s in pairs2)
                {
                    _a.SetBool(s.name, false);
                }
            }
            pairs2.Add(tmp);
            foreach (var e in _query.ToEntityArray(Allocator.Temp))
            {
                if(!_em.HasComponent<ChangeStateRequest>(e))continue;
                _em.SetComponentData(e, new ChangeStateRequest
                {
                    TargetState = (UnitAnimationState)tmp.index
                });
            }
        }
        
        [Serializable]
        public struct namepair
        {
            public string name;
            public int index;
        }
    }
}