#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIMockupSceneBuilder {
    const string ScenePath = "Assets/Scenes/UI.unity";
    const string RootName = "UI_Mockup_Root";
    const string AutoBuildFlagPath = "Temp/CodexBuildUIMockupsOnce.flag";
    const string MockupSetVersion = "ui-mockups-pokenav-map-view-controller";

    static Font uiFont;

    [InitializeOnLoadMethod]
    static void RunRequestedBuildOnce() {
        if(!File.Exists(AutoBuildFlagPath)) {
            return;
        }

        EditorApplication.delayCall += () => {
            if(!File.Exists(AutoBuildFlagPath)) {
                return;
            }

            try {
                File.Delete(AutoBuildFlagPath);
                RebuildUiSceneMockups();
                Debug.Log($"UI mockup scene rebuilt ({MockupSetVersion}).");
            } catch(Exception exception) {
                Debug.LogError($"UI mockup auto-build failed: {exception.Message}");
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Tools/PokemonProject/UI Mockups/Rebuild UI Scene Mockups")]
    public static void RebuildUiSceneMockups() {
        EnsureUiSceneLoaded();
        RemoveExistingRoot();

        var rootObject = new GameObject(RootName);

        EnsureEventSystem();
        BuildIndependentMockups(rootObject.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    static void EnsureUiSceneLoaded() {
        var activeScene = SceneManager.GetActiveScene();
        if(activeScene.path == ScenePath) {
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static void RemoveExistingRoot() {
        var existing = GameObject.Find(RootName);
        if(existing != null) {
            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    static void EnsureEventSystem() {
        if(UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static void BuildIndependentMockups(Transform root) {
        var screenManager = root.gameObject.AddComponent<RuntimeUIScreenManager>();
        screenManager.ClearRegisteredScreens();
        screenManager.ConfigureStartup("MainMenu", closeAllOnAwake: true, RuntimeUIScreenInputMode.Player);

        var mainMenu = CreateMockupCanvas(root, "MainMenu_UI", 70, true);
        BuildMainMenuScreen(mainMenu);
        screenManager.RegisterScreen("MainMenu", mainMenu.gameObject);

        var newGame = CreateMockupCanvas(root, "NewGame_UI", 80, false);
        BuildNewGameScreen(newGame);
        screenManager.RegisterScreen("NewGame", newGame.gameObject);

        var gameStartRules = CreateMockupCanvas(root, "GameStartBattleRulesSetup_UI", 90, false);
        BuildGameStartBattleRulesSetupScreen(gameStartRules);
        screenManager.RegisterScreen("GameStartBattleRulesSetup", gameStartRules.gameObject);

        var pokeNav = CreateMockupCanvas(root, "PokeNav_UI", 100, false);
        BuildPokeNavScreen(pokeNav);
        screenManager.RegisterScreen("PokeNav", pokeNav.gameObject);

        var miniMap = CreateMockupCanvas(root, "MiniMap_UI", 110, false);
        BuildMiniMapScreen(miniMap);
        screenManager.RegisterScreen("MiniMap", miniMap.gameObject, closesOtherScreens: false, inputModeOnOpen: RuntimeUIScreenInputMode.Player);

        var feed = CreateMockupCanvas(root, "WorldFeed_UI", 120, false);
        BuildWorldFeedScreen(feed);
        screenManager.RegisterScreen("WorldFeed", feed.gameObject, closesOtherScreens: false, inputModeOnOpen: RuntimeUIScreenInputMode.Player);

        var dialog = CreateMockupCanvas(root, "Dialog_UI", 130, false);
        BuildDialogScreen(dialog);
        screenManager.RegisterScreen("Dialog", dialog.gameObject, closesOtherScreens: false, inputModeOnOpen: RuntimeUIScreenInputMode.UI);

        var market = CreateMockupCanvas(root, "MarketBasket_UI", 140, false);
        BuildMarketBasketScreen(market);
        screenManager.RegisterScreen("MarketBasket", market.gameObject, closesOtherScreens: false, inputModeOnOpen: RuntimeUIScreenInputMode.Player);

        var battle = CreateMockupCanvas(root, "BattleRules_UI", 150, false);
        BuildBattleRulesScreen(battle);
        screenManager.RegisterScreen("BattleRules", battle.gameObject);

        var titles = CreateMockupCanvas(root, "TitleHonor_UI", 160, false);
        BuildTitleHonorScreen(titles);
        screenManager.RegisterScreen("TitleHonor", titles.gameObject);

        var tasks = CreateMockupCanvas(root, "QuestTaskBoard_UI", 170, false);
        BuildQuestTaskBoardScreen(tasks);
        screenManager.RegisterScreen("QuestTaskBoard", tasks.gameObject);

        var activities = CreateMockupCanvas(root, "ActivityZone_UI", 180, false);
        BuildActivityZoneScreen(activities);
        screenManager.RegisterScreen("ActivityZone", activities.gameObject);

        var care = CreateMockupCanvas(root, "PokemonCare_UI", 190, false);
        BuildPokemonCareScreen(care);
        screenManager.RegisterScreen("PokemonCare", care.gameObject);

        var shopRecipe = CreateMockupCanvas(root, "ShopRecipe_UI", 200, false);
        BuildShopRecipeScreen(shopRecipe);
        screenManager.RegisterScreen("ShopRecipe", shopRecipe.gameObject);

        var contests = CreateMockupCanvas(root, "ContestTournament_UI", 210, false);
        BuildContestTournamentScreen(contests);
        screenManager.RegisterScreen("ContestTournament", contests.gameObject);

        var pause = CreateMockupCanvas(root, "PauseMenu_UI", 220, false);
        BuildPauseMenuScreen(pause);
        screenManager.RegisterScreen("PauseMenu", pause.gameObject);

        var bag = CreateMockupCanvas(root, "BagInventory_UI", 230, false);
        BuildBagInventoryScreen(bag);
        screenManager.RegisterScreen("BagInventory", bag.gameObject);

        var party = CreateMockupCanvas(root, "PokemonParty_UI", 240, false);
        BuildPokemonPartyScreen(party);
        screenManager.RegisterScreen("PokemonParty", party.gameObject);

        var settings = CreateMockupCanvas(root, "Settings_UI", 250, false);
        BuildSettingsScreen(settings);
        screenManager.RegisterScreen("Settings", settings.gameObject);

        var saveLoad = CreateMockupCanvas(root, "SaveLoad_UI", 260, false);
        BuildSaveLoadScreen(saveLoad);
        screenManager.RegisterScreen("SaveLoad", saveLoad.gameObject);

        var overworldInteraction = CreateMockupCanvas(root, "OverworldInteraction_UI", 270, false);
        BuildOverworldInteractionScreen(overworldInteraction);
        screenManager.RegisterScreen("OverworldInteraction", overworldInteraction.gameObject, closesOtherScreens: false, inputModeOnOpen: RuntimeUIScreenInputMode.Player);

        var activityHud = CreateMockupCanvas(root, "ActivityActionHUD_UI", 280, false);
        BuildActivityActionHudScreen(activityHud);
        screenManager.RegisterScreen("ActivityActionHUD", activityHud.gameObject, closesOtherScreens: false, inputModeOnOpen: RuntimeUIScreenInputMode.Player);
    }

    static Transform CreateMockupCanvas(Transform root, string name, int sortingOrder, bool activeByDefault) {
        var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.SetActive(activeByDefault);
        return canvasObject.transform;
    }

    static void BuildTitleHonorScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(238, 240, 236, 255));
        HeaderBlock(root, "Header", "Titles & Honors", "Research Volunteer active / 2 badges / 5 permissions", 40, 32, 1840, 82, new Color32(86, 92, 120, 255));
        Tab(root, "Tab_Titles", "Titles", 52, 138, 132, true);
        Tab(root, "Tab_Badges", "Badges", 196, 138, 132, false);
        Tab(root, "Tab_Permissions", "Permissions", 340, 138, 156, false);

        var titles = Panel(root, "Titles_View", 52, 194, 760, 760, new Color32(255, 255, 255, 255));
        Label(titles.transform, "Title", "Current Titles", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(titles.transform, "Title_ResearchVolunteer", "Research Volunteer", "Temporary / Oak Lab / expires in 2 days", 28, 88, 690, new Color32(119, 104, 168, 255));
        ListRowWide(titles.transform, "Title_FieldAssistant", "Field Assistant", "Permanent / survey rank 2", 28, 170, 690, new Color32(67, 123, 133, 255));
        ListRowWide(titles.transform, "Title_RouteHelper", "Route Helper", "Local / Oak Town board", 28, 252, 690, new Color32(75, 151, 103, 255));

        var badges = Panel(root, "Badges_View", 52, 194, 760, 760, new Color32(255, 255, 255, 255));
        Label(badges.transform, "Title", "Badges", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(badges.transform, "Badge_Boulder", "Boulder Badge", "Gym access / Kanto rank +1", 28, 88, 690, new Color32(117, 123, 128, 255));
        ListRowWide(badges.transform, "Badge_Cascade", "Cascade Badge", "Water route trust / ferry discount", 28, 170, 690, new Color32(92, 151, 178, 255));
        ListRowWide(badges.transform, "Badge_FrontierPass", "Frontier Pass", "Locked / requires regional cup result", 28, 252, 690, new Color32(196, 111, 105, 255));
        badges.SetActive(false);

        var permissions = Panel(root, "Permissions_View", 52, 194, 760, 760, new Color32(255, 255, 255, 255));
        Label(permissions.transform, "Title", "Unlocked Access", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(permissions.transform, "Access_Research", "Research Tasks", "Allowed while Research Volunteer is active", 28, 88, 690, new Color32(119, 104, 168, 255));
        ListRowWide(permissions.transform, "Access_Farm", "Care Yard", "Allowed in Oak Town ranch plots", 28, 170, 690, new Color32(75, 151, 103, 255));
        ListRowWide(permissions.transform, "Access_Mine", "Old Quarry", "Locked / needs safety title", 28, 252, 690, new Color32(188, 139, 82, 255));
        permissions.SetActive(false);

        var detail = Panel(root, "Selected_Honor_Detail", 852, 194, 1016, 760, new Color32(248, 251, 246, 255));
        Label(detail.transform, "Title", "Research Volunteer", 34, 30, 420, 38, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(detail.transform, "Duration", "Temporary", 34, 88, new Color32(214, 153, 70, 255));
        InfoPill(detail.transform, "Issuer", "Oak Lab", 166, 88, new Color32(67, 123, 133, 255));
        Label(detail.transform, "Body", "Professor Oak's aide gave this title for Route 1 morning surveys. Locals will share research sightings while it is active.", 34, 146, 860, 82, 18, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(detail.transform, "Unlock_01", "Unlocks", "Research board", 34, 270, new Color32(119, 104, 168, 255));
        SmallCard(detail.transform, "Unlock_02", "Unlocks", "Survey points", 34, 322, new Color32(67, 123, 133, 255));
        SmallCard(detail.transform, "Unlock_03", "Expires", "2 days", 34, 374, new Color32(214, 153, 70, 255));
    }

    static void BuildQuestTaskBoardScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(235, 239, 242, 255));
        HeaderBlock(root, "Header", "Task Board", "Oak Town / 6 active tasks / 2 ready to turn in", 40, 32, 1840, 82, new Color32(66, 87, 103, 255));

        var filters = Panel(root, "Task_Filters", 52, 150, 360, 780, new Color32(255, 255, 255, 255));
        Label(filters.transform, "Title", "Sources", 24, 22, 220, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        MenuOption(filters.transform, "Filter_Professor", "Professor", 24, 82, 294, true);
        MenuOption(filters.transform, "Filter_Police", "Police", 24, 140, 294, false);
        MenuOption(filters.transform, "Filter_Local", "Local Board", 24, 198, 294, false);
        MenuOption(filters.transform, "Filter_Personal", "Personal", 24, 256, 294, false);

        var professor = Panel(root, "Professor_Tasks_View", 452, 150, 760, 780, new Color32(255, 255, 255, 255));
        Label(professor.transform, "Title", "Professor Tasks", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(professor.transform, "Task_Survey", "Route 1 Survey", "Observe 3 water Pokemon near the pond", 28, 88, 690, new Color32(119, 104, 168, 255));
        ListRowWide(professor.transform, "Task_FieldKit", "Field Kit Check", "Bring back soil and berry notes", 28, 170, 690, new Color32(75, 151, 103, 255));
        ListRowWide(professor.transform, "Task_Rumor", "Confirm Rumor", "Check north woods nest report", 28, 252, 690, new Color32(214, 153, 70, 255));

        var police = Panel(root, "Police_Tasks_View", 452, 150, 760, 780, new Color32(255, 255, 255, 255));
        Label(police.transform, "Title", "Police Tasks", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(police.transform, "Task_LostParcel", "Lost Parcel", "Ask shopkeepers near east road", 28, 88, 690, new Color32(196, 111, 105, 255));
        ListRowWide(police.transform, "Task_MarketWatch", "Market Watch", "Report repeated shelf theft rumors", 28, 170, 690, new Color32(188, 139, 82, 255));
        police.SetActive(false);

        var local = Panel(root, "LocalBoard_Tasks_View", 452, 150, 760, 780, new Color32(255, 255, 255, 255));
        Label(local.transform, "Title", "Local Board", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(local.transform, "Task_CareYard", "Care Yard Help", "Brush two tired Pokemon before evening", 28, 88, 690, new Color32(75, 151, 103, 255));
        ListRowWide(local.transform, "Task_BerryPatch", "Berry Patch", "Water the marked farm row", 28, 170, 690, new Color32(91, 130, 190, 255));
        local.SetActive(false);

        var personal = Panel(root, "Personal_Tasks_View", 452, 150, 760, 780, new Color32(255, 255, 255, 255));
        Label(personal.transform, "Title", "Personal", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(personal.transform, "Task_Mira", "Meet Mira", "Discuss casual 3v3 terms near east sign", 28, 88, 690, new Color32(92, 151, 178, 255));
        ListRowWide(personal.transform, "Task_Basket", "Market Basket", "Buy bait recipe before night", 28, 170, 690, new Color32(214, 153, 70, 255));
        personal.SetActive(false);

        var detail = Panel(root, "Selected_Task_Detail", 1240, 150, 580, 780, new Color32(248, 251, 246, 255));
        Label(detail.transform, "Title", "Route 1 Survey", 28, 24, 360, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(detail.transform, "Status", "Active", 28, 82, new Color32(75, 151, 103, 255));
        InfoPill(detail.transform, "Source", "Oak Lab", 132, 82, new Color32(119, 104, 168, 255));
        Label(detail.transform, "Body", "Observe water Pokemon near the pond and return with field notes before sunset.", 28, 142, 500, 68, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(detail.transform, "Reward_01", "Reward", "Research XP", 28, 246, new Color32(119, 104, 168, 255));
        SmallCard(detail.transform, "Reward_02", "Reward", "PokeNav note", 28, 298, new Color32(67, 123, 133, 255));
        SmallCard(detail.transform, "Need_01", "Needs", "Volunteer title", 28, 350, new Color32(214, 153, 70, 255));
    }

    static void BuildActivityZoneScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(236, 241, 235, 255));
        HeaderBlock(root, "Header", "Activity Zone", "Oak Town care yard / activities allowed inside marked area", 40, 32, 1840, 82, new Color32(74, 104, 76, 255));

        var categories = Panel(root, "Activity_Categories", 52, 150, 360, 780, new Color32(255, 255, 255, 255));
        Label(categories.transform, "Title", "Activities", 24, 22, 220, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        MenuOption(categories.transform, "Activity_Farming", "Farming", 24, 82, 294, true);
        MenuOption(categories.transform, "Activity_Mining", "Mining", 24, 140, 294, false);
        MenuOption(categories.transform, "Activity_Fishing", "Fishing", 24, 198, 294, false);
        MenuOption(categories.transform, "Activity_Care", "Pokemon Care", 24, 256, 294, false);

        var farming = Panel(root, "Farming_Activity_View", 452, 150, 820, 780, new Color32(255, 255, 255, 255));
        BuildActivityDetail(farming.transform, "Farming", "Berry row is open. Apricot trees need morning water and clean soil.", "Allowed", "Oak care yard", "Tool", "Watering can", "Reward", "Berries + care feed");

        var mining = Panel(root, "Mining_Activity_View", 452, 150, 820, 780, new Color32(255, 255, 255, 255));
        BuildActivityDetail(mining.transform, "Mining", "Old Quarry is locked until a safety title is earned from the town board.", "Locked", "Old Quarry", "Tool", "Pickaxe", "Reward", "Ore + fossil chance");
        mining.SetActive(false);

        var fishing = Panel(root, "Fishing_Activity_View", 452, 150, 820, 780, new Color32(255, 255, 255, 255));
        BuildActivityDetail(fishing.transform, "Fishing", "Route 1 pond allows low-level fishing during calm weather.", "Allowed", "Route 1 pond", "Tool", "Old rod", "Reward", "Water sightings");
        fishing.SetActive(false);

        var care = Panel(root, "Care_Activity_View", 452, 150, 820, 780, new Color32(255, 255, 255, 255));
        BuildActivityDetail(care.transform, "Pokemon Care", "Care yard tasks reduce tiredness and improve trust for active party Pokemon.", "Allowed", "Care yard", "Tool", "Brush kit", "Reward", "Trust + mood");
        care.SetActive(false);

        var area = Panel(root, "Activity_Area_Detail", 1312, 150, 508, 780, new Color32(248, 251, 246, 255));
        Label(area.transform, "Title", "Area Rules", 24, 24, 260, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        SmallCard(area.transform, "Rule_01", "Zone", "Allowed only here", 24, 88, new Color32(75, 151, 103, 255));
        SmallCard(area.transform, "Rule_02", "Owner", "Oak Ranch", 24, 140, new Color32(67, 123, 133, 255));
        SmallCard(area.transform, "Rule_03", "Title", "Route Helper", 24, 192, new Color32(214, 153, 70, 255));
        Label(area.transform, "Body", "Outside marked activity zones, tools stay inactive and Pokemon care actions are not offered.", 24, 266, 428, 86, 16, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
    }

    static void BuildActivityDetail(Transform root, string title, string body, string stateTitle, string stateBody, string toolTitle, string toolBody, string rewardTitle, string rewardBody) {
        Label(root, "Title", title, 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(root, "Body", body, 28, 84, 700, 72, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(root, "State", stateTitle, stateBody, 28, 196, new Color32(75, 151, 103, 255));
        SmallCard(root, "Tool", toolTitle, toolBody, 28, 248, new Color32(91, 130, 190, 255));
        SmallCard(root, "Reward", rewardTitle, rewardBody, 28, 300, new Color32(214, 153, 70, 255));
        ListRowWide(root, "Action_01", "Primary Action", "Interact with the marked station", 28, 392, 730, new Color32(67, 123, 133, 255));
        ListRowWide(root, "Action_02", "Pokemon Help", "Partner traits may improve yield", 28, 474, 730, new Color32(119, 104, 168, 255));
    }

    static void BuildPokemonCareScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(238, 243, 239, 255));
        HeaderBlock(root, "Header", "Pokemon Care", "Cyndaquil / calm mood / hungry soon", 40, 32, 1840, 82, new Color32(83, 102, 77, 255));
        Tab(root, "Tab_Needs", "Needs", 52, 138, 132, true);
        Tab(root, "Tab_Bond", "Bond", 196, 138, 132, false);
        Tab(root, "Tab_Routines", "Routines", 340, 138, 132, false);
        Tab(root, "Tab_Medical", "Medical", 484, 138, 132, false);

        var needs = Panel(root, "Care_Needs_View", 52, 194, 1080, 760, new Color32(255, 255, 255, 255));
        Label(needs.transform, "Title", "Needs", 28, 24, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        NeedBar(needs.transform, "Hunger", "Hunger", 28, 92, 0.68f, new Color32(214, 153, 70, 255));
        NeedBar(needs.transform, "Tiredness", "Tiredness", 28, 164, 0.36f, new Color32(91, 130, 190, 255));
        NeedBar(needs.transform, "Sleep", "Sleep", 28, 236, 0.48f, new Color32(119, 104, 168, 255));
        NeedBar(needs.transform, "Cleanliness", "Cleanliness", 28, 308, 0.82f, new Color32(75, 151, 103, 255));
        ListRowWide(needs.transform, "Care_Action_01", "Feed", "Use soft berry feed from basket", 28, 420, 820, new Color32(214, 153, 70, 255));
        ListRowWide(needs.transform, "Care_Action_02", "Rest", "Camp rest available after dusk", 28, 502, 820, new Color32(91, 130, 190, 255));

        var bond = Panel(root, "Care_Bond_View", 52, 194, 1080, 760, new Color32(255, 255, 255, 255));
        Label(bond.transform, "Title", "Bond", 28, 24, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(bond.transform, "Bond_Trust", "Trust", "High / follows calmly in town", 28, 92, 820, new Color32(75, 151, 103, 255));
        ListRowWide(bond.transform, "Bond_Personality", "Personality", "Curious / reacts to warm areas", 28, 174, 820, new Color32(214, 153, 70, 255));
        ListRowWide(bond.transform, "Bond_Companion", "Companion", "Can help with camp fire and night walks", 28, 256, 820, new Color32(119, 104, 168, 255));
        bond.SetActive(false);

        var routines = Panel(root, "Care_Routines_View", 52, 194, 1080, 760, new Color32(255, 255, 255, 255));
        Label(routines.transform, "Title", "Routines", 28, 24, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(routines.transform, "Routine_Morning", "Morning", "Brush, feed, check mood before route tasks", 28, 92, 820, new Color32(67, 123, 133, 255));
        ListRowWide(routines.transform, "Routine_Evening", "Evening", "Rest, camp talk, clean held items", 28, 174, 820, new Color32(119, 104, 168, 255));
        routines.SetActive(false);

        var medical = Panel(root, "Care_Medical_View", 52, 194, 1080, 760, new Color32(255, 255, 255, 255));
        Label(medical.transform, "Title", "Medical", 28, 24, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(medical.transform, "Medical_Status", "Status", "Healthy / no injury / no illness", 28, 92, 820, new Color32(75, 151, 103, 255));
        ListRowWide(medical.transform, "Medical_Clinic", "Clinic", "Oak Town nurse accepts walk-ins", 28, 174, 820, new Color32(91, 130, 190, 255));
        medical.SetActive(false);

        var portrait = Panel(root, "Care_Pokemon_Portrait", 1180, 194, 640, 760, new Color32(248, 251, 246, 255));
        Panel(portrait.transform, "Pokemon_Sprite_Placeholder", 164, 74, 300, 300, new Color32(218, 229, 220, 255));
        Label(portrait.transform, "Pokemon_Name", "Cyndaquil", 64, 410, 360, 36, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(portrait.transform, "Mood", "Calm", 64, 466, new Color32(75, 151, 103, 255));
        InfoPill(portrait.transform, "Need", "Hungry soon", 154, 466, new Color32(214, 153, 70, 255));
        Label(portrait.transform, "Care_Note", "Enjoys warm meals after rain. Trust increases faster after evening camp routines.", 64, 536, 500, 72, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
    }

    static void NeedBar(Transform parent, string name, string label, float x, float y, float fill, Color fillColor) {
        Label(parent, name + "_Label", label, x, y, 160, 24, 16, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Panel(parent, name + "_Track", x + 180, y + 4, 520, 18, new Color32(226, 232, 228, 255));
        Panel(parent, name + "_Fill", x + 180, y + 4, 520 * Mathf.Clamp01(fill), 18, fillColor);
    }

    static void BuildShopRecipeScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(240, 238, 232, 255));
        HeaderBlock(root, "Header", "Shop & Recipes", "Oak Town Market / shelf offers / 2 recipes available", 40, 32, 1840, 82, new Color32(105, 82, 62, 255));
        Tab(root, "Tab_Offers", "Offers", 52, 138, 132, true);
        Tab(root, "Tab_Recipes", "Recipes", 196, 138, 132, false);
        Tab(root, "Tab_Brands", "Brands", 340, 138, 132, false);
        Tab(root, "Tab_SpecialShops", "Special Shops", 484, 138, 156, false);

        var offers = Panel(root, "Shop_Offers_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(offers.transform, "Title", "Shelf Offers", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(offers.transform, "Offer_PotionA", "Potion A", "25 credits / restores 20% / common brand", 28, 92, 900, new Color32(88, 140, 190, 255));
        ListRowWide(offers.transform, "Offer_PotionB", "Potion B", "80 credits / restores 80% / premium brand", 28, 174, 900, new Color32(75, 151, 103, 255));
        ListRowWide(offers.transform, "Offer_Bait", "Soft Bait", "35 credits / attracts pond Pokemon", 28, 256, 900, new Color32(188, 139, 82, 255));
        ListRowWide(offers.transform, "Offer_Ball", "Poke Ball Pack", "500 credits / pack of 5", 28, 338, 900, new Color32(196, 111, 105, 255));

        var recipes = Panel(root, "Shop_Recipes_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(recipes.transform, "Title", "Recipes", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(recipes.transform, "Recipe_Bait", "Soft Bait Recipe", "120 credits / learned permanently", 28, 92, 900, new Color32(188, 139, 82, 255));
        ListRowWide(recipes.transform, "Recipe_Medicine", "Basic Medicine Recipe", "Locked / Herbalist title needed", 28, 174, 900, new Color32(75, 151, 103, 255));
        recipes.SetActive(false);

        var brands = Panel(root, "Shop_Brands_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(brands.transform, "Title", "Brands", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(brands.transform, "Brand_Common", "BlueCap", "Cheap, reliable, low recovery", 28, 92, 900, new Color32(88, 140, 190, 255));
        ListRowWide(brands.transform, "Brand_Premium", "GreenLeaf", "Expensive, stronger recovery, rare stock", 28, 174, 900, new Color32(75, 151, 103, 255));
        brands.SetActive(false);

        var special = Panel(root, "Shop_SpecialShops_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(special.transform, "Title", "Special Shops", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(special.transform, "Special_BallMaster", "Poke Ball Master", "Custom balls, apricorn requests, rare model stock", 28, 92, 900, new Color32(196, 111, 105, 255));
        ListRowWide(special.transform, "Special_Herbalist", "Herbalist", "Care medicine, feed recipes, sickness remedies", 28, 174, 900, new Color32(75, 151, 103, 255));
        ListRowWide(special.transform, "Special_BaitShop", "Bait Shop", "Pokemon food, lures, habitat-specific blends", 28, 256, 900, new Color32(188, 139, 82, 255));
        special.SetActive(false);

        var basket = Panel(root, "Shop_Basket_Summary", 1214, 194, 606, 760, new Color32(248, 251, 246, 255));
        Label(basket.transform, "Title", "Basket", 28, 24, 260, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        SmallCard(basket.transform, "Basket_01", "Potion A x2", "50", 28, 92, new Color32(88, 140, 190, 255));
        SmallCard(basket.transform, "Basket_02", "Recipe x1", "120", 28, 144, new Color32(188, 139, 82, 255));
        SmallCard(basket.transform, "Basket_03", "Balls x5", "500", 28, 196, new Color32(196, 111, 105, 255));
        Label(basket.transform, "Total", "Total: 670", 28, 300, 200, 30, 21, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
    }

    static void BuildContestTournamentScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(241, 238, 243, 255));
        HeaderBlock(root, "Header", "Contests & Tournaments", "Oak Town Junior Cup tomorrow / Frontier locked / World stage quiet", 40, 32, 1840, 82, new Color32(98, 78, 119, 255));
        Tab(root, "Tab_Contests", "Contests", 52, 138, 132, true);
        Tab(root, "Tab_Frontier", "Frontier", 196, 138, 132, false);
        Tab(root, "Tab_World", "World Cup", 340, 138, 132, false);
        Tab(root, "Tab_Regional", "Regional", 484, 138, 132, false);

        var contests = Panel(root, "Contests_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(contests.transform, "Title", "Pokemon Contests", 28, 24, 360, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(contests.transform, "Contest_Cute", "Cute Showcase", "Partner mood and care routines matter", 28, 92, 900, new Color32(214, 153, 70, 255));
        ListRowWide(contests.transform, "Contest_Smart", "Smart Showcase", "Research notes unlock bonus themes", 28, 174, 900, new Color32(119, 104, 168, 255));
        ListRowWide(contests.transform, "Contest_Strong", "Strong Showcase", "Battle titles improve entry rank", 28, 256, 900, new Color32(196, 111, 105, 255));

        var frontier = Panel(root, "BattleFrontier_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(frontier.transform, "Title", "Battle Frontier", 28, 24, 360, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(frontier.transform, "Frontier_Tower", "Battle Tower", "Locked / requires regional champion title", 28, 92, 900, new Color32(196, 111, 105, 255));
        ListRowWide(frontier.transform, "Frontier_Factory", "Battle Factory", "Draft rentals and unusual rule sets", 28, 174, 900, new Color32(91, 130, 190, 255));
        frontier.SetActive(false);

        var world = Panel(root, "WorldChampionship_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(world.transform, "Title", "World Championship", 28, 24, 420, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(world.transform, "World_Invite", "Invitation", "Requires champion rank or special story invite", 28, 92, 900, new Color32(188, 139, 82, 255));
        ListRowWide(world.transform, "World_Rules", "World Rules", "Power mechanics and party limits rotate by season", 28, 174, 900, new Color32(119, 104, 168, 255));
        world.SetActive(false);

        var regional = Panel(root, "RegionalCups_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(regional.transform, "Title", "Regional Cups", 28, 24, 360, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(regional.transform, "Cup_Kanto", "Kanto Junior Cup", "Tomorrow / 3 Pokemon / no Z-Move", 28, 92, 900, new Color32(67, 123, 133, 255));
        ListRowWide(regional.transform, "Cup_Alola", "Alola Trial Cup", "Locked / Z-Move reward path", 28, 174, 900, new Color32(119, 104, 168, 255));
        regional.SetActive(false);

        var details = Panel(root, "Tournament_Detail", 1214, 194, 606, 760, new Color32(248, 251, 246, 255));
        Label(details.transform, "Title", "Kanto Junior Cup", 28, 24, 360, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(details.transform, "Schedule", "Tomorrow", 28, 82, new Color32(214, 153, 70, 255));
        InfoPill(details.transform, "Party", "3 Pokemon", 150, 82, new Color32(91, 130, 190, 255));
        Label(details.transform, "Body", "A beginner-friendly town cup. Mega Evolution is disabled, but trainer titles can unlock special bracket notes.", 28, 142, 500, 72, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(details.transform, "Prize_01", "Prize", "Cup ribbon", 28, 252, new Color32(188, 139, 82, 255));
        SmallCard(details.transform, "Prize_02", "Title", "Junior finalist", 28, 304, new Color32(119, 104, 168, 255));
        SmallCard(details.transform, "Rule_01", "Rule", "No Z-Move", 28, 356, new Color32(196, 111, 105, 255));
    }

    static void BuildBagInventoryScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(238, 240, 236, 255));
        HeaderBlock(root, "Header", "Bag", "14 items / 2 recipes / 3 tools", 40, 32, 1840, 82, new Color32(77, 92, 102, 255));
        Tab(root, "Tab_Items", "Items", 52, 138, 132, true);
        Tab(root, "Tab_Tools", "Tools", 196, 138, 132, false);
        Tab(root, "Tab_Recipes", "Recipes", 340, 138, 132, false);
        Tab(root, "Tab_KeyItems", "Key Items", 484, 138, 132, false);

        var items = Panel(root, "Bag_Items_View", 52, 194, 980, 760, new Color32(255, 255, 255, 255));
        Label(items.transform, "Title", "Items", 28, 24, 260, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(items.transform, "Item_PotionA", "Potion A x2", "Restores 20% / common brand", 28, 92, 820, new Color32(88, 140, 190, 255));
        ListRowWide(items.transform, "Item_Bait", "Soft Bait x1", "Attracts pond Pokemon", 28, 174, 820, new Color32(188, 139, 82, 255));
        ListRowWide(items.transform, "Item_Pokeball", "Poke Ball x5", "Standard capture ball", 28, 256, 820, new Color32(196, 111, 105, 255));

        var tools = Panel(root, "Bag_Tools_View", 52, 194, 980, 760, new Color32(255, 255, 255, 255));
        Label(tools.transform, "Title", "Tools", 28, 24, 260, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(tools.transform, "Tool_Rod", "Old Rod", "Fishing allowed at Route 1 pond", 28, 92, 820, new Color32(92, 151, 178, 255));
        ListRowWide(tools.transform, "Tool_Brush", "Brush Kit", "Care yard grooming action", 28, 174, 820, new Color32(75, 151, 103, 255));
        ListRowWide(tools.transform, "Tool_Can", "Watering Can", "Berry and apricot plot action", 28, 256, 820, new Color32(91, 130, 190, 255));
        tools.SetActive(false);

        var recipes = Panel(root, "Bag_Recipes_View", 52, 194, 980, 760, new Color32(255, 255, 255, 255));
        Label(recipes.transform, "Title", "Recipes", 28, 24, 260, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(recipes.transform, "Recipe_Bait", "Soft Bait", "Berry pulp + pond herb", 28, 92, 820, new Color32(188, 139, 82, 255));
        ListRowWide(recipes.transform, "Recipe_Feed", "Warm Feed", "Locked / caretaker title", 28, 174, 820, new Color32(214, 153, 70, 255));
        recipes.SetActive(false);

        var keyItems = Panel(root, "Bag_KeyItems_View", 52, 194, 980, 760, new Color32(255, 255, 255, 255));
        Label(keyItems.transform, "Title", "Key Items", 28, 24, 260, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(keyItems.transform, "Key_PokeNav", "PokeNav", "Map, social notes, guide and region info", 28, 92, 820, new Color32(67, 123, 133, 255));
        ListRowWide(keyItems.transform, "Key_VolunteerPass", "Volunteer Pass", "Temporary Oak Lab research access", 28, 174, 820, new Color32(119, 104, 168, 255));
        keyItems.SetActive(false);

        var detail = Panel(root, "Bag_Item_Detail", 1080, 194, 740, 760, new Color32(248, 251, 246, 255));
        Label(detail.transform, "Title", "Potion A", 32, 28, 300, 36, 27, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(detail.transform, "Brand", "BlueCap", 32, 88, new Color32(88, 140, 190, 255));
        InfoPill(detail.transform, "Count", "x2", 150, 88, new Color32(75, 151, 103, 255));
        Label(detail.transform, "Body", "Cheap and reliable. Restores a small amount and appears in most town markets.", 32, 146, 620, 72, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(detail.transform, "Use_01", "Use", "Party heal", 32, 254, new Color32(75, 151, 103, 255));
        SmallCard(detail.transform, "Sell_01", "Sell", "12 credits", 32, 306, new Color32(188, 139, 82, 255));
    }

    static void BuildPokemonPartyScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(237, 242, 239, 255));
        HeaderBlock(root, "Header", "Pokemon Party", "2 active Pokemon / 1 partner / care check recommended", 40, 32, 1840, 82, new Color32(79, 96, 82, 255));

        var partyList = Panel(root, "Party_List", 52, 150, 500, 780, new Color32(255, 255, 255, 255));
        Label(partyList.transform, "Title", "Party", 24, 22, 220, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(partyList.transform, "Party_Cyndaquil", "Cyndaquil", "Lv 12 / calm / hungry soon", 24, 82, 430, new Color32(214, 122, 74, 255));
        ListRowWide(partyList.transform, "Party_Buizel", "Buizel", "Lv 10 / playful / clean", 24, 164, 430, new Color32(92, 151, 178, 255));
        ListRowWide(partyList.transform, "Party_Empty", "Empty Slot", "Available", 24, 246, 430, new Color32(150, 160, 168, 255));

        Tab(root, "Tab_Summary", "Summary", 596, 150, 132, true);
        Tab(root, "Tab_Moves", "Moves", 740, 150, 132, false);
        Tab(root, "Tab_Care", "Care", 884, 150, 132, false);
        Tab(root, "Tab_Companion", "Companion", 1028, 150, 148, false);

        var summary = Panel(root, "Party_Summary_View", 596, 206, 1224, 724, new Color32(255, 255, 255, 255));
        Label(summary.transform, "Title", "Cyndaquil", 34, 28, 320, 36, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Panel(summary.transform, "Pokemon_Sprite", 34, 90, 240, 240, new Color32(222, 232, 225, 255));
        SmallCard(summary.transform, "Type", "Type", "Fire", 320, 100, new Color32(214, 122, 74, 255));
        SmallCard(summary.transform, "Nature", "Nature", "Curious", 320, 152, new Color32(119, 104, 168, 255));
        SmallCard(summary.transform, "Title", "Role", "Partner", 320, 204, new Color32(75, 151, 103, 255));
        Label(summary.transform, "Body", "Trust is high enough for camp help. Warm weather and evening routines improve mood faster.", 320, 286, 720, 74, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);

        var moves = Panel(root, "Party_Moves_View", 596, 206, 1224, 724, new Color32(255, 255, 255, 255));
        Label(moves.transform, "Title", "Moves", 34, 28, 320, 36, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(moves.transform, "Move_01", "Quick Attack", "Classic slot / low cost", 34, 96, 900, new Color32(91, 130, 190, 255));
        ListRowWide(moves.transform, "Move_02", "Ember", "Fire action / burn chance", 34, 178, 900, new Color32(214, 122, 74, 255));
        ListRowWide(moves.transform, "Move_03", "Command Palette", "Unlocked later for alternate battle mode", 34, 260, 900, new Color32(119, 104, 168, 255));
        moves.SetActive(false);

        var care = Panel(root, "Party_Care_View", 596, 206, 1224, 724, new Color32(255, 255, 255, 255));
        Label(care.transform, "Title", "Care", 34, 28, 320, 36, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        NeedBar(care.transform, "Hunger", "Hunger", 34, 106, 0.68f, new Color32(214, 153, 70, 255));
        NeedBar(care.transform, "Energy", "Energy", 34, 178, 0.72f, new Color32(75, 151, 103, 255));
        NeedBar(care.transform, "Mood", "Mood", 34, 250, 0.84f, new Color32(119, 104, 168, 255));
        care.SetActive(false);

        var companion = Panel(root, "Party_Companion_View", 596, 206, 1224, 724, new Color32(255, 255, 255, 255));
        Label(companion.transform, "Title", "Companion", 34, 28, 320, 36, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(companion.transform, "Companion_01", "Field Help", "Can light camp fire and warm cold routes", 34, 96, 900, new Color32(214, 122, 74, 255));
        ListRowWide(companion.transform, "Companion_02", "Behavior", "Stays close in towns, explores near forests", 34, 178, 900, new Color32(75, 151, 103, 255));
        companion.SetActive(false);
    }

    static void BuildSettingsScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(237, 240, 242, 255));
        HeaderBlock(root, "Header", "Settings", "Gameplay, controls, audio and display", 40, 32, 1840, 82, new Color32(70, 82, 96, 255));

        var menu = Panel(root, "Settings_Categories", 52, 150, 360, 780, new Color32(255, 255, 255, 255));
        Label(menu.transform, "Title", "Categories", 24, 22, 220, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        MenuOption(menu.transform, "Category_Gameplay", "Gameplay", 24, 82, 294, true);
        MenuOption(menu.transform, "Category_Controls", "Controls", 24, 140, 294, false);
        MenuOption(menu.transform, "Category_Audio", "Audio", 24, 198, 294, false);
        MenuOption(menu.transform, "Category_Display", "Display", 24, 256, 294, false);

        var gameplay = Panel(root, "Settings_Gameplay_View", 452, 150, 1368, 780, new Color32(255, 255, 255, 255));
        BuildSettingsCategoryPanel(gameplay.transform, "Gameplay", "Text Speed", "Normal", "Autosave", "On", "Battle Mode", "Ask");

        var controls = Panel(root, "Settings_Controls_View", 452, 150, 1368, 780, new Color32(255, 255, 255, 255));
        BuildSettingsCategoryPanel(controls.transform, "Controls", "Interact", "Enter", "Menu", "Esc", "Run", "Shift");
        controls.SetActive(false);

        var audio = Panel(root, "Settings_Audio_View", 452, 150, 1368, 780, new Color32(255, 255, 255, 255));
        BuildSettingsCategoryPanel(audio.transform, "Audio", "Master", "80%", "Music", "65%", "SFX", "75%");
        audio.SetActive(false);

        var display = Panel(root, "Settings_Display_View", 452, 150, 1368, 780, new Color32(255, 255, 255, 255));
        BuildSettingsCategoryPanel(display.transform, "Display", "Window", "Fullscreen", "UI Scale", "100%", "VSync", "On");
        display.SetActive(false);
    }

    static void BuildSettingsCategoryPanel(Transform root, string title, string row1Title, string row1Body, string row2Title, string row2Body, string row3Title, string row3Body) {
        Label(root, "Title", title, 34, 30, 320, 36, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        SmallCard(root, "Setting_01", row1Title, row1Body, 34, 104, new Color32(91, 130, 190, 255));
        SmallCard(root, "Setting_02", row2Title, row2Body, 34, 156, new Color32(75, 151, 103, 255));
        SmallCard(root, "Setting_03", row3Title, row3Body, 34, 208, new Color32(119, 104, 168, 255));
    }

    static void BuildSaveLoadScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(239, 241, 237, 255));
        HeaderBlock(root, "Header", "Save / Load", "Autosave on / 2 manual slots / 1 backup", 40, 32, 1840, 82, new Color32(87, 94, 80, 255));
        Tab(root, "Tab_Save", "Save", 52, 138, 132, true);
        Tab(root, "Tab_Load", "Load", 196, 138, 132, false);
        Tab(root, "Tab_Backups", "Backups", 340, 138, 132, false);

        var save = Panel(root, "Save_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(save.transform, "Title", "Save Slots", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(save.transform, "Save_Slot_01", "Slot 1", "Oak Town / Day 3 / Route survey active", 28, 92, 900, new Color32(67, 123, 133, 255));
        ListRowWide(save.transform, "Save_Slot_02", "Slot 2", "Empty", 28, 174, 900, new Color32(150, 160, 168, 255));

        var load = Panel(root, "Load_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(load.transform, "Title", "Load Slots", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(load.transform, "Load_Slot_01", "Oak Town", "Day 3 / Cyndaquil partner / 2 badges", 28, 92, 900, new Color32(67, 123, 133, 255));
        ListRowWide(load.transform, "Load_Slot_02", "Johto Trip", "Day 12 / station pass / 5 titles", 28, 174, 900, new Color32(91, 130, 190, 255));
        load.SetActive(false);

        var backups = Panel(root, "Backups_View", 52, 194, 1120, 760, new Color32(255, 255, 255, 255));
        Label(backups.transform, "Title", "Backups", 28, 24, 320, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(backups.transform, "Backup_Auto", "Autosave", "15:40 / before market checkout", 28, 92, 900, new Color32(75, 151, 103, 255));
        ListRowWide(backups.transform, "Backup_Previous", "Previous Manual", "Day 2 / Oak Town center", 28, 174, 900, new Color32(214, 153, 70, 255));
        backups.SetActive(false);

        var detail = Panel(root, "SaveLoad_Detail", 1214, 194, 606, 760, new Color32(248, 251, 246, 255));
        Label(detail.transform, "Title", "Slot 1", 28, 24, 260, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        SmallCard(detail.transform, "Detail_01", "Region", "Kanto", 28, 92, new Color32(67, 123, 133, 255));
        SmallCard(detail.transform, "Detail_02", "Location", "Oak Town", 28, 144, new Color32(91, 130, 190, 255));
        SmallCard(detail.transform, "Detail_03", "Progress", "2 badges", 28, 196, new Color32(188, 139, 82, 255));
        Label(detail.transform, "Body", "Manual save includes current region, party, task state, market basket and active rumors.", 28, 284, 500, 74, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
    }

    static void BuildOverworldInteractionScreen(Transform root) {
        Panel(root, "Transparent_Play_Backdrop", 0, 0, 1920, 1080, new Color32(0, 0, 0, 0));
        Panel(root, "Player_Silhouette", 880, 600, 96, 150, new Color32(58, 72, 78, 255));
        Panel(root, "Activity_Node_Plot", 1040, 654, 180, 84, new Color32(120, 145, 96, 255));
        Panel(root, "Activity_Node_Highlight", 1032, 646, 196, 100, new Color32(237, 203, 108, 90));

        var prompt = Panel(root, "Interaction_Prompt", 820, 438, 520, 138, new Color32(248, 251, 246, 245));
        Label(prompt.transform, "Action_Key", "E", 18, 24, 58, 58, 32, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        Panel(prompt.transform, "Action_Key_Background", 18, 24, 58, 58, new Color32(67, 123, 133, 255)).transform.SetAsFirstSibling();
        Label(prompt.transform, "Target_Name", "Berry Plot", 96, 24, 260, 28, 21, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(prompt.transform, "Prompt_Text", "Water soil / inspect crop / ask Cyndaquil to help", 96, 60, 360, 24, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.MiddleLeft);
        InfoPill(prompt.transform, "Permission", "Allowed area", 96, 94, new Color32(75, 151, 103, 255));
        InfoPill(prompt.transform, "Tool", "Watering can", 230, 94, new Color32(91, 130, 190, 255));

        var actionWheel = Panel(root, "Context_Action_Wheel", 1330, 430, 286, 244, new Color32(42, 50, 55, 238));
        Label(actionWheel.transform, "Title", "Actions", 24, 20, 180, 28, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        MenuOption(actionWheel.transform, "Action_Water", "Water", 24, 66, 238, true);
        MenuOption(actionWheel.transform, "Action_Inspect", "Inspect", 24, 124, 238, false);
        MenuOption(actionWheel.transform, "Action_PartnerHelp", "Partner Help", 24, 182, 238, false);

        var blocked = Panel(root, "Blocked_Action_View", 820, 602, 520, 90, new Color32(58, 42, 42, 230));
        Label(blocked.transform, "Blocked_Title", "Mining unavailable here", 20, 14, 300, 24, 17, FontStyle.Bold, new Color32(255, 235, 228, 255), TextAnchor.MiddleLeft);
        Label(blocked.transform, "Blocked_Body", "Old Quarry permission required.", 20, 46, 360, 20, 14, FontStyle.Normal, new Color32(244, 214, 205, 255), TextAnchor.MiddleLeft);
        blocked.SetActive(false);

        var camp = Panel(root, "Camp_Care_Context_View", 160, 690, 430, 246, new Color32(248, 251, 246, 245));
        Label(camp.transform, "Title", "Camp Spot", 24, 22, 240, 28, 21, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(camp.transform, "Camp_Action_Feed", "Feed Pokemon", "Uses selected food from bag", 24, 70, 360, new Color32(214, 153, 70, 255));
        ListRowWide(camp.transform, "Camp_Action_Rest", "Rest", "Reduces tiredness after dusk", 24, 152, 360, new Color32(91, 130, 190, 255));
    }

    static void BuildActivityActionHudScreen(Transform root) {
        Panel(root, "Transparent_Play_Backdrop", 0, 0, 1920, 1080, new Color32(0, 0, 0, 0));
        Panel(root, "Player_Silhouette", 880, 604, 96, 150, new Color32(58, 72, 78, 255));
        Panel(root, "Active_Node", 1038, 656, 186, 78, new Color32(120, 145, 96, 255));

        var hud = Panel(root, "Activity_Action_HUD", 620, 752, 680, 156, new Color32(39, 47, 51, 242));
        Label(hud.transform, "Action_Title", "Watering Berry Plot", 24, 20, 320, 30, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        Label(hud.transform, "Action_State", "Hold action until the soil is soaked.", 24, 54, 420, 22, 15, FontStyle.Normal, new Color32(220, 228, 230, 255), TextAnchor.MiddleLeft);
        Panel(hud.transform, "Progress_Track", 24, 92, 510, 18, new Color32(75, 85, 90, 255));
        Panel(hud.transform, "Progress_Fill", 24, 92, 350, 18, new Color32(237, 203, 108, 255));
        Label(hud.transform, "Progress_Label", "68%", 552, 86, 72, 28, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
        InfoPill(hud.transform, "Tool_Pill", "Watering can", 24, 122, new Color32(91, 130, 190, 255));
        InfoPill(hud.transform, "Stamina_Pill", "Stamina -4", 176, 122, new Color32(214, 153, 70, 255));

        var partner = Panel(root, "Partner_Assist_HUD", 1328, 728, 420, 180, new Color32(248, 251, 246, 245));
        Label(partner.transform, "Title", "Cyndaquil Assist", 24, 22, 260, 28, 21, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        SmallCard(partner.transform, "Mood", "Mood", "Calm", 24, 70, new Color32(75, 151, 103, 255));
        SmallCard(partner.transform, "Help", "Help", "Warm soil", 24, 122, new Color32(214, 122, 74, 255));

        var result = Panel(root, "Activity_Result_Toast", 620, 594, 680, 98, new Color32(248, 251, 246, 245));
        Label(result.transform, "Result_Title", "Berry plot watered", 24, 18, 320, 28, 21, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(result.transform, "Result_Body", "Growth improved. Cyndaquil gained a little trust.", 24, 52, 520, 22, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.MiddleLeft);
        result.SetActive(false);
    }

    static void BuildMainMenuScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(32, 48, 52, 255));
        Panel(root, "Route_Silhouette", 820, 100, 880, 760, new Color32(56, 82, 78, 255));
        Panel(root, "Town_Lights", 1010, 602, 420, 78, new Color32(237, 203, 108, 120));
        Label(root, "Game_Title", "Pokemon Project", 120, 110, 620, 72, 54, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        Label(root, "Game_Subtitle", "Kanto start / free route / quiet morning", 126, 188, 540, 28, 18, FontStyle.Normal, new Color32(210, 226, 226, 255), TextAnchor.MiddleLeft);

        var menu = Panel(root, "MainMenu_Options", 120, 284, 360, 424, new Color32(245, 248, 243, 245));
        MenuOption(menu.transform, "Option_NewGame", "New Game", 26, 28, 308, true);
        MenuOption(menu.transform, "Option_Continue", "Continue", 26, 86, 308, false);
        MenuOption(menu.transform, "Option_LoadGame", "Load Game", 26, 144, 308, false);
        MenuOption(menu.transform, "Option_Settings", "Settings", 26, 202, 308, false);
        MenuOption(menu.transform, "Option_Credits", "Credits", 26, 260, 308, false);
        MenuOption(menu.transform, "Option_Exit", "Exit", 26, 318, 308, false);

        var newGameView = Panel(root, "MainMenu_NewGame_View", 540, 284, 560, 424, new Color32(245, 248, 243, 245));
        Label(newGameView.transform, "Title", "New Journey", 28, 26, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(newGameView.transform, "Body", "Start in Oak Town, choose a region path and set your opening battle rules before the first route.", 28, 78, 480, 58, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(newGameView.transform, "Preview_01", "Region", "Kanto", 28, 174, new Color32(67, 123, 133, 255));
        SmallCard(newGameView.transform, "Preview_02", "Rules", "Setup next", 28, 226, new Color32(119, 104, 168, 255));
        SmallCard(newGameView.transform, "Preview_03", "Partner", "Choose later", 28, 278, new Color32(75, 151, 103, 255));

        var continueView = Panel(root, "MainMenu_Continue_View", 540, 284, 560, 424, new Color32(245, 248, 243, 245));
        Label(continueView.transform, "Title", "Continue", 28, 26, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(continueView.transform, "Save_01", "Oak Town", "Day 3 / Route 1 survey active", 28, 88, 500, new Color32(67, 123, 133, 255));
        ListRowWide(continueView.transform, "Save_02", "Party", "Cyndaquil, Buizel", 28, 170, 500, new Color32(214, 122, 74, 255));
        continueView.SetActive(false);

        var loadView = Panel(root, "MainMenu_LoadGame_View", 540, 284, 560, 424, new Color32(245, 248, 243, 245));
        Label(loadView.transform, "Title", "Load Game", 28, 26, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(loadView.transform, "Slot_01", "Slot 1", "Oak Town / Day 3 / 4 badges", 28, 88, 500, new Color32(91, 130, 190, 255));
        ListRowWide(loadView.transform, "Slot_02", "Slot 2", "New Bark trip / Day 12 / Johto pass", 28, 170, 500, new Color32(75, 151, 103, 255));
        loadView.SetActive(false);

        var settingsView = Panel(root, "MainMenu_Settings_View", 540, 284, 560, 424, new Color32(245, 248, 243, 245));
        Label(settingsView.transform, "Title", "Settings", 28, 26, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        SmallCard(settingsView.transform, "Setting_01", "Text Speed", "Normal", 28, 96, new Color32(91, 130, 190, 255));
        SmallCard(settingsView.transform, "Setting_02", "Battle Mode", "Ask", 28, 148, new Color32(119, 104, 168, 255));
        SmallCard(settingsView.transform, "Setting_03", "Autosave", "On", 28, 200, new Color32(75, 151, 103, 255));
        settingsView.SetActive(false);

        var creditsView = Panel(root, "MainMenu_Credits_View", 540, 284, 560, 424, new Color32(245, 248, 243, 245));
        Label(creditsView.transform, "Title", "Credits", 28, 26, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(creditsView.transform, "Body", "A personal Pokemon-inspired project built for experiments, routes, companions and strange little systems.", 28, 84, 480, 80, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        creditsView.SetActive(false);

        var exitView = Panel(root, "MainMenu_Exit_View", 540, 284, 560, 424, new Color32(245, 248, 243, 245));
        Label(exitView.transform, "Title", "Exit", 28, 26, 300, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(exitView.transform, "Body", "Save before leaving if autosave is disabled.", 28, 84, 480, 40, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        exitView.SetActive(false);
    }

    static void BuildNewGameScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(232, 239, 236, 255));
        HeaderBlock(root, "Header", "New Game", "Choose your opening route, style and starting rules", 40, 32, 1840, 82, new Color32(67, 101, 91, 255));

        var flow = Panel(root, "NewGame_Flow", 52, 150, 360, 780, new Color32(255, 255, 255, 255));
        Label(flow.transform, "Title", "Setup", 24, 22, 200, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        MenuOption(flow.transform, "Step_Trainer", "Trainer", 24, 82, 294, true);
        MenuOption(flow.transform, "Step_Region", "Region", 24, 140, 294, false);
        MenuOption(flow.transform, "Step_PlayStyle", "Play Style", 24, 198, 294, false);
        MenuOption(flow.transform, "Step_Rules", "Battle Rules", 24, 256, 294, false);
        SmallCard(flow.transform, "Summary_01", "Name", "Eren", 24, 360, new Color32(67, 123, 133, 255));
        SmallCard(flow.transform, "Summary_02", "Region", "Kanto", 24, 412, new Color32(91, 130, 190, 255));
        SmallCard(flow.transform, "Summary_03", "Style", "Trainer", 24, 464, new Color32(75, 151, 103, 255));

        var trainerView = Panel(root, "NewGame_Trainer_View", 452, 150, 1368, 780, new Color32(248, 251, 246, 255));
        Label(trainerView.transform, "Title", "Trainer", 34, 30, 300, 36, 26, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Panel(trainerView.transform, "Trainer_Portrait", 34, 100, 250, 330, new Color32(207, 221, 215, 255));
        SmallCard(trainerView.transform, "Name_Field", "Name", "Eren", 330, 110, new Color32(67, 123, 133, 255));
        SmallCard(trainerView.transform, "Pronoun_Field", "Profile", "Custom", 330, 162, new Color32(91, 130, 190, 255));
        SmallCard(trainerView.transform, "Partner_Field", "Partner", "Choose in lab", 330, 214, new Color32(214, 153, 70, 255));
        Label(trainerView.transform, "Body", "Trainer details stay light at the start. Deeper customization can open later when the wardrobe and sprite assets are ready.", 330, 290, 820, 72, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);

        var regionView = Panel(root, "NewGame_Region_View", 452, 150, 1368, 780, new Color32(248, 251, 246, 255));
        BuildNewGameRegionPanel(regionView.transform);
        regionView.SetActive(false);

        var playStyleView = Panel(root, "NewGame_PlayStyle_View", 452, 150, 1368, 780, new Color32(248, 251, 246, 255));
        Label(playStyleView.transform, "Title", "Play Style", 34, 30, 300, 36, 26, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(playStyleView.transform, "Style_Trainer", "Trainer", "Battles, badges, tournaments and rivals", 34, 100, 620, new Color32(196, 111, 105, 255));
        ListRowWide(playStyleView.transform, "Style_Researcher", "Researcher", "Field notes, professor tasks and sightings", 34, 182, 620, new Color32(119, 104, 168, 255));
        ListRowWide(playStyleView.transform, "Style_Caretaker", "Caretaker", "Pokemon care, ranch tasks and partner trust", 34, 264, 620, new Color32(75, 151, 103, 255));
        ListRowWide(playStyleView.transform, "Style_FreeRoute", "Free Route", "Mixed play with no strict opening path", 34, 346, 620, new Color32(91, 130, 190, 255));
        var styleDetail = Panel(playStyleView.transform, "Selected_PlayStyle_View", 710, 100, 560, 326, new Color32(255, 255, 255, 255));
        Label(styleDetail.transform, "Title", "Trainer Route", 24, 22, 300, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(styleDetail.transform, "Body", "Oak gives the first route challenge, battle rules are asked next, and early trainer rumors become more common.", 24, 78, 478, 76, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(styleDetail.transform, "Bonus_01", "Start", "Oak Town", 24, 190, new Color32(67, 123, 133, 255));
        SmallCard(styleDetail.transform, "Bonus_02", "Focus", "Battles", 24, 242, new Color32(196, 111, 105, 255));
        playStyleView.SetActive(false);

        var rulesView = Panel(root, "NewGame_Rules_View", 452, 150, 1368, 780, new Color32(248, 251, 246, 255));
        Label(rulesView.transform, "Title", "Battle Rules", 34, 30, 300, 36, 26, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(rulesView.transform, "Body", "Opening rules are picked in a separate small setup screen before gameplay starts.", 34, 92, 760, 44, 18, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(rulesView.transform, "Rule_01", "Party", "3 Pokemon", 34, 170, new Color32(91, 130, 190, 255));
        SmallCard(rulesView.transform, "Rule_02", "Powers", "Mega only", 34, 222, new Color32(188, 139, 82, 255));
        SmallCard(rulesView.transform, "Rule_03", "Mode", "Ask before battle", 34, 274, new Color32(119, 104, 168, 255));
        rulesView.SetActive(false);
    }

    static void BuildNewGameRegionPanel(Transform root) {
        Label(root, "Title", "Starting Region", 34, 30, 360, 36, 26, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        var list = Panel(root, "Region_Options", 34, 100, 300, 360, new Color32(255, 255, 255, 255));
        MenuOption(list.transform, "Region_Kanto", "Kanto", 24, 28, 242, true);
        MenuOption(list.transform, "Region_Johto", "Johto", 24, 86, 242, false);
        MenuOption(list.transform, "Region_Sinnoh", "Sinnoh", 24, 144, 242, false);
        MenuOption(list.transform, "Region_Galar", "Galar", 24, 202, 242, false);
        MenuOption(list.transform, "Region_Alola", "Alola", 24, 260, 242, false);

        var kanto = Panel(root, "NewGame_Region_Kanto_View", 370, 100, 820, 360, new Color32(255, 255, 255, 255));
        BuildRegionStartCard(kanto.transform, "Kanto", "Oak Town", "Balanced opening with research, routes, simple markets and early trainer battles.", "Junior Cup", "Bus routes", "Classic gyms");

        var johto = Panel(root, "NewGame_Region_Johto_View", 370, 100, 820, 360, new Color32(255, 255, 255, 255));
        BuildRegionStartCard(johto.transform, "Johto", "New Bark", "Crafting, apricorn shops, old roads and calmer travel pacing.", "Apricorn fair", "Train station", "Double battles");
        johto.SetActive(false);

        var sinnoh = Panel(root, "NewGame_Region_Sinnoh_View", 370, 100, 820, 360, new Color32(255, 255, 255, 255));
        BuildRegionStartCard(sinnoh.transform, "Sinnoh", "Twinleaf", "Expeditions, mountain prep, fossil boards and stronger weather pressure.", "Fossil board", "Mountain rides", "Weather rules");
        sinnoh.SetActive(false);

        var galar = Panel(root, "NewGame_Region_Galar_View", 370, 100, 820, 360, new Color32(255, 255, 255, 255));
        BuildRegionStartCard(galar.transform, "Galar", "Postwick", "Stadium schedule, sponsors, public rankings and arena-specific power rules.", "Minor League", "Rail pass", "Gigantamax arenas");
        galar.SetActive(false);

        var alola = Panel(root, "NewGame_Region_Alola_View", 370, 100, 820, 360, new Color32(255, 255, 255, 255));
        BuildRegionStartCard(alola.transform, "Alola", "Iki Town", "Island trials, rotating ferry routes, local care tasks and Z-Move rewards.", "Trial helpers", "Ferry routes", "Z-Move trials");
        alola.SetActive(false);
    }

    static void BuildRegionStartCard(Transform root, string title, string town, string body, string eventName, string travelName, string ruleName) {
        Label(root, "Title", title, 24, 22, 280, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(root, "Town", town, 24, 74, new Color32(67, 123, 133, 255));
        Label(root, "Body", body, 24, 122, 720, 62, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(root, "Event", "Event", eventName, 24, 220, new Color32(214, 153, 70, 255));
        SmallCard(root, "Travel", "Travel", travelName, 24, 272, new Color32(91, 130, 190, 255));
        SmallCard(root, "Rules", "Rules", ruleName, 350, 220, new Color32(119, 104, 168, 255));
    }

    static void BuildGameStartBattleRulesSetupScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(23, 28, 32, 120));
        var modal = Panel(root, "RulesSetup_Modal", 520, 164, 880, 724, new Color32(248, 251, 246, 255));
        Label(modal.transform, "Title", "Opening Battle Rules", 34, 28, 420, 36, 27, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(modal.transform, "Subtitle", "These rules set the default feel of the journey.", 34, 72, 560, 24, 16, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.MiddleLeft);
        Tab(modal.transform, "Step_01_Tab", "Pokemon", 34, 118, 132, true);
        Tab(modal.transform, "Step_02_Tab", "Powers", 178, 118, 132, false);
        Tab(modal.transform, "Step_03_Tab", "Challenge", 322, 118, 150, false);

        var pokemonStep = Panel(modal.transform, "Step_01_PokemonCount_View", 34, 172, 812, 380, new Color32(255, 255, 255, 255));
        Label(pokemonStep.transform, "Title", "How many Pokemon?", 24, 22, 360, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(pokemonStep.transform, "Choice_One", "1 Pokemon", "Partner-focused challenge start", 24, 80, 744, new Color32(214, 153, 70, 255));
        ListRowWide(pokemonStep.transform, "Choice_Three", "3 Pokemon", "Balanced rules for towns and gyms", 24, 162, 744, new Color32(67, 123, 133, 255));
        ListRowWide(pokemonStep.transform, "Choice_Six", "6 Pokemon", "Classic full party journey", 24, 244, 744, new Color32(91, 130, 190, 255));

        var powersStep = Panel(modal.transform, "Step_02_PowerMechanics_View", 34, 172, 812, 380, new Color32(255, 255, 255, 255));
        Label(powersStep.transform, "Title", "Special powers", 24, 22, 360, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(powersStep.transform, "Power_Mega", "Mega Evolution", "Allowed with trainer charge and required item", 24, 80, 744, new Color32(188, 139, 82, 255));
        ListRowWide(powersStep.transform, "Power_ZMove", "Z-Move", "Locked until Alola trials or rare rewards", 24, 162, 744, new Color32(119, 104, 168, 255));
        ListRowWide(powersStep.transform, "Power_Gigantamax", "Gigantamax", "Allowed only where arena rules permit it", 24, 244, 744, new Color32(196, 111, 105, 255));
        powersStep.SetActive(false);

        var challengeStep = Panel(modal.transform, "Step_03_ChallengeRules_View", 34, 172, 812, 380, new Color32(255, 255, 255, 255));
        Label(challengeStep.transform, "Title", "Challenge rules", 24, 22, 360, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(challengeStep.transform, "Rule_Type", "Type Limit", "Off by default, trainer and events may override", 24, 80, 744, new Color32(92, 151, 178, 255));
        ListRowWide(challengeStep.transform, "Rule_Turns", "Turn Limit", "Off by default, tournaments may set strict limits", 24, 162, 744, new Color32(214, 153, 70, 255));
        ListRowWide(challengeStep.transform, "Rule_Mode", "Battle Mode", "Ask at start, then use selected default", 24, 244, 744, new Color32(119, 104, 168, 255));
        challengeStep.SetActive(false);

        Panel(modal.transform, "Footer_Divider", 34, 586, 812, 2, new Color32(190, 200, 196, 255));
        Label(modal.transform, "Back_Button", "Back", 34, 622, 120, 38, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        Panel(modal.transform, "Back_Button_Background", 34, 622, 120, 38, new Color32(92, 101, 104, 255)).transform.SetAsFirstSibling();
        Label(modal.transform, "Next_Button", "Next", 700, 622, 146, 38, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        Panel(modal.transform, "Next_Button_Background", 700, 622, 146, 38, new Color32(76, 98, 80, 255)).transform.SetAsFirstSibling();
    }

    static void BuildPauseMenuScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(20, 26, 30, 150));
        var shell = Panel(root, "PauseMenu_Shell", 362, 150, 1196, 760, new Color32(247, 250, 245, 248));
        Label(shell.transform, "Title", "Paused", 34, 28, 240, 38, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(shell.transform, "Clock", "Day 3 / 15:42 / Oak Town", 820, 36, 320, 24, 16, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.MiddleRight);

        var menu = Panel(shell.transform, "PauseMenu_Options", 34, 96, 300, 586, new Color32(255, 255, 255, 255));
        MenuOption(menu.transform, "Option_Resume", "Resume", 24, 28, 242, true);
        MenuOption(menu.transform, "Option_PokeNav", "PokeNav", 24, 86, 242, false);
        MenuOption(menu.transform, "Option_Bag", "Bag", 24, 144, 242, false);
        MenuOption(menu.transform, "Option_Party", "Pokemon Party", 24, 202, 242, false);
        MenuOption(menu.transform, "Option_Tasks", "Tasks", 24, 260, 242, false);
        MenuOption(menu.transform, "Option_Settings", "Settings", 24, 318, 242, false);
        MenuOption(menu.transform, "Option_SaveLoad", "Save / Load", 24, 376, 242, false);
        MenuOption(menu.transform, "Option_Exit", "Exit to Menu", 24, 434, 242, false);

        var resumeView = Panel(shell.transform, "Pause_Resume_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(resumeView.transform, "Resume", "Return to Route 1 beside the east sign.", "Current", "Route 1", "Next", "Talk to Mira", "Status", "Clear skies");

        var navView = Panel(shell.transform, "Pause_PokeNav_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(navView.transform, "PokeNav", "Open map, Pokedex notes, regional info and social rumors.", "Map", "Route 1", "Rumors", "1 active", "Guide", "Research");
        navView.SetActive(false);

        var bagView = Panel(shell.transform, "Pause_Bag_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(bagView.transform, "Bag", "Check items, recipes, tools and shop purchases.", "Items", "14", "Recipes", "2 learned", "Tools", "Fishing rod");
        bagView.SetActive(false);

        var partyView = Panel(shell.transform, "Pause_Party_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(partyView.transform, "Pokemon Party", "Review party health, needs, mood and partner care notes.", "Partner", "Cyndaquil", "Care", "Hungry soon", "Mood", "Calm");
        partyView.SetActive(false);

        var tasksView = Panel(shell.transform, "Pause_Tasks_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(tasksView.transform, "Tasks", "Track police tasks, professor assignments and local activities.", "Professor", "Route survey", "Police", "Lost parcel", "Market", "Sale note");
        tasksView.SetActive(false);

        var settingsView = Panel(shell.transform, "Pause_Settings_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(settingsView.transform, "Settings", "Adjust gameplay preferences without leaving the current session.", "Text", "Normal", "Autosave", "On", "Battle", "Ask");
        settingsView.SetActive(false);

        var saveLoadView = Panel(shell.transform, "Pause_SaveLoad_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(saveLoadView.transform, "Save / Load", "Save the current route state or load another slot.", "Slot 1", "Oak Town", "Autosave", "15:40", "Backup", "Day 2");
        saveLoadView.SetActive(false);

        var exitView = Panel(shell.transform, "Pause_Exit_View", 370, 96, 782, 586, new Color32(248, 251, 246, 255));
        BuildPauseDetailPanel(exitView.transform, "Exit to Menu", "Return to the main menu after saving or discarding current changes.", "Save", "Recommended", "Autosave", "On", "Destination", "Main Menu");
        exitView.SetActive(false);
    }

    static void BuildPauseDetailPanel(Transform root, string title, string body, string row1Title, string row1Body, string row2Title, string row2Body, string row3Title, string row3Body) {
        Label(root, "Title", title, 26, 24, 360, 34, 25, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(root, "Body", body, 26, 82, 680, 54, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(root, "Info_01", row1Title, row1Body, 26, 174, new Color32(67, 123, 133, 255));
        SmallCard(root, "Info_02", row2Title, row2Body, 26, 226, new Color32(119, 104, 168, 255));
        SmallCard(root, "Info_03", row3Title, row3Body, 26, 278, new Color32(75, 151, 103, 255));
    }

    static void BuildPokeNavScreen(Transform root) {
        var mapManager = root.gameObject.AddComponent<PokeNavMapUIManager>();

        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(235, 241, 237, 255));
        HeaderBlock(root, "Header", "PokeNav", "Route 1 clear skies / Oak Town market sale / 3 new sightings", 40, 32, 1840, 82, new Color32(43, 91, 98, 255));
        Tab(root, "Tab_Map", "Map", 52, 138, 132, true);
        Tab(root, "Tab_Pokedex", "Pokedex", 196, 138, 132, false);
        Tab(root, "Tab_Regions", "Regions", 340, 138, 132, false);
        Tab(root, "Tab_Social", "Social", 484, 138, 132, false);
        Tab(root, "Tab_Guide", "Guide", 628, 138, 132, false);

        var mapPanel = Panel(root, "PokeNav_Map_View", 52, 194, 1768, 790, new Color32(246, 249, 244, 255));
        BuildPokeNavMapPanel(mapPanel.transform, mapManager);

        var pokedexPanel = Panel(root, "PokeNav_Pokedex_View", 52, 194, 1768, 790, new Color32(246, 249, 244, 255));
        BuildPokeNavPokedexPanel(pokedexPanel.transform);
        pokedexPanel.SetActive(false);

        var regionsPanel = Panel(root, "PokeNav_Regions_View", 52, 194, 1768, 790, new Color32(246, 249, 244, 255));
        BuildPokeNavRegionsPanel(regionsPanel.transform);
        regionsPanel.SetActive(false);

        var socialPanel = Panel(root, "PokeNav_Social_View", 52, 194, 1768, 790, new Color32(246, 249, 244, 255));
        BuildPokeNavSocialPanel(socialPanel.transform);
        socialPanel.SetActive(false);

        var guidePanel = Panel(root, "PokeNav_Guide_View", 52, 194, 1768, 790, new Color32(246, 249, 244, 255));
        BuildPokeNavGuidePanel(guidePanel.transform);
        guidePanel.SetActive(false);
    }

    static void BuildPokeNavMapPanel(Transform root, PokeNavMapUIManager mapManager) {
        var map = Panel(root, "World_Map", 24, 24, 1190, 710, new Color32(203, 219, 204, 255));
        Panel(map.transform, "Water_North", 42, 36, 360, 192, new Color32(92, 151, 178, 255));
        Panel(map.transform, "Whisper_Woods", 74, 292, 382, 254, new Color32(91, 143, 93, 255));
        Panel(map.transform, "Oak_Town", 682, 126, 316, 224, new Color32(186, 178, 146, 255));
        Panel(map.transform, "Route_1_Field", 568, 458, 420, 160, new Color32(165, 196, 143, 255));
        Route(map.transform, "Route_To_Town", 348, 428, 456, 10, -17);
        Route(map.transform, "Town_Route", 750, 412, 252, 10, 11);
        Route(map.transform, "South_Route", 568, 540, 424, 10, 0);
        Marker(map.transform, "Marker_Player", 344, 414, new Color32(255, 238, 118, 255), "YOU");
        Marker(map.transform, "Marker_Event", 790, 220, new Color32(239, 101, 96, 255), "!");
        Marker(map.transform, "Marker_Pokemon", 874, 532, new Color32(101, 187, 129, 255), "P");
        Marker(map.transform, "Marker_Shop", 950, 196, new Color32(116, 181, 232, 255), "$");
        Label(map.transform, "Map_Label_Town", "Oak Town", 686, 142, 200, 28, 18, FontStyle.Bold, new Color32(42, 45, 48, 255), TextAnchor.MiddleLeft);
        Label(map.transform, "Map_Label_Route", "Route 1", 604, 474, 180, 28, 18, FontStyle.Bold, new Color32(42, 45, 48, 255), TextAnchor.MiddleLeft);
        Label(map.transform, "Map_Label_Forest", "Whisper Woods", 104, 314, 260, 28, 18, FontStyle.Bold, new Color32(42, 45, 48, 255), TextAnchor.MiddleLeft);
        var runtimeLayer = Panel(map.transform, "Runtime_World_Map_Marker_Layer", 0, 0, 1190, 710, new Color32(0, 0, 0, 0));
        runtimeLayer.GetComponent<Image>().raycastTarget = false;
        var runtimePlayer = Marker(runtimeLayer.transform, "Runtime_Player_Position", 586, 346, new Color32(255, 238, 118, 255), "");
        runtimePlayer.SetActive(false);

        var intel = Panel(root, "Location_Intel", 1242, 24, 502, 710, new Color32(248, 251, 246, 255));
        var intelTitleText = Label(intel.transform, "Intel_Title", "Selected marker", 24, 24, 420, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft).GetComponent<Text>();
        var intelMetaText = Label(intel.transform, "Intel_Meta", "Category / scene / state", 24, 60, 438, 22, 13, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.MiddleLeft).GetComponent<Text>();
        var summaryText = Label(intel.transform, "Intel_Summary", "Markers 0 / Favorites 0 / Feed 0", 24, 84, 438, 22, 13, FontStyle.Bold, new Color32(67, 123, 133, 255), TextAnchor.MiddleLeft).GetComponent<Text>();
        var activeTargetText = Label(intel.transform, "Intel_Target", "Target: none", 24, 108, 438, 22, 13, FontStyle.Bold, new Color32(188, 139, 82, 255), TextAnchor.MiddleLeft).GetComponent<Text>();
        var row1 = ListRow(intel.transform, "Intel_Row_01", "Pokemon sighting", "Buizel near water grass", 24, 144, new Color32(92, 151, 178, 255));
        var row2 = ListRow(intel.transform, "Intel_Row_02", "Trainer activity", "Mira accepts casual 3v3 rules", 24, 222, new Color32(196, 111, 105, 255));
        var row3 = ListRow(intel.transform, "Intel_Row_03", "Shop note", "Potion sale until evening", 24, 300, new Color32(214, 153, 70, 255));
        var row4 = ListRow(intel.transform, "Intel_Row_04", "Rumor", "Rare nest reported north", 24, 378, new Color32(119, 104, 168, 255));
        AddButtonTarget(row1);
        AddButtonTarget(row2);
        AddButtonTarget(row3);
        AddButtonTarget(row4);
        var intelBodyText = Label(intel.transform, "Intel_Body", "Soft grass borders the pond. Water Pokemon surface near morning rain, and Mira accepts friendly matches beside the east sign.", 24, 468, 438, 76, 16, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft).GetComponent<Text>();
        var distanceText = Label(intel.transform, "Intel_Distance", "Distance: unknown", 24, 552, 438, 22, 13, FontStyle.Bold, new Color32(91, 130, 190, 255), TextAnchor.MiddleLeft).GetComponent<Text>();
        var feedbackText = Label(intel.transform, "Intel_Feedback", "Select a row, then use controller actions from buttons or input bindings.", 24, 578, 438, 34, 13, FontStyle.Normal, new Color32(96, 78, 47, 255), TextAnchor.UpperLeft).GetComponent<Text>();
        SmallCard(intel.transform, "Guide_01", "Research Lead", "Talk to Oak's aide", 24, 626, new Color32(67, 123, 133, 255));
        SmallCard(intel.transform, "Guide_02", "Transport", "Bus route unlocked", 24, 678, new Color32(95, 122, 87, 255));

        var mapController = map.AddComponent<MapViewportUIController>();
        mapController.ConfigureView(MapViewportMode.AutoBounds, sameSceneOnly: false, labelsVisible: true, minimapRadius: 20f);
        mapController.BindUI(runtimeLayer.GetComponent<RectTransform>(), runtimePlayer.GetComponent<RectTransform>(), null, null, null, null, null, null);

        var mapViewController = root.gameObject.AddComponent<PokeNavMapViewController>();
        mapViewController.BindUI(
            mapManager,
            mapController,
            intelTitleText,
            intelMetaText,
            intelBodyText,
            distanceText,
            activeTargetText,
            summaryText,
            feedbackText,
            GetText(row1, "Intel_Row_01_Title"),
            GetText(row2, "Intel_Row_02_Title"),
            GetText(row3, "Intel_Row_03_Title"),
            GetText(row4, "Intel_Row_04_Title"));
    }

    static void BuildPokeNavPokedexPanel(Transform root) {
        var list = Panel(root, "Pokedex_List", 24, 24, 500, 710, new Color32(255, 255, 255, 255));
        Label(list.transform, "Title", "Pokedex", 24, 24, 240, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Tab(list.transform, "Filter_All", "All", 24, 74, 96, true);
        Tab(list.transform, "Filter_Seen", "Seen", 132, 74, 96, false);
        Tab(list.transform, "Filter_Caught", "Caught", 240, 74, 112, false);

        var allFilter = Panel(list.transform, "Pokedex_All_Filter_View", 24, 124, 438, 472, new Color32(255, 255, 255, 0));
        ListRowWide(allFilter.transform, "Entry_001", "Buizel", "Seen 4 / Caught 1 / Route 1 pond", 0, 0, 438, new Color32(92, 151, 178, 255));
        ListRowWide(allFilter.transform, "Entry_002", "Cyndaquil", "Partner / High trust", 0, 82, 438, new Color32(214, 122, 74, 255));
        ListRowWide(allFilter.transform, "Entry_003", "Squirtle", "Rumored near Oak Town", 0, 164, 438, new Color32(88, 140, 190, 255));
        ListRowWide(allFilter.transform, "Entry_004", "Tranquill", "Unknown habitat", 0, 246, 438, new Color32(150, 160, 168, 255));

        var seenFilter = Panel(list.transform, "Pokedex_Seen_Filter_View", 24, 124, 438, 472, new Color32(255, 255, 255, 0));
        ListRowWide(seenFilter.transform, "Seen_Entry_001", "Buizel", "Observed at water grass / Morning", 0, 0, 438, new Color32(92, 151, 178, 255));
        ListRowWide(seenFilter.transform, "Seen_Entry_002", "Squirtle", "Rumor-linked sighting / Oak Town", 0, 82, 438, new Color32(88, 140, 190, 255));
        ListRowWide(seenFilter.transform, "Seen_Entry_003", "Tranquill", "Flyover record / Unknown habitat", 0, 164, 438, new Color32(150, 160, 168, 255));
        seenFilter.SetActive(false);

        var caughtFilter = Panel(list.transform, "Pokedex_Caught_Filter_View", 24, 124, 438, 472, new Color32(255, 255, 255, 0));
        ListRowWide(caughtFilter.transform, "Caught_Entry_001", "Buizel", "Caught 1 / Care notes unlocked", 0, 0, 438, new Color32(92, 151, 178, 255));
        ListRowWide(caughtFilter.transform, "Caught_Entry_002", "Cyndaquil", "Partner / High trust / Needs rest", 0, 82, 438, new Color32(214, 122, 74, 255));
        caughtFilter.SetActive(false);

        var detail = Panel(root, "Pokedex_Detail", 552, 24, 1192, 710, new Color32(248, 251, 246, 255));
        Panel(detail.transform, "Pokemon_Portrait", 34, 42, 280, 240, new Color32(222, 232, 225, 255));
        Label(detail.transform, "Pokemon_Name", "Buizel", 352, 42, 320, 38, 28, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(detail.transform, "Knowledge_Level", "Knowledge: Familiar", 352, 96, new Color32(67, 123, 133, 255));
        InfoPill(detail.transform, "Capture_State", "Caught", 544, 96, new Color32(75, 151, 103, 255));
        Label(detail.transform, "Pokemon_Notes", "Often plays near shallow water and reacts quickly to splashing sounds. Likes clean ponds, soft berries and calm partners.", 352, 154, 730, 88, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(detail.transform, "Habitat_01", "Habitat", "Route 1 pond", 352, 280, new Color32(92, 151, 178, 255));
        SmallCard(detail.transform, "Research_01", "Research", "2/5 observations", 352, 332, new Color32(119, 104, 168, 255));
        SmallCard(detail.transform, "Care_01", "Care", "Likes clean water", 352, 384, new Color32(75, 151, 103, 255));
    }

    static void BuildPokeNavRegionsPanel(Transform root) {
        var regionList = Panel(root, "Region_List", 24, 24, 420, 710, new Color32(255, 255, 255, 255));
        Label(regionList.transform, "Title", "Regions", 24, 24, 240, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        NavItem(regionList.transform, "Region_Kanto", "Kanto", 24, 82, true);
        NavItem(regionList.transform, "Region_Johto", "Johto", 24, 140, false);
        NavItem(regionList.transform, "Region_Sinnoh", "Sinnoh", 24, 198, false);
        NavItem(regionList.transform, "Region_Galar", "Galar", 24, 256, false);
        NavItem(regionList.transform, "Region_Alola", "Alola", 24, 314, false);

        var kantoView = Panel(root, "Region_Kanto_View", 472, 24, 740, 710, new Color32(248, 251, 246, 255));
        BuildRegionDetailPanel(kantoView.transform, "Kanto Overview", "Current region", "League open", "Oak Town is calm this week. Route surveys are open, local markets are stocked and the junior cup starts tomorrow.", "Oak Town Junior Cup starts tomorrow", "Bus and ferry routes available", "Casual 3v3 common in towns");

        var johtoView = Panel(root, "Region_Johto_View", 472, 24, 740, 710, new Color32(248, 251, 246, 255));
        BuildRegionDetailPanel(johtoView.transform, "Johto Overview", "Travel unlocked", "League pending", "Old roads connect quiet towns, apricorn workshops and shrine paths. Local trainers favor patient double battles.", "Apricorn fair begins at sunset", "Train route requires station pass", "Double battle requests are common");
        johtoView.SetActive(false);

        var sinnohView = Panel(root, "Region_Sinnoh_View", 472, 24, 740, 710, new Color32(248, 251, 246, 255));
        BuildRegionDetailPanel(sinnohView.transform, "Sinnoh Overview", "Cold routes", "Badge gates", "Mountain paths are harsh after dusk. Fossil researchers need helpers, and several routes require warm gear.", "Oreburgh research board refreshed", "Mountain ride route recommended", "Weather clauses appear in gyms");
        sinnohView.SetActive(false);

        var galarView = Panel(root, "Region_Galar_View", 472, 24, 740, 710, new Color32(248, 251, 246, 255));
        BuildRegionDetailPanel(galarView.transform, "Galar Overview", "Distant region", "Cup schedule", "Stadium towns run on schedules, sponsors and public rankings. Arena rules decide when Gigantamax is allowed.", "Minor League signups open", "Railway transfer requires ticket", "Gigantamax rules only in arenas");
        galarView.SetActive(false);

        var alolaView = Panel(root, "Region_Alola_View", 472, 24, 740, 710, new Color32(248, 251, 246, 255));
        BuildRegionDetailPanel(alolaView.transform, "Alola Overview", "Island travel", "Trial route", "Island trials rotate through local tasks, beach surveys and guardian traditions. Z-Move rewards are tied to trial progress.", "Trial scout looking for helpers", "Ferry route rotates daily", "Z-Move use tied to local trials");
        alolaView.SetActive(false);

        var travel = Panel(root, "Travel_Policies", 1240, 24, 504, 710, new Color32(255, 255, 255, 255));
        Label(travel.transform, "Title", "Travel Policy", 24, 24, 260, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        SmallCard(travel.transform, "Policy_01", "Full party", "Allowed", 24, 82, new Color32(75, 151, 103, 255));
        SmallCard(travel.transform, "Policy_02", "Challenge run", "Optional", 24, 134, new Color32(214, 153, 70, 255));
        SmallCard(travel.transform, "Policy_03", "Frontier", "Locked", 24, 186, new Color32(196, 111, 105, 255));
        Label(travel.transform, "Body", "Leaving for a new region can keep the full party or begin a local challenge with one partner. Locked routes need titles or tickets.", 24, 258, 438, 96, 16, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
    }

    static void BuildRegionDetailPanel(Transform root, string title, string travelState, string leagueState, string description, string eventText, string transportText, string ruleText) {
        Label(root, "Title", title, 24, 24, 360, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(root, "Travel_State", travelState, 24, 76, new Color32(75, 151, 103, 255));
        InfoPill(root, "League_State", leagueState, 176, 76, new Color32(91, 130, 190, 255));
        Label(root, "Description", description, 24, 132, 660, 78, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        ListRowWide(root, "Activity_01", "Regional Event", eventText, 24, 246, 660, new Color32(214, 153, 70, 255));
        ListRowWide(root, "Activity_02", "Transport", transportText, 24, 328, 660, new Color32(67, 123, 133, 255));
        ListRowWide(root, "Activity_03", "Battle Rule", ruleText, 24, 410, 660, new Color32(119, 104, 168, 255));
    }

    static void BuildPokeNavSocialPanel(Transform root) {
        var feed = Panel(root, "Social_Feed", 24, 24, 780, 710, new Color32(255, 255, 255, 255));
        Label(feed.transform, "Title", "Social / Rumors", 24, 24, 300, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRowWide(feed.transform, "Post_01", "Mira", "Saw a Buizel near Route 1 pond. Might be nesting.", 24, 82, 714, new Color32(92, 151, 178, 255));
        ListRowWide(feed.transform, "Post_02", "Oak Aide", "Research volunteers needed for morning survey.", 24, 164, 714, new Color32(119, 104, 168, 255));
        ListRowWide(feed.transform, "Post_03", "Market Board", "Potion Brand B discounted until evening.", 24, 246, 714, new Color32(214, 153, 70, 255));
        ListRowWide(feed.transform, "Post_04", "Police Notice", "Lost package reported near the east sign.", 24, 328, 714, new Color32(196, 111, 105, 255));

        var rumor = Panel(root, "Rumor_Detail", 836, 24, 908, 710, new Color32(248, 251, 246, 255));
        Label(rumor.transform, "Title", "Selected Rumor", 24, 24, 300, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(rumor.transform, "Scope", "Village scope", 24, 76, new Color32(67, 123, 133, 255));
        InfoPill(rumor.transform, "Decay", "Expires in 1 day", 166, 76, new Color32(214, 153, 70, 255));
        InfoPill(rumor.transform, "Reliability", "Medium reliability", 338, 76, new Color32(91, 130, 190, 255));
        Label(rumor.transform, "Body", "The nest rumor is spreading around Oak Town. Three locals heard it from different sources; reliability drops tomorrow if no one confirms it.", 24, 138, 820, 86, 17, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(rumor.transform, "Impact_01", "Map", "Route 1 marker", 24, 260, new Color32(92, 151, 178, 255));
        SmallCard(rumor.transform, "Impact_02", "NPCs", "3 heard this", 24, 312, new Color32(119, 104, 168, 255));
        SmallCard(rumor.transform, "Impact_03", "Risk", "None", 24, 364, new Color32(75, 151, 103, 255));
    }

    static void BuildPokeNavGuidePanel(Transform root) {
        var categories = Panel(root, "Guide_Categories", 24, 24, 420, 710, new Color32(255, 255, 255, 255));
        Label(categories.transform, "Title", "Guide", 24, 24, 220, 32, 24, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        NavItem(categories.transform, "Guide_Research", "Research", 24, 82, true);
        NavItem(categories.transform, "Guide_Care", "Pokemon Care", 24, 140, false);
        NavItem(categories.transform, "Guide_Transit", "Transit", 24, 198, false);
        NavItem(categories.transform, "Guide_Battle", "Battle Rules", 24, 256, false);
        NavItem(categories.transform, "Guide_Farming", "Area Activities", 24, 314, false);

        var researchArticle = Panel(root, "Guide_Research_View", 472, 24, 1272, 710, new Color32(248, 251, 246, 255));
        BuildGuideArticlePanel(researchArticle.transform, "Research Basics", "Research tasks unlock field notes, sightings and professor trust. Some surveys need a temporary volunteer title.", "Requirement", "Research Volunteer title or professor assignment", "Where", "Oak Lab, Route 1 survey points, field stations", "Rewards", "PokeNav entries, region info, recipes and titles");

        var careArticle = Panel(root, "Guide_Care_View", 472, 24, 1272, 710, new Color32(248, 251, 246, 255));
        BuildGuideArticlePanel(careArticle.transform, "Pokemon Care", "Care routines improve trust, recovery and mood. Dirty, hungry or tired Pokemon may refuse demanding activities.", "Requirement", "Care tool, stable access or companion permission", "Where", "Care yards, ranch zones, camps and safe houses", "Rewards", "Trust, recovery bonuses, behavior notes and evolution hints");
        careArticle.SetActive(false);

        var transitArticle = Panel(root, "Guide_Transit_View", 472, 24, 1272, 710, new Color32(248, 251, 246, 255));
        BuildGuideArticlePanel(transitArticle.transform, "Transit", "Transit routes open after discovery, tickets or local permission. Some rides trigger route events and character meetings.", "Requirement", "Ticket, title, route discovery or ride-capable Pokemon", "Where", "Stations, docks, airports, ride posts and region gates", "Rewards", "Fast travel, new character meetings and route events");
        transitArticle.SetActive(false);

        var battleArticle = Panel(root, "Guide_Battle_View", 472, 24, 1272, 710, new Color32(248, 251, 246, 255));
        BuildGuideArticlePanel(battleArticle.transform, "Battle Rules", "Friendly trainers may negotiate party size, type limits, turn limits and allowed power mechanics before battle.", "Requirement", "Trainer agreement, facility rules or tournament bracket", "Where", "Gyms, frontier desks, police tasks and trainer encounters", "Rewards", "Badges, titles, charges, rankings and special invitations");
        battleArticle.SetActive(false);

        var farmingArticle = Panel(root, "Guide_AreaActivities_View", 472, 24, 1272, 710, new Color32(248, 251, 246, 255));
        BuildGuideArticlePanel(farmingArticle.transform, "Area Activities", "Farming, mining, fishing and care tasks only work in marked activity zones. Local permissions decide what can be done.", "Requirement", "Area permission, required tool or local assignment", "Where", "Tagged activity zones, farms, mines, docks and ranch plots", "Rewards", "Ingredients, recipes, materials, care progress and local reputation");
        farmingArticle.SetActive(false);
    }

    static void BuildGuideArticlePanel(Transform root, string title, string body, string row1Title, string row1Body, string row2Title, string row2Body, string row3Title, string row3Body) {
        Label(root, "Title", title, 24, 24, 420, 36, 26, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(root, "Body", body, 24, 86, 1110, 92, 18, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        ListRowWide(root, "Step_01", row1Title, row1Body, 24, 222, 1110, new Color32(119, 104, 168, 255));
        ListRowWide(root, "Step_02", row2Title, row2Body, 24, 304, 1110, new Color32(67, 123, 133, 255));
        ListRowWide(root, "Step_03", row3Title, row3Body, 24, 386, 1110, new Color32(75, 151, 103, 255));
    }

    static void BuildMiniMapScreen(Transform root) {
        Panel(root, "Transparent_Play_Backdrop", 0, 0, 1920, 1080, new Color32(0, 0, 0, 0));
        var mini = Panel(root, "MiniMap_Runtime_HUD", 1510, 40, 360, 292, new Color32(33, 39, 45, 230));
        var titleText = Label(mini.transform, "MiniMap_Title", "Minimap", 18, 16, 180, 28, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft).GetComponent<Text>();
        var modeText = Label(mini.transform, "Mode_Label", "Route 1", 164, 18, 172, 22, 13, FontStyle.Normal, new Color32(205, 216, 219, 255), TextAnchor.MiddleRight).GetComponent<Text>();

        var viewport = Panel(mini.transform, "MiniMap_Viewport_Mask", 22, 58, 316, 160, new Color32(216, 226, 217, 255));
        viewport.AddComponent<RectMask2D>();
        Panel(viewport.transform, "Area_Map_Layer_Placeholder", 0, 0, 316, 160, new Color32(188, 207, 188, 255));
        Panel(viewport.transform, "Road_H_Runtime_Layer", 48, 82, 220, 8, new Color32(120, 132, 116, 255));
        Panel(viewport.transform, "Road_V_Runtime_Layer", 154, 34, 8, 102, new Color32(120, 132, 116, 255));
        Panel(viewport.transform, "Fog_Or_Undiscovered_Overlay", 0, 0, 316, 160, new Color32(20, 26, 28, 28));
        var runtimeLayer = Panel(viewport.transform, "MiniMap_Runtime_Marker_Layer", 0, 0, 316, 160, new Color32(0, 0, 0, 0));
        runtimeLayer.GetComponent<Image>().raycastTarget = false;
        var playerMarker = Marker(runtimeLayer.transform, "Tracked_Player_Marker", 148, 74, new Color32(255, 238, 118, 255), "");
        var targetMarker = Marker(runtimeLayer.transform, "Tracked_Target_Marker", 238, 74, new Color32(239, 101, 96, 255), "");
        Marker(runtimeLayer.transform, "Tracked_NPC_Marker", 72, 108, new Color32(116, 181, 232, 255), "");
        var targetText = Label(mini.transform, "Target_Label", "Target: east sign", 22, 232, 180, 22, 14, FontStyle.Normal, new Color32(220, 228, 230, 255), TextAnchor.MiddleLeft).GetComponent<Text>();
        var zoomText = Label(mini.transform, "Zoom_Label", "Zoom 1.0x", 238, 232, 96, 22, 14, FontStyle.Normal, new Color32(220, 228, 230, 255), TextAnchor.MiddleRight).GetComponent<Text>();
        var nearbyText = Label(mini.transform, "Nearby_Label", "Nearby: Pond, East Sign, Mira", 22, 258, 312, 18, 11, FontStyle.Normal, new Color32(166, 178, 181, 255), TextAnchor.MiddleLeft).GetComponent<Text>();

        var mapController = mini.AddComponent<MapViewportUIController>();
        mapController.ConfigureView(MapViewportMode.MinimapFollowPlayer, sameSceneOnly: true, labelsVisible: false, minimapRadius: 14f);
        mapController.BindUI(runtimeLayer.GetComponent<RectTransform>(), playerMarker.GetComponent<RectTransform>(), targetMarker.GetComponent<RectTransform>(), titleText, modeText, targetText, zoomText, nearbyText);
    }

    static void BuildWorldFeedScreen(Transform root) {
        Panel(root, "Transparent_Play_Backdrop", 0, 0, 1920, 1080, new Color32(0, 0, 0, 0));
        var feed = Panel(root, "WorldFeed_Panel", 1420, 126, 460, 620, new Color32(37, 43, 48, 245));
        Label(feed.transform, "Feed_Title", "World Feed", 22, 20, 240, 32, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        FeedRow(feed.transform, "Feed_01", "15:40", "Found 1 Pokeball near Route 1.", 22, 76, new Color32(86, 156, 214, 255));
        FeedRow(feed.transform, "Feed_02", "15:32", "New rumor: north woods nest.", 22, 136, new Color32(181, 151, 231, 255));
        FeedRow(feed.transform, "Feed_03", "15:10", "Mira added casual 3v3 terms.", 22, 196, new Color32(230, 167, 82, 255));
        FeedRow(feed.transform, "Feed_04", "14:55", "PokeNav updated Route 1 sightings.", 22, 256, new Color32(93, 185, 131, 255));
        FeedRow(feed.transform, "Feed_05", "14:20", "Weather changed: clear skies.", 22, 316, new Color32(127, 190, 220, 255));
        Panel(feed.transform, "Scroll_Handle", 430, 76, 6, 176, new Color32(124, 137, 146, 255));
    }

    static void BuildDialogScreen(Transform root) {
        Panel(root, "Transparent_Play_Backdrop", 0, 0, 1920, 1080, new Color32(0, 0, 0, 0));
        Panel(root, "NPC_Silhouette", 760, 520, 160, 260, new Color32(76, 91, 101, 255));
        var bubble = Panel(root, "Speech_Bubble", 900, 380, 470, 150, new Color32(255, 255, 255, 255));
        Label(bubble.transform, "Speaker_Name", "Mira", 22, 16, 180, 24, 17, FontStyle.Bold, new Color32(43, 91, 98, 255), TextAnchor.MiddleLeft);
        Label(bubble.transform, "Bubble_Text", "Route 1 is calm today. I saw a water Pokemon near the pond.", 22, 48, 418, 64, 17, FontStyle.Normal, new Color32(40, 44, 48, 255), TextAnchor.UpperLeft);
        Panel(root, "Speech_Bubble_Tail", 850, 490, 70, 34, new Color32(255, 255, 255, 255));
        InfoPill(root, "Bubble_Mode", "Above NPC", 900, 552, new Color32(67, 123, 133, 255));
        InfoPill(root, "Bubble_State", "Auto closes", 1030, 552, new Color32(95, 122, 87, 255));
    }

    static void BuildMarketBasketScreen(Transform root) {
        Panel(root, "Transparent_Play_Backdrop", 0, 0, 1920, 1080, new Color32(0, 0, 0, 0));
        var basket = Panel(root, "Basket_Drawer", 1390, 120, 430, 640, new Color32(250, 252, 247, 248));
        Label(basket.transform, "Basket_Title", "Basket", 24, 22, 180, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(basket.transform, "Shop_Name", "Oak Town Market", 220, 26, 170, 24, 14, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.MiddleRight);
        SmallCard(basket.transform, "Line_01", "Potion A x2", "50", 24, 82, new Color32(88, 140, 190, 255));
        SmallCard(basket.transform, "Line_02", "Bait Recipe x1", "120", 24, 134, new Color32(188, 139, 82, 255));
        SmallCard(basket.transform, "Line_03", "Poke Ball x5", "500", 24, 186, new Color32(196, 111, 105, 255));
        Label(basket.transform, "Coupon_State", "Coupon slot: none", 24, 266, 360, 40, 14, FontStyle.Normal, new Color32(92, 101, 104, 255), TextAnchor.UpperLeft);
        InfoPill(basket.transform, "Loyalty_Pill", "12 loyalty pts", 24, 336, new Color32(75, 151, 103, 255));
        InfoPill(basket.transform, "Discount_Pill", "No discount", 176, 336, new Color32(91, 130, 190, 255));
        Panel(basket.transform, "Total_Divider", 24, 430, 360, 2, new Color32(190, 200, 196, 255));
        Label(basket.transform, "Subtotal", "Subtotal: 670", 24, 454, 180, 28, 18, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(basket.transform, "Checkout_Button", "Checkout", 244, 448, 140, 38, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        Panel(basket.transform, "Checkout_Button_Background", 244, 448, 140, 38, new Color32(76, 98, 80, 255)).transform.SetAsFirstSibling();

        var collapsed = Panel(root, "Basket_Collapsed_Button", 1660, 42, 160, 46, new Color32(76, 98, 80, 255));
        Label(collapsed.transform, "Collapsed_Text", "Basket 3", 0, 8, 160, 26, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    static void BuildBattleRulesScreen(Transform root) {
        Panel(root, "Backdrop", 0, 0, 1920, 1080, new Color32(236, 240, 243, 255));
        HeaderBlock(root, "Header", "Battle Rules", "Mira's proposal / Friendly match / Draft terms", 40, 32, 1840, 82, new Color32(88, 78, 118, 255));
        var modes = Panel(root, "Battle_Mode_Options", 52, 150, 560, 780, new Color32(255, 255, 255, 255));
        Label(modes.transform, "Modes_Title", "Battle Mode", 24, 22, 240, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        NavItem(modes.transform, "Mode_Classic", "Classic Four Move", 24, 82, true);
        NavItem(modes.transform, "Mode_Command", "Command Palette", 24, 140, false);
        NavItem(modes.transform, "Mode_Hybrid", "Hybrid", 24, 198, false);

        var classicMode = Panel(modes.transform, "Mode_Classic_View", 24, 270, 488, 172, new Color32(238, 241, 245, 255));
        Label(classicMode.transform, "Title", "Classic Four Move", 18, 14, 320, 26, 18, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(classicMode.transform, "Body", "Uses the current four selected moves, legacy AI hooks and familiar Pokemon-style battle pacing.", 18, 52, 432, 56, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(classicMode.transform, "Mode_State", "Best for", "Old flow", 18, 120, new Color32(91, 130, 190, 255));

        var commandMode = Panel(modes.transform, "Mode_CommandPalette_View", 24, 270, 488, 172, new Color32(238, 241, 245, 255));
        Label(commandMode.transform, "Title", "Command Palette", 18, 14, 320, 26, 18, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(commandMode.transform, "Body", "Uses base actions, elemental modifiers, action points and stamina decisions for a more custom battle flow.", 18, 52, 432, 56, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(commandMode.transform, "Mode_State", "Best for", "New flow", 18, 120, new Color32(119, 104, 168, 255));
        commandMode.SetActive(false);

        var hybridMode = Panel(modes.transform, "Mode_Hybrid_View", 24, 270, 488, 172, new Color32(238, 241, 245, 255));
        Label(hybridMode.transform, "Title", "Hybrid", 18, 14, 320, 26, 18, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(hybridMode.transform, "Body", "Keeps learned move identity while allowing limited modifiers, battle rules and trainer charge limits.", 18, 52, 432, 56, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(hybridMode.transform, "Mode_State", "Best for", "Bridge mode", 18, 120, new Color32(75, 151, 103, 255));
        hybridMode.SetActive(false);

        var rules = Panel(root, "Rule_Set_Details", 660, 150, 560, 780, new Color32(248, 251, 246, 255));
        Label(rules.transform, "Rules_Title", "Challenge Rules", 24, 22, 240, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        InfoPill(rules.transform, "Pill_Party", "3 Pokemon", 24, 82, new Color32(91, 130, 190, 255));
        InfoPill(rules.transform, "Pill_Type", "Water only", 154, 82, new Color32(92, 151, 178, 255));
        InfoPill(rules.transform, "Pill_Time", "10 turns", 286, 82, new Color32(214, 153, 70, 255));
        Label(rules.transform, "Rules_Body", "Mira proposes a friendly town match: three Pokemon, Water types only, ten turns. Both trainers must accept before the battle starts.", 24, 150, 500, 96, 16, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        var power = Panel(root, "Power_Mechanics", 1268, 150, 560, 780, new Color32(255, 255, 255, 255));
        Label(power.transform, "Power_Title", "Power Mechanics", 24, 22, 260, 30, 22, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        ListRow(power.transform, "Power_01", "Mega Evolution", "1 trainer charge", 24, 82, new Color32(188, 139, 82, 255));
        ListRow(power.transform, "Power_02", "Z-Move", "Alola event reward", 24, 164, new Color32(119, 104, 168, 255));
        ListRow(power.transform, "Power_03", "Gigantamax", "Regional gym rule", 24, 246, new Color32(196, 111, 105, 255));

        var megaView = Panel(power.transform, "Power_MegaEvolution_View", 24, 336, 488, 190, new Color32(238, 241, 245, 255));
        Label(megaView.transform, "Title", "Mega Evolution", 18, 14, 320, 26, 18, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(megaView.transform, "Body", "Temporary battle form change with base form swap, extra bonuses, turn limit and trainer charge usage.", 18, 52, 438, 54, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(megaView.transform, "Requirement", "Required", "Stone + charge", 18, 126, new Color32(188, 139, 82, 255));

        var zMoveView = Panel(power.transform, "Power_ZMove_View", 24, 336, 488, 190, new Color32(238, 241, 245, 255));
        Label(zMoveView.transform, "Title", "Z-Move", 18, 14, 320, 26, 18, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(zMoveView.transform, "Body", "Single-use special action gained from Alola trials, exploration rewards or special tournament permissions.", 18, 52, 438, 54, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(zMoveView.transform, "Requirement", "Required", "Crystal + rule", 18, 126, new Color32(119, 104, 168, 255));
        zMoveView.SetActive(false);

        var gigaView = Panel(power.transform, "Power_Gigantamax_View", 24, 336, 488, 190, new Color32(238, 241, 245, 255));
        Label(gigaView.transform, "Title", "Gigantamax", 18, 14, 320, 26, 18, FontStyle.Bold, new Color32(35, 38, 42, 255), TextAnchor.MiddleLeft);
        Label(gigaView.transform, "Body", "Arena-bound power state controlled by regional facilities, gym rules and limited battle permissions.", 18, 52, 438, 54, 15, FontStyle.Normal, new Color32(65, 70, 75, 255), TextAnchor.UpperLeft);
        SmallCard(gigaView.transform, "Requirement", "Required", "Arena + charge", 18, 126, new Color32(196, 111, 105, 255));
        gigaView.SetActive(false);
    }

    static GameObject Panel(Transform parent, string name, float x, float y, float width, float height, Color color) {
        var go = UiObject(parent, name, x, y, width, height);
        var image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    static void HeaderBlock(Transform parent, string name, string title, string subtitle, float x, float y, float width, float height, Color color) {
        var block = Panel(parent, name, x, y, width, height, color);
        Label(block.transform, name + "_Title", title, 18, 10, 260, 28, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        Label(block.transform, name + "_Subtitle", subtitle, 18, 38, width - 36, 18, 14, FontStyle.Normal, new Color32(220, 234, 235, 255), TextAnchor.MiddleLeft);
    }

    static void MenuOption(Transform parent, string name, string text, float x, float y, float width, bool selected) {
        var color = selected ? new Color32(67, 123, 133, 255) : new Color32(232, 237, 233, 255);
        Color textColor = selected ? Color.white : (Color)new Color32(45, 50, 54, 255);
        var item = Panel(parent, name, x, y, width, 42, color);
        if(selected) {
            Panel(item.transform, name + "_SelectedStripe", 0, 0, 6, 42, new Color32(237, 203, 108, 255));
        }
        Label(item.transform, name + "_Label", text, 18, 8, width - 36, 24, 15, selected ? FontStyle.Bold : FontStyle.Normal, textColor, TextAnchor.MiddleLeft);
    }

    static void NavItem(Transform parent, string name, string text, float x, float y, bool selected) {
        var color = selected ? new Color32(67, 123, 133, 255) : new Color32(57, 66, 73, 255);
        var item = Panel(parent, name, x, y, 184, 42, color);
        Label(item.transform, name + "_Label", text, 14, 8, 154, 24, 15, selected ? FontStyle.Bold : FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
    }

    static void Tab(Transform parent, string name, string text, float x, float y, float width, bool selected) {
        var color = selected ? new Color32(43, 91, 98, 255) : new Color32(212, 220, 218, 255);
        Color textColor = selected ? Color.white : (Color)new Color32(55, 60, 64, 255);
        var tab = Panel(parent, name, x, y, width, 32, color);
        Label(tab.transform, name + "_Label", text, 0, 4, width, 22, 14, selected ? FontStyle.Bold : FontStyle.Normal, textColor, TextAnchor.MiddleCenter);
    }

    static GameObject Marker(Transform parent, string name, float x, float y, Color color, string text) {
        var marker = Panel(parent, name, x, y, 28, 28, color);
        marker.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        Label(marker.transform, name + "_Text", text, 0, 2, 28, 22, 12, FontStyle.Bold, new Color32(32, 36, 38, 255), TextAnchor.MiddleCenter);
        return marker;
    }

    static void Route(Transform parent, string name, float x, float y, float width, float height, float rotationZ = 0f) {
        var route = Panel(parent, name, x, y, width, height, new Color32(238, 227, 170, 255));
        route.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, rotationZ);
    }

    static void InfoPill(Transform parent, string name, string text, float x, float y, Color color) {
        float width = Mathf.Max(96f, text.Length * 8f + 28f);
        var pill = Panel(parent, name, x, y, width, 28, color);
        Label(pill.transform, name + "_Label", text, 0, 4, width, 18, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    static GameObject ListRow(Transform parent, string name, string title, string body, float x, float y, Color stripe) {
        var row = Panel(parent, name, x, y, 292, 56, new Color32(236, 240, 234, 255));
        Panel(row.transform, name + "_Stripe", 0, 0, 7, 56, stripe);
        Label(row.transform, name + "_Title", title, 18, 7, 230, 20, 14, FontStyle.Bold, new Color32(38, 42, 45, 255), TextAnchor.MiddleLeft);
        Label(row.transform, name + "_Body", body, 18, 29, 248, 18, 13, FontStyle.Normal, new Color32(74, 80, 84, 255), TextAnchor.MiddleLeft);
        return row;
    }

    static Text GetText(GameObject parent, string childName) {
        if(parent == null) {
            return null;
        }

        var child = parent.transform.Find(childName);
        return child != null ? child.GetComponent<Text>() : null;
    }

    static void AddButtonTarget(GameObject row) {
        if(row == null) {
            return;
        }

        var button = row.GetComponent<Button>() ?? row.AddComponent<Button>();
        button.targetGraphic = row.GetComponent<Image>();
    }

    static void ListRowWide(Transform parent, string name, string title, string body, float x, float y, float width, Color stripe) {
        var row = Panel(parent, name, x, y, width, 64, new Color32(236, 240, 234, 255));
        Panel(row.transform, name + "_Stripe", 0, 0, 8, 64, stripe);
        Label(row.transform, name + "_Title", title, 20, 8, width * 0.34f, 22, 15, FontStyle.Bold, new Color32(38, 42, 45, 255), TextAnchor.MiddleLeft);
        Label(row.transform, name + "_Body", body, width * 0.38f, 8, width * 0.56f, 44, 14, FontStyle.Normal, new Color32(74, 80, 84, 255), TextAnchor.MiddleLeft);
    }

    static void SmallCard(Transform parent, string name, string title, string body, float x, float y, Color stripe) {
        var card = Panel(parent, name, x, y, 292, 36, new Color32(236, 240, 234, 255));
        Panel(card.transform, name + "_Stripe", 0, 0, 7, 36, stripe);
        Label(card.transform, name + "_Title", title, 18, 4, 108, 20, 13, FontStyle.Bold, new Color32(38, 42, 45, 255), TextAnchor.MiddleLeft);
        Label(card.transform, name + "_Body", body, 132, 4, 140, 20, 13, FontStyle.Normal, new Color32(74, 80, 84, 255), TextAnchor.MiddleRight);
    }

    static void FeedRow(Transform parent, string name, string time, string text, float x, float y, Color stripe) {
        var row = Panel(parent, name, x, y, 392, 42, new Color32(48, 56, 62, 255));
        Panel(row.transform, name + "_Stripe", 0, 0, 6, 42, stripe);
        Label(row.transform, name + "_Time", time, 16, 8, 52, 20, 13, FontStyle.Bold, new Color32(205, 216, 219, 255), TextAnchor.MiddleLeft);
        Label(row.transform, name + "_Text", text, 76, 8, 298, 20, 13, FontStyle.Normal, new Color32(235, 239, 240, 255), TextAnchor.MiddleLeft);
    }

    static GameObject Label(Transform parent, string name, string content, float x, float y, float width, float height, int size, FontStyle style, Color color, TextAnchor alignment) {
        var go = UiObject(parent, name, x, y, width, height);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = GetFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return go;
    }

    static GameObject UiObject(Transform parent, string name, float x, float y, float width, float height) {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return go;
    }

    static Font GetFont() {
        if(uiFont != null) {
            return uiFont;
        }

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(uiFont == null) {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        if(uiFont == null) {
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16);
        }

        return uiFont;
    }
}
#endif
