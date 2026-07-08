using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace SOTreeTool
{
    public class SOApiImporter : EditorWindow
    {
        private enum ImportType { Pokemon, Move, Item, Ability }
        private ImportType _currentType = ImportType.Pokemon;

        private string _searchId = "pikachu";
        private string _status = "Enter ID or Name and click Fetch.";
        private bool _isFetching = false;

        // Autocomplete suggestions state
        private List<string> _pokemonSuggestions = new();
        private List<string> _moveSuggestions = new();
        private List<string> _itemSuggestions = new();
        private List<string> _abilitySuggestions = new();
        
        private List<string> _currentMatches = new();
        private bool _dbLoaded = false;
        private bool _isDownloadingDb = false;

        // Fetch result caches
        private string _fetchedName = "";
        private string _fetchedDesc = "";

        // Multi-version Pokedex entries cache
        private struct VersionDescription
        {
            public string version;
            public string text;
        }
        private List<VersionDescription> _versionDescriptions = new();
        private int _selectedVersionIndex = 0;

        // Pokemon stats cache
        private int _hp, _atk, _def, _spAtk, _spDef, _spd;
        private float _weight, _height;
        private string _type1 = "None", _type2 = "None";
        private int _catchRate = 255;
        private int _xpYield = 0;
        private string _pokemonGenRoman = "generation-i";

        // Gender & Growth details
        private float _maleRatio = 0.5f;
        private bool _isGenderless = false;
        private bool _hasGenderDifferences = false;
        private string _growthRateName = "medium";
        private string _fetchedAbilityName = "";

        // EVs cache
        private int _evHp, _evAtk, _evDef, _evSpA, _evSpD, _evSpd;

        // Local Sprites (selected by user from assets) - Grid organized
        private Sprite _localFrontSprite;
        private Sprite _localBackSprite;
        private Sprite _localShinyFrontSprite;
        private Sprite _localShinyBackSprite;
        private Sprite _localFemaleFrontSprite;
        private Sprite _localFemaleBackSprite;
        private Sprite _localShinyFemaleFrontSprite;
        private Sprite _localShinyFemaleBackSprite;
        private Sprite _localIconSprite;
        private Sprite _localShinyIconSprite;

        // Evolution data cache
        private struct ParsedEvolution
        {
            public string targetName;
            public int minLevel;
            public string timeOfDay;
            public string itemName;
        }
        private List<ParsedEvolution> _parsedEvolutions = new();
        private string _evolutionChainString = "";

        // Locations/Encounter cache
        private List<string> _fetchedLocations = new();

        // Learnable moves cache
        private struct ParsedMove
        {
            public string moveName;
            public int level;
        }
        private List<ParsedMove> _parsedMoves = new();
        private List<string> _parsedTmMoves = new();

        // Move cache
        private int _power, _accuracy, _pp, _priority;
        private string _moveType = "None";
        private string _moveCategory = "Physical";

        // Item cache
        private float _price;
        private string _itemCategory = "HealHP";

        // Ability cache
        private string _abilityNameId = "None";

        // Save Options
        private string _saveFolder = "Assets/Game/Resources/Pokemons/Generation 1";
        private string _fileName = "";

        // UI Scroll
        private Vector2 _previewScrollPos;

        // Foldout states
        private bool _foldSprites = true;
        private bool _foldGeneral = true;
        private bool _foldStats = true;
        private bool _foldEvoLoc = true;
        private bool _foldLevelMoves = false;
        private bool _foldTmMoves = false;

        // Doomsday / Mass Importer State
        private int _doomsdayStart = 1;
        private int _doomsdayEnd = 151;
        private bool _isDoomsdayRunning = false;

        [MenuItem("Tools/SO API Importer")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SOApiImporter>();
            wnd.titleContent = new GUIContent("SO API Importer",
                EditorGUIUtility.IconContent("d_CloudConnect").image);
            wnd.minSize = new Vector2(520, 640);
        }

        private void OnEnable()
        {
            UpdateDefaultFolder();
            LoadOrDownloadSearchDatabase();
        }

        private void UpdateDefaultFolder()
        {
            switch (_currentType)
            {
                case ImportType.Pokemon:
                    _saveFolder = MapGenerationToFolder(_pokemonGenRoman);
                    break;
                case ImportType.Move:
                    _saveFolder = "Assets/Game/Resources/Moves/" + _moveCategory;
                    break;
                case ImportType.Item:
                    _saveFolder = "Assets/Game/Resources/Items";
                    break;
                case ImportType.Ability:
                    _saveFolder = "Assets/Game/Resources/Abilities";
                    break;
            }
            if (!Directory.Exists(_saveFolder))
            {
                _saveFolder = "Assets/Game/Resources/" + _currentType.ToString() + "s";
                if (!Directory.Exists(_saveFolder))
                {
                    _saveFolder = "Assets";
                }
            }
            UpdateMatches();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("ScriptableObject API Importer (PokeAPI)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Import stats, descriptions, and data directly into your assets.", EditorStyles.miniLabel);
            EditorGUILayout.Space(6);

            // Import type selector
            EditorGUILayout.BeginHorizontal();
            var prevType = _currentType;
            _currentType = (ImportType)GUILayout.Toolbar((int)_currentType, new string[] { "Pokemon", "Move", "Item", "Ability" });
            if (_currentType != prevType)
            {
                UpdateDefaultFolder();
                _fetchedName = "";
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);

            // Input section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Search by Name or Pokedex ID / Index:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            string prevSearch = _searchId;
            _searchId = EditorGUILayout.TextField(_searchId).Trim().ToLower();
            if (_searchId != prevSearch)
            {
                UpdateMatches();
            }

            EditorGUI.BeginDisabledGroup(_isFetching || string.IsNullOrEmpty(_searchId) || _isDoomsdayRunning);
            if (GUILayout.Button("Fetch", GUILayout.Width(60)))
            {
                StartFetch();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Render autocomplete suggestions if typing
            DrawAutocompleteSuggestions();

            EditorGUILayout.EndVertical();

            // Status display
            EditorGUILayout.HelpBox(_status, _isFetching ? MessageType.Info : MessageType.None);

            if (!string.IsNullOrEmpty(_fetchedName) && !_isDoomsdayRunning)
            {
                _previewScrollPos = EditorGUILayout.BeginScrollView(_previewScrollPos);
                DrawPreviewPanel();
                EditorGUILayout.EndScrollView();
            }

            // Doomsday/Mass Importer UI Section
            DrawDoomsdayUI();
        }

        private void DrawDoomsdayUI()
        {
            EditorGUILayout.Space(15);
            GUILayout.Label("Mass Importer (Doomsday Command)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Warning: This downloads a range of Pokemon sequentially. Existing assets will be updated in-place.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            _doomsdayStart = EditorGUILayout.IntField("Start Pokedex ID:", _doomsdayStart);
            _doomsdayEnd = EditorGUILayout.IntField("End Pokedex ID:", _doomsdayEnd);
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(_isFetching || _isDoomsdayRunning);
            if (GUILayout.Button("DOOMSDAY IMPORT: Fetch & Update All in Range", GUILayout.Height(30)))
            {
                StartDoomsdayImport();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawAutocompleteSuggestions()
        {
            if (!_dbLoaded)
            {
                EditorGUILayout.Space(2);
                if (_isDownloadingDb)
                {
                    GUILayout.Label("Downloading search autocomplete database...", EditorStyles.miniLabel);
                }
                else
                {
                    if (GUILayout.Button("Download Search Autocomplete Database (Offline)", EditorStyles.miniButton))
                    {
                        DownloadSearchDatabase();
                    }
                }
                return;
            }

            if (_currentMatches.Count > 0 && _searchId.Length >= 2 && !_currentMatches.Contains(_searchId))
            {
                EditorGUILayout.Space(2);
                GUILayout.Label("Suggestions:", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.BeginHorizontal();
                float widthUsed = 0;
                int countShown = 0;
                
                foreach (var match in _currentMatches)
                {
                    if (countShown >= 12) break;
                    
                    float buttonWidth = GUI.skin.button.CalcSize(new GUIContent(match)).x + 8;
                    if (widthUsed + buttonWidth > position.width - 40)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        widthUsed = 0;
                    }

                    if (GUILayout.Button(match, EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
                    {
                        _searchId = match;
                        GUI.FocusControl(null);
                        UpdateMatches();
                        StartFetch();
                    }

                    widthUsed += buttonWidth;
                    countShown++;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void UpdateMatches()
        {
            if (!_dbLoaded) return;

            List<string> sourceList = null;
            switch (_currentType)
            {
                case ImportType.Pokemon: sourceList = _pokemonSuggestions; break;
                case ImportType.Move:    sourceList = _moveSuggestions; break;
                case ImportType.Item:    sourceList = _itemSuggestions; break;
                case ImportType.Ability: sourceList = _abilitySuggestions; break;
            }

            if (sourceList == null || string.IsNullOrEmpty(_searchId) || _searchId.Length < 2)
            {
                _currentMatches.Clear();
                return;
            }

            _currentMatches = sourceList
                .Where(n => n.StartsWith(_searchId))
                .Union(sourceList.Where(n => n.Contains(_searchId)))
                .Take(15)
                .ToList();
        }

        private void DrawPreviewPanel()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Import Preview & Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Save Folder picker
            EditorGUILayout.BeginHorizontal();
            _saveFolder = EditorGUILayout.TextField("Save Folder:", _saveFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Destination", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        _saveFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside the Assets directory.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            _fileName = EditorGUILayout.TextField("Asset Filename:", _fileName);
            EditorGUILayout.Space(6);

            // Display common values
            EditorGUILayout.LabelField("Name (UI):", _fetchedName);

            // Multi-version description support
            if (_versionDescriptions.Count > 1)
            {
                string[] versionNames = _versionDescriptions.Select(vd => vd.version.ToUpper()).ToArray();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Game Description Version:", EditorStyles.miniLabel, GUILayout.Width(150));
                int newVerIdx = EditorGUILayout.Popup(_selectedVersionIndex, versionNames);
                if (newVerIdx != _selectedVersionIndex)
                {
                    _selectedVersionIndex = newVerIdx;
                    _fetchedDesc = _versionDescriptions[_selectedVersionIndex].text;
                }
                EditorGUILayout.EndHorizontal();
            }

            _fetchedDesc = EditorGUILayout.TextArea(_fetchedDesc, GUILayout.Height(50));
            EditorGUILayout.Space(6);

            // Detail panels depending on selected type
            if (_currentType == ImportType.Pokemon)
            {
                DrawPokemonPreview();
            }
            else if (_currentType == ImportType.Move)
            {
                DrawMovePreview();
            }
            else if (_currentType == ImportType.Item)
            {
                DrawItemPreview();
            }
            else if (_currentType == ImportType.Ability)
            {
                DrawAbilityPreview();
            }

            EditorGUILayout.Space(12);
            if (GUILayout.Button("Generate / Update ScriptableObject Asset (Resolves Refs)", GUILayout.Height(34)))
            {
                GenerateAsset();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPokemonPreview()
        {
            // 1. Foldout: Sprites Grid
            _foldSprites = EditorGUILayout.Foldout(_foldSprites, "Local Sprites (Visuals Grid)", true, EditorStyles.foldoutHeader);
            if (_foldSprites)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Header of grid
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Sprite Position", EditorStyles.miniBoldLabel, GUILayout.Width(120));
                GUILayout.Label("Normal Variant", EditorStyles.miniBoldLabel, GUILayout.MinWidth(150));
                GUILayout.Label("Shiny Variant", EditorStyles.miniBoldLabel, GUILayout.MinWidth(150));
                EditorGUILayout.EndHorizontal();

                // Front row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Front Sprite:", GUILayout.Width(120));
                _localFrontSprite = (Sprite)EditorGUILayout.ObjectField(_localFrontSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                _localShinyFrontSprite = (Sprite)EditorGUILayout.ObjectField(_localShinyFrontSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                EditorGUILayout.EndHorizontal();

                // Back row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Back Sprite:", GUILayout.Width(120));
                _localBackSprite = (Sprite)EditorGUILayout.ObjectField(_localBackSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                _localShinyBackSprite = (Sprite)EditorGUILayout.ObjectField(_localShinyBackSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                EditorGUILayout.EndHorizontal();

                // Female front row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Female Front:", GUILayout.Width(120));
                _localFemaleFrontSprite = (Sprite)EditorGUILayout.ObjectField(_localFemaleFrontSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                _localShinyFemaleFrontSprite = (Sprite)EditorGUILayout.ObjectField(_localShinyFemaleFrontSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                EditorGUILayout.EndHorizontal();

                // Female back row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Female Back:", GUILayout.Width(120));
                _localFemaleBackSprite = (Sprite)EditorGUILayout.ObjectField(_localFemaleBackSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                _localShinyFemaleBackSprite = (Sprite)EditorGUILayout.ObjectField(_localShinyFemaleBackSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                EditorGUILayout.EndHorizontal();

                // Party Icon row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Party Icon:", GUILayout.Width(120));
                _localIconSprite = (Sprite)EditorGUILayout.ObjectField(_localIconSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                _localShinyIconSprite = (Sprite)EditorGUILayout.ObjectField(_localShinyIconSprite, typeof(Sprite), false, GUILayout.MinWidth(150));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // 2. Foldout: General Info
            _foldGeneral = EditorGUILayout.Foldout(_foldGeneral, "General Properties", true, EditorStyles.foldoutHeader);
            if (_foldGeneral)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Types:", _type1 + " / " + _type2);
                EditorGUILayout.LabelField("Height:", _height + " m  |  Weight: " + _weight + " kg");
                EditorGUILayout.LabelField("Growth Rate:", _growthRateName);
                EditorGUILayout.LabelField("Default Ability:", _fetchedAbilityName);
                EditorGUILayout.LabelField("Genderless:", _isGenderless.ToString() + (_isGenderless ? "" : " (Male Ratio: " + _maleRatio + ")"));
                EditorGUILayout.LabelField("Catch Rate:", _catchRate.ToString());
                EditorGUILayout.LabelField("XP Yield:", _xpYield.ToString());
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // 3. Foldout: Stats Table
            _foldStats = EditorGUILayout.Foldout(_foldStats, "Base Stats & EV Yields", true, EditorStyles.foldoutHeader);
            if (_foldStats)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Draw a nice aligned grid table
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Stat", EditorStyles.miniBoldLabel, GUILayout.Width(100));
                GUILayout.Label("Base Value", EditorStyles.miniBoldLabel, GUILayout.Width(100));
                GUILayout.Label("EV Yield Awarded", EditorStyles.miniBoldLabel, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();

                DrawStatRow("HP", _hp, _evHp);
                DrawStatRow("Attack", _atk, _evAtk);
                DrawStatRow("Defense", _def, _evDef);
                DrawStatRow("Sp. Attack", _spAtk, _evSpA);
                DrawStatRow("Sp. Defense", _spDef, _evSpD);
                DrawStatRow("Speed", _spd, _evSpd);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // 4. Foldout: Evolutions & Locations
            _foldEvoLoc = EditorGUILayout.Foldout(_foldEvoLoc, "Evolution & Location Data", true, EditorStyles.foldoutHeader);
            if (_foldEvoLoc)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Full evolution string
                EditorGUILayout.LabelField("Full Evolution Chain:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(string.IsNullOrEmpty(_evolutionChainString) ? "(None)" : _evolutionChainString, EditorStyles.wordWrappedLabel);
                
                if (_parsedEvolutions.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Direct Evolution Dependencies:", EditorStyles.miniBoldLabel);
                    foreach (var evo in _parsedEvolutions)
                    {
                        string info = evo.targetName + " (LV: " + evo.minLevel + 
                                      (!string.IsNullOrEmpty(evo.itemName) ? ", Item: " + evo.itemName : "") +
                                      (!string.IsNullOrEmpty(evo.timeOfDay) ? ", Time: " + evo.timeOfDay : "") + ")";
                        EditorGUILayout.LabelField("  - " + info);
                    }
                }

                // Locations List
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Wild Encounter Locations:", EditorStyles.miniBoldLabel);
                if (_fetchedLocations.Count == 0)
                {
                    EditorGUILayout.LabelField("  No locations returned or not in wild.", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var loc in _fetchedLocations)
                    {
                        EditorGUILayout.LabelField("  - " + loc);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // 5. Foldout: Level Up Moves
            _foldLevelMoves = EditorGUILayout.Foldout(_foldLevelMoves, "Learnable Level Up Moves (" + _parsedMoves.Count + ")", true, EditorStyles.foldoutHeader);
            if (_foldLevelMoves)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (_parsedMoves.Count == 0)
                {
                    EditorGUILayout.LabelField("No level-up moves parsed.");
                }
                else
                {
                    foreach (var m in _parsedMoves)
                    {
                        EditorGUILayout.LabelField("  Level " + m.level + ": " + m.moveName);
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // 6. Foldout: TM Moves
            _foldTmMoves = EditorGUILayout.Foldout(_foldTmMoves, "Learnable TM Moves (" + _parsedTmMoves.Count + ")", true, EditorStyles.foldoutHeader);
            if (_foldTmMoves)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (_parsedTmMoves.Count == 0)
                {
                    EditorGUILayout.LabelField("No TM moves parsed.");
                }
                else
                {
                    foreach (var tm in _parsedTmMoves)
                    {
                        EditorGUILayout.LabelField("  - " + tm);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawStatRow(string label, int baseVal, int evVal)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            GUILayout.Label(baseVal.ToString(), GUILayout.Width(100));
            GUILayout.Label(evVal.ToString(), GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMovePreview()
        {
            _moveType = EditorGUILayout.TextField("Move Type:", _moveType);
            _moveCategory = EditorGUILayout.TextField("Category:", _moveCategory);
            EditorGUILayout.LabelField("Power:", _power.ToString());
            EditorGUILayout.LabelField("Accuracy:", _accuracy.ToString());
            EditorGUILayout.LabelField("PP:", _pp.ToString());
            EditorGUILayout.LabelField("Priority:", _priority.ToString());
        }

        private void DrawItemPreview()
        {
            _price = EditorGUILayout.FloatField("Price (Buy):", _price);
            _itemCategory = EditorGUILayout.TextField("Item Category:", _itemCategory);
        }

        private void DrawAbilityPreview()
        {
            EditorGUILayout.LabelField("Mapped Enum ID:", _abilityNameId);
        }

        // =========================================================
        //  Networking & Parsing
        // =========================================================
        private void StartFetch()
        {
            _isFetching = true;
            _status = "Contacting PokeAPI...";
            _versionDescriptions.Clear();
            _parsedEvolutions.Clear();
            _parsedMoves.Clear();
            _parsedTmMoves.Clear();
            _fetchedLocations.Clear();
            _evolutionChainString = "";
            _selectedVersionIndex = 0;

            string endpoint = "";
            if (_currentType == ImportType.Pokemon)
                endpoint = "pokemon/" + _searchId;
            else if (_currentType == ImportType.Move)
                endpoint = "move/" + _searchId;
            else if (_currentType == ImportType.Item)
                endpoint = "item/" + _searchId;
            else if (_currentType == ImportType.Ability)
                endpoint = "ability/" + _searchId;

            string url = "https://pokeapi.co/api/v2/" + endpoint;
            EditorCoroutineUtility.StartCoroutine(FetchRoutine(url), this);
        }

        private IEnumerator FetchRoutine(string url)
        {
            using (UnityWebRequest webReq = UnityWebRequest.Get(url))
            {
                yield return webReq.SendWebRequest();

                if (webReq.result != UnityWebRequest.Result.Success)
                {
                    _status = "Error: " + webReq.error;
                    _isFetching = false;
                    _fetchedName = "";
                    Repaint();
                    yield break;
                }

                string json = webReq.downloadHandler.text;
                ParseJson(json);
            }

            // If Pokemon, fetch description, growth rate, gender info, and evolution details
            if (_currentType == ImportType.Pokemon)
            {
                _status = "Fetching species description...";
                string speciesUrl = "https://pokeapi.co/api/v2/pokemon-species/" + _searchId;
                string speciesJson = "";
                using (UnityWebRequest webReq = UnityWebRequest.Get(speciesUrl))
                {
                    yield return webReq.SendWebRequest();
                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        speciesJson = webReq.downloadHandler.text;
                        ParseSpeciesJson(speciesJson);
                    }
                }

                // Parse evolution chain URL
                var matchEvo = Regex.Match(speciesJson, @"\""evolution_chain\""\s*:\s*\{\s*\""url\""\s*:\s*\""([^\""]+)""");
                if (matchEvo.Success)
                {
                    string chainUrl = matchEvo.Groups[1].Value;
                    _status = "Fetching evolution chain...";
                    using (UnityWebRequest webReq = UnityWebRequest.Get(chainUrl))
                    {
                        yield return webReq.SendWebRequest();
                        if (webReq.result == UnityWebRequest.Result.Success)
                        {
                            ParseEvolutionChainJson(webReq.downloadHandler.text);
                        }
                    }
                }

                // Fetch wild encounter locations
                _status = "Fetching encounter locations...";
                string encountersUrl = "https://pokeapi.co/api/v2/pokemon/" + _searchId + "/encounters";
                using (UnityWebRequest webReq = UnityWebRequest.Get(encountersUrl))
                {
                    yield return webReq.SendWebRequest();
                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        ParseEncountersJson(webReq.downloadHandler.text);
                    }
                }
            }

            _status = "Data loaded successfully.";
            _isFetching = false;
            UpdateDefaultFolder();
            Repaint();
        }

        private void ParseJson(string json)
        {
            _fetchedName = GetRootJsonValue(json, "name");
            if (string.IsNullOrEmpty(_fetchedName))
            {
                _fetchedName = _searchId;
            }
            _fetchedName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_fetchedName);
            _fileName = _fetchedName.Replace("-", "");

            if (_currentType == ImportType.Pokemon)
            {
                _weight = GetRootJsonIntValue(json, "weight") / 10f;
                _height = GetRootJsonIntValue(json, "height") / 10f;
                _xpYield = GetRootJsonIntValue(json, "base_experience");

                // Parse base stats & EV yields (effort)
                var statMatches = Regex.Matches(json, @"\""base_stat\""\s*:\s*([0-9]+)\s*,\s*\""effort\""\s*:\s*([0-9]+)\s*,\s*\""stat\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
                foreach (Match m in statMatches)
                {
                    int val = int.Parse(m.Groups[1].Value);
                    int evVal = int.Parse(m.Groups[2].Value);
                    string name = m.Groups[3].Value;

                    if (name == "hp") { _hp = val; _evHp = evVal; }
                    else if (name == "attack") { _atk = val; _evAtk = evVal; }
                    else if (name == "defense") { _def = val; _evDef = evVal; }
                    else if (name == "special-attack") { _spAtk = val; _evSpA = evVal; }
                    else if (name == "special-defense") { _spDef = val; _evSpD = evVal; }
                    else if (name == "speed") { _spd = val; _evSpd = evVal; }
                }

                // Parse types
                var typeMatches = Regex.Matches(json, @"\""type\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
                if (typeMatches.Count > 0) _type1 = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeMatches[0].Groups[1].Value);
                if (typeMatches.Count > 1) _type2 = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeMatches[1].Groups[1].Value);

                // Parse first non-hidden ability - match ability entry and check is_hidden at the same object level
                _fetchedAbilityName = "";
                var abilityEntries = Regex.Matches(json,
                    @"\""ability\""\s*:\s*\{[^}]*\""name\""\s*:\s*\""([^\""]+)\""[^}]*\}\s*,\s*\""is_hidden\""\s*:\s*(true|false)",
                    RegexOptions.Singleline);
                foreach (Match aMatch in abilityEntries)
                {
                    if (aMatch.Groups[2].Value == "false")
                    {
                        _fetchedAbilityName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(aMatch.Groups[1].Value);
                        break;
                    }
                }
                if (string.IsNullOrEmpty(_fetchedAbilityName))
                {
                    // Fallback: pick any ability if none explicitly marked non-hidden
                    var fallbackMatch = Regex.Match(json, @"\""ability\""\s*:\s*\{[^}]*\""name\""\s*:\s*\""([^\""]+)\""");
                    if (fallbackMatch.Success)
                        _fetchedAbilityName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fallbackMatch.Groups[1].Value);
                }

                ParseMovesJson(json);
            }
            else if (_currentType == ImportType.Move)
            {
                _power = GetRootJsonIntValue(json, "power");
                _accuracy = GetRootJsonIntValue(json, "accuracy");
                _pp = GetRootJsonIntValue(json, "pp");
                _priority = GetRootJsonIntValue(json, "priority");

                var typeMatch = Regex.Match(json, @"\""type\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
                if (typeMatch.Success) _moveType = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeMatch.Groups[1].Value);

                var dmgClassMatch = Regex.Match(json, @"\""damage_class\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
                if (dmgClassMatch.Success) _moveCategory = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dmgClassMatch.Groups[1].Value);

                var effectMatch = Regex.Match(json, @"\""effect\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""");
                if (effectMatch.Success)
                {
                    _fetchedDesc = effectMatch.Groups[1].Value.Replace("\\n", " ").Replace("$effect_chance%", "");
                }
            }
            else if (_currentType == ImportType.Item)
            {
                _price = GetRootJsonIntValue(json, "cost");

                var effectMatch = Regex.Match(json, @"\""text\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""");
                if (effectMatch.Success)
                {
                    _fetchedDesc = effectMatch.Groups[1].Value.Replace("\\n", " ");
                }
            }
            else if (_currentType == ImportType.Ability)
            {
                _abilityNameId = _fileName;

                var effectMatch = Regex.Match(json, @"\""short_effect\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""");
                if (!effectMatch.Success)
                {
                    effectMatch = Regex.Match(json, @"\""effect\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""");
                }
                if (effectMatch.Success)
                {
                    _fetchedDesc = effectMatch.Groups[1].Value.Replace("\\n", " ");
                }
            }
        }

        private void ParseSpeciesJson(string json)
        {
            _catchRate = GetRootJsonIntValue(json, "capture_rate");

            var genMatch = Regex.Match(json, @"\""generation\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            if (genMatch.Success)
            {
                _pokemonGenRoman = genMatch.Groups[1].Value;
            }

            // Parse Gender info
            _hasGenderDifferences = GetRootJsonValue(json, "has_gender_differences") == "true";
            int genderRate = GetRootJsonIntValue(json, "gender_rate");
            if (genderRate == -1)
            {
                _isGenderless = true;
                _maleRatio = -1f;
            }
            else
            {
                _isGenderless = false;
                _maleRatio = (8 - genderRate) / 8f;
            }

            // Parse Growth rate name
            var growthMatch = Regex.Match(json, @"\""growth_rate\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            if (growthMatch.Success)
            {
                _growthRateName = growthMatch.Groups[1].Value;
            }

            var matches = Regex.Matches(json, @"\""flavor_text\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""[^\}]*\}\s*,\s*\""version\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            _versionDescriptions.Clear();
            var addedVersions = new HashSet<string>();

            foreach (Match m in matches)
            {
                string text = m.Groups[1].Value.Replace("\\n", " ").Replace("\\f", " ").Replace("  ", " ").Trim();
                string ver = m.Groups[2].Value.Replace("-", " ").Trim();

                if (addedVersions.Add(ver))
                {
                    _versionDescriptions.Add(new VersionDescription { version = ver, text = text });
                }
            }

            if (_versionDescriptions.Count > 0)
            {
                _selectedVersionIndex = 0;
                _fetchedDesc = _versionDescriptions[0].text;
            }
            else
            {
                var match = Regex.Match(json, @"\""flavor_text\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""");
                if (match.Success)
                {
                    _fetchedDesc = match.Groups[1].Value.Replace("\\n", " ").Replace("\\f", " ");
                }
            }
        }

        private void ParseEvolutionChainJson(string json)
        {
            _parsedEvolutions.Clear();

            // Use fetched name (lowercase) since _searchId might be a numeric Pokedex ID
            string pokemonNameLower = _fetchedName.ToLower().Replace(" ", "-");
            string searchKey = "\"name\":\"" + pokemonNameLower + "\"";
            int idx = json.IndexOf(searchKey);
            if (idx < 0)
            {
                // Fallback: try _searchId directly for name-based searches
                searchKey = "\"name\":\"" + _searchId.ToLower() + "\"";
                idx = json.IndexOf(searchKey);
            }

            if (idx >= 0)
            {
                // Find the evolves_to array following this species entry
                int evolvesIdx = json.IndexOf("\"evolves_to\":", idx);
                if (evolvesIdx >= 0)
                {
                    int arrayStart = json.IndexOf('[', evolvesIdx);
                    if (arrayStart >= 0)
                    {
                        int arrayEnd = FindMatchingBracket(json, arrayStart);
                        if (arrayEnd > arrayStart + 1)
                        {
                            string arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1).Trim();
                            if (!string.IsNullOrEmpty(arrayContent))
                                ParseTopLevelEvolutionObjects(arrayContent);
                        }
                    }
                }
            }

            // Build full chain string from all species names in the JSON
            var chainMatches = Regex.Matches(json, @"\""species\""\s*:\s*\{[^}]*\""name\""\s*:\s*\""([^\""]+)\""");
            var uniqueSpecies = new List<string>();
            foreach (Match m in chainMatches)
            {
                string name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.Groups[1].Value);
                if (!uniqueSpecies.Contains(name)) uniqueSpecies.Add(name);
            }
            _evolutionChainString = string.Join(" -> ", uniqueSpecies);
        }

        // Parses all top-level evolution objects in the evolves_to array, supporting branched evolutions (e.g. Eevee).
        private void ParseTopLevelEvolutionObjects(string arrayContent)
        {
            int i = 0;
            while (i < arrayContent.Length)
            {
                int objStart = arrayContent.IndexOf('{', i);
                if (objStart < 0) break;

                int objEnd = FindMatchingBrace(arrayContent, objStart);
                if (objEnd < 0) break;

                string entry = arrayContent.Substring(objStart, objEnd - objStart + 1);

                var speciesMatch = Regex.Match(entry, @"\""species\""\s*:\s*\{[^}]*\""name\""\s*:\s*\""([^\""]+)\""");
                if (speciesMatch.Success)
                {
                    string targetName = speciesMatch.Groups[1].Value;
                    int minLevel = 0;
                    string timeOfDay = "";
                    string itemName = "";

                    var lvMatch = Regex.Match(entry, @"\""min_level\""\s*:\s*([0-9]+)");
                    if (lvMatch.Success) int.TryParse(lvMatch.Groups[1].Value, out minLevel);

                    var timeMatch = Regex.Match(entry, @"\""time_of_day\""\s*:\s*\""([^\""]+)\""");
                    if (timeMatch.Success && !string.IsNullOrEmpty(timeMatch.Groups[1].Value))
                        timeOfDay = timeMatch.Groups[1].Value;

                    var itemMatch = Regex.Match(entry, @"\""item\""\s*:\s*\{[^}]*\""name\""\s*:\s*\""([^\""]+)\""");
                    if (itemMatch.Success) itemName = itemMatch.Groups[1].Value;

                    _parsedEvolutions.Add(new ParsedEvolution
                    {
                        targetName = targetName,
                        minLevel = minLevel,
                        timeOfDay = timeOfDay,
                        itemName = itemName
                    });
                }

                i = objEnd + 1;
            }
        }

        private void ParseEncountersJson(string json)
        {
            _fetchedLocations.Clear();
            var matches = Regex.Matches(json, @"\""location_area\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            var uniqueLocs = new HashSet<string>();
            
            foreach (Match m in matches)
            {
                string raw = m.Groups[1].Value.Replace("-", " ");
                string formatted = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(raw);
                if (uniqueLocs.Add(formatted))
                {
                    _fetchedLocations.Add(formatted);
                }
            }
        }

        private void ParseMovesJson(string json)
        {
            _parsedMoves.Clear();
            _parsedTmMoves.Clear();
            int movesStart = json.IndexOf("\"moves\":");
            if (movesStart < 0) return;

            string movesData = json.Substring(movesStart);

            // 1. Level up moves parser
            var lvlMatches = Regex.Matches(movesData, @"\{\""move\"":\{\""name\""\s*:\s*\""([^\""]+)\"".*?\""level_learned_at\""\s*:\s*([0-9]+)\s*,\s*\""move_learn_method\""\s*:\s*\{\""name\""\s*:\s*\""level-up\""");
            var uniqueLvlMoves = new Dictionary<string, int>();

            foreach (Match m in lvlMatches)
            {
                string mName = m.Groups[1].Value;
                int level = int.Parse(m.Groups[2].Value);
                if (level == 0) level = 1;

                if (!uniqueLvlMoves.ContainsKey(mName))
                {
                    uniqueLvlMoves[mName] = level;
                }
                else
                {
                    if (level < uniqueLvlMoves[mName])
                        uniqueLvlMoves[mName] = level;
                }
            }

            foreach (var kv in uniqueLvlMoves.OrderBy(kv => kv.Value))
            {
                _parsedMoves.Add(new ParsedMove { moveName = kv.Key, level = kv.Value });
            }

            // 2. TM/HM moves parser
            var tmMatches = Regex.Matches(movesData, @"\{\""move\"":\{\""name\""\s*:\s*\""([^\""]+)\"".*?\""move_learn_method\""\s*:\s*\{\""name\""\s*:\s*\""machine\""");
            var tmSet = new HashSet<string>();
            foreach (Match m in tmMatches)
            {
                string mName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.Groups[1].Value.Replace("-", " "));
                tmSet.Add(mName);
            }
            _parsedTmMoves = tmSet.OrderBy(n => n).ToList();
        }

        // =========================================================
        //  JSON Depth Helper (Corrected to match opening quotes)
        // =========================================================
        private string GetRootJsonValue(string json, string key)
        {
            int braceLevel = 0;
            int bracketLevel = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                    if (inString && braceLevel == 1 && bracketLevel == 0)
                    {
                        string matchKey = $"\"{key}\"";
                        if (i + matchKey.Length <= json.Length && json.Substring(i, matchKey.Length) == matchKey)
                        {
                            int valStart = i + matchKey.Length;
                            while (valStart < json.Length && json[valStart] != ':') valStart++;
                            valStart++;
                            while (valStart < json.Length && char.IsWhiteSpace(json[valStart])) valStart++;

                            if (valStart < json.Length)
                            {
                                if (json[valStart] == '"')
                                {
                                    int valEnd = valStart + 1;
                                    while (valEnd < json.Length && (json[valEnd] != '"' || json[valEnd - 1] == '\\')) valEnd++;
                                    return json.Substring(valStart + 1, valEnd - valStart - 1);
                                }
                                else
                                {
                                    int valEnd = valStart;
                                    while (valEnd < json.Length && json[valEnd] != ',' && json[valEnd] != '}' && json[valEnd] != ']') valEnd++;
                                    return json.Substring(valStart, valEnd - valStart).Trim();
                                }
                            }
                        }
                    }
                }

                if (!inString)
                {
                    if (c == '{') braceLevel++;
                    else if (c == '}') braceLevel--;
                    else if (c == '[') bracketLevel++;
                    else if (c == ']') bracketLevel--;
                }
            }
            return "";
        }

        private int GetRootJsonIntValue(string json, string key)
        {
            string val = GetRootJsonValue(json, key);
            int.TryParse(val, out int res);
            return res;
        }

        // Finds the matching ']' for the '[' at openIdx, correctly skipping nested structures and strings.
        private int FindMatchingBracket(string text, int openIdx)
        {
            int depth = 0;
            bool inString = false;
            for (int i = openIdx; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"' && (i == 0 || text[i - 1] != '\\')) inString = !inString;
                if (!inString)
                {
                    if (c == '[') depth++;
                    else if (c == ']') { depth--; if (depth == 0) return i; }
                }
            }
            return -1;
        }

        // Finds the matching '}' for the '{' at openIdx, correctly skipping nested structures and strings.
        private int FindMatchingBrace(string text, int openIdx)
        {
            int depth = 0;
            bool inString = false;
            for (int i = openIdx; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"' && (i == 0 || text[i - 1] != '\\')) inString = !inString;
                if (!inString)
                {
                    if (c == '{') depth++;
                    else if (c == '}') { depth--; if (depth == 0) return i; }
                }
            }
            return -1;
        }

        // =========================================================
        //  Search Autocomplete Database Caching
        // =========================================================

        private string GetCacheFolderPath() => Path.Combine("Assets", "Editor", "SOTreeTool", "Cache");

        private void LoadOrDownloadSearchDatabase()
        {
            string folder = GetCacheFolderPath();
            string pPath = Path.Combine(folder, "pokemon_names.txt");
            string mPath = Path.Combine(folder, "move_names.txt");
            string iPath = Path.Combine(folder, "item_names.txt");
            string aPath = Path.Combine(folder, "ability_names.txt");

            if (File.Exists(pPath) && File.Exists(mPath) && File.Exists(iPath) && File.Exists(aPath))
            {
                _pokemonSuggestions = File.ReadAllLines(pPath).ToList();
                _moveSuggestions = File.ReadAllLines(mPath).ToList();
                _itemSuggestions = File.ReadAllLines(iPath).ToList();
                _abilitySuggestions = File.ReadAllLines(aPath).ToList();
                _dbLoaded = true;
                UpdateMatches();
            }
            else
            {
                DownloadSearchDatabase();
            }
        }

        private void DownloadSearchDatabase()
        {
            if (_isDownloadingDb) return;
            _isDownloadingDb = true;
            _status = "Downloading suggestions database...";
            EditorCoroutineUtility.StartCoroutine(DownloadDbRoutine(), this);
        }

        private IEnumerator DownloadDbRoutine()
        {
            string folder = GetCacheFolderPath();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // 1. Fetch Pokemon names
            _status = "Downloading Pokemon index...";
            using (UnityWebRequest req = UnityWebRequest.Get("https://pokeapi.co/api/v2/pokemon?limit=1500"))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var names = ParseNamesFromListJson(req.downloadHandler.text);
                    if (names.Count > 0)
                    {
                        File.WriteAllLines(Path.Combine(folder, "pokemon_names.txt"), names);
                        _pokemonSuggestions = names;
                    }
                }
            }

            // 2. Fetch Move names
            _status = "Downloading Moves index...";
            using (UnityWebRequest req = UnityWebRequest.Get("https://pokeapi.co/api/v2/move?limit=1000"))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var names = ParseNamesFromListJson(req.downloadHandler.text);
                    if (names.Count > 0)
                    {
                        File.WriteAllLines(Path.Combine(folder, "move_names.txt"), names);
                        _moveSuggestions = names;
                    }
                }
            }

            // 3. Fetch Item names
            _status = "Downloading Items index...";
            using (UnityWebRequest req = UnityWebRequest.Get("https://pokeapi.co/api/v2/item?limit=2100"))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var names = ParseNamesFromListJson(req.downloadHandler.text);
                    if (names.Count > 0)
                    {
                        File.WriteAllLines(Path.Combine(folder, "item_names.txt"), names);
                        _itemSuggestions = names;
                    }
                }
            }

            // 4. Fetch Ability names
            _status = "Downloading Abilities index...";
            using (UnityWebRequest req = UnityWebRequest.Get("https://pokeapi.co/api/v2/ability?limit=400"))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var names = ParseNamesFromListJson(req.downloadHandler.text);
                    if (names.Count > 0)
                    {
                        File.WriteAllLines(Path.Combine(folder, "ability_names.txt"), names);
                        _abilitySuggestions = names;
                    }
                }
            }

            _dbLoaded = true;
            _isDownloadingDb = false;
            _status = "Suggestions database successfully cached.";
            UpdateMatches();
            Repaint();
        }

        private List<string> ParseNamesFromListJson(string json)
        {
            var list = new List<string>();
            var matches = Regex.Matches(json, @"\""name\""\s*:\s*\""([^\""]+)""");
            foreach (Match m in matches)
            {
                string val = m.Groups[1].Value;
                if (!val.Contains("/") && !val.Contains("?"))
                {
                    list.Add(val);
                }
            }
            return list;
        }

        // =========================================================
        //  Doomsday / Mass Importer Coroutine
        // =========================================================
        private void StartDoomsdayImport()
        {
            _isDoomsdayRunning = true;
            EditorCoroutineUtility.StartCoroutine(DoomsdayImportRoutine(), this);
        }

        private IEnumerator DoomsdayImportRoutine()
        {
            int total = _doomsdayEnd - _doomsdayStart + 1;
            if (total <= 0)
            {
                _isDoomsdayRunning = false;
                yield break;
            }

            for (int id = _doomsdayStart; id <= _doomsdayEnd; id++)
            {
                float progress = (float)(id - _doomsdayStart) / total;
                if (EditorUtility.DisplayCancelableProgressBar("Doomsday Import", $"Fetching and updating Pokemon #{id} of {_doomsdayEnd}...", progress))
                {
                    break;
                }

                // Execute single Pokemon import sequence in-place
                string url = "https://pokeapi.co/api/v2/pokemon/" + id;
                _searchId = id.ToString();
                
                _versionDescriptions.Clear();
                _parsedEvolutions.Clear();
                _parsedMoves.Clear();
                _parsedTmMoves.Clear();
                _fetchedLocations.Clear();
                _evolutionChainString = "";

                // Web requests
                string pokemonJson = "";
                using (UnityWebRequest webReq = UnityWebRequest.Get(url))
                {
                    yield return webReq.SendWebRequest();
                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        pokemonJson = webReq.downloadHandler.text;
                    }
                }

                if (string.IsNullOrEmpty(pokemonJson)) continue;

                ParseJson(pokemonJson);

                string speciesJson = "";
                string speciesUrl = "https://pokeapi.co/api/v2/pokemon-species/" + id;
                using (UnityWebRequest webReq = UnityWebRequest.Get(speciesUrl))
                {
                    yield return webReq.SendWebRequest();
                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        speciesJson = webReq.downloadHandler.text;
                        ParseSpeciesJson(speciesJson);
                    }
                }

                var matchEvo = Regex.Match(speciesJson, @"\""evolution_chain\""\s*:\s*\{\s*\""url\""\s*:\s*\""([^\""]+)""");
                if (matchEvo.Success)
                {
                    using (UnityWebRequest webReq = UnityWebRequest.Get(matchEvo.Groups[1].Value))
                    {
                        yield return webReq.SendWebRequest();
                        if (webReq.result == UnityWebRequest.Result.Success)
                        {
                            ParseEvolutionChainJson(webReq.downloadHandler.text);
                        }
                    }
                }

                string encountersUrl = "https://pokeapi.co/api/v2/pokemon/" + id + "/encounters";
                using (UnityWebRequest webReq = UnityWebRequest.Get(encountersUrl))
                {
                    yield return webReq.SendWebRequest();
                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        ParseEncountersJson(webReq.downloadHandler.text);
                    }
                }

                // Resolve dependencies & Save/Update
                UpdateDefaultFolder();
                yield return GenerateAssetWithDepsRoutine();
            }

            EditorUtility.ClearProgressBar();
            _isDoomsdayRunning = false;
            _status = "Doomsday Mass Import complete.";
            Repaint();
            EditorUtility.DisplayDialog("Success", "Doomsday Mass Import completed successfully!", "OK");
        }

        // =========================================================
        //  Asset Generation & Dependency Resolution (With In-place updates)
        // =========================================================
        private void GenerateAsset()
        {
            EditorCoroutineUtility.StartCoroutine(GenerateSingleAssetRoutine(), this);
        }

        private IEnumerator GenerateSingleAssetRoutine()
        {
            _isFetching = true;
            yield return GenerateAssetWithDepsRoutine();
            _isFetching = false;
            Repaint();
            EditorUtility.DisplayDialog("Success", "Asset generated / updated successfully!", "OK");
        }

        private IEnumerator GenerateAssetWithDepsRoutine()
        {
            _status = "Resolving referenced SO assets...";
            Repaint();

            var evolutionList = new List<Evolution>();

            // 1. Resolve forward evolution assets recursively if not created
            foreach (var evoData in _parsedEvolutions)
            {
                PokemonBase evoAsset = null;
                yield return ResolvePokemonAssetRoutine(evoData.targetName, asset => evoAsset = asset);

                if (evoAsset != null)
                {
                    var evo = new Evolution();

                    var evoType = typeof(Evolution);
                    evoType.GetField("evolvesInto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(evo, evoAsset);
                    evoType.GetField("requiredLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(evo, evoData.minLevel);

                    GeneralDayPeriod timeEnum = GeneralDayPeriod.None;
                    if (evoData.timeOfDay == "day") timeEnum = GeneralDayPeriod.Day;
                    else if (evoData.timeOfDay == "night") timeEnum = GeneralDayPeriod.Night;

                    evoType.GetField("requiredTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(evo, timeEnum);

                    if (!string.IsNullOrEmpty(evoData.itemName))
                    {
                        EvolutionItem itemAsset = null;
                        yield return ResolveItemAssetRoutine(evoData.itemName, item => itemAsset = item);
                        if (itemAsset != null)
                        {
                            evoType.GetField("requiredItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?.SetValue(evo, itemAsset);
                        }
                    }

                    evolutionList.Add(evo);
                }
            }

            // 2. Resolve learnable moves dependencies (Level up)
            var finalMovesList = new List<LearnableMove>();
            foreach (var mData in _parsedMoves)
            {
                MoveBase moveAsset = null;
                yield return ResolveMoveAssetRoutine(mData.moveName, asset => moveAsset = asset);

                if (moveAsset != null)
                {
                    var lm = new LearnableMove();
                    var lmType = typeof(LearnableMove);
                    lmType.GetField("moveBase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(lm, moveAsset);
                    lmType.GetField("level", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(lm, mData.level);

                    finalMovesList.Add(lm);
                }
            }

            // 3. Resolve learnable moves by TM dependencies
            var finalTmMovesList = new List<MoveBase>();
            foreach (var tmName in _parsedTmMoves)
            {
                MoveBase tmAsset = null;
                string cleanName = tmName.Replace(" ", "-").ToLower();
                yield return ResolveMoveAssetRoutine(cleanName, asset => tmAsset = asset);
                if (tmAsset != null)
                {
                    finalTmMovesList.Add(tmAsset);
                }
            }

            // Create target folders
            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }

            string fullPath = Path.Combine(_saveFolder, _fileName + ".asset");
            ScriptableObject instance = null;

            if (_currentType == ImportType.Pokemon)
            {
                // In-place update check
                PokemonBase existing = AssetDatabase.LoadAssetAtPath<PokemonBase>(fullPath);
                bool isNew = (existing == null);
                if (isNew)
                {
                    instance = CreateInstance<PokemonBase>();
                }
                else
                {
                    instance = existing;
                }

                var so = new SerializedObject(instance);
                so.Update();

                so.FindProperty("_name").stringValue = _fetchedName;
                so.FindProperty("description").stringValue = _fetchedDesc;
                so.FindProperty("baseHeight").floatValue = _height;
                so.FindProperty("baseWeight").floatValue = _weight;
                so.FindProperty("catchRate").intValue = _catchRate;
                so.FindProperty("xpYield").intValue = _xpYield;
                so.FindProperty("maxHp").intValue = _hp;
                so.FindProperty("attack").intValue = _atk;
                so.FindProperty("defense").intValue = _def;
                so.FindProperty("spAttack").intValue = _spAtk;
                so.FindProperty("spDefense").intValue = _spDef;
                so.FindProperty("speed").intValue = _spd;

                // EV Yields
                so.FindProperty("hitPointsEvYield").intValue = _evHp;
                so.FindProperty("attackEvYield").intValue = _evAtk;
                so.FindProperty("defenseEvYield").intValue = _evDef;
                so.FindProperty("spAttackEvYield").intValue = _evSpA;
                so.FindProperty("spDefenseEvYield").intValue = _evSpD;
                so.FindProperty("speedEvYield").intValue = _evSpd;

                // Gender & Growth Properties
                so.FindProperty("isGenderless").boolValue = _isGenderless;
                so.FindProperty("hasGenderDifferences").boolValue = _hasGenderDifferences;
                so.FindProperty("maleRatio").floatValue = _maleRatio;
                SetGrowthRatePropertyValue(so.FindProperty("growthRate"), _growthRateName);

                // Types
                SetTypePropertyValue(so.FindProperty("type1"), _type1);
                SetTypePropertyValue(so.FindProperty("type2"), _type2);

                // Map default Ability enum
                var abilityProp = so.FindProperty("ability");
                if (abilityProp != null && !string.IsNullOrEmpty(_fetchedAbilityName))
                {
                    string[] enumNames = System.Enum.GetNames(typeof(AbilityID));
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        if (enumNames[i].ToLower() == _fetchedAbilityName.ToLower())
                        {
                            abilityProp.enumValueIndex = i;
                            break;
                        }
                    }
                }

                // Assign Local Normal & Shiny & Female Sprites (Grid organized)
                if (_localFrontSprite != null) so.FindProperty("frontSprite").objectReferenceValue = _localFrontSprite;
                if (_localBackSprite != null) so.FindProperty("backSprite").objectReferenceValue = _localBackSprite;
                if (_localIconSprite != null) so.FindProperty("iconSprite").objectReferenceValue = _localIconSprite;
                
                if (_localFemaleFrontSprite != null) so.FindProperty("femaleFrontSprite").objectReferenceValue = _localFemaleFrontSprite;
                if (_localFemaleBackSprite != null) so.FindProperty("femaleBackSprite").objectReferenceValue = _localFemaleBackSprite;
                
                if (_localShinyFrontSprite != null) so.FindProperty("shinyFrontSprite").objectReferenceValue = _localShinyFrontSprite;
                if (_localShinyBackSprite != null) so.FindProperty("shinyBackSprite").objectReferenceValue = _localShinyBackSprite;
                if (_localShinyFemaleFrontSprite != null) so.FindProperty("shinyFemaleFrontSprite").objectReferenceValue = _localShinyFemaleFrontSprite;
                if (_localShinyFemaleBackSprite != null) so.FindProperty("shinyFemaleBackSprite").objectReferenceValue = _localShinyFemaleBackSprite;
                if (_localShinyIconSprite != null) so.FindProperty("shinyIconSprite").objectReferenceValue = _localShinyIconSprite;

                // Write string locations list
                var locProp = so.FindProperty("encounterLocations");
                if (locProp != null)
                {
                    locProp.ClearArray();
                    for (int i = 0; i < _fetchedLocations.Count; i++)
                    {
                        locProp.InsertArrayElementAtIndex(i);
                        locProp.GetArrayElementAtIndex(i).stringValue = _fetchedLocations[i];
                    }
                }

                so.ApplyModifiedProperties();

                // Apply lists via reflection since they are private lists
                typeof(PokemonBase).GetField("evolutions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(instance, evolutionList);

                typeof(PokemonBase).GetField("learnableMovesLevelUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(instance, finalMovesList);

                typeof(PokemonBase).GetField("learnableMovesByTm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(instance, finalTmMovesList);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(instance, fullPath);
                }
                else
                {
                    EditorUtility.SetDirty(instance);
                }
            }
            else if (_currentType == ImportType.Move)
            {
                MoveBase existing = AssetDatabase.LoadAssetAtPath<MoveBase>(fullPath);
                bool isNew = (existing == null);
                if (isNew) instance = CreateInstance<MoveBase>();
                else instance = existing;

                var oSo = new SerializedObject(instance);
                oSo.Update();

                oSo.FindProperty("_name").stringValue = _fetchedName;
                oSo.FindProperty("description").stringValue = _fetchedDesc;
                oSo.FindProperty("power").intValue = _power;
                oSo.FindProperty("accuracy").intValue = _accuracy;
                oSo.FindProperty("pp").intValue = _pp;
                oSo.FindProperty("priority").intValue = _priority;

                SetTypePropertyValue(oSo.FindProperty("type"), _moveType);
                SetCategoryPropertyValue(oSo.FindProperty("category"), _moveCategory);

                oSo.ApplyModifiedProperties();

                if (isNew) AssetDatabase.CreateAsset(instance, fullPath);
                else EditorUtility.SetDirty(instance);
            }
            else if (_currentType == ImportType.Item)
            {
                ItemBase existing = AssetDatabase.LoadAssetAtPath<ItemBase>(fullPath);
                bool isNew = (existing == null);
                if (isNew) instance = CreateInstance<ItemBase>();
                else instance = existing;

                var oSo = new SerializedObject(instance);
                oSo.Update();

                oSo.FindProperty("_name").stringValue = _fetchedName;
                oSo.FindProperty("description").stringValue = _fetchedDesc;
                oSo.FindProperty("price").floatValue = _price;
                oSo.FindProperty("isSellable").boolValue = (_price > 0);

                SetItemTypePropertyValue(oSo.FindProperty("itemType"), _itemCategory);

                oSo.ApplyModifiedProperties();

                if (isNew) AssetDatabase.CreateAsset(instance, fullPath);
                else EditorUtility.SetDirty(instance);
            }
            else if (_currentType == ImportType.Ability)
            {
                AbilityBase existing = AssetDatabase.LoadAssetAtPath<AbilityBase>(fullPath);
                bool isNew = (existing == null);
                if (isNew) instance = CreateInstance<AbilityBase>();
                else instance = existing;

                var oSo = new SerializedObject(instance);
                oSo.Update();

                oSo.FindProperty("_name").stringValue = _fetchedName;
                oSo.FindProperty("description").stringValue = _fetchedDesc;

                var idProp = oSo.FindProperty("abilityId");
                if (idProp != null)
                {
                    string[] enumNames = System.Enum.GetNames(typeof(AbilityID));
                    int enumIdx = System.Array.IndexOf(enumNames, _fileName);
                    if (enumIdx >= 0)
                    {
                        idProp.enumValueIndex = enumIdx;
                    }
                    else
                    {
                        for (int i = 0; i < enumNames.Length; i++)
                        {
                            if (enumNames[i].ToLower() == _fileName.ToLower())
                            {
                                idProp.enumValueIndex = i;
                                break;
                            }
                        }
                    }
                }

                oSo.ApplyModifiedProperties();

                if (isNew) AssetDatabase.CreateAsset(instance, fullPath);
                else EditorUtility.SetDirty(instance);
            }

            if (instance != null)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                _status = $"Updated: {_fileName}";
            }
        }

        private IEnumerator ResolvePokemonAssetRoutine(string pName, System.Action<PokemonBase> callback)
        {
            pName = pName.ToLower().Trim();
            string searchName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pName).Replace("-", "");

            string guid = AssetDatabase.FindAssets($"t:PokemonBase {searchName}").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                callback(AssetDatabase.LoadAssetAtPath<PokemonBase>(path));
                yield break;
            }

            _status = $"Downloading dependency Pokemon: {pName}...";
            string url = $"https://pokeapi.co/api/v2/pokemon/{pName}";
            string mainJson = "";
            using (UnityWebRequest webReq = UnityWebRequest.Get(url))
            {
                yield return webReq.SendWebRequest();
                if (webReq.result == UnityWebRequest.Result.Success)
                {
                    mainJson = webReq.downloadHandler.text;
                }
            }

            if (string.IsNullOrEmpty(mainJson))
            {
                callback(null);
                yield break;
            }

            string speciesJson = "";
            string speciesUrl = $"https://pokeapi.co/api/v2/pokemon-species/{pName}";
            using (UnityWebRequest webReq = UnityWebRequest.Get(speciesUrl))
            {
                yield return webReq.SendWebRequest();
                if (webReq.result == UnityWebRequest.Result.Success)
                {
                    speciesJson = webReq.downloadHandler.text;
                }
            }

            string dispName = GetRootJsonValue(mainJson, "name");
            dispName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dispName);
            string fName = dispName.Replace("-", "");

            float pWeight = GetRootJsonIntValue(mainJson, "weight") / 10f;
            float pHeight = GetRootJsonIntValue(mainJson, "height") / 10f;
            int pXp = GetRootJsonIntValue(mainJson, "base_experience");

            int pHp = 10, pAtk = 10, pDef = 10, pSpA = 10, pSpD = 10, pSpd = 10;
            int pEvHp = 0, pEvAtk = 0, pEvDef = 0, pEvSpA = 0, pEvSpD = 0, pEvSpd = 0;

            var statMatches = Regex.Matches(mainJson, @"\""base_stat\""\s*:\s*([0-9]+)\s*,\s*\""effort\""\s*:\s*([0-9]+)\s*,\s*\""stat\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            foreach (Match m in statMatches)
            {
                int val = int.Parse(m.Groups[1].Value);
                int evVal = int.Parse(m.Groups[2].Value);
                string sName = m.Groups[3].Value;

                if (sName == "hp") { pHp = val; pEvHp = evVal; }
                else if (sName == "attack") { pAtk = val; pEvAtk = evVal; }
                else if (sName == "defense") { pDef = val; pEvDef = evVal; }
                else if (sName == "special-attack") { pSpA = val; pEvSpA = evVal; }
                else if (sName == "special-defense") { pSpD = val; pEvSpD = evVal; }
                else if (sName == "speed") { pSpd = val; pEvSpd = evVal; }
            }

            string pT1 = "None", pT2 = "None";
            var typeMatches = Regex.Matches(mainJson, @"\""type\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            if (typeMatches.Count > 0) pT1 = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeMatches[0].Groups[1].Value);
            if (typeMatches.Count > 1) pT2 = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeMatches[1].Groups[1].Value);

            int pCatch = 255;
            string pDesc = "";
            string genName = "generation-i";
            bool pGenderless = false;
            float pMaleRatio = 0.5f;
            bool pHasDifferences = false;
            string pGrowthRate = "medium";

            if (!string.IsNullOrEmpty(speciesJson))
            {
                pCatch = GetRootJsonIntValue(speciesJson, "capture_rate");
                var genMatch = Regex.Match(speciesJson, @"\""generation\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
                if (genMatch.Success) genName = genMatch.Groups[1].Value;

                pHasDifferences = GetRootJsonValue(speciesJson, "has_gender_differences") == "true";
                int genderRate = GetRootJsonIntValue(speciesJson, "gender_rate");
                if (genderRate == -1)
                {
                    pGenderless = true;
                    pMaleRatio = -1f;
                }
                else
                {
                    pGenderless = false;
                    pMaleRatio = (8 - genderRate) / 8f;
                }

                var grMatch = Regex.Match(speciesJson, @"\""growth_rate\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
                if (grMatch.Success) pGrowthRate = grMatch.Groups[1].Value;

                var descMatches = Regex.Matches(speciesJson, @"\""flavor_text\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""[^\}]*\}\s*,\s*\""version\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
                if (descMatches.Count > 0)
                {
                    pDesc = descMatches[0].Groups[1].Value.Replace("\\n", " ").Replace("\\f", " ").Replace("  ", " ").Trim();
                }
            }

            string genFolder = MapGenerationToFolder(genName);
            if (!Directory.Exists(genFolder)) Directory.CreateDirectory(genFolder);

            string assetPath = Path.Combine(genFolder, fName + ".asset");

            // In-place update check
            PokemonBase instance = AssetDatabase.LoadAssetAtPath<PokemonBase>(assetPath);
            bool isNew = (instance == null);
            if (isNew) instance = CreateInstance<PokemonBase>();

            var so = new SerializedObject(instance);
            so.Update();

            so.FindProperty("_name").stringValue = dispName;
            so.FindProperty("description").stringValue = pDesc;
            so.FindProperty("baseHeight").floatValue = pHeight;
            so.FindProperty("baseWeight").floatValue = pWeight;
            so.FindProperty("catchRate").intValue = pCatch;
            so.FindProperty("xpYield").intValue = pXp;
            so.FindProperty("maxHp").intValue = pHp;
            so.FindProperty("attack").intValue = pAtk;
            so.FindProperty("defense").intValue = pDef;
            so.FindProperty("spAttack").intValue = pSpA;
            so.FindProperty("spDefense").intValue = pSpD;
            so.FindProperty("speed").intValue = pSpd;

            so.FindProperty("hitPointsEvYield").intValue = pEvHp;
            so.FindProperty("attackEvYield").intValue = pEvAtk;
            so.FindProperty("defenseEvYield").intValue = pEvDef;
            so.FindProperty("spAttackEvYield").intValue = pEvSpA;
            so.FindProperty("spDefenseEvYield").intValue = pEvSpD;
            so.FindProperty("speedEvYield").intValue = pEvSpd;

            so.FindProperty("isGenderless").boolValue = pGenderless;
            so.FindProperty("hasGenderDifferences").boolValue = pHasDifferences;
            so.FindProperty("maleRatio").floatValue = pMaleRatio;
            SetGrowthRatePropertyValue(so.FindProperty("growthRate"), pGrowthRate);

            SetTypePropertyValue(so.FindProperty("type1"), pT1);
            SetTypePropertyValue(so.FindProperty("type2"), pT2);

            so.ApplyModifiedProperties();

            if (isNew) AssetDatabase.CreateAsset(instance, assetPath);
            else EditorUtility.SetDirty(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            callback(instance);
        }

        private IEnumerator ResolveItemAssetRoutine(string itemName, System.Action<EvolutionItem> callback)
        {
            itemName = itemName.ToLower().Trim();
            string searchName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(itemName).Replace("-", "");

            string guid = AssetDatabase.FindAssets($"t:EvolutionItem {searchName}").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                callback(AssetDatabase.LoadAssetAtPath<EvolutionItem>(path));
                yield break;
            }

            _status = $"Downloading dependency Item: {itemName}...";
            string url = $"https://pokeapi.co/api/v2/item/{itemName}";
            string itemJson = "";
            using (UnityWebRequest webReq = UnityWebRequest.Get(url))
            {
                yield return webReq.SendWebRequest();
                if (webReq.result == UnityWebRequest.Result.Success)
                {
                    itemJson = webReq.downloadHandler.text;
                }
            }

            if (string.IsNullOrEmpty(itemJson))
            {
                callback(null);
                yield break;
            }

            string dispName = GetRootJsonValue(itemJson, "name");
            dispName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dispName);
            string fName = dispName.Replace("-", "");
            int cost = GetRootJsonIntValue(itemJson, "cost");

            string itemDesc = "";
            var effectMatch = Regex.Match(itemJson, @"\""text\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""en\""");
            if (effectMatch.Success) itemDesc = effectMatch.Groups[1].Value.Replace("\\n", " ");

            string folder = "Assets/Game/Resources/Items";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string assetPath = Path.Combine(folder, fName + ".asset");

            EvolutionItem instance = AssetDatabase.LoadAssetAtPath<EvolutionItem>(assetPath);
            bool isNew = (instance == null);
            if (isNew) instance = CreateInstance<EvolutionItem>();

            var so = new SerializedObject(instance);
            so.Update();

            so.FindProperty("_name").stringValue = dispName;
            so.FindProperty("description").stringValue = itemDesc;
            so.FindProperty("price").floatValue = cost;
            so.FindProperty("isSellable").boolValue = (cost > 0);
            so.FindProperty("itemType").enumValueIndex = 4;

            so.ApplyModifiedProperties();

            if (isNew) AssetDatabase.CreateAsset(instance, assetPath);
            else EditorUtility.SetDirty(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            callback(instance);
        }

        private IEnumerator ResolveMoveAssetRoutine(string mName, System.Action<MoveBase> callback)
        {
            mName = mName.ToLower().Trim();
            string searchName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mName).Replace("-", "");

            string guid = AssetDatabase.FindAssets($"t:MoveBase {searchName}").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                callback(AssetDatabase.LoadAssetAtPath<MoveBase>(path));
                yield break;
            }

            _status = $"Downloading dependency Move: {mName}...";
            string url = $"https://pokeapi.co/api/v2/move/{mName}";
            string moveJson = "";
            using (UnityWebRequest webReq = UnityWebRequest.Get(url))
            {
                yield return webReq.SendWebRequest();
                if (webReq.result == UnityWebRequest.Result.Success)
                {
                    moveJson = webReq.downloadHandler.text;
                }
            }

            if (string.IsNullOrEmpty(moveJson))
            {
                callback(null);
                yield break;
            }

            string dispName = GetRootJsonValue(moveJson, "name");
            dispName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dispName);
            string fName = dispName.Replace("-", "");
            
            int power = GetRootJsonIntValue(moveJson, "power");
            int accuracy = GetRootJsonIntValue(moveJson, "accuracy");
            int pp = GetRootJsonIntValue(moveJson, "pp");
            int priority = GetRootJsonIntValue(moveJson, "priority");

            string mType = "None";
            var typeMatch = Regex.Match(moveJson, @"\""type\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            if (typeMatch.Success) mType = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeMatch.Groups[1].Value);

            string mCat = "Physical";
            var dmgClassMatch = Regex.Match(moveJson, @"\""damage_class\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            if (dmgClassMatch.Success) mCat = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dmgClassMatch.Groups[1].Value);

            string mDesc = "";
            var effectMatch = Regex.Match(moveJson, @"\""effect\""\s*:\s*\""([^\""]+)\""\s*,\s*\""language\""\s*:\s*\{\s*\""name\""\s*:\s*\""([^\""]+)""");
            if (effectMatch.Success) mDesc = effectMatch.Groups[1].Value.Replace("\\n", " ").Replace("$effect_chance%", "");

            string folder = "Assets/Game/Resources/Moves/" + mCat;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string assetPath = Path.Combine(folder, fName + ".asset");

            MoveBase instance = AssetDatabase.LoadAssetAtPath<MoveBase>(assetPath);
            bool isNew = (instance == null);
            if (isNew) instance = CreateInstance<MoveBase>();

            var so = new SerializedObject(instance);
            so.Update();

            so.FindProperty("_name").stringValue = dispName;
            so.FindProperty("description").stringValue = mDesc;
            so.FindProperty("power").intValue = power;
            so.FindProperty("accuracy").intValue = accuracy;
            so.FindProperty("pp").intValue = pp;
            so.FindProperty("priority").intValue = priority;

            SetTypePropertyValue(so.FindProperty("type"), mType);
            SetCategoryPropertyValue(so.FindProperty("category"), mCat);

            so.ApplyModifiedProperties();

            if (isNew) AssetDatabase.CreateAsset(instance, assetPath);
            else EditorUtility.SetDirty(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            callback(instance);
        }

        private string MapGenerationToFolder(string genRoman)
        {
            string num = "1";
            string suffix = genRoman.Replace("generation-", "").Trim().ToLower();
            switch (suffix)
            {
                case "i": num = "1"; break;
                case "ii": num = "2"; break;
                case "iii": num = "3"; break;
                case "iv": num = "4"; break;
                case "v": num = "5"; break;
                case "vi": num = "6"; break;
                case "vii": num = "7"; break;
                case "viii": num = "8"; break;
                case "ix": num = "9"; break;
            }
            return $"Assets/Game/Resources/Pokemons/Generation {num}";
        }

        private void SetTypePropertyValue(SerializedProperty prop, string typeName)
        {
            if (prop == null) return;
            string[] names = System.Enum.GetNames(typeof(PokemonType));
            int idx = System.Array.IndexOf(names, typeName);
            if (idx >= 0) prop.enumValueIndex = idx;
        }

        private void SetCategoryPropertyValue(SerializedProperty prop, string catName)
        {
            if (prop == null) return;
            string[] names = System.Enum.GetNames(typeof(MoveCategory));
            int idx = System.Array.IndexOf(names, catName);
            if (idx >= 0) prop.enumValueIndex = idx;
        }

        private void SetItemTypePropertyValue(SerializedProperty prop, string catName)
        {
            if (prop == null) return;
            prop.enumValueIndex = 0;
            // Combine fetched item name and category parameter for heuristic type detection
            string check = (_fetchedName + " " + (catName ?? "")).ToLower();
            if (check.Contains("ball")) prop.enumValueIndex = 5;
            else if (check.Contains("revive")) prop.enumValueIndex = 7;
            else if (check.Contains("potion") || check.Contains("restore") || check.Contains("heal")) prop.enumValueIndex = 0;
            else if (check.Contains("tm") || check.Contains("hm")) prop.enumValueIndex = 6;
        }

        private void SetGrowthRatePropertyValue(SerializedProperty prop, string grName)
        {
            if (prop == null) return;
            int val = 3; // Default MediumFast
            switch (grName.ToLower())
            {
                case "slow": val = 1; break;
                case "medium-slow": val = 2; break;
                case "medium":
                case "medium-fast": val = 3; break;
                case "fast": val = 4; break;
                case "slow-then-very-fast": val = 0; break;
                case "fast-then-very-slow": val = 5; break;
            }
            prop.enumValueIndex = val;
        }

        // =========================================================
        //  Editor Coroutine Runner (Robust implementation supporting nested yields)
        // =========================================================
        private static class EditorCoroutineUtility
        {
            public class EditorCoroutine
            {
                private readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();
                private readonly EditorWindow _owner;

                public EditorCoroutine(IEnumerator routine, EditorWindow owner)
                {
                    _stack.Push(routine);
                    _owner = owner;
                }

                public bool Update()
                {
                    if (_stack.Count == 0) return false;

                    IEnumerator current = _stack.Peek();

                    if (current.Current is IEnumerator nested)
                    {
                        _stack.Push(nested);
                        nested.MoveNext(); // Initialize: advance nested coroutine to its first yield point
                        return true;
                    }

                    if (current.Current is AsyncOperation op && !op.isDone)
                    {
                        return true;
                    }

                    if (!current.MoveNext())
                    {
                        _stack.Pop();
                    }

                    if (_owner != null) _owner.Repaint();
                    return _stack.Count > 0;
                }
            }

            public static IEnumerator StartCoroutine(IEnumerator routine, EditorWindow owner)
            {
                var coroutine = new EditorCoroutine(routine, owner);
                EditorApplication.CallbackFunction update = null;
                update = () =>
                {
                    if (!coroutine.Update())
                    {
                        EditorApplication.update -= update;
                    }
                };
                EditorApplication.update += update;
                return routine;
            }
        }
    }
}
