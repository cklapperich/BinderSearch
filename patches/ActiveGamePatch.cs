using HarmonyLib;
using BepInEx.Logging;

namespace BinderSearch.Patches
{
    public class ActiveGamePatch
    {
        [HarmonyPatch(typeof(InteractionPlayerController), "Start")]
        public class GameStartPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                Plugin.Logger.LogInfo("ActiveGamePatch Postfix called!");
                Plugin.activeGame = true;
                Plugin.Logger.LogInfo("Game is now active");
            }
        }
    }
}