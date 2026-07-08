using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Pure-ASCII strings only - no emoji or unicode dashes.
namespace SOTreeTool
{
    // =========================================================================
    //  SO Audit Panel  -  Scans project SOs for common data problems
    // =========================================================================
    public class SOAuditPanel : EditorWindow
    {
        // Audit results
        private List<AuditIssue> _issues = new();
        private List<AuditIssue> _filtered = new();

        // Filter state
        private string    _searchText    = "";
        private int       _severityFilter = 0; // 0=All, 1=Error, 2=Warning, 3=Info
        private string    _typeFilter     = "";
        private bool      _isScanning     = false;

        // UI
        private IMGUIContainer _resultContainer;
        private Vector2        _scrollPos;
        private Label          _statusLabel;

        private static readonly string[] SeverityLabels = { "All", "Error", "Warning", "Info" };
        private static readonly string[] SeverityNames  = { "",    "ERROR", "WARNING", "INFO" };
        private static readonly Color[]  SeverityColors = {
            Color.white,
            new Color(1.0f, 0.35f, 0.35f),
            new Color(1.0f, 0.85f, 0.35f),
            new Color(0.55f, 0.85f, 1.0f),
        };

        // ── Open ──────────────────────────────────────────────────────────────
        [MenuItem("Tools/SO Audit Panel")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SOAuditPanel>();
            wnd.titleContent = new GUIContent("SO Audit",
                EditorGUIUtility.IconContent("d_console.warnicon.sml").image);
            wnd.minSize = new Vector2(700, 400);
        }

        // ── Build UI ──────────────────────────────────────────────────────────
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            // Toolbar
            root.Add(BuildToolbar());

            // Status bar
            _statusLabel = new Label("Press 'Scan Project' to begin.");
            _statusLabel.style.fontSize   = 10;
            _statusLabel.style.color      = new Color(0.65f, 0.70f, 0.80f);
            _statusLabel.style.paddingLeft= 8;
            _statusLabel.style.paddingTop = _statusLabel.style.paddingBottom = 4;
            _statusLabel.style.backgroundColor = new Color(0.13f, 0.13f, 0.15f);
            root.Add(_statusLabel);

            // Filter bar
            root.Add(BuildFilterBar());

            // Results (IMGUI)
            _resultContainer = new IMGUIContainer(DrawResults);
            _resultContainer.style.flexGrow = 1;
            root.Add(_resultContainer);
        }

        private Toolbar BuildToolbar()
        {
            var tb = new Toolbar();
            tb.Add(new ToolbarButton(ScanProject) { text = "Scan Project" });
            tb.Add(new ToolbarButton(ScanSelectedType) { text = "Scan Type..." });
            tb.Add(new ToolbarSpacer());
            tb.Add(new ToolbarButton(ExportReport) { text = "Export Report" });
            tb.Add(new ToolbarButton(ClearResults) { text = "Clear" });
            return tb;
        }

        private VisualElement BuildFilterBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection   = FlexDirection.Row;
            bar.style.alignItems      = Align.Center;
            bar.style.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
            bar.style.paddingLeft = bar.style.paddingRight  = 8;
            bar.style.paddingTop  = bar.style.paddingBottom = 4;
            bar.style.borderBottomColor = new Color(0.09f, 0.09f, 0.11f);
            bar.style.borderBottomWidth = 1;

            var searchLbl = new Label("Search:");
            searchLbl.style.fontSize    = 10;
            searchLbl.style.marginRight = 4;
            searchLbl.style.color       = new Color(0.6f, 0.65f, 0.75f);
            bar.Add(searchLbl);

            var search = new TextField();
            search.style.width = 180;
            search.RegisterValueChangedCallback(e => { _searchText = e.newValue; ApplyFilter(); });
            bar.Add(search);

            var sevLbl = new Label("  Severity:");
            sevLbl.style.fontSize    = 10;
            sevLbl.style.marginRight = 4;
            sevLbl.style.color       = new Color(0.6f, 0.65f, 0.75f);
            bar.Add(sevLbl);

