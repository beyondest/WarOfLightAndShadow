using System;
using UnityEngine;

namespace SparFlame.Systems.General
{
   
    [RequireComponent(typeof(Animator))]
    public class DisableAnimationEvents : MonoBehaviour
    {
        private Animator _targetAnimator; // Assign your Animator in the Inspector

        private void Awake()
        {
            _targetAnimator = GetComponent<Animator>();
        }

        void Start()
        {
            
            if (_targetAnimator != null)
            {
                _targetAnimator.fireEvents = false;
            }
            else
            {
                Debug.LogWarning("Target Animator not assigned to DisableAnimationEvents script.");
            }
        }
    }

}