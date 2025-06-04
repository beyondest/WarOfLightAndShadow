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
        private EntityQuery _gamingTag;
        private EntityQuery _unitSelectionDataQuery;
  
        void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(GamingTag));
            _unitSelectionDataQuery = _em.CreateEntityQuery(typeof(UnitSelectionData));
        }

        void Update()
        {
            selectionBoxImage.enabled = false;
            selectionBoxImage.rectTransform.sizeDelta = Vector2.zero;
            if(_gamingTag.IsEmpty)
            {
                return;
            }
            // Unit selection system not found, do not show image
            if (!_unitSelectionDataQuery.TryGetSingleton(out UnitSelectionData unitSelectionData))
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