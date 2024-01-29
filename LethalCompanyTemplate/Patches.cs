﻿using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection.Emit;
using Unity.Netcode;
using System.Reflection;

namespace Poltergeist
{
    [HarmonyPatch]
    public static class Patches
    {
        public static GrabbableObject ignoreObj = null;

        /////////////////////////////// Needed to suppress certain base-game systems ///////////////////////////////
        /**
         * Prevents certain manipulations of the spectate camera that would interfere with the controls
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), "RaycastSpectateCameraAroundPivot")]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetSpectateCameraToGameOverMode))]
        public static bool PreventSpectateFollow()
        {
            return false;
        }

        /**
         * Before doing late update stuff, make sure that the spectate camera is set to be overridden
         * 
         * __instance The calling player controller
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        public static void OverrideSpectateCam(PlayerControllerB __instance)
        {
            __instance.playersManager.overrideSpectateCamera = true;
        }

        /**
         * If this is the object we're ignoring, ignore it
         * 
         * __instance The calling grabbable
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), "ActivateItemClientRpc")]
        public static bool SuppressDuplicateHonk(GrabbableObject __instance) { 
            if(__instance == ignoreObj)
            {
                ignoreObj = null;
                return __instance.NetworkManager.IsServer || __instance.NetworkManager.IsHost;
            }
            return true;
        }




        /////////////////////////////// These are needed to manage the state of the camera controller ///////////////////////////////
        /**
         * When switching the camera, disable or enable the controller
         * 
         * @param __instance The calling start of round object
         * @param newCamera The new camera
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchCamera))]
        public static void ManageCameraController(StartOfRound __instance, Camera newCamera)
        {
            if (newCamera == __instance.spectateCamera)
                SpectatorCamController.instance.EnableCam();
            else
                SpectatorCamController.instance.DisableCam();
        }

        /**
         * When the start of round object wakes up, make the camera controller
         * 
         * @param __instance The calling start of round component
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        public static void MakeCamController(StartOfRound __instance)
        {
            __instance.spectateCamera.gameObject.AddComponent<SpectatorCamController>();
        }


        /////////////////////////////// These set up interactions that the ghost camera can have ///////////////////////////////
        /**
         * Adds a ghost interactor to valid interactibles
         * 
         * @param __instance The calling interact trigger
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InteractTrigger), "Start")]
        public static void AddGhostInteractor(InteractTrigger __instance)
        {
            //If it's a door, add the interactible
            if(__instance.gameObject.GetComponent<DoorLock>() != null)
                __instance.gameObject.AddComponent<GhostInteractible>();

            //If it's the lightswitch, add one there too
            if(__instance.name.Equals("LightSwitch"))
                __instance.gameObject.AddComponent<GhostInteractible>();
        }

        /**
         * Add ghost interactor objects to airhorns and clownhorns
         * 
         * @param __instance The calling noise prop
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NoisemakerProp), "Start")]
        public static void AddInteractorForHorns(NoisemakerProp __instance)
        {
            if(__instance.name.Contains("Airhorn") || __instance.name.Contains("Clownhorn"))
            {
                __instance.gameObject.AddComponent<GhostInteractible>();
            }
        }

        /**
         * Add ghost interactor objects to boomboxes
         * 
         * @param __instance The calling boombox
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoomboxItem), "Start")]
        public static void AddInteractorForBoombox(BoomboxItem __instance)
        {
            __instance.gameObject.AddComponent<GhostInteractible>();
        }


        /////////////////////////////// Transpile grabbed object behaviour to facilitate ground use ///////////////////////////////
        /**
         * Make items usable by any client when not held
         * 
         * @param __instance The calling input action
         * @param __result The resulting value
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.UseItemOnClient))]
        public static IEnumerable<CodeInstruction> AllowGroundUse(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            //First, load the list of instructions
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            //Next, find where we need to insert
            int insertIndex = -1;
            object successLabel = null;
            for (int i = 0; i < code.Count - 1; i++)
            {
                //Count it as a good spot when we recognize the string being loaded
                if (code[i].opcode == OpCodes.Ldstr && ((string)code[i].operand).Equals("Can't use item; not owner"))
                {
                    Poltergeist.DebugLog("Found expected structure for transpiler of UseItemOnClient");

                    //Save the index to insert into
                    insertIndex = i;

                    //Save the label to jump to
                    successLabel = code[i - 1].operand;

                    break;
                }
            }

            //Construct the code to insert (check if the object is held)
            List<CodeInstruction> insertion = new List<CodeInstruction>();
            insertion.Add(new CodeInstruction(OpCodes.Ldarg_0));
            insertion.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.isHeld))));
            insertion.Add(new CodeInstruction(OpCodes.Brfalse_S, successLabel));

            //Insert the code
            if (insertIndex != -1)
            {
                code.InsertRange(insertIndex, insertion);
            }

            return code;
        }

        /**
         * Make items on the ground not forbid hearing with ownership
         * 
         * @param __instance The calling input action
         * @param __result The resulting value
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GrabbableObject), "ActivateItemClientRpc")]
        public static IEnumerable<CodeInstruction> AllowGroundHearing(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            //First, load the list of instructions
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            //Next, find where we need to insert
            int insertIndex = -1;
            object successLabel = null;
            for (int i = 0; i < code.Count - 1; i++)
            {
                //Count it as a good spot when we recognize the string being loaded
                if (code[i].opcode == OpCodes.Call && ((MethodInfo)code[i].operand).Name.Equals("get_IsOwner"))
                {
                    Poltergeist.DebugLog("Found expected structure for transpiler of ActivateItemClientRpc");

                    //Save the index to insert into
                    insertIndex = i - 1;

                    //Save the label to jump to
                    successLabel = code[i + 1].operand;

                    break;
                }
            }

            //Construct the code to insert (check if the object is held)
            List<CodeInstruction> insertion = new List<CodeInstruction>();
            insertion.Add(new CodeInstruction(OpCodes.Ldarg_0));
            insertion.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.isHeld))));
            insertion.Add(new CodeInstruction(OpCodes.Brfalse_S, successLabel));

            //Insert the code
            if (insertIndex != -1)
            {
                code.InsertRange(insertIndex, insertion);
            }

            return code;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), "UseItemOnClient")]
        public static void PrintClientUse()
        {
            Poltergeist.DebugLog("Used item on client");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), "ActivateItemServerRpc")]
        public static void PrintServerRpc()
        {
            Poltergeist.DebugLog("Server Rpc called for object");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), "ActivateItemClientRpc")]
        public static void PrintClientRpc()
        {
            Poltergeist.DebugLog("Client Rpc called for object");
        }
    }
}
