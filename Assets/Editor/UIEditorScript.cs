using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIControl))]
public class UIEditorScript : Editor
{
    public override void OnInspectorGUI()
    {
        UIControl uiControl = (UIControl)target;

        if (DrawDefaultInspector() || GUILayout.Button("Update"))
        {
            GameObject healthBar = GameObject.Find("HealthBar");
            while (healthBar.transform.childCount != 0)
            {
                DestroyImmediate(healthBar.transform.GetChild(0).gameObject);
            }
            if (healthBar.transform.childCount == 0)
            {
                uiControl.InitHealth();
            }
            uiControl.SetHealth(20);



            GameObject hungerBar = GameObject.Find("HungerBar");
            while (hungerBar.transform.childCount != 0)
            {
                DestroyImmediate(hungerBar.transform.GetChild(0).gameObject);
            }
            if (hungerBar.transform.childCount == 0)
            {
                uiControl.InitHunger();
            }
            uiControl.SetHunger(20);



            GameObject itemBar = GameObject.Find("ItemBar");
            while (itemBar.transform.childCount != 0)
            {
                DestroyImmediate(itemBar.transform.GetChild(0).gameObject);
            }
            if (itemBar.transform.childCount == 0)
            {
                uiControl.InitItems();
            }
        }
    }
}
