using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Poltergeist
{
    [DataContract]
    public class PoltergeistConfig : SyncedConfig<PoltergeistConfig>
    {
        [DataMember] public SyncedEntry<float> MaxPower { get; private set; }

        /**
         * Constructor made to website specs
         */
        public PoltergeistConfig(ConfigFile cfg) : base(Poltergeist.MOD_GUID)
        {
            ConfigManager.Register(this);

            //Client things
            Patches.defaultMode = cfg.Bind("General",
                "DefaultToVanilla",
                false,
                "If true, the vanilla spectate system will be used by default on death.").Value;
            SpectatorCamController.lightIntensity = cfg.Bind("General",
                "GhostLightIntensity",
                5f,
                "The intensity of the global light when dead.\n" +
                "WARNING: This game has a lot of fog, so excessively high values can decrease visibility.").Value;

            //Synced things
            MaxPower = cfg.BindSyncedEntry(
                "Synced",
                "MaxPower",
                100f,
                "The maximum power available to ghosts."
                );

            Poltergeist.DebugLog("Finished generating config");
        }
    }
}
