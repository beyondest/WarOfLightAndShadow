using System.Collections.Generic;

namespace SparFlame.Systems.SubGameplay.StateMachine.StateMachineTemplates
{

    public enum StateMachineT
    {
        Idle, 
        Attacking,
        Moving
    }
    public interface IStateMachine
    {
        void Enter();
        void Exit();
        void Update();
        
    }

    class Node
    {
        public Node parent;
        private Dictionary<string, object> _data;
        public object GetData(string key)
        {
            object data = null;
            if(_data.TryGetValue(key, out data))
                return data;
            var node = parent;
            while(node!= null)
            {
                var value = node.GetData(key);
                if(value!= null)
                    return value;
                node = node.parent;
            }
            return null;
        }
    }
}