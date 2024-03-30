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
        //Client configs
        public ConfigEntry<bool> DefaultToVanilla { get; private set; }
        public ConfigEntry<float> LightIntensity { get; private set; }

        //Regular synced configs
        [DataMember] public SyncedEntry<float> MaxPower { get; private set; }
        [DataMember] public SyncedEntry<float> Recharge { get; private set; }
        [DataMember] public SyncedEntry<int> AliveForMax { get; private set; }

        //Costs
        [DataMember] public SyncedEntry<float> DoorCost { get; private set; }
        [DataMember] public SyncedEntry<float> BigDoorCost { get; private set; }
        [DataMember] public SyncedEntry<float> ItemCost { get; private set; }
        [DataMember] public SyncedEntry<float> MiscCost { get; private set; }

        /**
         * Constructor made to website specs
         */
        public PoltergeistConfig(ConfigFile cfg) : base(Poltergeist.MOD_GUID)
        {
            ConfigManager.Register(this);
            SyncComplete += AfterSync;

            //Client things
            DefaultToVanilla = cfg.Bind("Client-Side",
                "DefaultToVanilla",
                false,
                "If true, the vanilla spectate system will be used by default on death.");
            LightIntensity = cfg.Bind("Client-Side",
                "GhostLightIntensity",
                5f,
                "The intensity of the global light when dead.\n" +
                "WARNING: This game has a lot of fog, so excessively high values can decrease visibility.");

            //Synced things
            MaxPower = cfg.BindSyncedEntry(
                "Synced",
                "MaxPower",
                100f,
                "The maximum power available to ghosts."
                );
            Recharge = cfg.BindSyncedEntry(
                "Synced",
                "PowerRecharge",
                10f,
                "How much power ghosts recharge every second."
                );
            AliveForMax = cfg.BindSyncedEntry(
                "Synced",
                "AliveForMaxPower",
                1,
                "The maximum numbers that can be alive for ghosts to have max power.\n" + 
                "(As soon as this many or fewer players are alive, all ghosts will be at max power.)"
                );

            //Object costs
            DoorCost = cfg.BindSyncedEntry(
                "Synced: Costs",
                "DoorCost",
                10f,
                "How much power it costs to open and close doors."
                );
            BigDoorCost = cfg.BindSyncedEntry(
                "Synced: Costs",
                "BigDoorCost",
                50f,
                "How much power it costs to open and close pneumatic doors."
                );
            ItemCost = cfg.BindSyncedEntry(
                "Synced: Costs",
                "ItemCost",
                5f,
                "How much power it costs to use noisy items on the ground."
                );
            MiscCost = cfg.BindSyncedEntry(
                "Synced: Costs",
                "MiscCost",
                5f,
                "How much power it costs to use anything that doesn't fall under the other settings."
                );

            //Bound all of the settings (can't be negative)
            LightIntensity.Value = MathF.Max(LightIntensity.Value, 0);
            MaxPower.Value = MathF.Max(MaxPower.Value, 0);
            Recharge.Value = MathF.Max(Recharge.Value, 0);
            AliveForMax.Value = Math.Max(AliveForMax.Value, 0);
            DoorCost.Value = MathF.Max(DoorCost.Value, 0);
            BigDoorCost.Value = MathF.Max(BigDoorCost.Value, 0);
            ItemCost.Value = MathF.Max(ItemCost.Value, 0);
            MiscCost.Value = MathF.Max(MiscCost.Value, 0);

            Poltergeist.DebugLog("Finished generating config");
        }

        /**
         * After syncing, mark us as synced and re-overwrite the file
         */
        private void AfterSync(object sender, EventArgs e)
        {
            Poltergeist.DebugLog("After Sync event is firing");

            //Re-overwrite the file
            float oldVal = Default.MaxPower.Value;
            Default.MaxPower.Value = -999;
            Default.MaxPower.Value = oldVal;

            oldVal = Default.Recharge.Value;
            Default.Recharge.Value = -999;
            Default.Recharge.Value = oldVal;

            int oldValInt = Default.AliveForMax.Value;
            Default.AliveForMax.Value = -999;
            Default.AliveForMax.Value = oldValInt;

            oldVal = Default.DoorCost.Value;
            Default.DoorCost.Value = -999;
            Default.DoorCost.Value = oldVal;

            oldVal = Default.BigDoorCost.Value;
            Default.BigDoorCost.Value = -999;
            Default.BigDoorCost.Value = oldVal;

            oldVal = Default.ItemCost.Value;
            Default.ItemCost.Value = -999;
            Default.ItemCost.Value = oldVal;

            oldVal = Default.MiscCost.Value;
            Default.MiscCost.Value = -999;
            Default.MiscCost.Value = oldVal;
        }
    }
}
