using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

// Pure-ASCII strings throughout to avoid ATGTextJobSystem / UIElements glyph crash.
namespace SOTreeTool
{
    // =========================================================================
    //  SO Tree Editor  -  Main Window
    // =========================================================================
    public class SOTreeEditor : EditorWindow
    {
        // ── Graph state ───────────────────────────────────────────────────────
        private Dictionary<string, NodeView> _nodeViews   = new();
        private List<ConnectionData>         _connections = new();
        private bool                         _checkCircular = true;

        // ── Link drag state ───────────────────────────────────────────────────
        private string         _pendingFromId = null;
        private IMGUIContainer _lineLayer;
        private Vector2        _mousePos;

        // ── Selection ─────────────────────────────────────────────────────────
        private string         _selectedNodeId   = null;
        private ConnectionData _selectedConn      = null;   // null = none

        // ── Sidebar state ─────────────────────────────────────────────────────
        private List<string>  _allProjectTypes = new();
        private List<string>  _filteredTypes   = new();
        private string        _typeSearch      = "";
        private VisualElement _graphContainer;
        private ScrollView    _typeListScroll;

        // ── Detail panel (IMGUI) ──────────────────────────────────────────────
        private IMGUIContainer       _detailContainer;
        private Vector2              _detailScroll;
        private UnityEditor.Editor   _cachedEditor;
        private UnityEngine.Object   _cachedEditorTarget;

        // ── Open ──────────────────────────────────────────────────────────────
        [MenuItem("Tools/SO Tree Editor")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SOTreeEditor>();
            wnd.titleContent = new GUIContent("SO Tree Editor",
                EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            wnd.minSize = new Vector2(1100, 560);
        }

        private void OnDestroy()
        {
            if (_cachedEditor != null) DestroyImmediate(_cachedEditor);
        }

        // =========================================================
        //  Build UI
        // =========================================================
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.Add(BuildToolbar());

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow      = 1;
            body.style.overflow      = Overflow.Hidden;
            root.Add(body);

            body.Add(BuildSidebar());
            body.Add(BuildCanvas());
            body.Add(BuildDetailPanel());

            RefreshTypes();
        }

        // ── Toolbar ───────────────────────────────────────────────────────────
        private Toolbar BuildToolbar()
        {
            var tb = new Toolbar();
            tb.Add(new ToolbarButton(SaveTree)  { text = "Save Tree" });
            tb.Add(new ToolbarButton(LoadTree)  { text = "Load Tree" });
            tb.Add(new ToolbarSpacer());
            tb.Add(new ToolbarButton(ClearAll)  { text = "Clear Canvas" });
            tb.Add(new ToolbarSpacer());
            var circ = new Toggle("Circular check") { value = _checkCircular };
            circ.RegisterValueChangedCallback(e => _checkCircular = e.newValue);
            tb.Add(circ);
            return tb;
        }

        // ── Sidebar ───────────────────────────────────────────────────────────
        private VisualElement BuildSidebar()
        {
            var panel = new VisualElement();
            panel.style.width            = 210;
            panel.style.backgroundColor  = new Color(0.17f, 0.17f, 0.19f);
            panel.style.borderRightColor = new Color(0.09f, 0.09f, 0.11f);
            panel.style.borderRightWidth = 1;
            panel.style.flexDirection    = FlexDirection.Column;

            var header = new Label("ScriptableObjects");
            header.style.fontSize                = 11;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color                   = new Color(0.75f, 0.85f, 1f);
            header.style.paddingLeft = header.style.paddingTop = header.style.paddingBottom = 8;
            header.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            panel.Add(header);

            var searchRow = new VisualElement();
            searchRow.style.flexDirection  = FlexDirection.Row;
            searchRow.style.alignItems     = Align.Center;
            searchRow.style.marginLeft = searchRow.style.marginRight = 6;
            searchRow.style.marginTop  = 6; searchRow.style.marginBottom = 2;

            var searchLbl = new Label("Search:");
            searchLbl.style.fontSize = 10; searchLbl.style.marginRight = 4;
            searchLbl.style.color    = new Color(0.6f, 0.65f, 0.75f);
            searchRow.Add(searchLbl);

            var sf = new TextField();
            sf.style.flexGrow = 1;
            sf.RegisterValueChangedCallback(e => { _typeSearch = e.newValue; ApplyFilter(); });
            searchRow.Add(sf);
            panel.Add(searchRow);

            var refreshBtn = new Button(RefreshTypes) { text = "Refresh Types" };
            refreshBtn.style.marginLeft = refreshBtn.style.marginRight = 6;
            refreshBtn.style.marginTop  = refreshBtn.style.marginBottom = 4;
            refreshBtn.style.fontSize   = 10;
            panel.Add(refreshBtn);

            _typeListScroll = new ScrollView(ScrollViewMode.Vertical);
            _typeListScroll.style.flexGrow = 1;
            panel.Add(_typeListScroll);
            return panel;
        }

