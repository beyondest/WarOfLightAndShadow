namespace SparFlame.Core.Utils
{
    public enum NodeState
    {
        Success,
        Failure,
        Running,
    }
    public interface IBtNode
    {
        NodeState Tick();
    }
    

}