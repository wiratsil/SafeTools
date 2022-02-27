using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using System.Linq;

public class MapGenerater : EditorWindow
{
    public static class ConstantVariables
    {
        public const int BUTTON_HEIGHT = 40;
        public const int WINDOW_BORDER = 5;
        public const int APPLY_HEIGHT = BUTTON_HEIGHT / 2;
        public const int WINDOW_MIN_WIDTH = 256;
        public const int WINDOW_MIN_HEIGHT = WINDOW_MIN_WIDTH + (BUTTON_HEIGHT * 3) + APPLY_HEIGHT + (WINDOW_BORDER * 2);
        public const int DEFAULT_FPS = 60;
    }
    static MapGenerater _instance;

    [MenuItem("Tools/Map Generate/Main Window", false, 0)]
    public static void ShowWindow()
    {
        _instance = (MapGenerater)GetWindow(typeof(MapGenerater), false, "Map Generater");
        _instance.minSize = new Vector2(ConstantVariables.WINDOW_MIN_WIDTH + (ConstantVariables.WINDOW_BORDER * 2), ConstantVariables.WINDOW_MIN_HEIGHT);
        _instance.Show();
    }

    static Object[] source;
    static string countGen = "9";
    static int oldCount = 0;
    Vector2 scrollPos;
    //gui
    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Label("Count of prefab :");
        EditorGUILayout.BeginHorizontal();
        countGen = GUILayout.TextField(countGen, 999);
        if (GUILayout.Button("Clear"))
        {
            source = new Object[int.Parse(countGen)];
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.Space(20);

        if (countGen == "")
            return;

        if (int.Parse(countGen) != oldCount)
        {
            var temp = source;
            source = new Object[int.Parse(countGen)];
            for (int i = 0; i < int.Parse(countGen); i++)
            {
                if (i < temp.Length)
                    source[i] = temp[i];
            }
            oldCount = int.Parse(countGen);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < int.Parse(countGen); i++)
        {
            GeneratePrefab(i);
        }
        EditorGUILayout.EndScrollView();
    }

    public void GeneratePrefab(int index)
    {
        EditorGUILayout.BeginHorizontal();
        source[index] = EditorGUILayout.ObjectField(source[index], typeof(Object), true);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Ins"))
        {
            Transform parentT = Selection.activeGameObject.GetComponent<Transform>();
            GameObject select = PrefabUtility.InstantiatePrefab(source[index], parentT.parent) as GameObject;
            select.transform.position = parentT.position;
            PolygonCollider2D poly = select.GetComponent<PolygonCollider2D>();
            poly.transform.position = new Vector3(poly.transform.position.x + ((float)poly.bounds.size.x * 1f),poly.transform.position.y);
            Selection.activeGameObject = select;
        }
        GUILayout.Space(10);
    }
    
}

