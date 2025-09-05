using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
namespace SparFlame.UI.General
{
    public class SelectionBoxController : MonoBehaviour
    {
        public Image selectionBoxImage; 
        private EntityManager _em;
        private EntityQuery _subGamingTag;
        private EntityQuery _mainGamingTag;
        private EntityQuery _unitSelectionDataQuery;
        private EntityQuery _armyGroupSelectionDataQuery;

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _subGamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            _mainGamingTag = _em.CreateEntityQuery(typeof(MainGamingTag));
            _unitSelectionDataQuery = _em.CreateEntityQuery(typeof(UnitSelectionData));
            _armyGroupSelectionDataQuery = _em.CreateEntityQuery(typeof(ArmyGroupSelectionData));
        }

        private void Update()
        {
            selectionBoxImage.enabled = false;
            selectionBoxImage.rectTransform.sizeDelta = Vector2.zero;
            if(_subGamingTag.IsEmpty && _mainGamingTag.IsEmpty)
            {
                return;
            }

            float2 min;
            float2 max;
            if (!_subGamingTag.IsEmpty)
            {
                // Unit selection system not found, do not show image
                if (!_unitSelectionDataQuery.TryGetSingleton(out UnitSelectionData unitSelectionData))
                    return;
            
                if (!unitSelectionData.IsDragSelecting)
                    return;
                selectionBoxImage.enabled = true;
                // Mouse Position : left down corner is (0,0). Notice that the scale of game view must be min
                 min = math.min(unitSelectionData.SelectionBoxStartPos, unitSelectionData.SelectionBoxEndPos);
                 max = math.max(unitSelectionData.SelectionBoxStartPos, unitSelectionData.SelectionBoxEndPos);
            }
            else
            {
                if(!_armyGroupSelectionDataQuery.TryGetSingleton(out ArmyGroupSelectionData armyGroupSelectionData))return;
                if(!armyGroupSelectionData.IsDragSelecting)return;
                selectionBoxImage.enabled = true;
                 min = math.min(armyGroupSelectionData.SelectionBoxStartPos, armyGroupSelectionData.SelectionBoxEndPos);
                 max = math.max(armyGroupSelectionData.SelectionBoxStartPos, armyGroupSelectionData.SelectionBoxEndPos);
            }
            var size = max - min;

            selectionBoxImage.rectTransform.position = (Vector2)min; 
            selectionBoxImage.rectTransform.sizeDelta = size; 
        }
    }
}