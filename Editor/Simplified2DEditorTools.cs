using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;


namespace Simplify2D
{
    public class Simplified2DEditorTools : EditorWindow
    {
        /// <summary>
        /// Default Settings
        /// </summary>
        static string scriptDefaultName = "New2DScript";
        static string canvasDefaultName = "Canvas";
        static Color defaultCanvasColor = new Color(0.2117647f, 0.6f, 0.7490196f);
        static string backgroundDefaultName = "Background";
        static string objectDefaultName = "Object";
        static string defaultSpriteName = "Sprites/Puzzle/element_yellow_diamond_glossy";
        static string eventSystemName = "EventSystem";

        /// <summary>
        /// Save referenct to the selected object
        /// </summary>
        static GameObject selectedGameObject;

        /// <summary>
        /// Flags for Recompile Handling
        /// </summary>
        static bool justRecompiled;
        bool waitingForRecompiling;
        string pathToScript;

        /// <summary>
        /// Constructor called after recompile
        /// </summary>
        static Simplified2DEditorTools()
        {
            justRecompiled = true;
        }        

        /// <summary>
        /// Create a canvas if not already created and
        /// create and adds a new Object to the canvas
        /// </summary>
        [MenuItem("Simplify2D/2D Objekt erstellen", false, 100)]
        static void CreateNew2DObject()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();

