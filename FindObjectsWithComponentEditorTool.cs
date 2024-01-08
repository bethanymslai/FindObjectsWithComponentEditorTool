using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
public class FindObjectsWithComponentEditorTool : EditorWindow
{
    #region Class Variables

    #region Component Search Variables
    private bool enableComponentSearchButton = true;
    private string componentType = "";
    private System.Type currentComponentType;
    private readonly List<Component> components = new List<Component>();
    private readonly List<Component> componentFilter = new List<Component>();
    private const string ComponentSearchGUI = "componentSearchGUI";
    private const BindingFlags ComponentFlags = 
        BindingFlags.NonPublic | BindingFlags.Public |
        BindingFlags.Instance | BindingFlags.Static;
    #endregion

    #region Object Search Variables
    private bool showRootObjectsOnly = true;
    private string objectNameSearchInput = "";
    private Vector2 gameObjectsScrollPosition;
    private const string ObjectSearchGUI = "objectSearchGUI";
    #endregion

    #region Function/Parameter Variables
    private string functionName = "";
    private string functionNameSearchInput = "";
    private readonly List<MethodInfo> functions = new List<MethodInfo>();
    private readonly List<MethodInfo> functionFilter = new List<MethodInfo>();
    private Vector2 functionsScrollPosition;
    private string[] functionParamInputs;
    private string[] functionParamGUINames;
    private ParameterInfo[] functionParameterInfos;
    private bool shouldTryRunFunction;
    private string parameterTypeName;
    private string parameterValName;
    private const string FunctionSearchGUI = "functionSearchGUI";
    private const string FunctionParamGUI = "functionParamGUI";
    private const BindingFlags FunctionFlags =
        BindingFlags.NonPublic | BindingFlags.Public |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    #endregion

    #region GUISkin Variables
    private GUISkin skin;
    private Color greyColor;
    private Color lightGreyColor;
    private Color darkGreyColor;
    private Color blackColor;
    private Color whiteColor;
    private Color cyanColor;
    private Texture2D lightGreyTex;
    private Texture2D blackTex;
    #endregion

    #endregion

    [MenuItem("Tools/Find Objects With Components")]
    public static void ShowWindow() => GetWindow(typeof(FindObjectsWithComponentEditorTool));

    public void OnInspectorUpdate() => Repaint();

    private void OnGUI()
    {
        SetWindowDimensions();
        InitializeColorsAndTextures();
        InitializeSkin();
    
        GUILayout.BeginHorizontal();
        CreditsGUI();
        GUILayout.FlexibleSpace();
        ToggleShowRootObjectsOnlyGUI();
        ToggleSearchBarGUI();
        GUILayout.EndHorizontal();
    
        GUILayout.Space(10);
    
        GUILayout.Box("Find all objects with a component in the scene.");
        GUILayout.BeginHorizontal();
        ComponentSearchbarGUI();
        ComponentSearchButtonGUI();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        if (components != null && components.Count > 0)
        {
            skin.scrollView.normal.background = Texture2D.grayTexture;
            gameObjectsScrollPosition = GUILayout.BeginScrollView(
                gameObjectsScrollPosition, GUILayout.Height(165));
            GameObjectSearchBarGUI();
            GUILayout.Space(2);
            GameObjectsListGUI();
            GUILayout.EndScrollView();
            GUILayout.Space(5);
            AllActiveEnabledGUIButtons();
        
            GUILayout.Space(10);

            skin.button.wordWrap = true;
            PopulateFunctionsList();
            if (functions != null && functions.Count > 0)
            {
                GUILayout.Box(new GUIContent(CheckIfSelectedObjectsContainsComponentObject()? $"Run a function on selected objs with the specified component." : $"Run a function on all objs with the specified component."
                    , "Functions with multiple parameters or unsupported parameter types are excluded."));
                functionsScrollPosition = GUILayout.BeginScrollView(functionsScrollPosition, GUILayout.Height(165));
                FunctionSearchbarGUI();
                GUILayout.Space(5);
                FunctionsListGUI();
                GUILayout.EndScrollView();
            }
        }
    }

    #region GUI Functions
    private void SetWindowDimensions()
    {
        minSize = new Vector2(365,540);
        maxSize = minSize;
    }

