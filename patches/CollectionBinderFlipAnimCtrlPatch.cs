using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BinderSearch.Patches
{
    [HarmonyPatch(typeof(CollectionBinderFlipAnimCtrl))]
    public static class CollectionBinderFlipAnimCtrlPatch
    {
        // Add GoToPage as a new method
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPatch("GoToPage")]
        public static class GoToPagePatch
        {
            public static bool Prefix(CollectionBinderFlipAnimCtrl __instance, int targetPage)
            {
                if (!__instance.enabled)
                    return false;

                // Get private fields via reflection
                var maxIndex = (int)AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_MaxIndex").GetValue(__instance);
                if (targetPage < 1 || targetPage > maxIndex)
                    return false;

                // Get BinderPageGrpList
                var binderPageGrpList = (List<BinderPageGrp>)AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_BinderPageGrpList").GetValue(__instance);

                // Trigger page flip animation
                binderPageGrpList[0].m_Anim.SetTrigger("GoNextPage");
                binderPageGrpList[1].m_Anim.SetTrigger("GoNextPage");
                binderPageGrpList[2].m_Anim.SetTrigger("SetHideNextIdle");

                // Reorder page groups like m_GoNext10
                var item = binderPageGrpList[0];
                binderPageGrpList.RemoveAt(0);
                binderPageGrpList.Add(item);

                // Update index
                var indexField = AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_Index");
                indexField.SetValue(__instance, targetPage);

                // Handle flip animation timing
                var canFlipCoroutineField = AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_CanFlipCoroutine");
                var currentCoroutine = (Coroutine)canFlipCoroutineField.GetValue(__instance);
                if (currentCoroutine != null)
                {
                    __instance.StopCoroutine(currentCoroutine);
                }
                canFlipCoroutineField.SetValue(__instance, __instance.StartCoroutine(DelayResetCanFlipBook(__instance, 0.55f)));

                // Update UI for all pages
                var updateMethod = AccessTools.Method(typeof(CollectionBinderFlipAnimCtrl), "UpdateBinderAllCardUI");
                updateMethod.Invoke(__instance, new object[] { 0, targetPage });
                __instance.StartCoroutine(DelaySetBinderPageCardIndex(__instance, 2, targetPage - 1));

                // Update UI display
                var binderUI = (CollectionBinderUI)AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_CollectionBinderUI").GetValue(__instance);
                binderUI.SetCurrentPage(targetPage);

                // Play sound effect
                SoundManager.PlayAudio("SFX_AlbumFlip", 0.6f, 1f);

                // Update next page if not at max
                if (targetPage < maxIndex)
                {
                    __instance.StartCoroutine(DelaySetBinderPageCardIndex(__instance, 1, targetPage + 1));
                }

                return false; // Skip original method
            }

            private static IEnumerator DelayResetCanFlipBook(CollectionBinderFlipAnimCtrl instance, float delay)
            {
                yield return new WaitForSeconds(delay);
                AccessTools.Field(typeof(CollectionBinderFlipAnimCtrl), "m_CanFlip").SetValue(instance, true);
            }

            private static IEnumerator DelaySetBinderPageCardIndex(CollectionBinderFlipAnimCtrl instance, int binderIndex, int pageIndex)
            {
                yield return new WaitForSeconds(0.5f);
                var updateMethod = AccessTools.Method(typeof(CollectionBinderFlipAnimCtrl), "UpdateBinderAllCardUI");
                updateMethod.Invoke(instance, new object[] { binderIndex, pageIndex });
            }
        }
    }
}
