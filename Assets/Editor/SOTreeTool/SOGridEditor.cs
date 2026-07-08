using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Pure-ASCII strings only to prevent ATGTextJobSystem UIElements glyph crash.
namespace SOTreeTool
{
    public class SOGridEditor : EditorWindow
    {
        private Vector2 _sidebarScrollPos;
        private Vector2 _mainScrollPos;

        private List<string> _allProjectTypes = new();
        private List<string> _filteredTypes = new();
        private string _typeSearch = "";

        private string _selectedTypeName = "";
        private List<ScriptableObject> _loadedAssets = new();
        private List<SerializedObject> _serializedAssets = new();
        private List<FieldInfo> _fields = new();
        private List<bool> _columnVisibility = new();

        private string _assetSearchText = "";
        private const float RowHeight = 22f;
        private const float ColWidth = 130f;
        private const float SidebarWidth = 220f;

        [MenuItem("Tools/SO Grid Editor")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SOGridEditor>();
            wnd.titleContent = new GUIContent("SO Grid Editor", 
                EditorGUIUtility.IconContent("d_Project").image);
            wnd.minSize = new Vector2(900, 500);
        }

        private void OnEnable()
        {
            RefreshTypes();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            // Left Sidebar
            DrawSidebar(SidebarWidth);

            // Resizer divider
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

            // Right content
            DrawMainArea();

            EditorGUILayout.EndHorizontal();
        }

        // =========================================================
        //  UI Drawing Parts
        // =========================================================
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh Types", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RefreshTypes();
            }

