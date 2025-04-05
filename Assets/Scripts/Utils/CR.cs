using System;
using System.Collections.Generic;
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
        /// <summary>
        /// <para>Addressable keys = EnumType name + Property name, all the combinations of them</para>>
        /// <para>e.g. Use this function to load Attack/Heal/Harvest Amount/Range/Speed/Targets Sprites</para>
        /// </summary>
        /// <param name="group"></param>
        /// <param name="lookUpDict"></param>
        /// <param name="propertyCutCount">Will cut off the last propertyCutCount properties, not to load them</param>
        /// <typeparam name="TEnum">Prefix</typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <typeparam name="TResource"></typeparam>
        public static void LoadEnumProperties<TEnum, TProperty, TResource>(AddressableResourceGroup group,
            Dictionary<TEnum, List<TResource>> lookUpDict, int propertyCutCount)
            where TEnum : Enum
        {
            var properties = typeof(TProperty).GetProperties();
            var propertyNames = new List<string>();
            for (var i = 0; i < properties.Length - propertyCutCount; i++)
            {
                propertyNames.Add(properties[i].Name);
            }

            foreach (TEnum type in Enum.GetValues(typeof(TEnum)))
            {
                var resourceList = new List<TResource>();
                var prefix = Enum.GetName(typeof(TEnum), type);
                var keys = new List<string>();
                foreach (var propertyName in propertyNames)
                {
                    keys.Add(prefix + propertyName);
                }

                var handle = LoadAssetsNameAsync<TResource>(keys, null, result =>
                {
                    resourceList.AddRange(result);
                    lookUpDict.Add(type, resourceList);
                });
                group.Add(handle);
            }
        }


        public static AsyncOperationHandle<IList<TResource>> LoadAssetsNameAsync<TResource>(IList<string> names,
            string suffix = null,
            Action<IList<TResource>> onComplete = null)
        {
            var keys = new List<string>();
            if (suffix != null)
            {
                foreach (var name in names)
                {
                    keys.Add(name + suffix);
                }
            }
            else
            {
                keys.AddRange(names);
            }

            var handle = Addressables.LoadAssetsAsync<TResource>(keys, null, Addressables.MergeMode.Union, true);
            handle.Completed += _ => onComplete?.Invoke(handle.Result);
            return handle;
        }

        public static AsyncOperationHandle<TResource> LoadAssetRefAsync<TResource>(
            AssetReference assetReference, Action<TResource> onComplete)
        {
            var handle = Addressables.LoadAssetAsync<TResource>(assetReference);
            handle.Completed += _ => onComplete?.Invoke(handle.Result);
            return handle;
        }

        public static AsyncOperationHandle<IList<TResource>> LoadTypeSuffix<TEnum, TResource>(
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

            var handle =
                Addressables.LoadAssetsAsync<TResource>(fullNames, _ => { }, Addressables.MergeMode.Union, true);
            handle.Completed += _ =>
            {
                if (handle.Result.Count < keys.Length)
                {
                    foreach (var result in handle.Result)
                    {
                        Debug.LogError($"Found asset {result}");
                    }

                    foreach (var key in keys)
                    {
                        Debug.LogError($"Require asset {key}");
                    }

                    throw new ArgumentException("Load Addressable assets wrong, missing some assets");
                }
                onComplete?.Invoke(handle.Result);
            };
            return handle;
        }

        public static void OnTypeSuffixLoadComplete<TEnum, TResource>(
            IList<TResource> result, Dictionary<TEnum, TResource> dict) where TEnum : Enum
        {
            var keys = Enum.GetValues(typeof(TEnum));
            var i = 0;
            foreach (TEnum key in keys)
            {
                try
                {
                    dict[key] = result[i];
                    i++;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
               
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


    public abstract class CustomResourceManager : MonoBehaviour
    {
        public abstract bool IsResourceLoaded();
    }
}