        // ── Canvas ────────────────────────────────────────────────────────────
        private VisualElement BuildCanvas()
        {
            var wrapper = new VisualElement();
            wrapper.style.flexGrow        = 1;
            wrapper.style.overflow        = Overflow.Hidden;
            wrapper.style.backgroundColor = new Color(0.13f, 0.13f, 0.15f);

            _lineLayer = new IMGUIContainer(DrawLines);
            _lineLayer.style.position = Position.Absolute;
            _lineLayer.style.left = _lineLayer.style.top = 0;
            _lineLayer.style.right = _lineLayer.style.bottom = 0;
            _lineLayer.pickingMode = PickingMode.Ignore;
            wrapper.Add(_lineLayer);

            var nodeContainer = new VisualElement();
            nodeContainer.name = "nodeContainer";
            nodeContainer.style.position = Position.Absolute;
            nodeContainer.style.left = nodeContainer.style.top = 0;
            nodeContainer.style.right = nodeContainer.style.bottom = 0;
            nodeContainer.RegisterCallback<MouseMoveEvent>(e =>
            {
                _mousePos = e.localMousePosition;
                if (_pendingFromId != null) _lineLayer.MarkDirtyRepaint();
            });
            nodeContainer.RegisterCallback<MouseDownEvent>(e =>
            {
                // Right-click cancels linking; click on empty deselects
                if (e.button == 1 && _pendingFromId != null)
                { CancelLinking(); e.StopPropagation(); return; }
                if (e.button == 0)
                { SetSelectedNode(null); SetSelectedConn(null); }
            });
            wrapper.Add(nodeContainer);
            _graphContainer = nodeContainer;

            var hint = new Label("Click a type in the sidebar to add a node.\nClick green port then red port to connect.\nClick a connection midpoint button to edit conditions.");
            hint.style.position       = Position.Absolute;
            hint.style.bottom         = 10;
            hint.style.right          = 12;
            hint.style.color          = new Color(0.35f, 0.35f, 0.40f);
            hint.style.fontSize       = 9;
            hint.style.unityTextAlign = TextAnchor.MiddleRight;
            hint.pickingMode          = PickingMode.Ignore;
            wrapper.Add(hint);

            return wrapper;
        }

        // ── Detail panel ──────────────────────────────────────────────────────
        private VisualElement BuildDetailPanel()
        {
            var panel = new VisualElement();
            panel.style.width            = 270;
            panel.style.backgroundColor  = new Color(0.17f, 0.17f, 0.19f);
            panel.style.borderLeftColor  = new Color(0.09f, 0.09f, 0.11f);
            panel.style.borderLeftWidth  = 1;
            panel.style.flexDirection    = FlexDirection.Column;

            var header = new Label("Properties");
            header.style.fontSize                = 11;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color                   = new Color(0.75f, 0.85f, 1f);
            header.style.paddingLeft = header.style.paddingTop = header.style.paddingBottom = 8;
            header.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            panel.Add(header);

            _detailContainer = new IMGUIContainer(DrawDetailPanel);
            _detailContainer.style.flexGrow = 1;
            panel.Add(_detailContainer);
            return panel;
        }

        // =========================================================
        //  Selection helpers
        // =========================================================
        public void SetSelectedNode(string id)
        {
            _selectedNodeId = id;
            _selectedConn   = null;
            // Highlight
            foreach (var kv in _nodeViews)
                kv.Value.SetSelected(kv.Key == id);
            InvalidateDetailCache();
            _detailContainer?.MarkDirtyRepaint();
        }

        public void SetSelectedConn(ConnectionData conn)
        {
            _selectedConn   = conn;
            _selectedNodeId = null;
            foreach (var kv in _nodeViews) kv.Value.SetSelected(false);
            _detailContainer?.MarkDirtyRepaint();
        }

        private void InvalidateDetailCache()
        {
            if (_cachedEditor != null) { DestroyImmediate(_cachedEditor); _cachedEditor = null; }
            _cachedEditorTarget = null;
        }

