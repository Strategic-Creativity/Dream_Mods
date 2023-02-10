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
        private Harmony harmony;

        private HashSet<ModType> patchesHashSet;
        public bool multiplayerActive {get; private set;} = false;
        public bool adventureModeActive{get; private set;} = false;

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
            AllFactionsHaveUnlimitedRange
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
            this.harmony = new Harmony("QualityOfLifeMadChessMods");
            Plugin.Instance = this;
            this.patchesHashSet = new HashSet<ModType>();
            Logger.LogInfo("Press 'TAB' inside game for mods info and hotkeys.\n Note : Key input only works in-game.\n Note : Press 'Shift + (hotkey)' to deativate mods.");
            this.PatchFixes();
            this.PatchOpMods();
            this.ActivateAllBalancedMods();
            SceneManager.sceneLoaded += this.onModsDisabledForMultiplayer;
            SceneManager.sceneLoaded += this.onModsEnabledForSingleplayer;
            SceneManager.sceneLoaded += this.onModsDisabledForAdventureMode;
            SceneManager.sceneLoaded += this.onModsEnabledForNonAdventure;
        }

        private void Update()
        {

            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.Tab))
            {
                LogOpModInfo();
                LogBalancedModInfo();
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F1))
            {
                this.TryPatch(ModType.WolfMultiJump);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
            {
                this.TryPatch(ModType.AllFactionsCanDoQueenPromotion);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
            {
                this.TryPatch(ModType.AllFactionsCanCaptureOwnPawns);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
            {
                this.TryPatch(ModType.AllFactionsHaveUnlimitedRange);
            }
            if (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift))
            {
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F1))
                {
                    this.TryUnpatch(ModType.WolfMultiJump);
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
                {
                    this.TryUnpatch(ModType.AllFactionsCanDoQueenPromotion);
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
                {
                    this.TryUnpatch(ModType.AllFactionsCanCaptureOwnPawns);
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
                {
                    this.TryUnpatch(ModType.AllFactionsHaveUnlimitedRange);
                }
            }
        }

        public void LogThisInfo(string message)
        {
            Logger.LogInfo(message);
        }

        private void TryPatch(ModType mod)
        {
            if (multiplayerActive)
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
            if (multiplayerActive)
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

        private void onModsDisabledForMultiplayer(Scene scene, LoadSceneMode sceneMode)
        {
            if (SceneManager.GetActiveScene().name == "10(Multiplayer)" && !this.multiplayerActive)
            {
                Logger.LogWarning("!!! Mods have been disbled for multiplayer. !!!");
                this.harmony.UnpatchSelf();
                this.multiplayerActive = true;
            }
        }

        private void onModsEnabledForSingleplayer(Scene scene, LoadSceneMode sceneMode)
        {
            //Logger.LogInfo("Scene loaded : " + SceneManager.GetActiveScene().name);
            if (SceneManager.GetActiveScene().name == "1(main)" && this.multiplayerActive)
            {
                Logger.LogWarning("!!! Mods have been re-enabled for singleplayer. !!!");
                this.PatchFixes();
                this.PatchOpMods();
                foreach (ModType item in this.patchesHashSet)
                {
                    this.PatchThisBalancedMod(item);
                }
                this.multiplayerActive = false;
            }
        }

        private void onModsDisabledForAdventureMode(Scene scene, LoadSceneMode sceneMode)
        {
            Logger.LogInfo("Scene loaded : " + SceneManager.GetActiveScene().name);
            if (SceneManager.GetActiveScene().name == "16(Story)" && !this.adventureModeActive)
            {
                Logger.LogWarning("!!! OP Mods have been disbled for adventure mode. !!!");
                this.UnpatchOpMods();
                this.adventureModeActive = true;
            }
        }

        private void onModsEnabledForNonAdventure(Scene scene, LoadSceneMode sceneMode)
        {
            if (SceneManager.GetSceneByName("1(main)").isLoaded && this.adventureModeActive)
            {
                Logger.LogWarning("!!! OP Mods have been re-enabled for versus mode. !!!");
                this.PatchOpMods();
                this.adventureModeActive = false;
            }
        }

        private void PatchFixes()
        {
            this.harmony.PatchAll(typeof(ShieldBugFix));
        }

        private void PatchOpMods()
        {

            this.harmony.PatchAll(typeof(BoardEditorMod));
            this.harmony.PatchAll(typeof(PieceRemoverMod));
            this.harmony.PatchAll(typeof(WolfSwapJump));

        }

        private void UnpatchOpMods()
        {

            this.harmony.Unpatch(typeof(Board).GetMethod("OnMouseDown"), HarmonyPatchType.All);
            this.harmony.Unpatch(typeof(Board).GetMethod("advance"), HarmonyPatchType.All);
            this.harmony.Unpatch(typeof(PieceAction).GetMethod("execute"), HarmonyPatchType.Postfix);

        }

        private void ActivateAllBalancedMods()
        {
            foreach (string mod in System.Enum.GetNames(typeof(ModType)))
            {
                ModType modType = (ModType)System.Enum.Parse(typeof(ModType), mod);
                this.PatchThisBalancedMod(modType);
                patchesHashSet.Add(modType);

            }
        }

        private void PatchThisBalancedMod(ModType modType)
        {
            switch (modType)
            {
                case ModType.WolfMultiJump:
                    this.harmony.PatchAll(typeof(WolfMultiJump));
                    break;
                case ModType.AllFactionsCanDoQueenPromotion:
                    this.harmony.PatchAll(typeof(AllFactionsCanDoQueenPromotion));
                    break;
                case ModType.AllFactionsCanCaptureOwnPawns:
                    this.harmony.PatchAll(typeof(AllFactionsCanCaptureOwnPawns));
                    this.harmony.PatchAll(typeof(AllFactionsCanCaptureOwnPawns_MovePatch));
                    break;
                case ModType.AllFactionsHaveUnlimitedRange:
                    this.harmony.PatchAll(typeof(AllFactionsHaveUnlimitedRange));
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
                    this.harmony.Unpatch(typeof(Piece).GetMethod("HandleCapturerPieceLogic"), HarmonyPatchType.All);
                    break;
                case ModType.AllFactionsCanDoQueenPromotion:
                    this.harmony.Unpatch(typeof(UpgradeSelect).GetMethod("Open"), HarmonyPatchType.All);
                    break;
                case ModType.AllFactionsCanCaptureOwnPawns:
                    this.harmony.Unpatch(typeof(Piece).GetMethod("validateTarget"), HarmonyPatchType.All);
                    this.harmony.Unpatch(typeof(PieceAction).GetMethod("execute"), HarmonyPatchType.Prefix);
                    break;
                case ModType.AllFactionsHaveUnlimitedRange:
                    this.harmony.Unpatch(typeof(Piece).GetMethod("findValidMoveList"), HarmonyPatchType.All);
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
            if (this.adventureModeActive)
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
                    return "This mod is currently work in progress";
            }
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

                    break;
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
                }
                if (list.Count < 1)
                {
                    break;
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
                if (PlayerInput.Instance !=  null && PlayerInput.Instance.upgradeselect_hook.is_enabled)
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

    [HarmonyPatch(typeof(Plugin), "Update")]
    public static class PieceRemoverMod
    {
        //
        [HarmonyPrefix]
        public static void PostFix(Plugin __instance)
        {
            if (UnityInput.Current.GetKeyDown(KeyCode.Delete) && PlayerInput.Instance != null
            && PlayerInput.Instance.board_instance != null && PlayerInput.Instance.board_instance.selectedPiece != null)
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
            if ((UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift)) 
            && !Plugin.Instance.adventureModeActive && !Plugin.Instance.multiplayerActive)
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

    [HarmonyPatch(typeof(UpgradeSelect), "Open")]
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

    [HarmonyPatch(typeof(Piece), "validateTarget")]
    public static class AllFactionsCanCaptureOwnPawns
    {
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
    }

    [HarmonyPatch(typeof(PieceAction), "execute")]
    public static class AllFactionsCanCaptureOwnPawns_MovePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PieceAction __instance, [HarmonyArgument(0)] int index, [HarmonyArgument(1)] ModChessGame game, ref bool __result)
        {
            Piece piece = __instance.piece_hook;
            Piece boardPiece = piece.board.onSquare(__instance.newPosition);
            if (piece != null && boardPiece != null && piece.team == boardPiece.team && piece.PieceType != PieceTypeEnum.Pawn 
            && __instance.skill == PieceAction.PieceSkill.None && boardPiece.PieceType == PieceTypeEnum.Pawn)
            {
                __result = true;
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

    [HarmonyPatch(typeof(Board), "updateMoves")]
    public static class AllFactionsHaveUnlimitedRange
    {
        [HarmonyPrefix]
        public static bool Prefix(Board __instance)
        {
            foreach (Team team in __instance.Teams)
            {

                foreach (Piece p in team.pieces.Values)
                {
                    if (p == null) {continue;}
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
                                patternClass.moveDist += 3;
                            }
                            break;
                    }
                }
            }
            return true;
        }

        [HarmonyPostfix]
        public static void PostFix(Board __instance)
        {
            foreach (Team team in __instance.Teams)
            {

                foreach (Piece p in team.pieces.Values)
                {
                    if (p == null) {continue;}
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
                                patternClass.moveDist -= 3;
                            }
                            break;
                    }
                }
            }
        }
    }
}
