using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SparFlame.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SparFlame.UI.General
{
    /// <summary>
    /// Custom Resource Loader, provide simple helper functions
    /// </summary>
    public static class CR
    {


        public static AsyncOperationHandle<TResource> LoadPrefabAddressableRefAsync<TResource>(
            AssetReference assetReference, Action<TResource> onComplete)
        {
            var handle = Addressables.LoadAssetAsync<TResource>(assetReference);
            handle.Completed += _ => onComplete?.Invoke(handle.Result);
            return handle;
        }
        
        public static AsyncOperationHandle<IList<TResource>> LoadTypeSuffixAddressableAsync<TEnum, TResource>(
            string suffix = null, Action<IList<TResource>> onComplete = null
             ) where TEnum : Enum
        {
            var keys = Enum.GetNames(typeof(TEnum));
            var fullNames = new List<string>();
            if (suffix != null)
            {
                foreach (var key in keys)
                {
                    fullNames.Add($"{key}{suffix}");
                }
            }
            else
            {
                fullNames.AddRange(keys);
            }
            var handle = Addressables.LoadAssetsAsync<TResource>(fullNames,_=>{},Addressables.MergeMode.Union,true);
            handle.Completed += _ => { onComplete?.Invoke(handle.Result); }; 
            return handle;
        }
        
        
        public static void OnTypeSuffixAddressableLoadComplete<TEnum,TResource>(
            IList<TResource> result, Dictionary<TEnum, TResource> dict) where TEnum : Enum
        {
            var keys = Enum.GetValues(typeof(TEnum));
            var i = 0;
            foreach (TEnum key in keys)
            {
                dict[key] = result[i];
                i++;
            }
        }
        
        

        #region Resource Load Methods (Deprecated)

        public static Dictionary<T, Sprite> ResourceLoadTypeSprites<T>(string path, string prefix = null) where T : Enum
        {
            var dict = new Dictionary<T, Sprite>();

            foreach (T type in Enum.GetValues(typeof(T)))
            {
                string fullName = type.ToString();
                if (!string.IsNullOrEmpty(prefix))
                    fullName = prefix + fullName;

                string fullPath = $"{path}/{fullName}";
                Sprite sprite = Resources.Load<Sprite>(fullPath);

                if (sprite != null)
                {
                    dict[type] = sprite;
                }
                else
                {
                    Debug.LogError($"Sprite not found: {fullPath}");
                }
            }

            return dict;
        }
        #endregion

    }
}