using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ConsoleMenuManager : MonoBehaviour
{
    public static Canvas canvas;
    [SerializeField] private ConsoleCommandManager consoleCommandManager;

    [Header("Console UI")]
    [SerializeField] private VHSButton firstSelectedButton;
    [SerializeField] public TMP_InputField consoleInput;
    [SerializeField] private TextMeshProUGUI consoleOutput;
    
    public static string consoleName;
    public static string inputName;
    
    public static string chatLog = "";
    
    [SerializeField] public bool consoleInitialized;
    
    private void Awake()
    {
        Services.ConsoleMenuManager = this;
        canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        chatLog += consoleOutput.text;
        chatLog = $"{consoleName} Console not initialized. Command mode only.";
    }

    private void Update()
    {
		if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (consoleInput.text == "") return;
            // add input into chat log
            chatLog += "\n" + inputName + " " + consoleInput.text;
            // clear input field
            if (!consoleCommandManager.CheckConsoleCommand(consoleInput.text) && consoleInitialized)
            {
                // TODO: if not command
            }

            consoleInput.text = "";
        }
        // update chat log
        consoleOutput.text = chatLog;
        // clean up chat log
        if (chatLog.Length > 1000) chatLog = chatLog.Substring(chatLog.Length - 1000);
    }
    
    public void ToggleConsole()
    { 
        GameManager.PauseGame(true);
        Services.VHSDisplay.DisplayStatus(GameManager.IsGamePaused? 5 : 0);
        canvas.enabled = GameManager.IsGamePaused;
        Services.ConsoleMenuManager.consoleInput.interactable = GameManager.IsGamePaused;
        firstSelectedButton.button.Select();
    }
}
