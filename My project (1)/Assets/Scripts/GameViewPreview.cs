#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Editor preview settings only; this file is excluded from player builds.
public static class GameViewPreview
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        if (File.Exists("Temp/FitGameView.request")) EditorApplication.update += ApplyPending;
    }

    static void ApplyPending()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= ApplyPending;
        Fit();
    }

    [MenuItem("Goblin/Fit Game View at 1x")]
    public static void Fit()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Fit;
            return;
        }
        try
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var windows = Resources.FindObjectsOfTypeAll(type);
            if (windows.Length == 0) throw new InvalidOperationException("Open the Game tab before fitting the preview.");
            foreach (var window in windows)
            {
                // Index zero is Free Aspect: the render target matches the tab's dimensions.
                var selected = type.GetProperty("selectedSizeIndex", Flags);
                if (selected == null || !selected.CanWrite) throw new MissingMemberException("GameView.selectedSizeIndex");
                selected.SetValue(window, 0);
                object zoom = type.GetField("m_ZoomArea", Flags)?.GetValue(window)
                    ?? type.GetProperty("zoomArea", Flags)?.GetValue(window);
                if (zoom == null) throw new MissingMemberException("GameView zoom area");
                var scale = zoom.GetType().GetProperty("scale", Flags);
                if (scale != null && scale.CanWrite) scale.SetValue(zoom, Vector2.one);
                else SetField(zoom, "m_Scale", Vector2.one);
                var translation = zoom.GetType().GetProperty("translation", Flags);
                if (translation != null && translation.CanWrite) translation.SetValue(zoom, Vector2.zero);
                else SetField(zoom, "m_Translation", Vector2.zero);
                ((EditorWindow)window).Repaint();
            }
            File.WriteAllText("Temp/GameViewFitReport.txt", "PASS: Game View set to Free Aspect, 1x scale, zero pan.\n");
            if (File.Exists("Temp/FitGameView.request")) File.Delete("Temp/FitGameView.request");
        }
        catch (Exception error)
        {
            File.WriteAllText("Temp/GameViewFitReport.txt", "FAILED: " + error);
            Debug.LogException(error);
        }
    }

    static void SetField(object instance, string name, object value)
    {
        for (Type type = instance.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Flags | BindingFlags.DeclaredOnly);
            if (field == null) continue;
            field.SetValue(instance, value);
            return;
        }
        throw new MissingFieldException(instance.GetType().Name, name);
    }
}
#endif
// Preview sizing helper.
