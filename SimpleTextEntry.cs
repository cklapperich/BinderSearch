using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleTextEntry : MonoBehaviour
{
    // Core UI Components
    private TMP_InputField inputField;
    private GameObject entryPanel;
    
    // Event to notify when text changes
    public delegate void OnTextChangedDelegate(string newText);
    public event OnTextChangedDelegate OnTextChanged;

    private void Start()
    {
        CreateUIElements();
        // Ensure panel starts hidden
        if (entryPanel != null)
        {
            entryPanel.SetActive(false);
        }
    }

    private void CreateUIElements()
    {
        try
        {
            // Create the entry panel
            entryPanel = new GameObject("EntryPanel");
            entryPanel.transform.SetParent(transform, false);
            
            // Add Canvas
            Canvas canvas = entryPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Ensure it's above other UI
            
            // Add CanvasScaler for proper scaling across different resolutions
            CanvasScaler scaler = entryPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            entryPanel.AddComponent<GraphicRaycaster>();

            // Create background panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(entryPanel.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.9f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 100);
            panelRect.anchoredPosition = Vector2.zero;

            // Create input field
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(panel.transform, false);
            
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 1);
            inputRect.offsetMin = new Vector2(20, 20);
            inputRect.offsetMax = new Vector2(-20, -20);
            
            inputField = inputObj.AddComponent<TMP_InputField>();

            // Create text component for input field
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 32;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Center;
            inputText.fontStyle = FontStyles.Bold;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Create placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Enter page number...";
            placeholder.fontSize = 32;
            placeholder.color = new Color(1, 1, 1, 0.5f);
            placeholder.alignment = TextAlignmentOptions.Center;
            
            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            // Configure input field
            inputField.textViewport = inputRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.caretWidth = 2;
            inputField.customCaretColor = true;
            inputField.caretColor = Color.white;
            inputField.selectionColor = new Color(1, 1, 1, 0.3f);
            
            // Set input field settings
            inputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.keyboardType = TouchScreenKeyboardType.NumberPad;
            inputField.characterLimit = 4;

            // Set up event listeners
            inputField.onValueChanged.AddListener(HandleTextChange);
            inputField.onEndEdit.AddListener(HandleEndEdit);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating UI elements: {e.Message}\n{e.StackTrace}");
        }
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(HandleTextChange);
            inputField.onEndEdit.RemoveListener(HandleEndEdit);
        }
    }

    private void HandleTextChange(string newText)
    {
        OnTextChanged?.Invoke(newText);
    }

    private void HandleEndEdit(string finalText)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            CloseEntryPanel();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseEntryPanel();
        }
    }

    public void ShowEntryPanel()
    {
        if (entryPanel != null)
        {
            entryPanel.SetActive(true);
            
            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
                inputField.Select();
            }
        }
    }

    private void CloseEntryPanel()
    {
        if (entryPanel != null)
        {
            entryPanel.SetActive(false);
        }
    }
}
