using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace Poltergeist
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils")]
    public class Poltergeist : BaseUnityPlugin
    {
        //Plugin info
        public const string MOD_GUID = "coderCleric.Poltergeist";
        public const string MOD_NAME = "Poltergeist";
        public const string MOD_VERSION = "0.3.1";

        //Static things
        private static Poltergeist instance = null;
        public static PoltergeistConfig config = null;

        private void Awake()
        {
            instance = this;

            //Make the patches
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            //Make the input instance
            new PoltergeistCustomInputs();

            //Make the config
            config = new PoltergeistConfig(Config);

            // All done!
            Logger.LogInfo($"Plugin Poltergeist is loaded!");
        }

        /**
         * Simple debug logging
         */
        public static void DebugLog(string msg)
        {
            instance.Logger.LogInfo(msg);
        }
    }
}