    private void InitializeColorsAndTextures()
    {
        // colors
        greyColor = new Color(204/255f, 204/255f, 204/255f, 255/255f);
        darkGreyColor = new Color(191 / 255f, 191 / 255f, 191 / 255f, 255 / 255f);
        lightGreyColor = new Color(227/255f,227/255f,227/255f,255/255f);
        blackColor = Color.black;
        whiteColor = Color.white;
        cyanColor = Color.cyan;
    
        // textures
        blackTex = Texture2D.blackTexture;

        // initialize light grey tex
        lightGreyTex = Texture2D.linearGrayTexture;
        var fillColorArray = lightGreyTex.GetPixels();
        for(var i = 0; i < fillColorArray.Length; ++i)
        {
            fillColorArray[i] = lightGreyColor;
        }
        lightGreyTex.SetPixels(fillColorArray);
        lightGreyTex.Apply();
    }

    private void InitializeSkin()
    {
        skin = CreateInstance<GUISkin>();

        // set GUISkin vals
        skin.box.normal.textColor = greyColor;
        skin.box.hover.textColor = greyColor;
        skin.box.active.textColor = blackColor;
        skin.box.border.left = 6;
        skin.box.border.right = 6;
        skin.box.border.top = 6;
        skin.box.border.bottom = 6;
        skin.box.margin.left = 4;
        skin.box.margin.right = 4;
        skin.box.margin.top = 4;
        skin.box.margin.bottom = 4;
        skin.box.padding.left = 4;
        skin.box.padding.right = 4;
        skin.box.padding.top = 4;
        skin.box.padding.bottom = 4;
        skin.box.wordWrap = true;
        skin.box.alignment = TextAnchor.UpperCenter;
    
        skin.button.normal.textColor = lightGreyColor;
        skin.button.hover.textColor = whiteColor;
        skin.button.active.textColor = greyColor;
        skin.button.border.left = 6;
        skin.button.border.right = 6;
        skin.button.border.top = 6;
        skin.button.border.bottom = 4;
        skin.button.margin.left = 4;
        skin.button.margin.right = 4;
        skin.button.margin.top = 4;
        skin.button.margin.bottom = 4;
        skin.button.padding.left = 6;
        skin.button.padding.right = 6;
        skin.button.padding.top = 3;
        skin.button.padding.bottom = 3;
        skin.button.alignment = TextAnchor.MiddleCenter;

        skin.toggle.stretchWidth = false;
        skin.toggle.normal.textColor = darkGreyColor;
        skin.toggle.hover.textColor = whiteColor;
        skin.toggle.active.textColor = whiteColor;
        skin.toggle.onNormal.textColor = lightGreyColor;
        skin.toggle.onHover.textColor = whiteColor;
        skin.toggle.onActive.textColor = whiteColor;
        skin.toggle.border.left = 14;
        skin.toggle.border.right = 0;
        skin.toggle.border.top = 14;
        skin.toggle.border.bottom = 0;
        skin.toggle.margin.left = 4;
        skin.toggle.margin.right = 4;
        skin.toggle.margin.top = 4;
        skin.toggle.margin.bottom = 4;
        skin.toggle.padding.left = 17;
        skin.toggle.padding.right = 5;
        skin.toggle.padding.top = 3;
        skin.toggle.padding.bottom = 0;
        skin.toggle.alignment = TextAnchor.UpperLeft;
    
        skin.label.normal.textColor = whiteColor;
        skin.label.hover.textColor = blackColor;
        skin.label.active.textColor = blackColor;
        skin.label.border.left = 0;
        skin.label.border.right = 0;
        skin.label.border.top = 0;
        skin.label.border.bottom = 0;
        skin.label.margin.left = 4;
        skin.label.margin.right = 4;
        skin.label.margin.top = 4;
        skin.label.margin.bottom = 4;
        skin.label.padding.left = 0;
        skin.label.padding.right = 0;
        skin.label.padding.top = 3;
        skin.label.padding.bottom = 3;
        skin.label.alignment = TextAnchor.UpperLeft;
    
        skin.scrollView.normal.textColor = blackColor;
        skin.scrollView.hover.textColor = blackColor;
        skin.scrollView.active.textColor = blackColor;
        skin.scrollView.border.left = 0;
        skin.scrollView.border.right = 0;
        skin.scrollView.border.top = 0;
        skin.scrollView.border.bottom = 0;
        skin.scrollView.margin.left = 10;
        skin.scrollView.margin.right = 10;
        skin.scrollView.margin.top = 0;
        skin.scrollView.margin.bottom = 0;
        skin.scrollView.padding.left = 0;
        skin.scrollView.padding.right = 0;
        skin.scrollView.padding.top = 5;
        skin.scrollView.padding.bottom = 5;
        skin.scrollView.alignment = TextAnchor.UpperLeft;

        skin.button.normal.background = blackTex;
        skin.button.hover.background = blackTex;
        skin.button.active.background = blackTex;
        skin.button.clipping = TextClipping.Clip;
        skin.verticalScrollbarThumb.normal.background = lightGreyTex;
        skin.verticalScrollbarThumb.fixedWidth = 4;
        skin.verticalScrollbar.fixedWidth = 4;
        skin.verticalScrollbar.normal.background = null;
        skin.horizontalScrollbarThumb.normal.background = lightGreyTex;
        skin.horizontalScrollbarThumb.fixedHeight = 4;
        skin.horizontalScrollbar.fixedHeight = 4;
        skin.horizontalScrollbar.normal.background = null;
        skin.verticalScrollbarUpButton.normal.background = blackTex;
        skin.verticalScrollbarDownButton.normal.background = blackTex;
        skin.horizontalScrollbarLeftButton.normal.background = blackTex;
        skin.horizontalScrollbarRightButton.normal.background = blackTex;
    
        GUI.skin = skin;
    }

