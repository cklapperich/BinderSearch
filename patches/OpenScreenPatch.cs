// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using System.Diagnostics;
// using HarmonyLib;
// using UnityEngine;
// using BepInEx.Logging;

// [HarmonyPatch(typeof(CardOpeningSequence), "OpenScreen")]
// public static class CardOpeningSequence_OpenScreen_Patch 
// {
//     static void Postfix(
//         CardOpeningSequence __instance,
//         ECollectionPackType collectionPackType,
//         bool isMultiPack, 
//         bool isPremiumPack,
//         MethodBase __originalMethod)
//     {
//         // Get stack trace for context
//         System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
        
//         Plugin.Log.LogInfo($"=== OpenScreen Called ===");
//         Plugin.Log.LogInfo($"CollectionPackType: {collectionPackType}");
//         Plugin.Log.LogInfo($"isMultiPack: {isMultiPack}");
//         Plugin.Log.LogInfo($"isPremiumPack: {isPremiumPack}");
//         Plugin.Log.LogInfo($"Instance: {__instance?.GetType().FullName}");
//         Plugin.Log.LogInfo($"Original Method: {__originalMethod?.DeclaringType?.FullName}.{__originalMethod?.Name}");
//         Plugin.Log.LogInfo($"Calling Method: {stackTrace.GetFrame(1)?.GetMethod()?.Name}");
//         Plugin.Log.LogInfo($"Full Stack Trace:");
//         Plugin.Log.LogInfo(stackTrace.ToString());
//         Plugin.Log.LogInfo("=====================");
//     }
// }