        // =========================================================
        //  Detail panel IMGUI drawing
        // =========================================================
        private void DrawDetailPanel()
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            if (_selectedConn != null)
                DrawConnectionConditions(_selectedConn);
            else if (_selectedNodeId != null && _nodeViews.TryGetValue(_selectedNodeId, out var nv))
                DrawNodeDetail(nv);
            else
            {
                EditorGUILayout.HelpBox("Select a node or a connection to see details.", MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Node detail (asset linker + inspector) ────────────────────────────
        private void DrawNodeDetail(NodeView nv)
        {
            EditorGUILayout.LabelField("Node: " + nv.TypeName, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Asset link field
            EditorGUILayout.LabelField("Linked Asset", EditorStyles.miniBoldLabel);
            ScriptableObject current = nv.LinkedAsset;
            ScriptableObject next = (ScriptableObject)EditorGUILayout.ObjectField(
                current, typeof(ScriptableObject), allowSceneObjects: false);

            if (next != current)
            {
                nv.SetLinkedAsset(next);
                InvalidateDetailCache();
            }

            // Quick-create button
            if (current == null)
            {
                EditorGUILayout.HelpBox("No asset linked. Link an existing asset above or create a new one.", MessageType.None);
                if (GUILayout.Button("Create New Asset for this Node"))
                    CreateAssetForNode(nv);
            }
            else
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
                DrawCachedInspector(current);

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Ping Asset in Project"))
                    EditorGUIUtility.PingObject(current);
                if (GUILayout.Button("Unlink Asset"))
                { nv.SetLinkedAsset(null); InvalidateDetailCache(); }
            }
        }

        private void DrawCachedInspector(UnityEngine.Object target)
        {
            if (_cachedEditorTarget != target)
            {
                if (_cachedEditor != null) DestroyImmediate(_cachedEditor);
                _cachedEditor       = UnityEditor.Editor.CreateEditor(target);
                _cachedEditorTarget = target;
            }
            if (_cachedEditor != null)
                _cachedEditor.OnInspectorGUI();
        }

        private void CreateAssetForNode(NodeView nv)
        {
            // Find the real type
            var type = SOTypeScanner.GetAllScriptableObjectTypeObjects()
                .FirstOrDefault(t => t.Name == nv.TypeName);
            if (type == null) { Debug.LogError("[SO Tree] Type not found: " + nv.TypeName); return; }

            string path = EditorUtility.SaveFilePanelInProject(
                "Create " + nv.TypeName, nv.TypeName, "asset", "Choose save location");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            nv.SetLinkedAsset(asset);
            InvalidateDetailCache();
            EditorGUIUtility.PingObject(asset);
        }

        // ── Connection condition editor ───────────────────────────────────────
        private static readonly string[] OpLabels  = { "==", "!=", ">", "<", ">=", "<=", "Contains", "HasFlag" };
        private static readonly string[] LogicLabels = { "AND (all must pass)", "OR (any must pass)" };

        private void DrawConnectionConditions(ConnectionData conn)
        {
            EditorGUILayout.LabelField("Connection Conditions", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // From / To info
            string fromName = _nodeViews.TryGetValue(conn.FromId, out var fn) ? fn.TypeName : conn.FromId;
            string toName   = _nodeViews.TryGetValue(conn.ToId,   out var tn) ? tn.TypeName : conn.ToId;
            EditorGUILayout.LabelField("From: " + fromName, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("To:   " + toName,   EditorStyles.miniLabel);
            EditorGUILayout.Space(6);

            // Connection label
            EditorGUILayout.LabelField("Label (optional)");
            conn.Label = EditorGUILayout.TextField(conn.Label ?? "");
            EditorGUILayout.Space(4);

            // Logic operator
            EditorGUILayout.LabelField("Condition Logic");
            conn.LogicOp = (ConditionLogic)EditorGUILayout.Popup((int)conn.LogicOp, LogicLabels);
            EditorGUILayout.Space(6);

            // Condition list
            EditorGUILayout.LabelField("Conditions (" + conn.Conditions.Count + ")", EditorStyles.miniBoldLabel);
            for (int i = 0; i < conn.Conditions.Count; i++)
            {
                var c = conn.Conditions[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Header row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Condition " + (i + 1), EditorStyles.boldLabel, GUILayout.Width(90));
                c.Negate = EditorGUILayout.ToggleLeft("NOT", c.Negate, GUILayout.Width(46));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                { conn.Conditions.RemoveAt(i); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
                EditorGUILayout.EndHorizontal();

                // Key
                EditorGUILayout.LabelField("Key (e.g. quest.main.done)");
                c.Key = EditorGUILayout.TextField(c.Key ?? "");

                // Operator + Value on same row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Op", GUILayout.Width(24));
                c.Op = EditorGUILayout.Popup(c.Op, OpLabels, GUILayout.Width(80));
                EditorGUILayout.LabelField("Value", GUILayout.Width(36));
                c.Value = EditorGUILayout.TextField(c.Value ?? "");
                EditorGUILayout.EndHorizontal();

                // Note
                c.Note = EditorGUILayout.TextField("Note", c.Note ?? "");

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ Add Condition"))
                conn.Conditions.Add(new LinkCondition());

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(conn.GetSummary(), MessageType.None);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Remove Connection"))
            { RemoveConnection(conn.FromId, conn.ToId); SetSelectedConn(null); }

            _lineLayer.MarkDirtyRepaint();
        }

        // =========================================================
        //  Type discovery & list
        // =========================================================
        private void RefreshTypes()
        {
            _allProjectTypes = SOTypeScanner.GetProjectScriptableObjectTypes(forceRefresh: true);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string q = _typeSearch?.ToLower() ?? "";
            _filteredTypes = string.IsNullOrEmpty(q)
                ? new List<string>(_allProjectTypes)
                : _allProjectTypes.Where(t => t.ToLower().Contains(q)).ToList();
            RebuildTypeList();
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

        private static readonly Dictionary<string, string> CatLabel = new()
        {
            { "Definition",  "[DEF]" }, { "Requirement", "[REQ]" }, { "Base",    "[BASE]" },
            { "Profile",     "[PROF]" },{ "Database",    "[DB]"  }, { "Item",   "[ITEM]" },
            { "Other",       "[OTHER]" },
        };

        private void RebuildTypeList()
        {
            _typeListScroll.Clear();
            var grouped = _filteredTypes
                .GroupBy(GetCategory)
                .OrderBy(g => g.Key == "Other" ? "zzz" : g.Key);

            foreach (var group in grouped)
            {
                string lbl = CatLabel.TryGetValue(group.Key, out var v) ? v : "[OTHER]";
                var foldout = new Foldout { text = lbl + " " + group.Key + " (" + group.Count() + ")", value = true };
                foldout.style.marginLeft = foldout.style.marginRight = 4;

                var toggle = foldout.Q<Toggle>();
                if (toggle != null)
                {
                    toggle.style.backgroundColor         = new Color(0.14f, 0.14f, 0.17f);
                    toggle.style.marginTop               = 4;
                    toggle.style.paddingLeft             = 2;
                    toggle.style.paddingTop = toggle.style.paddingBottom = 3;
                    toggle.style.color                   = new Color(0.70f, 0.80f, 1f);
                    toggle.style.fontSize                = 10;
                    toggle.style.unityFontStyleAndWeight = FontStyle.Bold;
                }

                foreach (var typeName in group.OrderBy(t => t))
                {
                    var captured = typeName;
                    var item = new Button(() => CreateNode(captured, GetStaggeredPosition())) { text = captured };
                    item.style.marginLeft = item.style.marginRight = 0;
                    item.style.marginTop  = 1; item.style.marginBottom = 0;
                    item.style.paddingLeft = 8;
                    item.style.paddingTop  = item.style.paddingBottom = 3;
                    item.style.fontSize    = 10;
                    item.style.unityTextAlign     = TextAnchor.MiddleLeft;
                    item.style.backgroundColor    = new Color(0.19f, 0.19f, 0.22f);
                    item.style.color              = new Color(0.85f, 0.90f, 1f);
                    item.style.borderLeftWidth = item.style.borderRightWidth =
                    item.style.borderTopWidth  = item.style.borderBottomWidth = 0;
                    item.style.borderTopLeftRadius    = item.style.borderTopRightRadius    =
                    item.style.borderBottomLeftRadius = item.style.borderBottomRightRadius = 3;
                    item.RegisterCallback<MouseEnterEvent>(_ => item.style.backgroundColor = new Color(0.25f, 0.35f, 0.56f));
                    item.RegisterCallback<MouseLeaveEvent>(_ => item.style.backgroundColor = new Color(0.19f, 0.19f, 0.22f));
                    foldout.Add(item);
                }
                _typeListScroll.Add(foldout);
            }
        }

        private Vector2 GetStaggeredPosition()
        {
            float off = (_nodeViews.Count % 10) * 22f;
            return new Vector2(60 + off, 60 + off);
        }

        // =========================================================
        //  Node management
        // =========================================================
        private void CreateNode(string typeName, Vector2 position)
        {
            var id   = System.Guid.NewGuid().ToString();
            var node = new NodeView(id, typeName, position, this);
            _nodeViews[id] = node;
            _graphContainer.Add(node);
            _lineLayer.MarkDirtyRepaint();
        }

        private void CreateNode(NodeData data)
        {
            var node = new NodeView(data.id, data.typeName, new Vector2(data.x, data.y), this);
            node.SetLabel(data.label);
            if (!string.IsNullOrEmpty(data.assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(data.assetPath);
                if (asset != null) node.SetLinkedAsset(asset);
            }
            _nodeViews[data.id] = node;
            _graphContainer.Add(node);
        }

        public void RemoveNode(string id)
        {
            if (_nodeViews.TryGetValue(id, out var nv))
            { _graphContainer.Remove(nv); _nodeViews.Remove(id); }
            _connections.RemoveAll(c => c.FromId == id || c.ToId == id);
            if (_selectedNodeId == id) { _selectedNodeId = null; InvalidateDetailCache(); }
            _lineLayer.MarkDirtyRepaint();
            _detailContainer?.MarkDirtyRepaint();
        }

        public void NotifyNodeMoved() => _lineLayer.MarkDirtyRepaint();

        // =========================================================
        //  Link management
        // =========================================================
        public void StartLinking(NodeView node, bool isOutput)
        {
            if (isOutput)
                _pendingFromId = node.Id;
            else if (!string.IsNullOrEmpty(_pendingFromId))
            {
                TryCreateConnection(_pendingFromId, node.Id);
                _pendingFromId = null;
                _lineLayer.MarkDirtyRepaint();
            }
        }

        public void CancelLinking() { _pendingFromId = null; _lineLayer.MarkDirtyRepaint(); }

        public bool TryCreateConnection(string fromId, string toId)
        {
            if (fromId == toId) return false;
            if (_connections.Any(c => c.FromId == fromId && c.ToId == toId)) return false;
            if (_checkCircular && WouldCreateCycle(fromId, toId))
            {
                EditorUtility.DisplayDialog("Circular Dependency",
                    "This link would create a circular dependency.", "OK");
                return false;
            }
            _connections.Add(new ConnectionData(fromId, toId));
            _lineLayer.MarkDirtyRepaint();
            return true;
        }

        public void RemoveConnection(string fromId, string toId)
        {
            _connections.RemoveAll(c => c.FromId == fromId && c.ToId == toId);
            _lineLayer.MarkDirtyRepaint();
        }

        private bool WouldCreateCycle(string fromId, string toId)
        {
            var visited = new HashSet<string>();
            var stack   = new Stack<string>();
            stack.Push(toId);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                if (!visited.Add(cur)) continue;
                foreach (var c in _connections.Where(c => c.FromId == cur))
                    stack.Push(c.ToId);
            }
            return visited.Contains(fromId);
        }

        // =========================================================
        //  Bezier drawing + connection midpoint buttons
        // =========================================================
        private void DrawLines()
        {
            if (Event.current == null) return;
            Handles.BeginGUI();

            foreach (var conn in _connections)
            {
                if (!_nodeViews.TryGetValue(conn.FromId, out var fn)) continue;
                if (!_nodeViews.TryGetValue(conn.ToId,   out var tn)) continue;

                Vector2 s    = PortPos(fn, true);
                Vector2 e    = PortPos(tn, false);
                bool    sel  = (_selectedConn == conn);
                DrawBezier(s, e, conn, sel);

                // Midpoint edit button (IMGUI button so it works in IMGUI layer)
                Vector2 mid  = (s + e) * 0.5f;
                string  btnTxt = conn.Conditions.Count > 0
                    ? conn.Conditions.Count + " cond."
                    : "edit";
                Color   old  = GUI.backgroundColor;
                GUI.backgroundColor = sel
                    ? new Color(0.3f, 0.6f, 1f)
                    : (conn.Conditions.Count > 0 ? new Color(0.25f, 0.65f, 0.85f) : new Color(0.25f, 0.25f, 0.30f));
                if (GUI.Button(new Rect(mid.x - 28, mid.y - 10, 56, 20), btnTxt))
                    SetSelectedConn(conn);
                GUI.backgroundColor = old;
            }

            // Preview line
            if (_pendingFromId != null && _nodeViews.TryGetValue(_pendingFromId, out var pn))
            {
                Vector2 s = PortPos(pn, true);
                Handles.DrawBezier(s, _mousePos,
                    s + Vector2.right * 60, _mousePos + Vector2.left * 60,
                    new Color(1f, 1f, 0.4f, 0.5f), null, 2f);
            }

            Handles.EndGUI();
        }

        private static void DrawBezier(Vector2 s, Vector2 e, ConnectionData conn, bool selected)
        {
            bool    hasCond = conn.Conditions.Count > 0;
            Color   col;
            if      (selected)  col = new Color(0.30f, 0.65f, 1.00f);
            else if (hasCond)   col = new Color(0.35f, 0.80f, 1.00f);
            else                col = new Color(0.50f, 0.90f, 0.40f);
            float w   = selected ? 3.5f : (hasCond ? 2.5f : 2f);
            float tan = Mathf.Max(70f, Mathf.Abs(e.x - s.x) * 0.5f);

            Handles.DrawBezier(s, e, s + Vector2.right * tan, e - Vector2.right * tan, col, null, w);

            // Arrowhead
            Vector2 dir = (e - s).normalized;
            Handles.color = col;
            Handles.DrawAAConvexPolygon(e,
                e - dir * 12f + new Vector2(-dir.y,  dir.x) * 5f,
                e - dir * 12f + new Vector2( dir.y, -dir.x) * 5f);

            // Label (connection label if set)
            if (!string.IsNullOrEmpty(conn.Label))
            {
                Vector2 mid = (s + e) * 0.5f + Vector2.up * 18f;
                var style   = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(mid.x - 60, mid.y - 8, 120, 16), conn.Label, style);
            }
        }

        private static Vector2 PortPos(NodeView n, bool isOutput)
        {
            float x = n.resolvedStyle.left + (isOutput ? n.resolvedStyle.width + 5f : -5f);
            float y = n.resolvedStyle.top  + n.resolvedStyle.height * 0.5f;
            return new Vector2(x, y);
        }

        // =========================================================
        //  Save / Load
        // =========================================================
        private void SaveTree()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save SO Tree", "MySOTree", "json", "Choose save location");
            if (string.IsNullOrEmpty(path)) return;

            var data = new SOTreeData();
            foreach (var kv in _nodeViews)
            {
                var n = kv.Value;
                data.nodes.Add(new NodeData
                {
                    id        = n.Id,
                    typeName  = n.TypeName,
                    label     = n.Label,
                    assetPath = n.LinkedAsset != null ? AssetDatabase.GetAssetPath(n.LinkedAsset) : "",
                    x         = n.resolvedStyle.left,
                    y         = n.resolvedStyle.top
                });
            }
            data.connections.AddRange(_connections);

            System.IO.File.WriteAllText(
                System.IO.Path.GetFullPath(path),
                JsonUtility.ToJson(data, prettyPrint: true));
            AssetDatabase.Refresh();
            Debug.Log("[SO Tree] Saved to " + path);
        }

        private void LoadTree()
        {
            string path = EditorUtility.OpenFilePanel("Load SO Tree", "Assets", "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = JsonUtility.FromJson<SOTreeData>(System.IO.File.ReadAllText(path));
            if (data == null) { Debug.LogError("[SO Tree] Failed to parse JSON."); return; }

            ClearAll();
            foreach (var nd in data.nodes)       CreateNode(nd);
            foreach (var cd in data.connections) _connections.Add(cd);
            _lineLayer.MarkDirtyRepaint();
            Debug.Log("[SO Tree] Loaded " + data.nodes.Count + " nodes, " + data.connections.Count + " connections.");
        }

        private void ClearAll()
        {
            foreach (var nv in _nodeViews.Values) _graphContainer.Remove(nv);
            _nodeViews.Clear();
            _connections.Clear();
            _pendingFromId = null;
            _selectedNodeId = null;
            _selectedConn   = null;
            InvalidateDetailCache();
            _lineLayer?.MarkDirtyRepaint();
            _detailContainer?.MarkDirtyRepaint();
        }
    }

    // =========================================================================
    //  Node View
    // =========================================================================
    public class NodeView : VisualElement
    {
        public string           Id          { get; }
        public string           TypeName    { get; }
        public string           Label       { get; private set; }
        public ScriptableObject LinkedAsset { get; private set; }

        private readonly SOTreeEditor _editor;
        private bool    _dragging;
        private Vector2 _dragStartMouse;
        private Vector2 _dragStartPos;
        private bool    _isSelected;

        private Label   _assetLabel;

        private static readonly Dictionary<string, Color> CatColors = new()
        {
            { "Definition",  new Color(0.18f, 0.38f, 0.72f) },
            { "Requirement", new Color(0.55f, 0.25f, 0.55f) },
            { "Base",        new Color(0.20f, 0.50f, 0.35f) },
            { "Profile",     new Color(0.55f, 0.38f, 0.10f) },
            { "Database",    new Color(0.20f, 0.38f, 0.50f) },
            { "Item",        new Color(0.55f, 0.45f, 0.10f) },
            { "Other",       new Color(0.30f, 0.30f, 0.35f) },
        };

        private static Color GetHeaderColor(string name)
        {
            if (name.EndsWith("Definition"))  return CatColors["Definition"];
            if (name.EndsWith("Requirement")) return CatColors["Requirement"];
            if (name.EndsWith("Base"))        return CatColors["Base"];
            if (name.EndsWith("Profile"))     return CatColors["Profile"];
            if (name.EndsWith("DB"))          return CatColors["Database"];
            if (name.Contains("Item"))        return CatColors["Item"];
            return CatColors["Other"];
        }

        public NodeView(string id, string typeName, Vector2 pos, SOTreeEditor editor)
        {
            Id = id; TypeName = typeName; Label = typeName; _editor = editor;

            style.position = Position.Absolute;
            style.left = pos.x; style.top = pos.y;
            style.width     = 175;
            style.minHeight = 66;
            style.backgroundColor        = new Color(0.20f, 0.22f, 0.26f);
            style.borderTopLeftRadius    = style.borderTopRightRadius    =
            style.borderBottomLeftRadius = style.borderBottomRightRadius = 7;
            UpdateBorderColor();

            // Header
            Color hc   = GetHeaderColor(typeName);
            var header = new VisualElement();
            header.style.backgroundColor    = hc;
            header.style.borderTopLeftRadius = header.style.borderTopRightRadius = 6;
            header.style.flexDirection  = FlexDirection.Row;
            header.style.alignItems     = Align.Center;
            header.style.paddingLeft    = 7;
            header.style.paddingTop = header.style.paddingBottom = 4;

            var titleLbl = new Label(typeName);
            titleLbl.style.color                  = Color.white;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.fontSize               = 10;
            titleLbl.style.flexGrow               = 1;
            titleLbl.style.overflow               = Overflow.Hidden;
            header.Add(titleLbl);

            var del = new Button(() => _editor.RemoveNode(Id)) { text = "X" };
            del.style.width = del.style.height = 16;
            del.style.fontSize    = 8; del.style.marginRight = 3;
            del.style.paddingLeft = del.style.paddingRight =
            del.style.paddingTop  = del.style.paddingBottom = 0;
            del.style.backgroundColor    = new Color(0, 0, 0, 0.3f);
            del.style.color              = new Color(1f, 0.75f, 0.75f);
            del.style.borderLeftWidth    = del.style.borderRightWidth    =
            del.style.borderTopWidth     = del.style.borderBottomWidth   = 0;
            del.style.borderTopLeftRadius    = del.style.borderTopRightRadius    =
            del.style.borderBottomLeftRadius = del.style.borderBottomRightRadius = 3;
            header.Add(del);
            Add(header);

            // Asset label
            _assetLabel = new Label("(no asset)");
            _assetLabel.style.fontSize      = 9;
            _assetLabel.style.color         = new Color(0.55f, 0.65f, 0.75f);
            _assetLabel.style.paddingLeft   = 7;
            _assetLabel.style.paddingTop    = 2;
            _assetLabel.style.paddingBottom = 2;
            _assetLabel.style.overflow      = Overflow.Hidden;
            Add(_assetLabel);

            // Label field
            var lf = new TextField { value = Label };
            lf.style.marginLeft = lf.style.marginRight = 6;
            lf.style.marginTop  = lf.style.marginBottom = 3;
            lf.style.fontSize   = 10;
            lf.RegisterValueChangedCallback(e => Label = e.newValue);
            Add(lf);

            // Ports
            Add(MakePort(isOutput: true,  color: new Color(0.30f, 0.82f, 0.30f)));
            Add(MakePort(isOutput: false, color: new Color(0.82f, 0.30f, 0.30f)));

            // Drag + select
            RegisterCallback<MouseDownEvent>(OnDown);
            RegisterCallback<MouseMoveEvent>(OnMove);
            RegisterCallback<MouseUpEvent>(OnUp);
        }

        public void SetLabel(string lbl)
        {
            Label = lbl;
            var tf = this.Q<TextField>();
            if (tf != null) tf.SetValueWithoutNotify(lbl);
        }

        public void SetLinkedAsset(ScriptableObject asset)
        {
            LinkedAsset = asset;
            _assetLabel.text  = asset != null ? asset.name : "(no asset)";
            _assetLabel.style.color = asset != null
                ? new Color(0.45f, 0.85f, 0.55f)
                : new Color(0.55f, 0.65f, 0.75f);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateBorderColor();
        }

        private void UpdateBorderColor()
        {
            Color c = _isSelected
                ? new Color(0.30f, 0.65f, 1.00f)
                : new Color(0.35f, 0.45f, 0.65f);
            style.borderLeftColor = style.borderRightColor =
            style.borderTopColor  = style.borderBottomColor = c;
            style.borderLeftWidth = style.borderRightWidth =
            style.borderTopWidth  = style.borderBottomWidth = _isSelected ? 2.5f : 1.5f;
        }

        private VisualElement MakePort(bool isOutput, Color color)
        {
            var p = new VisualElement();
            p.style.position = Position.Absolute;
            p.style.width = p.style.height = 14;
            p.style.left  = isOutput ? StyleKeyword.Auto : (StyleLength)(-8f);
            p.style.right = isOutput ? (StyleLength)(-8f) : StyleKeyword.Auto;
            p.style.top   = (StyleLength)28f;
            p.style.backgroundColor = color;
            p.style.borderTopLeftRadius    = p.style.borderTopRightRadius    =
            p.style.borderBottomLeftRadius = p.style.borderBottomRightRadius = 7;
            p.style.borderLeftColor = p.style.borderRightColor =
            p.style.borderTopColor  = p.style.borderBottomColor = Color.white;
            p.style.borderLeftWidth = p.style.borderRightWidth =
            p.style.borderTopWidth  = p.style.borderBottomWidth = 1f;
            bool cap = isOutput;
            p.RegisterCallback<MouseDownEvent>(e =>
            { _editor.StartLinking(this, cap); e.StopPropagation(); });
            p.tooltip = isOutput ? "Output port" : "Input port";
            return p;
        }

        private void OnDown(MouseDownEvent e)
        {
            if (e.button != 0) return;
            _editor.SetSelectedNode(Id);
            _dragging       = true;
            _dragStartMouse = e.mousePosition;
            _dragStartPos   = new Vector2(resolvedStyle.left, resolvedStyle.top);
            this.CaptureMouse();
            BringToFront();
            e.StopPropagation();
        }

        private void OnMove(MouseMoveEvent e)
        {
            if (!_dragging) return;
            Vector2 d = e.mousePosition - _dragStartMouse;
            style.left = _dragStartPos.x + d.x;
            style.top  = _dragStartPos.y + d.y;
            _editor.NotifyNodeMoved();
        }

        private void OnUp(MouseUpEvent e)
        {
            if (!_dragging) return;
            _dragging = false;
            this.ReleaseMouse();
        }
    }

    // =========================================================================
    //  Data models
    // =========================================================================
    [System.Serializable]
    public class SOTreeData
    {
        public List<NodeData>       nodes       = new();
        public List<ConnectionData> connections = new();
    }

    [System.Serializable]
    public class NodeData
    {
        public string id, typeName, label, assetPath;
        public float  x, y;
    }

    public enum ConditionLogic { AND, OR }

    [System.Serializable]
    public class ConnectionData
    {
        public string         FromId, ToId;
        public string         Label      = "";
        public ConditionLogic LogicOp    = ConditionLogic.AND;
        public List<LinkCondition> Conditions = new();

        public ConnectionData() {}
        public ConnectionData(string f, string t) { FromId = f; ToId = t; }

        public string GetSummary()
        {
            if (Conditions.Count == 0) return "(no conditions - always passes)";
            string sep  = LogicOp == ConditionLogic.AND ? " AND " : " OR ";
            string[] OpStr = { "==", "!=", ">", "<", ">=", "<=", "Contains", "HasFlag" };
            return string.Join(sep, Conditions.Select(c =>
                (c.Negate ? "NOT " : "") + (c.Key ?? "?") + " " +
                (c.Op >= 0 && c.Op < OpStr.Length ? OpStr[c.Op] : "?") + " " +
                (c.Value ?? "?")));
        }
    }

    [System.Serializable]
    public class LinkCondition
    {
        public string Key   = "";
        public int    Op    = 0;  // index into OpLabels
        public string Value = "";
        public bool   Negate = false;
        public string Note  = "";
    }
}