    private void CreditsGUI()
    {
        if (GUILayout.Button("Created by Bethany Lai. v1"))
            Application.OpenURL("https://www.linkedin.com/in/bethanylai/");
    }

    private void ToggleSearchBarGUI()
    {
        enableComponentSearchButton = GUILayout.Toggle(enableComponentSearchButton, new GUIContent("", "Manual search (recommended)"));
    }
    
    private void ToggleShowRootObjectsOnlyGUI()
    {
        var origToggleLeftPadding = skin.toggle.padding.left;
        
        skin.toggle.padding.left = 8;
        showRootObjectsOnly = GUILayout.Toggle(showRootObjectsOnly,
            new GUIContent("", "Show root objects only (recommended)"));

        skin.toggle.padding.left = origToggleLeftPadding;
    }

    private void ComponentSearchbarGUI()
    {
        EditorGUIUtility.labelWidth = 110;
        EditorGUIUtility.fieldWidth = 200;
    
        GUI.SetNextControlName(ComponentSearchGUI);
        componentType = EditorGUILayout.TextField("Component Name: ", componentType);
    
        NullCheckOnSwitchBetweenPlayAndEditMode();
    }

    private void ComponentSearchButtonGUI()
    {
        skin.button.normal.background = null;
        skin.button.hover.background = null;
        skin.button.active.background = null;
    
        if (enableComponentSearchButton)
        {
            var newSkin = CreateInstance<GUISkin>();
            newSkin.button.normal.textColor = lightGreyColor;
            newSkin.button.hover.textColor = whiteColor;
            newSkin.button.active.textColor = greyColor;
            newSkin.button.border.left = 6;
            newSkin.button.border.right = 6;
            newSkin.button.border.top = 6;
            newSkin.button.border.bottom = 4;
            newSkin.button.margin.left = 4;
            newSkin.button.margin.right = 4;
            newSkin.button.margin.top = 0;
            newSkin.button.margin.bottom = 4;
            newSkin.button.padding.left = 6;
            newSkin.button.padding.right = 6;
            newSkin.button.padding.top = 3;
            newSkin.button.padding.bottom = 3;
            newSkin.button.alignment = TextAnchor.MiddleCenter;
            newSkin.button.normal.background = null;
            newSkin.button.hover.background = null;
            newSkin.button.active.background = null;
            GUI.skin = newSkin;
            ClearComponentListOnEmptySearch();
            if (GUILayout.Button("Find"))
            {
                SearchButtonFunctionality();
            }

            GUI.skin = skin;
        }
        else
        {
            AutoSearchFunctionality();
        }
    }

    private void GameObjectSearchBarGUI()
    {
        EditorGUIUtility.labelWidth = 50;
        EditorGUIUtility.fieldWidth = 270;
    
        GUI.SetNextControlName(ObjectSearchGUI);
        objectNameSearchInput = EditorGUILayout.TextField("Search: ", objectNameSearchInput);
        RefreshComponentFilter();
    }

