using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

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
        //public bool MultiplayerActive { get; private set; } = false;
        public bool AdventureModeActive { get; private set; } = false;

        private enum OpModType
        {
            BoardEditorMod,
            PieceRemoverMod,
            WolfSwapJump
        }
        private enum ModType
        {
            WolfMultiJump,
            AllFactionsCanDoQueenPromotion,
            AllFactionsCanCaptureOwnPawns,
            AllFactionsHaveUnlimitedRange,
            AllFactionsHaveOldMermaidSwaps
        }

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
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! \n Note : This is only tested on demo version.(May not work on other versions)");
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
            Logger.LogInfo("Press 'TAB' inside game for mods info and hotkeys.\n Note : Key input only works in-game.\n Note : Press 'Shift + (hotkey)' to deativate mods.");
            this.PatchFixes();
            this.PatchOpMods();
            this.ActivateAllBalancedMods();
            //SceneManager.sceneLoaded += this.onModsDisabledForMultiplayer;
            //SceneManager.sceneLoaded += this.onModsEnabledForSingleplayer;
            SceneManager.sceneLoaded += this.onModsDisabledForAdventureMode;
            SceneManager.sceneLoaded += this.onModsEnabledForNonAdventure;
            SceneManager.sceneLoaded += this.onSceneLoaded;
            SceneManager.sceneUnloaded += this.onSceneUnloaded;
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
                    this.TryUnpatch(ModType.WolfMultiJump);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
                {
                    this.TryUnpatch(ModType.AllFactionsCanDoQueenPromotion);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
                {
                    this.TryUnpatch(ModType.AllFactionsCanCaptureOwnPawns);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
                {
                    this.TryUnpatch(ModType.AllFactionsHaveUnlimitedRange);
                    return;
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F5))
                {
                    this.TryUnpatch(ModType.AllFactionsHaveOldMermaidSwaps);
                    return;
                }
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F1))
            {
                this.TryPatch(ModType.WolfMultiJump);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
            {
                this.TryPatch(ModType.AllFactionsCanDoQueenPromotion);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
            {
                this.TryPatch(ModType.AllFactionsCanCaptureOwnPawns);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
            {
                this.TryPatch(ModType.AllFactionsHaveUnlimitedRange);
                return;
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F5))
            {
                this.TryPatch(ModType.AllFactionsHaveOldMermaidSwaps);
                return;
            }
        }

        public void LogThisInfo(string message)
        {
            Logger.LogInfo(message);
        }

        private void TryPatch(ModType mod)
        {
            // if (MultiplayerActive)
            // {
            //     Logger.LogWarning("You cannot enable/disable mods in multiplayer!");
            //     return;
            // }
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
            // if (MultiplayerActive)
            // {
            //     Logger.LogWarning("You cannot enable/disable mods in multiplayer!");
            //     return;
            // }
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

        // private void onModsDisabledForMultiplayer(Scene scene, LoadSceneMode sceneMode)
        // {
        //     if (SceneManager.GetActiveScene().name == "10(Multiplayer)" && !this.MultiplayerActive)
        //     {
        //         Logger.LogWarning("!!! Mods have been disbled for multiplayer. !!!");
        //         this.UnpatchFixes();
        //         this.DeactivateAllBalancedMods();
        //         this.UnpatchOpMods();
        //         this.MultiplayerActive = true;
        //     }
        // }

        // private void onModsEnabledForSingleplayer(Scene scene, LoadSceneMode sceneMode)
        // {
        //     //Logger.LogInfo("Scene loaded : " + SceneManager.GetActiveScene().name);
        //     if (SceneManager.GetSceneByName("1(main)").isLoaded && this.MultiplayerActive)
        //     {
        //         Logger.LogWarning("!!! Mods have been re-enabled for singleplayer. !!!");
        //         this.PatchFixes();
        //         this.PatchOpMods();
        //         foreach (ModType item in this.patchesHashSet)
        //         {
        //             this.PatchThisBalancedMod(item);
        //         }
        //         this.MultiplayerActive = false;
        //     }
        // }

        private void onModsDisabledForAdventureMode(Scene scene, LoadSceneMode sceneMode)
        {
            if (SceneManager.GetActiveScene().name == "16(Story)" && !this.AdventureModeActive)
            {
                Logger.LogWarning("!!! OP Mods have been disbled for adventure mode. !!!");
                this.UnpatchOpMods();
                this.AdventureModeActive = true;
            }
        }

        private void onModsEnabledForNonAdventure(Scene scene, LoadSceneMode sceneMode)
        {
            if (SceneManager.GetSceneByName("1(main)").isLoaded && this.AdventureModeActive)
            {
                Logger.LogWarning("!!! OP Mods have been re-enabled for versus mode. !!!");
                this.PatchOpMods();
                this.AdventureModeActive = false;
            }
        }

        private void onSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            //if(scene.name == "7(Game)" || scene.name == "17(StoryGame)") { AllFactionsHaveOldMermaidSwaps.initialized = false;}
        }

        private void onSceneUnloaded(Scene scene)
        {
            //if(scene.name == "7(Game)" || scene.name == "17(StoryGame)") { AllFactionsHaveOldMermaidSwaps.initialized = false;}
        }

        private void PatchFixes()
        {
            this.fixesPatcher.PatchAll(typeof(ShieldBugFix));
        }

        private void UnpatchFixes()
        {
            this.fixesPatcher.UnpatchSelf();
        }

        private void PatchOpMods()
        {

            this.opPatchers[(int)OpModType.BoardEditorMod].PatchAll(typeof(BoardEditorMod));
            this.opPatchers[(int)OpModType.PieceRemoverMod].PatchAll(typeof(PieceRemoverMod));
            this.opPatchers[(int)OpModType.WolfSwapJump].PatchAll(typeof(WolfSwapJump));
            
        }

        private void UnpatchOpMods()
        {

            this.opPatchers[(int)OpModType.BoardEditorMod].UnpatchSelf();
            this.opPatchers[(int)OpModType.PieceRemoverMod].UnpatchSelf();
            this.opPatchers[(int)OpModType.WolfSwapJump].UnpatchSelf();

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
                case ModType.WolfMultiJump:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(WolfMultiJump));
                    break;
                case ModType.AllFactionsCanDoQueenPromotion:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsCanDoQueenPromotion));
                    break;
                case ModType.AllFactionsCanCaptureOwnPawns:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsCanCaptureOwnPawns));
                    break;
                case ModType.AllFactionsHaveUnlimitedRange:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsHaveUnlimitedRange));
                    break;
                case ModType.AllFactionsHaveOldMermaidSwaps:
                    this.balancedPatchers[(int)modType].PatchAll(typeof(AllFactionsHaveOldMermaidSwaps));
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
                case ModType.WolfMultiJump:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.AllFactionsCanDoQueenPromotion:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.AllFactionsCanCaptureOwnPawns:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.AllFactionsHaveUnlimitedRange:
                    this.balancedPatchers[(int)modType].UnpatchSelf();
                    break;
                case ModType.AllFactionsHaveOldMermaidSwaps:
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
                case OpModType.BoardEditorMod:
                    return "Select any piece and then hold 'Shift' \n\t & click anywhere on board to clone it(for white color team), \n\t or hold 'Shift + Alt' to make clone for black color team. \n\t Note : It takes one turn to fully register pieces.";
                case OpModType.PieceRemoverMod:
                    return "Select any piece and press 'Delete' key to remove it.";
                case OpModType.WolfSwapJump:
                    return "Hold 'Shift' key while using 'blood jump' ability of wolf \n\t to swap places with pawn instead of capturing it.";
                default:
                    return "This mod is currently work in progress !!!";
            }
        }

        private void LogPatternClassInfo()
        {
            Board b = PlayerInput.Instance.board_instance;

            FieldInfo[] fieldInfos = typeof(patternClass).GetFields();
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

    [HarmonyPatch(typeof(Piece), "TargetIsShielded")]
    public static class ShieldBugFix
    {
        //Main function only considers first shield to check if target is shielded
        [HarmonyPrefix]
        public static bool Prefix(Piece __instance, [HarmonyArgument(0)] Piece target, ref bool __result)
        {
            if (target.PieceType == PieceTypeEnum.King)
            {
                __result = false;
                return false;
            }
            if (target.team.TeamData.AlliesInvincible > 0)
            {
                __result = true;
                return false;
            }
            if (target.team.roster.Race != Race.Angels)
            {
                __result = false;
                return false;
            }
            Piece[] pieces = __instance.board.GetPieces(PieceTypeEnum.Queen, target.team.color);
            if (pieces == null || pieces.Length == 0)
            {
                __result = false;
                return false;
            }
            foreach (Piece piece in pieces)
            {
                if (piece.isCaptured || !piece.inPlay)
                {

                    continue;
                }
                List<Piece> list = new List<Piece>();
                if (piece.backShield)
                {
                    list.Add(__instance.game.GetPiece(__instance.board.PiecePositionList[piece.Position.x, (int)((sbyte)(piece.Position.y + ((__instance.team.rivals[0].color == TeamColor.White) ? -1 : 1)))]));
                }
                else if (piece.sideShield)
                {
                    list.Add(__instance.game.GetPiece(__instance.board.PiecePositionList[(int)((sbyte)(piece.Position.x - 1)), piece.Position.y]));
                    list.Add(__instance.game.GetPiece(__instance.board.PiecePositionList[(int)((sbyte)(piece.Position.x + 1)), piece.Position.y]));
                    list.Add(__instance.game.GetPiece(__instance.board.PiecePositionList[piece.Position.x, (int)((sbyte)(piece.Position.y - 1))]));
                    list.Add(__instance.game.GetPiece(__instance.board.PiecePositionList[piece.Position.x, (int)((sbyte)(piece.Position.y + 1))]));
                }
                if (list.Count < 1)
                {
                    continue;
                }
                using (List<Piece>.Enumerator enumerator = list.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == target)
                        {
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Board), "OnMouseDown")]
    public static class BoardEditorMod
    {
        //
        [HarmonyPrefix]
        public static bool Prefix(Board __instance)
        {

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (PlayerInput.Instance != null && PlayerInput.Instance.upgradeselect_hook.is_enabled)
                {
                    return false;
                }
                Vector3 vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                BoardSquare s = new BoardSquare(Mathf.CeilToInt(vector.x + (float)Board.BOARDSIZE / 2f - 1f), Mathf.CeilToInt(vector.z + (float)Board.BOARDSIZE / 2f - 1f));
                Plugin.Instance.LogThisInfo($". . . board clicked on {s.x}, {s.y} . . .");
                if (s.isValid && __instance.selectedPiece != null)
                {
                    Piece sp = __instance.selectedPiece;
                    Piece target = __instance.onSquare(s);
                    if (target != null || sp.PieceType == PieceTypeEnum.King)
                    {
                        return false;
                    }
                    Team team = null;
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.AltGr) || Input.GetKey(KeyCode.RightAlt))
                    {
                        team = __instance.GetTeamByColor(TeamColor.Black);
                    }
                    else
                    {
                        team = __instance.GetTeamByColor(TeamColor.White);
                    }
                    Piece np = team.newPiece(sp.gameObject, new BoardSquare[] { s });
                    np.DoTransition(sp.Position, s, false, 0.2f);
                    np.findValidMoveList();
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Piece), "OnMouseDown")]
    public static class BoardEditorMod_PiecePatch
    {
        //
        [HarmonyPrefix]
        public static bool Prefix(Piece __instance)
        {

            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && __instance.board.selectedPiece != null)
            {

                BoardSquare s = __instance.Position;
                Plugin.Instance.LogThisInfo($". . . board clicked on {s.x}, {s.y} . . .");
                if (s.isValid && __instance.board.selectedPiece != null)
                {
                    Piece sp = __instance.board.selectedPiece;
                    __instance.captured(0);
                    Team team = null;
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.AltGr) || Input.GetKey(KeyCode.RightAlt))
                    {
                        team = __instance.board.GetTeamByColor(TeamColor.Black);
                    }
                    else
                    {
                        team = __instance.board.GetTeamByColor(TeamColor.White);
                    }
                    Piece np = team.newPiece(sp.gameObject, new BoardSquare[] { s });
                    np.DoTransition(sp.Position, s, false, 0.2f);
                    np.findValidMoveList();
                    UnityEngine.Object.Destroy(__instance, 1.0f);
                }

                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Plugin), "UpdateHook")]
    public static class PieceRemoverMod
    {
        //
        [HarmonyPrefix]
        public static void PostFix(Plugin __instance)
        {
            if (UnityInput.Current.GetKeyDown(KeyCode.Delete) && PlayerInput.Instance.board_instance.selectedPiece != null)
            {
                Piece p = PlayerInput.Instance.board_instance.selectedPiece;
                p.captured(0);
            }

        }
    }

    [HarmonyPatch(typeof(PieceAction), "execute")]
    public static class WolfSwapJump
    {

        [HarmonyPostfix]
        public static void Postfix(PieceAction __instance)
        {
            if (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift))
            {
                Piece piece = __instance.piece_hook;
                Piece boardPiece = piece.board.onSquare(__instance.newPosition);
                if (Piece.CheckFriendlyCaptureDoubleTurn(piece, boardPiece))
                {
                    boardPiece = piece.team.newPiece(boardPiece.gameObject, new BoardSquare[] { __instance.oldPosition });
                    boardPiece.DoTransition(__instance.newPosition, __instance.oldPosition, false, 0.2f);
                }
            }
        }
    }

    
    [HarmonyPatch(typeof(Piece), "HandleCapturerPieceLogic")]
    public static class WolfMultiJump
    {
        [HarmonyPostfix]
        public static void Postfix(Piece __instance, [HarmonyArgument(0)] Piece capturerPiece)
        {
            capturerPiece.PieceData.FriendlyCapturedDoubleTurn = false;
        }
    }

    [HarmonyPatch(typeof(UpgradeSelect), nameof(UpgradeSelect.Open))]
    public static class AllFactionsCanDoQueenPromotion
    {
        [HarmonyPostfix]
        public static void Postfix(UpgradeSelect __instance, [HarmonyArgument(0)] Piece p)
        {
            Vector3 position = p.Position.WorldSpace(1f);
            Vector3 vector = Camera.main.WorldToScreenPoint(position);
            vector = __instance.transform.InverseTransformPoint(vector);
            Plugin.Instance.LogThisInfo("Queen Promotion code running ...");
            if (vector.y < 0f)
            {
                vector.y += 360f;
            }
            for (int i = 0; i < __instance.pieces.Count; i++)
            {
                __instance.pieces[i].transform.localPosition = new Vector3(vector.x, vector.y - 120f * (float)i, 1f);
            }

            __instance.pieces[0].gameObject.SetActive(true);
        }
    }


    public static class AllFactionsCanCaptureOwnPawns
    {
        [HarmonyPatch(typeof(Piece), nameof(Piece.validateTarget))]
        [HarmonyPrefix]
        public static bool Prefix(Piece __instance, [HarmonyArgument(0)] int pid, [HarmonyArgument(1)] Piece target, ref bool __result)
        {
            if (target != null && !target.Invincible && !__instance.TargetIsShielded(target) && !__instance.patterns[pid].cantTarget &&
            target.team == __instance.team && !__instance.PieceType.IsPawn() && target.PieceType.IsPawn())
            {

                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PieceAction), nameof(PieceAction.execute))]
        [HarmonyPrefix]
        public static bool Prefix_MovePatch(PieceAction __instance, [HarmonyArgument(0)] int index, [HarmonyArgument(1)] ModChessGame game, ref bool __result)
        {
            Piece piece = __instance.piece_hook;
            Piece boardPiece = piece.board.onSquare(__instance.newPosition);

            if (piece != null && boardPiece != null && piece.team == boardPiece.team && piece.PieceType != PieceTypeEnum.Pawn
            && __instance.skill == PieceAction.PieceSkill.None && boardPiece.PieceType == PieceTypeEnum.Pawn && piece.PieceType != PieceTypeEnum.Bishop)
            {
                __result = true;
                //Plugin.Instance.LogThisInfo("Pieces hide in pawn instead of caputring it!");
                boardPiece.captured(piece.GUPID);
                piece.Position = __instance.newPosition;
                piece.HasMoved = true;
                if (piece.pieceRace == Race.Tikis && (piece.PieceType == PieceTypeEnum.Knight || piece.PieceType == PieceTypeEnum.Knight_alt))
                {
                    piece.PieceType = ((piece.PieceType == PieceTypeEnum.Knight) ? PieceTypeEnum.Knight_alt : PieceTypeEnum.Knight);
                    piece.TikiMorph(piece.PieceType);
                }
                return false;
            }
            return true;
        }
    }


    public static class AllFactionsHaveUnlimitedRange
    {
        private static void EditPatternForUnlimitedRange(Board board, bool increase)
        {
            foreach (Team team in board.Teams)
            {

                foreach (Piece p in team.pieces.Values)
                {
                    if (p == null) { continue; }
                    PieceTypeEnum pieceType = p.PieceType;
                    switch (pieceType)
                    {
                        case PieceTypeEnum.Rook:
                        case PieceTypeEnum.Bishop:
                        case PieceTypeEnum.Queen:
                        case PieceTypeEnum.Chimp:
                        case PieceTypeEnum.Mole:
                        case PieceTypeEnum.Tower:
                        case PieceTypeEnum.Scorpion:
                        case PieceTypeEnum.Joy:
                            foreach (patternClass patternClass in p.patterns)
                            {
                                if (patternClass.movement == null || patternClass.movement.Length == 0) { continue; }
                                patternClass.moveDist += (increase) ? 6 : -6;
                            }
                            break;
                    }
                }
            }
        }
        [HarmonyPrepare]
        public static void Initialize()
        {
            if (PlayerInput.Instance && PlayerInput.Instance.board_instance)
            {
                EditPatternForUnlimitedRange(PlayerInput.Instance.board_instance, true);
            }
        }

        [HarmonyPatch(typeof(Board), nameof(Board.init))]
        [HarmonyPostfix]
        public static void PostFix(Board __instance)
        {
            EditPatternForUnlimitedRange(__instance, true);
        }

        [HarmonyCleanup]
        public static void Unpatch_Cleanup()
        {
            if (PlayerInput.Instance && PlayerInput.Instance.board_instance)
            {
                EditPatternForUnlimitedRange(PlayerInput.Instance.board_instance, false);
            }
        }
    }

    public static class AllFactionsHaveOldMermaidSwaps
    {
        public static bool initialized = false;
        private static patternClass whiteRookSwapsPC = new patternClass()
        {
            allBoardMove = true,
            mustTarget = true,
            targetsAllies = true,
            doesNotTargetEnemies = true,
            switchPosition = true,
            enabled = true,
            moveDist = 4,
            targetWhitelist = new string[] { "King" },
            targetBlacklist = new string[] { },
            target = new BoardSquare[] { },
            movement = new BoardSquare[] { },
            required = new BoardSquare[] { }
        };
        private static patternClass blackRookSwapsPC = new patternClass()
        {
            allBoardMove = true,
            mustTarget = true,
            targetsAllies = true,
            doesNotTargetEnemies = true,
            switchPosition = true,
            enabled = true,
            moveDist = 4,
            targetWhitelist = new string[] { "King" },
            targetBlacklist = new string[] { },
            target = new BoardSquare[] { },
            movement = new BoardSquare[] { },
            required = new BoardSquare[] { }
        };
        private static patternClass whiteBishopSwapsPC = new patternClass()
        {
            allBoardMove = true,
            mustTarget = true,
            targetsAllies = true,
            doesNotTargetEnemies = true,
            switchPosition = true,
            enabled = true,
            moveDist = 4,
            targetWhitelist = new string[] { "Pawn" },
            targetBlacklist = new string[] { },
            target = new BoardSquare[] { },
            movement = new BoardSquare[] { },
            required = new BoardSquare[] { }
        };
        private static patternClass blackBishopSwapsPC = new patternClass()
        {
            allBoardMove = true,
            mustTarget = true,
            targetsAllies = true,
            doesNotTargetEnemies = true,
            switchPosition = true,
            enabled = true,
            moveDist = 4,
            targetWhitelist = new string[] { "Pawn" },
            targetBlacklist = new string[] { },
            target = new BoardSquare[] { },
            movement = new BoardSquare[] { },
            required = new BoardSquare[] { }
        };
        private static patternClass whiteKnightSwapsPC = new patternClass()
        {
            allBoardMove = true,
            mustTarget = true,
            targetsAllies = true,
            doesNotTargetEnemies = true,
            switchPosition = true,
            enabled = true,
            moveDist = 4,
            targetWhitelist = new string[] { "Queen" },
            targetBlacklist = new string[] { },
            target = new BoardSquare[] { },
            movement = new BoardSquare[] { },
            required = new BoardSquare[] { }
        };
        private static patternClass blackKnightSwapsPC = new patternClass()
        {
            allBoardMove = true,
            mustTarget = true,
            targetsAllies = true,
            doesNotTargetEnemies = true,
            switchPosition = true,
            enabled = true,
            moveDist = 4,
            targetWhitelist = new string[] { "Queen" },
            targetBlacklist = new string[] { },
            target = new BoardSquare[] { },
            movement = new BoardSquare[] { },
            required = new BoardSquare[] { }
        };

        private static void UpdateTargetWhitelist(ref patternClass pc, Piece p, PieceTypeEnum[] pieceTypes)
        {
            if (pieceTypes.Length == 0) { return; }
            List<string> pieceNames = new List<string>(1);
            for (int i = 0; i < pieceTypes.Length; i++)
            {
                Piece[] pieces = p.board.GetPieces(pieceTypes[i], p.team.color);
                if (pieces.Length != 0)
                {
                    pieceNames.Add(pieces[0].pieceName);
                }
            }
            pc.targetWhitelist = pieceNames.ToArray();
        }

        private static void UpdatePatternForSwaps(Piece p)
        {
            PieceTypeEnum pieceType = p.PieceType;
            patternClass pc = null;
            PieceTypeEnum[] pieceTypes = null;
            switch (pieceType)
            {
                case PieceTypeEnum.Knight:
                case PieceTypeEnum.Knight_alt:
                case PieceTypeEnum.Griffin:
                    pc = (p.team.color == TeamColor.White) ? whiteKnightSwapsPC : blackKnightSwapsPC;
                    pieceTypes = new PieceTypeEnum[] {PieceTypeEnum.Queen, PieceTypeEnum.Baron,
                            PieceTypeEnum.Duke, PieceTypeEnum.Chimp, PieceTypeEnum.Mole};
                    break;
                case PieceTypeEnum.Bishop:
                    pc = (p.team.color == TeamColor.White) ? whiteBishopSwapsPC : blackBishopSwapsPC;
                    pieceTypes = new PieceTypeEnum[] { PieceTypeEnum.Pawn, PieceTypeEnum.SkeletonSummon };
                    break;
                case PieceTypeEnum.Rook:
                case PieceTypeEnum.Joy:
                case PieceTypeEnum.Rage:
                    pc = (p.team.color == TeamColor.White) ? whiteRookSwapsPC : blackRookSwapsPC;
                    pieceTypes = new PieceTypeEnum[] { PieceTypeEnum.King, PieceTypeEnum.Madness };
                    break;
                default:
                    break;
            }
            if (pc != null && pieceTypes != null)
            {
                UpdateTargetWhitelist(ref pc, p, pieceTypes);
                if (p.patterns[p.patterns.Length] != pc)
                {
                    System.Array.Resize<patternClass>(ref p.patterns, p.patterns.Length + 1);
                    p.patterns[p.patterns.Length - 1] = pc;
                }
                initialized = true;
            }
        }

        private static void Init_PatchPatterns(Board __instance)
        {

            foreach (Team team in __instance.Teams)
            {
                foreach (Piece p in team.pieces.Values)
                {
                    if (p == null) { continue; }
                    UpdatePatternForSwaps(p);
                }
            }
        }

        [HarmonyPrepare]
        public static void Init()
        {
            if (PlayerInput.Instance == null || PlayerInput.Instance.board_instance == null) { return; }
            Init_PatchPatterns(PlayerInput.Instance.board_instance);
        }

        [HarmonyPatch(typeof(Board), nameof(Board.init))]
        [HarmonyPostfix]
        public static void Postfix(Board __instance)
        {
            Init_PatchPatterns(__instance);
            // for (int i = 0; i < 8; i++)
            // {
            //     for (int j = 0; j < 8; j++)
            //     {
            //         __instance.SpawnWaterTile(new BoardSquare(i, j), __instance.activeTeam);
            //     }
            // }
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.MoveReplacedBySkill))]
        [HarmonyPrefix]
        public static bool Prefix_MovePatch(Piece __instance, [HarmonyArgument(0)] patternClass pattern, [HarmonyArgument(1)] bool hasTarget, [HarmonyArgument(2)] ref bool captured, ref PieceAction.PieceSkill __result)
        {
            if (pattern.switchPosition)
            {
                captured = true;
                __result = PieceAction.PieceSkill.SwitchPosition;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.init))]
        [HarmonyPostfix]
        public static void Postfix_PieceInit(Piece __instance)
        {
            
            switch (__instance.PieceType)
            {
                case PieceTypeEnum.Queen:
                case PieceTypeEnum.Chimp:
                case PieceTypeEnum.Mole:
                case PieceTypeEnum.Duke:
                case PieceTypeEnum.Baron:
                    foreach (Piece item in __instance.team.pieces.Values)
                    {
                        switch (item.PieceType)
                        {
                            case PieceTypeEnum.Knight:
                            case PieceTypeEnum.Knight_alt:
                            case PieceTypeEnum.Griffin:
                                UpdatePatternForSwaps(item);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case PieceTypeEnum.Pawn:
                case PieceTypeEnum.SkeletonSummon:
                    foreach (Piece item in __instance.team.pieces.Values)
                    {
                        switch (item.PieceType)
                        {
                            case PieceTypeEnum.Bishop:
                                UpdatePatternForSwaps(item);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    UpdatePatternForSwaps(__instance);
                    break;
            }
        }

        [HarmonyCleanup]
        public static void PatchCleanup()
        {
            initialized = false;
        }
    }
}
