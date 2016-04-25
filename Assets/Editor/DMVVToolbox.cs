using UnityEditor;
using UnityEngine;
using Assets.Editor.MVVReader;

public class DMVVToolbox : EditorWindow
{
    public float myGameTime;
    public GameObject gameObject = null;
    public bool animating = false;

    void animateMVV()
    {
        myGameTime += 0.01f;
        gameObject.GetComponent<Renderer>().sharedMaterial.SetFloat("time", myGameTime);
        SceneView.RepaintAll();
    }

    // Add menu item to the Window menu
    [MenuItem("Window/DMVV Toolbox")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(DMVVToolbox));
    }

    /// <summary>
    /// Build the toolbox
    /// </summary>
    void OnGUI()
    {
        GUILayout.Label("Dynamic MVV", EditorStyles.boldLabel);
        gameObject = (GameObject) EditorGUI.ObjectField(new Rect(3, 23, position.width - 6, 20), "DMVV Mesh", gameObject, typeof(GameObject));
        GUILayout.BeginArea(new Rect(3, 46, position.width - 6, 50));
        if (GUILayout.Button("Load VOML"))
        {
            MVVRoot obj = new MVVRoot();
            var path = EditorUtility.OpenFilePanel(
                    "XML file",
                    "",
                    "xml");
            obj.readFromFile(path, "MAIN");
            gameObject.GetComponent<Renderer>().sharedMaterial = obj.getMaterial("Assets/Resources/MVVShader.shader");
            obj.passToShader(gameObject.GetComponent<Renderer>().sharedMaterial);
        }
        string btn_play = "Start Animation";
        if (animating) btn_play = "Stop Animation";
        if (GUILayout.Button(btn_play))
        {
            if (animating)
            {
                EditorApplication.update -= animateMVV;
                animating = false;
            }
            else
            {
                EditorApplication.update += animateMVV;
                animating = true;
            }
        }
        GUILayout.EndArea();
    }
}