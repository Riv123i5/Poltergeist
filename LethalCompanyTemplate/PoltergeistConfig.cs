using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
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
            Poltergeist.DebugLog("test0");
            ConfigManager.Register(this);

            Poltergeist.DebugLog("Test1");
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
            Poltergeist.DebugLog("Test2");

            //Synced things
            MaxPowerConfig = cfg.BindSyncedEntry(
                "Synced",
                "MaxPower",
                DEFAULT_MAXPOWER,
                new ConfigDescription(
                    "The maximum power available to ghosts.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            RechargeConfig = cfg.BindSyncedEntry(
                "Synced",
                "PowerRecharge",
                DEFAULT_RECHARGE,
                new ConfigDescription(
                    "How much power ghosts recharge every second.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            AliveForMaxConfig = cfg.BindSyncedEntry(
                "Synced",
                "AliveForMaxPower",
                DEFAULT_ALIVEFORMAX,
                new ConfigDescription(
                    "The maximum numbers that can be alive for ghosts to have max power.\n" +
                    "(As soon as this many or fewer players are alive, all ghosts will be at max power.)",
                    new AcceptableValueRange<int>(0, int.MaxValue)
                    )
                );
            Poltergeist.DebugLog("Test3");

            //Object costs
            DoorCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "DoorCost",
                DEFAULT_DOORCOST,
                new ConfigDescription(
                    "How much power it costs to open and close doors.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            BigDoorCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "BigDoorCost",
                DEFAULT_BIGDOORCOST,
                new ConfigDescription(
                    "How much power it costs to open and close pneumatic doors.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            ItemCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "ItemCost",
                DEFAULT_ITEMCOST,
                new ConfigDescription(
                    "How much power it costs to use noisy items on the ground.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            MiscCostConfig = cfg.BindSyncedEntry(
                "Synced: Costs",
                "MiscCost",
                DEFAULT_MISCCOST,
                new ConfigDescription(
                    "How much power it costs to use anything that doesn't fall under the other settings.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            Poltergeist.DebugLog("Test4");

            //Bound light intensity
            LightIntensity.Value = MathF.Max(LightIntensity.Value, 0);
            Poltergeist.DebugLog("Test5");

            //Set up the events
            //InitialSyncCompleted += AfterSync;
            MaxPowerConfig.Changed += AfterSyncFloat;
            RechargeConfig.Changed += AfterSyncFloat;
            AliveForMaxConfig.Changed += AfterSyncInt;
            DoorCostConfig.Changed += AfterSyncFloat;
            BigDoorCostConfig.Changed += AfterSyncFloat;
            ItemCostConfig.Changed += AfterSyncFloat;
            MiscCostConfig.Changed += AfterSyncFloat;
            //SyncReverted += AfterRevert;
            Poltergeist.DebugLog("Test6");

            //Set the interface to use the instance values
            InstanceAsInterface();

            Poltergeist.DebugLog("Finished generating config");
            Poltergeist.DebugLog("Test7");
        }

        /**
         * After syncing, mark us as synced and update the interface
         */
        private void AfterSyncFloat(object sender, SyncedSettingChangedEventArgs<float> e)
        {
            Poltergeist.DebugLog("After Sync event is firing for " + e.ChangedEntry.Entry.Definition.Key);
        }
        private void AfterSyncInt(object sender, SyncedSettingChangedEventArgs<int> e)
        {
            Poltergeist.DebugLog("After Sync event is firing for " + e.ChangedEntry.Entry.Definition.Key);
        }

        /**
         * Sets the public interface to have the values from the instance
         */
        private static void InstanceAsInterface()
        {
            //If something with the instance is wrong, force the defaults instead
            /*if(Poltergeist.config.MaxPowerConfig == null)
            {
                ForceDefaults();
                return;
            }*/

            MaxPower = Poltergeist.config.MaxPowerConfig.Value;
            Recharge = Poltergeist.config.RechargeConfig.Value;
            AliveForMax = Poltergeist.config.AliveForMaxConfig.Value;
            DoorCost = Poltergeist.config.DoorCostConfig.Value;
            BigDoorCost = Poltergeist.config.BigDoorCostConfig.Value;
            ItemCost = Poltergeist.config.ItemCostConfig.Value;
            MiscCost = Poltergeist.config.MiscCostConfig.Value;
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

            if (!synced) //CHECK FOR HOST
                ForceDefaults();

            synced = true;
        }
    }
}
