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
        //Default values of the synced settings
        private const float DEFAULT_MAXPOWER = 100;
        private const float DEFAULT_RECHARGE = 10;
        private const int DEFAULT_ALIVEFORMAX = 1;
        private const float DEFAULT_DOORCOST = 10;
        private const float DEFAULT_BIGDOORCOST = 50;
        private const float DEFAULT_ITEMCOST = 5;
        private const float DEFAULT_MISCCOST = 5;

        //Static flags for managing state
        private static bool synced = false;

        //Client configs
        public ConfigEntry<bool> DefaultToVanilla { get; private set; }
        public ConfigEntry<float> LightIntensity { get; private set; }

        //Regular synced configs
        [DataMember] private SyncedEntry<float> MaxPowerConfig;
        [DataMember] private SyncedEntry<float> RechargeConfig;
        [DataMember] private SyncedEntry<int> AliveForMaxConfig;

        //Costs
        [DataMember] private SyncedEntry<float> DoorCostConfig;
        [DataMember] private SyncedEntry<float> BigDoorCostConfig;
        [DataMember] private SyncedEntry<float> ItemCostConfig;
        [DataMember] private SyncedEntry<float> MiscCostConfig;

        //Public interface
        public static float MaxPower {  get; private set; }
        public static float Recharge { get; private set; }
        public static float AliveForMax { get; private set; }
        public static float DoorCost { get; private set; }
        public static float BigDoorCost { get; private set; }
        public static float ItemCost { get; private set; }
        public static float MiscCost { get; private set; }

        /**
         * Constructor made to website specs
         */
        public PoltergeistConfig(ConfigFile cfg) : base(Poltergeist.MOD_GUID)
        {
            ConfigManager.Register(this);
            SyncComplete += AfterSync;
            SyncReverted += AfterRevert;

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
            MaxPowerConfig = cfg.BindSyncedEntry(
                "Synced",
                "MaxPower",
                DEFAULT_MAXPOWER,
                "The maximum power available to ghosts."
                );
            RechargeConfig = cfg.BindSyncedEntry(
                "Synced",
                "PowerRecharge",
                DEFAULT_RECHARGE,
                "How much power ghosts recharge every second."
                );
            AliveForMaxConfig = cfg.BindSyncedEntry(
                "Synced",
                "AliveForMaxPower",
                DEFAULT_ALIVEFORMAX,
                "The maximum numbers that can be alive for ghosts to have max power.\n" + 
                "(As soon as this many or fewer players are alive, all ghosts will be at max power.)"
                );

            //Object costs
            DoorCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "DoorCost",
                DEFAULT_DOORCOST,
                "How much power it costs to open and close doors."
                );
            BigDoorCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "BigDoorCost",
                DEFAULT_BIGDOORCOST,
                "How much power it costs to open and close pneumatic doors."
                );
            ItemCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "ItemCost",
                DEFAULT_ITEMCOST,
                "How much power it costs to use noisy items on the ground."
                );
            MiscCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "MiscCost",
                DEFAULT_MISCCOST,
                "How much power it costs to use anything that doesn't fall under the other settings."
                );

            //Bound all of the settings (can't be negative)
            LightIntensity.Value = MathF.Max(LightIntensity.Value, 0);
            MaxPowerConfig.Value = MathF.Max(MaxPowerConfig.Value, 0);
            RechargeConfig.Value = MathF.Max(RechargeConfig.Value, 0);
            AliveForMaxConfig.Value = Math.Max(AliveForMaxConfig.Value, 0);
            DoorCostConfig.Value = MathF.Max(DoorCostConfig.Value, 0);
            BigDoorCostConfig.Value = MathF.Max(BigDoorCostConfig.Value, 0);
            ItemCostConfig.Value = MathF.Max(ItemCostConfig.Value, 0);
            MiscCostConfig.Value = MathF.Max(MiscCostConfig.Value, 0);

            //Set the interface to use the instance values
            InstanceAsInterface();

            Poltergeist.DebugLog("Finished generating config");
        }

        /**
         * After syncing, mark us as synced and re-overwrite the file
         */
        private void AfterSync(object sender, EventArgs e)
        {
            Poltergeist.DebugLog("After Sync event is firing");
            synced = true;

            //Re-overwrite the file
            float oldVal = Default.MaxPowerConfig.Value;
            Default.MaxPowerConfig.Value = -999;
            Default.MaxPowerConfig.Value = oldVal;

            oldVal = Default.RechargeConfig.Value;
            Default.RechargeConfig.Value = -999;
            Default.RechargeConfig.Value = oldVal;

            int oldValInt = Default.AliveForMaxConfig.Value;
            Default.AliveForMaxConfig.Value = -999;
            Default.AliveForMaxConfig.Value = oldValInt;

            oldVal = Default.DoorCostConfig.Value;
            Default.DoorCostConfig.Value = -999;
            Default.DoorCostConfig.Value = oldVal;

            oldVal = Default.BigDoorCostConfig.Value;
            Default.BigDoorCostConfig.Value = -999;
            Default.BigDoorCostConfig.Value = oldVal;

            oldVal = Default.ItemCostConfig.Value;
            Default.ItemCostConfig.Value = -999;
            Default.ItemCostConfig.Value = oldVal;

            oldVal = Default.MiscCostConfig.Value;
            Default.MiscCostConfig.Value = -999;
            Default.MiscCostConfig.Value = oldVal;

            //Set the host's sent config to be what we use
            InstanceAsInterface();
        }

        /**
         * After we revert, set the interface to the instance (which is now our settings)
         */
        private void AfterRevert(object  sender, EventArgs e)
        {
            InstanceAsInterface();
            synced = false;
        }

        /**
         * Sets the public interface to have the values from the instance
         */
        private static void InstanceAsInterface()
        {
            //If something with the instance is wrong, force the defaults instead
            if(Instance.MaxPowerConfig == null)
            {
                ForceDefaults();
                return;
            }

            MaxPower = Instance.MaxPowerConfig.Value;
            Recharge = Instance.RechargeConfig.Value;
            AliveForMax = Instance.AliveForMaxConfig.Value;
            DoorCost = Instance.DoorCostConfig.Value;
            BigDoorCost = Instance.BigDoorCostConfig.Value;
            ItemCost = Instance.ItemCostConfig.Value;
            MiscCost = Instance.MiscCostConfig.Value;
        }

        /**
         * Forces the public interface to take on the default settings
         */
        private static void ForceDefaults()
        {
            MaxPower = DEFAULT_MAXPOWER;
            Recharge = DEFAULT_RECHARGE;
            AliveForMax = DEFAULT_ALIVEFORMAX;
            DoorCost = DEFAULT_DOORCOST;
            BigDoorCost = DEFAULT_BIGDOORCOST;
            ItemCost = DEFAULT_ITEMCOST;
            MiscCost = DEFAULT_MISCCOST;
        }

        /**
         * If we didn't sync and aren't the host, need to force default settings
         */
        public static void CheckSync()
        {
            Poltergeist.DebugLog("Checking for sync");

            if (!synced && !IsHost)
                ForceDefaults();

            synced = true;
        }
    }
}
