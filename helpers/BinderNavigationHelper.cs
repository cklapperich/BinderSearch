using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;

namespace BinderSearch.Helpers
{
    public static class BinderNavigationHelper
    {
        public static bool NavigateToPage(CollectionBinderFlipAnimCtrl binderCtrl, int targetPage, ManualLogSource logger = null)
        {
            if (!binderCtrl.enabled)
            {
                Plugin.Logger.LogWarning("Binder is not enabled!");
                return false;
            }
            // Get private fields via reflection
            var maxIndex = (int)AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_MaxIndex").GetValue(binderCtrl);
            if (targetPage < 1 || targetPage > maxIndex)
            {
                Plugin.Logger.LogWarning($"Target page {targetPage} is out of bounds (max: {maxIndex})");
                return false;
            }

            // Get BinderPageGrpList
            var binderPageGrpList = (List<BinderPageGrp>)AccessTools.Field(
                typeof(CollectionBinderFlipAnimCtrl), 
                "m_BinderPageGrpList"
            ).GetValue(binderCtrl);

            // Trigger page flip animation
            binderPageGrpList[0].m_Anim.SetTrigger("GoNextPage");
            binderPageGrpList[1].m_Anim.SetTrigger("GoNextPage");
            binderPageGrpList[2].m_Anim.SetTrigger("SetHideNextIdle");

            // Reorder page groups
            var item = binderPageGrpList[0];
            binderPageGrpList.RemoveAt(0);
            binderPageGrpList.Add(item);

            // Update index
            var indexField = AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_Index");
            indexField.SetValue(binderCtrl, targetPage);

            // Handle flip animation timing
            var canFlipCoroutineField = AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_CanFlipCoroutine");
            var currentCoroutine = (Coroutine)canFlipCoroutineField.GetValue(binderCtrl);
            if (currentCoroutine != null)
            {
                binderCtrl.StopCoroutine(currentCoroutine);
            }

            // Start new coroutine for flip timing
            var newCoroutine = binderCtrl.StartCoroutine(DelayResetCanFlipBook(binderCtrl, 0.55f));
            canFlipCoroutineField.SetValue(binderCtrl, newCoroutine);

            Plugin.Logger.LogInfo($"Successfully navigated to page {targetPage}");
            return true;
        }

        private static IEnumerator DelayResetCanFlipBook(CollectionBinderFlipAnimCtrl binderCtrl, float delay)
        {
            yield return new WaitForSeconds(delay);
            var canFlipField = AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_CanFlip");
            canFlipField.SetValue(binderCtrl, true);
        }
    }
}