    private void GameObjectsListGUI()
    {
        skin.button.normal.background = blackTex;
        skin.button.hover.background = blackTex;
        skin.button.active.background = blackTex;
        foreach (var component in componentFilter)
        {
            GUILayout.BeginHorizontal();
        
            // active checkbox
            ToggleGameObjectActive(component, GUILayout.Toggle(GetIsGameObjectActive(component), new GUIContent("Active", "Toggle " + component.gameObject.name + " active/inactive")));
        
            // enabled checkbox
            var canEnable = GetIsComponentEnabled(component);
            if (canEnable >= 0)
                ToggleComponentEnabled(component, GUILayout.Toggle(canEnable == 1, new GUIContent("Enabled", "Toggle " + component.gameObject.name + "'s " + component.GetType().Name + " component enabled/disabled")));
        
            // original style vals
            var origColor = skin.button.normal.textColor;
            var origPadding = skin.button.padding.left;
            var origHover = skin.button.hover.textColor;
            var origActive = skin.button.active.textColor;
            var origOnActive = skin.button.onActive.textColor;
            skin.button.alignment = TextAnchor.MiddleLeft;
            skin.button.padding.left = 30;
            if (Selection.gameObjects.Contains(component.gameObject))
                skin.button.normal.textColor = cyanColor;
            if (Selection.gameObjects.Contains(component.gameObject))
            {
                skin.button.hover.textColor = cyanColor;
                skin.button.active.textColor = cyanColor;
                skin.button.onActive.textColor = cyanColor;
            }

            CheckIfSelectedGameObjectInWindow(component);
        
            // revert style vals
            skin.button.normal.textColor = origColor;
            skin.button.padding.left = origPadding;
            skin.button.alignment = TextAnchor.MiddleCenter;
            skin.button.hover.textColor = origHover;
            skin.button.active.textColor = origActive;
            skin.button.onActive.textColor = origOnActive;
        
            GUILayout.EndHorizontal();
        }
    }

    private void AllActiveEnabledGUIButtons()
    {
        skin.button.normal.background = null;
        skin.button.hover.background = null;
        skin.button.active.background = null;
        skin.button.fixedWidth = 150;
    
        GUILayout.BeginHorizontal();
        GUILayout.Space(30);
    
        // active
        if (GUILayout.Button(CheckIfSelectedObjectsContainsComponentObject()? 
                new GUIContent("Selected active", "Set selected objects listed above to active") :
                new GUIContent("All active", "Set all objects listed above to active")))
        {
            ToggleAllGameObjectsActive(true);
        }
        if (GUILayout.Button(CheckIfSelectedObjectsContainsComponentObject()? 
                new GUIContent("Selected inactive", "Set selected objects listed above to inactive") :
                new GUIContent("All inactive", "Set all objects listed above to inactive")))
        {
            ToggleAllGameObjectsActive(false);
        }
        GUILayout.EndHorizontal();
    
        // enabled
        if (GetIsComponentEnabled(components[0]) >= 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            if (GUILayout.Button(CheckIfSelectedObjectsContainsComponentObject()? 
                    new GUIContent("Enable selected", "Enable selected " + components[0].GetType().Name + " components on objects listed above") :
                    new GUIContent("Enable all", "Enable all " + components[0].GetType().Name + " components on objects listed above")))
            {
                ToggleAllComponentsEnabled(true);
            }
            if (GUILayout.Button(CheckIfSelectedObjectsContainsComponentObject()? 
                    new GUIContent("Disable selected", "Disable selected " + components[0].GetType().Name + " components on objects listed above") :
                    new GUIContent("Disable all", "Disable all " + components[0].GetType().Name + " components on objects listed above")))
            {
                ToggleAllComponentsEnabled(false);
            }

            GUILayout.EndHorizontal();
        }
    }

    private void FunctionSearchbarGUI()
    {
        GUI.SetNextControlName(FunctionSearchGUI);
        functionNameSearchInput = EditorGUILayout.TextField("Search: ", functionNameSearchInput);
        RefreshFunctionFilterAndParams();
    }
    
    private void FunctionsListGUI()
    {
        var focused = GUI.GetNameOfFocusedControl();
        for (var i = 0; i < functionFilter.Count; i++)
        {
            functionParameterInfos = functionFilter[i].GetParameters();
            if (!CheckIfValidParameters()) continue;
            
            GUILayout.BeginHorizontal();
            functionName = functionFilter[i].Name;
            shouldTryRunFunction = GUILayout.Button(functionName);
            parameterTypeName = "";
            parameterValName = "";
            if (functionParameterInfos.Length > 0)
            {
                PopulateParameterTypeAndFillerTexts();
            
                // parameter text field dimensions
                var parameter = functionParameterInfos[0];
                var textDimensions = GUI.skin.label.CalcSize(new GUIContent(parameterTypeName));
                var paramTextDimensions = GUI.skin.label.CalcSize(new GUIContent(parameter.Name));
                EditorGUIUtility.labelWidth = 10 + paramTextDimensions.x + textDimensions.x;
                EditorGUIUtility.fieldWidth = 130;

                GUI.SetNextControlName(functionParamGUINames[i]);
                if (CheckIfSelectedFunctionParamTextField(i))
                    functionParamInputs[i] = EditorGUILayout.TextField(parameterTypeName + " " + parameter.Name, functionParamInputs[i]);
                else
                    EditorGUILayout.TextField(parameterTypeName + " " + parameter.Name, parameterValName);
            }
            GUILayout.EndHorizontal();
            
            TryRunFunctionWithParameters(i);
        }
    }
    