            // Create new canvas
            if (canvas == null)
            {
                // Setup canvas
                GameObject canvasGo = new GameObject(canvasDefaultName);
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                canvasGo.AddComponent<Simplified2DCanvas>();

                Vector2 canvasSize = canvas.GetComponent<RectTransform>().rect.size;

                // Create Background
                GameObject background = new GameObject(backgroundDefaultName);
                Image image = background.AddComponent<Image>();
                RectTransform rectTransform = background.GetComponent<RectTransform>();

                rectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 1);

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasSize.x);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, canvasSize.y);

                rectTransform.anchoredPosition = new Vector2(canvasSize.x / 2, canvasSize.y / 2);
                background.transform.SetParent(canvas.transform);

                image.color = defaultCanvasColor;
            }

            // EventSystem?
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject(eventSystemName);
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            Setup2DObject(canvas);
        }

        /// <summary>
        /// Draws a window for create a new script which will
        /// inherate from the Shape2D-Class
        /// </summary>
        [MenuItem("Simplify2D/Neues Skript erstellen", false, 101)]
        static void ShowCreateScriptWindow()
        {
            GetWindow<Simplified2DEditorTools>("2D Skript erstellen");
        }

        /// <summary>
        /// Limit the framerate to 60 FPS
        /// </summary>
        [MenuItem("Simplify2D/Framerate/Begrenzen (~60 FPS)", false, 201)]
        static void LimitFramerateTo60()
        {
            PlayerPrefs.SetInt("LimitFPS", 1);
        }

        [MenuItem("Simplify2D/Framerate/Begrenzen (~60 FPS)", true, 201)]
        static bool LimitFramerateTo60Validate()
        {
            return PlayerPrefs.GetInt("LimitFPS", 0) == 0;
        }

        [MenuItem("Simplify2D/Framerate/Unbegrenzt", false, 221)]
        static void UnlimitFramerate()
        {
            PlayerPrefs.SetInt("LimitFPS", 0);
        }

        [MenuItem("Simplify2D/Framerate/Unbegrenzt", true, 221)]
        static bool UnlimitFramerateValidate()
        {
            return PlayerPrefs.GetInt("LimitFPS", 0) == 1;
        }


        /// <summary>
        /// Creates a new GO with a image component and 
        /// make it a child of a given canvas
        /// </summary>
        /// <param name="canvas">parent canvas for the object</param>
        static void Setup2DObject(Canvas canvas)
        {
            GameObject newObject = new GameObject(objectDefaultName);

            Image image = newObject.AddComponent<Image>();
            RectTransform rectTransform = newObject.GetComponent<RectTransform>();

            Vector2 canvasSize = canvas.GetComponent<RectTransform>().rect.size;

            rectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);

            Vector2 size = new Vector2(100, 100);

            Sprite sprite = Resources.Load<Sprite>(defaultSpriteName);
            image.sprite = sprite;

            size.x = image.sprite.rect.width;
            size.y = image.sprite.rect.height;

            rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, canvasSize.x / 2 - size.x / 2, size.x);
            rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, canvasSize.y / 2 - size.y / 2, size.y);

            newObject.transform.SetParent(canvas.transform);
        }

        void OnGUI()
        {
            GUILayout.Space(20);

            GameObject selected = Selection.activeObject as GameObject;
            if (selected == null || selected.name.Length == 0)
            {
                GUILayout.Label("Selektieren Sie bitte ein GameObject in der Szene auf welchem das Skript platziert werden soll.", EditorStyles.wordWrappedLabel);
                return;
            }
            else
            {
                GUILayout.Label("Bitte einen Namen für das Skript eingeben.", EditorStyles.wordWrappedLabel);
                GUILayout.Space(20);
                scriptDefaultName = EditorGUILayout.TextField("Name: ", scriptDefaultName);
                GUILayout.Space(20);
                if (GUILayout.Button("Erstellen"))
                {
                    CreateNew2DScript(scriptDefaultName);
                }
                GUILayout.Space(20);
                GUILayout.Label("Vergessen Sie nicht, dass erstellte Skript noch in einen Ordner zu legen.", EditorStyles.wordWrappedLabel);
                GUILayout.Space(20);
            }
        }

        /// <summary>
        /// Check if there was a recompile
        /// </summary>
        void Update()
        {
            if (justRecompiled && waitingForRecompiling)
            {
                waitingForRecompiling = false;
                OnRecompileCompleted();
            }
            justRecompiled = false;
        }


        /// <summary>
        /// Writes the script template which derives from
        /// Simplifed2D to a new file 
        /// </summary>
        /// <param name="scriptName">Name of the script</param>
        private void CreateNew2DScript(string scriptName)
        {
            selectedGameObject = Selection.activeObject as GameObject;
            if (selectedGameObject == null || selectedGameObject.name.Length == 0)
            {
                Debug.Log("Wenn Sie ein GameObject beim Erstellen selektiert haben, wird das neue Skript automatisch darauf platziert!");
                return;
            }

            // remove whitespace and minus
            scriptName = scriptName.Replace(" ", "_");
            scriptName = scriptName.Replace("-", "_");
            pathToScript = "Assets/" + scriptName + ".cs";
            if (File.Exists(pathToScript) == false)
            { // do not overwrite
                using (StreamWriter outfile =
                    new StreamWriter(pathToScript))
                {
                    outfile.WriteLine("using UnityEngine;");
                    outfile.WriteLine("using Simplify2D;");
                    outfile.WriteLine(" ");
                    outfile.WriteLine("// TODO: Was macht diese Klasse?");
                    outfile.WriteLine("public class " + scriptName + " : Simplified2D");
                    outfile.WriteLine("{");
                    outfile.WriteLine("\t// Use this for initialization");
                    outfile.WriteLine("\tvoid Start ()");
                    outfile.WriteLine("\t{");
                    outfile.WriteLine("\t");
                    outfile.WriteLine("\t}");
                    outfile.WriteLine(" ");
                    outfile.WriteLine("\t// Update is called once per frame");
                    outfile.WriteLine("\tvoid Update ()");
                    outfile.WriteLine("\t{");
                    outfile.WriteLine("\t");
                    outfile.WriteLine("\t}");
                    outfile.WriteLine("}");
                }//File written
            }

            waitingForRecompiling = true;
            // Refresh database
            AssetDatabase.ImportAsset(pathToScript, ImportAssetOptions.ForceSynchronousImport);
            //AssetDatabase.Refresh();     
        }


        /// <summary>
        /// Called when compile is completed:
        /// Add the new script to the selected GameObject
        /// </summary>
        public void OnRecompileCompleted()
        {
            if (!String.IsNullOrEmpty(pathToScript))
            {
                MonoScript monoScript = AssetDatabase.LoadAssetAtPath(pathToScript, typeof(MonoScript)) as MonoScript;
                selectedGameObject = Selection.activeObject as GameObject;
                Type monoScriptClass = monoScript.GetClass();
                if (selectedGameObject.GetComponent(monoScriptClass) == null)
                {
                    selectedGameObject.AddComponent(monoScriptClass);
                }
            }
        }
    }
}