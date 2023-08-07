using BepInEx;
using BepInEx.Logging;
using DG.Tweening;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyFirstPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        private Harmony[] balancedPatchers;
        private Harmony[] opPatchers;
        private Harmony fixesPatcher;

        private HashSet<ModType> patchesHashSet;
        public static Dictionary<ModType, ChessBoardState.Team> biasDict;
        public bool MultiplayerActive { get; private set; } = false;
        public bool AdventureModeActive { get; private set; } = false;

        private enum OpModType
        {
            Piece_Cloner,
            Piece_Remover,
            Piece_Mover,
            Moves_Analysis_Tool
        }
        public enum ModType
        {
            Queen_promotion,
            Self_pawn_capture,
            Unlimited_range,
            Tunnel_swaps,
            Super_Secrets,
            Water_of_Magic,
            Super_Aliens,
            Divine_Angels
        }

        private Dictionary<ModType, KeyCode> hotkeyDict;

        //public ChessBoardState.Team teamFavor { get; private set; }

        /*this.scenes[1].name = "1(main)";
		this.scenes[5].name = "5(options)";
		this.scenes[6].name = "6(beastiary)";
		this.scenes[7].name = "7(Game)";
		this.scenes[9].name = "9(Lobby)";
		this.scenes[10].name = "10(Multiplayer)";
		this.scenes[11].name = "11(Versus)";
		this.scenes[12].name = "12(HowTo)";
		this.scenes[13].name = "13(Credits)";
		this.scenes[14].name = "14(Challenges)";
		this.scenes[15].name = "15(1PChallenges)";
		this.scenes[16].name = "16(Story)";
		this.scenes[17].name = "17(StoryGame)";*/

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("DreamMods is loaded! \n Note : This is only tested on version v0.81 (May not work on other versions)");
            string[] balancedModsNames = System.Enum.GetNames(typeof(ModType));
            this.balancedPatchers = new Harmony[balancedModsNames.Length];
            for (int i = 0; i < this.balancedPatchers.Length; i++)
            {
                this.balancedPatchers[i] = new Harmony(balancedModsNames[i]);
            }
            biasDict = new Dictionary<ModType, ChessBoardState.Team>();
            foreach (ModType mod in System.Enum.GetValues(typeof(ModType)))
            {
                biasDict[mod] = ChessBoardState.Team.None;
            }
            //Hotkey initialization logic
            hotkeyDict = new Dictionary<ModType, KeyCode>();
            hotkeyDict[ModType.Queen_promotion] = KeyCode.F1;
            hotkeyDict[ModType.Self_pawn_capture] = KeyCode.F2;
            hotkeyDict[ModType.Unlimited_range] = KeyCode.F3;
            hotkeyDict[ModType.Tunnel_swaps] = KeyCode.F4;
            hotkeyDict[ModType.Super_Secrets] = KeyCode.F5;
            hotkeyDict[ModType.Water_of_Magic] = KeyCode.F6;
            hotkeyDict[ModType.Super_Aliens] = KeyCode.F7;
            hotkeyDict[ModType.Divine_Angels] = KeyCode.F8;
            string[] opModsNames = System.Enum.GetNames(typeof(OpModType));
            this.opPatchers = new Harmony[opModsNames.Length];
            for (int i = 0; i < this.opPatchers.Length; i++)
            {
                this.opPatchers[i] = new Harmony(opModsNames[i]);
            }
            Plugin.Instance = this;
            this.fixesPatcher = new Harmony("fixesPatcher");
            this.patchesHashSet = new HashSet<ModType>();
            //this.teamFavor = ChessBoardState.Team.White;
            Logger.LogInfo("Press 'TAB' inside game for mods info and hotkeys.\n Note : Key input only works in-game.\n Note : Press 'Shift + (hotkey)' to deativate mods.");
            this.PatchFixes();
            this.PatchOpMods();
            //this.ActivateAllBalancedMods();
            SceneManager.sceneLoaded += this.OnModsDisabledForMultiplayer;
            SceneManager.sceneLoaded += this.OnModsEnabledForSingleplayer;
            //SceneManager.sceneLoaded += this.onModsDisabledForAdventureMode;
            //SceneManager.sceneLoaded += this.onModsEnabledForNonAdventure;
            SceneManager.sceneLoaded += this.OnSceneLoaded;
            SceneManager.sceneUnloaded += this.OnSceneUnloaded;
        }

        private void Start()
        {
            ShowNotificationPopup($"DreamMods v{PluginInfo.PLUGIN_VERSION} is loaded!",
                Color.black, Color.white, true);
        }

        public void UpdateHook() { }


        private void Update()
        {
            this.UpdateHook();
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.KeypadPlus))
            {
                PlayerInput.Instance.MenuSelection_hook.SelectShop();
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.KeypadMinus))
            {
                PlayerInput.Instance.MenuSelection_hook.SelectHowTo();
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.KeypadMultiply))
            {
                SaveSystem.ResetPurchases();
                SaveSystem.data.SetGold(0);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.KeypadDivide))
            {
                PlayerInput.Instance.MenuSelection_hook.SwitchSkin(1);
                PlayerInput.Instance.MenuSelection_hook.SwitchSkin(2);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.Tab))
            {
                LogOpModInfo();
                LogBalancedModInfo();
            }

            if (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift))
            {

                foreach (ModType balancedMod in hotkeyDict.Keys)
                {
                    if (UnityInput.Current.GetKeyDown(hotkeyDict[balancedMod]))
                    {
                        this.TryUnpatch(balancedMod);
                        return;
                    }
                }
            }
            if (UnityInput.Current.GetKey(KeyCode.Alpha1))
            {
                this.TrySetBias(ChessBoardState.Team.None);
                return;
            }
            if (UnityInput.Current.GetKey(KeyCode.Alpha2))
            {
                this.TrySetBias(ChessBoardState.Team.White);
                return;
            }
            if (UnityInput.Current.GetKey(KeyCode.Alpha3))
            {
                this.TrySetBias(ChessBoardState.Team.Black);
                return;
            }
            foreach (ModType balancedMod in hotkeyDict.Keys)
            {
                if (UnityInput.Current.GetKeyDown(hotkeyDict[balancedMod]))
                {
                    this.TryPatch(balancedMod);
                    return;
                }
            }
            
        }

        public void LogThisInfo(string message)
        {
            Logger.LogInfo(message);
        }

        private void TryPatch(ModType mod)
        {
            if (MultiplayerActive)
            {
                Logger.LogWarning("You cannot enable/disable mods in multiplayer!");
                return;
            }
            if (patchesHashSet.Contains(mod))
            {
                LogThisInfo($"{mod.CleanName()} mod is already active");
                ShowNotificationPopup($"{mod.CleanName()} mod is already active",
                new Color(0f, 0.3f, 0f), Color.white, true);
                return;
            }
            this.PatchThisBalancedMod(mod);
            LogThisInfo($". . . {mod.CleanName()} mod activated . . .");
            //PopupText.TryShowMessage($". . . {mod.ToString()} mod activated . . .");
            ShowNotificationPopup($". . . {mod.CleanName()} mod activated . . .",
                new Color(0f, 0.3f, 0f), Color.white, true);
            this.patchesHashSet.Add(mod);
            LogBalancedModInfo();
        }

        private void TryUnpatch(ModType mod)
        {
            if (MultiplayerActive)
            {
                Logger.LogWarning("You cannot enable/disable mods in multiplayer!");
                return;
            }
            if (!patchesHashSet.Contains(mod))
            {
                LogThisInfo($"{mod.CleanName()} mod is already disabled");
                ShowNotificationPopup($"{mod.CleanName()} mod is already disabled",
                new Color(0.3f, 0f, 0f), Color.white, true);
                return;
            }
            this.UnpatchThisBalancedMod(mod);
            LogThisInfo($". . . {mod.CleanName()} mod disabled . . .");
            ShowNotificationPopup($". . . {mod.CleanName()} mod disabled . . .",
                new Color(0.3f, 0f, 0f), Color.white, true);
            this.patchesHashSet.Remove(mod);
            LogBalancedModInfo();
        }

        private void OnModsDisabledForMultiplayer(Scene scene, LoadSceneMode sceneMode)
        {
            if (SceneManager.GetActiveScene().name == "10(Multiplayer)" && !this.MultiplayerActive)
            {
                Logger.LogWarning("!!! Mods have been disbled for multiplayer. !!!");
                this.UnpatchFixes();
                this.DeactivateAllBalancedMods();
                this.UnpatchOpMods();
                this.MultiplayerActive = true;
            }
        }

        private void OnModsEnabledForSingleplayer(Scene scene, LoadSceneMode sceneMode)
        {
            //Logger.LogInfo("Scene loaded : " + SceneManager.GetActiveScene().name);
            if (SceneManager.GetSceneByName("1(main)").isLoaded && this.MultiplayerActive)
            {
                Logger.LogWarning("!!! Mods have been re-enabled for singleplayer. !!!");
                this.PatchFixes();
                this.PatchOpMods();
                foreach (ModType item in this.patchesHashSet)
                {
                    this.PatchThisBalancedMod(item);
                }
                this.MultiplayerActive = false;
            }
        }

        //private void OnModsDisabledForAdventureMode(Scene scene, LoadSceneMode sceneMode)
        //{
        //    if (SceneManager.GetActiveScene().name == "16(Story)" && !this.AdventureModeActive)
        //    {
        //        Logger.LogWarning("!!! OP Mods have been disbled for adventure mode. !!!");
        //        this.UnpatchOpMods();
        //        this.AdventureModeActive = true;
        //    }
        //}

        //private void OnModsEnabledForNonAdventure(Scene scene, LoadSceneMode sceneMode)
        //{
        //    if (SceneManager.GetSceneByName("1(main)").isLoaded && this.AdventureModeActive)
        //    {
        //        Logger.LogWarning("!!! OP Mods have been re-enabled for versus mode. !!!");
        //        this.PatchOpMods();
        //        this.AdventureModeActive = false;
        //    }
        //}

        private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            //if(scene.name == "7(Game)" || scene.name == "17(StoryGame)") { AllFactionsHaveFullBoardMermaidSwaps.initialized = false;}
        }

        private void OnSceneUnloaded(Scene scene)
        {
            //if(scene.name == "7(Game)" || scene.name == "17(StoryGame)") { AllFactionsHaveFullBoardMermaidSwaps.initialized = false;}
        }

        private void PatchFixes()
        {
            this.fixesPatcher.PatchAll(typeof(SecretUnlockedPatch));
        }

        private void UnpatchFixes()
        {
            this.fixesPatcher.UnpatchSelf();
        }

        private void PatchOpMods()
        {

            this.opPatchers[(int)OpModType.Piece_Cloner].PatchAll(typeof(PieceClonerMod));
            this.opPatchers[(int)OpModType.Piece_Remover].PatchAll(typeof(PieceRemoverMod));
            this.opPatchers[(int)OpModType.Piece_Mover].PatchAll(typeof(PieceMoverMod));
            this.opPatchers[(int)OpModType.Moves_Analysis_Tool].PatchAll(typeof(MoveListAnalysisTool));
            //this.opPatchers[(int)OpModType.WolfSwapJump].PatchAll(typeof(WolfSwapJump));

        }

        private void UnpatchOpMods()
        {

            this.opPatchers[(int)OpModType.Piece_Cloner].UnpatchSelf();
            this.opPatchers[(int)OpModType.Piece_Remover].UnpatchSelf();
            this.opPatchers[(int)OpModType.Piece_Mover].UnpatchSelf();
            this.opPatchers[(int)OpModType.Moves_Analysis_Tool].UnpatchSelf();
            //this.opPatchers[(int)OpModType.WolfSwapJump].UnpatchSelf();

        }

        private void ActivateAllBalancedMods()
        {
            foreach (string mod in System.Enum.GetNames(typeof(ModType)))
            {
                ModType modType = (ModType)System.Enum.Parse(typeof(ModType), mod);
                if (!patchesHashSet.Contains(modType))
                {
                    this.PatchThisBalancedMod(modType);
                    patchesHashSet.Add(modType);
                }
            }
        }

        private void DeactivateAllBalancedMods()
        {
            foreach (string mod in System.Enum.GetNames(typeof(ModType)))
            {
                ModType modType = (ModType)System.Enum.Parse(typeof(ModType), mod);
                if (patchesHashSet.Contains(modType))
                {
                    this.UnpatchThisBalancedMod(modType);
                    patchesHashSet.Remove(modType);
                }
            }
        }

        private void PatchThisBalancedMod(ModType modType)
        {
            switch (modType)
            {
                case ModType.Queen_promotion:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsCanDoQueenPromotion));
                    break;
                case ModType.Self_pawn_capture:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsCanCaptureOwnPawns));
                    break;
                case ModType.Unlimited_range:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsHaveUnlimitedRange));
                    break;
                case ModType.Tunnel_swaps:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsHaveFullBoardMermaidSwaps));
                    break;
                case ModType.Super_Secrets:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(SecretSpellCastManaBonus));
                    break;
                case ModType.Water_of_Magic:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(WaterTilesBoostAlliesAndDebuffEnemies));
                    break;
                case ModType.Super_Aliens:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AliensCanChainTheirExtraTurns));
                    break;
                case ModType.Divine_Angels:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AngelsAbilitiesHaveMoreRange));
                    break;
                default:
                    Logger.LogError($"'{modType.CleanName()}' mod is currently not supported");
                    return;
            }
        }

        private void UnpatchThisBalancedMod(ModType modType)
        {
            switch (modType)
            {
                case ModType.Queen_promotion:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Self_pawn_capture:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Unlimited_range:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Tunnel_swaps:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Super_Secrets:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Water_of_Magic:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Super_Aliens:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Divine_Angels:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                default:
                    Logger.LogError($"{modType.CleanName()} mod is currently not supported");
                    return;
            }
        }

        private void LogBalancedModInfo()
        {
            //int expectedCapacity = System.Enum.GetValues(typeof(ModType)).Length * 4 + 3;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("List of balanced mods [(name) (hotkey) (bias) (state)] :");
            int i = 1;
            foreach (ModType mod in System.Enum.GetValues(typeof(ModType)))
            {
                string state = patchesHashSet.Contains(mod) ? "(active)" : "";
                sb.AppendLine($"{i}.\t{mod.CleanName()} ({hotkeyDict[mod]}) {mod.Bias()} {state}");
                i++;
            }
            sb.AppendLine("To set bias, hold [0] (None), [1] (White) or [2] (Black)\n\tand press the respective hotkey !");
            Logger.LogInfo(sb);
        }

        private void LogOpModInfo()
        {
            if (this.AdventureModeActive)
            {
                Logger.LogWarning("OP Mods are disbled for adventure mode!");
                return;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("List of OP mods and their usage :");
            foreach (OpModType opMod in System.Enum.GetValues(typeof(OpModType)))
            {
                sb.Append($"{opMod} (Usage) : ");
                sb.AppendLine(this.GetOpModUsageInfoString(opMod));
            }
            Logger.LogInfo(sb);
        }

        private string GetOpModUsageInfoString(OpModType opModType)
        {
            switch (opModType)
            {
                case OpModType.Piece_Cloner:
                    return "Select any piece and then hold (Shift) \n\t & click anywhere on board to clone it(for white color team), \n\t or hold (Shift) + (Alt) to make clone for black color team.";
                case OpModType.Piece_Remover:
                    return "Select any piece and press (Delete) key to remove it.";
                case OpModType.Piece_Mover:
                    return "Select any piece then hold (Alt) key and click on an empty tile to move it there.";
                //case OpModType.WolfSwapJump:
                //    return "Hold 'Shift' key while using 'blood jump' ability of wolf \n\t to swap places with pawn instead of capturing it.";
                case OpModType.Moves_Analysis_Tool:
                    return "Use arrow keys (←) (→) to go back and forth between moves.\n\t Or hold (↑) or (↓) to go continously for long traverse. \n\t Use (Ctrl) to get back to analysis after extrapolation.\n\t Use (S) to save current move-list anytime(even when game ends in extrapolation mode).";
                default:
                    return $"{opModType.ToString()} mod is currently work in progress !!!";
            }
        }

        private void LogPatternClassInfo()
        {
            Board b = PlayerInput.Instance.board_instance;

            FieldInfo[] fieldInfos = typeof(patternClass).GetFields(BindingFlags.NonPublic);
            FieldInfo[] pieceFieldInfos = typeof(Piece).GetFields();
            Piece p = b.GetPieces(PieceTypeEnum.Rook, b.activeTeam.color)[0];
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            patternClass[] pcArray = p.patterns;
            sb.AppendLine("================= Rook Pattern Class ===============");
            foreach (FieldInfo item in pieceFieldInfos)
            {

                if (item.FieldType == typeof(bool))
                {
                    if ((bool)item.GetValue(p) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                }
                else if (item.FieldType == typeof(int))
                {
                    sb.AppendLine($"{item.Name}(int) : {item.GetValue(p)}");
                }
            }
            for (int i = 0; i < pcArray.Length; i++)
            {
                patternClass pattern = pcArray[i];
                sb.AppendLine($"-------------- Pattern Class Index : {i} ---------------");
                foreach (FieldInfo item in fieldInfos)
                {

                    if (item.FieldType == typeof(bool))
                    {
                        if ((bool)item.GetValue(pattern) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                    }
                    else if (item.FieldType == typeof(System.String[]))
                    {
                        sb.AppendLine($"{item.Name}(string[]) :- ");
                        foreach (string arrayItem in (string[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem}");
                        }
                    }
                    else if (item.FieldType == typeof(BoardSquare[]))
                    {
                        sb.AppendLine($"{item.Name}(BoardSquare[]) :- ");
                        foreach (BoardSquare arrayItem in (BoardSquare[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem.ToString()}");
                        }
                    }
                    else if (item.FieldType == typeof(int))
                    {
                        sb.AppendLine($"{item.Name}(int) : {item.GetValue(pattern)}");
                    }
                }
            }
            p = b.GetPieces(PieceTypeEnum.Bishop, b.activeTeam.color)[0];
            pcArray = p.patterns;
            sb.AppendLine("================= Bishop Pattern Class ===============");
            foreach (FieldInfo item in pieceFieldInfos)
            {

                if (item.FieldType == typeof(bool))
                {
                    if ((bool)item.GetValue(p) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                }
                else if (item.FieldType == typeof(int))
                {
                    sb.AppendLine($"{item.Name}(int) : {item.GetValue(p)}");
                }
            }
            for (int i = 0; i < pcArray.Length; i++)
            {
                patternClass pattern = pcArray[i];
                sb.AppendLine($"-------------- Pattern Class Index : {i} ---------------");
                foreach (FieldInfo item in fieldInfos)
                {

                    if (item.FieldType == typeof(bool))
                    {
                        if ((bool)item.GetValue(pattern) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                    }
                    else if (item.FieldType == typeof(System.String[]))
                    {
                        sb.AppendLine($"{item.Name}(string[]) :- ");
                        foreach (string arrayItem in (string[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem}");
                        }
                    }
                    else if (item.FieldType == typeof(BoardSquare[]))
                    {
                        sb.AppendLine($"{item.Name}(BoardSquare[]) :- ");
                        foreach (BoardSquare arrayItem in (BoardSquare[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem.ToString()}");
                        }
                    }
                    else if (item.FieldType == typeof(int))
                    {
                        sb.AppendLine($"{item.Name}(int) : {item.GetValue(pattern)}");
                    }
                }
            }
            p = b.GetPieces(PieceTypeEnum.Knight, b.activeTeam.color)[0];
            pcArray = p.patterns;
            sb.AppendLine("================= Knight Pattern Class ===============");
            foreach (FieldInfo item in pieceFieldInfos)
            {

                if (item.FieldType == typeof(bool))
                {
                    if ((bool)item.GetValue(p) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                }
                else if (item.FieldType == typeof(int))
                {
                    sb.AppendLine($"{item.Name}(int) : {item.GetValue(p)}");
                }
            }
            for (int i = 0; i < pcArray.Length; i++)
            {
                patternClass pattern = pcArray[i];
                sb.AppendLine($"-------------- Pattern Class Index : {i} ---------------");
                foreach (FieldInfo item in fieldInfos)
                {

                    if (item.FieldType == typeof(bool))
                    {
                        if ((bool)item.GetValue(pattern) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                    }
                    else if (item.FieldType == typeof(System.String[]))
                    {
                        sb.AppendLine($"{item.Name}(string[]) :- ");
                        foreach (string arrayItem in (string[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem}");
                        }
                    }
                    else if (item.FieldType == typeof(BoardSquare[]))
                    {
                        sb.AppendLine($"{item.Name}(BoardSquare[]) :- ");
                        foreach (BoardSquare arrayItem in (BoardSquare[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem.ToString()}");
                        }
                    }
                    else if (item.FieldType == typeof(int))
                    {
                        sb.AppendLine($"{item.Name}(int) : {item.GetValue(pattern)}");
                    }
                }
            }
            p = b.GetPieces(PieceTypeEnum.Queen, b.activeTeam.color)[0];
            pcArray = p.patterns;
            sb.AppendLine("================= Queen Pattern Class ===============");
            foreach (FieldInfo item in pieceFieldInfos)
            {

                if (item.FieldType == typeof(bool))
                {
                    if ((bool)item.GetValue(p) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                }
                else if (item.FieldType == typeof(int))
                {
                    sb.AppendLine($"{item.Name}(int) : {item.GetValue(p)}");
                }
            }
            for (int i = 0; i < pcArray.Length; i++)
            {
                patternClass pattern = pcArray[i];
                sb.AppendLine($"-------------- Pattern Class Index : {i} ---------------");
                foreach (FieldInfo item in fieldInfos)
                {

                    if (item.FieldType == typeof(bool))
                    {
                        if ((bool)item.GetValue(pattern) == true) { sb.AppendLine($"{item.Name}(bool) : True"); }
                    }
                    else if (item.FieldType == typeof(System.String[]))
                    {
                        sb.AppendLine($"{item.Name}(string[]) :- ");
                        foreach (string arrayItem in (string[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem}");
                        }
                    }
                    else if (item.FieldType == typeof(BoardSquare[]))
                    {
                        sb.AppendLine($"{item.Name}(BoardSquare[]) :- ");
                        foreach (BoardSquare arrayItem in (BoardSquare[])item.GetValue(pattern))
                        {
                            sb.AppendLine($"\t {arrayItem.ToString()}");
                        }
                    }
                    else if (item.FieldType == typeof(int))
                    {
                        sb.AppendLine($"{item.Name}(int) : {item.GetValue(pattern)}");
                    }
                }
            }

            Logger.LogInfo(sb);
        }

        public void ShowNotificationPopup(string text, Color bgColor, Color textColor, bool standalone = false, System.Action onComplete = null)
        {
            GameObject bg = new GameObject();
            bg.name = "Notification";
            bg.AddComponent<RectTransform>().sizeDelta = new Vector2(290f, 100f);
            bg.AddComponent<Image>().color = bgColor;
            CanvasGroup canvasGroup = bg.AddComponent<CanvasGroup>();

            GameObject gameObject = new GameObject();
            gameObject.name = "Text";
            TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
            textMeshProUGUI.text = text;
            textMeshProUGUI.color = textColor;
            textMeshProUGUI.alignment = TextAlignmentOptions.Center;
            textMeshProUGUI.fontSize = 20f;
            textMeshProUGUI.enableWordWrapping = false;

            GameObject gameObject2 = null;
            if (!standalone)
            {
                gameObject2 = PlayerInput.Instance.FindGO("Game - Right Board", true);
                bg.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (go.name == "Canvas") { gameObject2 = go; break; }
                }
                if (gameObject2 == null) { gameObject2 = PlayerInput.Instance.FindGO("Canvas", true); }
                bg.transform.localScale = new Vector3(2f, 1f, 1f);
            }
            gameObject.transform.SetParent(bg.transform);
            gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);

            bg.transform.SetParent(gameObject2.transform);
            bg.transform.localPosition = new Vector3(15f, -50f, 0f);
            canvasGroup.alpha = 0f;
            Sequence s = DOTween.Sequence();
            s.Append(canvasGroup.DOFade(1f, 0.5f));
            s.AppendInterval(1f);
            s.Append(canvasGroup.DOFade(0f, 0.5f));
            s.AppendCallback(delegate
            {
                UnityEngine.Object.Destroy(bg);
                if (onComplete != null) onComplete();
            });
        }

        private void TrySetBias(ChessBoardState.Team bias)
        {
            foreach (ModType balancedMod in hotkeyDict.Keys)
            {
                if (UnityInput.Current.GetKeyDown(hotkeyDict[balancedMod]))
                {
                    biasDict[balancedMod] = bias;
                    Logger.LogInfo($". . . {balancedMod.CleanName()}'s bias changed to {bias} . . .");
                    Color fg = bias == ChessBoardState.Team.Black ? Color.white : Color.black;
                    Color bg;
                    switch (bias)
                    {
                        case ChessBoardState.Team.None:
                            bg = new Color(0.6f, 0.6f, 0.6f);
                            break;
                        case ChessBoardState.Team.White:
                            bg = Color.white;
                            break;
                        case ChessBoardState.Team.Black:
                            bg = Color.black;
                            break;
                        default:
                            bg = new Color(0.6f, 0.6f, 0.6f);
                            break;
                    }
                    ShowNotificationPopup($". . . {balancedMod.CleanName()}'s bias changed to {bias} . . .", bg, fg, true);
                    return;
                }
            }
        }
    }





    public static class MoveListAnalysisTool
    {
        private static int backIndex = 0;

        private static Phase working = Phase.None;

        private static List<ChessBoardState> originalMovesList;

        private static ChessBoard chessBoard;

        [HarmonyPatch(typeof(ChessBoard), nameof(ChessBoard.Init))]
        [HarmonyPostfix]
        public static void Init_ChessBoard(ChessBoard __instance)
        {
            chessBoard = __instance;
            working = Phase.None;
            originalMovesList = new List<ChessBoardState>(0);
        }

        [HarmonyPatch(typeof(ChessBoard), "Update")]
        [HarmonyPostfix]
        public static void Update_ChessBoard(ChessBoard __instance)
        {
            PreventAIFromRunning(__instance);
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKey(KeyCode.UpArrow))
            {
                EnsureInitialization();
                Move_Backward(__instance);
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKey(KeyCode.DownArrow))
            {

                Move_Forward(__instance);
            }

            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                UpdateMovesListAfterExtrapolationEnd(__instance);
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveEndGameMovelist();
                TryRevertGameEnd(__instance);
            }
        }

        private static void Move_Backward(ChessBoard self)
        {
            TryRevertGameEnd(self);
            if (working != Phase.Working || self.phase != ChessBoard.Phase.PlayerMove) { return; }
            backIndex++;
            if (self.saved_moves().Count > 1)
            {
                self.saved_moves().RemoveAt(0);
            }
            else
            {
                backIndex--;
            }
            Plugin.Instance.LogThisInfo($"(<-) pressed, Back index is : {backIndex}");
            //Plugin.Instance.LogThisInfo("State Removal working fine");
            ChessBoardState lastMove = self.GetLastMove();
            MoveAnimations.TweenStateToState(self.state, lastMove, ChessBoard.Phase.UndoMove, null);
            ChessBoardState.CopyState(lastMove, self.state);
            ChessBoardState.CopyMoves(lastMove, self.state);
            ChessBoardState.CopyPieces(lastMove, self.state);
            //Plugin.Instance.LogThisInfo("State Copying Working fine");
            self.state.SetTurn((self.saved_moves().Count < 2) ? 1 : (self.state.turn + 1));
            UnityEngine.Object.FindObjectOfType<MoveHistoryUI>().UpdateMoves(self, null, false);
            UnityEngine.Object.FindObjectOfType<GameBoardArrows>().RemoveAIMoveArrow();
            self.phase = ChessBoard.Phase.Animation;
            //Plugin.Instance.LogThisInfo("All other things working fine");
        }

        private static void Move_Forward(ChessBoard self)
        {
            if (working != Phase.Working || self.phase != ChessBoard.Phase.PlayerMove) { return; }
            backIndex--;
            if (backIndex < 0)
            {
                backIndex = 0;
                working = Phase.None;
                Plugin.Instance.LogThisInfo($"(->) pressed, Analysis stopped !");
                Plugin.Instance.ShowNotificationPopup("Analysis stopped !",
                Color.black, Color.white, false);
                return;
            }
            self.next_move = originalMovesList[backIndex].last_move;
            self.phase = ChessBoard.Phase.ExecuteMove;
            //self.saved_moves().Insert(0, originalMovesList[backIndex - 1]);
            Plugin.Instance.LogThisInfo($"(->) pressed, Back index is : {backIndex}");
            //self.saved_moves().Insert(0, originalMovesList[backIndex-1]);
            //Plugin.Instance.LogThisInfo("State Adding working fine");
            //ChessBoardState lastMove = self.GetLastMove();
            //MoveAnimations.TweenStateToState(self.state, lastMove, ChessBoard.Phase.UndoMove, null);
            //ChessBoardState.CopyState(lastMove, self.state);
            //ChessBoardState.CopyMoves(lastMove, self.state);
            //ChessBoardState.CopyPieces(lastMove, self.state);
            //Plugin.Instance.LogThisInfo("State Copying Working fine");
            //self.state.SetTurn(self.state.turn - 1);
            //UnityEngine.Object.FindObjectOfType<MoveHistoryUI>().UpdateMoves(self, null);
            //UnityEngine.Object.FindObjectOfType<GameBoardArrows>().RemoveAIMoveArrow();
            //self.phase = ChessBoard.Phase.Animation;
            //Plugin.Instance.LogThisInfo("All other things working fine");
        }

        [HarmonyPatch(typeof(MoveHistoryUI), nameof(MoveHistoryUI.UpdateMoves))]
        [HarmonyPrefix]
        public static bool ExtraDetailCopyMoves(MoveHistoryUI __instance, ChessBoard board, ChessBoardState add_state, bool printout)
        {
            if (!printout)
            {
                return true;
            }
            for (int i = 0; i < __instance.movesUIP1.Length; i++)
            {
                __instance.movesUIP1[i].text = "";
                __instance.movesUIP2[i].text = "";
                __instance.turnNumbers()[i].text = (i + 1).ToString();
            }
            int num = 10;
            if (board.state.GetCurrentTeam() == ChessBoardState.Team.Black)
            {
                num = 9;
            }
            List<ChessBoardState> list = new List<ChessBoardState>();
            if (add_state != null)
            {
                list.Add(add_state);
                num--;
            }
            list.AddRange(board.GetTheSavedMoves(-1));
            list.Reverse();
            if (list.Count < 1)
            {
                return false;
            }
            int num2 = 0;
            ChessBoardState.Team team = ChessBoardState.Team.None;
            List<string> list2 = new List<string>();
            List<string> list3 = new List<string>();
            for (int j = 0; j < list.Count; j++)
            {
                ChessBoardState chessBoardState = list[j];
                ChessBoardState chessBoardState2 = new ChessBoardState();
                chessBoardState2.Init();
                ChessBoardState.CopyState(chessBoardState, chessBoardState2);
                ChessBoardState.CopyPieces(chessBoardState, chessBoardState2);
                chessBoardState2.GenerateMoves(ChessBoardState.Team.None, true, false, false);
                PieceMoves.Move last_move = chessBoardState2.last_move;
                ChessBoardState.Team team2 = chessBoardState2.GetTeam(last_move.piece_moved);
                Utility.IndexToPos((int)last_move.to_square, out int x1, out int y1);
                Utility.IndexToPos(last_move.from_square, out int x0, out int y0);
                string text = (PlayerInput.Instance.GetTypeGFX(chessBoardState2.pieces[(int)last_move.piece_moved].race, chessBoardState2.pieces[(int)last_move.piece_moved].type).localization ?? "").ToString() + " ";
                
                if (last_move.move_type == 11)
                {
                    if (chessBoardState2.last_move.to_square > chessBoardState2.last_move.from_square)
                    {
                        text = "0-0";
                    }
                    else
                    {
                        text = "0-0-0";
                    }
                }
                if (last_move.move_type != 11)
                {
                    //Change done here to include starting position also
                    text += Utility.positionToString(x0, y0) + " ";
                    if (last_move.piece_targeted != 0 && chessBoardState.IsMoveThreateningSquare(chessBoardState.pieces[(int)last_move.piece_targeted].square, last_move))
                    {
                        text += "x";
                    }
                    text += Utility.positionToString(x1, y1);
                }
                if (chessBoardState2.IsTeamInCheckMate((team2 == ChessBoardState.Team.White) ? ChessBoardState.Team.Black : ChessBoardState.Team.White))
                {
                    text += "#";
                }
                else if (chessBoardState2.IsTeamInCheck((team2 == ChessBoardState.Team.White) ? ChessBoardState.Team.Black : ChessBoardState.Team.White))
                {
                    text += "+";
                }
                if (team == team2)
                {
                    if (team2 == ChessBoardState.Team.White)
                    {
                        list2.Add(text);
                        list3.Add("");
                    }
                    else
                    {
                        list3.Add(text);
                        list2.Add("");
                    }
                    num2++;
                }
                else if (team2 == ChessBoardState.Team.White)
                {
                    list2.Add(text);
                    if (list2.Count > list3.Count)
                    {
                        num2++;
                    }
                }
                else
                {
                    list3.Add(text);
                    if (list3.Count > list2.Count)
                    {
                        num2++;
                    }
                }
                team = team2;
            }
            if (list2.Count < list3.Count)
            {
                list2.Add("");
            }
            if (list3.Count < list2.Count)
            {
                list3.Add("");
            }
            string text2 = "";
            for (int k = 0; k < num2; k++)
            {
                text2 += (k + 1).ToString();
                if (k < list2.Count)
                {
                    text2 = text2 + " " + list2[k];
                }
                if (k < list3.Count)
                {
                    text2 = text2 + " " + list3[k];
                }
                text2 += "\n";
            }
            GUIUtility.systemCopyBuffer = text2;
            Plugin.Instance.LogThisInfo("Move-list(extra detail) copied to clipboard !");
            
            return false;
        }

        private static void UpdateMovesListAfterExtrapolationEnd(ChessBoard self)
        {
            TryRevertGameEnd(self);
            if (working != Phase.Extrapolating || self.phase != ChessBoard.Phase.PlayerMove) { return; }
            while (self.saved_moves().Count > 1 + originalMovesList.Count - backIndex)
            {
                self.saved_moves().RemoveAt(0);
            }
            Plugin.Instance.LogThisInfo($"(Ctrl) pressed, Restoring analysis state before extrapolation.");
            Plugin.Instance.ShowNotificationPopup("Analysis state restored",
                Color.black, Color.white, false);
            //Plugin.Instance.LogThisInfo("State Removal working fine");
            ChessBoardState lastMove = self.GetLastMove();
            MoveAnimations.TweenStateToState(self.state, lastMove, ChessBoard.Phase.UndoMove, null);
            ChessBoardState.CopyState(lastMove, self.state);
            ChessBoardState.CopyMoves(lastMove, self.state);
            ChessBoardState.CopyPieces(lastMove, self.state);
            //Plugin.Instance.LogThisInfo("State Copying Working fine");
            UnityEngine.Object.FindObjectOfType<MoveHistoryUI>().UpdateMoves(self, null, false);
            UnityEngine.Object.FindObjectOfType<GameBoardArrows>().RemoveAIMoveArrow();
            self.phase = ChessBoard.Phase.Animation;
            //Plugin.Instance.LogThisInfo("All other things working fine");
            working = Phase.Working;
        }


        [HarmonyPatch(typeof(ChessBoard), "TryMakeMoveOnSquare")]
        [HarmonyPostfix]
        public static void TryMakeMoveOnSquare_Extrapolation(ref bool __result)
        {
            if (working == Phase.Working & __result == true)
            {
                working = Phase.Extrapolating;
                Plugin.Instance.LogThisInfo("Extrapolation started . . .");
                Plugin.Instance.ShowNotificationPopup("Extrapolation started . . .",
                Color.black, Color.white, false);
            }
        }

        private static void EnsureInitialization()
        {
            if (working != Phase.None) { return; }
            working = Phase.Working;
            originalMovesList = chessBoard.GetTheSavedMoves(-1);
            backIndex = 0;
            Plugin.Instance.LogThisInfo("Analysis started . . .");
            Plugin.Instance.ShowNotificationPopup("Analysis started . . .",
                Color.black, Color.white, false);
        }

        private static void TryRevertGameEnd(ChessBoard self)
        {
            if (self.phase != ChessBoard.Phase.EndGame) { return; }
            VictoryManager.instance.GameIsDone = false;

            self.phase = ChessBoard.Phase.PlayerMove;

        }

        private static void PreventAIFromRunning(ChessBoard self)
        {
            if (self.phase == ChessBoard.Phase.AIStart && working == Phase.Working)
            {
                self.phase = ChessBoard.Phase.PlayerMove;
                self.AI.Stop_Thread = true;
            }
        }

        private static void SaveEndGameMovelist()
        {
            Plugin.Instance.LogThisInfo("(S) pressed, current move-list saved for analysis ! ! !");
            Plugin.Instance.ShowNotificationPopup("move-list saved for analysis !",
                Color.black, Color.white, false, delegate ()
                {
                    working = Phase.None;
                    EnsureInitialization();
                });

        }

        private enum Phase
        {
            None,
            Working,
            Extrapolating
        }
    }

    public static class SecretUnlockedPatch
    {
        [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.IsRaceUnlocked))]
        [HarmonyPrefix]
        public static bool Unlock(PlayerInput __instance, [HarmonyArgument(0)] Race race, ref bool __result)
        {
            if (race == Race.Cthulhu)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    public static class PieceClonerMod
    {
        [HarmonyPatch(typeof(ChessBoard), "OnMouseDown")]
        [HarmonyPrefix]
        public static bool Prefix(ChessBoard __instance)
        {

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (PlayerInput.Instance != null && PlayerInput.Instance.upgradeselect_hook.is_enabled)
                {
                    return false;
                }
                Coords coords = __instance.MousePosToSquare(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                if (!ChessBoard.IsValidSquare(coords.x, coords.y))
                {
                    return false;
                }

                Plugin.Instance.LogThisInfo($". . . board clicked on {coords.x}, {coords.y} . . .");
                if (__instance.selectedPiece > 0)
                {
                    ChessBoardState.Piece sp = __instance.state.pieces[__instance.selectedPiece];
                    byte target = __instance.state.GetPieceOnSquare(coords.x, coords.y);
                    if (target > 0 || sp.type == ChessBoardState.Piece.Type_King)
                    {
                        return false;
                    }
                    ChessBoardState.Team team = ChessBoardState.Team.None;
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.AltGr) || Input.GetKey(KeyCode.RightAlt))
                    {
                        team = ChessBoardState.Team.Black;
                    }
                    else
                    {
                        team = ChessBoardState.Team.White;
                    }
                    __instance.state.AddPiece(sp.race, sp.type, (byte)ChessBoardState.PosToIndex(coords.x, coords.y), team, __instance.state.GetEmptyPieceIndex());
                    __instance.phase = ChessBoard.Phase.None;
                }
                return false;
            }
            return true;
        }
    }

    public static class PieceRemoverMod
    {
        [HarmonyPatch(typeof(ChessBoard), "OnMouseDown")]
        [HarmonyPostfix]
        public static void Postfix(ChessBoard __instance)
        {

            if (Input.GetKey(KeyCode.Delete))
            {
                if (PlayerInput.Instance != null && PlayerInput.Instance.upgradeselect_hook.is_enabled)
                {
                    return;
                }

                if (__instance.selectedPiece > 0)
                {
                    ChessBoardState.Piece sp = __instance.state.pieces[__instance.selectedPiece];
                    if (sp.type == ChessBoardState.Piece.Type_King)
                    {
                        return;
                    }
                    __instance.state.SetPieceOnSquare(sp.square, 0, false);
                    Plugin.Instance.LogThisInfo($" . . . piece deleted successfully . . . ");
                    __instance.phase = ChessBoard.Phase.None;
                    __instance.pieceGraphics[__instance.selectedPiece].SetVisible(false);
                }
            }
        }
    }

    public static class PieceMoverMod
    {
        [HarmonyPatch(typeof(ChessBoard), "OnMouseDown")]
        [HarmonyPrefix]
        public static bool Prefix(ChessBoard __instance)
        {

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (PlayerInput.Instance != null && PlayerInput.Instance.upgradeselect_hook.is_enabled)
                {
                    return false;
                }
                Coords coords = __instance.MousePosToSquare(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                if (!ChessBoard.IsValidSquare(coords.x, coords.y))
                {
                    return false;
                }

                Plugin.Instance.LogThisInfo($". . . board clicked on {coords.x}, {coords.y} . . .");
                byte p = __instance.selectedPiece;
                if (p > 0)
                {
                    byte target = __instance.state.GetPieceOnSquare(coords.x, coords.y);
                    if (target > 0)
                    {
                        return false;
                    }
                    //ChessBoardState.Team team = __instance.state.GetTeam(p);
                    __instance.state.SetPieceOnSquare(__instance.state.pieces[p].square, 0, false);
                    __instance.state.SetPieceOnSquare((byte)Utility.PosToIndex(coords.x, coords.y), p, true);
                    __instance.UpdatePieceGraphics(false, __instance.state);
                    MoveAnimations.TweenStateToState(__instance.state, __instance.state, ChessBoard.Phase.None);
                    __instance.phase = ChessBoard.Phase.None;
                }
                return false;
            }
            return true;
        }
    }

    public static class WolfSwapJump
    {
        //[HarmonyPatch(typeof(PieceAction), nameof(PieceAction.execute))]
        //[HarmonyPostfix]
        //public static void Postfix(PieceAction __instance)
        //{
        //    if (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift))
        //    {
        //        Piece piece = __instance.piece_hook;
        //        Piece boardPiece = piece.board.onSquare(__instance.newPosition);
        //        if (Piece.CheckFriendlyCaptureDoubleTurn(piece, boardPiece))
        //        {
        //            boardPiece = piece.team.newPiece(boardPiece.gameObject, new BoardSquare[] { __instance.oldPosition });
        //            boardPiece.DoTransition(__instance.newPosition, __instance.oldPosition, false, 0.2f);
        //        }
        //    }
        //}
    }



    public static class WolfMultiJump
    {
        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.KnightMoves))]
        [HarmonyPostfix]
        public static void Postfix(ChessBoardState state, byte pc, bool move_only, bool IsAIMove, bool Tengu_Special_moves, ref List<PieceMoves.Move> __result)
        {
            List<PieceMoves.Move> list = Utility.NormalKnightMoves(state, pc, move_only, IsAIMove, Tengu_Special_moves);
            //List<PieceMoves.Move> list3 = new List<PieceMoves.Move>();
            //ChessBoardState.Team team = state.GetTeam(pc);
            if (ChessBoardState.IsMoveARepeatMove(state.last_move) && state.last_move.piece_moved == pc)
            {
                using (List<PieceMoves.Move>.Enumerator enumerator2 = list.GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        PieceMoves.Move move3 = enumerator2.Current;
                        if (move3.move_type == 9)
                        {
                            __result.Add(move3);
                        }
                    }
                }
            }
            //foreach (PieceMoves.Move move4 in list)
            //{
            //    if (move4.move_type == 9 && state.pieces[(int)move4.piece_targeted].IsPawn() && state.GetTeam(move4.piece_targeted) == team)
            //    {
            //        move4.option = 206;
            //    }
            //    list3.Add(move4);
            //}
            //list = list3;
        }
    }


    public static class AllFactionsCanDoQueenPromotion
    {
        [HarmonyPatch(typeof(UpgradeSelect), nameof(UpgradeSelect.Open), typeof(byte))]
        [HarmonyPostfix]
        public static void Postfix(UpgradeSelect __instance, [HarmonyArgument(0)] byte p)
        {
            ChessBoardState.Team bias = Plugin.ModType.Queen_promotion.Bias();
            if (bias != ChessBoardState.Team.None && ChessBoard.Instance.state.GetTeam(p) != bias) { return; }
            __instance.new_piece_to_upgrade = p;
            Vector2 pieceWorldPosition = InstanceMonoBehaviour<ChessBoard>.Instance.GetPieceWorldPosition(p, InstanceMonoBehaviour<ChessBoard>.Instance.state);
            pieceWorldPosition.x -= 4f;
            pieceWorldPosition.y += 4f;
            Vector3 vector = Camera.main.WorldToScreenPoint(pieceWorldPosition);
            vector = __instance.gameObject.transform.InverseTransformPoint(vector);
            if (InstanceMonoBehaviour<ChessBoard>.Instance.state.pieces[p].square >= 56)
            {
                for (int i = 0; i < __instance.pieces.Count; i++)
                {
                    __instance.pieces[i].transform.localPosition = new Vector3(vector.x, vector.y + (float)(120 * i) + 60f, 1f);
                    __instance.pieces[i].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
            }
            else if (InstanceMonoBehaviour<ChessBoard>.Instance.state.pieces[p].square <= 7)
            {
                float num = 420f;
                for (int j = 0; j < __instance.pieces.Count; j++)
                {
                    __instance.pieces[j].transform.localPosition = new Vector3(vector.x, vector.y + (float)(120 * j) - num, 1f);
                    __instance.pieces[j].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
            }
            __instance.pieces[0].gameObject.SetActive(true);
        }


    }

    public static class AllFactionsCanCaptureOwnPawns
    {
        //public static bool Prefix(Piece __instance, [HarmonyArgument(0)] int pid, [HarmonyArgument(1)] Piece target, ref bool __result)
        //{
        //    if (target != null && !target.Invincible && !__instance.patterns[pid].allBoardMove && !__instance.patterns[pid].cantTarget &&
        //    target.team == __instance.team && !__instance.PieceType.IsPawn() && target.PieceType.IsPawn() && !__instance.patterns[pid].switchPosition)
        //    {

        //        __result = true;
        //        return false;
        //    }
        //    return true;
        //} 

        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.CanCapture))]
        [HarmonyPrefix]
        public static bool Prefix(ChessBoardState __instance, [HarmonyArgument(0)] ChessBoardState.Team team, [HarmonyArgument(2)] byte capturer, [HarmonyArgument(3)] byte target, ref bool __result)
        {
            ChessBoardState.Team bias = Plugin.ModType.Self_pawn_capture.Bias();
            if ((bias == ChessBoardState.Team.None || team == bias) && __instance.GetTeam(target) == team
                && __instance.pieces[target].IsPawn() && !__instance.pieces[capturer].IsPawn())
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    public static class AllFactionsHaveUnlimitedRange
    {

        [HarmonyPatch(typeof(PieceMoves), "StraightSlideMoves")]
        [HarmonyPrefix]
        public static void PreFix_Orthogonal([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(2)] byte pc, [HarmonyArgument(3)] ref int __result)
        {
            ChessBoardState.Team bias = Plugin.ModType.Unlimited_range.Bias();
            if (bias != ChessBoardState.Team.None && ChessBoard.Instance.state.GetTeam(pc) != bias) { return; }
            ChessBoardState.Piece p = state.pieces[pc];
            if (p.IsQueen() || p.IsRook())
            {
                __result = 7;
            }
        }

        [HarmonyPatch(typeof(PieceMoves), "AngleSlideMoves")]
        [HarmonyPrefix]
        public static void PreFix_Diagonal([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(2)] byte pc, [HarmonyArgument(3)] ref int __result)
        {
            ChessBoardState.Team bias = Plugin.ModType.Unlimited_range.Bias();
            if (bias != ChessBoardState.Team.None && ChessBoard.Instance.state.GetTeam(pc) != bias) { return; }
            ChessBoardState.Piece p = state.pieces[pc];
            if (p.IsQueen() || p.type == ChessBoardState.Piece.Type_Bishop)
            {
                __result = 7;
            }
        }
    }

    public static class AllFactionsHaveFullBoardMermaidSwaps
    {
        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.RookMoves))]
        [HarmonyPostfix]
        public static void Postfix_Rook([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(1)] byte pc, List<PieceMoves.Move> __result)
        {
            ChessBoardState.Team bias = Plugin.ModType.Tunnel_swaps.Bias();
            if (bias != ChessBoardState.Team.None && ChessBoard.Instance.state.GetTeam(pc) != bias) { return; }
            byte b = (state.GetTeam(pc) == ChessBoardState.Team.White) ? state.white_king : state.black_king;
            __result.Add(new PieceMoves.Move(pc, b, 5, state.pieces[b].square, state.pieces[pc].square, 0));
        }

        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.BishopMoves))]
        [HarmonyPostfix]
        public static void Postfix_Bishop([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(1)] byte pc, [HarmonyArgument(3)] bool IsAIMove, List<PieceMoves.Move> __result)
        {
            ChessBoardState.Team bias = Plugin.ModType.Tunnel_swaps.Bias();
            if (bias != ChessBoardState.Team.None && ChessBoard.Instance.state.GetTeam(pc) != bias) { return; }
            byte square = state.pieces[pc].square;
            foreach (byte b in state.GetPiecesOfTypeAndTeam(1, state.GetTeam(pc)))
            {
                Utility.PawnMoveOnSquare(new PieceMoves.Move(pc, b, 5, state.pieces[b].square, square, 0), b, square, state, __result, IsAIMove);
            }
        }

        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.KnightMoves))]
        [HarmonyPostfix]
        public static void Postfix_Knight([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(1)] byte pc, List<PieceMoves.Move> __result)
        {
            ChessBoardState.Team bias = Plugin.ModType.Tunnel_swaps.Bias();
            if (bias != ChessBoardState.Team.None && ChessBoard.Instance.state.GetTeam(pc) != bias) { return; }
            byte square = state.pieces[pc].square;
            using (List<byte>.Enumerator enumerator = state.GetPiecesOfTypeAndTeam(5, state.GetTeam(pc)).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    byte b = enumerator.Current;
                    __result.Add(new PieceMoves.Move(pc, b, 5, state.pieces[b].square, square, 0));
                }
            }
        }
    }

    public static class SecretSpellCastManaBonus
    {
        private static int whiteManaBonus = 0;
        private static int blackManaBonus = 0;

        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.AddManaForTeam))]
        [HarmonyPrefix]
        public static void Prefix(ChessBoardState __instance, [HarmonyArgument(0)] ChessBoardState.Team team, [HarmonyArgument(1)] ref int add)
        {
            ChessBoardState.Team bias = Plugin.ModType.Super_Secrets.Bias();
            if (add > 0 && (bias == ChessBoardState.Team.None || __instance.GetCurrentTeam() == bias))
            {
                add += team == ChessBoardState.Team.White ? whiteManaBonus : blackManaBonus;
            }
        }

        [HarmonyPatch(typeof(ChessBoard), "TryMakeMoveOnSquare")]
        [HarmonyPostfix]
        public static void PostFix(ChessBoard __instance, ref bool __result)
        {
            ChessBoardState.Team bias = Plugin.ModType.Super_Secrets.Bias();
            if (__result && (bias == ChessBoardState.Team.None || __instance.state.GetCurrentTeam() == bias))
            {
                byte t = __instance.next_move.move_type;

                if (t == 20 || t == 22 || t == 23 || t == 24 || t == 25)
                {
                    //__instance.state.pieces[__instance.next_move.piece_moved].type = t;
                    if (__instance.state.GetCurrentTeam() == ChessBoardState.Team.White)
                    {
                        whiteManaBonus++;
                        Plugin.Instance.LogThisInfo($" . . . Mana Income Bonus increased to +{whiteManaBonus}. . . ");
                    }
                    else
                    {
                        blackManaBonus++;
                        Plugin.Instance.LogThisInfo($" . . . Mana Income Bonus increased to +{blackManaBonus}. . . ");
                    }
                    
                }
            }
        }

        [HarmonyPatch(typeof(ChessBoard), nameof(ChessBoard.Init))]
        [HarmonyPostfix]
        public static void Init()
        {
            whiteManaBonus = 0;
            blackManaBonus = 0;
        }
    }

    public static class WaterTilesBoostAlliesAndDebuffEnemies
    {
        [HarmonyPrepare]
        public static void Init()
        {
            if (Plugin.ModType.Water_of_Magic.Bias() == ChessBoardState.Team.None)
            {
                Plugin.Instance.LogThisInfo("Please use a meaningful bias for this mod.(White/Black)");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.DoEndOfTurnAdjustments))]
        public static void Postfix(ChessBoardState __instance)
        {
            foreach (byte square in __instance.GetWaterTiles())
            {
                if (Plugin.ModType.Water_of_Magic.Bias() == ChessBoardState.Team.None)
                { 
                    return;
                }
                byte p = __instance.GetPieceOnSquare(square);
                if (__instance.GetTeam(p) == Plugin.ModType.Water_of_Magic.Bias())
                {
                    if (__instance.pieces[p].IsPawn() || __instance.pieces[p].IsKnight() ||
                        __instance.pieces[p].type == ChessBoardState.Piece.Type_Bishop)
                    {
                        __instance.pieces[p].AddFlag(ChessBoardState.Piece.Flag_Invincible);
                    }
                    if (__instance.pieces[p].IsPawn())
                    {
                        __instance.pieces[p].AddFlag(ChessBoardState.Piece.Flag_Naga_Queen_Boost);
                    }
                    __instance.pieces[p].AddFlag(ChessBoardState.Piece.Flag_War_Drums);
                }
                else
                {
                    __instance.pieces[p].AddFlag(ChessBoardState.Piece.Flag_Paralyzed);
                }
            }
        }
    }

    public static class AliensCanChainTheirExtraTurns
    {
        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.QueenMoves))]
        [HarmonyPrefix]
        public static bool QueenMoves_Patch(ChessBoardState state, bool time_shift, byte pc, ref List<PieceMoves.Move> __result)
        {
            ChessBoardState.Team team = state.GetTeam(pc);
            if (Plugin.ModType.Super_Aliens.Bias() != ChessBoardState.Team.None &&
                team != Plugin.ModType.Super_Aliens.Bias() || state.pieces[pc].race != 1) { return true; }
            PieceMoves.CanCapture canCapture = time_shift ? PieceMoves.CanCapture.trunk_only : PieceMoves.CanCapture.all;
            List<PieceMoves.Move> list = new List<PieceMoves.Move>();
            List<PieceMoves.Move> list2 = Utility.StraightSlideMoves(state, team, pc, 3, 0, canCapture, false, false);
            list2.AddRange(PieceMoves.AngleSlideMoves(state, team, pc, 3, 0, canCapture, false, false, false));
            using (List<PieceMoves.Move>.Enumerator enumerator = list2.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    PieceMoves.Move move = enumerator.Current;
                    if (move.move_type != 9 || (move.move_type == 9 && !ChessBoardState.IsMoveARepeatMove(state.last_move)))
                    {
                        move.option = PieceMoves.Move.Option_Time_Shift;
                    }
                    list.Add(move);
                }
                __result = list; return false;
            }
        }

        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.RookMoves))]
        [HarmonyPrefix]
        public static bool RookMoves_Patch(ChessBoardState state, byte pc, bool time_shift, ref List<PieceMoves.Move> __result)
        {

            ChessBoardState.Team team = state.GetTeam(pc);
            if (Plugin.ModType.Super_Aliens.Bias() != ChessBoardState.Team.None &&
                team != Plugin.ModType.Super_Aliens.Bias() || state.pieces[pc].race != 1) { return true; }
            PieceMoves.CanCapture capture_possible = time_shift ? PieceMoves.CanCapture.trunk_only : PieceMoves.CanCapture.all;
            List<PieceMoves.Move> list = new List<PieceMoves.Move>();
            using (List<PieceMoves.Move>.Enumerator enumerator = Utility.StraightSlideMoves(state, team, pc, 4, 0, capture_possible, false, false).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    PieceMoves.Move move = enumerator.Current;
                    if (move.move_type != PieceMoves.Move.Type_Capture)
                    {
                        move.option = 213;
                    }
                    list.Add(move);
                }
                __result = list;
                return false;
            }
        }

        [HarmonyPatch(typeof(PieceMoves), "PawnMoveOnSquare")]
        [HarmonyPrefix]
        public static bool PawnMoveOnSquare_Patch(PieceMoves.Move move, byte piece, byte square, ChessBoardState state, List<PieceMoves.Move> list)
        {
            if (Plugin.ModType.Super_Aliens.Bias() != ChessBoardState.Team.None &&
                state.GetTeam(piece) != Plugin.ModType.Super_Aliens.Bias()) { return true; }
            if (!state.CanPromoteOnSquare(piece, (int)square) && move.option == 0 &&
                move.move_type != PieceMoves.Move.Type_Capture &&
                state.last_move.option == PieceMoves.Move.Option_Time_Harvest &&
                state.pieces[state.last_move.piece_moved].type == ChessBoardState.Piece.Type_Rook)
            {
                move.option = PieceMoves.Move.Option_Time_Shift;
                list.Add(move);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.BishopMoves))]
        [HarmonyPrefix]
        public static bool BishopMoves_Patch(ChessBoardState state, byte pc, bool time_shift, ref List<PieceMoves.Move> __result)
        {
            if ((Plugin.ModType.Super_Aliens.Bias() != ChessBoardState.Team.None &&
                state.GetTeam(pc) != Plugin.ModType.Super_Aliens.Bias()) || state.pieces[pc].race != 1) { return true; }
            ChessBoardState.Piece piece = state.pieces[(int)pc];
            ChessBoardState.Team team = state.GetTeam(pc);
            List<PieceMoves.Move> list = new List<PieceMoves.Move>();
            PieceMoves.CanCapture canCapture = time_shift ? PieceMoves.CanCapture.trunk_only : PieceMoves.CanCapture.all;
            if (piece.type != 3)
            {
                return true;
            }
            if (!ChessBoardState.IsMoveARepeatMove(state.last_move) || state.pieces[(int)state.last_move.piece_moved].type != 3 || state.GetTeam(state.last_move.piece_moved) != team)
            {
                foreach (PieceMoves.Move move in PieceMoves.AngleSlideMoves(state, team, pc, 1, 0, canCapture, false, false, false))
                {
                    move.option = PieceMoves.Move.Option_Remote_Boost;
                    list.Add(move);
                }
                list.AddRange(PieceMoves.AngleSlideMoves(state, team, pc, 4, 0, canCapture, false, false, true));
                __result = list; return false;
            }
            if (state.last_move.piece_moved != pc)
            {
                foreach (PieceMoves.Move move in PieceMoves.AngleSlideMoves(state, team, pc, 1, 0, PieceMoves.CanCapture.trunk_only, false, false, false))
                {
                    move.option = PieceMoves.Move.Option_Remote_Boost;
                    list.Add(move);
                }
                foreach (PieceMoves.Move move in PieceMoves.AngleSlideMoves(state, team, pc, 3, 0, PieceMoves.CanCapture.trunk_only, false, false, true))
                {
                    move.option = PieceMoves.Move.Option_Time_Shift;
                    list.Add(move);
                }

                __result = list; return false;
            }
            return true;

        }

        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.GenerateMoves))]
        [HarmonyPrefix]
        public static bool GenerateMoves(ChessBoardState __instance, ChessBoardState.Team team, bool clean, bool IsAIMove, bool IgnoreRepeatMoves)
        {
            List<PieceMoves.Move> list = (team == ChessBoardState.Team.White) ? __instance.white_moves : __instance.black_moves;
            if (team == ChessBoardState.Team.None)
            {
                __instance.white_moves.Clear();
                __instance.black_moves.Clear();
            }
            else
            {
                list.Clear();
            }
            bool flag = ChessBoardState.IsMoveARepeatMove(__instance.last_move);
            bool timeShift = false;
            if (!IgnoreRepeatMoves && flag && __instance.last_move.option == PieceMoves.Move.Option_Time_Shift)
            {
                timeShift = true;
            }
            if (!flag) { return true; }
            for (byte b = 0; b < 64; b += 1)
            {
                byte pieceOnSquare = __instance.GetPieceOnSquare((int)b);
                if (pieceOnSquare >= 1 && !__instance.pieces[(int)pieceOnSquare].CheckFlag(16))
                {
                    if (!IgnoreRepeatMoves)
                    {
                        ChessBoardState.Piece piece = __instance.pieces[(int)__instance.last_move.piece_moved];
                        if (piece.race == 1)
                        {
                            if (piece.type == 3)
                            {
                                if (__instance.pieces[(int)pieceOnSquare].type != 3 && !timeShift)
                                {
                                    goto IL_2DC;
                                }
                                if (__instance.GetTeam(pieceOnSquare) != __instance.GetTeam(__instance.last_move.piece_moved))
                                {
                                    goto IL_2DC;
                                }
                            }
                            else if (piece.type == 4)
                            {
                                if (!__instance.pieces[(int)pieceOnSquare].IsPawn())
                                {
                                    goto IL_2DC;
                                }
                                if (__instance.GetTeam(pieceOnSquare) != __instance.GetTeam(__instance.last_move.piece_moved))
                                {
                                    goto IL_2DC;
                                }
                            }
                            else if (timeShift && (__instance.pieces[(int)pieceOnSquare].IsPawn() || __instance.GetTeam(pieceOnSquare) != __instance.GetTeam(__instance.last_move.piece_moved)))
                            {
                                goto IL_2DC;
                            }
                        }
                    }
                    ChessBoardState.Team team2 = __instance.GetTeam(pieceOnSquare);
                    if (team == ChessBoardState.Team.None)
                    {
                        list = ((team2 == ChessBoardState.Team.White) ? __instance.white_moves : __instance.black_moves);
                    }
                    else if (team2 != team)
                    {
                        goto IL_2DC;
                    }
                    foreach (PieceMoves.Move move in __instance.GetMovesForPiece(pieceOnSquare, timeShift, IsAIMove, true))
                    {
                        ChessBoardState.Team team3 = __instance.GetTeam(move.piece_moved);
                        if (true || move.move_type == 9 || ((team3 != ChessBoardState.Team.White || !__instance.CheckFlagOnSquare(move.to_square, 2)) && (team3 != ChessBoardState.Team.Black || !__instance.CheckFlagOnSquare(move.to_square, 4))))
                        {
                            list.Add(move);
                        }
                    }
                }
            IL_2DC:;
            }
            if (clean)
            {
                if (team == ChessBoardState.Team.None)
                {
                    __instance.CleanMoveList(ChessBoardState.Team.White, IsAIMove);
                    __instance.CleanMoveList(ChessBoardState.Team.Black, IsAIMove);
                    return false;
                }
                __instance.CleanMoveList(team, IsAIMove);
            }
            return false;
        }
    }

    public static class AngelsAbilitiesHaveMoreRange
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.CanCapture))]
        public static bool Zealot_OrthoShield_Patch(ChessBoardState __instance, ChessBoardState.Team team, bool move_only, byte capturer, byte target, ref bool __result)
        {
            if ((Plugin.ModType.Super_Aliens.Bias() == ChessBoardState.Team.None || __instance.GetTeam(target) == Plugin.ModType.Super_Aliens.Bias()) && __instance.pieces[target].race == 2 && __instance.pieces[target].type == ChessBoardState.Piece.Type_Bishop)
            {
                ChessBoardState.Piece targetPiece = __instance.pieces[target];
                ChessBoardState.Team team1 = __instance.GetTeam(target);
                int tx, ty, cx, cy;
                Utility.IndexToPos(targetPiece.square, out tx, out ty);
                Utility.IndexToPos(__instance.pieces[capturer].square, out cx, out cy);
                if (tx == cx || (team1 == ChessBoardState.Team.White && ty <= cy) ||
                    (team1 == ChessBoardState.Team.Black && ty >= cy))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.IsProtectedByAngelRook))]
        public static bool AngelRook_Patch(ChessBoardState __instance, byte pc, ref bool __result)
        {
            //if (__instance.pieces[(int)pc].race == 2 && __instance.pieces[(int)pc].type == 4)
            //{
            //    __result = false;
            //    return false;
            //}
            ChessBoardState.Team team = __instance.GetTeam(pc);
            if (team != Plugin.ModType.Super_Aliens.Bias()) { return true; }
            byte b = __instance.pieces[(int)pc].square;
            for (int i = 0; i < 8; i++)
            {
                if (team == ChessBoardState.Team.White)
                {
                    b -= 8;
                }
                else
                {
                    b += 8;
                }
                if (ChessBoard.IsValidSquare((int)b))
                {
                    byte pieceOnSquare = __instance.GetPieceOnSquare((int)b);
                    if (pieceOnSquare > 0 && __instance.GetTeam(pieceOnSquare) == team && __instance.pieces[(int)pieceOnSquare].race == 2 && __instance.pieces[(int)pieceOnSquare].type == 4)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            b = (byte)(__instance.pieces[(int)pc].square - 4);
            for (int i = -4; i < 4; i++)
            {

                if (ChessBoard.IsValidSquare((int)b))
                {
                    byte pieceOnSquare = __instance.GetPieceOnSquare((int)b);
                    if (pieceOnSquare > 0 && __instance.GetTeam(pieceOnSquare) == team && __instance.pieces[(int)pieceOnSquare].race == 2 && __instance.pieces[(int)pieceOnSquare].type == 4)
                    {
                        __result = true;
                        return false;
                    }
                }
                b++;
            }
            __result = false;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.DoEndOfTurnAdjustments))]
        public static void DoEndOfTurnAdjustments(ChessBoardState __instance, PieceMoves.Move mov) {
            //if (Plugin.Instance.teamFavor == ChessBoardState.Team.None) { return; }
            for (byte b = 0; b < 64; b += 1) {
                byte pieceOnSquare = __instance.GetPieceOnSquare((int)b);
                if (pieceOnSquare != 0 && __instance.pieces[pieceOnSquare].race == ChessBoardState.Piece.Race_Angels
                    && !__instance.pieces[pieceOnSquare].IsPawn() && (Plugin.ModType.Super_Aliens.Bias() == ChessBoardState.Team.None || __instance.GetTeam(pieceOnSquare) == Plugin.ModType.Super_Aliens.Bias())) {
                    using (List<byte>.Enumerator enumerator = __instance.GetSurroundingTiles(__instance.pieces[pieceOnSquare].square).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							byte s = enumerator.Current;
							byte pieceOnSquare4 = __instance.GetPieceOnSquare((int)s);
                            ChessBoardState.Team team2 = __instance.GetTeam(pieceOnSquare4);

                            if (pieceOnSquare4 > 0 && team2 == __instance.GetOpposingTeam(pieceOnSquare) && (Plugin.ModType.Super_Aliens.Bias() == ChessBoardState.Team.None || team2 != Plugin.ModType.Super_Aliens.Bias()) && !__instance.pieces[(int)pieceOnSquare4].IsPawn())
							{
								__instance.pieces[(int)pieceOnSquare4].AddFlag(64);
							}
						}
					}
                }
            }
        }
    }

    public static class Utility
    {
        public static string positionToString(int x, int y)
        {
            string result;
            switch (x)
            {
                case 0:
                    result = "A" + (y + 1).ToString();
                    break;
                case 1:
                    result = "B" + (y + 1).ToString();
                    break;
                case 2:
                    result = "C" + (y + 1).ToString();
                    break;
                case 3:
                    result = "D" + (y + 1).ToString();
                    break;
                case 4:
                    result = "E" + (y + 1).ToString();
                    break;
                case 5:
                    result = "F" + (y + 1).ToString();
                    break;
                case 6:
                    result = "G" + (y + 1).ToString();
                    break;
                case 7:
                    result = "H" + (y + 1).ToString();
                    break;
                default:
                    result = "error";
                    break;
            }
            return result;
        }
        public static bool IsTeamAI(ChessBoardState.Team team)
        {
            return (team == ChessBoardState.Team.White && PlayerInput.Instance.player_1_is_AI) || (team == ChessBoardState.Team.Black && PlayerInput.Instance.player_2_is_AI);
        }

        public static void PawnMoveOnSquare(PieceMoves.Move move, byte piece, byte square, ChessBoardState state, List<PieceMoves.Move> list, bool IsAIMove)
        {
            if (state.CanPromoteOnSquare(piece, (int)square))
            {
                if (!IsAIMove)
                {
                    PieceMoves.Move move2 = new PieceMoves.Move();
                    move2.CopyFrom(move);
                    move2.option = 204;
                    list.Add(move2);
                    return;
                }
                PieceMoves.Move move3 = new PieceMoves.Move();
                move3.CopyFrom(move);
                move3.option = 202;
                list.Add(move3);
                if (state.pieces[(int)move.piece_moved].race != 11)
                {
                    PieceMoves.Move move4 = new PieceMoves.Move();
                    move4.CopyFrom(move);
                    move4.option = 200;
                    list.Add(move4);
                    PieceMoves.Move move5 = new PieceMoves.Move();
                    move5.CopyFrom(move);
                    move5.option = 201;
                    list.Add(move5);
                }
                if (state.pieces[(int)move.piece_moved].race == 9)
                {
                    PieceMoves.Move move6 = new PieceMoves.Move();
                    move6.CopyFrom(move);
                    move6.option = 203;
                    list.Add(move6);
                    return;
                }
            }
            else
            {
                list.Add(move);
            }
        }

        public static void IndexToPos(int index, out int item, out int item2)
        {
            item = index % 8;
            item2 = index >> 3;
        }

        public static int PosToIndex(int x, int y)
        {
            return (y << 3) + x;
        }

        public static List<PieceMoves.Move> NormalKnightMoves(ChessBoardState state, byte pc, bool move_only, bool IsAIMove, bool Tengu_Special_moves)
        {
            List<PieceMoves.Move> list = new List<PieceMoves.Move>();
            ChessBoardState.Piece piece = state.pieces[(int)pc];
            ChessBoardState.Team team = state.GetTeam(pc);
            int item, item2;
            Utility.IndexToPos((int)piece.square, out item, out item2);
            int sqr_x = item;
            int sqr_y = item2;
            int num = (piece.type == 63) ? 3 : 2;
            for (int i = 0; i < 8; i++)
            {
                switch (i)
                {
                    case 0:
                        sqr_x = item - 1;
                        sqr_y = item2 + num;
                        break;
                    case 1:
                        sqr_x = item + 1;
                        sqr_y = item2 + num;
                        break;
                    case 2:
                        sqr_x = item + 1;
                        sqr_y = item2 - num;
                        break;
                    case 3:
                        sqr_x = item - 1;
                        sqr_y = item2 - num;
                        break;
                    case 4:
                        sqr_x = item - num;
                        sqr_y = item2 + 1;
                        break;
                    case 5:
                        sqr_x = item - num;
                        sqr_y = item2 - 1;
                        break;
                    case 6:
                        sqr_x = item + num;
                        sqr_y = item2 - 1;
                        break;
                    case 7:
                        sqr_x = item + num;
                        sqr_y = item2 + 1;
                        break;
                }
                Utility.AddMoveToList(state, sqr_x, sqr_y, pc, team, move_only, false, 0, list, 0, false, false);
            }

            return list;
        }

        public static List<PieceMoves.Move> StraightSlideMoves(ChessBoardState state, ChessBoardState.Team team, byte pc, int distance, byte option, PieceMoves.CanCapture capture_possible, bool third_eye, bool can_jump)
        {
            List<PieceMoves.Move> list = new List<PieceMoves.Move>();
            Utility.IndexToPos((int)state.pieces[(int)pc].square, out int item, out int item2);
            if (!can_jump)
            {
                can_jump = state.pieces[(int)pc].CheckFlag(128);
            }
            for (int i = 0; i < 4; i++)
            {
                int num = 0;
                int num2 = 0;
                switch (i)
                {
                    case 0:
                        num = 1;
                        num2 = 0;
                        break;
                    case 1:
                        num = 0;
                        num2 = -1;
                        break;
                    case 2:
                        num = -1;
                        num2 = 0;
                        break;
                    case 3:
                        num = 0;
                        num2 = 1;
                        break;
                }
                int num3 = item + num;
                int num4 = item2 + num2;
                for (int j = 0; j < distance; j++)
                {
                    if (!ChessBoard.IsValidSquare(num3, num4))
                    {
                        if (!third_eye)
                        {
                            break;
                        }
                        if (num3 < 0)
                        {
                            num3 = 7;
                        }
                        else
                        {
                            if (num3 <= 7)
                            {
                                break;
                            }
                            num3 = 0;
                        }
                    }
                    if (!Utility.AddMoveToList(state, num3, num4, pc, team, capture_possible != PieceMoves.CanCapture.all, can_jump, option, list, 0, false, false))
                    {
                        break;
                    }
                    num3 += num;
                    num4 += num2;
                }
            }
            return list;
        }



        private static bool AddMoveToList(ChessBoardState state, int sqr_x, int sqr_y, byte pc, ChessBoardState.Team team, bool move_only, bool can_jump, byte option, List<PieceMoves.Move> list, byte movetype_override = 0, bool skip = false, bool capture_only = false)
        {
            if (!ChessBoard.IsValidSquare(sqr_x, sqr_y))
            {
                return false;
            }
            byte b = (byte)ChessBoardState.PosToIndex(sqr_x, sqr_y);
            byte pieceOnSquare = state.GetPieceOnSquare((int)b);
            if (pieceOnSquare == 0 && capture_only)
            {
                return false;
            }
            if (pieceOnSquare > 0 && !state.CanCapture(team, move_only, pc, pieceOnSquare))
            {
                return state.CanMoveThrough(pc, pieceOnSquare) || can_jump;
            }
            if (skip)
            {
                return pieceOnSquare == 0;
            }
            byte b2 = (byte)(move_only ? 2 : 1);
            if (pieceOnSquare > 0)
            {
                b2 = 9;
            }
            if (movetype_override > 0)
            {
                b2 = movetype_override;
            }
            list.Add(new PieceMoves.Move(pc, pieceOnSquare, b2, b, state.pieces[(int)pc].square, option));
            return can_jump || b2 != 9;
        }
    }

    public static class Extensions
    {
        public static ChessBoardState.Team Bias(this Plugin.ModType modType)
        {
            return Plugin.biasDict[modType];
        }

        public static string CleanName(this Plugin.ModType modType)
        {
            return modType.ToString().Replace('_', ' ');
        }

        public static TextMeshProUGUI[] turnNumbers(this MoveHistoryUI self)
        {
            FieldInfo fieldInfo = typeof(MoveHistoryUI).GetField("turnNumbers", BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

            return (TextMeshProUGUI[])fieldInfo.GetValue(self);
        }

        public static List<ChessBoardState> saved_moves(this ChessBoard self)
        {
            FieldInfo fieldInfo = typeof(ChessBoard).GetField("saved_moves", BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

            return (List<ChessBoardState>)fieldInfo.GetValue(self);
        }

        public static void CleanMoveList(this ChessBoardState state, ChessBoardState.Team team, bool IsAIMove)
        {
            MethodInfo methodInfo = typeof(ChessBoardState).GetMethod("CleanMoveList", BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

            methodInfo.Invoke(state, new object[] { team, IsAIMove });
        }
    }

    public abstract class BalancedMod
    {
        private ChessBoardState.Team bias = ChessBoardState.Team.None;

        public void SetBias(ChessBoardState.Team team)
        {
            this.bias = team;
        }

        public ChessBoardState.Team GetBias()
        {
            return this.bias;
        }
    }
}
