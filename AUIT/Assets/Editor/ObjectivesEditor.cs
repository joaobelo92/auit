using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AUIT.AdaptationObjectives;
using UnityEditor;
using UnityEditor.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectivesEditor : EditorWindow
{
    private const string SearchDirectory = "Assets/AUIT/AdaptationObjectives/Objectives";
    private const string ScriptTypeToFind = ".cs";
    private VisualElement _mRightPane;
    
    private GameObject _selectedGameObject;
    private GameObject[] _gameObjects;
    private string[] _gameObjectNames;

    private ToolbarMenu _dropdown;
    private Button _displayButton;
    private ObjectField _objectField;
    private Button _vizButton;

    [MenuItem("AUIT/Adaptation Objectives")]
    public static void ShowObjectivesEditor()
    {
        EditorWindow wnd = GetWindow<ObjectivesEditor>();
        wnd.titleContent = new GUIContent("AUIT Objectives");
    }
    
    public void CreateGUI()
    {        
        List<string> scriptPaths = new List<string>();
        List<string> objectiveNames = new List<string>();
        FindScriptsOfType(SearchDirectory, ScriptTypeToFind, scriptPaths);

        foreach (var script in scriptPaths)
        {
            string pattern = @"/([^/]+)\.cs$";

            Match match = Regex.Match(script, pattern);

            string displayName = Regex.Replace(match.Groups[1].Value, @"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", " $1");
            objectiveNames.Add(displayName);
        }

        var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(splitView);

        var leftPane = new ListView
        {
            makeItem = () => new Label(),
            bindItem = (item, index) => { ((Label)item).text = objectiveNames[index]; },
            itemsSource = objectiveNames
        };
        leftPane.onSelectionChange += OnObjectiveSelectionChange;
        splitView.Add(leftPane);
        _mRightPane = new VisualElement();
        splitView.Add(_mRightPane);
    }
    
    private void OnEnable()
    {
        // When the window is enabled, update the list of GameObject names
        UpdateGameObjectNames();
    }
    
    private void UpdateGameObjectNames()
    {
        
        // Get all GameObjects in the scene
        _gameObjects = FindObjectsOfType<GameObject>();

        // Create an array to store the names
        _gameObjectNames = new string[_gameObjects.Length];

        for (int i = 0; i < _gameObjects.Length; i++)
        {
            _gameObjectNames[i] = _gameObjects[i].name;
        }
    }

    private void OnObjectiveSelectionChange(IEnumerable<object> selectedItems)
    {
        _mRightPane.Clear();
        _mRightPane.style.flexDirection = FlexDirection.Column;
        _mRightPane.style.alignItems = Align.Center;

        var selectedSprite = LoadImage("Assets/AUIT/AdaptationObjectives/Objectives/AnchorToTargetObjective.png");
        if (selectedSprite == null)
            return;

        var objectiveDescription = new Label(
            "This objective aims to position the UI element at a pre-defined offset from the selected context source." +
            "The context source is the the camera by default (user’s head), but it is possible to change " +
            "to any other GameObject with a Transform. Both the offset and the leeway are customizable in the objective" +
            "script")
        {
            style =
            {
                marginLeft = 10,
                marginRight = 10,
                marginTop = 10,
                marginBottom = 10,
                whiteSpace = WhiteSpace.Normal,
            }
        };
        _mRightPane.Add(objectiveDescription);

        var spriteImage = new Image
        {
            style =
            {
                marginBottom = 10,
                maxWidth = 400
            },
            scaleMode = ScaleMode.ScaleToFit,
            sprite = selectedSprite
        };

        _mRightPane.Add(spriteImage);
        
        _objectField = new ObjectField("Add objective to Game Object:")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = 300f,
                marginBottom = 10f
            }
        };
        _objectField.RegisterValueChangedCallback(OnGameObjectSelected);
        _mRightPane.Add(_objectField);
        
        _displayButton = new Button
        {
            text = "Add objective to Game Object",
            style =
            {
                width = 200,
                marginBottom = 10
            }
        };
        _displayButton.SetEnabled(false);

        _mRightPane.Add(_displayButton);
        
        _vizButton = new Button
        {
            text = "Visualize objective",
            style =
            {
                maxWidth = 200f,
                marginBottom = 10f
            }
        };

        _mRightPane.Add(_vizButton);
    }
    
    private void OnGameObjectSelected(ChangeEvent<Object> evt)
    {
        _selectedGameObject = evt.newValue as GameObject;
        _displayButton.SetEnabled(true);
    }

    void FindScriptsOfType(string directory, string scriptType, List<string> scriptPaths)
    {
        string[] files = Directory.GetFiles(directory, "*" + scriptType, SearchOption.AllDirectories);

        foreach (string file in files)
        {
            string scriptContent = File.ReadAllText(file);

            if (scriptContent.Contains("public class") || scriptContent.Contains("public struct"))
            {
                scriptPaths.Add(file);
            }
        }
    }
    
    private Sprite LoadImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            Debug.LogError("Image path is empty!");
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);

        if (sprite == null)
        {
            Debug.LogError("Image not found at path: " + imagePath);
        }

        return sprite;
    }
}
