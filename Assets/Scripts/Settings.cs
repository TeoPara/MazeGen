using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    TMP_InputField width, height;
    TMP_InputField startPointX, startPointY;
    TMP_InputField endPointX, endPointY;
    Toggle hexToggle;
    Button fixPrompt;
    Button cancelButton;
    Slider waitTime;
    
    
    /// <summary> On Launch:  find all of the inputs, configure their behaviour, set default values, and center the camera </summary>
    void Awake()
    {
        Transform hud = GameObject.Find("HUD").transform;
        
        Transform hudSize = hud.Find("Size");
        width = hudSize.Find("X").GetComponent<TMP_InputField>();
        height = hudSize.Find("Y").GetComponent<TMP_InputField>();

        Transform hudStart = hud.Find("Start");
        startPointX = hudStart.Find("X").GetComponent<TMP_InputField>();
        startPointY = hudStart.Find("Y").GetComponent<TMP_InputField>();
        
        Transform hudEnd = hud.Find("End");
        endPointX = hudEnd.Find("X").GetComponent<TMP_InputField>();
        endPointY = hudEnd.Find("Y").GetComponent<TMP_InputField>();

        hexToggle = hud.Find("Type").GetComponent<Toggle>();
        
        // Inputs
        width       .onValueChanged.AddListener(_ => { CheckAll(); });
        height      .onValueChanged.AddListener(_ => { CheckAll(); });
        startPointX .onValueChanged.AddListener(_ => { CheckAll(); });
        startPointY .onValueChanged.AddListener(_ => { CheckAll(); });
        endPointX   .onValueChanged.AddListener(_ => { CheckAll(); });
        endPointY   .onValueChanged.AddListener(_ => { CheckAll(); });
        
        // Buttons
        hud.Find("Type").GetComponent<Toggle>().onValueChanged.AddListener(_ => { CheckAll();});
        hud.Find("Run").GetComponent<Button>().onClick.AddListener(AttemptRun);
        cancelButton = hud.Find("Cancel").GetComponent<Button>();
        cancelButton.onClick.AddListener(() => { runInterface?.Cancel?.Invoke(); });
        
        // Slider
        waitTime = hud.Find("WaitTime").GetComponent<Slider>();
        waitTime.onValueChanged.AddListener((value) => { runInterface?.AdjustWaitTime?.Invoke(value); } );
        
        // Fix Prompt
        fixPrompt = hud.Find("FixPrompt").Find("Button").GetComponent<Button>();
        fixPrompt.onClick.AddListener(FixAll);
        
        // Set default values
        FixAll();
        
        // Center camera on the map
        Main.LastCreatedMap.FocusCamera();
    }


    /// <summary> Disables all inputs & shows cancellation prompt </summary>
    /// <param name="c"> True when adjusting settings, false when running </param>
    void ToggleInputs(bool c)
    {
        width       .interactable = c;
        height      .interactable = c;
        startPointX .interactable = c;
        startPointY .interactable = c;
        endPointX   .interactable = c;
        endPointY   .interactable = c;
        fixPrompt   .interactable = c;
        hexToggle   .interactable = c;
        
        cancelButton.gameObject.SetActive(!c);
    }
    
    /// <summary> Checks if all input values are valid </summary>
    bool CheckAll()
    {
        // Clear the trial
        Main.ClearSpawnedCompletionTrials();
        
        bool bad = !ValidateSize(false) || !ValidateStartPoint(false) || !ValidateEndPoint(false);
        fixPrompt.transform.parent.gameObject.SetActive(bad);
        return !bad;
    }

    /// <summary> Attempts to fix all input values </summary>
    /// <returns> Whether fix is successful </returns>
    void FixAll()
    {
        ValidateSize(true);
        ValidateStartPoint(true);
        ValidateEndPoint(true);
    }



    Main.RunInterface runInterface;
    void AttemptRun()
    {
        if (!CheckAll())
            return;

        Main.RunParams runParams = new()
        {
            Size  = new Vector2Int(Convert.ToInt32(width.text),       Convert.ToInt32(height.text)),
            Start = new Vector2Int(Convert.ToInt32(startPointX.text), Convert.ToInt32(startPointY.text)),
            End   = new Vector2Int(Convert.ToInt32(endPointX.text),   Convert.ToInt32(endPointY.text))
        };
        
        // Disable inputs. They will be re-enabled when the run is completed
        ToggleInputs(false);
        
        // Start the Maze generator - define a completionCallback
        runInterface = Main.Run (runParams, hexToggle.isOn, () =>
        {
            ToggleInputs(true);
            runInterface = null;
        });
        // The function returns a class that can be used to mess with the ongoing generation
        // Immediately use it to set the speed 
        runInterface.AdjustWaitTime(waitTime.value);
    }
    
    /// <param name="adjust"> Whether to auto-fix the input values </param>
    /// <returns> Whether the input values are now  </returns>
    /// <returns> Center the camera on the map </returns>
    bool ValidateSize(bool adjust)
    {
        // Get width and height inputs
        int.TryParse(width.text, out int xParsed);
        int.TryParse(height.text, out int yParsed);
        
        // Abort if out of range
        if (!adjust)
        {
            if (xParsed is < 3 or > 250)
                return false;
            if (yParsed is < 3 or > 250)
                return false;
        }
        // Put back into range if out of range
        else
        {
            xParsed = xParsed switch
            {
                < 3 => 3,
                > 250 => 250,
                _ => xParsed
            };
            yParsed = yParsed switch
            {
                < 3 => 3,
                > 250 => 250,
                _ => yParsed
            };
        }
        width.text = xParsed.ToString();
        height.text = yParsed.ToString();

        // Render the map
        Main.CreateMap(new Vector2Int(xParsed, yParsed), hexToggle.isOn).RenderTiles();
        
        // Success
        return true;
    }
    
    /// <param name="isEnd"> False to target StartPoint. True to target EndPoint </param>
    /// <param name="adjust"> Whether to auto-fix the input values </param>
    /// <returns> Whether the input values are now valid </returns>
    bool ValidatePoint(bool isEnd, bool adjust)
    {
        // Abort if size invalid
        if (!ValidateSize(adjust))
            return false;

        int widthInt = Convert.ToInt32(width.text);
        int heightInt = Convert.ToInt32(height.text);
        
        // Invalid input                                                         or out of range
        if (!int.TryParse(!isEnd ? startPointX.text : endPointX.text, out int aX) || aX < 0 || aX >= widthInt)
        {
            if (!adjust) return false;
            
            bool condition = !isEnd && aX < widthInt;
            (!isEnd ? startPointX : endPointX).text = condition ? "0" : (widthInt - 1).ToString();
            aX = condition ? 0 : widthInt - 1;
        }
        // Invalid input                                                         or out of range
        if (!int.TryParse(!isEnd ? startPointY.text : endPointY.text, out int aY) || aY < 0 || aY >= heightInt)
        {
            if (!adjust) return false;
            
            bool condition = !isEnd && aY < heightInt;
            (!isEnd ? startPointY : endPointY).text = condition ? "0" : (heightInt - 1).ToString();
            aY = condition ? 0 : heightInt - 1;
        }
        
        Rendering.PlaceOrb(new Vector2Int(aX, aY), !isEnd ? Color.green : Color.red, !isEnd ? 0 : 1, hexToggle.isOn);
        return true;
    }
    bool ValidateStartPoint(bool adjust) => ValidatePoint(false, adjust);
    bool ValidateEndPoint(bool adjust) => ValidatePoint(true, adjust);
}