    #endregion

    #region Functionality

    #region Active/Enabled/Select Functions
    private bool GetIsGameObjectActive(Component comp)
    {
        return comp.gameObject.activeInHierarchy;
    }

    private void ToggleGameObjectActive(Component comp, bool toggle)
    {
        comp.gameObject.SetActive(toggle);
    }

    private bool GetIsAllGameObjectsActive()
    {
        return components.Select(GetIsGameObjectActive).All(compActive => compActive);
    }

    private void ToggleAllGameObjectsActive(bool toggle)
    {
        foreach (var comp in components)
        {
            if (CheckIfSelectedObjectsContainsComponentObject())
            {
                if (Selection.gameObjects.Contains(comp.gameObject))
                    ToggleGameObjectActive(comp, toggle);
            }
            else
                ToggleGameObjectActive(comp, toggle);
        }
    }

    private int GetIsComponentEnabled(Component comp)
    {
        PropertyInfo property = comp.GetType().GetProperty("enabled", ComponentFlags);
        if (property != null && property.PropertyType == typeof(bool))
        {
            if (property.GetValue(comp).Equals(true))
                return 1;
            return 0;
        }
        return -1;
    }

    private void ToggleComponentEnabled(Component comp, bool toggle)
    {
        PropertyInfo property = comp.GetType().GetProperty("enabled", ComponentFlags);
        if (property != null && property.PropertyType == typeof(bool)) {
            property.SetValue(comp,toggle);
        }
    }

    private int GetIsAllComponentsEnabled()
    {
        foreach (var compActive in components.Select(GetIsComponentEnabled))
        {
            if (compActive == -1) return -1;
            if (compActive == 0) return 0;
        }
        return 1;
    }

    private void ToggleAllComponentsEnabled(bool toggle)
    {
        foreach (var comp in components)
        {
            if (CheckIfSelectedObjectsContainsComponentObject())
            {
                if (Selection.gameObjects.Contains(comp.gameObject))
                    ToggleComponentEnabled(comp, toggle);
            }
            else
                ToggleComponentEnabled(comp, toggle);
        }
    }
    
    private void CheckIfSelectedGameObjectInWindow(Component component)
    {
        if (GUILayout.Button(component.gameObject.name))
        {
            Object[] newSelection;
            if (Selection.gameObjects.Contains(component.gameObject)) // if the object is already selected
            {
                newSelection = new Object[Selection.gameObjects.Length - 1];
                int count = 0;
                foreach (var obj in Selection.gameObjects)
                {
                    if (obj == component.gameObject)
                        continue;
                    newSelection[count] = obj;
                    count++;
                }
            }
            else // if the object is not selected
            {
                newSelection = new Object[Selection.gameObjects.Length + 1];
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                    newSelection[i] = Selection.gameObjects[i];
                newSelection[newSelection.Length - 1] = component.gameObject;
            }
            Selection.objects = newSelection;
        }
    }
    
    #endregion

    #region Component Search Functions
    private bool CheckIfSelectedObjectsContainsComponentObject()
    {
        return Selection.gameObjects.Length > 0 && components.Where(comp => comp != null).Any(comp => Selection.Contains(comp.gameObject));
    }

