using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TeamCherry.Localization;

namespace SilkenSisters.Patches
{

    [HarmonyPatch(typeof(Language), "Get")]
    [HarmonyPatch(new[] { typeof(string), typeof(string) })]
    public static class Language_Get_Patch
    {
        private static void Prefix(ref string key, ref string sheetTitle)
        {
            if (key.Contains("SILKEN_SISTERS")) sheetTitle = $"Mods.{SilkenSisters.Id}";
        }
    }
    
}
