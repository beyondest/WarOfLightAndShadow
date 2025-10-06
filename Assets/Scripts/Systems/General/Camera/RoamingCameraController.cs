using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.Systems.General.Camera
{
    public class RoamingCameraController : MonoBehaviour
    {
        public float waitSecondsPerPosition = 2f;
        public float moveSecondsPerPosition = 1f;

        public float afterCrystalAnimationWaitSeconds = 2f;
        public float crystalMovingDuration = 3f;
        public float crystalStartHeightBias;
        public event Action OnStartRoamingCamera;
        public event Action OnEndRoamingCamera;
        public event Action OnCrystalAnimationEnd;
        public event Action OnShowEnemy;
        public event Action OnShowPlayer;
        public event Action OnSwitchToPlayer;
        public static RoamingCameraController Instance;


        public void PlayCrystalSwitchAnimation(Entity newCrystal, float3 finalPosition)
        {
            var cam = UnityEngine.Camera.main;
            StartCoroutine(CrystalSwitch(newCrystal, finalPosition, cam!.transform.parent.transform));
        }

        private IEnumerator CrystalSwitch(Entity newCrystal, float3 finalPosition, Transform rigTransform)
        {
            var startPos = finalPosition + crystalStartHeightBias;
            rigTransform.position = startPos;
            _em.SetComponentData(newCrystal, new LocalTransform
            {
                Position = startPos,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            var elapsedTime = 0f;
            while (elapsedTime < crystalMovingDuration)
            {
                float t = elapsedTime / crystalMovingDuration;
                var targetPos = math.lerp(startPos, finalPosition, t);
                rigTransform.position = targetPos;
                _em.SetComponentData(newCrystal, new LocalTransform
                {
                    Position = targetPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            yield return new WaitForSecondsRealtime(afterCrystalAnimationWaitSeconds);
            OnCrystalAnimationEnd?.Invoke();
        }

        public void StartRoamingCamera(Transform rigTransform, List<float3> positions,
            int enemyCount)
        {
            StartCoroutine(RoamingCamera(rigTransform, positions, enemyCount));
        }

        private EntityManager _em;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private IEnumerator RoamingCamera(Transform rigTransform, List<float3> positions,
            int enemyCount)
        {
            var index = 0;
            OnStartRoamingCamera?.Invoke();
            OnShowEnemy?.Invoke();
            while (index < positions.Count)
            {
                if (index == enemyCount)
                {
                }

                var position = positions[index];
                rigTransform.position = position;
                index++;
                yield return new WaitForSecondsRealtime(waitSecondsPerPosition);
                if (index >= positions.Count) break;
                if (index == enemyCount)
                {
                    OnSwitchToPlayer?.Invoke();
                    OnShowPlayer?.Invoke();
                }

                var nextPosition = positions[index];
                StartMovingCamera(rigTransform, nextPosition, moveSecondsPerPosition);
                yield return new WaitForSecondsRealtime(moveSecondsPerPosition);
            }

            OnEndRoamingCamera?.Invoke();
        }

        private IEnumerator MovingCamera(Transform rigTransform, float3 nextPosition, float time)
        {
            if (time == 0) yield break;
            var t = 0f;
            while (t < time)
            {
                rigTransform.position = math.lerp(rigTransform.position, nextPosition, t / time);
                t += Time.deltaTime;
                yield return null;
            }
        }

        private void StartMovingCamera(Transform rigTransform, float3 nextPosition, float time)
        {
            StartCoroutine(MovingCamera(rigTransform, nextPosition, time));
        }
    }
}