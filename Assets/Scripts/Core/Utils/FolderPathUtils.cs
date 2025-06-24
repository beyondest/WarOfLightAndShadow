using System;
using System.IO;
using UnityEngine;

namespace SparFlame.Core.Utils
{
    public static class FolderPathUtils
    {
        private const string SaveFolder = "SaveData";
        
        private static readonly string SaveRootFolder = Path.Combine(Application.persistentDataPath, SaveFolder);

        public static string GetPlayerSaveSlotFolder(int playerSaveSlot)
        {
            return Path.Combine(SaveRootFolder, "Player" + playerSaveSlot);
        }
        
        
    }
    public static class SingleIdGenerator
    {
        private static readonly long BaseId;
        private static int _counter = 0;

        static SingleIdGenerator()
        {
            // 初始化时获取当前 UTC 时间戳
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // or Seconds
            BaseId = unixTime * 1000; // 留 3 位给 counter（最多 999 个 ArmyGroup）
        }

        public static long GetNewArmyGroupId()
        {
            return BaseId + _counter++;
        }
    }

}