            var sevDropdown = new DropdownField(SeverityLabels.ToList(), 0);
            sevDropdown.style.width = 100;
            sevDropdown.RegisterValueChangedCallback(e =>
            {
                _severityFilter = System.Array.IndexOf(SeverityLabels, e.newValue);
                ApplyFilter();
            });
            bar.Add(sevDropdown);

            var typeLbl = new Label("  Type:");
            typeLbl.style.fontSize    = 10;
            typeLbl.style.marginRight = 4;
            typeLbl.style.color       = new Color(0.6f, 0.65f, 0.75f);
            bar.Add(typeLbl);

            var typeField = new TextField();
            typeField.style.width = 140;
            typeField.RegisterValueChangedCallback(e => { _typeFilter = e.newValue; ApplyFilter(); });
            bar.Add(typeField);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            bar.Add(spacer);

            var countLbl = new Label();
            countLbl.name = "countLabel";
            countLbl.style.fontSize = 10;
            countLbl.style.color    = new Color(0.65f, 0.70f, 0.80f);
            bar.Add(countLbl);

            return bar;
        }

        // =========================================================
        //  Scanning
        // =========================================================
        private void ScanProject()
        {
            if (_isScanning) return; // Prevent concurrent scans
            _issues.Clear();
            _statusLabel.text = "Scanning all project ScriptableObjects...";
            _isScanning = true;
            _resultContainer?.MarkDirtyRepaint();

            var types = SOTypeScanner.GetAllScriptableObjectTypeObjects();
            int total = 0;

            foreach (var type in types)
            {
                var guids = AssetDatabase.FindAssets("t:" + type.Name);
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset   = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset == null) continue;
                    AuditAsset(asset, type, path);
                    total++;
                }
            }

            _isScanning = false;
            ApplyFilter();
            _statusLabel.text = (_isScanning ? "Scanning..." : "Scan complete.") + " " + total + " assets checked, " + _issues.Count + " issues found.";
            _resultContainer.MarkDirtyRepaint();
        }

        private void ScanSelectedType()
        {
            // Show a popup to pick a type
            var menu = new GenericMenu();
            foreach (var typeName in SOTypeScanner.GetProjectScriptableObjectTypes())
            {
                var captured = typeName;
                menu.AddItem(new GUIContent(typeName), false, () => ScanSingleType(captured));
            }
            menu.ShowAsContext();
        }

        private void ScanSingleType(string typeName)
        {
            _issues.RemoveAll(i => i.TypeName == typeName);

            var type = SOTypeScanner.GetAllScriptableObjectTypeObjects()
                .FirstOrDefault(t => t.Name == typeName);
            if (type == null) return;

            var guids = AssetDatabase.FindAssets("t:" + typeName);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset   = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;
                AuditAsset(asset, type, path);
            }

            ApplyFilter();
            _statusLabel.text = "Scanned " + typeName + ". " + _issues.Count(i => i.TypeName == typeName) + " issues found.";
            _resultContainer.MarkDirtyRepaint();
        }

        private void AuditAsset(ScriptableObject asset, System.Type type, string assetPath)
        {
            var so = new SerializedObject(asset);
            var prop = so.GetIterator();

            // Check default/empty name
            string assetName = asset.name;
            if (string.IsNullOrEmpty(assetName) || assetName.StartsWith("New "))
                AddIssue(AuditSeverity.Warning, type.Name, assetName, assetPath,
                    "Asset has default or empty name: " + (assetName ?? "(null)"));

            // Walk all serialized properties
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                string propPath = prop.propertyPath;

                // Check for broken missing script (m_Script is null)
                if (propPath == "m_Script")
                {
                    if (prop.objectReferenceValue == null)
                        AddIssue(AuditSeverity.Error, type.Name, assetName, assetPath,
                            "Missing script reference - asset may be broken.");
                    continue;
                }

                switch (prop.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        // Check for null object references on fields that look required
                        // (field name doesn't contain "optional", "fallback", "override")
                        if (prop.objectReferenceValue == null)
                        {
                            string lower = propPath.ToLower();
                            bool looksRequired = !lower.Contains("optional") &&
                                                 !lower.Contains("fallback") &&
                                                 !lower.Contains("override") &&
                                                 !lower.Contains("nullable") &&
                                                 !lower.Contains("icon") &&
                                                 !lower.Contains("sprite");
                            if (looksRequired)
                                AddIssue(AuditSeverity.Warning, type.Name, assetName, assetPath,
                                    "Null object reference: " + propPath);
                        }
                        break;

                    case SerializedPropertyType.String:
                        if (string.IsNullOrEmpty(prop.stringValue))
                        {
                            string lower = propPath.ToLower();
                            if (lower.Contains("name") || lower.Contains("id") || lower.Contains("key"))
                                AddIssue(AuditSeverity.Info, type.Name, assetName, assetPath,
                                    "Empty string in important field: " + propPath);
                        }
                        break;

                    case SerializedPropertyType.ArraySize:
                        if (prop.intValue == 0)
                        {
                            string lower = propPath.ToLower();
                            if (lower.Contains("reward") || lower.Contains("drop") ||
                                lower.Contains("option") || lower.Contains("step") ||
                                lower.Contains("item") || lower.Contains("phase"))
                                AddIssue(AuditSeverity.Info, type.Name, assetName, assetPath,
                                    "Empty list that may need entries: " + propPath.Replace(".Array.size", ""));
                        }
                        break;
                }

            }

            // Type-specific checks
            RunTypeSpecificChecks(so, type, assetName, assetPath);
        }

        private void RunTypeSpecificChecks(SerializedObject so, System.Type type, string name, string path)
        {
            string typeName = type.Name;

            // QuestBase checks
            if (typeName == "QuestBase")
            {
                var steps = so.FindProperty("steps");
                if (steps != null && steps.isArray && steps.arraySize == 0)
                    AddIssue(AuditSeverity.Warning, typeName, name, path, "Quest has no steps defined.");
            }

            // DialogGraphDefinition checks
            if (typeName == "DialogGraphDefinition")
            {
                var nodes = so.FindProperty("nodes");
                if (nodes != null && nodes.isArray && nodes.arraySize == 0)
                    AddIssue(AuditSeverity.Warning, typeName, name, path, "Dialog graph has no nodes.");
            }

            // PokemonBase checks
            if (typeName == "PokemonBase")
            {
                var maxHp = so.FindProperty("maxHp");
                if (maxHp != null && maxHp.intValue == 0)
                    AddIssue(AuditSeverity.Error, typeName, name, path, "PokemonBase has 0 maxHp - likely unset.");

                var learnableMoves = so.FindProperty("learnableMoves");
                if (learnableMoves != null && learnableMoves.isArray && learnableMoves.arraySize == 0)
                    AddIssue(AuditSeverity.Warning, typeName, name, path, "PokemonBase has no learnable moves.");
            }

            // ItemBase checks - use type name matching to avoid cross-assembly Type.GetType issues
            if (typeName == "ItemBase" || type.Name.EndsWith("Item") ||
                (type.BaseType != null && (type.BaseType.Name == "ItemBase" || type.BaseType.Name.EndsWith("Item"))))
            {
                var price = so.FindProperty("price");
                if (price != null && price.intValue == 0)
                    AddIssue(AuditSeverity.Info, typeName, name, path, "Item price is 0 - intentional?");
            }
        }

        private void AddIssue(AuditSeverity severity, string typeName, string assetName, string assetPath, string message)
        {
            _issues.Add(new AuditIssue
            {
                Severity  = severity,
                TypeName  = typeName,
                AssetName = assetName,
                AssetPath = assetPath,
                Message   = message
            });
        }

        // =========================================================
        //  Filter
        // =========================================================
        private void ApplyFilter()
        {
            _filtered = _issues.Where(i =>
            {
                if (_severityFilter > 0 && (int)i.Severity != _severityFilter) return false;
                if (!string.IsNullOrEmpty(_typeFilter) &&
                    !i.TypeName.ToLower().Contains(_typeFilter.ToLower())) return false;
                if (!string.IsNullOrEmpty(_searchText))
                {
                    string q = _searchText.ToLower();
                    if (!i.AssetName.ToLower().Contains(q) &&
                        !i.Message.ToLower().Contains(q) &&
                        !i.TypeName.ToLower().Contains(q)) return false;
                }
                return true;
            }).ToList();

            // Update count label
            var root = rootVisualElement;
            var countLbl = root.Q<Label>("countLabel");
            if (countLbl != null)
                countLbl.text = _filtered.Count + " / " + _issues.Count + " issues";

            _resultContainer?.MarkDirtyRepaint();
        }

        private void ClearResults()
        {
            _issues.Clear();
            _filtered.Clear();
            _statusLabel.text = "Results cleared.";
            _resultContainer?.MarkDirtyRepaint();
        }

        // =========================================================
        //  Draw results (IMGUI)
        // =========================================================
        private void DrawResults()
        {
            if (_filtered.Count == 0)
            {
                GUILayout.Space(20);
                EditorGUILayout.LabelField(
                    _issues.Count == 0 ? "No scan results yet. Click 'Scan Project' to start."
                                       : "No issues match the current filter.",
                    EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Column header
            float totalW = position.width > 0 ? position.width - 270f : 400f;
            var headerStyle = new GUIStyle(EditorStyles.toolbar);
            EditorGUILayout.BeginHorizontal(headerStyle);
            GUILayout.Label("Severity", GUILayout.Width(70));
            GUILayout.Label("Type", GUILayout.Width(160));
            GUILayout.Label("Asset", GUILayout.Width(160));
            GUILayout.Label("Issue", GUILayout.Width(totalW));
            GUILayout.Label("Actions", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            string lastType = null;
            for (int i = 0; i < _filtered.Count; i++)
            {
                var issue = _filtered[i];

                // Type group header
                if (issue.TypeName != lastType)
                {
                    lastType = issue.TypeName;
                    var groupStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 10,
                        normal   = { textColor = new Color(0.70f, 0.80f, 1f) }
                    };
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("-- " + issue.TypeName + " --", groupStyle);
                }

                // Row background
                var rowColor = i % 2 == 0
                    ? new Color(0.18f, 0.18f, 0.20f)
                    : new Color(0.16f, 0.16f, 0.18f);
                var rowRect = EditorGUILayout.BeginHorizontal();
                EditorGUI.DrawRect(rowRect, rowColor);

                // Severity badge
                int sevIdx = (int)issue.Severity;
                var sevStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = SeverityColors[sevIdx] },
                    fontStyle = FontStyle.Bold,
                    fontSize  = 9
                };
                GUILayout.Label(sevIdx < SeverityNames.Length ? SeverityNames[sevIdx] : "?", sevStyle, GUILayout.Width(70));

                // Type
                GUILayout.Label(issue.TypeName, EditorStyles.miniLabel, GUILayout.Width(160));

                // Asset name
                GUILayout.Label(issue.AssetName, EditorStyles.miniLabel, GUILayout.Width(160));

                // Message
                GUILayout.Label(issue.Message, EditorStyles.miniLabel, GUILayout.Width(totalW));

                // Actions
                if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(issue.AssetPath);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                }
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(issue.AssetPath);
                    if (asset != null) Selection.activeObject = asset;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        //  Export report
        // =========================================================
        private void ExportReport()
        {
            string path = EditorUtility.SaveFilePanel("Export Audit Report", "", "SOAuditReport", "txt");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SO Audit Report - " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("Total issues: " + _issues.Count);
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            foreach (var group in _filtered.GroupBy(i => i.TypeName).OrderBy(g => g.Key))
            {
                sb.AppendLine("[" + group.Key + "] - " + group.Count() + " issue(s)");
                foreach (var issue in group)
                {
                    string sev = issue.Severity == AuditSeverity.Error   ? "ERROR"   :
                                 issue.Severity == AuditSeverity.Warning ? "WARNING" : "INFO";
                    sb.AppendLine("  [" + sev + "] " + issue.AssetName + ": " + issue.Message);
                    sb.AppendLine("    Path: " + issue.AssetPath);
                }
                sb.AppendLine();
            }

            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log("[SO Audit] Report exported to: " + path);
        }
    }

    // =========================================================================
    //  Data
    // =========================================================================
    public enum AuditSeverity { All = 0, Error = 1, Warning = 2, Info = 3 }

    public class AuditIssue
    {
        public AuditSeverity Severity;
        public string        TypeName;
        public string        AssetName;
        public string        AssetPath;
        public string        Message;
    }
}
