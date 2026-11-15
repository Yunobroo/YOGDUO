using UnityEngine;
using UnityEngine.InputSystem;

// Keyboard, controller and mouse navigation for the IMGUI menus (title, shop, end screen).
//   Choose: D-pad / left stick / arrow keys / W-S (A-D and left-right also step)
//   Select: A / Cross, Enter or Space      Back: B / Circle or Esc
//   Mouse:  hover selects, click activates
// Usage: call Tick() from Update while the menu is visible, wrap the drawing in
// Begin()/End() inside OnGUI, and draw each choice with Button(). While any menu ticks,
// GameplayBlocked is true so the goblin doesn't move with the same stick.
public class MenuNav
{
    public int index;
    public bool Back { get; private set; }

    int count, registering;
    bool confirm, mouseMoved;
    int lastDir;
    float nextRepeat;
    int openedFrame = -1;

    static int blockedUntilFrame = -1;
    public static bool GameplayBlocked => Time.frameCount <= blockedUntilFrame;
    public static void BlockGameplay() => blockedUntilFrame = Time.frameCount + 1;

    // Call when the menu appears (or its choices change) so the first choice is selected
    // and the press that opened it doesn't also select something.
    public void Open(int startIndex = 0)
    {
        index = startIndex;
        openedFrame = Time.frameCount;
        lastDir = 0;
    }

    public void Tick()
    {
        BlockGameplay();
        var keys = Keyboard.current;
        var pad = Gamepad.current;
        var mouse = Mouse.current;

        int dir = 0;
        if (keys != null)
        {
            if (keys.upArrowKey.isPressed || keys.wKey.isPressed || keys.leftArrowKey.isPressed || keys.aKey.isPressed) dir = -1;
            else if (keys.downArrowKey.isPressed || keys.sKey.isPressed || keys.rightArrowKey.isPressed || keys.dKey.isPressed) dir = 1;
        }
        if (pad != null && dir == 0)
        {
            Vector2 stick = pad.leftStick.ReadValue() + pad.dpad.ReadValue();
            if (stick.y > .5f || stick.x < -.5f) dir = -1;
            else if (stick.y < -.5f || stick.x > .5f) dir = 1;
        }
        // One step per press; holding repeats after a short pause.
        if (dir != 0 && count > 0 && (dir != lastDir || Time.unscaledTime >= nextRepeat))
        {
            index = ((index + dir) % count + count) % count;
            nextRepeat = Time.unscaledTime + (dir != lastDir ? .4f : .12f);
        }
        lastDir = dir;

        bool fresh = Time.frameCount > openedFrame;
        confirm = fresh && ((keys != null && (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame))
                         || (pad != null && pad.buttonSouth.wasPressedThisFrame));
        Back = fresh && ((keys != null && keys.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame));
        mouseMoved = mouse != null && mouse.delta.ReadValue().sqrMagnitude > .01f;
    }

    public void Begin() => registering = 0;

    public void End()
    {
        count = registering;
        if (count > 0) index = Mathf.Clamp(index, 0, count - 1);
    }

    // Draws a choice. True when it is clicked, or selected and confirmed with keys / controller.
    public bool Button(Rect rect, string label, GUIStyle style, float ui = 1)
    {
        int i = registering++;
        var e = Event.current;
        if (mouseMoved && e.type == EventType.Repaint && rect.Contains(e.mousePosition)) index = i;
        bool selected = i == index;
        bool clicked = GUI.Button(rect, label, style);
        if (selected && e.type == EventType.Repaint)
        {
            // Torch-lit frame and a small marker show which choice the controller is on.
            DungeonUI.Outline(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), DungeonUI.Torch, 2);
            float marker = 6 * ui;
            DungeonUI.Fill(new Rect(rect.x - 3 - marker * 2, rect.center.y - marker * .5f, marker, marker), DungeonUI.Torch);
        }
        if (selected && confirm && GUI.enabled && e.type == EventType.Repaint)
        {
            confirm = false;
            return true;
        }
        return clicked;
    }
}
