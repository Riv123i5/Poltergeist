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
        [DataMember] public SyncedEntry<float> maxPower { get; private set; }
        [DataMember] public SyncedEntry<float> recharge { get; private set; }
        [DataMember] public SyncedEntry<int> aliveForMax { get; private set; }

        /**
         * Constructor made according to spec on wiki
         */
        public PoltergeistConfig(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
        {
            ConfigManager.Register(this);
            
            //Handle the client-side stuff
            Patches.defaultMode = cfg.Bind<bool>("Client-Side",
                "DefaultToVanilla",
                false,
                "If true, the vanilla spectate system will be used by default on death.").Value;
            SpectatorCamController.lightIntensity = cfg.Bind<float>("Client-Side",
                "GhostLightIntensity",
                5,
                "The intensity of the global light when dead.\n" +
                "WARNING: This game has a lot of fog, so excessively high values can decrease visibility.").Value;

            //Handle the synced stuff
            maxPower = cfg.BindSyncedEntry(
                "Synced",
                "MaxPower",
                100f,
                "How much power is available to the ghosts at maximum."
                );
            recharge = cfg.BindSyncedEntry(
                "Synced",
                "PowerRecharge",
                5f,
                "How much power ghosts should recharge per second."
                );
            aliveForMax = cfg.BindSyncedEntry(
                "Synced",
                "AliveForMaxPower",
                1,
                "What is the maximum number of players that can be alive for the ghosts to have max power.\n(As soon as this number or fewer players are left alive, ghosts will be at max power.))"
                );

            //Bound all of the values
            maxPower.Value = MathF.Max(0, maxPower.Value);
            recharge.Value = MathF.Max(0, recharge.Value);
            aliveForMax.Value = Math.Max(0, aliveForMax.Value);
        }
    }
}
