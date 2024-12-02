﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace BinderSearch
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Card Shop Simulator.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static ConfigEntry<KeyboardShortcut> SearchHotkey;
        public static bool activeGame = false;
        private static int currentPage = 1;
        private string pendingValue = "";
        
        // UI Components
        private SimpleTextEntry textEntry;
        private bool uiInitialized = false;
        private static GameObject searchUIObject;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin Binder Search is loaded!");

            SearchHotkey = Config.Bind(
                "Hotkey Settings",
                "Search Hotkey",
                new KeyboardShortcut(KeyCode.T),
                "Press this key to activate the binder search feature when the binder is open."
            );

            harmony.PatchAll();
        }

        private void Start()
        {
            SetupSearchUI();
        }

        private void SetupSearchUI()
        {
            try
            {
                if (uiInitialized && textEntry != null)
                {
                    Logger.LogInfo("UI already initialized and valid, skipping setup");
                    return;
                }

                Logger.LogInfo("Creating search UI...");
                
                // Clean up any existing UI
                if (searchUIObject != null)
                {
                    Destroy(searchUIObject);
                }

                // Create UI at root level
                searchUIObject = new GameObject("BinderSearchUI");
                DontDestroyOnLoad(searchUIObject);
                
                // Add SimpleTextEntry component
                textEntry = searchUIObject.AddComponent<SimpleTextEntry>();
                
                if (textEntry == null)
                {
                    throw new Exception("Failed to add SimpleTextEntry component");
                }

                // Subscribe to the text changed event
                textEntry.OnTextChanged += OnSearchValueChanged;
                
                uiInitialized = true;
                Logger.LogInfo($"UI initialization complete. TextEntry null? {textEntry == null}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error setting up search UI: {ex.Message}\nStack trace: {ex.StackTrace}");
                uiInitialized = false;
                textEntry = null;
            }
        }

        private void OnDestroy()
        {
            Logger.LogInfo("Plugin OnDestroy called");
            if (textEntry != null)
            {
                textEntry.OnTextChanged -= OnSearchValueChanged;
            }
            if (searchUIObject != null)
            {
                Destroy(searchUIObject);
            }
            uiInitialized = false;
            textEntry = null;
        }

        private void TriggerSearch()
        {
            try
            {
                Logger.LogInfo("TriggerSearch called");

                if (textEntry == null)
                {
                    Logger.LogInfo("TextEntry is null, attempting to reinitialize...");
                    SetupSearchUI();
                    
                    if (textEntry == null)
                    {
                        Logger.LogError("Failed to initialize TextEntry!");
                        return;
                    }
                }

                // Get current page
                var binderUI = UnityEngine.Object.FindObjectOfType<CollectionBinderUI>();
                if (binderUI != null)
                {
                    Logger.LogInfo("Found CollectionBinderUI");
                    var pageText = binderUI.m_PageText.text;
                    Logger.LogInfo($"Current page text: {pageText}");
                    var parts = pageText.Split('/');
                    if (parts.Length > 0)
                    {
                        int.TryParse(parts[0].Trim(), out currentPage);
                        Logger.LogInfo($"Parsed current page: {currentPage}");
                    }
                }
                else
                {
                    Logger.LogWarning("CollectionBinderUI not found!");
                    return;
                }

                pendingValue = ""; // Reset pending value
                textEntry.ShowEntryPanel();
                Logger.LogInfo("Text entry panel shown");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error triggering search: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        private void OnSearchValueChanged(string value)
        {
            Logger.LogInfo($"Search value changed to: {value}");
            pendingValue = value;
        }

        private void NavigateToPage(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            
            if (int.TryParse(value, out int targetPage))
            {
                var binderUI = UnityEngine.Object.FindObjectOfType<CollectionBinderUI>();
                if (binderUI != null && binderUI.m_CollectionAlbum != null)
                {
                    // Get max page
                    var pageText = binderUI.m_PageText.text;
                    var parts = pageText.Split('/');
                    if (parts.Length > 1)
                    {
                        if (int.TryParse(parts[1].Trim(), out int maxPage))
                        {
                            Logger.LogInfo($"Navigating from page {currentPage} to {targetPage} (max: {maxPage})");
                            // Ensure target page is within bounds
                            if (targetPage < 1) targetPage = 1;
                            if (targetPage > maxPage) targetPage = maxPage;

                            // TODO: call the binder animation ctrl flip patch here.

                            Logger.LogInfo($"Navigation complete - now on page {currentPage}");
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (SearchHotkey.Value.IsDown())
            {
                Logger.LogInfo("Search hotkey pressed");
                Logger.LogInfo($"activeGame state: {activeGame}");
                Logger.LogInfo($"UI initialized: {uiInitialized}");
                Logger.LogInfo($"TextEntry null? {textEntry == null}");
                
                if (activeGame)
                {
                    var binderUI = UnityEngine.Object.FindObjectOfType<CollectionBinderUI>();
                    Logger.LogInfo($"BinderUI found: {binderUI != null}");
                    
                    if (binderUI != null && binderUI.m_ScreenGrp.activeSelf)
                    {
                        TriggerSearch();
                    }
                }
            }

            // Handle Enter key for navigation
            if (!string.IsNullOrEmpty(pendingValue) && 
                textEntry != null && 
                textEntry.gameObject.activeSelf && 
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                NavigateToPage(pendingValue);
                pendingValue = ""; // Clear pending value after navigation
            }
        }

        [HarmonyPatch(typeof(InteractionPlayerController), "Start")]
        public class ActiveGamePatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                activeGame = true;
                Logger.LogInfo("Game is now active");
            }
        }
    }
}