    private void FindAllComponentsInScene()
    {
        var objs = (GameObject[])FindObjectsOfType(typeof(GameObject));
        foreach (var obj in objs)
        {
            var compsInChildren = obj.GetComponentsInChildren(typeof(Component));
            foreach (var comp in compsInChildren)
            {
                var noSpace = componentType.Replace(" ", string.Empty);
                if (!enableComponentSearchButton)
                {
                    if (components.Contains(comp)) continue;
                    if (components.Count > 0 &&
                        !(components[0].GetType().Name.ToLower().Equals(componentType.ToLower()) ||
                          components[0].GetType().Name.ToLower().Equals(noSpace.ToLower())))
                        components.Clear();
                }

                if (!comp) continue;

                if (CheckForBaseTypes(comp, noSpace))
                {
                    if (!showRootObjectsOnly)
                        components.Add(comp);
                    else
                    {
                        switch (noSpace.ToLower())
                        {
                            case "transform":
                            {
                                var compParent = comp.transform.root;
                                if (!components.Contains(compParent))
                                    components.Add(compParent);
                                break;
                            }
                            case "renderer":
                            {
                                var compParent = GetParentComponent<Renderer>(comp);
                                if (!components.Contains(compParent))
                                    components.Add(compParent);
                                break;
                            }
                            case "collider":
                            {
                                var compParent = GetParentComponent<Collider>(comp);
                                if (!components.Contains(compParent))
                                    components.Add(compParent);
                                break;
                            }
                            default:
                                components.Add(comp);
                                break;
                        }
                    }
                }
            }
        }
    }

    private Component GetParentComponent<T>(Component comp)
    {
        var parentComponent = comp;
        var currentTrans = comp.transform;
        while (currentTrans.parent != null)
        {
            if (currentTrans.parent.TryGetComponent<T>(out var parComp))
            {
                parentComponent = parComp as Component;
            }
            currentTrans = currentTrans.parent;
        }

        return parentComponent;
    }

    private bool CheckForBaseTypes(Component comp, string noSpace)
    {
        var baseTypes = new List<System.Type>();
        var baseType = comp.GetType();
        baseTypes.Add(baseType);
        while (baseType.BaseType != null && (
                    !baseType.BaseType.Name.ToLower().Equals("component") &&
                    !baseType.BaseType.Name.ToLower().Equals("behaviour")
                    ))
        {
            baseType = baseType.BaseType;
            baseTypes.Add(baseType);
        }

        /*
        var str = comp.name + ": ";
        foreach (var type in baseTypes)
            str += type.Name + ", ";
        Debug.Log(str);*/

        bool match = false;
        foreach (var type in baseTypes.Where(type => type.Name.ToLower().Equals(componentType.ToLower()) ||
                                                     type.Name.ToLower().Equals(noSpace.ToLower())))
        {
            currentComponentType = type;
            match = true;
        }
        return match;
    }
    
    private void SearchButtonFunctionality()
    {
        components.Clear();
        FindAllComponentsInScene();
    }

    private void AutoSearchFunctionality()
    {
        if (GUI.GetNameOfFocusedControl().Equals(ComponentSearchGUI))
        {
            ClearComponentListOnEmptySearch();
            FindAllComponentsInScene();
        }
    }

    private void ClearComponentListOnEmptySearch()
    {
        if (componentType.Length <= 0)
            components.Clear();
    }

    private void RefreshComponentFilter()
    {
        if (GUI.GetNameOfFocusedControl().Equals(ObjectSearchGUI) || GUI.GetNameOfFocusedControl().Equals(ComponentSearchGUI))
        {
            componentFilter.Clear();
            foreach (var component in components.Where(component => component != null).Where(component => component.gameObject.name.ToLower().Contains(objectNameSearchInput)))
            {
                componentFilter.Add(component);
            }
        }
    }

    private void NullCheckOnSwitchBetweenPlayAndEditMode()
    {
        if (components.Count > 0 && components[0] == null) // null check
        {
            components.Clear();
            componentType = "";
        }
    }
    
    #endregion
    
    #region Function/Parameter Functions
    
    private void PopulateFunctionsList()
    {
        functions.Clear();
        if (components != null && components.Count > 0)
        {
            var type = currentComponentType;
            var methods = type.GetMethods(FunctionFlags);
            foreach (var method in methods)
            {
                if (method.DeclaringType != null && method.DeclaringType.IsSubclassOf(typeof(MonoBehaviour)) &&
                    method.Name != "Update" && method.Name != "Start" && method.Name != "LateUpdate" && method.Name != "Awake" && method.Name != "FixedUpdate" && 
                    method.Name != "OnDestroy" && method.Name != "OnEnable" && method.Name != "OnDisable" &&
                    method.Name != "OnTriggerEnter" && method.Name != "OnTriggerStay" && method.Name != "OnTriggerExit" &&
                    method.Name != "OnCollisionEnter" && method.Name != "OnCollisionStay" && method.Name != "OnCollisionExit" && 
                    method.Name != "OnTriggerEnter" && method.Name != "OnTriggerStay" && method.Name != "OnTriggerExit" && 
                    method.Name != "OnCollisionEnter2D" && method.Name != "OnCollisionStay2D" && method.Name != "OnCollisionExit2D" &&
                    method.Name != "OnTriggerEnter2D" && method.Name != "OnTriggerStay2D" && method.Name != "OnTriggerExit2D" &&
                    method.ReturnType.Name != "IEnumerator")
                    functions.Add(method);
            }
        }
    }
    
