using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.GamePlaySystem.General;
namespace SparFlame.UI.GamePlay
{
    public class UnitSelectionBoxController : MonoBehaviour
    {
        public Image selectionBoxImage; 
        private EntityManager _em;

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Update()
        {
            selectionBoxImage.enabled = false;
            selectionBoxImage.rectTransform.sizeDelta = Vector2.zero;
            // When game paused, do nothing
            if (!_em.CreateEntityQuery(typeof(NotPauseTag)).TryGetSingletonEntity< NotPauseTag>(out var _))
            {
                return;
            }
            // Unit selection system not found, do not show image
            if (!_em.CreateEntityQuery(typeof(UnitSelectionData)).TryGetSingleton(out UnitSelectionData unitSelectionData))
            {
                return;
            }

            if (!unitSelectionData.IsDragSelecting)
            {
                return;
            }
            selectionBoxImage.enabled = true;
            // Mouse Position : left down corner is (0,0). Notice that the scale of game view must be min
            var min = math.min(unitSelectionData.SelectionBoxStartPos, unitSelectionData.SelectionBoxEndPos);
            var max = math.max(unitSelectionData.SelectionBoxStartPos, unitSelectionData.SelectionBoxEndPos);
            var size = max - min;
            selectionBoxImage.rectTransform.position = (Vector2)min; 
            selectionBoxImage.rectTransform.sizeDelta = size; 
        }
    }
}