            if (!string.IsNullOrEmpty(_selectedTypeName))
            {
                if (GUILayout.Button("Reload Assets", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    SelectType(_selectedTypeName);
                }

                GUILayout.Space(10);
                GUILayout.Label("Columns:", EditorStyles.miniLabel);

                if (GUILayout.Button("Toggle All", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    bool target = _columnVisibility.Any(v => !v);
                    for (int i = 0; i < _columnVisibility.Count; i++) _columnVisibility[i] = target;
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar(float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandHeight(true));
            
            // Search box
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Search:", EditorStyles.miniLabel, GUILayout.Width(45));
            string newSearch = EditorGUILayout.TextField(_typeSearch);
            if (newSearch != _typeSearch)
            {
                _typeSearch = newSearch;
                ApplyTypeFilter();
            }
            EditorGUILayout.EndHorizontal();

            _sidebarScrollPos = EditorGUILayout.BeginScrollView(_sidebarScrollPos, GUILayout.ExpandHeight(true));

            var grouped = _filteredTypes
                .GroupBy(GetCategory)
                .OrderBy(g => g.Key == "Other" ? "zzz" : g.Key);

            foreach (var group in grouped)
            {
                string catLabel = GetCategoryLabel(group.Key);
                EditorGUILayout.LabelField(catLabel + " " + group.Key + " (" + group.Count() + ")", EditorStyles.boldLabel);

                foreach (var typeName in group.OrderBy(t => t))
                {
                    bool isSelected = (_selectedTypeName == typeName);
                    var style = new GUIStyle(EditorStyles.miniButtonLeft);
                    if (isSelected)
                    {
                        style.normal.textColor = Color.yellow;
                        style.fontStyle = FontStyle.Bold;
                    }

                    if (GUILayout.Button("  " + typeName, style, GUILayout.Height(20)))
                    {
                        SelectType(typeName);
                    }
                }
                GUILayout.Space(6);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawMainArea()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (string.IsNullOrEmpty(_selectedTypeName))
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a ScriptableObject type from the sidebar to edit.", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            // Asset search and header row info
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Filter Assets:", EditorStyles.miniLabel, GUILayout.Width(80));
            _assetSearchText = EditorGUILayout.TextField(_assetSearchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            GUILayout.Space(10);
            GUILayout.Label("Found " + _loadedAssets.Count + " assets", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Toggle Columns Bar
            DrawColumnToggles();

            // Grid scroll view
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos, true, true);

            var visibleFields = GetVisibleFields();
            float tableWidth = ColWidth + (visibleFields.Count * ColWidth) + 100f;

            EditorGUILayout.BeginVertical(GUILayout.Width(tableWidth));

            // Draw Header Row
            DrawHeaderRow(visibleFields);

            // Draw Data Rows
            var filteredAssets = GetFilteredAssets();
            for (int r = 0; r < filteredAssets.Count; r++)
            {
                var pair = filteredAssets[r];
                DrawDataRow(r, pair.asset, pair.so, visibleFields);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawColumnToggles()
        {
            if (_fields.Count == 0) return;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Visible Columns: ", EditorStyles.miniBoldLabel, GUILayout.Width(100));
            
            float currentWidth = 100;
            for (int i = 0; i < _fields.Count; i++)
            {
                string fieldName = _fields[i].Name;
                float itemWidth = GUI.skin.toggle.CalcSize(new GUIContent(fieldName)).x + 10;
                
                if (currentWidth + itemWidth > position.width - SidebarWidth - 50)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label("", GUILayout.Width(100));
                    currentWidth = 100;
                }

                _columnVisibility[i] = EditorGUILayout.ToggleLeft(fieldName, _columnVisibility[i], GUILayout.Width(itemWidth));
                currentWidth += itemWidth;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeaderRow(List<FieldInfo> visibleFields)
        {
            var style = new GUIStyle(EditorStyles.toolbarButton);
            style.alignment = TextAnchor.MiddleLeft;
            style.fontStyle = FontStyle.Bold;

            EditorGUILayout.BeginHorizontal();

            // Asset Name Header
            GUILayout.Box("Asset Name", style, GUILayout.Width(ColWidth), GUILayout.Height(RowHeight));

            foreach (var field in visibleFields)
            {
                GUILayout.Box(field.Name, style, GUILayout.Width(ColWidth), GUILayout.Height(RowHeight));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDataRow(int rowIndex, ScriptableObject asset, SerializedObject so, List<FieldInfo> visibleFields)
        {
            if (asset == null || so == null) return;
            so.Update();

            var rowStyle = rowIndex % 2 == 0 ? "CN EntryBackEven" : "CN EntryBackOdd";
            var style = new GUIStyle(rowStyle);

            EditorGUILayout.BeginHorizontal(style, GUILayout.Height(RowHeight));

            // Asset name & select/ping button
            EditorGUILayout.BeginHorizontal(GUILayout.Width(ColWidth));
            if (GUILayout.Button(asset.name, EditorStyles.label, GUILayout.Width(ColWidth - 45)))
            {
                Selection.activeObject = asset;
            }
            if (GUILayout.Button("P", GUILayout.Width(18)))
            {
                EditorGUIUtility.PingObject(asset);
            }
            if (GUILayout.Button("R", GUILayout.Width(18)))
            {
                string path = AssetDatabase.GetAssetPath(asset);
                string newName = InternalRenameAssetDialog(asset.name);
                if (!string.IsNullOrEmpty(newName) && newName != asset.name)
                {
                    AssetDatabase.RenameAsset(path, newName);
                    AssetDatabase.SaveAssets();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Field values
            foreach (var field in visibleFields)
            {
                var prop = so.FindProperty(field.Name);
                if (prop != null)
                {
                    DrawPropertyCell(prop);
                }
                else
                {
                    // Fallback to reflection read only display if SerializedProperty not found
                    try
                    {
                        object val = field.GetValue(asset);
                        string valStr = val != null ? val.ToString() : "null";
                        GUILayout.Label(valStr, EditorStyles.miniLabel, GUILayout.Width(ColWidth));
                    }
                    catch
                    {
                        GUILayout.Label("-", EditorStyles.miniLabel, GUILayout.Width(ColWidth));
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPropertyCell(SerializedProperty prop)
        {
            // Renders standard SerializedProperty editor elements inside a constrained column
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUILayout.IntField(prop.intValue, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = EditorGUILayout.Toggle(prop.boolValue, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = EditorGUILayout.FloatField(prop.floatValue, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = EditorGUILayout.TextField(prop.stringValue, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = EditorGUILayout.ColorField(prop.colorValue, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.ObjectReference:
                    var objFieldType = prop.GetSystemType() ?? typeof(UnityEngine.Object);
                    prop.objectReferenceValue = EditorGUILayout.ObjectField(prop.objectReferenceValue, objFieldType, false, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = EditorGUILayout.Popup(prop.enumValueIndex, prop.enumDisplayNames, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = EditorGUILayout.Vector2Field("", prop.vector2Value, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = EditorGUILayout.Vector3Field("", prop.vector3Value, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.Rect:
                    prop.rectValue = EditorGUILayout.RectField("", prop.rectValue, GUILayout.Width(ColWidth));
                    break;
                case SerializedPropertyType.AnimationCurve:
                    prop.animationCurveValue = EditorGUILayout.CurveField(prop.animationCurveValue, GUILayout.Width(ColWidth));
                    break;
                default:
                    // Read only display for unsupported structures (like lists / composite structs)
                    if (prop.isArray)
                    {
                        GUILayout.Label("Array (" + prop.arraySize + ")", EditorStyles.miniLabel, GUILayout.Width(ColWidth));
                    }
                    else
                    {
                        GUILayout.Label(prop.displayName, EditorStyles.miniLabel, GUILayout.Width(ColWidth));
                    }
                    break;
            }
        }

        // =========================================================
        //  Helper Methods
        // =========================================================
        private string InternalRenameAssetDialog(string oldName)
        {
            // SaveFilePanel returns a full OS path - extract just the filename without extension
            // so it can be passed correctly to AssetDatabase.RenameAsset(path, newName)
            string result = EditorUtility.SaveFilePanel("Rename Asset - Type new name and Save", "", oldName, "asset");
            if (string.IsNullOrEmpty(result)) return oldName;
            string newName = System.IO.Path.GetFileNameWithoutExtension(result);
            return string.IsNullOrEmpty(newName) ? oldName : newName;
        }

        private void RefreshTypes()
        {
            _allProjectTypes = SOTypeScanner.GetProjectScriptableObjectTypes(forceRefresh: true);
            ApplyTypeFilter();
        }

        private void ApplyTypeFilter()
        {
            string q = _typeSearch?.ToLower() ?? "";
            _filteredTypes = string.IsNullOrEmpty(q)
                ? new List<string>(_allProjectTypes)
                : _allProjectTypes.Where(t => t.ToLower().Contains(q)).ToList();
        }

        private void SelectType(string typeName)
        {
            _selectedTypeName = typeName;
            _loadedAssets.Clear();
            _serializedAssets.Clear();
            _fields.Clear();
            _columnVisibility.Clear();

            // 1. Find all assets in project
            var guids = AssetDatabase.FindAssets("t:" + typeName);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null)
                {
                    _loadedAssets.Add(asset);
                    _serializedAssets.Add(new SerializedObject(asset));
                }
            }

            // 2. Discover Serialized Fields via Reflection
            var type = SOTypeScanner.GetAllScriptableObjectTypeObjects()
                .FirstOrDefault(t => t.Name == typeName);
            
            if (type != null)
            {
                // Gather fields (public or private with SerializeField attribute)
                var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var f in allFields)
                {
                    if (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                    {
                        if (f.Name == "m_Script") continue;
                        _fields.Add(f);
                        _columnVisibility.Add(true);
                    }
                }
            }
        }

        private List<FieldInfo> GetVisibleFields()
        {
            var list = new List<FieldInfo>();
            for (int i = 0; i < _fields.Count; i++)
            {
                if (_columnVisibility[i]) list.Add(_fields[i]);
            }
            return list;
        }

        private List<(ScriptableObject asset, SerializedObject so)> GetFilteredAssets()
        {
            var list = new List<(ScriptableObject, SerializedObject)>();
            string q = _assetSearchText.ToLower();

            for (int i = 0; i < _loadedAssets.Count; i++)
            {
                var asset = _loadedAssets[i];
                if (string.IsNullOrEmpty(q) || asset.name.ToLower().Contains(q))
                {
                    list.Add((asset, _serializedAssets[i]));
                }
            }
            return list;
        }

        private static string GetCategory(string name)
        {
            if (name.EndsWith("Definition"))  return "Definition";
            if (name.EndsWith("Requirement")) return "Requirement";
            if (name.EndsWith("Base"))        return "Base";
            if (name.EndsWith("Profile"))     return "Profile";
            if (name.EndsWith("DB"))          return "Database";
            if (name.Contains("Item"))        return "Item";
            return "Other";
        }

        private static string GetCategoryLabel(string cat)
        {
            switch (cat)
            {
                case "Definition":  return "[DEF]";
                case "Requirement": return "[REQ]";
                case "Base":        return "[BASE]";
                case "Profile":     return "[PROF]";
                case "Database":    return "[DB]";
                case "Item":        return "[ITEM]";
                default:            return "[OTHER]";
            }
        }
    }

    // Helper extension to resolve system type from SerializedProperty
    public static class SerializedPropertyExtensions
    {
        public static Type GetSystemType(this SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetFieldValue(obj, elementName, index);
                }
                else
                {
                    obj = GetFieldValue(obj, element);
                }
            }
            return obj != null ? obj.GetType() : typeof(UnityEngine.Object);
        }

        private static object GetFieldValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f.GetValue(source);
                type = type.BaseType;
            }
            return null;
        }

        private static object GetFieldValue(object source, string name, int index)
        {
            var enumerable = GetFieldValue(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext()) return null;
            }
            return enumerator.Current;
        }
    }
}