    private void RefreshFunctionFilterAndParams()
    {
        if (GUI.GetNameOfFocusedControl().Equals(FunctionSearchGUI) || GUI.GetNameOfFocusedControl().Equals(ComponentSearchGUI))
        {
            functionFilter.Clear();
            foreach (var method in functions.Where(method => method.Name.ToLower().Contains(functionNameSearchInput)))
                functionFilter.Add(method);
            
            functionParamInputs = new string[functionFilter.Count];
            for (int i = 0; i < functionParamInputs.Length; i++)
                functionParamInputs[i] = "";
            
            functionParamGUINames = new string[functionFilter.Count];
            for (int i = 0; i < functionParamGUINames.Length; i++)
                functionParamGUINames[i] = FunctionParamGUI + i.ToString();
        }
    }
    
    private bool CheckIfValidParameters()
    {
        if (functionParameterInfos.Length > 1) return false;
        if (functionParameterInfos.Length > 0)
        {
            if (!functionParameterInfos[0].ParameterType.Name.ToLower().Contains("int") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("single") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("string") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("boolean") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("gameobject") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("transform") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("collider") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("rigidbody") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("vector2") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("vector3") &&
                !functionParameterInfos[0].ParameterType.Name.ToLower().Contains("color"))
                return false;
        }

        return true;
    }

    private void PopulateParameterTypeAndFillerTexts()
    {
        var parameter = functionParameterInfos[0];
        switch (parameter.ParameterType.Name.ToLower())
        {
            case "int32":
                parameterTypeName = "int";
                parameterValName = "e.g., 3";
                break;
            case "single":
                parameterTypeName = "float";
                parameterValName = "e.g., 3.5";
                break;
            case "string":
                parameterTypeName = "string";
                parameterValName = "e.g., hello";
                break;
            case "boolean":
                parameterTypeName = "bool";
                parameterValName = "e.g., true";
                break;
            case "transform":
                parameterTypeName = "Transform";
                parameterValName = "name of GameObject";
                break;
            case "gameobject":
                parameterTypeName = "GameObject";
                parameterValName = "name of GameObject";
                break;
            case "collider2d":
                parameterTypeName = "Collider2D";
                parameterValName = "name of GameObject";
                break;
            case "rigidbody2d":
                parameterTypeName = "Rigidbody2D";
                parameterValName = "name of GameObject";
                break;
            case "rigidbody":
                parameterTypeName = "Rigidbody";
                parameterValName = "name of GameObject";
                break;
            case "collider":
                parameterTypeName = "Collider";
                parameterValName = "name of GameObject";
                break;
            case "vector3":
                parameterTypeName = "Vector3";
                parameterValName = "e.g., (3, 2, 1)";
                break;
            case "vector2":
                parameterTypeName = "Vector2";
                parameterValName = "e.g., (3, 2)";
                break;
            case "color":
                parameterTypeName = "Color";
                parameterValName = "e.g., (255, 255, 255)";
                break;
        }
    }

    private bool CheckIfSelectedFunctionParamTextField(int i)
    {
        return GUI.GetNameOfFocusedControl().Equals(functionParamGUINames[i]) || functionParamInputs[i].Length > 0;
    }

