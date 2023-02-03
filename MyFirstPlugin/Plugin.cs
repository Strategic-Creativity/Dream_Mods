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
        private bool multiplayerActive = false;

        private enum ModType
        {
            WolfSwapJump,
            WolfMultiJump,
            AllFactionsCanDoQueenPromotion,
            AllFactionsCanCaptureOwnPawns
        }

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! \n Note : This is only tested on demo version.(May not work on other versions)");
            this.harmony = new Harmony("QualityOfLifeMadChessMods");
            Plugin.Instance = this;
            this.patchesHashSet = new HashSet<ModType>();
            Logger.LogInfo("Press 'TAB' inside game for mods info and hotkeys.\n Note : Key input only works in-game.\n Note : Press 'Shift + (hotkey)' to deativate mods.");
            this.harmony.PatchAll(typeof(ShieldBugFix));
            this.harmony.PatchAll(typeof(BoardEditorMod));
            //this.harmony.PatchAll(typeof(BoardEditorMod_PiecePatch));
            SceneManager.sceneLoaded += this.onModsDisabledForMultiplayer;
            SceneManager.sceneUnloaded += this.onModsEnabledForSingleplayer;
        }

        private void Update()
        {

            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.Tab))
            {
                Logger.LogInfo("\n BoardEditorMod (Usage): Select any piece and then hold 'Shift' \n & click anywhere on board to clone it(for white color team), \n or hold 'Shift + Alt' to make clone for black color team \n Note : It takes one turn to fully register pieces.");
                LogModInfo();
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F1))
            {
                this.TryPatch(ModType.WolfSwapJump);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
            {
                this.TryPatch(ModType.WolfMultiJump);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
            {
                this.TryPatch(ModType.AllFactionsCanDoQueenPromotion);
            }
            if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
            {
                this.TryPatch(ModType.AllFactionsCanCaptureOwnPawns);
            }
            if (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift))
            {
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F1))
                {
                    this.TryUnpatch(ModType.WolfSwapJump);
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F2))
                {
                    this.TryUnpatch(ModType.WolfMultiJump);
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F3))
                {
                    this.TryUnpatch(ModType.AllFactionsCanDoQueenPromotion);
                }
                if (UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.F4))
                {
                    this.TryUnpatch(ModType.AllFactionsCanCaptureOwnPawns);
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
            switch (mod)
            {
                case ModType.WolfSwapJump:
                    this.harmony.PatchAll(typeof(WolfSwapJump));
                    break;
                case ModType.WolfMultiJump:
                    this.harmony.PatchAll(typeof(WolfMultiJump));
                    break;
                case ModType.AllFactionsCanDoQueenPromotion:
                    this.harmony.PatchAll(typeof(AllFactionsCanDoQueenPromotion));
                    break;
                case ModType.AllFactionsCanCaptureOwnPawns:
                    this.harmony.PatchAll(typeof(AllFactionsCanCaptureOwnPawns));
                    break;
                default:
                    Logger.LogError($"{mod.ToString()} mod is currently not supported");
                    return;
            }
            LogThisInfo($". . . {mod.ToString()} mod activated . . .");
            this.patchesHashSet.Add(mod);
            LogModInfo();
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
            switch (mod)
            {
                case ModType.WolfSwapJump:
                    this.harmony.Unpatch(typeof(PieceAction).GetMethod("execute"), HarmonyPatchType.All);
                    break;
                case ModType.WolfMultiJump:
                    this.harmony.Unpatch(typeof(Piece).GetMethod("HandleCapturerPieceLogic"), HarmonyPatchType.All);
                    break;
                case ModType.AllFactionsCanDoQueenPromotion:
                    this.harmony.Unpatch(typeof(UpgradeSelect).GetMethod("Open"), HarmonyPatchType.All);
                    break;
                case ModType.AllFactionsCanCaptureOwnPawns:
                    this.harmony.Unpatch(typeof(Piece).GetMethod("validateTarget"), HarmonyPatchType.All);
                    break;
                default:
                    Logger.LogError($"{mod.ToString()} mod is currently not supported");
                    return;
            }
            LogThisInfo($". . . {mod.ToString()} mod disabled . . .");
            this.patchesHashSet.Remove(mod);
            LogModInfo();
        }

        private void onModsDisabledForMultiplayer(Scene scene, LoadSceneMode sceneMode)
        {
            if (PlayerInput.Instance.IsSceneLoaded(10))
            {
                Logger.LogWarning("!!! Mods have been disbled for multiplayer. !!!");
                this.harmony.UnpatchSelf();
                this.multiplayerActive = true;
            }
        }

        private void onModsEnabledForSingleplayer(Scene scene)
        {
            if (!PlayerInput.Instance.IsSceneLoaded(10) && multiplayerActive)
            {
                Logger.LogWarning("!!! Mods have been re-enabled for singleplayer. !!!");
                this.harmony.PatchAll(typeof(ShieldBugFix));
                this.harmony.PatchAll(typeof(BoardEditorMod));
                foreach (ModType item in this.patchesHashSet)
                {
                    switch (item)
                    {
                        case ModType.WolfSwapJump:
                            this.harmony.Unpatch(typeof(PieceAction).GetMethod("execute"), HarmonyPatchType.All);
                            break;
                        case ModType.WolfMultiJump:
                            this.harmony.Unpatch(typeof(Piece).GetMethod("HandleCapturerPieceLogic"), HarmonyPatchType.All);
                            break;
                        case ModType.AllFactionsCanDoQueenPromotion:
                            this.harmony.Unpatch(typeof(UpgradeSelect).GetMethod("Open"), HarmonyPatchType.All);
                            break;
                        case ModType.AllFactionsCanCaptureOwnPawns:
                            this.harmony.Unpatch(typeof(Piece).GetMethod("validateTarget"), HarmonyPatchType.All);
                            break;
                        default:
                            Logger.LogError($"{item.ToString()} mod is currently not supported");
                            return;
                    }
                }
                this.multiplayerActive = false;
            }
        }

        private void LogModInfo()
        {
            //int expectedCapacity = System.Enum.GetValues(typeof(ModType)).Length * 4 + 3;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("List of mods {(name) then (hotkey) then (state)} :");
            int i = 1;
            foreach (string mod in System.Enum.GetNames(typeof(ModType)))
            {
                string state = patchesHashSet.Contains((ModType)System.Enum.Parse(typeof(ModType), mod)) ? "(active)" : "";
                sb.AppendLine($"{i}.\t{mod.ToString()} ({'F' + i.ToString()}) {state}");
                i++;
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
                if (PlayerInput.Instance.upgradeselect_hook.is_enabled)
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
                    if (target != null)
                    {
                        target.captured(0);
                        UnityEngine.Object.Destroy(target.gameObject);
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

    [HarmonyPatch(typeof(PieceAction), "execute")]
    public static class WolfSwapJump
    {
        [HarmonyPostfix]
        public static void Postfix(PieceAction __instance)
        {
            Piece piece = __instance.piece_hook;
            Piece boardPiece = piece.board.onSquare(__instance.newPosition);
            if (Piece.CheckFriendlyCaptureDoubleTurn(piece, boardPiece))
            {
                //Plugin.Instance.LogThisInfo("Wolf jump patch if statment called.");
                boardPiece = piece.team.newPiece(boardPiece.gameObject, new BoardSquare[] { __instance.oldPosition });
                boardPiece.DoTransition(__instance.newPosition, __instance.oldPosition, false, 0.2f);
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
}
