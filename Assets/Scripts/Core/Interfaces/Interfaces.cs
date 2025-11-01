using Unity.Entities;

namespace SparFlame.Core.Interfaces
{
    public interface IStructLookup
    {
        void Update(ref SystemState state);
    }

    public interface IEcsTransferSystem<in T>
    {
        void Init(T mono);
    }
}