    private void TryRunFunctionWithParameters(int i)
    {
        if (shouldTryRunFunction)
        {
            if (functionParameterInfos.Length <= 0)
                RunFunctionOnAllComponents();
            else switch (parameterTypeName)
            {
                case "int":
                {
                    if (int.TryParse(functionParamInputs[i], out var returnVal))
                        RunFunctionOnAllComponents(returnVal);
                    break;
                }
                case "string":
                    if (functionParamInputs[i].Length > 0)
                        RunFunctionOnAllComponents(functionParamInputs[i]);
                    break;
                case "float":
                {
                    if (float.TryParse(functionParamInputs[i], out var returnVal))
                        RunFunctionOnAllComponents(returnVal);
                    break;
                }
                case "bool" when functionParamInputs[i].ToLower().Equals("true") || functionParamInputs[i].Equals("1"):
                    RunFunctionOnAllComponents(true);
                    break;
                case "bool":
                {
                    if (functionParamInputs[i].ToLower().Equals("false") || functionParamInputs[i].Equals("0"))
                        RunFunctionOnAllComponents(false);
                    break;
                }
                case "Vector2":
                case "Vector3":
                case "Color":
                {
                    var copy = functionParamInputs[i];
                    if (parameterTypeName.Equals("Color") && !copy.Contains(","))
                    {
                        if (!copy.Contains("#")) copy = "#" + copy;
                        if (ColorUtility.TryParseHtmlString(copy, out var col))
                            RunFunctionOnAllComponents(col);
                        break;
                    }
                    copy = copy.Replace(" ", string.Empty);
                    copy = copy.Replace("(", string.Empty);
                    copy = copy.Replace(")", string.Empty);
                    copy = copy.Replace("<", string.Empty);
                    copy = copy.Replace(">", string.Empty);
                    var vals = copy.Split(char.Parse(","));
                    switch (parameterTypeName)
                    {
                        case "Vector2":
                            if (vals.Length != 2) break;
                            if (float.TryParse(vals[0], out var v2X) && float.TryParse(vals[1], out var v2Y))
                                RunFunctionOnAllComponents(new Vector2(v2X,v2Y));
                            break;
                        case "Vector3":
                            if (vals.Length != 3) break;
                            if (float.TryParse(vals[0], out var v3X) && float.TryParse(vals[1], out var v3Y) && float.TryParse(vals[2], out var v3Z))
                                RunFunctionOnAllComponents(new Vector3(v3X,v3Y,v3Z));
                            break;
                        case "Color":
                            if (vals.Length != 3 && vals.Length != 4) break;
                            if (int.TryParse(vals[0], out var cr) && int.TryParse(vals[1], out var cg) &&
                                int.TryParse(vals[2], out var cb))
                            {
                                if (vals.Length == 4)
                                {
                                    if (int.TryParse(vals[3], out var ca))
                                        RunFunctionOnAllComponents(new Color(cr/255f, cg/255f, cb/255f, ca/255f));
                                }
                                else
                                    RunFunctionOnAllComponents(new Color(cr/255f, cg/255f, cb/255f, 1));
                            }
                            break;
                    }
                    break;
                }
                case "GameObject":
                case "Transform":
                case "Collider":
                case "Collider2D":
                case "Rigidbody":
                case "Rigidbody2D":
                {
                    var objs = (GameObject[])FindObjectsOfType(typeof(GameObject));
                    foreach (var obj in objs)
                    {
                        if (obj.name.Equals(functionParamInputs[i]))
                        {
                            switch (parameterTypeName)
                            {
                                case "GameObject":
                                    RunFunctionOnAllComponents(obj);
                                    break;
                                case "Transform":
                                    RunFunctionOnAllComponents(obj.transform);
                                    break;
                                case "Collider":
                                    RunFunctionOnAllComponents(obj.GetComponent<Collider>());
                                    break;
                                case "Collider2D":
                                    RunFunctionOnAllComponents(obj.GetComponent<Collider2D>());
                                    break;
                                case "Rigidbody":
                                    RunFunctionOnAllComponents(obj.GetComponent<Rigidbody>());
                                    break;
                                case "Rigidbody2D":
                                    RunFunctionOnAllComponents(obj.GetComponent<Rigidbody2D>());
                                    break;
                            }

                            break;
                        }
                    }

                    break;
                }
            }
        }
    }
    
    private void RunFunctionOnAllComponents<T>(T param)
    {
        foreach (var comp in components.Where(comp => param != null))
        {
            if (CheckIfSelectedObjectsContainsComponentObject())
            {
                if (Selection.gameObjects.Contains(comp.gameObject))
                    comp.SendMessage(functionName, param, SendMessageOptions.DontRequireReceiver);
            }
            else
                comp.SendMessage(functionName, param, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void RunFunctionOnAllComponents()
    {
        foreach (var comp in components)
        {
            if (CheckIfSelectedObjectsContainsComponentObject())
            {
                if (Selection.gameObjects.Contains(comp.gameObject))
                    comp.SendMessage(functionName, SendMessageOptions.DontRequireReceiver);
            }
            else
                comp.SendMessage(functionName, SendMessageOptions.DontRequireReceiver);
        }
    }
    
    #endregion

    #endregion
}
#endif
