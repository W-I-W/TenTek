using System.Collections.Generic;

using UnityEngine;


public static class CursorData
{
    public static bool isCursorVisible { get; private set; } = false;

    private static Dictionary<string, GameObject> s_Objects = new Dictionary<string, GameObject>();

    public static void Add(GameObject panel)
    {
        if (s_Objects.ContainsKey(panel.name)) return;
        s_Objects.Add(panel.name, panel);
    }

    public static void OnActivate(GameObject panel)
    {
        isCursorVisible = true;
        s_Objects[panel.name].SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void OnDeactivate(GameObject panel)
    {
        bool isActivatable = false;
        foreach(var _ in s_Objects)
        {

            if (_.Value.activeSelf == true)
            {
                isActivatable = true;
            }
        }

        isCursorVisible = isActivatable;
        if (!isCursorVisible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
