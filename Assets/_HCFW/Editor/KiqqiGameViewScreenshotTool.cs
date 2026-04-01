using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class KiqqiGameViewScreenshotTool
{
    private const string DefaultFolder = "Screenshots";

    [MenuItem("Kiqqi/Capture Screen")]
    public static void CaptureGameViewScreenshot()
    {
        Vector2Int resolution = GetMainGameViewSize();

        if (resolution.x <= 0 || resolution.y <= 0)
        {
            Debug.LogError("Could not determine Game View resolution.");
            return;
        }

        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), DefaultFolder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = "GameView_" + resolution.x + "x" + resolution.y + "_" + timeStamp + ".png";
        string fullPath = Path.Combine(folderPath, fileName);

        ScreenCapture.CaptureScreenshot(fullPath);

        Debug.Log("Game View screenshot saved: " + fullPath);
        EditorUtility.RevealInFinder(fullPath);
    }

    private static Vector2Int GetMainGameViewSize()
    {
        Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null)
        {
            return Vector2Int.zero;
        }

        MethodInfo getSizeMethod = gameViewType.GetMethod(
            "GetSizeOfMainGameView",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        if (getSizeMethod == null)
        {
            return Vector2Int.zero;
        }

        object result = getSizeMethod.Invoke(null, null);
        if (result is Vector2 size)
        {
            return new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
        }

        return Vector2Int.zero;
    }
}