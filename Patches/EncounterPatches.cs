using HarmonyLib;
using HutongGames.PlayMaker;
using Silksong.FsmUtil;
using UnityEngine.SceneManagement;

namespace SilkenSisters.Patches
{
    internal class EncounterPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void PatchLaceDeath(FsmState __instance)
        {

            // Enable Corpse Lace Hooking
            if (__instance.Fsm.GameObject.name == "Encounter Scene Control" && __instance.Name == "Init" && (SceneManager.GetActiveScene().name == "Coral_19" || SceneManager.GetActiveScene().name == "Dust_01"))
            {
                __instance.fsm.DisableAction("Check", 3);
            }
        }


    }
}
