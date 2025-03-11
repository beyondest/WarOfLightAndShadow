using System;
using UnityEngine;
using UnityEngine.AI;

namespace SparFlame.GamePlaySystem.Movement
{
    
    public class Vanguard : MonoBehaviour
    {
        private Camera _cam;
        private NavMeshAgent _agent;
        public LayerMask layerMask;

        private void Start()
        {
            _cam = Camera.main;
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                RaycastHit hit;
                UnityEngine.Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
                {
                    _agent.SetDestination(hit.point) ;
                }
            }

            var pathend = _agent.pathEndPosition;
            Debug.Log($"{pathend}");
            if (_agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                Debug.Log($"Cannot reach destination");
            }

        }
    }
}