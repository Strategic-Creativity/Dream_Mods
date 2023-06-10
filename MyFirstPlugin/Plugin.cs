using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;
using JetBrains.Annotations;
using System;
using TMPro;

namespace MyFirstPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        private Harmony[] balancedPatchers;
        private Harmony[] opPatchers;
        private Harmony fixesPatcher;

        private HashSet<ModType> patchesHashSet;
        public bool MultiplayerActive { get; private set; } = false;
        public bool AdventureModeActive { get; private set; } = false;

        private enum OpModType
        {
            Piece_Cloner,
            Piece_Remover,
            Piece_Mover,
            Moves_Analysis_Tool
        }
        private enum ModType
        {
            Wolf_multi_jump,
            All_factions_can_do_queen_promotion,
            All_factions_can_capture_own_pawns,
            All_factions_have_unlimited_range,
            All_factions_have_mermaid_swaps_always,
            Secret_spell_cast_mana_bonus,
            Water_tiles_boost_allies_debuff_enemies
        }

        public ChessBoardState.Team teamFavor { get; private set; }

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
            Logger.LogInfo($"DreamMods is loaded! \n Note : This is only tested on version v0.8 (May not work on other versions)");
            string[] balancedModsNames = System.Enum.GetNames(typeof(ModType));
            this.balancedPatchers = new Harmony[balancedModsNames.Length];
            for (int i = 0; i < this.balancedPatchers.Length; i++)
            {
                this.balancedPatchers[i] = new Harmony(balancedModsNames[i]);
            }
            string[] opModsNames = System.Enum.GetNames(typeof(OpModType));
            this.opPatchers = new Harmony[opModsNames.Length];
            for (int i = 0; i < this.opPatchers.Length; i++)
            {
                this.opPatchers[i] = new Harmony(opModsNames[i]);
            }
            Plugin.Instance = this;
            this.fixesPatcher = new Harmony("fixesPatcher");
            this.patchesHashSet = new HashSet<ModType>();
            this.teamFavor = ChessBoardState.Team.White;
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
            
        }

        public void UpdateHook() { }
        private void Update()
        {
            this.UpdateHook();
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.KeypadPlus))
            {
                LogPatternClassInfo();
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.Tab))
            {
                LogOpModInfo();
                LogBalancedModInfo();
            }

            if (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift))
            {
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F1))
                {
                    this.TryUnpatch(ModType.Wolf_multi_jump);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
                {
                    this.TryUnpatch(ModType.All_factions_can_do_queen_promotion);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
                {
                    this.TryUnpatch(ModType.All_factions_can_capture_own_pawns);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
                {
                    this.TryUnpatch(ModType.All_factions_have_unlimited_range);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F5))
                {
                    this.TryUnpatch(ModType.All_factions_have_mermaid_swaps_always);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F6))
                {
                    this.TryUnpatch(ModType.Secret_spell_cast_mana_bonus);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F7))
                {
                    this.TryUnpatch(ModType.Water_tiles_boost_allies_debuff_enemies);
                    return;
                }
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F1))
            {
                this.TryPatch(ModType.Wolf_multi_jump);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
            {
                this.TryPatch(ModType.All_factions_can_do_queen_promotion);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
            {
                this.TryPatch(ModType.All_factions_can_capture_own_pawns);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
            {
                this.TryPatch(ModType.All_factions_have_unlimited_range);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F5))
            {
                this.TryPatch(ModType.All_factions_have_mermaid_swaps_always);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F6))
            {
                this.TryPatch(ModType.Secret_spell_cast_mana_bonus);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F7))
            {
                this.TryPatch(ModType.Water_tiles_boost_allies_debuff_enemies);
                return;
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
                LogThisInfo($"{mod.ToString()} mod is already active");
                return;
            }
            this.PatchThisBalancedMod(mod);
            LogThisInfo($". . . {mod.ToString()} mod activated . . .");
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
                LogThisInfo($"{mod.ToString()} mod is already disabled");
                return;
            }
            this.UnpatchThisBalancedMod(mod);
            LogThisInfo($". . . {mod.ToString()} mod disabled . . .");
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
                //case ModType.Wolf_multi_jump:
                //    this.balancedPatchers[(int)modType].PatchAll(typeof(Wolf_multi_jump));
                //    break;
                case ModType.All_factions_can_do_queen_promotion:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsCanDoQueenPromotion));
                    break;
                case ModType.All_factions_can_capture_own_pawns:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsCanCaptureOwnPawns));
                    break;
                case ModType.All_factions_have_unlimited_range:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsHaveUnlimitedRange));
                    break;
                case ModType.All_factions_have_mermaid_swaps_always:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsHaveFullBoardMermaidSwaps));
                    break;
                case ModType.Secret_spell_cast_mana_bonus:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(SecretSpellCastManaBonus));
                    break;
                case ModType.Water_tiles_boost_allies_debuff_enemies:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(WaterTilesBoostAlliesDebuffEnemies));
                    break;
                default:
                    Logger.LogError($"{modType.ToString()} mod is currently not supported");
                    return;
            }
        }

        private void UnpatchThisBalancedMod(ModType modType)
        {
            switch (modType)
            {
                case ModType.Wolf_multi_jump:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.All_factions_can_do_queen_promotion:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.All_factions_can_capture_own_pawns:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.All_factions_have_unlimited_range:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.All_factions_have_mermaid_swaps_always:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Secret_spell_cast_mana_bonus:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.Water_tiles_boost_allies_debuff_enemies:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                default:
                    Logger.LogError($"{modType.ToString()} mod is currently not supported");
                    return;
            }
        }

        private void LogBalancedModInfo()
        {
            //int expectedCapacity = System.Enum.GetValues(typeof(ModType)).Length * 4 + 3;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("List of balanced mods {(name) then (hotkey) then (state)} :");
            int i = 1;
            foreach (string mod in System.Enum.GetNames(typeof(ModType)))
            {
                string state = patchesHashSet.Contains((ModType)System.Enum.Parse(typeof(ModType), mod)) ? "(active)" : "";
                sb.AppendLine($"{i}.\t{mod.ToString()} ({'F' + i.ToString()}) {state}");
                i++;
            }
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
            foreach (string opMod in System.Enum.GetNames(typeof(OpModType)))
            {
                sb.Append($"{opMod} (Usage) : ");
                sb.AppendLine(this.GetOpModUsageInfoString((OpModType)System.Enum.Parse(typeof(OpModType), opMod)));
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
            originalMovesList = new List<ChessBoardState> (0);
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
                EnsureInitialization();
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
            UnityEngine.Object.FindObjectOfType<MoveHistoryUI>().UpdateMoves(self, null);
            UnityEngine.Object.FindObjectOfType<GameBoardArrows>().RemoveAIMoveArrow();
            self.phase = ChessBoard.Phase.Animation;
            //Plugin.Instance.LogThisInfo("All other things working fine");
        }

        private static void Move_Forward(ChessBoard self)
        {
            if (working != Phase.Working || self.phase != ChessBoard.Phase.PlayerMove) { return; }
            backIndex--;
            if (backIndex < 0) { 
                backIndex = 0;
                working = Phase.None;
                Plugin.Instance.LogThisInfo($"(->) pressed, Analysis stopped !");
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

        //[HarmonyPatch(typeof(MoveHistoryUI), nameof(MoveHistoryUI.UpdateMoves))]
        //[HarmonyPrefix]
        //public static bool UpdateMoves_MoveHistoryUI(MoveHistoryUI __instance, [HarmonyArgument(0)]ChessBoard board, [HarmonyArgument(1)]ChessBoardState add_state)
        //{
        //    if (working != Phase.Working)
        //    {
        //        return true;
        //    }
        //    for (int i = 0; i < __instance.movesUIP1.Length; i++)
        //    {
        //        __instance.movesUIP1[i].text = "";
        //        __instance.movesUIP2[i].text = "";
        //        __instance.turnNumbers()[i].text = (i + 1).ToString();
        //    }
        //    int num = 10;
        //    if (board.state.GetCurrentTeam() == ChessBoardState.Team.Black)
        //    {
        //        num = 9;
        //    }
        //    List<ChessBoardState> list = new List<ChessBoardState>();
        //    if (add_state != null)
        //    {
        //        list.Add(add_state);
        //        num--;
        //    }
        //    list.AddRange(board.GetTheSavedMoves(-1));
        //    list.Reverse();
        //    if (list.Count < 1)
        //    {
        //        return false;
        //    }
        //    int num2 = 0;
        //    ChessBoardState.Team team = ChessBoardState.Team.None;
        //    List<string> list2 = new List<string>();
        //    List<string> list3 = new List<string>();
        //    for (int j = 0; j < list.Count; j++)
        //    {
        //        ChessBoardState chessBoardState = list[j];
        //        ChessBoardState chessBoardState2 = new ChessBoardState();
        //        chessBoardState2.Init();
        //        ChessBoardState.CopyState(chessBoardState, chessBoardState2);
        //        ChessBoardState.CopyPieces(chessBoardState, chessBoardState2);
        //        chessBoardState2.GenerateMoves(ChessBoardState.Team.None, true, false, false);
        //        PieceMoves.Move last_move = chessBoardState2.last_move;
        //        ChessBoardState.Team team2 = chessBoardState2.GetTeam(last_move.piece_moved);
        //        Utility.IndexToPos((int)last_move.to_square, out int item, out int item2);
        //        string text = (PlayerInput.Instance.GetTypeGFX(chessBoardState2.pieces[(int)last_move.piece_moved].race, chessBoardState2.pieces[(int)last_move.piece_moved].type).localization ?? "").ToString() + " ";
        //        if (last_move.piece_targeted != 0 && chessBoardState.IsMoveThreateningSquare(chessBoardState.pieces[(int)last_move.piece_targeted].square, last_move))
        //        {
        //            text += "x";
        //        }
        //        if (last_move.move_type == 11)
        //        {
        //            if (chessBoardState2.last_move.to_square > chessBoardState2.last_move.from_square)
        //            {
        //                text = "0-0";
        //            }
        //            else
        //            {
        //                text = "0-0-0";
        //            }
        //        }
        //        if (last_move.move_type != 11)
        //        {
        //            text += Utility.positionToString(item, item2);
        //        }
        //        if (chessBoardState2.IsTeamInCheckMate((team2 == ChessBoardState.Team.White) ? ChessBoardState.Team.Black : ChessBoardState.Team.White))
        //        {
        //            text += "#";
        //        }
        //        else if (chessBoardState2.IsTeamInCheck((team2 == ChessBoardState.Team.White) ? ChessBoardState.Team.Black : ChessBoardState.Team.White))
        //        {
        //            text += "+";
        //        }
        //        if (team == team2)
        //        {
        //            if (team2 == ChessBoardState.Team.White)
        //            {
        //                list2.Add(text);
        //                list3.Add("");
        //            }
        //            else
        //            {
        //                list3.Add(text);
        //                list2.Add("");
        //            }
        //            num2++;
        //        }
        //        else if (team2 == ChessBoardState.Team.White)
        //        {
        //            list2.Add(text);
        //            if (list2.Count > list3.Count)
        //            {
        //                num2++;
        //            }
        //        }
        //        else
        //        {
        //            list3.Add(text);
        //            if (list3.Count > list2.Count)
        //            {
        //                num2++;
        //            }
        //        }
        //        team = team2;
        //    }
        //    if (list2.Count < list3.Count)
        //    {
        //        list2.Add("");
        //    }
        //    if (list3.Count < list2.Count)
        //    {
        //        list3.Add("");
        //    }
        //    int num3 = num2 - __instance.movesUIP1.Length;
        //    if (num3 < 0)
        //    {
        //        num3 = 0;
        //    }
        //    for (int k = 0; k < __instance.movesUIP1.Length; k++)
        //    {
        //        int num4 = k + num3;
        //        if (num4 < list2.Count)
        //        {
        //            __instance.movesUIP1[k].text = list2[num4];
        //        }
        //        if (num4 < list3.Count)
        //        {
        //            __instance.movesUIP2[k].text = list3[num4];
        //        }
        //        __instance.turnNumbers()[k].text = (num4 + 1).ToString();
        //    }
        //    return false;
        //}
            
        private static void UpdateMovesListAfterExtrapolationEnd(ChessBoard self)
        {
            TryRevertGameEnd(self);
            if (working != Phase.Extrapolating || self.phase != ChessBoard.Phase.PlayerMove) { return; }
            while (self.saved_moves().Count > 1 + originalMovesList.Count - backIndex)
            {
                self.saved_moves().RemoveAt(0);
            }
            Plugin.Instance.LogThisInfo($"(Ctrl) pressed, Restoring analysis state before extrapolation.");
            //Plugin.Instance.LogThisInfo("State Removal working fine");
            ChessBoardState lastMove = self.GetLastMove();
            MoveAnimations.TweenStateToState(self.state, lastMove, ChessBoard.Phase.UndoMove, null);
            ChessBoardState.CopyState(lastMove, self.state);
            ChessBoardState.CopyMoves(lastMove, self.state);
            ChessBoardState.CopyPieces(lastMove, self.state);
            //Plugin.Instance.LogThisInfo("State Copying Working fine");
            UnityEngine.Object.FindObjectOfType<MoveHistoryUI>().UpdateMoves(self, null);
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
            }
        }

        private static void EnsureInitialization()
        {
            if (working != Phase.None) { return; }
            working = Phase.Working;
            originalMovesList = chessBoard.GetTheSavedMoves(-1);
            backIndex = 0;
            Plugin.Instance.LogThisInfo("Analysis started . . .");
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
            Plugin.Instance.LogThisInfo("(S) pressed, current move-list saved ! ! !");
            working = Phase.None;
            EnsureInitialization();
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
            if (race == Race.Cthulhu) {
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
                if (__instance.selectedPiece  > 0)
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
            if (__instance.GetTeam(target) == team && __instance.pieces[target].IsPawn() && __instance.pieces[capturer].IsPawn())
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
        public static void PreFix_Orthogonal([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(2)] byte pc, [HarmonyArgument(3)] ref int distance)
        {
            ChessBoardState.Piece p = state.pieces[pc];
            if (p.IsQueen() || p.IsRook())
            {
                distance = 7;
            }
        }

        [HarmonyPatch(typeof(PieceMoves), "AngleSlideMoves")]
        [HarmonyPrefix]
        public static void PreFix_Diagonal([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(2)] byte pc, [HarmonyArgument(3)] ref int distance)
        {
            ChessBoardState.Piece p = state.pieces[pc];
            if (p.IsQueen() || p.type == ChessBoardState.Piece.Type_Bishop)
            {
                distance = 7;
            }
        }
    }

    public static class AllFactionsHaveFullBoardMermaidSwaps
    {
        //public static bool initialized = false;
        //private static patternClass whiteRookSwapsPC = new patternClass()
        //{
        //    allBoardMove = true,
        //    mustTarget = true,
        //    targetsAllies = true,
        //    doesNotTargetEnemies = true,
        //    switchPosition = true,
        //    enabled = true,
        //    moveDist = 4,
        //    targetWhitelist = new string[] { "King" },
        //    targetBlacklist = new string[] { },
        //    target = new BoardSquare[] { },
        //    movement = new BoardSquare[] { },
        //    required = new BoardSquare[] { }
        //};
        //private static patternClass blackRookSwapsPC = new patternClass()
        //{
        //    allBoardMove = true,
        //    mustTarget = true,
        //    targetsAllies = true,
        //    doesNotTargetEnemies = true,
        //    switchPosition = true,
        //    enabled = true,
        //    moveDist = 4,
        //    targetWhitelist = new string[] { "King" },
        //    targetBlacklist = new string[] { },
        //    target = new BoardSquare[] { },
        //    movement = new BoardSquare[] { },
        //    required = new BoardSquare[] { }
        //};
        //private static patternClass whiteBishopSwapsPC = new patternClass()
        //{
        //    allBoardMove = true,
        //    mustTarget = true,
        //    targetsAllies = true,
        //    doesNotTargetEnemies = true,
        //    switchPosition = true,
        //    enabled = true,
        //    moveDist = 4,
        //    targetWhitelist = new string[] { "Pawn" },
        //    targetBlacklist = new string[] { },
        //    target = new BoardSquare[] { },
        //    movement = new BoardSquare[] { },
        //    required = new BoardSquare[] { }
        //};
        //private static patternClass blackBishopSwapsPC = new patternClass()
        //{
        //    allBoardMove = true,
        //    mustTarget = true,
        //    targetsAllies = true,
        //    doesNotTargetEnemies = true,
        //    switchPosition = true,
        //    enabled = true,
        //    moveDist = 4,
        //    targetWhitelist = new string[] { "Pawn" },
        //    targetBlacklist = new string[] { },
        //    target = new BoardSquare[] { },
        //    movement = new BoardSquare[] { },
        //    required = new BoardSquare[] { }
        //};
        //private static patternClass whiteKnightSwapsPC = new patternClass()
        //{
        //    allBoardMove = true,
        //    mustTarget = true,
        //    targetsAllies = true,
        //    doesNotTargetEnemies = true,
        //    switchPosition = true,
        //    enabled = true,
        //    moveDist = 4,
        //    targetWhitelist = new string[] { "Queen" },
        //    targetBlacklist = new string[] { },
        //    target = new BoardSquare[] { },
        //    movement = new BoardSquare[] { },
        //    required = new BoardSquare[] { }
        //};
        //private static patternClass blackKnightSwapsPC = new patternClass()
        //{
        //    allBoardMove = true,
        //    mustTarget = true,
        //    targetsAllies = true,
        //    doesNotTargetEnemies = true,
        //    switchPosition = true,
        //    enabled = true,
        //    moveDist = 4,
        //    targetWhitelist = new string[] { "Queen" },
        //    targetBlacklist = new string[] { },
        //    target = new BoardSquare[] { },
        //    movement = new BoardSquare[] { },
        //    required = new BoardSquare[] { }
        //};

        //private static void UpdateTargetWhitelist(ref patternClass pc, Piece p, PieceTypeEnum[] pieceTypes)
        //{
        //    if (pieceTypes.Length == 0) { return; }
        //    List<string> pieceNames = new List<string>(1);
        //    for (int i = 0; i < pieceTypes.Length; i++)
        //    {
        //        Piece[] pieces = p.board.GetPieces(pieceTypes[i], p.team.color);
        //        if (pieces.Length != 0)
        //        {
        //            pieceNames.Add(pieces[0].pieceName);
        //        }
        //    }
        //    pc.targetWhitelist = pieceNames.ToArray();
        //}

        //private static void UpdatePatternForSwaps(Piece p)
        //{
        //    PieceTypeEnum pieceType = p.PieceType;
        //    patternClass pc = null;
        //    PieceTypeEnum[] pieceTypes = null;
        //    switch (pieceType)
        //    {
        //        case PieceTypeEnum.Knight:
        //        case PieceTypeEnum.Knight_alt:
        //        case PieceTypeEnum.Griffin:
        //            pc = (p.team.color == TeamColor.White) ? whiteKnightSwapsPC : blackKnightSwapsPC;
        //            pieceTypes = new PieceTypeEnum[] {PieceTypeEnum.Queen, PieceTypeEnum.Baron,
        //                    PieceTypeEnum.Duke, PieceTypeEnum.Chimp, PieceTypeEnum.Mole};
        //            break;
        //        case PieceTypeEnum.Bishop:
        //            pc = (p.team.color == TeamColor.White) ? whiteBishopSwapsPC : blackBishopSwapsPC;
        //            pieceTypes = new PieceTypeEnum[] { PieceTypeEnum.Pawn, PieceTypeEnum.SkeletonSummon };
        //            break;
        //        case PieceTypeEnum.Rook:
        //        case PieceTypeEnum.Joy:
        //        case PieceTypeEnum.Rage:
        //            pc = (p.team.color == TeamColor.White) ? whiteRookSwapsPC : blackRookSwapsPC;
        //            pieceTypes = new PieceTypeEnum[] { PieceTypeEnum.King, PieceTypeEnum.Madness };
        //            break;
        //        default:
        //            break;
        //    }
        //    if (pc != null && pieceTypes != null)
        //    {
        //        UpdateTargetWhitelist(ref pc, p, pieceTypes);
        //        if (p.patterns[p.patterns.Length - 1] != pc)
        //        {
        //            System.Array.Resize<patternClass>(ref p.patterns, p.patterns.Length + 1);
        //            p.patterns[p.patterns.Length - 1] = pc;
        //        }
        //        initialized = true;
        //    }
        //}

        //private static void Init_PatchPatterns(Board __instance)
        //{

        //    foreach (Team team in __instance.Teams)
        //    {
        //        foreach (Piece p in team.pieces.Values)
        //        {
        //            if (p == null) { continue; }
        //            UpdatePatternForSwaps(p);
        //        }
        //    }
        //}

        //[HarmonyPrepare]
        //public static void Init()
        //{
        //    if (PlayerInput.Instance == null || PlayerInput.Instance.board_instance == null) { return; }
        //    Init_PatchPatterns(PlayerInput.Instance.board_instance);
        //}

        //[HarmonyPatch(typeof(Board), nameof(Board.init))]
        //[HarmonyPostfix]
        //public static void Postfix(Board __instance)
        //{
        //    Init_PatchPatterns(__instance);
        //    // for (int i = 0; i < 8; i++)
        //    // {
        //    //     for (int j = 0; j < 8; j++)
        //    //     {
        //    //         __instance.SpawnWaterTile(new BoardSquare(i, j), __instance.activeTeam);
        //    //     }
        //    // }
        //}

        //[HarmonyPatch(typeof(Piece), nameof(Piece.MoveReplacedBySkill))]
        //[HarmonyPrefix]
        //public static bool Prefix_MovePatch(Piece __instance, [HarmonyArgument(0)] patternClass pattern, [HarmonyArgument(1)] bool hasTarget, [HarmonyArgument(2)] ref bool captured, ref PieceAction.PieceSkill __result)
        //{
        //    if (pattern.switchPosition)
        //    {
        //        captured = true;
        //        __result = PieceAction.PieceSkill.SwitchPosition;
        //        return false;
        //    }
        //    return true;
        //}

        //[HarmonyPatch(typeof(Piece), nameof(Piece.init))]
        //[HarmonyPostfix]
        //public static void Postfix_PieceInit(Piece __instance)
        //{

        //    switch (__instance.PieceType)
        //    {
        //        case PieceTypeEnum.Queen:
        //        case PieceTypeEnum.Chimp:
        //        case PieceTypeEnum.Mole:
        //        case PieceTypeEnum.Duke:
        //        case PieceTypeEnum.Baron:
        //            foreach (Piece item in __instance.team.pieces.Values)
        //            {
        //                switch (item.PieceType)
        //                {
        //                    case PieceTypeEnum.Knight:
        //                    case PieceTypeEnum.Knight_alt:
        //                    case PieceTypeEnum.Griffin:
        //                        UpdatePatternForSwaps(item);
        //                        break;
        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        case PieceTypeEnum.Pawn:
        //        case PieceTypeEnum.SkeletonSummon:
        //            foreach (Piece item in __instance.team.pieces.Values)
        //            {
        //                switch (item.PieceType)
        //                {
        //                    case PieceTypeEnum.Bishop:
        //                        UpdatePatternForSwaps(item);
        //                        break;
        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        default:
        //            UpdatePatternForSwaps(__instance);
        //            break;
        //    }
        //}

        //[HarmonyCleanup]
        //public static void PatchCleanup()
        //{
        //    initialized = false;
        //}
        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.RookMoves))]
        [HarmonyPostfix]
        public static void Postfix_Rook([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(1)] byte pc, List<PieceMoves.Move> __result)
        {
            byte b = (state.GetTeam(pc) == ChessBoardState.Team.White) ? state.white_king : state.black_king;
            __result.Add(new PieceMoves.Move(pc, b, 5, state.pieces[b].square, state.pieces[pc].square, 0));
        }

        [HarmonyPatch(typeof(PieceMoves), nameof(PieceMoves.BishopMoves))]
        [HarmonyPostfix]
        public static void Postfix_Bishop([HarmonyArgument(0)] ChessBoardState state, [HarmonyArgument(1)] byte pc, [HarmonyArgument(3)] bool IsAIMove, List<PieceMoves.Move> __result)
        {
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
            if (add > 0)
            {
                add += team == ChessBoardState.Team.White ? whiteManaBonus : blackManaBonus;
            }
        }

        [HarmonyPatch(typeof(ChessBoard), "TryMakeMoveOnSquare")]
        [HarmonyPostfix]
        public static void PostFix(ChessBoard __instance, ref bool __result)
        {
            if (__result)
            {
                byte t = __instance.next_move.move_type;
                
                if (t == 20 || t == 22 || t == 23 || t == 24 || t == 25) { 
                    //__instance.state.pieces[__instance.next_move.piece_moved].type = t;
                    if (__instance.state.GetCurrentTeam() == ChessBoardState.Team.White)
                    {
                        whiteManaBonus++;
                    }
                    else
                    {
                        blackManaBonus++;
                    }
                    Plugin.Instance.LogThisInfo(" . . . Mana Income Increased . . . ");
                }
            }
        }

        [HarmonyPatch(typeof(ChessBoard), nameof(ChessBoard.Init))]
        [HarmonyPostfix]
        public static void Init(ChessBoard __instance)
        {
            whiteManaBonus = 0;
            blackManaBonus = 0;
        }
    }

    public static class WaterTilesBoostAlliesDebuffEnemies
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChessBoardState), nameof(ChessBoardState.DoEndOfTurnAdjustments))]
        public static void Postfix(ChessBoardState __instance)
        {
            foreach (byte square in __instance.GetWaterTiles())
            {
                if (Plugin.Instance.teamFavor == ChessBoardState.Team.None) { return; }
                byte p = __instance.GetPieceOnSquare(square);
                if (__instance.GetTeam(p) == Plugin.Instance.teamFavor)
                {
                    if (__instance.pieces[p].IsPawn() || __instance.pieces[p].IsKnight() ||
                        __instance.pieces[p].type == ChessBoardState.Piece.Type_Bishop)
                    {
                        __instance.pieces[p].AddFlag(ChessBoardState.Piece.Flag_Invincible);
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
    }

}
