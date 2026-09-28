using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ValheimSkillCustomizer
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SkillCustomizerPlugin : BaseUnityPlugin
    {
        private const string ModGUID = "com.viking.valheimskillcustomizer";
        private const string ModName = "Valheim Skill Customizer";
        private const string ModVersion = "1.2.0";

        private readonly Harmony harmony = new Harmony(ModGUID);

        // Configuration Entries
        public static ConfigEntry<bool> IsConfigLocked;
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<DeathPenaltyMode> DeathPenaltySetting;
        public static ConfigEntry<float> GlobalXpMultiplier;

        public static Dictionary<Skills.SkillType, ConfigEntry<float>> IndividualSkillMultipliers = new Dictionary<Skills.SkillType, ConfigEntry<float>>();

        public enum DeathPenaltyMode
        {
            NoSkillDrain,
            HalfSkillDrain,
            NormalSkillDrain,
            DoubleSkillDrain
        }

        private void Awake()
        {
            // --- 0. Master Settings / Locking ---
            ConfigurationManagerAttributes lockAttributes = new ConfigurationManagerAttributes { Order = 210 };
            IsConfigLocked = Config.Bind(
                "0. Master Settings (Made by viking)",
                "Lock Configuration",
                true,
                new ConfigDescription("If true, configuration settings will be locked to server-side values via ConditionalConfigSync for non-admin players.", null, lockAttributes)
            );

            ModEnabled = BindConfig(
                "0. Master Settings (Made by viking)",
                "Mod Enabled",
                true,
                "Enable or disable this mod's entire functionality completely. [Synced with Server]",
                200
            );

            DeathPenaltySetting = BindConfig(
                "1. Death Penalty",
                "Skill Drain Modifier",
                DeathPenaltyMode.NormalSkillDrain,
                "Choose how much skill XP you lose upon death. [Synced with Server]",
                100
            );

            GlobalXpMultiplier = BindConfigRange(
                "2. Global XP Modifier",
                "All Skills Global Multiplier",
                1.0f,
                "Global multiplier stacked on top of individual skill settings. [Synced with Server]",
                99
            );
            BindStepEnforcer(GlobalXpMultiplier);

            int orderTracker = 90;
            foreach (Skills.SkillType skillType in Enum.GetValues(typeof(Skills.SkillType)))
            {
                if (skillType == Skills.SkillType.None || skillType == Skills.SkillType.All) continue;

                string skillName = skillType.ToString();

                ConfigEntry<float> skillConfig = BindConfigRange(
                    "3. Individual Skill Multipliers",
                    $"{skillName} XP Multiplier",
                    1.0f,
                    $"{skillName} experience point multiplier. [Synced with Server]",
                    orderTracker--
                );

                BindStepEnforcer(skillConfig);
                IndividualSkillMultipliers[skillType] = skillConfig;
            }

            harmony.PatchAll();
            Logger.LogInfo($"{ModName} loaded successfully! Made by Viking.");
        }

        // FIXED: Using concrete explicitly-typed objects to ensure C# 7.3 runtime stability
        private ConfigEntry<T> BindConfig<T>(string group, string name, T value, string description, int order)
        {
            ConfigurationManagerAttributes attributes = new ConfigurationManagerAttributes { Order = order };
            return Config.Bind(group, name, value, new ConfigDescription(description, null, attributes));
        }

        // FIXED: Replaced anonymous types with explicit class declarations
        private ConfigEntry<float> BindConfigRange(string group, string name, float value, string description, int order)
        {
            ConfigurationManagerAttributes attributes = new ConfigurationManagerAttributes { Order = order, ShowRangeAsPercent = false };
            return Config.Bind(group, name, value, new ConfigDescription(description, new AcceptableValueRange<float>(0f, 10f), attributes));
        }

        private void BindStepEnforcer(ConfigEntry<float> configEntry)
        {
            configEntry.SettingChanged += (sender, args) =>
            {
                float roundedValue = (float)Math.Round(configEntry.Value * 2, MidpointRounding.AwayFromZero) / 2f;
                if (Math.Abs(configEntry.Value - roundedValue) > 0.01f)
                {
                    configEntry.Value = roundedValue;
                }
            };
        }

        // --- HARMONY PATCHES ---
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        static class Patch_RaiseSkill
        {
            static void Prefix(Skills.SkillType skillType, ref float factor)
            {
                if (!ModEnabled.Value) return;

                factor *= GlobalXpMultiplier.Value;

                if (IndividualSkillMultipliers.TryGetValue(skillType, out var skillConfig))
                {
                    factor *= skillConfig.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
        static class Patch_OnDeath
        {
            static bool Prefix(Skills __instance)
            {
                if (!ModEnabled.Value) return true;

                if (DeathPenaltySetting.Value == DeathPenaltyMode.NoSkillDrain)
                {
                    return false;
                }

                float multiplier = 1.0f;
                switch (DeathPenaltySetting.Value)
                {
                    case DeathPenaltyMode.HalfSkillDrain:
                        multiplier = 0.5f;
                        break;
                    case DeathPenaltyMode.DoubleSkillDrain:
                        multiplier = 2.0f;
                        break;
                    case DeathPenaltyMode.NormalSkillDrain:
                    default:
                        return true;
                }

                float vanillaDrainRate = 0.05f;
                float customDrainRate = vanillaDrainRate * multiplier;

                var m_skillDataField = AccessTools.Field(typeof(Skills), "m_skillData");
                var skillDataMap = m_skillDataField.GetValue(__instance) as IDictionary;

                if (skillDataMap != null)
                {
                    foreach (DictionaryEntry entry in skillDataMap)
                    {
                        var skillObj = entry.Value;
                        var levelField = AccessTools.Field(skillObj.GetType(), "m_level");

                        if (levelField != null)
                        {
                            float currentLevel = (float)levelField.GetValue(skillObj);
                            currentLevel -= currentLevel * customDrainRate;
                            if (currentLevel < 0f) currentLevel = 0f;

                            levelField.SetValue(skillObj, currentLevel);
                        }
                    }
                }

                var m_playerField = AccessTools.Field(typeof(Skills), "m_player");
                Player player = m_playerField.GetValue(__instance) as Player;

                if (player != null)
                {
                    player.Message(MessageHud.MessageType.TopLeft, "$msg_skills_lowered", 0, null);
                }

                return false;
            }
        }
    }

    public class ConfigurationManagerAttributes
    {
        public bool? Browsable;
        public bool? ShowRangeAsPercent;
        public int? Order;
    }
}

