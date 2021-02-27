using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_Stay_Strapped
{
    public class StayStrappedMod : Mod
    {
        private static StayStrappedMod _instance;
        public static StayStrappedMod Instance => _instance;

        public StayStrappedMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("CM_Stay_Strapped");
            harmony.PatchAll();

            _instance = this;
        }
    }
}
