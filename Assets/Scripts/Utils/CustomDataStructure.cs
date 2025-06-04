using System;

namespace SparFlame.Utils
{
    public static class CustomDs
    {
        [Serializable]
        public struct Range
        {
            public float lower;
            public float upper;
        }
        
        /// <summary>
        /// Every IResourceManager should be put in bootStrapper scene,
        /// and if it is to be destroyed, you have to manually release resource and unregister
        /// </summary>
        public interface IResourceManager
        {
            bool IsInitialized { get; }
            float InitProgress { get; }
            void LoadResources();
            void UnloadResources();
        }

    }
}