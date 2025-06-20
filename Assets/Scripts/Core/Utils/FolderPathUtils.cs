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
}