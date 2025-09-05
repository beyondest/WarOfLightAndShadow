using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.GlobalMono;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
    public class ButtonBackToMainWorld : ButtonUtils.SelfButton
    {
        [SerializeField] private GameObject backToMainWorldPanel;
        public static ButtonBackToMainWorld Instance;
        public event Action<Entity> OnEcsDeleteArmyGroup;

        protected override void Awake()
        {
            base.Awake();
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            GameController.Instance.OnSwitchGameStatusForSystems += status =>
            {
                backToMainWorldPanel.SetActive(status.SubGameStatus == SubGameStatus.PlayerCity);
            };
        }


        public override void OnClick()
        {
            var emptyArmyGroups = new List<Entity>();
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(typeof(ArmyGroupUnit), typeof(InSubGameTag));
            var hasEmptyArmyGroup = false;
            var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities )
            {
                if (em.GetBuffer<ArmyGroupUnit>(entity).Length == 0)
                {
                    hasEmptyArmyGroup = true;
                    emptyArmyGroups.Add(entity);
                }
            }
            entities.Dispose();

            if (hasEmptyArmyGroup)
            {
                ConfirmWindow.Instance.Show(
                    "You have army group with no units. Close the window will delete the army group.",
                    () =>
                    {
                        foreach (var emptyArmyGroup in emptyArmyGroups)
                        {
                            OnEcsDeleteArmyGroup?.Invoke(emptyArmyGroup);
                        }
                        FrameDelayInvoker.Instance.InvokeAfterFrames(1, () =>
                        {
                            GameController.Instance.BackToMainWorld();
                        });
                    });
            }
            else
            {
                GameController.Instance.BackToMainWorld();
            }
        }
    }
}