using Unity.Entities;

namespace SparFlame.Components.General
{
    public enum SaveType
    {
        Manual, // Happens when player save
        Automatic, // Happens when a war ends or some crucial things happen
        SaveSubGameplayDataToTmp, // Happens when exit the city or save some data but no need to write to any slot
        SaveEnemySpecificArmyGroupSubData // Happens when a new enemy army group is created; Will save to tmp files
    }
    public struct NeedSaveTag : IComponentData, IEnableableComponent
    {
    }
}