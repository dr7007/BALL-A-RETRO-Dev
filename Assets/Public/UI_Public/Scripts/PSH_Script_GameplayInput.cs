using System;
using UnityEngine;

// Closing a gameplay overlay changes the state below any still-open system menu.
// The menu observes this transition instead of restoring its stale opening snapshot.
public static class PSH_Script_GameplayInput
{
    public static event Action Resumed;

    public static void Resume()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Resumed?.Invoke();
    }
}
