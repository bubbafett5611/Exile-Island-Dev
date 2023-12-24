// Reference: 0Harmony
using Facepunch;
#if CARBON
using HarmonyLib;
#else
using Harmony;
#endif
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WebSocketSharp;

/* Ideas
 * Vehiclular combatant - more damage to vehicles (maybe including bradley and heli?)
 * Add heli as XP source.
 * Add CUI option to enable/disable components that are shredded by the shredder perk.
 * Add an XP cap for certain sources (quests, building etc).
 * Colour the pump bar based on xp progression for the level
 * Possible buff for healing percentage when using medical deviucs with whitelist or blacklist
 * - Add option to disable BotRespawn
 * - Reach out to Steen and ask him to add support natively for xp
 * Add a dictionary to override the yield for certain ore types.
 * Check the HV rockets update to see why it's causing issues.
 */

/* 1.4.10
 * Fixed an issue with the cooking ultimate adding the raiding ultimate cooldown when displaying the on cool down message.
 * Added config option to prevent skin IDs from force updating when set to 0.
 * Added the locate nodes command to UI
 * Added a config option to control xp based on crafting time.
 * Added support for events: Arctic Base Event, Gas Station Event, Sputnik event and Shipwreck Event.
 * Added localization for a number of different messages.
 */

namespace Oxide.Plugins
{
    [Info("Skill Tree", "imthenewguy", "1.4.10")]
    [Description("Skills on a tree!")]
    class SkillTree : RustPlugin
    {
        #region Config

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("General settings")]
            public GeneralSettings general_settings = new GeneralSettings();

            public class GeneralSettings
            {
                [JsonProperty("Skill points per level")]
                public int points_per_level = 2;

                [JsonProperty("Maximum points a player can spend [default]")]
                public int max_skill_points = 200;

                [JsonProperty("Modified max skill points based on permissions [must be higher than default]")]
                public Dictionary<string, int> max_skill_points_override = new Dictionary<string, int>();

                [JsonProperty("Maximum level a player can get to")]
                public int max_player_level = 100;

                [JsonProperty("Allow players to respec their skill points")]
                public bool allow_respecs = true;

                [JsonProperty("Cost per point to respec [default]")]
                public double respec_cost = 30;

                [JsonProperty("Cost per point to respec based on permissions [must be lower than default]")]
                public Dictionary<string, double> respec_cost_override = new Dictionary<string, double>();

                [JsonProperty("Permission based level requirement override")]
                public Dictionary<string, PermOverride> level_requirement_override = new Dictionary<string, PermOverride>(StringComparer.InvariantCultureIgnoreCase);

                [JsonProperty("Permission based point requirement override")]
                public Dictionary<string, PermOverride> point_requirement_override = new Dictionary<string, PermOverride>(StringComparer.InvariantCultureIgnoreCase);

                [JsonProperty("Currency type to respec [scrap, economics, srp, custom]")]
                public string respec_currency = "scrap";

                [JsonProperty("If currency is set to custom, what are the details of the item")]
                public CustomCurrency respec_currency_custom = new CustomCurrency();

                [JsonProperty("Multiplier increase after each respec [0.2 = a 20% increase in the cost to respec each time. 0 = no increase] [resets on wipe or data reset]")]
                public float respec_multiplier = 0;

                [JsonProperty("Maximum value that the respec multiplier can get to [0 = no limit]")]
                public float respec_multiplier_max = 0;

                [JsonProperty("List of rewards the player receives based on level")]
                public Dictionary<int, LevelReward> level_rewards = new Dictionary<int, LevelReward>();

                [JsonProperty("Require players to have specific tree permissions to open them")]
                public bool require_tree_perms = false;

                [JsonProperty("Show the quick navigation buttons to the player")]
                public bool show_navigation_buttons = true;

                [JsonProperty("Drop bag on death")]
                public bool drop_bag_on_death = true;

                [JsonProperty("XP pump bar settings")]
                public PumpBar pump_bar_settings = new PumpBar();

                [JsonProperty("Cache images using skinid or url? [skinid, url]")]
                public string image_cache_source = "skinid";

                [JsonProperty("Redownload all images when the plugin reloads if using URL?")]
                public bool replace_on_reload = false;
            }

            [JsonProperty("Base yield settings")]
            public BaseYieldSettings base_yield_settings = new BaseYieldSettings();

            public class BaseYieldSettings
            {
                [JsonProperty("Allow Skill Tree to adjust the base amount of resource received? Buffs will base their modifiers off of the amended base amount.")]
                public bool adjust_base_yield = false;

                [JsonProperty("Yield types multipliers [1.0 = vanilla]")]
                public Dictionary<YieldTypes, float> multipliers = new Dictionary<YieldTypes, float>();
            }

            [JsonProperty("Buff settings")]
            public BuffSettings buff_settings = new BuffSettings();

            public class BuffSettings
            {
                [JsonProperty("Minimum components for the component perk")]
                public int min_components = 1;

                [JsonProperty("Maximum components for the component perk")]
                public int max_components = 1;

                [JsonProperty("Minimum electrical components for the electrical component perk")]
                public int min_electrical_components = 1;

                [JsonProperty("Maximum electrical components for the electrical component perk")]
                public int max_electrical_components = 1;

                [JsonProperty("Minimum additional scrap to be added to crates and barrels")]
                public int min_extra_scrap = 1;

                [JsonProperty("Maximum additional scrap to be added to crates and barrels")]
                public int max_extra_scrap = 2;

                [JsonProperty("PVP Buff Critical damage modifier. Picks a random value between 0 and the declared value, and += the damage% onto the hit")]
                public float pvp_critical_modifier = 0.3f;

                [JsonProperty("Should the LootPickup buff only work with melee weapons?")]
                public bool lootPickupBuffMeleeOnly = false;

                [JsonProperty("Should we have a maximum distance for the loot pickup buff [0 = unlimited]?")]
                public float loot_pickup_buff_max_distance = 0;

                [JsonProperty("Allow the LootPickup buff to work with road-side signs?")]
                public bool allow_roadsigns = false;

                [JsonProperty("List of animals for animal resist ability", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> animals = new List<string>();

                [JsonProperty("Details of a horse to change when a player mounts a horse with the relevant perk unlocked")]
                public HorseStats horse_buff_info = new HorseStats();

                [JsonProperty("Delay for HealthRegen perk after taking damage")]
                public float health_regen_combat_delay = 5f;

                [JsonProperty("Delay between attempts at tracking animals")]
                public float track_delay = 10f;

                [JsonProperty("List of skins that the rationer perk will not refund")]
                public List<ulong> no_refund_item_skins = new List<ulong>();

                [JsonProperty("Bag cooldown time")]
                public float bag_cooldown_time = 10f;

                [JsonProperty("Bag prefab")]
                public string bag_prefab = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";

                [JsonProperty("Allow the Harvest Grown Yield to increase the amount of clones a player receives?")]
                public bool clone_yield = true;

                [JsonProperty("Primitive weapons for the primitive weapons ability", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> primitive_weapons = new List<string>();

                [JsonProperty("Harvesting yield blacklist [items listed here will not be affected by the harvesting yield perks]")]
                public List<string> harvest_yield_blacklist = new List<string>();

                [JsonProperty("List of items that the Durability perk will not work with")]
                public List<string> durability_blacklist = new List<string>();

                [JsonProperty("Force all modified extended weapons to unload their ammo when the plugin unloads/hook all users with the buff log off?")]
                public bool force_unload_extended_mag_weapons_unload = false;

                [JsonProperty("Prevent flyhack kicks when a player lands?")]
                public bool prevent_flyhack_kick_fall_damage = false;

                [JsonProperty("Automatically apply the boat turbo when a player mounts the boat? [Disables the turbo command]")]
                public bool boat_turbo_on_mount = false;

                [JsonProperty("Forager settings")]
                public ForagerSettings forager_settings = new ForagerSettings();
                public class ForagerSettings
                {
                    [JsonProperty("Command to use the ability")]
                    public string command = "forage";

                    [JsonProperty("Distance to search for map collectibles")]
                    public float distance = 100f;

                    [JsonProperty("Time that the locations are displayed on the screen")]
                    public float time_on_screen = 60f;

                    [JsonProperty("Usage cooldown")]
                    public float cooldown = 60f;

                    [JsonProperty("Black list of shortnames for collecibles that won't be displayed")]
                    public List<string> blacklist = new List<string>();

                    [JsonProperty("Display colours")]
                    public Dictionary<string, float[]> displayColours = new Dictionary<string, float[]>();
                }

                [JsonProperty("Tea looter settings")]
                public TeaLooterSettings tea_looter_settings = new TeaLooterSettings();

                public class TeaLooterSettings
                {
                    [JsonProperty("Minimum stack of tea that the player will receive when the Tea Looter buff procs?")]
                    public int min_tea = 1;

                    [JsonProperty("Maximum stack of tea that the player will receive when the Tea Looter buff procs?")]
                    public int max_tea = 1;

                    [JsonProperty("Tea looter table [shortname : drop weight]")]
                    public Dictionary<string, int> TeaDropTable = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

                    [JsonProperty("Containers that tea can be found in")]
                    public List<string> containers = new List<string>();
                }

                [JsonProperty("Raid perk settings")]
                public RaidTreeSettings raid_perk_settings = new RaidTreeSettings();
                public class RaidTreeSettings
                {
                    [JsonProperty("Settings for trap_damage_reduction")]
                    public TrapDamageReductionSettings trap_damage_reduction_settings = new TrapDamageReductionSettings();
                    public class TrapDamageReductionSettings
                    {
                        [JsonProperty("Traps blacklist - will ignore the buff")]
                        public List<string> blacklist = new List<string>();

                        [JsonProperty("Only allow this buff to work with raidable base traps")]
                        public bool raidable_bases_only = false;
                    }

                    [JsonProperty("Settings for trap_damage_increase")]
                    public TrapDamageIncreaseSettings trap_damage_increase_settings = new TrapDamageIncreaseSettings();
                    public class TrapDamageIncreaseSettings
                    {
                        [JsonProperty("Traps blacklist - will ignore the buff")]
                        public List<string> blacklist = new List<string>();

                        [JsonProperty("Only allow this buff to work with raidable base traps")]
                        public bool raidable_bases_only = false;
                    }

                    [JsonProperty("Settings for Personal_Explosive_Reduction")]
                    public PersonalExplosiveReductionSettings personal_explosive_reduction_settings = new PersonalExplosiveReductionSettings();
                    public class PersonalExplosiveReductionSettings
                    {
                        [JsonProperty("Prefabs blacklist - will ignore the buff")]
                        public List<string> blacklist = new List<string>();

                        [JsonProperty("Work with fire explosives such as molotov's and incin rockets?")]
                        public bool fire_damage_reduction = true;
                    }

                    [JsonProperty("Settings for Double_Explosion_chance")]
                    public DoubleExplosionChanceSettings Double_Explosion_chance_settings = new DoubleExplosionChanceSettings();
                    public class DoubleExplosionChanceSettings
                    {
                        [JsonProperty("Prefabs black - will not trigger with items listed here")]
                        public List<string> blacklist = new List<string>();

                        [JsonProperty("Only allow this buff to work with raidable base?")]
                        public bool raidable_bases_only = false;
                    }

                    [JsonProperty("Settings for Explosion_Radius")]
                    public ExpolosionRadiusSettings Explosion_Radius_settings = new ExpolosionRadiusSettings();
                    public class ExpolosionRadiusSettings
                    {
                        [JsonProperty("Prefabs blacklist - will ignore the buff")]
                        public List<string> blacklist = new List<string>();

                        [JsonProperty("Add the difference to the minimum explosion radius")]
                        public bool add_to_minimum = true;

                        [JsonProperty("Only allow this buff to work with raidable base?")]
                        public bool raidable_bases_only = false;
                    }

                    [JsonProperty("Settings for Lock_Picker")]
                    public LockPickerSettings Lock_Picker_settings = new LockPickerSettings();
                    public class LockPickerSettings
                    {
                        [JsonProperty("Command required for the player to activate their lockpick ability")]
                        public string pick_command = "picklock";

                        [JsonProperty("Time after the command has been used before the ability use expires [seconds]")]
                        public float time = 30;

                        [JsonProperty("Delay before the Lock_Picker ability can be used again [seconds]")]
                        public float use_delay = 600;

                        [JsonProperty("Only allow this buff to work with raidable base?")]
                        public bool raidable_bases_only = false;

                        [JsonProperty("Damage the player receives when they fail an attempt")]
                        public float damage_per_fail = 0;

                        [JsonProperty("Show the pick lock timer when activating the ability")]
                        public bool show_timer = true;

                        [JsonProperty("How often should the timer update")]
                        public int timer_tick_rate = 1;

                        [JsonProperty("Set the lock to unlocked when successfully picking a lock? [false will open the entity but keep it locked]")]
                        public bool unlock_entity = false;
                    }

                    [JsonProperty("Settings for Dudless_Explosive")]
                    public DudlessExplosiveSettings Dudless_Explosiv_settings = new DudlessExplosiveSettings();
                    public class DudlessExplosiveSettings
                    {
                        [JsonProperty("Only allow this buff to work with raidable base?")]
                        public bool raidable_bases_only = false;
                    }

                    [JsonProperty("Settings for Trap_Spotter")]
                    public TrapSpotterSettings Trap_Spotter_settings = new TrapSpotterSettings();
                    public class TrapSpotterSettings
                    {
                        [JsonProperty("Max distance from the player to search for traps")]
                        public float distance = 20f;

                        [JsonProperty("Cooldown between uses [seconds]")]
                        public float cooldown = 60f;

                        [JsonProperty("Time that the traps will be displayed for [seconds]")]
                        public float time_on_screen = 60f;

                        [JsonProperty("Show the names of each trap prefab")]
                        public bool show_names = true;

                        [JsonProperty("Command to perform the search")]
                        public string command = "traps";

                        [JsonProperty("Only allow this buff to work with raidable base?")]
                        public bool raidable_bases_only = false;

                        [JsonProperty("Colours that the traps will be displayed in")]
                        public Dictionary<string, float[]> trap_colours = new Dictionary<string, float[]>();


                    }
                }

                [JsonProperty("Underwater Breathing settings")]
                public UnderwaterBreathingSettings underwaterSettings = new UnderwaterBreathingSettings();
                public class UnderwaterBreathingSettings
                {
                    public string anchor_min = "1 1";
                    public string anchor_max = "1 1";
                    public string offset_min = "-48 -48";
                    public string offset_max = "-12 -12";
                }

                [JsonProperty("Allow the UnderwaterDamageBonus to function in PVP")]
                public bool UnderwaterDamageBonus_pvp = true;

                [JsonProperty("Base the crafting xp on the time it takes to craft the item? [false will use blueprint craft time]")]
                public bool timeBasedCraftingXP = true;
            }

            [JsonProperty("Chat command settings")]
            public ChatCommands chat_commands = new ChatCommands();

            public class ChatCommands
            {
                [JsonProperty("Use mouse 3 to toggle the boat turbo (performance heavy on high pop servers). Set false to use a chat command instead")]
                public bool use_input_key_boat = false;

                [JsonProperty("Chat command for turbo")]
                public string turbo_cmd = "turbo";

                [JsonProperty("Chat commands to open the skill tree", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> chat_cmd = new List<string>() { "st", "skilltree", "skills" };

                [JsonProperty("Chat/console commands to open the score board", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> score_chat_cmd = new List<string>() { "score", "scoreboard" };

                [JsonProperty("Chat command for tracking an animal with the AnimalTracker buff")]
                public string track_animal_cmd = "track";
            }

            [JsonProperty("XP settings")]
            public XPSettings xp_settings = new XPSettings();

            public class XPSettings
            {
                [JsonProperty("XP Loss settings")]
                public XPLossSettings xp_loss_settings = new XPLossSettings();

                public class XPLossSettings
                {
                    [JsonProperty("Allow players to go into xp debt if they lose more xp than their level would allow?")]
                    public bool allow_xp_debt = true;

                    [JsonProperty("Use the players xp (not the level xp) to calculate loss")]
                    public bool percentage_of_current_xp = false;

                    [JsonProperty("Death penalty grace period - players won't lose xp if they die again within the specified time [seconds]")]
                    public float no_xp_loss_time = 0;

                    [JsonProperty("Percentage of xp into their current level that players lose when killed in PVP")]
                    public double pvp_death_penalty = 20;

                    [JsonProperty("Percentage of xp into their current level that players lose when killed in PVE")]
                    public double pve_death_penalty = 20;

                    [JsonProperty("Percentage of xp into their current level that players lose when they suicide")]
                    public double suicide_death_penalty = 0;

                    [JsonProperty("Prevent xp loss when a player is offline?")]
                    public bool prevent_offline_xp_loss = true;

                    [JsonProperty("Permission based modifiers [1.0 = no reduced amount]")]
                    public Dictionary<string, double> xp_loss_override = new Dictionary<string, double>();
                }

                [JsonProperty("Permissions to adjust xp gain modifiers (skilltree.<perm>) [1.0 is default modifier]")]
                public Dictionary<string, double> xp_perm_modifier = new Dictionary<string, double>();

                [JsonProperty("How long should the xp be displayed for")]
                public float xp_display_time = 1f;

                [JsonProperty("Colour of the xp text when unmodified")]
                public string xp_display_col_unmodified = "FFFFFF";

                [JsonProperty("Colour of the xp text when modified")]
                public string xp_display_col_modified = "00b6ff";

                [JsonProperty("Prevent XP loss when dying at an event hosted by EventManager")]
                public bool prevent_xp_loss = true;

                [JsonProperty("Enable xp drop hud for players by default")]
                public bool enable_xp_drop_by_default = true;

                [JsonProperty("Restrict XP gain to the tools listed in their respective tools list?")]
                public bool white_listed_tools_only = false;

                [JsonProperty("Require grown plants to be ripe to provide xp")]
                public bool ripe_required = true;

                [JsonProperty("Give xp for crafting ingredients? [Requires Cooking.cs]")]
                public bool cooking_award_xp_ingredients = false;

                [JsonProperty("Whitelist - List of items to award crafting xp")]
                public List<string> craft_xp_whitelist = new List<string>();

                [JsonProperty("Blacklist - List of items that will not award crafting xp")]
                public List<string> craft_xp_blacklist = new List<string>();

                [JsonProperty("Use LootDefender to handle the XP for BradleyAPC")]
                public bool UseLootDefender = true;

                [JsonProperty("Allow players with god mode to receive xp?")]
                public bool allow_godemode_xp = true;

                [JsonProperty("Cooldown for awarding xp after using a swipe card")]
                public float swipe_card_xp_cooldown = 0;

                [JsonProperty("Decimal places for the xp to be rounded to")]
                public int xp_rounding = 2;

                [JsonProperty("List of meals that will not provide xp [Requires Cooking.cs]")]
                public List<string> cooking_black_list = new List<string>();

                [JsonProperty("Experience sources - Set xp value to 0 to disable for that type", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public XPSources xp_sources = new XPSources();

                [JsonProperty("Night time settings")]
                public NightTimeGains night_settings = new NightTimeGains();

                public class NightTimeGains
                {
                    [JsonProperty("Modifier for xp gained at night [1.0 = standard]")]
                    public float night_xp_gain_modifier = 1f;

                    [JsonProperty("Modifier for woodcutting yield at night [1.0 = standard]")]
                    public float night_woodcutting_yield_modifier = 1f;

                    [JsonProperty("Modifier for mining yield at night [1.0 = standard]")]
                    public float night_mining_yield_modifier = 1f;

                    [JsonProperty("Modifier for skinning yield at night [1.0 = standard]")]
                    public float night_skinning_yield_modifier = 1f;

                    [JsonProperty("Modifier for harvesting yield at night [1.0 = standard]")]
                    public float night_harvesting_yield_modifier = 1f;

                    [JsonProperty("Should the harvesting yield increase include player-grown plants?")]
                    public bool include_grown_harvesting = false;
                }

                [JsonProperty("Allow players to move their XP bar?")]
                public bool allow_move_xp_bar = true;
            }

            [JsonProperty("Wipe and plugin update settings")]
            public WipeUpdate wipe_update_settings = new WipeUpdate();

            public class WipeUpdate
            {
                [JsonProperty("Erase all data on wipe - wipes everything")]
                public bool erase_data_on_wipe = false;

                [JsonProperty("Erase ExtraPockets storage on wipe")]
                public bool erase_ExtraPockets_on_wipe = true;

                [JsonProperty("Refund skill points on server wipe")]
                public bool refund_sp_on_wipe = true;

                [JsonProperty("Give the player with the highest xp bonus skill points next wipe")]
                public bool bonus_skill_points = false;

                [JsonProperty("How many skill points should they receive for winning?")]
                public int bonus_skill_points_amount = 5;

                [JsonProperty("Automatically add new trees from the default config?")]
                public bool auto_update_trees = true;

                [JsonProperty("Automatically add new nodes from the default config?")]
                public bool auto_update_nodes = true;

                [JsonProperty("Starting skill points")]
                public int starting_skill_points = 0;

                [JsonProperty("Dictionary of permission based overrides for starting skill points")]
                public Dictionary<string, int> starting_skill_point_overrides = new Dictionary<string, int>();
            }

            [JsonProperty("Rested XP Settings")]
            public RestXPSettings rested_xp_settings = new RestXPSettings();

            public class RestXPSettings
            {
                [JsonProperty("Give players who have been offline a bonus to xp gain when they log in next?")]
                public bool rested_xp_enabled = true;

                [JsonProperty("Rested xp pool to accumulate per hour offline")]
                public double rested_xp_per_hour = 1000;

                [JsonProperty("Bonus xp rate while rested (until the rested xp pool is depleted) [0.25 = 25% bonus]")]
                public double rested_xp_rate = 0.25;

                [JsonProperty("Maximum xp a player can have in their rested pool [0 = no limit]")]
                public double rested_xp_pool_max = 25000;

                [JsonProperty("Reset rested xp pools on wipe?")]
                public bool rested_xp_reset_on_wipe = false;

                [JsonProperty("Modifiers based on permissions to adjust the rested xp value [1.0 = 100% increase. 0.0 = no increase]")]
                public Dictionary<string, float> rested_xp_modifier_perm_mod = new Dictionary<string, float>();
            }

            [JsonProperty("Tools and Black list/White list settings")]
            public ToolAndListSettings tools_black_white_list_settings = new ToolAndListSettings();

            public class ToolAndListSettings
            {
                [JsonProperty("Global black list - these items will not gain xp and benefits at all")]
                public List<string> black_listed_gather_items = new List<string>();

                [JsonProperty("Black listed parts for component and electrical luck abilities", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> comp_blacklist = new List<string>();

                [JsonProperty("Power tool modifiers (chainsaw and jackhammer). 1 = full xp/buffs. 0 = off. 0.5 = half xp/buffs.")]
                public PowerTools power_tool_modifier = new PowerTools()
                {
                    mining_yield_modifier = 0.25f,
                    mining_xp_modifier = 0.25f,
                    mining_luck_modifier = 0.25f,

                    skinning_yield_modifier = 0.25f,
                    skinning_xp_modifier = 0.25f,
                    skinning_luck_modifier = 0.25f,

                    woodcutting_yield_modifier = 0.25f,
                    woodcutting_xp_modifier = 0.25f,
                    woodcutting_luck_modifier = 0.25f
                };

                [JsonProperty("Extra Pockets black list - disallows items that match")]
                public List<string> black_list = new List<string>();

                [JsonProperty("Extra Pockets white list - will only allow items that match")]
                public List<string> white_list = new List<string>();

                [JsonProperty("Extra pockets button anchors")]
                public ExtraPocketsButtonAnchors extra_pockets_button_anchor = new ExtraPocketsButtonAnchors();

                [JsonProperty("A black list of items that will not be refunded when using the thrifty tinkerer buff", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> craft_refund_blacklist = new List<string>() { "gunpowder", "explosives", "sulfur" };

                [JsonProperty("A black list of items that will not be duplicated while using the thirfty duplicator buff", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> craft_duplicate_blacklist = new List<string>() { "gunpowder", "explosives", "sulfur" };

                [JsonProperty("Woodcutting tools - Tools that meet whitelist requirements and work with the reduced durability ability", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> wc_tools = new List<string>();

                [JsonProperty("Mining tools - Tools that meet whitelist requirements and work with the reduced durability ability", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> mining_tools = new List<string>();

                [JsonProperty("Skinning tools - Tools that meet whitelist requirements and work with the reduced durability ability", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> skinning_tools = new List<string>();

                [JsonProperty("Blacklist of weapon prefab shortnames that cannot benefit from the Extended Mag buff")]
                public List<string> extendedMag_weapon_blacklist = new List<string>();

                [JsonProperty("Blacklist of ammo item shortnames that cannot benefit from the Extended Mag buff")]
                public List<string> extendedMag_ammotype_blacklist = new List<string>();
            }

            [JsonProperty("Effect settings")]
            public EffectSettings effect_settings = new EffectSettings();

            public class EffectSettings
            {
                [JsonProperty("Instant repair effect")]
                public string repair_effect = "assets/bundled/prefabs/fx/build/repair_full_metal.prefab";

                [JsonProperty("Level up effect")]
                public string level_effect = "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab";

                [JsonProperty("Node unlock effect")]
                public string skill_point_unlock_effect = "assets/prefabs/misc/halloween/lootbag/effects/loot_bag_upgrade.prefab";

                [JsonProperty("Node level effect")]
                public string skill_point_level_effect = "assets/prefabs/misc/easter/painted eggs/effects/eggpickup.prefab";

                [JsonProperty("Lock pick fail attempt")]
                public string lockpick_fail_effect = "assets/prefabs/deployable/bear trap/effects/bear-trap-deploy.prefab";

                [JsonProperty("Lock pick success attempt")]
                public string lockpick_success_effect = "assets/prefabs/deployable/locker/effects/locker-deploy.prefab";
            }

            [JsonProperty("Better Chat settings")]
            public BetterChatSettings betterchat_settings = new BetterChatSettings();

            public class BetterChatSettings
            {
                [JsonProperty("Format for BetterChat title showing the playeres level. Set to null to disable. {0} is the colour value and {1} is the player level value")]
                public string better_title_format = "<color=#{0}>[Lv.{1}]</color>";

                [JsonProperty("Default colour for BetterChat xp titles")]
                public string better_title_default_col = "0cb072";

                [JsonProperty("Colour for BetterChat xp titles for players who are max level")]
                public string better_title_max_col = "32ff00";
            }

            [JsonProperty("Loot settings")]
            public LootTables loot_settings = new LootTables();

            public class LootTables
            {
                [JsonProperty("Mining luck loot table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<LootItems> mining_loot_table = new List<LootItems>();

                [JsonProperty("Woodcutting luck loot table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<LootItems> wc_loot_table = new List<LootItems>();

                [JsonProperty("Skinning luck loot table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<LootItems> skinning_loot_table = new List<LootItems>();

                [JsonProperty("Fishing luck loot table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<LootItems> fishing_loot_table = new List<LootItems>();

                [JsonProperty("Whitelist of loot crates to trigger the spawn chance for components, electronics and scrap. Set to null to not use the whitelist.", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> loot_crate_whitelist = new List<string>();
            }

            [JsonProperty("Ultimate settings")]
            public UltimateSettings ultimate_settings = new UltimateSettings();

            public class UltimateSettings
            {
                [JsonProperty("Background colour for the ultimate node")]
                public string ultimate_node_background_col = "1 0.8741453 0 1";

                [JsonProperty("Ultimate settings for woodcutting")]
                public WoodcuttingUltimate ultimate_woodcutting = new WoodcuttingUltimate();

                [JsonProperty("Ultimate settings for mining")]
                public MiningUltimate ultimate_mining = new MiningUltimate();

                [JsonProperty("Ultimate settings for vehicle")]
                public VehicleUltimate ultimate_vehicle = new VehicleUltimate();

                [JsonProperty("Ultimate settings for medical")]
                public MedicalUltimate ultimate_medical = new MedicalUltimate();

                [JsonProperty("Ultimate settings for harvesting")]
                public HarvesterUltimate ultimate_harvesting = new HarvesterUltimate();

                [JsonProperty("Ultimate settings for scavenger")]
                public Scav_Ultimate ultimate_scavenger = new Scav_Ultimate();

                [JsonProperty("Ultimate settings for combat")]
                public CombatUltimate ultimate_combat = new CombatUltimate();

                [JsonProperty("Ultimate settings for skinning")]
                public SkinningUltimate ultimate_skinning = new SkinningUltimate();

                [JsonProperty("Ultimate settings for build craft")]
                public BuildCraftUltimate ultimate_buildCraft = new BuildCraftUltimate();

                [JsonProperty("Ultimate settings for raiding")]
                public RaidingUltimate ultimate_raiding = new RaidingUltimate();

                [JsonProperty("Ultimate settings for cooking")]
                public CookingUltimate ultimate_cooking = new CookingUltimate();
            }

            [JsonProperty("Misc settings")]
            public MiscSettings misc_settings = new MiscSettings();

            public class MiscSettings
            {
                [JsonProperty("BotRespawn settings")]
                public BotRespawnSettings botRespawnSettings = new BotRespawnSettings();
                public class BotRespawnSettings
                {
                    [JsonProperty("Enable botrespawn profile tracking and xp")]
                    public bool enabled = true;

                    [JsonProperty("BotReSpawn profile and xp list", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public Dictionary<string, double> botrespawn_profiles = new Dictionary<string, double>();
                }

                [JsonProperty("Human NPC's name that can be used to open the skill tree")]
                public string npc_name = "";

                [JsonProperty("Should we add a button to the pump bar to open SkillTree?")]
                public bool button_to_pump_bar = true;

                [JsonProperty("Should SkillTree call out to other plugins before it starts modifying the value of the items? [required for plugins that need pre-modified item amounts]")]
                public bool call_HandleDispenser = false;

                [JsonProperty("Log all xp that players gain [Will create a very large log file]?")]
                public bool log_player_xp_gain = false;

                [JsonProperty("Allow the rationer perk to work with cooking meals?")]
                public bool ration_cooking_meals = true;

                [JsonProperty("Steam64 ID to use when sending a message to a player [0 is the default rust icon]")]
                public ulong ChatID = 76561199514393612;

                [JsonProperty("Anchor points for time left UI")]
                public AnchorSettings timeLeft_anchor = new AnchorSettings("0.5 0", "0.5 0", "-69.664 87.1", "70.336 103.1");

                [JsonProperty("Update skin IDs from the default config if they are set to 0?")]
                public bool update_skinIDs_from_default = false;
            }

            [JsonProperty("NpcSpawn (by KpucTaji) settings")]
            public BetterNPC betternpc_settings = new BetterNPC();

            public class BetterNPC
            {
                [JsonProperty("Give xp based on the name of a NpcSpawn, rather than the scientist type?")]
                public bool betternpc_give_xp = true;

                [JsonProperty("Dictionary of NPC names and the value that they provide")]
                public Dictionary<string, double> NPC_xp_table = new Dictionary<string, double>(StringComparer.InvariantCultureIgnoreCase);
            }

            [JsonProperty("Skill tree", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, Configuration.TreeInfo> trees = new Dictionary<string, Configuration.TreeInfo>();

            [JsonProperty("Leveling information. Y value must be set to 2 or 3")]
            public ExperienceInfo level = new ExperienceInfo();

            [JsonProperty("Notification settings")]
            public NotificationSettings notification_settings = new NotificationSettings();

            [JsonProperty("Third-party plugin settings")]
            public ThirdPartyPluginSettings thirdPartyPluginSettings = new ThirdPartyPluginSettings();
            public class ThirdPartyPluginSettings
            {
                [JsonProperty("SurvivalArena Settings")]
                public SurvivalArenaSettings survivalArenaSettings = new SurvivalArenaSettings();
                public class SurvivalArenaSettings
                {
                    [JsonProperty("Disable the skinning ultimate buff when the player joins the game?")]
                    public bool disable_skinning_ultimate_buff_on_join = true;
                }

                [JsonProperty("Paintball Settings")]
                public PaintballSettings paintballSettings = new PaintballSettings();
                public class PaintballSettings
                {
                    [JsonProperty("Disable the skinning ultimate buff when the player joins the game?")]
                    public bool disable_skinning_ultimate_buff_on_join = true;
                }
            }

            public class ExtraPocketsButtonAnchors
            {
                public string x_min = "185.4";
                public string y_min = "25.4";
                public string x_max = "227.4";
                public string y_max = "67.4";
            }

            public class HorseStats
            {
                public bool Increase_Horse_RunSpeed = true;
                public bool Increase_Horse_MaxSpeed = true;
                public bool Increase_Horse_TrotSpeed = false;
                public bool Increase_Horse_TurnSpeed = false;
                public bool Increase_Horse_WalkSpeed = false;
            }

            public class NotificationSettings
            {
                [JsonProperty("Settings for Notify plugin")]
                public NotifySettings notifySettings = new NotifySettings();

                [JsonProperty("Settings for Discord plugin")]
                public DiscordSettings discordSettings = new DiscordSettings();
            }

            public class DiscordSettings
            {
                [JsonProperty("Webhook URL")]
                public string webhook;

                [JsonProperty("Send a discord notification when a player levels up?")]
                public bool send_level_up = true;
            }

            public class NotifySettings
            {
                [JsonProperty("Language key and message type to display when a player gains a level")]
                public KeyValuePair<string, int> level_up_notification = new KeyValuePair<string, int>("NotifyLevelGained", 0);

            }

            public class CustomCurrency
            {
                public string displayName = "";
                public string shortname = "";
                public ulong skin = 0;
            }

            public class TreeInfo
            {
                public bool enabled = true;
                [JsonProperty("Minimum level to unlock")]
                public int min_level = 0;
                [JsonProperty("Minimum points to unlock")]
                public int min_points = 0;
                [JsonProperty("Points required to unlock tier 2 nodes")]
                public int level_2_point_requirement = 5;
                [JsonProperty("Points required to unlock tier 3 nodes")]
                public int level_3_point_requirement = 10;
                [JsonProperty("Points required to unlock the ultimate")]
                public int level_4_point_requirement = 25;
                public Dictionary<string, NodeInfo> nodes = new Dictionary<string, NodeInfo>();
                public TreeInfo(Dictionary<string, NodeInfo> nodes, bool enabled = true)
                {
                    this.nodes = nodes;
                    this.enabled = enabled;
                }
                public class NodeInfo
                {
                    [JsonProperty("Permission required to show this node")]
                    public string required_permission;
                    public bool enabled;
                    public int max_level;
                    public int tier;
                    public float value_per_buff;
                    public KeyValuePair<Buff, BuffType> buff_info;
                    public string icon_url;
                    public ulong skin;
                    public Permissions permissions;
                    public NodeInfo(bool enabled, int max_level, int tier, float value_per_buff, KeyValuePair<Buff, BuffType> buff_info, string icon_url, ulong skin, Permissions permissions = null)
                    {
                        this.enabled = enabled;
                        this.max_level = max_level;
                        this.tier = tier;
                        this.value_per_buff = value_per_buff;
                        this.buff_info = buff_info;
                        this.icon_url = icon_url;
                        this.permissions = permissions;
                        this.skin = skin;
                    }
                }
            }

            public class PowerTools
            {
                [JsonProperty("Yield modifier when using a jackhammer to mine")]
                public float mining_yield_modifier;

                [JsonProperty("Yield modifier when using a chainsaw to chop wood")]
                public float woodcutting_yield_modifier;

                [JsonProperty("Yield modifier when using a power tool to skin")]
                public float skinning_yield_modifier;

                [JsonProperty("XP modifier when using a jackhammer to mine")]
                public float mining_xp_modifier;

                [JsonProperty("XP modifier when using a chainsaw to chop wood")]
                public float woodcutting_xp_modifier;

                [JsonProperty("XP modifier when using a power tool to skin")]
                public float skinning_xp_modifier;

                [JsonProperty("Luck modifier when using a jackhammer to mine")]
                public float mining_luck_modifier;

                [JsonProperty("Luck modifier when using a chainsaw to chop wood")]
                public float woodcutting_luck_modifier;

                [JsonProperty("Luck modifier when using a power tool to skin")]
                public float skinning_luck_modifier;
            }

            public class ExperienceInfo
            {
                public double x = 0.07;
                public int y = 2;
                public Dictionary<int, double> xp_table = new Dictionary<int, double>();

                //XP - (Level / X) ^ 2
                //Level = X * SQR-Y

                public void CalculateTable(int max_level)
                {
                    for (int i = 0; i <= max_level; i++)
                    {
                        if (xp_table.ContainsKey(i))
                        {
                            var newValue = Math.Floor(Math.Pow(i / x, y));
                            if (xp_table[i] != newValue)
                            {
                                xp_table[i] = newValue;
                                UpdatedTable = true;
                            }
                        }

                        else xp_table.Add(i, Math.Floor(Math.Pow(i / x, y)));
                    }
                }
                public int GetLevel(double xp)
                {
                    int highest = 0;
                    for (int i = 0; i < 9999; i++)
                    {
                        if (xp > Math.Floor(Math.Pow(i / x, y))) highest = i;
                        else return highest;
                    }

                    return 9999;
                    //foreach (var level in xp_table)
                    //{
                    //    if (xp > level.Value) highest = level.Key;
                    //    else
                    //    {
                    //        Interface.Oxide.LogInfo($"Highest: {highest}");
                    //        return highest;
                    //    }
                    //}

                    //Interface.Oxide.LogInfo($"Level = X[{x}] * SqrtXP[{Math.Sqrt(xp)}] (xp: {xp}): {x * Math.Sqrt(xp)}");
                    //Interface.Oxide.LogInfo($"Convert.ToInt32(Math.Floor(x * Math.Sqrt(xp))): {Convert.ToInt32(Math.Floor(x * Math.Sqrt(xp)))}");
                    //return Convert.ToInt32(Math.Floor(x * Math.Sqrt(xp)));
                }

                public double GetLevelStartXP(int level)
                {
                    return Math.Pow(level / x, y);
                }

                public static bool UpdatedTable = false;
            }

            public class XPSources
            {
                public double NodeHit = 12;
                public double NodeHitFinal = 50;
                public double TreeHit = 8;
                public double TreeHitFinal = 40;
                public double SkinHit = 10;
                public double SkinHitFinal = 50;
                public double CollectWildPlant = 30;
                public double CollectGrownPlant = 5;
                public double BuildingBlockDeployed = 0;
                public double FishCaught = 100;
                public double Crafting = 0.25;
                public double ScientistNormal = 150;
                public double TunnelDweller = 125;
                public double UnderwaterDweller = 125;
                public double ScientistHeavy = 300;
                public double SmallAnimal = 20;
                public double MediumAnimal = 50;
                public double LargeAnimal = 100;
                public double RoadSign = 10;
                public double Barrel = 20;
                public double Scarecrow = 100;
                public double Mission = 1000;
                public double BradleyAPC = 1000;
                public double LootHackedCrate = 200;
                public double LootHeliCrate = 250;
                public double LootBradleyCrate = 50;
                public double CookingMealXP = 10;
                public double RaidableBaseCompletion_Easy = 100;
                public double RaidableBaseCompletion_Medium = 200;
                public double RaidableBaseCompletion_Hard = 300;
                public double RaidableBaseCompletion_Expert = 400;
                public double RaidableBaseCompletion_Nightmare = 500;
                public double Win_HungerGames = 2000;
                public double Win_ScubaArena = 2000;
                public double Win_Skirmish = 2000;
                public double Gut_Fish = 10;
                public double default_botrespawn = 100;
                public double crate_basic = 0;
                public double crate_elite = 50;
                public double crate_mine = 0;
                public double crate_normal = 0;
                public double crate_normal_2 = 0;
                public double crate_normal_2_food = 0;
                public double crate_normal_2_medical = 0;
                public double crate_tools = 0;
                public double crate_underwater_advanced = 100;
                public double crate_underwater_basic = 25;
                public double crate_ammunition = 0;
                public double crate_food_1 = 0;
                public double crate_food_2 = 0;
                public double crate_fuel = 0;
                public double crate_medical = 0;
                public double supply_drop = 500;
                public double Harbor_Event_Winner = 2000f;
                public double Junkyard_Event_Winner = 2000f;
                public double PowerPlant_Event_Winner = 2000f;
                public double Satellite_Event_Winner = 2000f;
                public double Water_Event_Winner = 2000f;
                public double Air_Event_Winner = 2000f;
                public double Armored_Train_Winner = 2000f;
                public double Convoy_Winner = 2000f;
                public double SurvivalArena_Winner = 2000f;
                public double swipe_card_level_1 = 50;
                public double swipe_card_level_2 = 100;
                public double swipe_card_level_3 = 250;
                public double boss_monster = 1000;
                public double Zombie = 100;
                public double Raider = 100;
                public double JetPilot = 100;
                public double ArcticBaseEvent_Winner = 2000f;
                public double GasStationEvent_Winner = 2000f;
                public double SputnikEvent_Winner = 2000f;
                public double ShipWreckEvent_Winner = 2000f;
            }

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class PermOverride
        {
            [JsonProperty("Dictionary of trees and their override values [case sensitive]")]
            public Dictionary<string, int> treeRequirementOverride = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public PermOverride(Dictionary<string, int> treeRequirementOverride)
            {
                this.treeRequirementOverride = treeRequirementOverride;
            }
        }

        public class Permissions
        {
            public string description;
            public Dictionary<int, PermissionInfo> perms = new Dictionary<int, PermissionInfo>();

            public Permissions(string description, Dictionary<int, PermissionInfo> perms)
            {
                this.description = description;
                this.perms = perms;
            }
        }

        public class PermissionInfo
        {
            public Dictionary<string, string> perms_list = new Dictionary<string, string>();

            // Key = perm. Value = displayName.
            public PermissionInfo(Dictionary<string, string> perms_to_add)
            {
                this.perms_list = perms_to_add;
            }
        }

        public class LootItems
        {
            public string shortname;
            public int min;
            public int max;
            public int dropWeight;
            public string displayName;
            public ulong skin;
            public LootItems(string shortname, int min, int max, ulong skin = 0, string displayName = null, int dropWeight = 100)
            {
                this.shortname = shortname;
                this.min = min;
                this.max = max;
                this.skin = skin;
                this.displayName = displayName;
                this.dropWeight = dropWeight;
            }
        }

        public class PumpBar
        {
            [JsonProperty("Enable the xp bar?")]
            public bool enabled = true;

            [JsonProperty("Default xp bar offset - this is for any new player connecting to the server")]
            public xp_bar_offset offset_default = new xp_bar_offset()
            {
                min_x = -365.713f,
                min_y = 20f,
                max_x = -233.287f,
                max_y = 42f
            };

            [JsonProperty("Anchor points")]
            public XP_Bar_Anchors anchor_default = new XP_Bar_Anchors()
            {
                anchor_min = "1 0",
                anchor_max = "1 0"
            };

            [JsonProperty("Colour of the pump bar")]
            public string pump_bar_colour = "0.5471698 0.3533202 0 0.6078432";

            [JsonProperty("Colour of the pump bar when in xp debt")]
            public string pump_bar_colour_debt = "0.8 0 0 0.6078432";

            [JsonProperty("Font set for the pump bar")]
            public string pump_bar_font = "robotocondensed-regular.ttf";

            [JsonProperty("Font size for the pump bar")]
            public int pump_bar_font_size = 10;

            [JsonProperty("Pump bar formatting [1= (CurrentXP)/(TotalXP)] [2= (LevelCurrentXP)/(LevelTotalXP)] [3= (XPLeft) (%)]")]
            public int pump_bar_formatting = 2;
        }

        public class RaidingUltimate
        {
            [JsonProperty("Command to call in MLRS strike")]
            public string command = "strike";

            [JsonProperty("Maximum duration that the Raiding Ultimate can be active for [seconds]")]
            public float max_duration = 30;

            [JsonProperty("Cooldown between uses [minutes]")]
            public float cooldown = 360;

            [JsonProperty("How long between each update tick when targeting [seconds]")]
            public float tick_interval = 1f;

            [JsonProperty("How many ticks are required for the targeting to be successful")]
            public int ticks_required = 5;

            [JsonProperty("How many rockets should be fired when the ultimate is successful")]
            public int missile_amount = 6;

            [JsonProperty("Effect to run when the target is successfully acquired [leave blank for no effect]")]
            public string missile_fire_confirmation_effect = "assets/prefabs/building/wall.frame.shopfront/effects/metal_transaction_complete.prefab";

            [JsonProperty("Delay between each rocket when the barrage starts")]
            public float delay_between_rockets = 0.5f;

            [JsonProperty("Allow the Double_Explosion_chance buff to trigger with this ultimate?")]
            public bool allow_doubling = false;

            [JsonProperty("Reset the MLRS strike cool down on respec?")]
            public bool reset_strike_cooldown_on_respec = false;

            [JsonProperty("Only allow the ultimate to work in RaidableBase Zones?")]
            public bool raidable_bases_only = false;

            [JsonProperty("Prevent multiple players using the MLRS ability at the same location?")]
            public bool prevent_mlrs_spamming = true;

            [JsonProperty("Radius from the strike centre point that will disallow additional strikes")]
            public float prevention_radius = 60f;

            [JsonProperty("Time that strikes will be prevented in that location [seconds]")]
            public float prevention_duration = 1800;

            [JsonProperty("Require the player to have MLRS rockets in their inventory in order to use the ultimate?")]
            public bool require_ammo = false;

            [JsonProperty("Show time remaining to launch a strike")]
            public bool show_time_remaining = true;

            [JsonProperty("How long after a server is wiped should we prevent the MLRS ultimate from being used? [Hours]")]
            public float wipe_prevention_time = 2;
        }

        public class BuildCraftUltimate
        {
            [JsonProperty("Chance that a card with lower access will successfully unlock the door (the card is damaged regardless) [%]")]
            public int success_chance = 100;

            [JsonProperty("Notify the player when their ultimate failed")]
            public bool notify_fail = true;
        }

        public class SkinningUltimate
        {
            [JsonProperty("How long should each buff last for [0 = off]?")]
            public Dictionary<AnimalBuff, float> enabled_buffs = new Dictionary<AnimalBuff, float>();

            [JsonProperty("Wolf perk: what health scale bonus should the player receive per team member near by [1.0 is 100%]?")]
            public float wolf_health_scale = 0.25f;

            [JsonProperty("Wolf perk: How close do teammates need to be in order to contribute to the perk [radius]?")]
            public float wolf_team_dist = 30f;

            [JsonProperty("Bear perk: Maximum health of the overshield")]
            public float bear_overshield_max = 50f;

            [JsonProperty("Stag perk: Maximum distance that the perk can detect dangerous entities from [radius]")]
            public float stag_danger_dist = 30f;

            [JsonProperty("Stag perk: Time between procs [seconds]")]
            public float stag_timer = 10f;

            [JsonProperty("Stag perk: Draw the enemy location?")]
            public bool stag_draw_enemy = true;

            [JsonProperty("Boar perk: Blacklist of components for the boar buff")]
            public List<string> boar_blackList = new List<string>();

            [JsonProperty("Boar perk: Chance when collecting mushrooms and berries that a player [%]")]
            public float boar_chance = 2f;

            [JsonProperty("Boar perk: Minimum quantity to give")]
            public int boar_min_quantity = 1;

            [JsonProperty("Boar perk: Maximum quantity to give")]
            public int boar_max_quantity = 4;

            [JsonProperty("Anchor points for the stag danger UI icon")]
            public AnchorSettings stag_danger_icon_anchor = new AnchorSettings("0.5 0", "0.5 0", "-201.4 87.6", "33.4 107.6");

            [JsonProperty("Anchor points for the bear overshield UI")]
            public AnchorSettings overshield_anchor = new AnchorSettings("0.5 0", "0.5 0", "-201.4 87.6", "-97.4 107.6");
        }

        public class AnchorSettings
        {
            public string anchorMin;
            public string anchorMax;
            public string offsetMin;
            public string offsetMax;
            public AnchorSettings(string anchorMin, string anchorMax, string offsetMin, string offsetMax)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.offsetMin = offsetMin;
                this.offsetMax = offsetMax;
            }
        }

        public class CookingUltimate
        {
            [JsonProperty("Command to activate the buff")]
            public string command = "teatime";

            [JsonProperty("Cooldown between uses [minutes]")]
            public float buff_cooldown = 240;

            [JsonProperty("Apply modifiers even if the player has an existing buff of the same type with a higher modifier?")]
            public bool override_better_mod = false;

            [JsonProperty("Apply modifiers even if the player has an existing buff of the same type with a longer duration?")]
            public bool override_better_duration = true;

            [JsonProperty("Modifiers that the player will receive when the buff is used")]
            public Dictionary<Modifier.ModifierType, ModifierValues> tea_mods = new Dictionary<Modifier.ModifierType, ModifierValues>();

            public class ModifierValues
            {
                [JsonProperty("Duration [seconds]")]
                public float duration;
                [JsonProperty("Modifier [1.0 = 100%]")]
                public float modifier;

                public ModifierValues(float duration, float modifier)
                {
                    this.duration = duration;
                    this.modifier = modifier;
                }
            }
        }

        public class CombatUltimate
        {
            [JsonProperty("What scale of damage should the player receive as health [1.0 = 100%]")]
            public float health_scale = 0.01f;

            [JsonProperty("Should the healing effect of the ultimate work against players")]
            public bool players_enabled = true;

            [JsonProperty("Should the healing effect of the ultimate work against animals")]
            public bool animals_enabled = true;

            [JsonProperty("Should the healing effect of the ultimate work against scientists")]
            public bool scientists_enabled = true;

            [JsonProperty("List of weapon prefabs (not items) that will not trigger the heal")]
            public List<string> weapon_blacklist = new List<string>();

            [JsonProperty("Allow fire based damage to heal players?")]
            public bool heal_from_fire_damage = false;
        }

        public class Scav_Ultimate
        {
            [JsonProperty("List of items that you dont want the perk to recycle")]
            public List<string> item_blacklist = new List<string>();

            [JsonProperty("Scrap items that have a unique name?")]
            public bool scrap_named_items = false;

            [JsonProperty("Scrap items that have a non-default skin?")]
            public bool scrap_skinned_items = false;
        }

        public class HarvesterUltimate
        {
            [JsonProperty("Chat command that players can use to set their plant genes")]
            public string gene_chat_command = "setgenes";

            [JsonProperty("Cooldown between ultimate triggers. Set to 0 if you want all plants to have their genes adjusted [seconds]")]
            public float cooldown = 0f;

            [JsonProperty("Notify a player in chat when they go on cooldown (recommended for longer cooldowns)")]
            public bool notify_on_cooldown = true;
        }

        public class MedicalUltimate
        {
            [JsonProperty("Chance for the player to resurrect [out of 100]")]
            public float resurrection_chance = 50f;

            [JsonProperty("Delay between resurrections after a successful resurrection [seconds]")]
            public float resurrection_delay = 1200f;

            [JsonProperty("Prevent the button being sent when a player kills themselves or is the aggressor of their own death?")]
            public bool prevent_on_suicide = true;
        }

        public class VehicleUltimate
        {
            [JsonProperty("Reduction percentage [1.0 == full reduction]")]
            public float reduce_by = 1f;
        }

        public class WoodcuttingUltimate
        {
            [JsonProperty("Award xp for each tree that the perk cuts down?")]
            public bool award_xp = false;

            [JsonProperty("Distance from the player (radius) that trees will be cut down?")]
            public float distance_from_player = 10f;
        }

        public class MiningUltimate
        {
            [JsonProperty("Distance from the player (radius) that nodes will appear (radius)")]
            public float distance_from_player = 200f;

            [JsonProperty("Cooldown time on the ability (seconds)")]
            public float cooldown = 60f;

            [JsonProperty("How many seconds should the marked ores appear on the players hud?")]
            public float hud_time = 60f;

            [JsonProperty("Text size")]
            public int text_size = 12;

            [JsonProperty("Chat command to find the nodes.")]
            public string find_node_cmd = "locatenodes";

            [JsonProperty("Automatically trigger the mining ultimate when the player equips a pickaxe (still abides by the cooldown time)")]
            public bool trigger_on_item_change = false;

            [JsonProperty("UI Colour for sulfur nodes [0:red, 1:green, 2:blue, 3:white, 4:black, 5:yellow, 6:cyan, 7:magenta]")]
            public int sulfur_colour = 5;

            [JsonProperty("UI Colour for metal nodes [0:red, 1:green, 2:blue, 3:white, 4:black, 5:yellow, 6:cyan, 7:magenta]")]
            public int metal_colour = 0;

            [JsonProperty("UI Colour for stone nodes [0:red, 1:green, 2:blue, 3:white, 4:black, 5:yellow, 6:cyan, 7:magenta]")]
            public int stone_colour = 6;

            [JsonProperty("Show the player the distance of the node?")]
            public bool show_distance = true;

            [JsonProperty("List of tools to trigger the ultimate")]
            public List<string> tools_list = new List<string>();
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            config.level.CalculateTable(config.general_settings.max_player_level > 0 ? config.general_settings.max_player_level : 100);
            config.trees = DefaultTrees;

            config.general_settings.level_rewards.Add(100, new LevelReward(new Dictionary<string, string>() { ["say <color=#ffae00>{name}</color> reached level <color=#4cff03>100</color>!"] = "You have reached a milestone level!" }, new List<string>() { "say Test data reset." }));

            config.tools_black_white_list_settings.wc_tools = new List<string>()
            {
                "hatchet", "axe.salvaged", "stonehatchet", "chainsaw"
            };

            config.ultimate_settings.ultimate_mining.tools_list = DefaultUltimateToolsList;

            config.tools_black_white_list_settings.mining_tools = new List<string>()
            {
                "pickaxe", "stone.pickaxe", "icepick.salvaged", "jackhammer"
            };

            config.tools_black_white_list_settings.skinning_tools = new List<string>()
            {
                "knife.bone", "knife.butcher", "knife.combat", "hatchet"
            };

            config.loot_settings.mining_loot_table = DefaultLootItems;

            config.loot_settings.wc_loot_table = DefaultLootItems;

            config.loot_settings.skinning_loot_table = DefaultLootItems;

            config.loot_settings.fishing_loot_table = DefaultLootItems;

            config.tools_black_white_list_settings.comp_blacklist = new List<string>() { "generic", "chassis", "glue", "bleach", "ducttape", "sticks", "vehicle.chassis", "vehicle.module", "vehicle.chassis.4mod", "vehicle.chassis.3mod", "vehicle.chassis.2mod", "electric.generator.small" };

            config.buff_settings.primitive_weapons = new List<string>() { "spear.stone", "spear.wooden", "bone.club", "bow.hunting" };

            config.buff_settings.animals = new List<string>() { "boar", "horse", "stag", "chicken", "wolf", "bear", "scarecrow", "polarbear" };

            config.loot_settings.loot_crate_whitelist = new List<string>()
            {
                "assets/bundled/prefabs/radtown/crate_elite.prefab",
                "assets/bundled/prefabs/radtown/crate_basic.prefab",
                "assets/bundled/prefabs/radtown/crate_normal.prefab",
                "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                "assets/bundled/prefabs/radtown/crate_tools.prefab",
                "assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_tools.prefab",
                "assets/bundled/prefabs/radtown/crate_normal_2_food.prefab",
                "assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_fuel.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab",
                "assets/bundled/prefabs/radtown/underwater_labs/vehicle_parts.prefab"
            };

            config.tools_black_white_list_settings.black_listed_gather_items = new List<string>() { "bone.club" };

            config.general_settings.respec_cost_override.Add("vip", Math.Round(config.general_settings.respec_cost / 2, 0));
            config.general_settings.max_skill_points_override.Add("vip", config.general_settings.max_skill_points + (Convert.ToInt32(config.general_settings.max_skill_points * 0.2)));
            config.general_settings.max_skill_points_override.Add("nolimit", 0);
            config.xp_settings.xp_loss_settings.xp_loss_override.Add("vip", 0.5);
            config.xp_settings.xp_perm_modifier.Add("vip", 1.0);

            config.buff_settings.no_refund_item_skins = new List<ulong>() { 2529344523, 2546992444, 2546992685 };
            config.ultimate_settings.ultimate_scavenger.item_blacklist = DefaultScavengerUltimateBlacklist;
            config.ultimate_settings.ultimate_skinning.enabled_buffs = DefaultAnimalBuffs;

            config.xp_settings.cooking_black_list = new List<string>() { "ingredient bag" };

            config.buff_settings.durability_blacklist = DefaultDurabilityBlacklist;

            config.buff_settings.tea_looter_settings.TeaDropTable = DefaultTeaWeights;
            config.buff_settings.tea_looter_settings.containers = DefaultTeaContainers;
            config.buff_settings.forager_settings.displayColours = DefaultForagerColours;
            config.ultimate_settings.ultimate_cooking.tea_mods = DefaultCookingUltimateMods;

            config.buff_settings.raid_perk_settings.Trap_Spotter_settings.trap_colours = DefaultSpotterCols;
        }

        Dictionary<Modifier.ModifierType, CookingUltimate.ModifierValues> DefaultCookingUltimateMods
        {
            get
            {
                return new Dictionary<Modifier.ModifierType, CookingUltimate.ModifierValues>()
                {
                    [Modifier.ModifierType.Max_Health] = new CookingUltimate.ModifierValues(3600, 0.2f),
                    [Modifier.ModifierType.Ore_Yield] = new CookingUltimate.ModifierValues(3600, 0.5f),
                    [Modifier.ModifierType.Radiation_Exposure_Resistance] = new CookingUltimate.ModifierValues(3600, 0.5f),
                    [Modifier.ModifierType.Radiation_Resistance] = new CookingUltimate.ModifierValues(3600, 0.5f),
                    [Modifier.ModifierType.Scrap_Yield] = new CookingUltimate.ModifierValues(3600, 4),
                    [Modifier.ModifierType.Wood_Yield] = new CookingUltimate.ModifierValues(3600, 2)
                };
            }
        }

        [ConsoleCommand("addleveloverride")]
        void AddLevelOverride(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (!config.general_settings.level_requirement_override.IsNullOrEmpty())
            {
                arg.ReplyWith("There is already data in the Level requirement override field.");
                return;
            }

            config.general_settings.level_requirement_override = DefaultLevelPermOverride;
            SaveConfig();
            arg.ReplyWith("Added entry for level requirement override to the config.");
        }

        Dictionary<string, PermOverride> DefaultLevelPermOverride
        {
            get
            {
                return new Dictionary<string, PermOverride>()
                {
                    ["skilltree.minleveloverride"] = new PermOverride(new Dictionary<string, int>()
                    {
                        ["Raiding"] = 10
                    })
                };
            }
        }

        [ConsoleCommand("addpointoverride")]
        void AddPointOverride(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (!config.general_settings.point_requirement_override.IsNullOrEmpty())
            {
                arg.ReplyWith("There is already data in the Level requirement override field.");
                return;
            }

            config.general_settings.point_requirement_override = DefaultPointPermOverride;
            SaveConfig();
            arg.ReplyWith("Added entry for point requirement override to the config.");
        }

        Dictionary<string, PermOverride> DefaultPointPermOverride
        {
            get
            {
                return new Dictionary<string, PermOverride>()
                {
                    ["skilltree.minpointsoverride"] = new PermOverride(new Dictionary<string, int>()
                    {
                        ["Raiding"] = 10
                    })
                };
            }
        }

        Dictionary<string, float[]> DefaultForagerColours
        {
            get
            {
                return new Dictionary<string, float[]>()
                {
                    ["hemp-collectable"] = new float[3] { 0.0965f, 0.550f, 0.0165f },
                    ["corn-collectable"] = new float[3] { 0.906f, 0.920f, 0.101f },
                    ["pumpkin-collectable"] = new float[3] { 0.840f, 0.547f, 0.0420f },
                    ["potato-collectable"] = new float[3] { 0.360f, 0.245f, 0.0468f },
                    ["berry-blue-collectable"] = new float[3] { 0.0670f, 0.600f, 0.670f },
                    ["berry-green-collectable"] = new float[3] { 0.0315f, 0.450f, 0.0385f },
                    ["berry-yellow-collectable"] = new float[3] { 0.710f, 0.699f, 0.0355f },
                    ["berry-red-collectable"] = new float[3] { 0.720f, 0.0360f, 0.253f },
                    ["berry-white-collectable"] = new float[3] { 1f, 1f, 1f },
                    ["diesel_collectable"] = new float[3] { 0.940f, 0.681f, 0.423f },
                    ["mushroom-cluster-5"] = new float[3] { 0.530f, 0.265f, 0.349f },
                    ["mushroom-cluster-6"] = new float[3] { 0.530f, 0.265f, 0.349f },
                    ["sulfur-collectable"] = new float[3] { 0.630f, 0.515f, 0.101f },
                    ["metal-collectable"] = new float[3] { 0.320f, 0.314f, 0.294f },
                    ["stone-collectable"] = new float[3] { 0.440f, 0.435f, 0.418f },
                    ["wood-collectable"] = new float[3] { 0.250f, 0.225f, 0.135f },
                };
            }
        }

        Dictionary<string, int> DefaultTeaWeights
        {
            get
            {
                return new Dictionary<string, int>()
                {
                    ["radiationresisttea"] = 100,
                    ["radiationresisttea.advanced"] = 100,
                    ["radiationresisttea.pure"] = 100,
                    ["healingtea"] = 100,
                    ["healingtea.advanced"] = 100,
                    ["healingtea.pure"] = 100,
                    ["maxhealthtea"] = 100,
                    ["maxhealthtea.advanced"] = 100,
                    ["maxhealthtea.pure"] = 100,
                    ["oretea"] = 100,
                    ["oretea.advanced"] = 100,
                    ["oretea.pure"] = 100,
                    ["radiationremovetea"] = 100,
                    ["radiationremovetea.advanced"] = 100,
                    ["radiationremovetea.pure"] = 100,
                    ["scraptea"] = 100,
                    ["scraptea.advanced"] = 100,
                    ["scraptea.pure"] = 100,
                    ["woodtea"] = 100,
                    ["woodtea.advanced"] = 100,
                    ["woodtea.pure"] = 100
                };
            }
        }

        List<string> DefaultTeaContainers
        {
            get
            {
                return new List<string>()
                {
                    "crate_normal_2_food",
                    "invisible_crate_normal_2_food",
                    "crate_food_1",
                    "crate_food_2",
                    "wagon_crate_normal_2_food",
                    "foodbox",
                    "invisible_foodbox",
                    "dmfood"
                };
            }
        }

        List<string> DefaultDurabilityBlacklist
        {
            get
            {
                return new List<string>()
                {
                    "keycard_blue",
                    "keycard_red",
                    "keycard_green"
                };
            }
        }

        List<string> DefaultDoubpleExplosionList
        {
            get
            {
                return new List<string>()
                {
                    "rocket_basic",
                    "40mm_grenade_he",
                    "rocket_hv",
                    "grenade.beancan.entity"
                };
            }
        }

        List<string> DefaultUltimateToolsList
        {
            get
            {
                return new List<string>()
                {
                    "pickaxe", "stone.pickaxe", "icepick.salvaged", "jackhammer"
                };
            }
        }

        List<LootItems> DefaultLootItems
        {
            get
            {
                return new List<LootItems>()
                {
                    new LootItems("keycard_blue", 1, 1),
                    new LootItems("keycard_green", 1, 1),
                    new LootItems("keycard_red", 1, 1),
                    new LootItems("lowgradefuel", 1, 10)
                };
            }
        }

        public class LevelReward
        {
            [JsonProperty("List of commands and chat messages that the player receives when reaching the specified level [Left = command. Right = Private message to player]. {id} = steam ID. {name} == name.")]
            public Dictionary<string, string> reward_commands = new Dictionary<string, string>();

            [JsonProperty("List of commands that are fired off when the player data is reset")]
            public List<string> reset_commands = new List<string>();

            public LevelReward(Dictionary<string, string> reward_commands, List<string> reset_commands = null)
            {
                this.reward_commands = reward_commands;
                this.reset_commands = reset_commands;
            }
        }

        Dictionary<string, float> DefaultUnderwaterChance
        {
            get
            {
                return new Dictionary<string, float>()
                {
                    ["assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab"] = 5f,
                    ["assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab"] = 5f,
                    ["assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab"] = 5f,
                    ["assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab"] = 5f,
                    ["assets/bundled/prefabs/radtown/crate_underwater_basic.prefab"] = 5f
                };
            }
        }

        List<LootItems> GetSharkLoot()
        {
            List<LootItems> loot = new List<LootItems>();
            foreach (var item in ItemManager.GetItemDefinitions())
                loot.Add(new LootItems(item.shortname, 1, item.isWearable ? 1 : item.isHoldable ? 1 : UnityEngine.Random.Range(2, 5)));
            return loot;
        }

        public Dictionary<string, List<LootItems>> DeepSeaLooterLootTable;
        public List<LootItems> SharkLootTable;

        Dictionary<string, List<LootItems>> GetUnderwaterLoot()
        {
            Dictionary<string, List<LootItems>> result = new Dictionary<string, List<LootItems>>();

            List<LootItems> items = new List<LootItems>();

            foreach (var item in ItemManager.GetItemDefinitions().Where(x => x.category == ItemCategory.Component))
                items.Add(new LootItems(item.shortname, 1, 3));

            result.Add("assets/bundled/prefabs/radtown/crate_underwater_basic.prefab", items);
            result.Add("assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab", items);
            result.Add("assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab", items);

            foreach (var item in ItemManager.GetItemDefinitions().Where(x => x.category == ItemCategory.Electrical || x.category == ItemCategory.Weapon || x.category == ItemCategory.Attire))
            {
                if (item.category == ItemCategory.Attire || item.category == ItemCategory.Weapon) items.Add(new LootItems(item.shortname, 1, 1));
                else items.Add(new LootItems(item.shortname, 1, 3));
            }
            result.Add("assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab", items);
            result.Add("assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab", items);

            return result;
        }

        List<string> OldLinks = new List<string>()
        {
            "https://www.dropbox.com/s/vc5ing7pbtxv6er/Paladinskill_07_nobg.png?dl=1",
            "https://imgur.com/zAkmSub.png",
            "https://www.dropbox.com/s/jkhionm6r7gfj95/Paladinskill_12_nobg.png?dl=1",
            "https://www.dropbox.com/s/k0v94pupgo0qmwy/Paladinskill_20_nobg.png?dl=1",
            "https://www.dropbox.com/s/6qp1kfvtw4ixsk7/Assassinskill_16_nobg.png?dl=1",
            "https://www.dropbox.com/s/muim3nqo63nkwqk/Paladinskill_02_nobg.png?dl=1",
            "https://www.dropbox.com/s/qsivpq886epcypo/Mageskill_18_nobg.png?dl=1",
            "https://www.dropbox.com/s/fin32eer1d88f0f/Engineerskill_32_nobg.png?dl=1",
            "https://imgur.com/hO7fA3z.png",
            "https://www.dropbox.com/s/hjq9kird3uxohlo/Archerskill_17_nobg.png?dl=1",
            "https://www.dropbox.com/s/o3zb8kfu53r4m3k/Warriorskill_50_nobg.png?dl=1",
            "https://www.dropbox.com/s/gkp6k8zwfh3rc41/Archerskill_29_nobg.png?dl=1",
            "https://www.dropbox.com/s/qybq86hkdoab14r/Assassinskill_26_nobg.png?dl=1",
            "https://www.dropbox.com/s/dnebyrhp7bugl61/Mageskill_35_nobg.png?dl=1",
            "https://www.dropbox.com/s/095ij4c20yomy8x/Mageskill_49_nobg.png?dl=1",
            "https://www.dropbox.com/s/st1blgav08kgnrb/Regrowth.png?dl=1",
            "https://www.dropbox.com/s/71bryupwygpuv9z/Druideskill_11_nobg.png?dl=1",
            "https://imgur.com/2AnzivI.png",
            "https://www.dropbox.com/s/v85ya0x8roo1q67/33_Critical_strike_nobg.png?dl=1",
            "https://imgur.com/sA5KtYp.png",
            "https://www.dropbox.com/s/k7e89kpwroskp1f/34_Critical_strike2_nobg.png?dl=1",
            "https://www.dropbox.com/s/s9lr3wl5ej5u9k7/36_Critical_strike4_nobg.png?dl=1",
            "https://www.dropbox.com/s/igcf7tqn1ezolnh/35_Critical_strike3_nobg.png?dl=1",
            "https://www.dropbox.com/s/t0ahbcc13b24yr0/Assassinskill_21_nobg.png?dl=1",
            "https://www.dropbox.com/s/1hzsha39lqkulrp/Archerskill_10_nobg.png?dl=1",
            "https://www.dropbox.com/s/lio5lk1sqn32k1a/Warriorskill_13_nobg.png?dl=1",
            "https://imgur.com/GNdYVEI.png",
            "https://imgur.com/C4mseOn.png",
            "https://imgur.com/jsO5eGQ.png",
            "https://www.dropbox.com/s/prwf810f4cqun9l/Engineerskill_33_nobg.png?dl=1",
            "https://www.dropbox.com/s/hvd4su438s54erh/Druideskill_02_nobg.png?dl=1",
            "https://imgur.com/YNkQ2mb.png",
            "https://www.dropbox.com/s/v0a5rw82gjmxi1t/Warlock_15_nobg.png?dl=1",
            "https://www.dropbox.com/s/oh6tnjojkgjehvd/Druideskill_03_nobg.png?dl=1",
            "https://www.dropbox.com/s/apydr8pj51z030x/Archerskill_09_nobg.png?dl=1",
            "https://imgur.com/fH2sIUp.png",
            "https://imgur.com/Ys5zO7o.png",
            "https://www.dropbox.com/s/h9ixd1ug2en4pn4/Paladinskill_17_nobg.png?dl=1",
            "https://imgur.com/7Ko8KLg.png",
            "https://www.dropbox.com/s/dqaxiz73p8uwf7q/Mageskill_03_nobg.png?dl=1",
            "https://www.dropbox.com/s/8e1wtvbnroayreq/Archerskill_20_nobg.png?dl=1",
            "https://imgur.com/N5ixdsD.png",
            "https://www.dropbox.com/s/xgqae0jhp1exp8w/Mageskill_38_nobg.png?dl=1",
            "https://www.dropbox.com/s/g25krvk8c5n99d9/Warriorskill_29_nobg.png?dl=1",
            "https://www.dropbox.com/s/18sptrssfukaf7s/Priestskill_48_nobg.png?dl=1",
            "https://imgur.com/yCpYOFB.png",
            "https://www.dropbox.com/s/y948cf4lfhx2aoy/Druideskill_23_nobg.png?dl=1",
            "https://imgur.com/UjF7tZp.png",
            "https://www.dropbox.com/s/596bfn1pzm1hzhn/Engineerskill_24_nobg.png?dl=1",
            "https://imgur.com/TKrf3oQ.png",
            "https://imgur.com/Kmbv9gS.png",
            "https://imgur.com/wG7cG8X.png",
            "https://imgur.com/xMp1fhd.png",
            "https://imgur.com/cVhnv41.png",
            "https://imgur.com/Utjvw8E.png",
            "https://imgur.com/LvwIvql.png",
            "https://www.dropbox.com/s/amyh43h1p1x62fr/Engineerskill_01_nobg.png?dl=1",
            "https://www.dropbox.com/s/p88fvhnvu3e7x6q/Assassinskill_35_nobg.png?dl=1",
            "https://www.dropbox.com/s/y83duc088snp1d9/Engineerskill_04_nobg.png?dl=1",
            "https://www.dropbox.com/s/dfdymr0lpf201b2/Engineerskill_17_nobg.png?dl=1",
            "https://www.dropbox.com/s/nqhoai1x52ujht3/Engineerskill_15_nobg.png?dl=1",
            "https://www.dropbox.com/s/xvsbehljd76maos/Engineerskill_09_nobg.png?dl=1",
            "https://imgur.com/q9KYN2K.png",
            "https://www.dropbox.com/s/1ca4a25o8yb28za/Archerskill_33_nobg.png?dl=1",
            "https://imgur.com/OFND5GM.png",
            "https://imgur.com/GPfIawP.png",
            "https://imgur.com/RXuJ7LI.png",
            "https://www.dropbox.com/s/8not58eqky3s7nv/Engineerskill_23_nobg.png?dl=1",
            "https://www.dropbox.com/s/1htiof75vq9wp2u/Assassinskill_44_nobg.png?dl=1",
            "https://imgur.com/c6L8ASa.png",
            "https://www.dropbox.com/s/swzyy1va1ga06ds/Assassinskill_36_nobg.png?dl=1",
            "https://www.dropbox.com/s/oq8641e4oz6sd41/Druideskill_20_nobg.png?dl=1",
            "https://www.dropbox.com/s/ahn5ji2zzffzowa/Engineerskill_21_nobg.png?dl=1",
            "https://www.dropbox.com/s/uvqzjks7ak673gy/Engineerskill_31_nobg.png?dl=1",
            "https://www.dropbox.com/s/n82viwka4nf2yop/Mageskill_16_nobg.png?dl=1",
            "https://imgur.com/RTARVWQ.png",
            "https://imgur.com/cuzgYOX.png",
            "https://www.dropbox.com/s/a83zlb8l27wvvq8/Warriorskill_49_nobg.png?dl=1",
            "https://imgur.com/AfLYdhS.png",
            "https://imgur.com/KNmzwFE.png",
            "https://imgur.com/UfnqXLe.png",
            "https://imgur.com/V8O6zv6.png",
            "https://imgur.com/Lo690dm.png",
            "https://imgur.com/KONhg85.png",
            "https://imgur.com/akFL8MA.png",
            "https://imgur.com/QbfJgP3.png",
            "https://www.dropbox.com/s/6ov6c33ntgbhqhf/Archerskill_15_nobg.png?dl=1",
            "https://imgur.com/EaABpHv.png",
            "https://imgur.com/bebpjg0.png",
            "https://imgur.com/N7qOlOC.png",
            "https://imgur.com/O0ls6gI.png",
            "https://imgur.com/YyHmSrN.png",
            "https://imgur.com/rmRaZQG.png",
            "https://imgur.com/R4Ik5me.png",
            "https://imgur.com/nT08292.png",
            "https://imgur.com/52AYAmf.png",
            "https://imgur.com/sUrtJJ6.png"
        };


        Dictionary<string, Configuration.TreeInfo> DefaultTrees
        {
            get
            {
                return new Dictionary<string, Configuration.TreeInfo>()
                {
                    ["Mining"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Amature Miner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Mining_Yield, BuffType.Percentage), "https://www.dropbox.com/s/dajctapqtv8bt4z/Amature_Miner.png?dl=1", 2873965665),
                        ["Stroke of luck"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Node_Spawn_Chance, BuffType.Percentage), "https://www.dropbox.com/s/ym4bktn12jr6t4p/Stroke_of_Luck.png?dl=1", 2873042230),
                        ["Adept Miner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.075f, new KeyValuePair<Buff, BuffType>(Buff.Mining_Yield, BuffType.Percentage), "https://www.dropbox.com/s/pon14r7e6bvn2kd/Adept_Miner.png?dl=1", 2873042840),
                        ["Instant Mining"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Instant_Mine, BuffType.Percentage), "https://www.dropbox.com/s/x3awxx2vkfun2io/Instant_Mining.png?dl=1", 2873042968),
                        ["Mining Luck"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.01f, new KeyValuePair<Buff, BuffType>(Buff.Mining_Luck, BuffType.Percentage), "https://www.dropbox.com/s/uk7oix7ojnsqyjh/Mining_Luck.png?dl=1", 2873043145),
                        ["Expert Miner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Mining_Yield, BuffType.Percentage), "https://www.dropbox.com/s/rgoj7fey7xvzrgk/Expert_Miner.png?dl=1", 2873043244),
                        ["Refiner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Smelt_On_Mine, BuffType.Percentage), "https://www.dropbox.com/s/wbsgo1egl10dwcx/Refiner.png?dl=1", 2873043347),
                        ["Robust pickaxe"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Mining_Tool_Durability, BuffType.Percentage), "https://www.dropbox.com/s/j4nnel0lzonhu9b/Robust_pickaxe.png?dl=1", 2873043495),
                        ["Stone Sense"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Mining_Ultimate, BuffType.IO), "https://www.dropbox.com/s/kkge1vpuptc37z4/Stone_Sense.png?dl=1", 2873043666),
                        ["Efficient Miner"] = new Configuration.TreeInfo.NodeInfo(true, 1, 2, 1f, new KeyValuePair<Buff, BuffType>(Buff.Mining_Hotspot, BuffType.IO), "https://www.dropbox.com/s/xgyvuu1x28u22js/Mining_Hotspot.v1.png?dl=1", 2987629553),
                    }),
                    ["Woodcutting"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {

                        ["Amature Woodcutter"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Yield, BuffType.Percentage), "https://www.dropbox.com/s/2s89vo3bmqyefux/Amature_Woodcutter.png?dl=1", 2873043851),
                        ["Adept Woodcutter"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.075f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Yield, BuffType.Percentage), "https://www.dropbox.com/s/nf9reuenek59a6y/Adept_Woodcutter.png?dl=1", 2873043965),
                        ["Instant Woodcutting"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Instant_Chop, BuffType.Percentage), "https://www.dropbox.com/s/7vut5y0vub9e05c/Instant_Woodcutting.png?dl=1", 2873044070),
                        ["Woodcutting Luck"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.01f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Luck, BuffType.Percentage), "https://www.dropbox.com/s/6acfo7hlj0sxviq/Woodcutters_Luck.png?dl=1", 2873044171),
                        ["Efficient Lumberjack"] = new Configuration.TreeInfo.NodeInfo(true, 1, 2, 1f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Hotspot, BuffType.Percentage), "https://www.dropbox.com/s/8zmb3b99qqqfc7p/Woodcutting_Hotspot.v1.png?dl=1", 2987395716),
                        ["Expert Woodcutter"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Yield, BuffType.Percentage), "https://www.dropbox.com/s/tzlbixc5ufbxqjs/Expert_Woodcutter.png?dl=1", 2873044270),
                        ["Chimney"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Coal, BuffType.Percentage), "https://www.dropbox.com/s/d8ovy2trv5tuipw/Chimney.png?dl=1", 2873044493),
                        ["Tree Regrowth"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.025f, new KeyValuePair<Buff, BuffType>(Buff.Regrowth, BuffType.Percentage), "https://www.dropbox.com/s/kbyk4nhzu7akmhb/Tree_Regrowth.png?dl=1", 2874292571),
                        ["Robust Axe"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Tool_Durability, BuffType.Percentage), "https://www.dropbox.com/s/4p5gqo7fbfaw9jz/Robust_Axe.png?dl=1", 2873044601),
                        ["Deforestation"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Woodcutting_Ultimate, BuffType.IO), "https://www.dropbox.com/s/tww0gg1pwh90qbb/Deforestation.png?dl=1", 2873044743)
                    }),
                    ["Skinning"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Amature Skinner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Skinning_Yield, BuffType.Percentage), "https://www.dropbox.com/s/dxrecqcjlsaqskm/Amature_Skinner.png?dl=1", 2873044870),
                        ["Skilled Tracker"] = new Configuration.TreeInfo.NodeInfo(true, 1, 1, 1f, new KeyValuePair<Buff, BuffType>(Buff.AnimalTracker, BuffType.IO), "https://www.dropbox.com/s/mai6z52eiqqqrwx/Skilled_Tracker.png?dl=1", 2873044977),
                        ["Adept Skinner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.075f, new KeyValuePair<Buff, BuffType>(Buff.Skinning_Yield, BuffType.Percentage), "https://www.dropbox.com/s/cy3eo6mr6r4gbeb/Adept_Skinner.png?dl=1", 2873045152),
                        ["Instant Skinner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Instant_Skin, BuffType.Percentage), "https://www.dropbox.com/s/wgic15j3uecdegl/Instant_Skinner.png?dl=1", 2873045291),
                        ["Robust Knife"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Skinning_Tool_Durability, BuffType.Percentage), "https://www.dropbox.com/s/j8fkg0xu1cfz7wd/Robust_Knife.png?dl=1", 2873045413),
                        ["Expert Skinner"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Skinning_Yield, BuffType.Percentage), "https://www.dropbox.com/s/fs66l8mad4uci9p/Expert_Skinner.png?dl=1", 2873045569),
                        ["Survival Chef"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Skin_Cook, BuffType.Percentage), "https://www.dropbox.com/s/eahszj2z8lppx66/Survival_Chef.png?dl=1", 2873046097),
                        ["Steel Knife"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Skinning_Tool_Durability, BuffType.Percentage), "https://www.dropbox.com/s/0c8kf86a43rmo5r/Steel_Knife.png?dl=1", 2873046697),
                        ["Skilled Hunter"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Animal_NPC_Damage, BuffType.Percentage), "https://www.dropbox.com/s/asrhyvazu2ilkqx/Skilled_Hunter.png?dl=1", 2873046221),
                        ["Primal Identity"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Skinning_Ultimate, BuffType.IO), "https://www.dropbox.com/s/ba8gmtvr62rek6q/Primal_Identity.png?dl=1", 2873046423),
                        ["Skinning Luck"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.03f, new KeyValuePair<Buff, BuffType>(Buff.Skinning_Luck, BuffType.Percentage), "https://www.dropbox.com/s/mpqadgqy1h6uj38/Shamanskill_09_nobg.v1.png?dl=1", 2912885514),
                    }),
                    ["Harvesting"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Amature Harvester"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Harvest_Wild_Yield, BuffType.Percentage), "https://www.dropbox.com/s/kpvo6cniebzgvdx/Amature_Harvester.png?dl=1", 2873046943),
                        ["Hobbiest Harvester"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.15f, new KeyValuePair<Buff, BuffType>(Buff.Harvest_Wild_Yield, BuffType.Percentage), "https://www.dropbox.com/s/s5slqgk7sxdd1w3/Hobbiest_Harvester.png?dl=1", 2873047153),
                        ["Amature Farmer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.075f, new KeyValuePair<Buff, BuffType>(Buff.Harvest_Grown_Yield, BuffType.Percentage), "https://www.dropbox.com/s/sqpubic88t7oyaw/Amature_Farmer.png?dl=1", 2873047337),
                        ["Extra pockets"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 2f, new KeyValuePair<Buff, BuffType>(Buff.ExtraPockets, BuffType.Slots), "https://www.dropbox.com/s/damaguptw7r54r5/Extra_pockets.png?dl=1", 2873047443),
                        ["Expert Harvester"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Harvest_Wild_Yield, BuffType.Percentage), "https://www.dropbox.com/s/t8xms1jaxgoissj/Expert_Harvester.png?dl=1", 2873047547),
                        ["Expert Farmer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Harvest_Grown_Yield, BuffType.Percentage), "https://www.dropbox.com/s/s1z0p2z80r6bkpn/Expert_Farmer.png?dl=1", 2873047644),
                        ["Fisherman"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Extra_Fish, BuffType.Percentage), "https://www.dropbox.com/s/ohvnyq50bmt46ok/Fisherman.png?dl=1", 2873047745),
                        ["Botanist"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Harvester_Ultimate, BuffType.IO), "https://www.dropbox.com/s/8fylvtokuhcj24u/Botanist.png?dl=1", 2873047865),
                        ["Fishing Luck"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.03f, new KeyValuePair<Buff, BuffType>(Buff.Fishing_Luck, BuffType.Percentage), "https://www.dropbox.com/s/lrwsqmsyx7a8653/Shamanskill_14_nobg.v1.png?dl=1", 2912896627),
                        ["Ichthyologist"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Bite_Speed, BuffType.Percentage), "https://www.dropbox.com/s/o60v2xmswg9nb91/Archerskill_09_nobg.v2.png?dl=1", 3006635760),
                        ["Foragers Intuition"] = new Configuration.TreeInfo.NodeInfo(true, 1, 3, 1f, new KeyValuePair<Buff, BuffType>(Buff.Forager, BuffType.IO), "https://www.dropbox.com/s/y2k9s25terdic5p/Druideskill_10_nobg.v1.png?dl=1", 3010448075),
                        ["Braided Line"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.07f, new KeyValuePair<Buff, BuffType>(Buff.Rod_Tension_Bonus, BuffType.Percentage), "https://www.dropbox.com/s/p3rkfmoay0tglpz/Warriorskill_38_nobg.v1.png?dl=1", 3012306483),
                    }),
                    ["Medical"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Bandage Expert"] = new Configuration.TreeInfo.NodeInfo(true, 1, 1, 1f, new KeyValuePair<Buff, BuffType>(Buff.Double_Bandage_Heal, BuffType.IO), "https://www.dropbox.com/s/p1wlvzz5vtyvd24/Bandage_Expert.png?dl=1", 2873048071),
                        ["Radiation Expert"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Radiation_Reduction, BuffType.Percentage), "https://www.dropbox.com/s/g18h9sv1q5wi1t7/Radiation_Expert.png?dl=1", 2873965334),
                        ["Revitalization"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.HealthRegen, BuffType.PerSecond), "https://www.dropbox.com/s/9neapkjx2ntpclm/Revitalization.png?dl=1", 2873048224),
                        ["Flame Retardant"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Fire_Damage_Reduction, BuffType.Percentage), "https://www.dropbox.com/s/mmvn40hp81niuc5/Flame_Retardant.png?dl=1", 2873048354),
                        ["Accident Evasion"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Fall_Damage_Reduction, BuffType.Percentage), "https://www.dropbox.com/s/1n69ohui470smxj/Accident_Evasion.png?dl=1", 2873048450),
                        ["Battle Medic"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Reviver, BuffType.Percentage), "https://www.dropbox.com/s/opmy93244kmtj7j/Battle_Medic.png?dl=1", 2873048560),
                        ["Rugged Up"] = new Configuration.TreeInfo.NodeInfo(true, 1, 3, 1f, new KeyValuePair<Buff, BuffType>(Buff.No_Cold_Damage, BuffType.IO), "https://www.dropbox.com/s/l9hmad91yyb8v3x/Rugged_Up.png?dl=1", 2873048673),
                        ["Perfect Balance"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Fall_Damage_Reduction, BuffType.Percentage), "https://www.dropbox.com/s/vn5rf6prsaviblj/Perfect_Balance.png?dl=1", 2873048757),
                        ["Second Wind"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Wounded_Resist, BuffType.Percentage), "https://www.dropbox.com/s/zh9wcs2alr4uzlm/Second_Wind.png?dl=1", 2873048855),
                        ["Fresh Spawn"] = new Configuration.TreeInfo.NodeInfo(true, 1, 3, 1.0f, new KeyValuePair<Buff, BuffType>(Buff.Spawn_Health, BuffType.Percentage), "https://www.dropbox.com/scl/fi/241p09h1vhv7v0anqfz91/Priestskill_27_nobg.v1.png?rlkey=vqt9cl274qcxkq050mzqhdhcs&dl=1", 3036169025),
                        ["Messiah"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Medical_Ultimate, BuffType.IO), "https://www.dropbox.com/s/qz76ogz9tayfgwb/Messiah.png?dl=1", 2873048963)

                    }),
                    ["Combat"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Animal Tamer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Animal_Damage_Resist, BuffType.Percentage), "https://www.dropbox.com/s/5wgfza2h6d1nxpd/Animal_Tamer.png?dl=1", 2873049142),
                        ["Defence Research"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Human_NPC_Defence, BuffType.Percentage), "https://www.dropbox.com/s/rs66cu2qrawjfjw/Defence_Research.png?dl=1", 2873049362),
                        ["Resourceful"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Free_Bullet_Chance, BuffType.Percentage), "https://www.dropbox.com/s/1vmyged7iu3j9fi/Resourceful.png?dl=1", 2873049496),
                        ["Scientific Breakthrough"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Human_NPC_Damage, BuffType.Percentage), "https://www.dropbox.com/s/910bdsqaon9ja22/Scientific_Breakthrough.png?dl=1", 2873049588),
                        ["Duelist"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Melee_Resist, BuffType.Percentage), "https://www.dropbox.com/s/619ww8i1fu7a28e/Duelist.png?dl=1", 2873049666),
                        ["Lucky Shot"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.01f, new KeyValuePair<Buff, BuffType>(Buff.PVP_Critical, BuffType.Percentage), "https://www.dropbox.com/s/iezzpbs15qbmt1u/Lucky_Shot.png?dl=1", 2873049731),
                        ["Assassin"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.01f, new KeyValuePair<Buff, BuffType>(Buff.PVP_Damage, BuffType.Percentage), "https://www.dropbox.com/s/i39yhkrdvwti1dn/Assassin.png?dl=1", 2873049790),
                        ["Guarded"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.PVP_Shield, BuffType.Percentage), "https://www.dropbox.com/s/yufh2ieo4kysb5g/Guarded.png?dl=1", 2873049899),
                        ["Drum Mag"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Extended_Mag, BuffType.Percentage), "https://www.dropbox.com/s/2zil9t4brndzgd7/Extended_Mag.v1.png?dl=1", 2995245729),
                        ["Vampiric Tendencies"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Combat_Ultimate, BuffType.IO), "https://www.dropbox.com/s/3brv8bohuk75npj/Vampiric_Tendencies.png?dl=1", 2873050024),
                        ["Maintenance"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Durability, BuffType.Percentage), "https://www.dropbox.com/s/b1bgfqdxe2wunr0/Maintenance.png?dl=1", 2873050116)
                    }),
                    ["Build_Craft"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Amature Tinkerer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Craft_Speed, BuffType.Percentage), "https://www.dropbox.com/s/4hw98tpjmsohf9n/Amature_Tinkerer.png?dl=1", 2873050271),
                        ["Thrifty Renovator"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Upgrade_Refund, BuffType.Percentage), "https://www.dropbox.com/s/36274c1xkjjtb0f/Thirfty_Renovator.png?dl=1", 2873050381),
                        ["Adept Tinkerer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Craft_Speed, BuffType.Percentage), "https://www.dropbox.com/s/mikhmlqfx5y7fvl/Adept_Tinkerer.png?dl=1", 2873050490),
                        ["Thrifty Tinkerer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Craft_Refund, BuffType.Percentage), "https://www.dropbox.com/s/yp73p3ruy4l4oh2/Thrifty_Tinkerer.png?dl=1", 2873050600),
                        ["Researcher"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Research_Refund, BuffType.Percentage), "https://www.dropbox.com/s/armldhxjctbg67w/Research.png?dl=1", 2873050672),
                        ["Expert Tinkerer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Craft_Speed, BuffType.Percentage), "https://www.dropbox.com/s/8ibntd9n1033dh4/Expert_Tinkerer.png?dl=1", 2873050815),
                        ["Blast Furnace"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Smelt_Speed, BuffType.Percentage), "https://www.dropbox.com/s/p0i3ef8mq4mug05/Blast_Furnace.png?dl=1", 2873050908),
                        ["Primitive Expert"] = new Configuration.TreeInfo.NodeInfo(true, 1, 1, 1f, new KeyValuePair<Buff, BuffType>(Buff.Primitive_Expert, BuffType.IO), "https://www.dropbox.com/s/gvy0dreisfvd9t5/Primitive_Expert.png?dl=1", 2873051010),
                        ["Thrifty Duplicator"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.03f, new KeyValuePair<Buff, BuffType>(Buff.Craft_Duplicate, BuffType.Percentage), "https://www.dropbox.com/s/lzd1p3l8q5t7rx2/Thrifty_Duplicator.png?dl=1", 2873051123),
                        ["Access Granted"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Build_Craft_Ultimate, BuffType.IO), "https://www.dropbox.com/s/58hmrgytnntn314/Access_Granted.png?dl=1", 2873051221),
                        ["Blacksmith"] = new Configuration.TreeInfo.NodeInfo(true, 1, 3, 1f, new KeyValuePair<Buff, BuffType>(Buff.MaxRepair, BuffType.IO), "https://www.dropbox.com/s/b4k6p03u77x9d7t/Blacksmith.png?dl=1", 2873051294)

                    }),
                    ["Scavenging"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Looter"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Extra_Scrap_Barrel, BuffType.Percentage), "https://www.dropbox.com/s/y1cmvfjafenh1du/Looter.png?dl=1", 2873051741),
                        ["Barrel Smasher"] = new Configuration.TreeInfo.NodeInfo(true, 1, 1, 1f, new KeyValuePair<Buff, BuffType>(Buff.Barrel_Smasher, BuffType.IO), "https://www.dropbox.com/s/sebu0m6s5v2sual/Barrel_Smasher.png?dl=1", 2873051884),
                        ["Loot Magnet"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Loot_Pickup, BuffType.Percentage), "https://www.dropbox.com/s/hqthffkbbm73krh/Loot_Magnet.png?dl=1", 2873965904),
                        ["Lucky Looter"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.05f, new KeyValuePair<Buff, BuffType>(Buff.Extra_Scrap_Crate, BuffType.Percentage), "https://www.dropbox.com/s/wpo449q9tinohgh/Lucky_Looter.png?dl=1", 2873052061),
                        ["Electronics Luck"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Electronic_Chest, BuffType.Percentage), "https://www.dropbox.com/s/zshx9nme86dqrhm/Electronics_Luck.png?dl=1", 2873052180),
                        ["Component Luck"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Component_Chest, BuffType.Percentage), "https://www.dropbox.com/s/vlz5kic6d0qw5df/Components_Luck.png?dl=1", 2873052277),
                        ["Component Salvager"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Component_Barrel, BuffType.Percentage), "https://www.dropbox.com/s/b4xu8xxe7zjwrwj/Component_Salvager.png?dl=1", 2873052390),
                        ["Electronics Salvager"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Electronic_Barrel, BuffType.Percentage), "https://www.dropbox.com/s/jcgwgqzvoxw8y9e/Electronics_Salvager.png?dl=1", 2873052471),
                        ["Optimized Recycling"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.5f, new KeyValuePair<Buff, BuffType>(Buff.Recycler_Speed, BuffType.Seconds), "https://www.dropbox.com/s/3mq7xdj8zmdltp7/Optimized_Recycling.png?dl=1", 2873052554),
                        ["Shredder"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Scavengers_Ultimate, BuffType.IO), "https://www.dropbox.com/s/xib7ax3gwo6gg97/Shredder.png?dl=1", 2873052624)
                    }),
                    ["Vehicles"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Amature Rider"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Riding_Speed, BuffType.Percentage), "https://www.dropbox.com/s/9lj4colvhioznl1/Amature_Rider.png?dl=1", 2873052721),
                        ["Adept Rider"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Riding_Speed, BuffType.Percentage), "https://www.dropbox.com/s/javi1k9ys88iwxg/Adept_Rider.png?dl=1", 2873053592),
                        ["Expert Rider"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Riding_Speed, BuffType.Percentage), "https://www.dropbox.com/s/oxfeh3b1xvqqo7t/Expert_Rider.png?dl=1", 2873053752),
                        ["Economical Pilot"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.03f, new KeyValuePair<Buff, BuffType>(Buff.Heli_Fuel_Rate, BuffType.Percentage), "https://www.dropbox.com/s/0h1hq0nco5dimtv/Economical_Pilot.png?dl=1", 2873054261),
                        ["Hybrid Pilot"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.07f, new KeyValuePair<Buff, BuffType>(Buff.Heli_Fuel_Rate, BuffType.Percentage), "https://www.dropbox.com/s/wgjms949cdlzegm/Hybrid_Pilot.png?dl=1", 2873054373),
                        ["Yachtman"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Boat_Speed, BuffType.Percentage), "https://www.dropbox.com/s/86096soadnsy2qh/Yachtman.png?dl=1", 2873054539),
                        ["Economical Captain"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.15f, new KeyValuePair<Buff, BuffType>(Buff.Boat_Fuel_Rate, BuffType.Percentage), "https://www.dropbox.com/s/5f4agk38f2y1ch4/Economical_Captain.png?dl=1", 2873054654),
                        ["Mechanic"] = new Configuration.TreeInfo.NodeInfo(true, 1, 3, 1f, new KeyValuePair<Buff, BuffType>(Buff.Vehicle_Mechanic, BuffType.IO), "https://www.dropbox.com/s/pdpum3gj4sfiowy/Mechanic.png?dl=1", 2873054766),
                        ["Tank"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Vehicle_Ultimate, BuffType.Percentage), "https://www.dropbox.com/s/xdcl74d0e82e32l/Tank.png?dl=1", 2873054991),
                        ["Jet Engine"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.3f, new KeyValuePair<Buff, BuffType>(Buff.Heli_Speed, BuffType.Percentage), "https://www.dropbox.com/s/9948qp5tv7oc6l9/Shamanskill_42_nobg.v1.png?dl=1", 3009474256),
                    }),
                    ["Cooking"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Easily satisfied"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Extra_Food_Water, BuffType.Percentage), "https://www.dropbox.com/s/473rrrfjnt9huij/Easily_Satisfied.png?dl=1", 2873055105),
                        ["Iron Stomach"] = new Configuration.TreeInfo.NodeInfo(true, 1, 2, 1f, new KeyValuePair<Buff, BuffType>(Buff.Iron_Stomach, BuffType.IO), "https://www.dropbox.com/s/221dsnnubpa2825/Iron_Stomach.png?dl=1", 2873055201),
                        ["Glutton"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Metabolism_Boost, BuffType.Percentage), "https://www.dropbox.com/s/t0r4pttz6b7663c/Glutton.png?dl=1", 2873055301),
                        ["Fruggal Rationer"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Rationer, BuffType.Percentage), "https://www.dropbox.com/s/azdltk7o99y7mpk/Fruggal_Rationer.png?dl=1", 2873055400),
                        ["Tea Party"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Extended_Tea_Duration, BuffType.Percentage), "https://www.dropbox.com/s/himcda9ukseslvf/TeTime.png?dl=1", 3005685932),
                        ["Tea Connoisseur"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Tea_Looter, BuffType.Percentage), "https://www.dropbox.com/s/htpiwo9aeqildv8/Archerskill_38_nobg.v1.png?dl=1", 3006539324),
                        ["Burst Of Energy"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1f, new KeyValuePair<Buff, BuffType>(Buff.Cooking_Ultimate, BuffType.IO), "https://www.dropbox.com/s/jjhk0ug4ydajdqa/Assassinskill_12_nobg.v1.png?dl=1", 3011025470),
                    }),
                    ["Underwater"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Cage Diver"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.SharkResistance, BuffType.Percentage), "https://www.dropbox.com/s/hti5v35qh45lj94/Cage_Diver.png?dl=1", 2873055798),
                        ["Gilled"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 60f, new KeyValuePair<Buff, BuffType>(Buff.WaterBreathing, BuffType.Seconds), "https://www.dropbox.com/s/e48z07x10qkmh4s/Gilled.png?dl=1", 2873055882),
                        ["Reckless Diver"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.SharkResistance, BuffType.Percentage), "https://www.dropbox.com/s/k204yk4b6upj0r4/Reckless_Diver.png?dl=1", 2873056389),
                        ["Shark Veterinarian"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.SharkSkinner, BuffType.Percentage), "https://www.dropbox.com/s/zjiqshz9tzpgh82/Shark_Veterinarian.png?dl=1", 2873056528),
                        ["Treasure Hunter"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.DeepSeaLooter, BuffType.Percentage), "https://www.dropbox.com/s/po3zsz763hopkjc/Treasure_Hunter.png?dl=1", 2873056606),
                        ["Nimble Fingers"] = new Configuration.TreeInfo.NodeInfo(true, 1, 3, 1f, new KeyValuePair<Buff, BuffType>(Buff.InstantUntie, BuffType.IO), "https://www.dropbox.com/s/3m853nqxq2qmoih/Nimble_Fingers.png?dl=1", 2873056697),
                        ["Aquatic Combatant"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.10f, new KeyValuePair<Buff, BuffType>(Buff.UnderwaterDamageBonus, BuffType.Percentage), "https://www.dropbox.com/s/683npn5fwvkbtni/Aquatic_Combatant.png?dl=1", 2873056786)
                    }),
                    ["Raiding"] = new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()
                    {
                        ["Trap Evader"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Trap_Damage_Reduction, BuffType.Percentage), "https://www.dropbox.com/s/lr88azap541gn7s/Trappingly%20Challenged.v1.png?dl=1", 2928072989),
                        ["Mine Sweeper"] = new Configuration.TreeInfo.NodeInfo(true, 5, 1, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Trap_Damage_Increase, BuffType.Percentage), "https://www.dropbox.com/s/j35nvrjiyg7wadl/Trap_Damage_Increase.v1.png?dl=1", 2928093322),
                        ["Blast Suit"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Personal_Explosive_Reduction, BuffType.Percentage), "https://www.dropbox.com/s/szg0pp5ow0fi11r/Personal_Explosive_Reduction.v1.png?dl=1", 2928111784),
                        ["Demolition"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Double_Explosion_Chance, BuffType.Percentage), "https://www.dropbox.com/s/wk8xom6d7ab09jw/Double_Explosion_chance.v1.png?dl=1", 2928137975),
                        ["Pressed Explosive"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.1f, new KeyValuePair<Buff, BuffType>(Buff.Explosion_Radius, BuffType.Percentage), "https://www.dropbox.com/s/nk89h8chtl4svx1/Explosion_Radius.v1.png?dl=1", 2928169093),
                        ["Master Thief"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.02f, new KeyValuePair<Buff, BuffType>(Buff.Lock_Picker, BuffType.Percentage), "https://www.dropbox.com/s/pcuy6jo1q567p9b/Lock_Picker.png?dl=1", 2928793256),
                        ["Rain hellfire"] = new Configuration.TreeInfo.NodeInfo(true, 1, 4, 1.0f, new KeyValuePair<Buff, BuffType>(Buff.Raiding_Ultimate, BuffType.IO), "https://www.dropbox.com/s/tfn326c3pnhyri4/Raiding_Ultimate.v1.png?dl=1", 2929679053),
                        ["Reliable Explosive"] = new Configuration.TreeInfo.NodeInfo(true, 5, 2, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Dudless_Explosive, BuffType.Percentage), "https://www.dropbox.com/s/rrxxt78hu2r0vb4/Dudless_Explosive.png?dl=1", 2930484526),
                        ["Perceptive"] = new Configuration.TreeInfo.NodeInfo(true, 5, 3, 0.2f, new KeyValuePair<Buff, BuffType>(Buff.Trap_Spotter, BuffType.Percentage), "https://www.dropbox.com/s/as74z7rjlfiwy98/Druideskill_27_nobg.v1.png?dl=1", 3012262421),
                    }),
                };
            }
        }

        Dictionary<AnimalBuff, float> DefaultAnimalBuffs
        {
            get
            {
                return new Dictionary<AnimalBuff, float>()
                {
                    [AnimalBuff.Bear] = 120f,
                    [AnimalBuff.Chicken] = 600f,
                    [AnimalBuff.Boar] = 1200f,
                    [AnimalBuff.Stag] = 300f,
                    [AnimalBuff.Wolf] = 600f,
                    [AnimalBuff.PolarBear] = 600f
                };
            }
        }

        List<string> DefaultScavengerUltimateBlacklist
        {
            get
            {
                return new List<string>()
                {
                    "lowgradefuel",
                    "targeting.computer",
                    "cctv.camera"
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
                SaveConfig();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                Interface.Oxide.UnloadPlugin(Name);
            }
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #region Data

        PluginInfo pcdData;
        private DynamicConfigFile PCDDATA;

        void Init()
        {
            Instance = this;
            PCDDATA = Interface.Oxide.DataFileSystem.GetFile(this.Name);
            permission.RegisterPermission("skilltree.noxploss", this);
            permission.RegisterPermission("skilltree.chat", this);
            permission.RegisterPermission("skilltree.xp", this);
            permission.RegisterPermission("skilltree.tree", this);
            permission.RegisterPermission(perm_admin, this);
            permission.RegisterPermission("skilltree.bag.keepondeath", this);
            permission.RegisterPermission("skilltree.notitles", this);
            permission.RegisterPermission(perm_no_scoreboard, this);

            foreach (var perm in config.general_settings.max_skill_points_override.Keys)
            {
                var permString = perm.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase) ? perm : "skilltree." + perm;
                if (!permission.PermissionExists(permString)) permission.RegisterPermission(permString, this);
            }
            foreach (var perm in config.general_settings.respec_cost_override.Keys)
            {
                var permString = perm.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase) ? perm : "skilltree." + perm;
                if (!permission.PermissionExists(permString)) permission.RegisterPermission(permString, this);
            }
            foreach (var perm in config.xp_settings.xp_perm_modifier.Keys)
            {
                var permString = perm.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase) ? perm : "skilltree." + perm;
                if (!permission.PermissionExists(permString)) permission.RegisterPermission(permString, this);
            }
            foreach (var perm in config.rested_xp_settings.rested_xp_modifier_perm_mod.Keys)
            {
                var permString = perm.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase) ? perm : "skilltree." + perm;
                if (!permission.PermissionExists(permString)) permission.RegisterPermission(permString, this);
            }
            foreach (var perm in config.wipe_update_settings.starting_skill_point_overrides.Keys)
            {
                var permString = perm.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase) ? perm : "skilltree." + perm;
                if (!permission.PermissionExists(permString)) permission.RegisterPermission(permString, this);
            }
            permission.RegisterPermission("skilltree.all", this);
            foreach (var tree in config.trees)
            {
                permission.RegisterPermission("skilltree." + tree.Key.ToString(), this);
            }

            if (config.xp_settings.xp_sources.Harbor_Event_Winner == 0) Unsubscribe("OnHarborEventWinner");
            if (config.xp_settings.xp_sources.Junkyard_Event_Winner == 0) Unsubscribe("OnJunkyardEventWinner");
            if (config.xp_settings.xp_sources.Satellite_Event_Winner == 0) Unsubscribe("OnSatDishEventWinner");
            if (config.xp_settings.xp_sources.Water_Event_Winner == 0) Unsubscribe("OnWaterEventWinner");
            if (config.xp_settings.xp_sources.Air_Event_Winner == 0) Unsubscribe("OnAirEventWinner");
            if (config.xp_settings.xp_sources.PowerPlant_Event_Winner == 0) Unsubscribe("OnPowerPlantEventWinner");
            if (config.xp_settings.xp_sources.Armored_Train_Winner == 0) Unsubscribe("OnArmoredTrainEventWin");
            if (config.xp_settings.xp_sources.Convoy_Winner == 0) Unsubscribe("OnConvoyEventWin");
            if (config.xp_settings.xp_sources.SurvivalArena_Winner == 0) Unsubscribe(nameof(OnSurvivalArenaWin));
            if (config.xp_settings.xp_sources.boss_monster == 0) Unsubscribe(nameof(OnBossKilled));

        }

        void Unload()
        {
            SaveNewNodesToConfig();
            DeepSeaLooterLootTable?.Clear();
            SharkLootTable?.Clear();
            if (BasePlayer.activePlayerList?.Count > 0)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    try
                    {
                        CuiHelper.DestroyUi(player, "SkillTree");
                        CuiHelper.DestroyUi(player, "XP_Tick");
                        CuiHelper.DestroyUi(player, "respec_confirmation");
                        CuiHelper.DestroyUi(player, "SkillTreeXPBar");
                        CuiHelper.DestroyUi(player, "ui_mover");
                        CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
                        CuiHelper.DestroyUi(player, "NavigationMenu");
                        CuiHelper.DestroyUi(player, "SkillTree_PlayerMenu");
                        CuiHelper.DestroyUi(player, "ExtraPocketsButton");
                        CuiHelper.DestroyUi(player, "ScoreBoardPanel");
                        CuiHelper.DestroyUi(player, "ScoreboardBackPanel");
                        CuiHelper.DestroyUi(player, "SkillTree_UltimateMenu");
                        CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_ResurrectionButton");
                        CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_Failed");
                        CuiHelper.DestroyUi(player, "Plant_Gene_Select");
                        CuiHelper.DestroyUi(player, "Plant_Gene_Select_background");
                        CuiHelper.DestroyUi(player, "Overshield_main");
                        CuiHelper.DestroyUi(player, "StagDangerUI");
                        CuiHelper.DestroyUi(player, "UnderwaterBreathCounter");

                        DoClear(player);
                        player.EndLooting();
                        LoggingOff(player);
                    }
                    catch { Puts($"Failed to update data for {player?.userID}"); }
                }
            }
            foreach (var player in BasePlayer.allPlayerList)
            {
                try
                {
                    if (player != null)
                    {
                        try { DestroyRegen(player); } catch { }
                        try { DestroyWaterBreathing(player); } catch { }
                        try { DestroyRaidBehaviour(player); } catch { }
                        try { DestroyInstantUntie(player); } catch { }
                    }
                }
                catch { }
            }

            foreach (var horse in HorseStats)
            {
                try
                {
                    if (horse.Value.horse != null && horse.Value.horse.IsAlive()) RestoreHorseStats(horse.Value.horse, true);
                }
                catch { }
            }
            HorseStats.Clear();
            HorseStats = null;

            foreach (var heli in tracked_helis)
            {
                try
                {
                    if (heli.Value != null && heli.Value.IsAlive()) heli.Value.fuelPerSec = default_heli_fuel_rate;
                }
                catch { }
            }
            tracked_helis.Clear();
            tracked_helis = null;

            foreach (var heli in MiniStats)
            {
                try
                {
                    if (heli.Value.mini == null || heli.Value.mini.IsDead()) continue;
                    RestoreMiniStats(heli.Value.mini, null, false);
                }
                catch { }
            }
            MiniStats.Clear();
            MiniStats = null;

            foreach (var boat in tracked_rhibs)
            {
                try
                {
                    if (boat.Value != null && boat.Value.IsAlive()) boat.Value.fuelPerSec = default_rhib_fuel_rate;
                }
                catch { }
            }
            tracked_rhibs.Clear();
            tracked_rhibs = null;

            foreach (var boat in tracked_rowboats)
            {
                try
                {
                    if (boat.Value != null && boat.Value.IsAlive()) boat.Value.fuelPerSec = default_rowboat_fuel_rate;
                }
                catch { }
            }
            tracked_rowboats.Clear();
            tracked_rowboats = null;

            foreach (var boat in Boats)
            {
                try
                {
                    if (boat.Value.boat == null || boat.Value.boat.IsDead()) continue;
                    ResetBoatSpeed(boat.Value.boat, boat.Value.player, false);
                }
                catch { }
            }
            Boats.Clear();
            Boats = null;

            foreach (var chatcommand in config.chat_commands.score_chat_cmd)
            {
                try
                {
                    cmd.RemoveChatCommand(chatcommand, this);
                    cmd.RemoveConsoleCommand(chatcommand, this);
                }
                catch { }
            }
            foreach (var chatcommand in config.chat_commands.chat_cmd)
            {
                try
                {
                    cmd.RemoveChatCommand(chatcommand, this);
                }
                catch { }
            }
            cmd.RemoveChatCommand(config.ultimate_settings.ultimate_raiding.command, this);
            cmd.RemoveChatCommand(config.ultimate_settings.ultimate_cooking.command, this);
            cmd.RemoveChatCommand(config.buff_settings.raid_perk_settings.Lock_Picker_settings.pick_command.Trim(), this);
            if (!config.buff_settings.boat_turbo_on_mount) cmd.RemoveChatCommand(config.chat_commands.turbo_cmd, this);
            SaveData();

            ScoreBoard.scoreList.Clear();
            healers.Clear();
            MiningUltimateCooldowns.Clear();

            foreach (var entity in reduced_damage_entities)
            {
                try
                {
                    RestoreSkinToChildren(entity.Value);
                }
                catch { }
            }
            reduced_damage_entities.Clear();

            cmd.RemoveChatCommand(config.ultimate_settings.ultimate_mining.find_node_cmd, this);
            cmd.RemoveChatCommand(config.ultimate_settings.ultimate_harvesting.gene_chat_command, this);
            cmd.RemoveChatCommand(config.chat_commands.track_animal_cmd, this);
            cmd.RemoveChatCommand(config.buff_settings.forager_settings.command, this);
            cmd.RemoveConsoleCommand(config.buff_settings.forager_settings.command, this);
            cmd.RemoveChatCommand(config.buff_settings.raid_perk_settings.Trap_Spotter_settings.command, this);
            cmd.RemoveConsoleCommand(config.buff_settings.raid_perk_settings.Trap_Spotter_settings.command, this);
            Duds.Clear();
            ItemDefs.Clear();

            foreach (var p in ActivePickers)
            {
                try
                {
                    DestroyPicker(p.Key, p.Value, false);
                }
                catch { }
            }
            ActivePickers.Clear();
            LockPickCooldowns.Clear();

            BaseYieldOverrides?.Clear();
            LastSwipe?.Clear();
            LastSwipe = null;

            ResetWeaponCapacities(false, config.buff_settings.force_unload_extended_mag_weapons_unload);

            ModifiedWeapons.Clear();
            ModifiedWeapons = null;

            TrackedPermissionPerms?.Clear();
            TrackedPermissionPerms = null;

            InstanceDataPlayerQueue?.Clear();
            InstanceDataPlayerQueue = null;

            foreach (var rod in TrackedRods)
            {
                try
                {
                    ResetRod(rod, false);
                }
                catch { }
            }

            TrackedRods?.Clear();
            TrackedRods = null;

            try { AddLogs(); } catch { }

            _harmony.UnpatchAll(Name + "Patch");
        }

        void ResetWeaponCapacities(bool clear = true, bool forceUnloadWeapons = true)
        {
            foreach (var weapon in ModifiedWeapons)
            {
                if (weapon.Value == null) continue;
                RemoveMods(weapon.Value, 0, clear, forceUnloadWeapons);
            }
        }

        void SaveData()
        {
            PCDDATA.WriteObject(pcdData);
        }

        void LoadData()
        {
            try
            {
                pcdData = Interface.Oxide.DataFileSystem.ReadObject<PluginInfo>(this.Name);
            }
            catch
            {
                Puts("Couldn't load player data, creating new Playerfile");
                pcdData = new PluginInfo();
            }
        }

        class PluginInfo
        {
            public DateTime wipeTime = DateTime.MinValue;
            public ulong highest_player;
            public Dictionary<ulong, PlayerInfo> pEntity = new Dictionary<ulong, PlayerInfo>();
        }

        class PlayerInfo
        {
            public string name;
            public double xp;
            public int current_level;
            public int achieved_level;
            public int available_points;
            public Dictionary<string, int> buff_values = new Dictionary<string, int>();
            public bool xp_drops = true;
            public bool xp_hud = true;
            public bool better_chat_enabled = true;
            public xp_bar_offset xp_hud_pos = new xp_bar_offset();
            public List<ItemInfo> pouch_items = new List<ItemInfo>();
            public DateTime logged_off = DateTime.Now;
            public double xp_bonus_pool;
            public bool extra_pockets_button = true;
            public bool notifications = true;
            public Dictionary<Buff, UltimatePlayerSettings> ultimate_settings = new Dictionary<Buff, UltimatePlayerSettings>();
            public string plant_genes = "gggggg";
            public float respec_multiplier = 0;
            public DateTime raiding_ultimate_used_time = DateTime.MinValue;
            public DateTime cooking_ultimate_used_time = DateTime.MinValue;
            public DateTime Trap_Spotter_used_time = DateTime.MinValue;
            public double xp_debt;
            public DateTime last_xp_loss;
        }
        public class UltimatePlayerSettings
        {
            public bool enabled = true;

        }

        public class xp_bar_offset
        {
            public float min_x;
            public float min_y;
            public float max_x;
            public float max_y;
        }

        public class XP_Bar_Anchors
        {
            public string anchor_min = "1 0";
            public string anchor_max = "1 0";
        }

        public ScoreboardInfo ScoreBoard = new ScoreboardInfo();

        public class ScoreboardInfo
        {
            public float lastChecked;
            public Dictionary<ulong, ScoreInfo> scoreList = new Dictionary<ulong, ScoreInfo>();

            public class ScoreInfo
            {
                public string name;
                public double xp;
                public ScoreInfo(string name, double xp)
                {
                    this.name = name;
                    this.xp = xp;
                }
            }
        }

        Dictionary<ulong, TreeInfo> TreeData = new Dictionary<ulong, TreeInfo>();

        class TreeInfo
        {
            public Dictionary<string, NodesInfo> trees = new Dictionary<string, NodesInfo>();
            public int total_points_spent;
        }

        class NodesInfo
        {
            public int min_level;
            public int min_points;
            public int points_spent;
            public int level_2_point_requirement;
            public int level_3_point_requirement;
            public int level_4_point_requirement;
            public Dictionary<string, NodeInfo> nodes = new Dictionary<string, NodeInfo>();
        }

        class NodeInfo
        {
            public string description;
            public int level_current;
            public int level_max;
            public int tier;
            public float value_per_buff;
            public KeyValuePair<Buff, BuffType> buffInfo;
        }

        Dictionary<ulong, HorseInfo> HorseStats = new Dictionary<ulong, HorseInfo>();
        public class HorseInfo
        {
            public RidableHorse horse;
            public float current_maxSpeed;
            public float current_runSpeed;
            public float current_walkSpeed;
            public float current_trotSpeed;
            public float current_turnSpeed;
            public BasePlayer player;
        }

        Dictionary<ulong, MiniInfo> MiniStats = new Dictionary<ulong, MiniInfo>();
        public class MiniInfo
        {
            public PlayerHelicopter mini;
            public float old_lift_fraction;
            public BasePlayer player;
            public float old_engineThrustMax;

            public MiniInfo(BasePlayer player, PlayerHelicopter mini, float old_lift_fraction, float old_engineThrustMax)
            {
                this.player = player;
                this.mini = mini;
                this.old_lift_fraction = old_lift_fraction;
                this.old_engineThrustMax = old_engineThrustMax;
            }
        }

        Dictionary<ulong, BuffDetails> buffDetails = new Dictionary<ulong, BuffDetails>();
        class BuffDetails
        {
            public Dictionary<Buff, float> buff_values = new Dictionary<Buff, float>();
        }

        Dictionary<string, Buff> BuffTypes = new Dictionary<string, Buff>();

        Dictionary<ulong, BoatInfo> Boats = new Dictionary<ulong, BoatInfo>();

        class BoatInfo
        {
            public float defaultSpeed;
            public MotorRowboat boat;
            public BasePlayer player;
            public BoatInfo(BasePlayer player, MotorRowboat boat, float defaultSpeed)
            {
                this.player = player;
                this.boat = boat;
                this.defaultSpeed = defaultSpeed;
            }
        }

        Dictionary<string, ItemBlueprint> item_BPs = new Dictionary<string, ItemBlueprint>();
        Dictionary<string, ItemDefinition> ItemDefs = new Dictionary<string, ItemDefinition>();

        List<ulong> notifiedPlayers = new List<ulong>();

        public float default_heli_fuel_rate = 0.5f;
        public float default_rowboat_fuel_rate = 0.1f;
        public float default_rhib_fuel_rate = 0.25f;

        #endregion

        #region Enums

        enum DeathType
        {
            PVE,
            PVP,
            Suicide
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum BuffType
        {
            IO,
            Percentage,
            Seconds,
            PerSecond,
            Slots,
            Permission
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Buff
        {
            None,
            Mining_Yield,
            Instant_Mine,
            Smelt_On_Mine,
            Mining_Luck,
            Mining_Tool_Durability,
            Woodcutting_Yield,
            Instant_Chop,
            Woodcutting_Luck,
            Woodcutting_Coal,
            Woodcutting_Tool_Durability,
            Skinning_Yield,
            Instant_Skin,
            Skinning_Tool_Durability,
            Skin_Cook,
            Harvest_Wild_Yield,
            Harvest_Grown_Yield,
            Extra_Fish,
            Double_Bandage_Heal,
            Radiation_Reduction,
            Extra_Food_Water,
            Fire_Damage_Reduction,
            Fall_Damage_Reduction,
            No_Cold_Damage,
            Wounded_Resist,
            Animal_Damage_Resist,
            Riding_Speed,
            Free_Bullet_Chance,
            Primitive_Expert,
            Upgrade_Refund,
            Craft_Speed,
            Research_Refund,
            Craft_Refund,
            Extra_Scrap_Barrel,
            Barrel_Smasher,
            Extra_Scrap_Crate,
            Component_Chest,
            Electronic_Chest,
            Component_Barrel,
            Electronic_Barrel,
            Melee_Resist,
            Iron_Stomach,
            Boat_Speed,
            Recycler_Speed,
            Smelt_Speed,
            Heli_Fuel_Rate,
            Boat_Fuel_Rate,
            Vehicle_Mechanic,
            Reviver,
            Rationer,
            PVP_Critical,
            PVP_Damage,
            PVP_Shield,
            Metabolism_Boost,
            Loot_Pickup,
            Node_Spawn_Chance,
            HealthRegen,
            AnimalTracker,
            ExtraPockets,
            Human_NPC_Damage,
            Animal_NPC_Damage,
            Human_NPC_Defence,
            Craft_Duplicate,
            WaterBreathing,
            SharkResistance,
            SharkSkinner,
            DeepSeaLooter,
            InstantUntie,
            UnderwaterDamageBonus,
            Permission,
            MaxRepair,
            Durability,
            Regrowth,
            Skinning_Luck,
            Fishing_Luck,
            Extended_Mag,

            // Raid tree perks
            Trap_Damage_Reduction,
            Trap_Damage_Increase,
            Personal_Explosive_Reduction,
            Building_Damage_Increase,
            Double_Explosion_Chance,
            Lock_Picker,
            Explosion_Radius,
            Dudless_Explosive,

            Woodcutting_Hotspot,
            Mining_Hotspot,
            Extended_Tea_Duration,
            Tea_Looter,
            Bite_Speed,
            Heli_Speed,
            Forager,
            Trap_Spotter,
            Rod_Tension_Bonus,
            Spawn_Health,

            Woodcutting_Ultimate = 991,
            Mining_Ultimate = 992,
            Combat_Ultimate = 993,
            Vehicle_Ultimate = 994,
            Harvester_Ultimate = 995,
            Medical_Ultimate = 996,
            Skinning_Ultimate = 997,
            Build_Craft_Ultimate = 998,
            Scavengers_Ultimate = 999,
            Raiding_Ultimate = 1000,
            Cooking_Ultimate = 1001
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum YieldTypes
        {
            Wood,
            Stone,
            Metal,
            Sulfur,
            Corn,
            Potato,
            Pumpkin,
            Cloth,
            Diesel,
            AnimalFat,
            Bones,
            Leather,
            Fish,
            Seed,
            Mushroom,
            Berry,
        }

        //public string[] Trees = { "Mining", "Woodcutting", "Skinning", "Harvesting", "Combat", "Medical", "Build_Craft", "Scavenging", "Vehicles", "Cooking"};

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            List<Buff> buffs = Pool.GetList<Buff>();
            buffs.AddRange(Enum.GetValues(typeof(Buff)).Cast<Buff>());
            Dictionary<string, string> buffMessages = new Dictionary<string, string>();
            foreach (var buff in buffs)
            {
                var str = buff.ToString();
                if (!buffMessages.ContainsKey("UI" + str)) buffMessages.Add("UI" + str, str.Replace("_", " "));
            }
            Pool.FreeList(ref buffs);

            List<string> titles = Pool.GetList<string>();
            titles.AddRange(config.trees.Keys);
            Dictionary<string, string> TitleMessages = new Dictionary<string, string>();
            foreach (var title in titles)
            {
                if (!TitleMessages.ContainsKey(title)) TitleMessages.Add(title, title.Replace("_", " "));
            }
            Pool.FreeList(ref titles);

            List<string> node_names = Pool.GetList<string>();
            foreach (var entry in config.trees)
            {
                foreach (var node in entry.Value.nodes)
                {
                    if (!node_names.Contains(node.Key)) node_names.Add(node.Key);
                }
            }
            foreach (var node in node_names)
            {
                if (!TitleMessages.ContainsKey(node)) TitleMessages.Add(node, node);
            }
            Pool.FreeList(ref node_names);

            Dictionary<string, string> DefaultMessages = new Dictionary<string, string>()
            {
                ["None"] = "This has no buffs.",
                ["Mining_Yield"] = "This skill increases your mining yield by <color=#42f105>{0}%</color> per level.",
                ["Instant_Mine"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to instantly mine out a node on hit.",
                ["Smelt_On_Mine"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to refine the mined ore.",
                ["Mining_Luck"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to receive a random item when you mine out a node.",
                ["Skinning_Luck"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to receive a random item when you skin out a corpse.",
                ["Fishing_Luck"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to receive a random item when you catch a fish.",
                ["Mining_Tool_Durability"] = "This skill decreases the durability loss of your mining equipment by <color=#42f105>{0}%</color> per level.",
                ["Mining_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill allows you to locate nodes within a <color=#42f105>{0}m</color> radius.",
                ["Medical_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill gives you a <color=#42f105>{0}%</color> chance to resurrect at your last place of death.",
                ["Harvester_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill gives you the ability to set the genetic composition for plants you deploy{0}.",
                ["Woodcutting_Yield"] = "This skill increases your woodcutting yield by <color=#42f105>{0}%</color> per level.",
                ["Instant_Chop"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to instantly chop a tree down on hit.",
                ["Regrowth"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to respawn a chopped tree.",
                ["Woodcutting_Luck"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to receive a random item when you cut down a tree.",
                ["Woodcutting_Coal"] = "This skill gives you a <color=#42f105>{0}%</color> chance to receive some charcoal while woodcutting.",
                ["Woodcutting_Tool_Durability"] = "This skill decreases the durability loss of your woodcutting equipment by <color=#42f105>{0}%</color> per level.",
                ["Woodcutting_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill will harvest surrounding trees in a <color=#42f105>{0}m</color> radius when you cut a tree down.",
                ["Skinning_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> Killing an animal while this skill is active will give you a temporary buff. <color=#42f105>{0}</color>",
                ["Combat_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill will heal you for <color=#42f105>{0}%</color> of the damage done to certain enemies. Enemies: {1}",
                ["Scavengers_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill automatically recycles components from barrels when broken.",
                ["Build_Craft_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill will allow you to use any coloured swipe card in any reader to access it{0}.",
                ["Skinning_Yield"] = "This skill increases your skinning yield by <color=#42f105>{0}%</color> per level.",
                ["Instant_Skin"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to instantly skin out the animal on hit.",
                ["Skinning_Tool_Durability"] = "This skill decreases the durability loss of your skinning equipment by <color=#42f105>{0}%</color> per level",
                ["Skin_Cook"] = "This skill gives you a <color=#42f105>{0}%</color> per level chance of receiving your meat cooked, rather than raw, while skinning.",
                ["Permission"] = "",
                ["Harvest_Wild_Yield"] = "This skill increases your harvesting yield by <color=#42f105>{0}%</color> per level while harvesting wild collectibles.",
                ["Harvest_Grown_Yield"] = "This skill increases your harvesting yield by <color=#42f105>{0}%</color> per level while harvesting grown plants.",
                ["Extra_Fish"] = "This skill gives you a <color=#42f105>{0}%</color> per level chance of receiving an extra fish when you catch a fish.",
                ["Double_Bandage_Heal"] = "This skill will double the amount of healing received from bandages.",
                ["Radiation_Reduction"] = "This skill will reduce radiation damage received by <color=#42f105>{0}%</color> per level.",
                ["Extra_Food_Water"] = "This skill will increase the amount of calories and hydration received by <color=#42f105>{0}%</color> per level when eating food.",

                ["WaterBreathing"] = "This skill will allow you to breath underwater for <color=#42f105>{0} seconds</color> per level.",
                ["SharkResistance"] = "This skill will reduce the damage received from sharks by <color=#42f105>{0}%</color> per level.",
                ["SharkSkinner"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of finding a useful item when skinning a shark.",
                ["DeepSeaLooter"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of finding a useful item when looting crates underwater.",
                ["InstantUntie"] = "This skill allows you to instantly untie underwater crates.",
                ["UnderwaterDamageBonus"] = "This skill increases the damage done while you are underwater by <color=#42f105>{0}%</color> per level.",

                ["Fire_Damage_Reduction"] = "This skill will reduce fire damage by <color=#42f105>{0}%</color> per level.",
                ["Fall_Damage_Reduction"] = "This skill will reduce fall damage by <color=#42f105>{0}%</color> per level.",
                ["No_Cold_Damage"] = "This skill prevents you from being damaged by the cold.",
                ["Wounded_Resist"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of immediately getting up after being wounded.",
                ["Animal_Damage_Resist"] = "This skill reduces the damage taken by animals by <color=#42f105>{0}%</color> per level.",
                ["Riding_Speed"] = "This skill increases the speed of your mounted horse by <color=#42f105>{0}%</color> per level.",
                ["Free_Bullet_Chance"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of not using a bullet while firing.",
                ["Primitive_Expert"] = "This skill makes primitive weapons lose no durability.",
                ["Upgrade_Refund"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of receiving your building materials back when upgrading your building blocks.",
                ["Craft_Speed"] = "This skill increases your crafting speed by <color=#42f105>{0}%</color> per level.",
                ["MaxRepair"] = "This skill will reset the durability of items to max when repairing them.",
                ["Durability"] = "This skill reduces durability loss by <color=#42f105>{0}%</color> per level.",
                ["Smelt_Speed"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to smelt ore when a log is burned, in addition to the normal smelt rate.",
                ["Research_Refund"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of receiving your scrap back when researching.",
                ["Craft_Refund"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of receiving your components back when crafting.",
                ["Craft_Duplicate"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of duplicating an item while crafting.",
                ["Extra_Scrap_Barrel"] = "This skill gives you <color=#42f105>{0}%</color> chance per level to receive extra scrap when smashing a barrel.",
                ["Barrel_Smasher"] = "This skill allows you to smash a barrel in 1 hit with any weapon.",
                ["Loot_Pickup"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to add the loot straight into your inventory when destroying a barrel",
                ["Node_Spawn_Chance"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level to spawn a new node after a node is destroyed.",
                ["HealthRegen"] = "This skill regenerates your health by <color=#42f105>{0}hp</color> per level per second.",
                ["Extra_Scrap_Crate"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of finding extra scrap in a crate, the first time you loot it.",
                ["Component_Chest"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of finding components in a crate, the first time you loot it.",
                ["Electronic_Chest"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of finding electronics in a crate, the first time you loot it.",
                ["Component_Barrel"] = "This skill gives you <color=#42f105>{0}%</color> chance per level of finding extra components when destroying barrels.",
                ["Electronic_Barrel"] = "This skill gives you a <color=#42f105>{0}%</color> chance per level of finding extra electronics when destroying barrels.",
                ["Melee_Resist"] = "This skill will reduce melee damage by <color=#42f105>{0}%</color> per level.",
                ["Iron_Stomach"] = "This skill will prevent you from being sick when consuming raw and spoiled food.",
                ["Recycler_Speed"] = "This skill will increase your recycling tick speed by {0} seconds per level",
                ["Boat_Speed"] = "This skill will allow you to toggle your speed while in a boat by pressing mouse 3 or typing the turbo command. Increases your speed by <color=#42f105>{0}%</color> per level.",
                ["BoatSpeedAuto"] = "This skill will increase the speed of your mounted boat by <color=#42f105>{0}%</color> per level.",
                ["Heli_Fuel_Rate"] = "This skill will reduce your fuel consumption when flying a helicopter by <color=#42f105>{0}%</color> per level.",
                ["Boat_Fuel_Rate"] = "This skill will reduce your fuel consumption when using a boat by <color=#42f105>{0}%</color> per level.",
                ["Vehicle_Mechanic"] = "This skill will allow you to instantly repair vehicles at no cost.",
                ["Vehicle_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill reduces damage dealt to your mounted vehicle by <color=#42f105>{0}%</color>.",
                ["Raiding_Ultimate"] = "<color=#db03cb>ULTIMATE:</color> This skill allows you to call in a MLRS strike.",
                ["Reviver"] = "This skill will heal a downed player when revived for <color=#42f105>{0}%</color> of their health per level.",
                ["PVP_Shield"] = "Reduce the damage receive in PVP by <color=#42f105>{0}%</color> per level.",
                ["Extended_Mag"] = "Increases the maximum ammo capacity of your weapons by <color=#42f105>{0}%</color> per level.",
                ["Tea_Looter"] = "This skill gives you a <color=#42f105>{0}%</color> per level of finding tea in food crates and boxes.",
                ["Bite_Speed"] = "This skill increases the speed to hook a fish while fishing by <color=#42f105>{0}%</color> per level.",
                ["Heli_Speed"] = "This skill increases the lift fraction of helicopters <color=#42f105>{0}%</color> per level.",
                ["Forager"] = "This skill allows you to locate collectibles within a <color=#42f105>{0}m</color> radius.",
                ["PVP_Critical"] = "This skill will give you a <color=#42f105>{0}%</color> chance per level of critically damaging a player in PVP when hit.",
                ["PVP_Damage"] = "This skill will increase the amount of damage you do in PVP by <color=#42f105>{0}%</color> per level.",
                ["Metabolism_Boost"] = "Increases your calories and hydration by <color=#42f105>{0}%</color> per level.",
                ["Rationer"] = "This skill will provide you with a <color=#42f105>{0}%</color> chance per level chance to receive your consumed items back.",
                ["AnimalTracker"] = "This skill will allow you to track the closest animal to you using the <color=#42f105>/track</color> command.",
                ["ExtraPockets"] = "This skill will provide you pouch that can be accessed via the <color=#42f105>/pouch</color> command. Storage is increased by <color=#42f105>{0}</color> slots per level.",
                ["Human_NPC_Damage"] = "This skill increases the damage done to human NPCs by <color=#42f105>{0}%</color> per level.",
                ["Human_NPC_Defence"] = "This skill decreases the damage done from scientists by <color=#42f105>{0}%</color> per level.",
                ["Animal_NPC_Damage"] = "This skill increases the damage done to animals by <color=#42f105>{0}%</color> per level.",

                ["Trap_Damage_Reduction"] = "This skill reduces the damage taken from traps by <color=#42f105>{0}%</color> per level.",
                ["Trap_Damage_Increase"] = "This skill increases the damage against traps by <color=#42f105>{0}%</color> per level.",
                ["Personal_Explosive_Reduction"] = "This skill reduces the damage taken from your own explosives by <color=#42f105>{0}%</color> per level.",
                ["Double_Explosion_Chance"] = "This skill provides you with a <color=#42f105>{0}%</color> chance per level to have an explosive trigger twice.",
                ["Explosion_Radius"] = "Increases the radius of your explosives by <color=#42f105>{0}%</color> per level.",
                ["Lock_Picker"] = "Provides the player with a <color=#42f105>{0}%</color> chance per level to open a locked entity.",
                ["Dudless_Explosive"] = "Provides a <color=#42f105>{0}%</color> chance per level for a dud explosive to explode anyway.",
                ["Trap_Spotter"] = "Provides a <color=#42f105>{0}%</color> chance per level to see nearby traps.",
                ["Rod_Tension_Bonus"] = "Provides a <color=#42f105>{0}%</color> reduction per level to the rod tension when catching fish.",
                ["Spawn_Health"] = "This skill changes your starting health to <color=#42f105>{0}hp</color> when respawning.",
                ["Woodcutting_Hotspot_IO"] = "This skill will treat all hits on a tree as hotspot hits.",
                ["Woodcutting_Hotspot_Percentage"] = "This skill will provide you with a <color=#42f105>{0}%</color> chance per level for a hit on a tree to automatically hit the hotspot.",
                ["Mining_Hotspot_IO"] = "This skill will treat all hits on ore as hotspot hits.",
                ["Mining_Hotspot_Percentage"] = "This skill will provide you with a <color=#42f105>{0}%</color> chance per level for a hit on a node to automatically hit the hotspot.",
                ["Extended_Tea_Duration"] = "This skill will extend the duration of tea effects by <color=#42f105>{0}%</color> per level.",

                ["ItemFound"] = "You found {0}x {1} hiding in the {2}.",
                ["ExtraFish"] = "You received {0} extra {1}!",
                ["CraftRefund"] = "You received a refund on your last craft.",
                ["DuplicateProc"] = "You received an additional <color=#42f105>[{0}]</color> while crafting.",
                ["IronTummy"] = "You eat the {0} and feel ok.",
                ["WoundSave"] = "Your ability prevent you from being wounded.",
                ["LostXP"] = "You lost {0} for dying.",
                ["FreeUpgrade"] = "You received a free upgrade!",
                ["ScrapRefund"] = "You received a scrap refund on your research.",
                ["LevelEarn"] = "You gained a level and earned {0} skill point.\nNew level: {1}",
                ["LevelEarnDiscord"] = "{0}[{1}] achieved level {2}.",
                ["MorePlayersFound"] = "More than one player found: {0}",
                ["NoMatch"] = "No player was found that matched: {0}",
                ["MaxSP"] = "You have spent the maximum number of skill points allowed.",
                ["MaxedNode"] = "You have already maxed out this node.",
                ["NoPrevTierIncUltimate"] = "You do not have enough points in the previous tier to level this node.\nTier 2 requires {0} points.\nTier 3 requires {1} points.\nUltimate requires {2}.",
                ["MaxedSkillPoints"] = "You do not have enough skill points left to level this node.",
                ["AssignedMaxedSkillPoints"] = "You have already assigned the maximum skill points allowed.",
                ["UnlockedFirstNode"] = "You unlocked the {0} node [1/{1}]",
                ["UnlockedNode"] = "You gained a level in the {0} node [{1}/{2}]",
                ["NoPermsTree"] = "You do not have permission to access to the Skill Tree.",
                ["RespecNoScrap"] = "You do not have enough scrap to respect your skill tree.",
                ["EconNotLoaded"] = "Economics is not loaded. Contact your administrator.",
                ["EconNoCash"] = "You do not have enough cash to respect your skill tree.",
                ["EconErrorCash"] = "Error taking cash from your account.",
                ["SRNotLoaded"] = "ServerRewards is not loaded. Contact your administrator.",
                ["SRNoPoints"] = "You do not have enough points to respect your skill tree.",
                ["SRPointError"] = "Error taking points from your account.",
                ["PaidRespec"] = "You paid {0} to respec.",
                ["NoPermsChat"] = "You do not permissions to use the skill tree chat command.",
                ["GiveXPUsage"] = "Usage: givexp <player> <amount>",
                ["ResetXPUsage"] = "Usage: /resetdata <player>",
                ["ResetXPDebtUsage"] = "Usage: stresetxpdebt <id>",
                ["XPLastArg"] = "XP amount required as the last argument.",
                ["GaveXP"] = "You were given {0} xp by {1}",
                ["ReceivedXP"] = "You gave {0} {1} xp.",
                ["NoPermsXP"] = "You do not have permission to gain xp on this server.",
                ["PrintXPNone"] = "You are level {0} and have {1}/{2} xp.",
                ["Mining"] = "Mining",
                ["Woodcutting"] = "Woodcutting",
                ["Skinning"] = "Skinning",
                ["Harvesting"] = "Harvesting",
                ["Combat"] = "Combat",
                ["Medical"] = "Medical",
                ["Build_Craft"] = "Build Craft",
                ["Scavenging"] = "Scavenging",
                ["AccessReminder"] = "You can access the Skill Tree menu by typing: <color=#42f105>/{0}</color>",
                ["TurboToggleOn"] = "Toggled boat turbo on.",
                ["TurboToggleOff"] = "Toggled boat turbo off.",
                ["TurboInUse"] = "This boat is already being boosted.",
                ["RespecCost"] = "Respec Cost: <color=#ffb600>{0}</color>",
                ["RespecButton"] = "<color=#ffb600>Respec</color>",
                ["ResetData"] = "Reset the data for {0}.",
                ["ReceivedSP"] = "You received {0} skill points.\nNew available balance: {1}",
                ["GaveSP"] = "You gave {0} skill points to {1}.",
                ["GiveSPUsage"] = "Usage: givesp <player> <amount>",
                ["Rationed"] = "You managed to ration the <color=#ffb600>{0}</color> you just consumed.",
                ["PointsRefunded"] = "Your skill points have been refunded.",
                ["PointsRefundedAll"] = "Refunded all skill points.",
                ["NoPlayersSetup"] = "There are no players setup.",
                ["NodeSpawned"] = "A new node spawned in place of your old one thanks to your buff!",
                ["RespecNoCustom"] = "You do not have enough {0} for this.",
                ["UIToggleXP"] = "Toggle the xp indicator that is displayed when gaining xp.",
                ["ON"] = "ON",
                ["OFF"] = "OFF",
                ["UIToggleXPBar"] = "Toggle the xp pump bar.",
                ["RepositionBar"] = "Reposition the xp pump bar.",
                ["ToggleBagButton"] = "Toggle the ExtraPockets hud button.",
                ["UIClose"] = "CLOSE",
                ["UIChange"] = "CHANGE",
                ["UIPlayerSettings"] = "Player Settings",
                ["TrackWait"] = "You must wait {0} seconds before your next tracking attempt.",
                ["NoAnimals"] = "No animals were found!",
                ["TrackFresh"] = "You see fresh animal tracks leading {0}.",
                ["TrackOlder"] = "You see slightly older animal tracks leading {0}.",
                ["TrackOldest"] = "You see old animal tracks leading {0}.",
                ["LevelReward"] = "You received {0} {1} for reaching level {2}.",
                ["UISkillTree"] = "Skill Tree",
                ["UIBuffInformation"] = "Buff Information",
                ["UITreePointsSpent"] = "Tree Points Spent:",
                ["UITotalPointsSpent"] = "Total Points Spent:",
                ["UIAvailablePoints"] = "Available Points:",
                ["UIRestedXPPool"] = "Rested XP Pool:",
                ["UICurrentLevel"] = "Current Level:",
                ["UIXP"] = "XP:",
                ["ButtonPlayerSettings"] = "Player Settings",
                ["UICost"] = "<color=#ffb600>COST:</color> {0}",
                ["UIScrap"] = "scrap",
                ["UIPoints"] = "points",
                ["UIDollars"] = "$",
                ["UIAreYouSure"] = "Are you sure you want to respec your skills?",
                ["ButtonYes"] = "<color=#ffb600>YES</color>",
                ["ButtonNo"] = "<color=#ffb600>NO</color>",
                ["ToggleNotifications"] = "Receive notifications from the Skill Tree plugin when a buff triggers.",
                ["notificationsOff"] = "You will no longer receive notifications from buff triggers.",
                ["notificationsOn"] = "You will now receive notifications for buff triggers.",
                ["DisabledRegen"] = "Your regen has been disabled for <color=#ff8000>{0} seconds</color> after taking damage.",
                ["UIMaxLevel"] = "Maximum Level: <color=#ffb600>{0}</color>",
                ["UISelectedNode"] = "<color=#f481fa>{0}</color>",
                ["RestedNotification"] = "You feel rested and have a bonus xp rate of <color=#00b2ff>{0}%</color> for <color=#00b2ff>{1}</color> xp.",
                ["SharkStomachFound"] = "You find {0} <color=#42f105>{1}</color> in the sharks stomach.",
                ["some"] = "some",
                ["a"] = "a",
                ["HarvestUltiCDNotification"] = "Your harvesting ultimate is now on cooldown for {0} seconds.",
                ["BuildCraftFailNotify"] = "Your BuildCraft ultimate failed to unlock the door.",
                ["Build_Craft_Ultimate_DescriptionAddition"] = ".\n<color=#db03cb>Success chance per swipe:</color> {0}%",
                ["Harvesting_Ultimate_DescriptionAddition"] = ".\n<color=#db03cb>Cooldown:</color> {0} seconds",
                ["UltimateSettingsUIDescription"] = "Toggle your {0} Ultimate buff on or off",
                ["Build_Craft_formatted"] = "Build & Craft",
                ["UltimateToggleOnMining"] = "You can locate nodes within <color=#DFF008>{0}m</color> using the chat command: <color=#DFF008>{1}</color> once every <color=#DFF008>{2}</color> seconds.",
                ["UltimateToggleOnVehicle"] = "Your mounted vehicle will take <color=#DFF008>{0}%</color> less damage from all sources.",
                ["UltimateToggleOnMedical"] = "You will now have a <color=#DFF008>{0}%</color> chance of resurrecting at your place of death when you click the RESURRECT button on the death screen.",
                ["UltimateToggleOnHarvester"] = "Your plants will now be deployed with your desired gene set. Type <color=#DFF008>/{0}</color> to set your desired genes. Cooldown: <color=#DFF008>{1} seconds</color>.",
                ["UltimateToggleOnBuildCraft"] = "You can now use any coloured key card on a swipe card reader to access a door. Power is still required. Success chance: <color=#DFF008>{0}%</color>",
                ["UltimateToggleOnWoodcutting"] = "You will now cut down any tree in a <color=#DFF008>{0}m</color> radius.",
                ["UltimateToggleOnRaiding"] = "You can now use the <color=#DFF008>/{0}</color> command to call in an MLRS strike. Cooldown: <color=#DFF008>{1} minutes</color>.",
                ["UltimateToggleOnCooking"] = "You can now use the <color=#DFF008>/{0}</color> command to apply temporary tea buffs to yourself. Cooldown: <color=#DFF008>{1} minutes</color>.",
                ["UltimateToggleOnScavengers"] = "You will now receive recycled components whenever you destroy a barrel.",
                ["UltimateSettings"] = "<color=#ffb600>Ultimate Settings</color>",
                ["BlacklistedItemsFound"] = "Dropped black listed items to the floor: \n{0}",
                ["WhitelistedItemsNotFound"] = "Dropped non-white listed items to the floor: \n{0}",
                ["PumpBarLevelText"] = "<color=#fbff00>Lv.{0}:</color>",
                ["PumpBarDebtTitleText"] = "<color=#fbff00>DEBT:</color>",
                ["PumpBarXPText"] = "<color=#FFFFFF>{0} / {1}</color>",
                ["PumpBarXPTextDebt"] = "<color=#FFFFFF>{0}</color>",
                ["UINextArrow"] = "> >",
                ["UIBackArrow"] = "< <",
                ["FailReload"] = "SkillTree failed to find your player data. Please reconnect to the server...",
                ["TargetFailReload"] = "SkillTree failed to find the target players data. They will need to reconnect to the server for this command to work...",
                ["stgiveitemUsage"] = "Usage: /stgiveitem <target player id> <shortname> <quantity> <skin ID> <Optional: displayName>",
                ["stgiveitemInvalidID"] = "ID: {0} is invalid.",
                ["stgiveitemNoPlayerFound"] = "No player found that matched ID: {0}",
                ["stgiveitemInvalidShortname"] = "Shortname: {0} is invalid.",
                ["stgiveitemQuantityInvalid"] = "Quantity: {0} is invalid.",
                ["stgiveitemSkinInvalid"] = "Skin ID: {0} is invalid.",
                ["popupxpstring"] = "<color=#{0}>+{1} XP</color>",
                ["RegrowthProc"] = "You finish cutting the tree down and it instantly grows back!",
                ["NotifyLevelGained"] = "You have reached level: {0}. Available points: {1}.",
                ["RespecMultiplierMessage"] = "\n<size=10><color=#FF0000>This will increase the cost of your next respec by {0}%</color></size>",
                ["Buff.Durability_Extended.Description"] = "\nExcludes: {0}",
                ["Buff.Raiding_Ultimate_Extended.Description"] = "\n<color=#db03cb>Command:</color> /{0}\n<color=#db03cb>Cooldown:</color> {1} minutes",
                ["Buff.Raiding_Ultimate_Ammo_Extended.Description"] = "\n<color=#db03cb>Requires:</color> {0}x MLRS rockets",
                ["OnlyWorksWithRaidableBases"] = "\n<color=#ff7400>Only works with raidable bases.</color>",

                ["Buff.Personal_Explosive_Reduction_Extended.Description.ReducesFireDamage"] = "\n<color=#ffff00>Reduces damage from fire:</color> {0}",
                ["UIExcludesList"] = "\n<color=#ffff00>Exludes:</color> {0}",


                ["Buff.Double_Explosion_Chance_Extended.Description"] = "\n<size=10><color=#ff7400>Excludes munitions from the MLRS ultimate.</color></size>",

                ["Buff.Lock_Picker_Extended.Description.Command"] = "\n<color=#ffff00>Command:</color> /{0}",
                ["Buff.Lock_Picker_Extended.Description.Cooldown"] = "\n<color=#ffff00>Cooldown:</color> {0} seconds",


                ["Buff.Mining_Yield_Extended.Description"] = "\n<size=10><color=#ffff00>Jackhammer bonus yield modifier:</color> {0}{1}%</size>",
                ["Buff.Woodcutting_Yield.Description"] = "\n<size=10><color=#ffff00>Chainsaw bonus yield modifier:</color> {0}{1}%</size>",
                ["Buff.Skinning_Yield_Extended.Description"] = "\n<size=10><color=#ffff00>Powertool bonus yield modifier:</color> {0}{1}%</size>",
                ["Buff.Forager.Description"] = "\n<color=#ffff00>Command:</color> /{0}\n<color=#ffff00>Cooldown:</color> {1} seconds",
                ["Buff.Trap_Spotter.Description"] = "\n<color=#ffff00>Command:</color> /{0}\n<color=#ffff00>Cooldown:</color> {1} seconds",

                ["MiningLuckModifierDescription"] = "\n<size=10><color=#ffff00>Jackhammer luck modifier:</color> {0}{1}%</size>",
                ["WoodcuttingLuckModifierDescription"] = "\n<size=10><color=#ffff00>Chainsaw luck modifier:</color> {0}{1}%</size>",
                ["SkinningLuckModifierDescription"] = "\n<size=10><color=#ffff00>Power tool luck modifier:</color> {0}{1}%</size>",

                ["Buff.Cooldown"] = "\n<color=#ffff00>Cooldown:</color> {0} {1}",
                ["Seconds"] = "seconds",
                ["Buff.HealthRegen.Delay"] = "\n<color=#ffff00>Damage activation delay:</color> {0} seconds",
                ["Buff.PVP_Critical.Amount"] = "\n<color=#ffff00>Critical damage bonus:</color> {0}%",
                ["Buff.Loot_Pickup.MeleeOnly"] = "\n<color=#ffff00>Melee Only:</color> True",
                ["Buff.Loot_Pickup.Distance"] = "\n<color=#ffff00>Melee Only:</color> {0}m",
                ["Buff.Animal_Damage_Resist.Animals"] = "\n<color=#ffff00>Animals:</color> {0}",
                ["Buff.Excluded"] = "\n<color=#ffff00>Excludes: </color> {0}",
                ["RaidBehaviourExpiredMessage"] = "Your ultimate timer has expired.",
                ["RaidBehaviourSuccessMessage"] = "You have successfully acquired your target. Launch in progress...",
                ["Harvesting_Ultimate_Command"] = "\n<color=#db03cb>Command:</color> /{0}",
                ["Mining_Ultimate_Command"] = "\n<color=#db03cb>Command:</color> /{0}",
                ["DetectionText"] = "<size=10>Detection</size>",
                ["AccumulatedXPDebt"] = "You have accumulated <color=#fb2a00>{0}</color> of xp debt. Current Debt: <color=#fb2a00>{1}</color>.",
                ["UIDebtText"] = " [<color=#fb2a00>{0}</color>]",
                ["MoveXPDisabled"] = "Moving the xp bar has been disabled on this server.",
                ["CardSwipeCooldownMessage"] = "You were not awarded xp for swiping as you still have {0} seconds left on your cooldown.",
                ["LockPickStillActive"] = "You still have your Lock Pick ability active. Attempt to open a locked door that you are not authorized on before the timer runs out.",
                ["LockPickCooldown"] = "Your lock pick ability is still on cooldown.",
                ["LockPickActivated"] = "You have activated your Lock Pick ability. It will remain active for {0} seconds or until you attempt to open a locked entity that you do not have access to.",
                ["LockPickSuccess"] = "You manage to pick the lock and break into the door. Ability is on cooldown for {0} seconds.",
                ["LockPickFailed"] = "You failed to pick the lock. You can attempt another lock pick in {0} seconds.",
                ["ExtendedTeaDurationMessage"] = "Your buff increased the duration of the tea's effects:",
                ["ExtendedTeaDurationMessageBody"] = "\n- <color=#42f105>{0}</color>: {1} minutes.",
                ["TeaFound"] = "You found {0}x {1} in the crate.",
                ["MinLevelColHasLevel"] = "<color=#76de07>",
                ["MinLevelColUnderLevel"] = "<color=#de3807>",
                ["MinLevelString"] = "Requires Level: {0}</color>",
                ["MinPointString"] = "Requires Total Points Spent: {0}</color>",
                ["TimeLeftPicklock"] = "<color=#ffb600>Pick time remaining: {0}</color>",
                ["MLRSTimeLeft"] = "<color=#ffb600>Strike time remaining: {0}</color>",
                ["FailMinLevel"] = "You must be level at least {0} to unlock nodes on this tree.",
                ["FailMinPointsSpent"] = "You must have spent at least {0} to unlock nodes on this tree.",
                ["MiningUltimateCooldownMessage"] = "You are still on cooldown from the last time you used this ability. Please wait {0} seconds before trying again.",
                ["hemp-collectable"] = "Hemp",
                ["corn-collectable"] = "Corn",
                ["pumpkin-collectable"] = "Pumpkin",
                ["potato-collectable"] = "Potato",
                ["berry-blue-collectable"] = "Blue Berry",
                ["berry-green-collectable"] = "Green Berry",
                ["berry-yellow-collectable"] = "Yellow Berry",
                ["berry-red-collectable"] = "Red Berry",
                ["berry-white-collectable"] = "White Berry",
                ["diesel_collectable"] = "Diesel",
                ["mushroom-cluster-5"] = "Mushroom",
                ["mushroom-cluster-6"] = "Mushroom",
                ["sulfur-collectable"] = "Sulfur",
                ["metal-collectable"] = "Metal",
                ["stone-collectable"] = "Stone",
                ["wood-collectable"] = "Wood",
                ["Wood_Yield"] = "Wood Yield",
                ["Ore_Yield"] = "Ore Yield",
                ["Radiation_Resistance"] = "Radiation Resistance",
                ["Radiation_Exposure_Resistance"] = "Radiation Exposure Resistance",
                ["Max_Health"] = "Max Health",
                ["Scrap_Yield"] = "Scrap Yield",
                ["Cooking_Ultimate_Description"] = "<size=10><color=#db03cb>ULTIMATE:</color> Provides you with the following mods when used:\n</size>",
                ["CookingUltimateToggleOffCooldownFail"] = "Your buff is off cooldown, so you cannot toggle off your tea buffs.",
                ["CookingUltimateCooldownReminder"] = "You cannot use this ultimate again for another <color=#ffae00>{0}</color>.",
                ["CookingUltimateNotUnlocked"] = "This abilitiy requires the cooking ultimate to be unlocked.",
                ["CookingUltimateNotEnabled"] = "This ultimate is disabled. Enable it through the skill tree menu, under {0}.",
                ["CookingUltimateMod"] = "- {0}: <color=#61e500>+{1}%</color>\n",
                ["CombatUltimateScientists"] = "<color=#42f105>Scientists</color>",
                ["CombatUltimateAnimals"] = "<color=#42f105>Animals</color>",
                ["CombatUltimatePlayers"] = "<color=#42f105>Players</color>",
                ["CookingUltimateDescriptionSize"] = "<size=7>",
                ["CookingUltimateDescriptionBottom"] = "</size><size=10><color=#db03cb>Command:</color> /{0}\n<color=#db03cb>Cooldown:</color> {1} minutes</size>",
                ["UltimateSettingsButton"] = "Ultimate Settings",
                ["AppliedTeaMessage"] = "Applied the following tea mods:\n",
                ["AppliedTeaMessageModString"] = "<color=#db03cb>Mod:</color> <color=#aaad0d>{0} [<color=#42f105>{1}%</color>]</color> - <color=#db03cb>Duration:</color> <color=#aaad0d>{2} minutes.</color>\n",
                ["TrapSpotterNotUnlocked"] = "You must have the Trap Spotter buff to use this command.",
                ["TrapSpotterCooldownReminder"] = "You cannot use this ability again for another <color=#ffae00>{0}</color>.",
                ["flameturret.deployed"] = "Flame Turret",
                ["autoturret_deployed"] = "Auto Turret",
                ["spikes.floor"] = "Floor Spikes",
                ["teslacoil.deployed"] = "Teslacoil",
                ["beartrap"] = "Bear Trap",
                ["landmine"] = "Landmine",
                ["guntrap.deployed"] = "Shotgun Trap",
                ["Trap"] = "Trap",
                ["UnderwaterDamageBonusPVP"] = "<color=#efda0a>PVP enabled:</color> {0}",
                ["PermUpdateDelay"] = "<color=#ff8700>Your Skill Tree permissions were recently changed. This may take up to 10 seconds to reflect.</color>",
                ["UINoUltimatesUnlocked"] = "you do not have any ultimate abilities unlocked",
                //"boar", "horse", "stag", "chicken", "wolf", "bear", "scarecrow", "polarbear"
                ["boar"] = "Boar",
                ["horse"] = "Horse",
                ["stag"] = "Stag",
                ["chicken"] = "Chicken",
                ["wolf"] = "Wolf",
                ["bear"] = "Bear",
                ["scarecrow"] = "Scarecrow",
                ["polarbear"] = "Polar Bear",
                ["SkinningUltimateToggleText"] = "You will now receive perks when killing a: <color=#DFF008>{0}.</color>",
                ["CombatUltimateAnd"] = " and ",
                ["CombatUltimateToggleOnMessage"] = "You will now receive <color=#DFF008>{0}%</color> of the damage as health when damaging {1}.",
                ["RaidingUltimateNoFreeSlot"] = "You must have 1 free inventory slot available in your belt.",
                ["RaidingUltimateNotUnlocked"] = "This command requires the Raiding Ultimate to be unlocked.",
                ["RaidingUltimateAlreadyActive"] = "This ability is already active.",
                ["RaidingMissingAmmo"] = "You do not have enough MLRS rockets in your inventory. Requires: {0}",
                ["RaidingUltimateCooldown"] = "You cannot use this ultimate again for another <color=#ffae00>{0}</color>.",
                ["minutes"] = "minutes",
                ["seconds"] = "seconds",
                ["UIBinocularsMessage"] = "LOOK THROUGH YOUR <color=#ffb600>BINOCULARS</color> AND HOLD <color=#ffb600>E</color> TO SET YOUR TARGET",
                ["RaidingUltimateResetTimeout"] = "Your strike cooldown has been reset as it timed out.",
                ["RaidingUltimateChatInstructions"] = "You have received some binoculars. Use them to set your target.",
                ["UIUnlocksIn"] = "<color=#f5d800>Unlocks in:</color>",
                ["UILevelUpButton"] = "<color=#ffb600>Level Up</color>",
                ["UIEnabled"] = "Enabled",
                ["metal"] = "metal",
                ["stone"] = "stone",
                ["sulfur"] = "sulfur",

                ["ForageBuffCooldown"] = "You are still on cooldown for another {0} seconds.",
                ["BagCooldownMsg"] = "You must wait {0} seconds before attempting to open your bag again.",
                ["NeedBagBuff"] = "You need to have the Extra Pockets buff in order to access this pouch.",
                ["DudExplodedAnyway"] = "You explosive was a dud, but it exploded anyway!",
                ["UltimateDisabledMessage"] = "Ultimate: {0} has been disabled.",
                ["DisableNoclipCommand"] = "You cannot use this command while in noclip.",
                ["RequireHarvestingUltimateMsg"] = "You need to have unlocked the Harvesting ultimate in order to use this command.",
                ["AnimalBuffDescription_Chicken"] = "You feel the power of the chicken flow through you. You can no longer receive fall damage.",
                ["AnimalBuffDescription_Bear"] = "You feel the power of the bear flow through you. Scientists cower in fear and will not engage unless engaged.",
                ["AnimalBuffDescription_Wolf"] = "You feel the power of the wolf flow through you. Healing feels more potent with friends around.",
                ["AnimalBuffDescription_Boar"] = "You feel the power of the boar flow through you. You may find some useful stuff when collecting wild berries and mushrooms.",
                ["AnimalBuffDescription_Stag"] = "You feel the power of the stag flow through you. You feel incredibly alert to nearby player threats.",
                ["AnimalBuffDescription_PolarBear"] = "You feel the power of the polar bear flow through you. It has wrapped you in a thick, damage absorbing skin.",
                ["AnimalBuffFinishedMsg"] = "You feel the power of the {buff} leave you...",
                ["BoarLootMsg"] = "You find something burried under the {0}...",
                ["Mushroom"] = "mushroom",
                ["BerryBush"] = "berry bush",
                ["RocketStrikeCooldownMsg"] = "The server does not allow the MLRS strike to be used so close to wipe. This ability will be available in {0}",
                ["Hours"] = "hours",
                ["Minutes"] = "minutes",
                ["Seconds"] = "seconds",
                ["BinocularGiveFail"] = "Failed to give binoculars due to stacking issues.",
                ["StrikeCancelledDueToPreviousStrike"] = "Strike cancelled - location too closed to a previous strike zone. Available in: {0} minutes.",
                ["PouchItemsRemoved"] = "Your items were removed from your pouch and returned to you.",
                ["BCToggleOff"] = "Toggled better chat titles off.",
                ["BCToggleOn"] = "Toggled better chat titles on.",
            };

            Dictionary<string, string> langMessages = new Dictionary<string, string>();
            foreach (var kvp in DefaultMessages)
            {
                if (!langMessages.ContainsKey(kvp.Key)) langMessages.Add(kvp.Key, kvp.Value);
            }
            foreach (var kvp in buffMessages)
            {
                if (!langMessages.ContainsKey(kvp.Key)) langMessages.Add(kvp.Key, kvp.Value);
            }
            foreach (var kvp in TitleMessages)
            {
                if (!langMessages.ContainsKey(kvp.Key)) langMessages.Add(kvp.Key, kvp.Value);
            }
            foreach (var animal in config.ultimate_settings.ultimate_skinning.enabled_buffs?.Keys ?? DefaultAnimalBuffs.Keys)
            {
                var animalStr = animal.ToString();
                if (!langMessages.ContainsKey(animalStr)) langMessages.Add(animalStr, animalStr);
            }

            lang.RegisterMessages(langMessages, this);
            DefaultMessages.Clear();
            buffMessages.Clear();
            TitleMessages.Clear();
        }

        #endregion

        #region Hooks
        void OnUserPermissionGranted(string id, string permName) => UpdatePlayerPerms(id, permName, true);
        void OnUserPermissionRevoked(string id, string permName) => UpdatePlayerPerms(id, permName, false);
        void OnGroupPermissionGranted(string name, string permName) => UpdatePlayersInGroup(name, permName);
        void OnGroupPermissionRevoked(string name, string permName) => UpdatePlayersInGroup(name, permName);

        void OnUserGroupAdded(string id, string groupName) => UpdatePlayerPermsOnGroupMove(id, groupName);
        void OnUserGroupRemoved(string id, string groupName) => UpdatePlayerPermsOnGroupMove(id, groupName);

        void UpdatePlayerPermsOnGroupMove(string id, string groupName)
        {
            foreach (var permName in permission.GetGroupPermissions(groupName))
            {
                if (!permName.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var perm in config.trees.Keys)
                {
                    if (permName.Equals("skilltree." + perm.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        var player = BasePlayer.Find(id);
                        if (player != null) Player.Message(player, lang.GetMessage("PermUpdateDelay", this, player.UserIDString), config.misc_settings.ChatID);
                        QueueInstanceDataUpdate(id);
                        return;
                    }
                }
            }
        }

        void UpdatePlayerPerms(string id, string permName, bool granted)
        {
            if (!permName.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase)) return;
            foreach (var perm in config.trees.Keys)
            {
                if (permName.Equals("skilltree." + perm.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var player = BasePlayer.Find(id);
                    if (player != null) Player.Message(player, lang.GetMessage("PermUpdateDelay", this, player.UserIDString), config.misc_settings.ChatID);
                    QueueInstanceDataUpdate(id);
                    return;
                }
            }
            if (permName.Equals("skilltree.all", StringComparison.OrdinalIgnoreCase)) QueueInstanceDataUpdate(id);
            else if (TrackedPermissionPerms.Contains(permName)) QueueInstanceDataUpdate(id);
        }

        void UpdatePlayersInGroup(string name, string permName)
        {
            if (!permName.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase)) return;
            foreach (var perm in config.trees.Keys)
            {
                if (permName.Equals("skilltree." + perm.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var user in permission.GetUsersInGroup(name))
                    {
                        var id = user.Split(' ')[0];
                        var player = BasePlayer.Find(id);
                        if (player != null) Player.Message(player, lang.GetMessage("PermUpdateDelay", this, player.UserIDString), config.misc_settings.ChatID);
                        QueueInstanceDataUpdate(id);
                    }
                    return;
                }
            }
            if (permName.Equals("skilltree.all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var user in permission.GetUsersInGroup(name))
                {
                    QueueInstanceDataUpdate(user.Split(' ')[0]);
                }
                return;
            }
            else if (TrackedPermissionPerms.Contains(permName))
            {
                foreach (var user in permission.GetUsersInGroup(name))
                {
                    QueueInstanceDataUpdate(user.Split(' ')[0]);
                }
                return;
            }
        }

        void UpdatePlayerPerms(string id)
        {
            var player = BasePlayer.Find(id);
            if (player != null)
            {
                CuiHelper.DestroyUi(player, "SkillTree");
                CuiHelper.DestroyUi(player, "respec_confirmation");
                CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
                CuiHelper.DestroyUi(player, "NavigationMenu");
                DoClear(player);
                LoggingOff(player);
                HandleNewConnection(player);
            }
        }

        Timer InstanceDataUpdateTimer;
        List<string> InstanceDataPlayerQueue = new List<string>();

        void QueueInstanceDataUpdate(string playerID)
        {
            if (!InstanceDataPlayerQueue.Contains(playerID)) InstanceDataPlayerQueue.Add(playerID);
            if (InstanceDataUpdateTimer == null || InstanceDataUpdateTimer.Destroyed)
            {
                Puts("Initializing timer to update node permissions. Permissions will update in 10 seconds.");
                InstanceDataUpdateTimer = timer.Once(10f, () =>
                {
                    foreach (var id in InstanceDataPlayerQueue)
                        try
                        {
                            UpdatePlayerPerms(id);
                        }
                        catch (Exception ex)
                        {
                            Puts($"Failed to update data for {id}. Exception: {ex.Message}");
                        }

                    InstanceDataPlayerQueue.Clear();
                });
            }
        }

        Dictionary<ulong, BaseProjectile> ModifiedWeapons = new Dictionary<ulong, BaseProjectile>();

        bool CanModifyMagazine(BaseProjectile weapon)
        {
            if (config.tools_black_white_list_settings.extendedMag_weapon_blacklist.Contains(weapon.ShortPrefabName)) return false;
            if (weapon.primaryMagazine != null && config.tools_black_white_list_settings.extendedMag_ammotype_blacklist.Contains(weapon.primaryMagazine.ammoType.shortname)) return false;

            return true;
        }

        object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (type != AntiHackType.FlyHack) return null;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd) || bd.buff_values.ContainsKey(Buff.Fall_Damage_Reduction)) return null;
            return true;
        }

        object OnWeaponReload(BaseProjectile weapon, BasePlayer player)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd)) return null;

            if (!CanModifyMagazine(weapon))
            {
                RemoveMods(weapon, 0);
                return null;
            }

            float value;
            if (!bd.buff_values.TryGetValue(Buff.Extended_Mag, out value))
            {
                if (ModifiedWeapons.ContainsKey(weapon.net.ID.Value))
                {
                    RemoveMods(weapon, 0);
                }
                return null;
            }
            if (!ModifiedWeapons.ContainsKey(weapon.net.ID.Value)) ModifiedWeapons.Add(weapon.net.ID.Value, weapon);
            int num = Mathf.CeilToInt(ProjectileWeaponMod.Mult(weapon, (ProjectileWeaponMod x) => x.magazineCapacity, (ProjectileWeaponMod.Modifier y) => y.scalar, 1f) * (float)weapon.primaryMagazine.definition.builtInSize);
            var totalAmmoCapacity = num + Convert.ToInt32(weapon.primaryMagazine.definition.builtInSize * value);
            weapon.primaryMagazine.capacity = totalAmmoCapacity;

            return null;
        }

        object OnWeaponModChange(BaseProjectile weapon, BasePlayer player)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;

            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd)) return null;

            if (!CanModifyMagazine(weapon))
            {
                RemoveMods(weapon, 0);
                return null;
            }

            float value;
            if (!bd.buff_values.TryGetValue(Buff.Extended_Mag, out value))
            {
                RemoveMods(weapon, 0);
                return null;
            }

            int num = Mathf.CeilToInt(ProjectileWeaponMod.Mult(weapon, (ProjectileWeaponMod x) => x.magazineCapacity, (ProjectileWeaponMod.Modifier y) => y.scalar, 1f) * (float)weapon.primaryMagazine.definition.builtInSize);

            var totalAmmoCapacity = num + Convert.ToInt32(weapon.primaryMagazine.definition.builtInSize * value);


            var oldMagCapacity = weapon.primaryMagazine.capacity;

            weapon.primaryMagazine.capacity = totalAmmoCapacity;

            if (oldMagCapacity > weapon.primaryMagazine.capacity)
            {
                weapon.SendNetworkUpdateImmediate();
                RemoveMods(weapon, weapon.primaryMagazine.capacity - num, false);
            }
            else if (oldMagCapacity != weapon.primaryMagazine.capacity)
                weapon.SendNetworkUpdateImmediate();

            return true;
        }

        void RemoveMods(BaseProjectile weapon, int extraAmmo, bool removeFromDic = true, bool forceUnload = true)
        {
            DelayedModsChanged(weapon, extraAmmo, forceUnload);
            if (removeFromDic) ModifiedWeapons.Remove(weapon.net.ID.Value);
        }

        public void DelayedModsChanged(BaseProjectile weapon, int extraAmmoCapacity = 0, bool unloadModifiedWeapons = true)
        {
            int num = Mathf.CeilToInt(ProjectileWeaponMod.Mult(weapon, (ProjectileWeaponMod x) => x.magazineCapacity, (ProjectileWeaponMod.Modifier y) => y.scalar, 1f) * (float)weapon.primaryMagazine.definition.builtInSize) + extraAmmoCapacity;
            if (num == weapon.primaryMagazine.capacity && weapon.primaryMagazine.contents <= num)
            {
                return;
            }
            if (weapon.primaryMagazine.contents > 0 && weapon.primaryMagazine.contents > num && unloadModifiedWeapons)
            {
                int contents = weapon.primaryMagazine.contents;
                BasePlayer player = weapon.GetOwnerPlayer();
                ItemContainer itemContainer = null;
                if (player != null)
                {
                    itemContainer = player.inventory.containerMain;
                }
                else if (weapon.GetCachedItem() != null)
                {
                    itemContainer = weapon.GetCachedItem().parent;
                }

                weapon.primaryMagazine.contents = 0;
                if (itemContainer != null)
                {
                    Item item = ItemManager.Create(weapon.primaryMagazine.ammoType, contents, 0uL);
                    if (!item.MoveToContainer(itemContainer))
                    {
                        Vector3 vPos = weapon.transform.position;
                        if (itemContainer.entityOwner != null)
                        {
                            vPos = itemContainer.entityOwner.transform.position + Vector3.up * 0.25f;
                        }

                        item.Drop(vPos, Vector3.up * 5f);
                    }
                }
            }
            weapon.primaryMagazine.capacity = num;
            weapon.SendNetworkUpdate();
        }

        object CanTakeCutting(BasePlayer player, GrowableEntity plant)
        {
            BuffDetails bd;
            float value;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.TryGetValue(Buff.Harvest_Grown_Yield, out value) || Interface.CallHook("STCanReceiveYield", player, plant) != null) return null;

            var num = (plant.Properties.BaseCloneCount + plant.Genes.GetGeneTypeCount(GrowableGenetics.GeneType.Yield) / 2) * value;

            var guaranteedYield = Convert.ToInt32(Math.Truncate(num));
            var rolledYield = Convert.ToSingle(num - guaranteedYield);
            if (rolledYield > 0 && UnityEngine.Random.Range(0f, 1f) >= 1 - rolledYield) guaranteedYield++;

            if (guaranteedYield <= 0) return null;
            var item = ItemManager.Create(plant.Properties.CloneItem, guaranteedYield);
            GrowableGeneEncoding.EncodeGenesToItem(plant, item);

            player.GiveItem(item);
            return null;
        }

        object OnTreeMarkerHit(TreeEntity tree, HitInfo info)
        {
            if (info == null || info.InitiatorPlayer == null || info.InitiatorPlayer.IsNpc || !info.InitiatorPlayer.userID.IsSteamId()) return null;

            BuffDetails bd;
            if (!buffDetails.TryGetValue(info.InitiatorPlayer.userID, out bd)) return null;

            float value;
            if (!bd.buff_values.TryGetValue(Buff.Woodcutting_Hotspot, out value)) return null;

            if (value < 1 && !RollSuccessful(value)) return null;

            return true;
        }

        // Credit to Nivex
        void OnMeleeAttack(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || !player.userID.IsSteamId() || !(info.HitEntity is OreResourceEntity)) return;

            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd)) return;

            float value;
            if (!bd.buff_values.TryGetValue(Buff.Mining_Hotspot, out value)) return;

            if (value < 1 && !RollSuccessful(value)) return;

            var ore = info.HitEntity as OreResourceEntity;
            if (ore._hotSpot != null)
            {
                ore._hotSpot.transform.position = info.HitPositionWorld;
                ore._hotSpot.SendNetworkUpdateImmediate(false);
            }
            else ore._hotSpot = ore.SpawnBonusSpot(info.HitPositionWorld);
        }

        void SetPicker(BasePlayer player)
        {
            if (ActivePickers.ContainsKey(player))
            {
                //Player.Message(player, lang.GetMessage("LockPickStillActive", this, player.UserIDString));
                Player.Message(player, lang.GetMessage("LockPickStillActive", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            float value;
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.TryGetValue(Buff.Lock_Picker, out value))
            {
                if (LockPickCooldowns.ContainsKey(player.userID))
                {
                    if (LockPickCooldowns[player.userID] > Time.time)
                    {
                        Player.Message(player, lang.GetMessage("LockPickCooldown", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                    LockPickCooldowns[player.userID] = Time.time + config.buff_settings.raid_perk_settings.Lock_Picker_settings.use_delay;
                }
                else LockPickCooldowns.Add(player.userID, Time.time + config.buff_settings.raid_perk_settings.Lock_Picker_settings.use_delay);

                ActivePickerClass pickerData;
                if (!ActivePickers.TryGetValue(player, out pickerData)) ActivePickers.Add(player, pickerData = new ActivePickerClass(value));
                else pickerData.chance = value;

                Player.Message(player, string.Format(lang.GetMessage("LockPickActivated", this, player.UserIDString), config.buff_settings.raid_perk_settings.Lock_Picker_settings.time), config.misc_settings.ChatID);

                var timeLeft = Convert.ToInt32(config.buff_settings.raid_perk_settings.Lock_Picker_settings.time);
                pickerData.timer = timer.Every(config.buff_settings.raid_perk_settings.Lock_Picker_settings.timer_tick_rate, () =>
                {
                    timeLeft -= config.buff_settings.raid_perk_settings.Lock_Picker_settings.timer_tick_rate;
                    if (timeLeft <= 0) DestroyPicker(player, pickerData);
                    else if (config.buff_settings.raid_perk_settings.Lock_Picker_settings.show_timer) PendingTimer(player, string.Format(lang.GetMessage("TimeLeftPicklock", this, player.UserIDString), timeLeft));
                });
            }
        }

        public class ActivePickerClass
        {
            public float chance;
            public Timer timer;

            public ActivePickerClass(float chance)
            {
                this.chance = chance;
            }
        }

        private void PendingTimer(BasePlayer player, string text)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "PendingTimer",
                Parent = "Hud",
                Components = {
                    new CuiTextComponent { Text = text, Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = config.misc_settings.timeLeft_anchor.anchorMin, AnchorMax = config.misc_settings.timeLeft_anchor.anchorMax, OffsetMin = config.misc_settings.timeLeft_anchor.offsetMin, OffsetMax = config.misc_settings.timeLeft_anchor.offsetMax }
                }
            });

            CuiHelper.DestroyUi(player, "PendingTimer");
            CuiHelper.AddUi(player, container);
        }

        void DestroyPicker(BasePlayer player, ActivePickerClass pickerData, bool remove = true)
        {
            if (pickerData == null && !ActivePickers.TryGetValue(player, out pickerData)) return;

            if (pickerData.timer != null && !pickerData.timer.Destroyed) pickerData.timer.Destroy();
            if (remove) ActivePickers.Remove(player);
            CuiHelper.DestroyUi(player, "PendingTimer");
        }

        Dictionary<BasePlayer, ActivePickerClass> ActivePickers = new Dictionary<BasePlayer, ActivePickerClass>();
        Dictionary<ulong, float> LockPickCooldowns = new Dictionary<ulong, float>();
        object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            if (baseLock == null || player == null) return null;
            var ownerEntity = baseLock.GetParentEntity();
            if (ownerEntity == null) return null;
            if (!PassRaidableBasesCheck(ownerEntity, Buff.Lock_Picker)) return null;
            var hasAccess = baseLock is CodeLock ? (baseLock as CodeLock).whitelistPlayers.Contains(player.userID) : baseLock.HasLockPermission(player);
            ActivePickerClass pickerData;
            if (hasAccess || !baseLock.IsLocked() || !ActivePickers.TryGetValue(player, out pickerData)) return null;

            if (Interface.CallHook("STOnLockpickAttempt", player, baseLock) != null) return null;

            ActivePickers.Remove(player);
            if (RollSuccessful(pickerData.chance))
            {
                if (!string.IsNullOrEmpty(config.effect_settings.lockpick_success_effect)) EffectNetwork.Send(new Effect(config.effect_settings.lockpick_fail_effect, player.transform.position, player.transform.position), player.net.connection);
                Player.Message(player, string.Format(lang.GetMessage("LockPickSuccess", this, player.UserIDString), config.buff_settings.raid_perk_settings.Lock_Picker_settings.use_delay), config.misc_settings.ChatID);
                DestroyPicker(player, pickerData);
                if (config.buff_settings.raid_perk_settings.Lock_Picker_settings.unlock_entity) NextTick(() => baseLock.SetFlag(BaseEntity.Flags.Locked, false));
                return true;
            }
            Player.Message(player, string.Format(lang.GetMessage("LockPickFailed", this, player.UserIDString), config.buff_settings.raid_perk_settings.Lock_Picker_settings.use_delay), config.misc_settings.ChatID);
            if (!string.IsNullOrEmpty(config.effect_settings.lockpick_fail_effect)) EffectNetwork.Send(new Effect(config.effect_settings.lockpick_fail_effect, player.transform.position, player.transform.position), player.net.connection);
            if (config.buff_settings.raid_perk_settings.Lock_Picker_settings.damage_per_fail > 0)
            {
                player.Hurt(config.buff_settings.raid_perk_settings.Lock_Picker_settings.damage_per_fail);
            }
            DestroyPicker(player, pickerData);
            return null;
        }

        #region OnEntityTakeDamage

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || info.damageTypes == null || entity == null || info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Decay) return null;
            var AttackerIsRealPlayer = info.InitiatorPlayer != null && !info.InitiatorPlayer.IsNpc && info.InitiatorPlayer.userID.IsSteamId();
            BuffDetails bd;

            #region Trap

            if (IsTrap(entity) && AttackerIsRealPlayer)
            {
                float value;
                if (!buffDetails.TryGetValue(info.InitiatorPlayer.userID, out bd) || !bd.buff_values.TryGetValue(Buff.Trap_Damage_Increase, out value) || config.buff_settings.raid_perk_settings.trap_damage_increase_settings.blacklist.Contains(entity.ShortPrefabName) || !PassRaidableBasesCheck(entity, Buff.Trap_Damage_Increase)) return null;

                info.damageTypes.ScaleAll(1 + value);
                return null;
            }
            #endregion

            var damageType = info.damageTypes.GetMajorityDamageType();
            #region BaseAnimalNPC
            var animal = entity as BaseAnimalNPC;
            if (animal != null && info.InitiatorPlayer != null && info.InitiatorPlayer.userID.IsSteamId())
            {
                if (!buffDetails.TryGetValue(info.InitiatorPlayer.userID, out bd))
                {
                    if (info.InitiatorPlayer.IsConnected) Player.Message(info.InitiatorPlayer, lang.GetMessage("FailReload", this, info.InitiatorPlayer.UserIDString), config.misc_settings.ChatID);
                    return null;
                }

                float value;
                if (bd.buff_values.TryGetValue(Buff.Animal_NPC_Damage, out value))
                {
                    info.damageTypes.ScaleAll(1f + value);
                }

                if (config.ultimate_settings.ultimate_combat.animals_enabled && bd.buff_values.ContainsKey(Buff.Combat_Ultimate) && IsUltimateEnabled(info.InitiatorPlayer, Buff.Combat_Ultimate) && CanCombatUltimateTrigger(info.InitiatorPlayer, info, damageType))
                {
                    info.InitiatorPlayer.Heal(info.damageTypes.Total() * config.ultimate_settings.ultimate_combat.health_scale);
                }

                return null;
            }

            #endregion

            #region SimpleShark

            var shark = entity as SimpleShark;
            if (shark != null && AttackerIsRealPlayer)
            {
                if (!buffDetails.TryGetValue(info.InitiatorPlayer.userID, out bd)) return null;

                float value;
                if (bd.buff_values.TryGetValue(Buff.UnderwaterDamageBonus, out value) && IsUnderwater(info.InitiatorPlayer))
                {
                    info.damageTypes.ScaleAll(1f + value);
                }
                return null;
            }
            #endregion

            #region BasePlayer

            var player = entity as BasePlayer;
            if (player != null)
            {
                float value;
                if ((player.IsNpc || !player.userID.IsSteamId()) && info.InitiatorPlayer != null)
                {
                    var attackerPlayer = info.InitiatorPlayer;
                    if (attackerPlayer.IsNpc || !attackerPlayer.userID.IsSteamId()) return null;
                    //Confirmed attacker is real.
                    if (!buffDetails.TryGetValue(attackerPlayer.userID, out bd))
                    {
                        if (attackerPlayer.IsConnected) Player.Message(attackerPlayer, lang.GetMessage("FailReload", this, attackerPlayer.UserIDString), config.misc_settings.ChatID);
                        return null;
                    }
                    if (bd.buff_values.TryGetValue(Buff.Human_NPC_Damage, out value))
                    {
                        var scale = value;
                        info?.damageTypes?.ScaleAll(1f + scale);
                    }
                    if (config.ultimate_settings.ultimate_combat.scientists_enabled && bd.buff_values.ContainsKey(Buff.Combat_Ultimate) && IsUltimateEnabled(info.InitiatorPlayer, Buff.Combat_Ultimate) && CanCombatUltimateTrigger(info.InitiatorPlayer, info, damageType))
                    {
                        attackerPlayer.Heal(info.damageTypes.Total() * config.ultimate_settings.ultimate_combat.health_scale);
                    }
                    if (bd.buff_values.TryGetValue(Buff.UnderwaterDamageBonus, out value) && IsUnderwater(player))
                    {
                        info.damageTypes.ScaleAll(1f + value);
                    }
                    return null;
                }
                if (!buffDetails.TryGetValue(player.userID, out bd))
                {
                    if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                    return null;
                }
                if (bd == null) return null;
                if (bd.buff_values.TryGetValue(Buff.Trap_Damage_Reduction, out value) && IsTrap(info.Initiator) && !config.buff_settings.raid_perk_settings.trap_damage_reduction_settings.blacklist.Contains(info.Initiator.ShortPrefabName) && PassRaidableBasesCheck(info.Initiator, Buff.Trap_Damage_Reduction))
                {
                    var reducedValue = 1f - value;
                    if (reducedValue < 0) reducedValue = 0;
                    info.damageTypes.ScaleAll(reducedValue);
                    if (reducedValue > 0) AddRegenDelay(player);
                    HandleBearBuff(player, info, damageType);
                    return null;
                }
                var damage = info?.damageTypes?.GetMajorityDamageType();
                switch (damage)
                {
                    case Rust.DamageType.Thirst:
                    case Rust.DamageType.Hunger:
                        AddRegenDelay(player);
                        return null;
                    case Rust.DamageType.Cold:
                    case Rust.DamageType.ColdExposure:
                        if (bd.buff_values.ContainsKey(Buff.No_Cold_Damage))
                        {
                            player.metabolism.temperature.SetValue(20f);
                            info.damageTypes.ScaleAll(0f);
                            return null;
                        }
                        else AddRegenDelay(player);
                        return null;
                    case Rust.DamageType.Radiation:
                    case Rust.DamageType.RadiationExposure:
                        if (bd.buff_values.TryGetValue(Buff.Radiation_Reduction, out value))
                        {
                            var reducedValue = 1f - value;
                            if (reducedValue < 0) reducedValue = 0;
                            if (reducedValue == 0)
                            {
                                player.metabolism.radiation_level.SetValue(0);
                                player.metabolism.radiation_poison.SetValue(0);
                            }
                            info.damageTypes.ScaleAll(reducedValue);
                            if (reducedValue > 0) AddRegenDelay(player);
                        }
                        else AddRegenDelay(player);
                        return null;
                    case Rust.DamageType.Heat:
                        if (bd.buff_values.TryGetValue(Buff.Fire_Damage_Reduction, out value) || (config.buff_settings.raid_perk_settings.personal_explosive_reduction_settings.fire_damage_reduction && bd.buff_values.TryGetValue(Buff.Personal_Explosive_Reduction, out value) && info.InitiatorPlayer != null && info.InitiatorPlayer == player))
                        {
                            var reducedValue = 1f - value;
                            if (reducedValue < 0) reducedValue = 0;
                            info.damageTypes.ScaleAll(reducedValue);
                            if (reducedValue > 0) AddRegenDelay(player);
                        }
                        else AddRegenDelay(player);
                        return null;
                    case Rust.DamageType.Fall:
                        if (HasAnimalBuff(player, AnimalBuff.Chicken)) info.damageTypes.ScaleAll(0f);
                        else if (bd.buff_values.TryGetValue(Buff.Fall_Damage_Reduction, out value))
                        {
                            var reducedValue = 1f - value;
                            if (reducedValue < 0) reducedValue = 0;
                            if (reducedValue > 0) AddRegenDelay(player);
                            info.damageTypes.ScaleAll(reducedValue);
                        }
                        else AddRegenDelay(player);
                        HandleBearBuff(player, info, Rust.DamageType.Fall);
                        return null;
                }
                var attacker = info.Initiator;
                if (attacker == null) return null;
                if (bd.buff_values.TryGetValue(Buff.Animal_Damage_Resist, out value) && config.buff_settings.animals.Contains(attacker.ShortPrefabName))
                {
                    var reducedValue = 1f - value;
                    if (reducedValue < 0) reducedValue = 0;
                    info.damageTypes.ScaleAll(reducedValue);
                    if (reducedValue > 0) AddRegenDelay(player);
                    HandleBearBuff(player, info, damageType);
                    return null;
                }
                if (bd.buff_values.TryGetValue(Buff.SharkResistance, out value) && attacker is SimpleShark)
                {
                    var reducedValue = 1f - value;
                    if (reducedValue < 0) reducedValue = 0;
                    info.damageTypes.ScaleAll(reducedValue);
                    if (reducedValue > 0) AddRegenDelay(player);
                    HandleBearBuff(player, info, damageType);
                    return null;
                }
                var damageScale = 1f;
                var player_attacker = attacker as BasePlayer;
                if (player_attacker != null && AttackerIsRealPlayer)
                {
                    if (bd.buff_values.TryGetValue(Buff.Melee_Resist, out value))
                    {
                        var heldEntity = player_attacker.GetHeldEntity();
                        if (heldEntity != null && heldEntity is BaseMelee)
                        {
                            damageScale -= value;
                        }
                    }

                    if (bd.buff_values.TryGetValue(Buff.PVP_Shield, out value))
                    {
                        damageScale -= value;
                    }

                    // hurt self
                    if (player_attacker == player)
                    {
                        if (bd.buff_values.TryGetValue(Buff.Personal_Explosive_Reduction, out value) && (info.WeaponPrefab == null || !config.buff_settings.raid_perk_settings.personal_explosive_reduction_settings.blacklist.Contains(info.WeaponPrefab.ShortPrefabName)) && IsExplosivePrefab(info.WeaponPrefab, damageType))
                        {
                            var reducedValue = 1f - value;
                            if (reducedValue < 0) reducedValue = 0;
                            info.damageTypes.ScaleAll(reducedValue);
                            if (reducedValue > 0) AddRegenDelay(player);
                            HandleBearBuff(player, info, damageType);
                            return null;
                        }
                    }

                    BuffDetails abd;
                    if (buffDetails.TryGetValue(player_attacker.userID, out abd))
                    {
                        if (abd.buff_values.TryGetValue(Buff.PVP_Critical, out value) && RollSuccessful(value))
                        {
                            damageScale += UnityEngine.Random.Range(0.01f, config.buff_settings.pvp_critical_modifier);
                        }
                        if (abd.buff_values.TryGetValue(Buff.PVP_Damage, out value))
                        {
                            damageScale += value;
                        }

                        if (config.buff_settings.UnderwaterDamageBonus_pvp && abd.buff_values.TryGetValue(Buff.UnderwaterDamageBonus, out value) && IsUnderwater(player_attacker))
                        {
                            damageScale += value;
                        }

                        if (config.ultimate_settings.ultimate_combat.players_enabled && abd.buff_values.ContainsKey(Buff.Combat_Ultimate) && IsUltimateEnabled(info.InitiatorPlayer, Buff.Combat_Ultimate) && CanCombatUltimateTrigger(info.InitiatorPlayer, info, damageType))
                        {
                            player_attacker.Heal(info.damageTypes.Total() * config.ultimate_settings.ultimate_combat.health_scale);
                        }
                    }
                }
                var npc_attacker = attacker as NPCPlayer;
                if (npc_attacker != null)
                {
                    if (bd.buff_values.ContainsKey(Buff.Human_NPC_Defence))
                    {
                        damageScale -= bd.buff_values[Buff.Human_NPC_Defence];
                    }
                }
                if (damageScale < 0) damageScale = 0;
                if (damageScale != 1f) info.damageTypes.ScaleAll(damageScale);
                if (damageScale > 0) AddRegenDelay(player);
                HandleBearBuff(player, info, damageType);
                return null;
            }
            #endregion

            #region LootContainer

            var lootContainer = entity as LootContainer;
            if (lootContainer != null && AttackerIsRealPlayer)
            {
                if (lootContainer.ShortPrefabName == "trash-pile-1") return null;
                if (buffDetails.TryGetValue(info.InitiatorPlayer.userID, out bd))
                {
                    if (bd.buff_values.ContainsKey(Buff.Barrel_Smasher) && IsBarrel(lootContainer.ShortPrefabName)) info?.damageTypes?.ScaleAll(100f);
                    if (info?.damageTypes?.Total() >= lootContainer?.health)
                    {
                        if (bd.buff_values.ContainsKey(Buff.Extra_Scrap_Barrel) && IsBarrel(lootContainer.ShortPrefabName) && RollSuccessful(bd.buff_values[Buff.Extra_Scrap_Barrel]))
                        {
                            lootContainer.inventory.capacity++;
                            var item = ItemManager.CreateByName("scrap", UnityEngine.Random.Range(config.buff_settings.min_extra_scrap, config.buff_settings.max_extra_scrap + 1));
                            if (!item.MoveToContainer(lootContainer.inventory)) item.Remove();
                        }
                        if (bd.buff_values.ContainsKey(Buff.Component_Barrel) && IsBarrel(lootContainer.ShortPrefabName) && RollSuccessful(bd.buff_values[Buff.Component_Barrel]))
                        {
                            var itemDef = GetRandomItemDef(ItemCategory.Component);
                            var quantity = UnityEngine.Random.Range(config.buff_settings.min_components, config.buff_settings.max_components + 1);
                            AddItemsToBarrel(itemDef, quantity, lootContainer);
                        }
                        if (bd.buff_values.ContainsKey(Buff.Electronic_Barrel) && IsBarrel(lootContainer.ShortPrefabName) && RollSuccessful(bd.buff_values[Buff.Electronic_Barrel]))
                        {
                            var itemDef = GetRandomItemDef(ItemCategory.Electrical);
                            var quantity = UnityEngine.Random.Range(config.buff_settings.min_electrical_components, config.buff_settings.max_electrical_components + 1);
                            AddItemsToBarrel(itemDef, quantity, lootContainer);
                        }
                        List<Item> _containerItems = Pool.GetList<Item>();
                        _containerItems.AddRange(lootContainer.inventory.itemList);
                        if (bd.buff_values.ContainsKey(Buff.Scavengers_Ultimate) && IsUltimateEnabled(info.InitiatorPlayer, Buff.Scavengers_Ultimate))
                        {
                            foreach (var item in _containerItems)
                            {
                                ScrapItems(item, lootContainer);
                            }
                        }
                        Pool.FreeList(ref _containerItems);
                    }
                }

                return null;
            }
            #endregion

            #region BaseMountable

            var mountable = entity as BaseMountable;
            if (mountable != null)
            {
                #region BaseVehicle

                var vehicle = mountable as BaseVehicle;
                if (vehicle != null)
                {
                    var driver = vehicle.GetDriver();
                    if (driver == null) return null;
                    if (buffDetails.TryGetValue(driver.userID, out bd) && bd.buff_values.ContainsKey(Buff.Vehicle_Ultimate))
                    {
                        info.damageTypes.ScaleAll(1f - config.ultimate_settings.ultimate_vehicle.reduce_by);
                    }
                    return null;
                }

                #endregion

                if (entity.skinID == 444) info.damageTypes.ScaleAll(0f);
                return null;
            }
            #endregion

            return null;
        }

        bool IsExplosivePrefab(BaseEntity entity, Rust.DamageType damageType)
        {
            if (damageType == Rust.DamageType.Explosion || damageType == Rust.DamageType.AntiVehicle) return true;
            if (entity == null) return false;

            switch (entity.ShortPrefabName)
            {
                case "explosive.satchel.deployed":
                case "grenade.beancan.deployed":
                case "grenade.f1.deployed":
                case "grenade.flashbang.deployed":
                case "40mm_grenade_he":
                case "rocket_hv":
                    return true;

                default: return false;
            }
        }

        #endregion

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (action.Equals("gut", StringComparison.OrdinalIgnoreCase)) AwardXP(player, config.xp_settings.xp_sources.Gut_Fish, null, false, false, "gutting");
            return null;
        }

        object OnItemUse(Item item, int amountToUse)
        {
            var player = item.GetOwnerPlayer();
            if (player == null) return null;
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Rationer))
            {
                if (item.info.category == ItemCategory.Food && RollSuccessful(bd.buff_values[Buff.Rationer]) && !config.buff_settings.no_refund_item_skins.Contains(item.skin) && !item.info.shortname.StartsWith("fish.", StringComparison.Ordinal) && !item.info.shortname.StartsWith("clone.", StringComparison.Ordinal) && !item.info.shortname.StartsWith("seed.", StringComparison.Ordinal))
                {
                    if (!PassCookingChecks(item)) return null;
                    var refunded_item = ItemManager.CreateByName(item.info.shortname, amountToUse, item.skin);
                    if (item.name != null) refunded_item.name = item.name;
                    GiveItem(player, refunded_item);
                    //player.GiveItem(refunded_item);                   
                    if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("Rationed", this, player.UserIDString), item.name ?? item.info.displayName.english), config.misc_settings.ChatID);
                }
            }
            return null;
        }

        bool PassCookingChecks(Item item)
        {
            if (Cooking != null && Cooking.IsLoaded)
            {
                if (Convert.ToBoolean(Cooking.Call("IsCookingMeal", item))) return config.misc_settings.ration_cooking_meals;
                if (Convert.ToBoolean(Cooking.Call("IsCustomIngredient", item))) return false;
            }
            return true;
        }

        bool NotificationsOn(BasePlayer player)
        {
            if (!pcdData.pEntity.ContainsKey(player.userID) || pcdData.pEntity[player.userID].notifications) return true;
            return false;
        }

        void OnPlayerRevive(BasePlayer reviver, BasePlayer player)
        {
            if (reviver == null || player == null) return;
            BuffDetails bd;
            if (buffDetails.TryGetValue(reviver.userID, out bd) && bd.buff_values.ContainsKey(Buff.Reviver))
            {
                BasePlayer revived_player = player;
                NextTick(() =>
                {
                    if (revived_player == null) return;
                    var healthFor = 100 * bd.buff_values[Buff.Reviver];
                    if (healthFor > revived_player.health) revived_player.SetHealth(healthFor);
                });
            }

        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem == null)
            {
                Timer timer;
                if (MiningUltimate_AutoTrigger_Timers.TryGetValue(player.userID, out timer))
                {
                    if (timer != null && !timer.Destroyed) timer.Destroy();
                    MiningUltimate_AutoTrigger_Timers.Remove(player.userID);
                }
                return;
            }

            if (!IsPickaxe(newItem.info.shortname)) return;

            TriggerMiningUltimateFromItem(player);
        }

        bool IsPickaxe(string shortname)
        {
            return config.ultimate_settings.ultimate_mining.tools_list.Contains(shortname);
        }

        Dictionary<ulong, Timer> MiningUltimate_AutoTrigger_Timers = new Dictionary<ulong, Timer>();

        void OnPlayerAssist(BasePlayer target, BasePlayer player) => OnPlayerRevive(player, target);

        void OnMissionSucceeded(BaseMission mission, BaseMission.MissionInstance missionInstance, BasePlayer player)
        {
            AwardXP(player, config.xp_settings.xp_sources.Mission, null, false, false, "mission");
        }

        void OnNewSave(string filename)
        {
            if (config.wipe_update_settings.bonus_skill_points && pcdData.pEntity != null && pcdData.pEntity.Count > 0)
            {
                var highest_player = 0ul;
                var highest_xp = 0d;
                foreach (var kvp in pcdData.pEntity)
                {
                    if (kvp.Value.xp > highest_xp)
                    {
                        highest_xp = kvp.Value.xp;
                        highest_player = kvp.Key;
                    }
                }
                Puts($"The player with the highest score is {highest_player} with {highest_xp} xp achieved.");
                pcdData.highest_player = highest_player;
            }
            if (config.wipe_update_settings.refund_sp_on_wipe) ResetSkills();

            foreach (var pi in pcdData.pEntity)
            {
                pi.Value.respec_multiplier = 0;
                if (config.wipe_update_settings.erase_ExtraPockets_on_wipe)
                {
                    pi.Value.pouch_items.Clear();
                }
                pi.Value.raiding_ultimate_used_time = DateTime.MinValue;
                if (config.rested_xp_settings.rested_xp_reset_on_wipe)
                {
                    pi.Value.xp_bonus_pool = 0;
                    pi.Value.logged_off = DateTime.Now;
                }
                pi.Value.xp_debt = 0;
            }

            if (config.wipe_update_settings.erase_data_on_wipe)
            {
                ResetAllData();
            }

            pcdData.wipeTime = DateTime.Now;
        }

        object OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
            var item = tool.GetItem();
            if (item == null || item.info.shortname != "bandage") return null;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - OnHealingItemUse. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return null;
            }
            if (bd.buff_values.ContainsKey(Buff.Double_Bandage_Heal))
            {
                if (!healers.Contains(player)) healers.Add(player);
            }
            return null;
        }

        List<BasePlayer> healers = new List<BasePlayer>();

        object OnPlayerHealthChange(BasePlayer player, float oldValue, float newValue)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
            if (newValue < oldValue) return null;
            if (healers.Contains(player))
            {
                healers.Remove(player);
                player.Heal(newValue - oldValue);
            }
            if (HasAnimalBuff(player, AnimalBuff.Wolf) && player.Team != null && player.Team.teamID > 0)
            {
                List<BasePlayer> nearby_players = Pool.GetList<BasePlayer>();
                var entities = FindEntitiesOfType<BasePlayer>(player.transform.position, config.ultimate_settings.ultimate_skinning.wolf_team_dist);
                nearby_players.AddRange(entities.Where(x => x.Team != null && x.Team.teamID == player.Team.teamID));
                Pool.FreeList(ref entities);

                Unsubscribe(nameof(OnPlayerHealthChange));
                player.Heal((newValue - oldValue) * (nearby_players.Count * config.ultimate_settings.ultimate_skinning.wolf_health_scale));
                Subscribe(nameof(OnPlayerHealthChange));

                Pool.FreeList(ref nearby_players);
            }

            return null;
        }

        object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item) => HandleDispenser(dispenser, player, item);

        object HandleDispenser(ResourceDispenser dispenser, BasePlayer player, Item item, bool bonus = false)
        {
            if (player.IsNpc || !player.userID.IsSteamId() || dispenser == null || item == null) return null;

            item.amount = GetMultipliedItemAmount(item);

            BuffDetails bd;
            var tool = player.GetActiveItem();
            if (config.misc_settings.call_HandleDispenser) Interface.CallHook("OnSkillTreeHandleDispenser", player, dispenser.baseEntity, item); // no credit given if this hook isn't called in XDQuest	
            if (tool != null && config.tools_black_white_list_settings.black_listed_gather_items.Contains(tool.info.shortname)) return null;
            if (!buffDetails.TryGetValue(player.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - HandleDispenser. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return null;
            }
            if (bd == null || bd.buff_values == null) return null;
            HeldEntity heldEntity = player.GetHeldEntity();
            var gather_modifier = 1f;
            var xp_modifier = 1f;
            var luck_modifier = 1f;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
            {
                if (heldEntity != null && heldEntity is Chainsaw)
                {
                    gather_modifier = config.tools_black_white_list_settings.power_tool_modifier.woodcutting_yield_modifier;
                    xp_modifier = config.tools_black_white_list_settings.power_tool_modifier.woodcutting_xp_modifier;
                    luck_modifier = config.tools_black_white_list_settings.power_tool_modifier.woodcutting_luck_modifier;
                }

                if (!bonus)
                {
                    if (PassWhitelistCheck(player, tool, GatherSourceType.Woodcutting)) AwardXP(player, config.xp_settings.xp_sources.TreeHit * xp_modifier, dispenser.baseEntity, false, false, "woodcutting");
                }
                if (bd.buff_values.ContainsKey(Buff.Woodcutting_Yield))
                {
                    if (Interface.CallHook("STCanReceiveYield", player, dispenser.baseEntity, item) == null)
                    {
                        var amount = item.amount * (bd.buff_values[Buff.Woodcutting_Yield] * gather_modifier) + (TOD_Sky.Instance.IsNight ? item.amount * ((config.xp_settings.night_settings.night_woodcutting_yield_modifier - 1) * gather_modifier) : 0);
                        if (amount > 0.5)
                        {
                            if (amount < 1) amount = 1;
                            item.amount += Convert.ToInt32(amount);
                        }
                    }
                }

                if (!bonus && bd.buff_values.ContainsKey(Buff.Instant_Chop) && RollSuccessful((bd.buff_values[Buff.Instant_Chop] * luck_modifier)))
                {
                    foreach (var r in dispenser.containedItems)
                    {
                        if (r.amount < 1) continue;
                        r.amount = GetMultipliedItemAmount(r.itemDef.shortname, Convert.ToInt32(r.amount));
                        var bonus_amount = Convert.ToInt32(r.amount + (r.amount * (TOD_Sky.Instance.IsNight ? (config.xp_settings.night_settings.night_woodcutting_yield_modifier - 1) * gather_modifier : 0)) + (r.amount * (bd.buff_values.ContainsKey(Buff.Woodcutting_Yield) ? bd.buff_values[Buff.Woodcutting_Yield] * gather_modifier : 0)));
                        if (r.itemDef.shortname == item.info.shortname) item.amount += bonus_amount;
                        else
                        {
                            //player.GiveItem(ItemManager.CreateByName(r.itemDef.shortname, amount_to_give));
                            if (bonus_amount > 0) GiveItem(player, ItemManager.CreateByName(r.itemDef.shortname, bonus_amount));
                        }

                        r.amount = 0;
                    }
                }
                if (bd.buff_values.ContainsKey(Buff.Woodcutting_Coal) && RollSuccessful((bd.buff_values[Buff.Woodcutting_Coal] * gather_modifier)))
                {
                    GiveItem(player, ItemManager.CreateByName("charcoal", item.amount));
                    //player.GiveItem(ItemManager.CreateByName("charcoal", item.amount));
                }
            }
            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
            {
                if (heldEntity != null && heldEntity is Jackhammer)
                {
                    gather_modifier = config.tools_black_white_list_settings.power_tool_modifier.mining_yield_modifier;
                    xp_modifier = config.tools_black_white_list_settings.power_tool_modifier.mining_xp_modifier;
                    luck_modifier = config.tools_black_white_list_settings.power_tool_modifier.mining_luck_modifier;
                }

                if (!bonus)
                {
                    if (PassWhitelistCheck(player, tool, GatherSourceType.Mining)) AwardXP(player, config.xp_settings.xp_sources.NodeHit * xp_modifier, dispenser.baseEntity, false, false, "mining");
                }

                var nightAmount = TOD_Sky.Instance.IsNight ? Convert.ToInt32(item.amount * ((config.xp_settings.night_settings.night_mining_yield_modifier - 1) * gather_modifier)) : 0;

                if (bd.buff_values.ContainsKey(Buff.Mining_Yield))
                {
                    HandleMiningYield(dispenser, player, item, bd, gather_modifier);
                }

                item.amount += nightAmount;

                if (!bonus && bd.buff_values.ContainsKey(Buff.Instant_Mine) && RollSuccessful((bd.buff_values[Buff.Instant_Mine] * luck_modifier)))
                {
                    HandleInstantMining(dispenser, player, item, bd);
                }
                if (dispenser.baseEntity != null && !string.IsNullOrEmpty(dispenser.baseEntity.ShortPrefabName) && dispenser.baseEntity.ShortPrefabName != "stone-ore" && bd.buff_values.ContainsKey(Buff.Smelt_On_Mine) && RollSuccessful((bd.buff_values[Buff.Smelt_On_Mine] * gather_modifier)))
                {
                    HandleSmeltOnMine(player, item);
                }
            }
            if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
            {
                if (heldEntity != null && (heldEntity is Jackhammer || heldEntity is Chainsaw))
                {
                    gather_modifier = config.tools_black_white_list_settings.power_tool_modifier.skinning_yield_modifier;
                    xp_modifier = config.tools_black_white_list_settings.power_tool_modifier.skinning_xp_modifier;
                    luck_modifier = config.tools_black_white_list_settings.power_tool_modifier.skinning_luck_modifier;
                }
                bool isFinalHit = dispenser.containedItems.Where(x => x.amount > 0).FirstOrDefault() == null;
                // Old award xp spot

                if (isFinalHit && dispenser.baseEntity.ShortPrefabName.Equals("shark.corpse") && bd.buff_values.ContainsKey(Buff.SharkSkinner) && RollSuccessful(bd.buff_values[Buff.SharkSkinner] * luck_modifier))
                {
                    var randomItem = GetSharkLoot().GetRandom();
                    var _item = ItemManager.CreateByName(randomItem.shortname, randomItem.max == 1 ? 1 : UnityEngine.Random.Range(randomItem.min > 1 ? randomItem.min : 1, randomItem.max));
                    player.GiveItem(_item);
                    Player.Message(player, String.Format(lang.GetMessage("SharkStomachFound", this, player.UserIDString), _item.amount > 1 ? lang.GetMessage("some", this, player.UserIDString) : lang.GetMessage("a", this, player.UserIDString), _item.info.displayName.english), config.misc_settings.ChatID);
                }

                var nightAmount = TOD_Sky.Instance.IsNight ? Convert.ToInt32(item.amount * (config.xp_settings.night_settings.night_skinning_yield_modifier - 1)) : 0;

                if (bd.buff_values.ContainsKey(Buff.Skinning_Yield))
                {
                    if (Interface.CallHook("STCanReceiveYield", player, dispenser.baseEntity, item) == null)
                    {
                        var amount = item.amount * (bd.buff_values[Buff.Skinning_Yield] * gather_modifier) + (TOD_Sky.Instance.IsNight ? item.amount * ((config.xp_settings.night_settings.night_skinning_yield_modifier - 1) * gather_modifier) : 0);
                        if (amount > 0.5)
                        {
                            if (amount < 1) amount = 1;
                            item.amount += Convert.ToInt32(amount);
                        }
                    }
                }

                item.amount += nightAmount;

                if (!isFinalHit && bd.buff_values.ContainsKey(Buff.Instant_Skin) && RollSuccessful((bd.buff_values[Buff.Instant_Skin] * luck_modifier)))
                {
                    foreach (var r in dispenser.containedItems)
                    {
                        if (r.amount < 1) continue;
                        r.amount = GetMultipliedItemAmount(r.itemDef.shortname, Convert.ToInt32(r.amount));
                        var bonus_amount = Convert.ToInt32(r.amount + (r.amount * (TOD_Sky.Instance.IsNight ? (config.xp_settings.night_settings.night_skinning_yield_modifier - 1) * gather_modifier : 0)) + (r.amount * (bd.buff_values.ContainsKey(Buff.Skinning_Yield) ? bd.buff_values[Buff.Skinning_Yield] * gather_modifier : 0)));
                        if (r.itemDef.shortname == item.info.shortname) item.amount += bonus_amount;
                        else
                        {
                            //player.GiveItem(ItemManager.CreateByName(r.itemDef.shortname, amount_to_give));
                            if (bonus_amount > 0) GiveItem(player, ItemManager.CreateByName(r.itemDef.shortname, bonus_amount));
                        }

                        r.amount = 0;
                    }
                    isFinalHit = true;
                }
                if (bd.buff_values.ContainsKey(Buff.Skin_Cook) && RollSuccessful((bd.buff_values[Buff.Skin_Cook] * luck_modifier)))
                {
                    var cooked = GetCookedMeat(item.info.shortname);
                    if (!string.IsNullOrEmpty(cooked))
                    {
                        GiveItem(player, ItemManager.CreateByName(cooked, item.amount));
                        //player.GiveItem(ItemManager.CreateByName(cooked, item.amount));
                        item.amount = 0;
                        item.Remove();
                    }
                }

                if (isFinalHit && bd.buff_values.ContainsKey(Buff.Skinning_Luck) && RollSuccessful((bd.buff_values[Buff.Skinning_Luck] * luck_modifier)) && config.loot_settings.skinning_loot_table.Count > 0)
                {
                    var randProfile = RollLootItem(config.loot_settings.skinning_loot_table);
                    if (randProfile != null)
                    {
                        var randomitem = CreateDropItem(randProfile);
                        if (randomitem != null)
                        {
                            if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("ItemFound", this, player.UserIDString), randomitem.amount, randomitem.name ?? randomitem.info.displayName.english, "corpse"), config.misc_settings.ChatID);
                            if (randomitem != null) player.GiveItem(randomitem);
                        }
                    }
                    //if (def != null) player.GiveItem(ItemManager.CreateByName(randomitem.Key, quantity));                    
                }

                if (PassWhitelistCheck(player, tool, GatherSourceType.Skinning)) AwardXP(player, (isFinalHit ? config.xp_settings.xp_sources.SkinHitFinal : config.xp_settings.xp_sources.SkinHit) * xp_modifier, dispenser.baseEntity, false, false, "skinning");
            }
            return null;
        }

        void HandleSmeltOnMine(BasePlayer player, Item item)
        {
            var refined = GetRefinedMaterial(item.info.shortname);
            if (!string.IsNullOrEmpty(refined))
            {
                GiveItem(player, ItemManager.CreateByName(refined, Math.Max(item.amount, 1)));
                item.amount = 0;
                item.Remove();
            }
        }

        void HandleInstantMining(ResourceDispenser dispenser, BasePlayer player, Item item, BuffDetails bd)
        {
            Interface.CallHook("STOnInstantMineTrigger", player, dispenser, item);
            foreach (var r in dispenser.containedItems)
            {
                if (r.amount < 1) continue;
                r.amount = GetMultipliedItemAmount(r.itemDef.shortname, Convert.ToInt32(r.amount));
                var bonus_amount = Convert.ToInt32(r.amount + (r.amount * (TOD_Sky.Instance.IsNight ? config.xp_settings.night_settings.night_mining_yield_modifier - 1 : 0)) + (r.amount * (bd.buff_values.ContainsKey(Buff.Mining_Yield) ? bd.buff_values[Buff.Mining_Yield] : 0)));
                if (r.itemDef.shortname == item.info.shortname) item.amount += bonus_amount;
                else
                {
                    //player.GiveItem(ItemManager.CreateByName(r.itemDef.shortname, amount_to_give));
                    if (bonus_amount > 0) GiveItem(player, ItemManager.CreateByName(r.itemDef.shortname, bonus_amount));
                }

                r.amount = 0;
            }
        }

        void HandleMiningYield(ResourceDispenser dispenser, BasePlayer player, Item item, BuffDetails bd, float gather_modifier)
        {
            if (Interface.CallHook("STCanReceiveYield", player, dispenser.baseEntity, item) == null)
            {
                var amount = item.amount * (bd.buff_values[Buff.Mining_Yield] * gather_modifier) + (TOD_Sky.Instance.IsNight ? item.amount * ((config.xp_settings.night_settings.night_mining_yield_modifier - 1) * gather_modifier) : 0);
                if (amount > 0.5)
                {
                    if (amount < 1) amount = 1;
                    item.amount += Convert.ToInt32(amount);
                }
            }
        }

        string GetCookedMeat(string shortname)
        {
            switch (shortname)
            {
                case "bearmeat": return "bearmeat.cooked";
                case "chicken.raw": return "chicken.cooked";
                case "deermeat.raw": return "deermeat.cooked";
                case "fish.raw": return "fish.cooked";
                case "horsemeat.raw": return "horsemeat.cooked";
                case "humanmeat.raw": return "humanmeat.cooked";
                case "meat.boar": return "meat.pork.cooked";
                case "wolfmeat.raw": return "wolfmeat.cooked";
                default: return null;
            }
        }

        string GetRefinedMaterial(string shortname)
        {
            switch (shortname)
            {
                case "hq.metal.ore": return "metal.refined";
                case "metal.ore": return "metal.fragments";
                case "sulfur.ore": return "sulfur";
                default: return null;
            }
        }

        void HandleTree(BasePlayer player, ResourceDispenser dispenser)
        {
            var heldEntity = player.GetHeldEntity();
            if (heldEntity == null || !(heldEntity is AttackEntity)) return;
            // Sets the skinID to 222 if the deforest perk is triggering the tree to fall.
            if (dispenser.baseEntity.skinID == 0) dispenser.baseEntity.skinID = 222;
            dispenser.AssignFinishBonus(player, 1f, heldEntity as AttackEntity);
            HitInfo hitInfo = new HitInfo(player, dispenser.baseEntity, Rust.DamageType.Generic, dispenser.baseEntity.MaxHealth(), dispenser.transform.position);
            hitInfo.gatherScale = 0f;
            hitInfo.PointStart = dispenser.transform.position;
            hitInfo.PointEnd = dispenser.transform.position;
            hitInfo.WeaponPrefab = heldEntity;
            hitInfo.Weapon = null;
            dispenser.baseEntity.OnAttacked(hitInfo);
        }

        public static bool UltimateTriggered = false;

        LootItems RollLootItem(List<LootItems> items)
        {
            var count = 0;
            foreach (var entry in items)
                count += entry.dropWeight;

            var roll = UnityEngine.Random.Range(0, count + 1);
            var _checked = 0;

            foreach (var entry in items)
            {
                _checked += entry.dropWeight;
                if (roll <= _checked) return entry;
            }

            return items.GetRandom();
        }

        Item CreateDropItem(LootItems info)
        {
            var item = ItemManager.CreateByName(info.shortname, Math.Max(UnityEngine.Random.Range(info.min, info.max + 1), 1), info.skin);
            if (item == null) return null;
            if (!string.IsNullOrEmpty(info.displayName)) item.name = info.displayName;
            return item;
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || item == null || player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - OnDispenserBonus. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return null;
            }

            HeldEntity heldEntity = player.GetHeldEntity();
            var gather_modifier = 1f;
            var xp_modifier = 1f;
            var luck_modifier = 1f;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
            {
                if (heldEntity != null && heldEntity is Chainsaw)
                {
                    gather_modifier = config.tools_black_white_list_settings.power_tool_modifier.woodcutting_yield_modifier;
                    xp_modifier = config.tools_black_white_list_settings.power_tool_modifier.woodcutting_xp_modifier;
                    luck_modifier = config.tools_black_white_list_settings.power_tool_modifier.woodcutting_luck_modifier;
                }

                float value;
                if ((!UltimateTriggered || config.ultimate_settings.ultimate_woodcutting.award_xp) && PassWhitelistCheck(player, null, GatherSourceType.WoodcuttingFinal)) AwardXP(player, config.xp_settings.xp_sources.TreeHitFinal * xp_modifier, dispenser.baseEntity, false, false, "woodcutting final");
                if (bd.buff_values.ContainsKey(Buff.Woodcutting_Luck) && RollSuccessful((bd.buff_values[Buff.Woodcutting_Luck] * luck_modifier)) && config.loot_settings.wc_loot_table.Count > 0)
                {
                    var randProfile = RollLootItem(config.loot_settings.wc_loot_table);
                    if (randProfile != null)
                    {
                        var randomitem = CreateDropItem(randProfile);
                        if (randomitem != null)
                        {
                            if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("ItemFound", this, player.UserIDString), randomitem.amount, randomitem.name ?? randomitem.info.displayName.english, "tree"), config.misc_settings.ChatID);
                            player.GiveItem(randomitem);
                        }
                    }
                    //if (def != null) player.GiveItem(ItemManager.CreateByName(randomitem.Key, quantity));                    
                }
                if (bd.buff_values.TryGetValue(Buff.Regrowth, out value) && RollSuccessful(value * luck_modifier))
                {
                    var nodes = FindEntitiesOfType<ResourceEntity>(dispenser.baseEntity.transform.position, 2);
                    bool alreadySpawned = false;
                    foreach (var node in nodes)
                    {
                        if (node == dispenser.baseEntity) continue;
                        if (InRange(node.transform.position, dispenser.baseEntity.transform.position, 0.05f))
                        {
                            alreadySpawned = true;
                            break;
                        }
                    }
                    Pool.FreeList(ref nodes);

                    if (!alreadySpawned)
                    {
                        var newTree = GameManager.server.CreateEntity(dispenser.baseEntity.PrefabName, dispenser.baseEntity.transform.position, dispenser.baseEntity.transform.rotation);
                        newTree.Spawn();
                        if (NotificationsOn(player)) Player.Message(player, lang.GetMessage("RegrowthProc", this, player.UserIDString), config.misc_settings.ChatID);
                    }
                }
                if (!UltimateTriggered && bd.buff_values.ContainsKey(Buff.Woodcutting_Ultimate) && IsUltimateEnabled(player, Buff.Woodcutting_Ultimate) && luck_modifier > 0)
                {
                    float _time = Time.time;
                    List<ResourceDispenser> other_trees = Pool.GetList<ResourceDispenser>();
                    var entities = FindEntitiesOfType<BaseEntity>(dispenser.transform.position, config.ultimate_settings.ultimate_woodcutting.distance_from_player);
                    other_trees.AddRange(entities.Where(x => x.PrefabName.StartsWith("assets/bundled/prefabs/autospawn/resource/v3_") || x.PrefabName.StartsWith("assets/bundled/prefabs/autospawn/resource/swamp")).Select(x => x.GetComponent<ResourceDispenser>()));
                    Pool.FreeList(ref entities);
                    UltimateTriggered = true;
                    foreach (var _dispenser in other_trees)
                    {
                        if (_dispenser == null) continue;
                        if (_dispenser != dispenser && _dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                        {
                            HandleTree(player, _dispenser);
                        }
                    }
                    UltimateTriggered = false;
                }
            }
            else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
            {
                if (heldEntity != null && heldEntity is Jackhammer)
                {
                    gather_modifier = config.tools_black_white_list_settings.power_tool_modifier.mining_yield_modifier;
                    xp_modifier = config.tools_black_white_list_settings.power_tool_modifier.mining_xp_modifier;
                    luck_modifier = config.tools_black_white_list_settings.power_tool_modifier.mining_luck_modifier;
                }

                if (PassWhitelistCheck(player, null, GatherSourceType.MiningFinal) && item.info.shortname != "hq.metal.ore")
                {
                    AwardXP(player, config.xp_settings.xp_sources.NodeHitFinal * luck_modifier, dispenser.baseEntity, false, false, "mining final");
                }

                if (bd.buff_values.ContainsKey(Buff.Mining_Luck) && RollSuccessful((bd.buff_values[Buff.Mining_Luck] * luck_modifier)) && config.loot_settings.mining_loot_table.Count > 0)
                {
                    var randProfile = RollLootItem(config.loot_settings.mining_loot_table);
                    if (randProfile != null)
                    {
                        var randomitem = CreateDropItem(randProfile);
                        if (randomitem != null)
                        {
                            if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("ItemFound", this, player.UserIDString), randomitem.amount, randomitem.name ?? randomitem.info.displayName.english, "node"), config.misc_settings.ChatID);
                            player.GiveItem(randomitem);
                        }
                    }
                    //if (def != null) player.GiveItem(ItemManager.CreateByName(randomitem.Key, quantity));                    
                }
            }
            // Dispenser bonus doesnt trigger for flesh.
            HandleDispenser(dispenser, player, item, true);
            return null;
        }

        enum GatherSourceType
        {
            Woodcutting,
            WoodcuttingFinal,
            Mining,
            MiningFinal,
            Skinning
        }

        bool PassWhitelistCheck(BasePlayer player, Item tool, GatherSourceType type)
        {
            if (!config.xp_settings.white_listed_tools_only) return true;

            if (tool == null) tool = player.GetActiveItem();
            if (tool == null) return true;

            switch (type)
            {
                case GatherSourceType.Woodcutting:
                case GatherSourceType.WoodcuttingFinal:
                    if (config.tools_black_white_list_settings.wc_tools.Count == 0 || config.tools_black_white_list_settings.wc_tools.Contains(tool.info.shortname)) return true;
                    else return false;

                case GatherSourceType.Mining:
                case GatherSourceType.MiningFinal:
                    if (config.tools_black_white_list_settings.mining_tools.Count == 0 || config.tools_black_white_list_settings.mining_tools.Contains(tool.info.shortname)) return true;
                    else return false;

                case GatherSourceType.Skinning:
                    if (config.tools_black_white_list_settings.skinning_tools.Count == 0 || config.tools_black_white_list_settings.skinning_tools.Contains(tool.info.shortname)) return true;
                    else return false;
            }

            return true;
        }

        object OnItemRepair(BasePlayer player, Item item)
        {
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd))
            {
                if (bd.buff_values.ContainsKey(Buff.MaxRepair) && Interface.CallHook("STOnItemRepairWithMaxRepair", item) == null)
                {
                    NextTick(() =>
                    {
                        if (item != null && item.condition == item.maxCondition)
                        {
                            item.maxCondition = ItemDefs[item.info.shortname].condition.max;
                            item.condition = item.maxCondition;
                            item.MarkDirty();
                        }
                    });
                }
            }
            return null;
        }

        void OnLoseCondition(Item item, ref float amount)
        {
            var player = item.GetEntityOwner() as BasePlayer;
            if (player != null && !player.IsNpc && player.userID.IsSteamId())
            {
                BuffDetails bd;
                if (buffDetails.TryGetValue(player.userID, out bd))
                {
                    float amount_to_repair = 0f;
                    float value;
                    if (bd.buff_values.TryGetValue(Buff.Durability, out value) && !config.buff_settings.durability_blacklist.Contains(item.info.shortname))
                        amount_to_repair += (amount * value);

                    if (bd.buff_values.ContainsKey(Buff.Primitive_Expert) && config.buff_settings.primitive_weapons.Contains(item.info.shortname))
                        amount_to_repair = amount;

                    else if (bd.buff_values.TryGetValue(Buff.Woodcutting_Tool_Durability, out value) && config.tools_black_white_list_settings.wc_tools.Contains(item.info.shortname))
                        amount_to_repair += (value * amount);

                    else if (bd.buff_values.TryGetValue(Buff.Mining_Tool_Durability, out value) && config.tools_black_white_list_settings.mining_tools.Contains(item.info.shortname))
                        amount_to_repair += (value * amount);

                    else if (bd.buff_values.TryGetValue(Buff.Skinning_Tool_Durability, out value) && config.tools_black_white_list_settings.skinning_tools.Contains(item.info.shortname))
                        amount_to_repair += (value * amount);

                    item.condition += amount_to_repair >= amount ? amount : amount_to_repair;
                }
            }
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd))
            {
                if (bd.buff_values.ContainsKey(Buff.Free_Bullet_Chance) && RollSuccessful(bd.buff_values[Buff.Free_Bullet_Chance]))
                {
                    var heldEntity = projectile.GetItem();
                    if (heldEntity == null) return;
                    projectile.primaryMagazine.contents++;
                    projectile.SendNetworkUpdateImmediate();
                }
            }
        }

        void CanCatchFish(BasePlayer player, BaseFishingRod fishingRod, Item fish)
        {
            if (player.IsNpc || !player.userID.IsSteamId()) return;
            fish.amount = GetMultipliedItemAmount(fish);

            AwardXP(player, config.xp_settings.xp_sources.FishCaught, null, false, false, "fishing");

            float value;
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.TryGetValue(Buff.Extra_Fish, out value))
            {
                RollExtraFish(player, fish, value);
            }
        }

        void RollExtraFish(BasePlayer player, Item fish, float value)
        {
            var extraFish = 0;

            for (int i = 1; i < 999; i++)
            {
                if (i < value) extraFish++;
                else break;
            }
            float leftOver = value - extraFish;
            var roll = UnityEngine.Random.Range(0f, 100f);
            if (roll > 100f - (leftOver / 1 * 100))
            {
                extraFish++;
            }
            if (extraFish < 1) return;
            if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("ExtraFish", this, player.UserIDString), extraFish, fish.info.displayName.english), config.misc_settings.ChatID);
            fish.amount += extraFish;
        }

        void OnFishCatch(Item item, BaseFishingRod rod, BasePlayer player)
        {
            BuffDetails bd;
            float luck;
            if (!buffDetails.TryGetValue(player.userID, out bd)) return;
            if (bd.buff_values.TryGetValue(Buff.Fishing_Luck, out luck) && RollSuccessful(luck))
            {
                var randProfile = RollLootItem(config.loot_settings.fishing_loot_table);
                if (randProfile != null)
                {
                    var randomitem = CreateDropItem(randProfile);
                    if (randomitem != null)
                    {
                        if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("ItemFound", this, player.UserIDString), randomitem.amount, randomitem.name ?? randomitem.info.displayName.english, item.info.displayName.english), config.misc_settings.ChatID);
                        player.GiveItem(randomitem);
                    }
                }
            }
            if (TrackedRods.Contains(rod)) ResetRod(rod);
        }

        void ApplyRodStrength(BaseFishingRod rod, float modifier)
        {
            rod.GlobalStrainSpeedMultiplier = 1 - modifier;
            TrackedRods.Add(rod);
        }

        void ResetRod(BaseFishingRod rod, bool doRemove = true)
        {
            rod.GlobalStrainSpeedMultiplier = 1f;
            if (doRemove) TrackedRods.Remove(rod);
        }

        List<BaseFishingRod> TrackedRods = new List<BaseFishingRod>();
        void OnFishingStopped(BaseFishingRod rod, BaseFishingRod.FailReason failReason)
        {
            if (TrackedRods.Contains(rod)) ResetRod(rod);
        }

        bool CheckHarvestingBlacklist = false;

        void OnCollectiblePickup(CollectibleEntity entity, BasePlayer player)
        {
            if (player == null || entity == null || player.IsNpc || !player.userID.IsSteamId() || entity.itemList == null) return;

            if (config.base_yield_settings.adjust_base_yield)
            {
                foreach (var item in entity.itemList)
                {
                    item.amount = GetMultipliedItemAmount(item.itemDef.shortname, item.amount);
                }
            }

            AwardXP(player, config.xp_settings.xp_sources.CollectWildPlant, entity, false, false, "collect entity");
            BuffDetails bd;
            float buffValue;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.TryGetValue(Buff.Harvest_Wild_Yield, out buffValue))
            {
                foreach (var item in entity.itemList)
                {
                    if (item == null || Interface.CallHook("STCanReceiveYield", player, entity, item) != null) continue;
                    if (CheckHarvestingBlacklist && config.buff_settings.harvest_yield_blacklist.Contains(item.itemDef.shortname)) continue;
                    item.amount += Convert.ToInt32(Math.Round((buffValue * item.amount) + (TOD_Sky.Instance.IsNight ? (config.xp_settings.night_settings.night_harvesting_yield_modifier - 1) * item.amount : 0), 0, MidpointRounding.AwayFromZero));
                }
            }
            else
            {
                foreach (var item in entity.itemList)
                {
                    if (item == null || Interface.CallHook("STCanReceiveYield", player, entity, item) != null) continue;
                    item.amount += Convert.ToInt32(Math.Round(TOD_Sky.Instance.IsNight ? (config.xp_settings.night_settings.night_harvesting_yield_modifier - 1) * item.amount : 0, 0, MidpointRounding.AwayFromZero));
                }
            }
            if (HasAnimalBuff(player, AnimalBuff.Boar) && !string.IsNullOrEmpty(entity.PrefabName) && (entity.PrefabName.StartsWith("assets/content/nature/plants/mushroom/") || entity.PrefabName.StartsWith("assets/bundled/prefabs/autospawn/collectable/"))) RollBoarLoot(player, entity);
        }

        Dictionary<ulong, float> Harvester_Ultimate_cooldown = new Dictionary<ulong, float>();

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (go == null) return;
            var player = plan?.GetOwnerPlayer();
            if (player == null) return;
            var entity = go.ToBaseEntity();
            if (entity != null)
            {
                if (entity is BuildingBlock) AwardXP(player, config.xp_settings.xp_sources.BuildingBlockDeployed, entity, false, false, "building");
            }

            GrowableEntity plant = go.GetComponent<GrowableEntity>();
            if (plant != null)
            {
                BuffDetails bd;
                PlayerInfo pi;
                if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Harvester_Ultimate) && IsUltimateEnabled(player, Buff.Harvester_Ultimate) && pcdData.pEntity.TryGetValue(player.userID, out pi))
                {
                    if (config.ultimate_settings.ultimate_harvesting.cooldown > 0)
                    {
                        float cd;
                        if (!Harvester_Ultimate_cooldown.TryGetValue(player.userID, out cd))
                        {
                            Harvester_Ultimate_cooldown.Add(player.userID, Time.time + config.ultimate_settings.ultimate_harvesting.cooldown);
                            if (config.ultimate_settings.ultimate_harvesting.notify_on_cooldown) Player.Message(player, string.Format(lang.GetMessage("HarvestUltiCDNotification", this, player.UserIDString), config.ultimate_settings.ultimate_harvesting.cooldown), config.misc_settings.ChatID);
                        }

                        else
                        {
                            if (Time.time < cd) return;
                            else
                            {
                                Harvester_Ultimate_cooldown[player.userID] = Time.time + config.ultimate_settings.ultimate_harvesting.cooldown;
                                if (config.ultimate_settings.ultimate_harvesting.notify_on_cooldown) Player.Message(player, string.Format(lang.GetMessage("HarvestUltiCDNotification", this, player.UserIDString), config.ultimate_settings.ultimate_harvesting.cooldown), config.misc_settings.ChatID);
                            }
                        }
                    }
                    var genes = plant.Genes.Genes;
                    for (int i = 0; i < pi.plant_genes.Length; i++)
                    {
                        switch (pi.plant_genes[i])
                        {
                            case 'g':
                                genes[i].Set(GrowableGenetics.GeneType.GrowthSpeed);
                                genes[i].SetPrevious(GrowableGenetics.GeneType.GrowthSpeed);
                                break;

                            case 'e':
                                genes[i].Set(GrowableGenetics.GeneType.Empty);
                                genes[i].SetPrevious(GrowableGenetics.GeneType.Empty);
                                break;

                            case 'x':
                                genes[i].Set(GrowableGenetics.GeneType.Empty);
                                genes[i].SetPrevious(GrowableGenetics.GeneType.Empty);
                                break;

                            case 'w':
                                genes[i].Set(GrowableGenetics.GeneType.WaterRequirement);
                                genes[i].SetPrevious(GrowableGenetics.GeneType.WaterRequirement);
                                break;

                            case 'y':
                                genes[i].Set(GrowableGenetics.GeneType.Yield);
                                genes[i].SetPrevious(GrowableGenetics.GeneType.Yield);
                                break;

                            case 'h':
                                genes[i].Set(GrowableGenetics.GeneType.Hardiness);
                                genes[i].SetPrevious(GrowableGenetics.GeneType.Hardiness);
                                break;

                            default:
                                genes[i].Set(GrowableGenetics.GeneType.GrowthSpeed);
                                genes[i].SetPrevious(GrowableGenetics.GeneType.GrowthSpeed);
                                break;
                        }
                    }
                    plant.SendNetworkUpdateImmediate();
                }
            }
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item, ItemCrafter crafter)
        {
            if (crafter == null) return;
            var player = crafter.owner;
            if (player == null) return;

            if (task.blueprint == null)
            {
                return;
            }

            if (!ExcludeFromCraftXP(item.info.shortname))
            {
                var experienceGain = CraftTimes.ContainsKey(task.taskUID) && config.buff_settings.timeBasedCraftingXP ? Math.Round(CraftTimes[task.taskUID] * config.xp_settings.xp_sources.Crafting, 2) : Math.Round((item.info.Blueprint.time + 0.99f) * config.xp_settings.xp_sources.Crafting, 2);
                AwardXP(player, experienceGain, null, false, false, "crafting");
            }
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd))
            {
                float value;
                if (bd.buff_values.TryGetValue(Buff.Craft_Refund, out value) && RollSuccessful(value))
                {
                    var refunded = 0;
                    ItemBlueprint bp;
                    if (item_BPs.TryGetValue(item.info.shortname, out bp))
                    {
                        foreach (var component in bp.ingredients)
                        {
                            if (config.tools_black_white_list_settings.craft_refund_blacklist.Contains(component.itemDef.shortname)) continue;
                            var nitem = ItemManager.CreateByName(component.itemDef.shortname, Convert.ToInt32(component.amount));
                            if (nitem == null) continue;
                            if (!player.inventory.containerBelt.IsFull() || !player.inventory.containerMain.IsFull())
                            {
                                GiveItem(player, nitem);
                                //player.inventory.GiveItem(nitem);
                            }
                            else nitem.DropAndTossUpwards(player.transform.position);
                            refunded++;
                        }
                        if (refunded > 0 && NotificationsOn(player)) Player.Message(player, lang.GetMessage("CraftRefund", this, player.UserIDString), config.misc_settings.ChatID);
                    }
                }
                if (bd.buff_values.TryGetValue(Buff.Craft_Duplicate, out value) && RollSuccessful(value) && !config.tools_black_white_list_settings.craft_duplicate_blacklist.Contains(item.info.shortname))
                {
                    var ditem = ItemManager.CreateByName(item.info.shortname, item.amount, item.skin);
                    if (ditem != null)
                    {
                        if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("DuplicateProc", this, player.UserIDString), item.info.displayName.english), config.misc_settings.ChatID);
                        player.GiveItem(ditem);
                    }
                }
            }

            if (task.amount > 0)
            {
                if (CraftTimes.ContainsKey(task.taskUID)) CraftTimes[task.taskUID] = GetModifiedTime(player, task);
                return;
            }

            CraftTimes.Remove(task.taskUID);

            if (task.blueprint != null && task.blueprint.name.Contains("(Clone)"))
            {
                var behaviours = task.blueprint.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour.name.Contains("(Clone)")) UnityEngine.Object.Destroy(behaviour);
                }
            }
        }

        object OnItemCraft(ItemCraftTask task, BasePlayer player, Item fromTempBlueprint)
        {
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd))
            {
                if (bd.buff_values.ContainsKey(Buff.Craft_Speed))
                {
                    var craftingTime = task.blueprint.time;
                    var reducedTime = craftingTime - (craftingTime * bd.buff_values[Buff.Craft_Speed]);
                    if (reducedTime <= 0) reducedTime = 0.0f;
                    if (!task.blueprint.name.Contains("(Clone)"))
                        task.blueprint = UnityEngine.Object.Instantiate(task.blueprint);
                    task.blueprint.time = reducedTime;

                }
                if (!CraftTimes.ContainsKey(task.taskUID)) CraftTimes.Add(task.taskUID, GetModifiedTime(player, task));
            }

            return null;
        }

        void OnItemCraftCancelled(ItemCraftTask task)
        {
            if (task == null) return;
            CraftTimes.Remove(task.taskUID);
        }

        bool ExcludeFromCraftXP(string shortname)
        {
            if (config.xp_settings.craft_xp_whitelist != null && config.xp_settings.craft_xp_whitelist.Count > 0)
            {
                if (config.xp_settings.craft_xp_whitelist.Contains(shortname)) return false;
                return true;
            }
            if (config.xp_settings.craft_xp_blacklist != null && config.xp_settings.craft_xp_blacklist.Contains(shortname)) return true;
            return false;
        }

        Dictionary<int, float> CraftTimes = new Dictionary<int, float>();


        float GetModifiedTime(BasePlayer player, ItemCraftTask task)
        {
            var workbenchLevel = player.currentCraftLevel;

            if (workbenchLevel == 0) return task.blueprint.time;
            var diff = workbenchLevel - task.blueprint.workbenchLevelRequired;
            if (diff < 0.5) return task.blueprint.time;
            else if (diff < 1.5) return task.blueprint.time / 2;
            else return task.blueprint.time / 4;
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            item.amount = GetMultipliedItemAmount(item);

            if (player.IsNpc || !player.userID.IsSteamId()) return;
            if (!item.info.shortname.Contains("seed") && (!config.xp_settings.ripe_required || plant.State == PlantProperties.State.Ripe)) AwardXP(player, config.xp_settings.xp_sources.CollectGrownPlant, plant, false, false, "grown plant");
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Harvest_Grown_Yield) && Interface.CallHook("STCanReceiveYield", player, plant, item) == null)
            {
                var quantity = Convert.ToInt32(Math.Round(bd.buff_values[Buff.Harvest_Grown_Yield] * item.amount + (TOD_Sky.Instance.IsNight && config.xp_settings.night_settings.include_grown_harvesting ? (config.xp_settings.night_settings.night_harvesting_yield_modifier - 1) * item.amount : 0), 0, MidpointRounding.AwayFromZero));
                item.amount += quantity;
            }
            else item.amount += Convert.ToInt32(Math.Round(TOD_Sky.Instance.IsNight && config.xp_settings.night_settings.include_grown_harvesting ? (config.xp_settings.night_settings.night_harvesting_yield_modifier - 1) * item.amount : 0, 0, MidpointRounding.AwayFromZero));
        }

        public static bool ContainsTopology(TerrainTopology.Enum mask, Vector3 position)
        {
            return (TerrainMeta.TopologyMap.GetTopology(position, (int)mask));
        }

        public bool IsUnderwater(BasePlayer player)
        {
            return player.WaterFactor() == 1f || ContainsTopology(TerrainTopology.Enum.Monument, player.transform.position) && ContainsTopology(TerrainTopology.Enum.Ocean, player.transform.position);
        }

        bool IsTrap(BaseEntity entity)
        {
            switch (entity?.ShortPrefabName)
            {
                case "flameturret.deployed":
                case "autoturret_deployed":
                case "spikes.floor":
                case "teslacoil.deployed":
                case "beartrap":
                case "landmine":
                case "guntrap.deployed":
                    return true;

                default: return false;
            }
        }

        bool RaidableBasesLoaded() => RaidableBases != null && RaidableBases.IsLoaded;

        bool PassRaidableBasesCheck(BaseEntity entity, Buff buff)
        {
            if (!RaidableBasesLoaded()) return true;
            switch (buff)
            {
                case Buff.Trap_Damage_Reduction:
                    if (!config.buff_settings.raid_perk_settings.trap_damage_reduction_settings.raidable_bases_only) return true;
                    break;

                case Buff.Trap_Damage_Increase:
                    if (!config.buff_settings.raid_perk_settings.trap_damage_increase_settings.raidable_bases_only) return true;
                    break;

                case Buff.Explosion_Radius:
                    if (!config.buff_settings.raid_perk_settings.Explosion_Radius_settings.raidable_bases_only) return true;
                    return Convert.ToBoolean(RaidableBases.Call("EventTerritory", entity.transform.position));

                case Buff.Double_Explosion_Chance:
                    if (!config.buff_settings.raid_perk_settings.Double_Explosion_chance_settings.raidable_bases_only) return true;
                    return Convert.ToBoolean(RaidableBases.Call("EventTerritory", entity.transform.position));

                case Buff.Lock_Picker:
                    if (!config.buff_settings.raid_perk_settings.Lock_Picker_settings.raidable_bases_only) return true;
                    break;

                case Buff.Dudless_Explosive:
                    if (!config.buff_settings.raid_perk_settings.Dudless_Explosiv_settings.raidable_bases_only) return true;
                    return Convert.ToBoolean(RaidableBases.Call("EventTerritory", entity.transform.position));

                case Buff.Trap_Spotter:
                    if (!config.buff_settings.raid_perk_settings.Trap_Spotter_settings.raidable_bases_only) return true;
                    return Convert.ToBoolean(RaidableBases.Call("EventTerritory", entity.transform.position));
            }
            return Convert.ToBoolean(RaidableBases.Call("HasEventEntity", entity));
        }

        bool PassRaidableBasesCheck(Vector3 pos, Buff buff)
        {
            if (!RaidableBasesLoaded()) return true;
            switch (buff)
            {
                case Buff.Raiding_Ultimate:
                    if (!config.ultimate_settings.ultimate_raiding.raidable_bases_only) return true;
                    return Convert.ToBoolean(RaidableBases.Call("EventTerritory", pos));

                default: return true;
            }
        }

        void HandleBearBuff(BasePlayer player, HitInfo info, Rust.DamageType damageType)
        {
            if (HasAnimalBuff(player, AnimalBuff.PolarBear) && OverShields.ContainsKey(player))
            {
                var shield_value = OverShields[player];
                var total_Damage = info.damageTypes.Total();
                if (shield_value <= 0) RemoveAnimalBuff(player);
                else if (total_Damage <= shield_value)
                {
                    OverShields[player] = shield_value - total_Damage;
                    info.damageTypes.ScaleAll(0f);
                    Overshield_main(player, shield_value - total_Damage);
                }
                else
                {
                    var excess_damage = total_Damage - shield_value;
                    Unsubscribe("OnEntityTakeDamage");
                    player.Hurt(excess_damage, damageType);
                    Subscribe("OnEntityTakeDamage");
                    info.damageTypes.ScaleAll(0f);
                    RemoveAnimalBuff(player);
                }
            }
        }

        private void OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (recycler.IsOn()) return;
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Recycler_Speed))
            {
                float speedDecrease = bd.buff_values[Buff.Recycler_Speed];
                recycler.CancelInvoke(nameof(recycler.RecycleThink));
                var recycler_speed = 5.0f - speedDecrease;
                if (recycler_speed <= 0.1) recycler_speed = 0.1f;
                timer.Once(0.1f, () => recycler.InvokeRepeating(recycler.RecycleThink, recycler_speed - 0.1f, recycler_speed));
            }
        }

        List<LootContainer> looted_crates = new List<LootContainer>();
        void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            switch (entity.ShortPrefabName)
            {
                case "codelockedhackablecrate_oilrig":
                case "codelockedhackablecrate":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.LootHackedCrate, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "heli_crate":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.LootHeliCrate, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "bradley_crate":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.LootBradleyCrate, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_basic":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_basic, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_elite":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_elite, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_mine":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_mine, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_normal":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_normal, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_normal_2":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_normal_2, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_normal_2_food":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_normal_2_food, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_normal_2_medical":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_normal_2_medical, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_tools":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_tools, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_underwater_advanced":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_underwater_advanced, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_underwater_basic":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_underwater_basic, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_ammunition":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_ammunition, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_food_1":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_food_1, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_food_2":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_food_2, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_fuel":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_fuel, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "crate_medical":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_medical, entity, false, false, entity.ShortPrefabName);
                    }
                    break;

                case "trash-pile-1":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_food_1, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "supply_drop":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.supply_drop, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "vehicle_parts":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_basic, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
                case "minecart":
                    if (!looted_crates.Contains(entity))
                    {
                        looted_crates.Add(entity);
                        AwardXP(player, config.xp_settings.xp_sources.crate_normal, entity, false, false, entity.ShortPrefabName);
                    }
                    break;
            }
        }

        [HookMethod("RolledLootPickup")]
        public bool RolledLootPickup(BasePlayer player)
        {
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd))
            {
                float value;
                if (bd.buff_values.TryGetValue(Buff.Loot_Pickup, out value) && (value >= 1f || RollSuccessful(value))) return true;
            }
            return false;
        }

        Dictionary<BasePlayer, bool> LastMagnetSuccess = new Dictionary<BasePlayer, bool>();

        void OnBonusItemDropped(Item item, BasePlayer player)
        {
            BuffDetails bd;
            bool result;
            if (LastMagnetSuccess.TryGetValue(player, out result) && result && buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Loot_Pickup))
            {
                GiveItem(player, item);
            }

        }

        bool IsBarrel(string shortname)
        {
            switch (shortname)
            {
                case "loot-barrel-1": return true;
                case "loot-barrel-2": return true;
                case "loot_barrel_1": return true;
                case "loot_barrel_2": return true;
                case "oil_barrel": return true;
                default: return false;
            }
        }

        void HandleLootPickup(BasePlayer player, LootContainer entity)
        {
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Loot_Pickup) && entity.inventory?.itemList != null && entity.inventory.itemList.Count > 0)
            {
                if (!LastMagnetSuccess.ContainsKey(player)) LastMagnetSuccess.Add(player, false);

                if (RollSuccessful(bd.buff_values[Buff.Loot_Pickup]))
                {
                    if (config.buff_settings.loot_pickup_buff_max_distance > 0 && Vector3.Distance(entity.transform.position, player.transform.position) > config.buff_settings.loot_pickup_buff_max_distance)
                    {
                        LastMagnetSuccess[player] = false;
                        return;
                    }

                    List<Item> item_drops = Pool.GetList<Item>();
                    if (!config.buff_settings.lootPickupBuffMeleeOnly || (player.GetHeldEntity() != null && player.GetHeldEntity() is BaseMelee))
                    {
                        item_drops.AddRange(entity.inventory.itemList);

                        BasePlayer _player = player;
                        NextTick(() =>
                        {
                            if (_player == null)
                            {
                                Pool.FreeList(ref item_drops);
                                return;
                            }
                            foreach (var item in item_drops)
                            {
                                var parent = item.GetOwnerPlayer();
                                if (parent == null)
                                {
                                    GiveItem(_player, item);
                                }
                            }
                            LastMagnetSuccess[player] = false;
                            Pool.FreeList(ref item_drops);
                        });
                    }
                    LastMagnetSuccess[player] = true;
                }
                else LastMagnetSuccess[player] = false;
            }
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            var player = info.InitiatorPlayer;
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return;
            BuffDetails bd;
            switch (entity.ShortPrefabName)
            {
                case "sulfur-ore":
                case "metal-ore":
                case "stone-ore":
                    float value;
                    if (buffDetails.TryGetValue(player.userID, out bd))
                    {
                        var heldEntity = player.GetHeldEntity();
                        if (bd.buff_values.TryGetValue(Buff.Node_Spawn_Chance, out value) && RollSuccessful(value * (heldEntity != null && heldEntity is Jackhammer ? config.tools_black_white_list_settings.power_tool_modifier.mining_luck_modifier : 1f)))
                        {
                            Vector3 pos = entity.transform.position;
                            string prefab = entity.PrefabName;
                            Quaternion rot = entity.transform.rotation;
                            ulong oldID = entity.net.ID.Value;

                            NextTick(() =>
                            {
                                var nodes = FindEntitiesOfType<OreResourceEntity>(pos, 2);
                                foreach (var node in nodes)
                                {
                                    if (node.PrefabName != prefab || node.net.ID.Value == oldID) continue;
                                    if (InRange(node.transform.position, pos, 0.1f))
                                    {
                                        Pool.FreeList(ref nodes);
                                        return;
                                    }
                                }
                                Pool.FreeList(ref nodes);

                                var newNode = GameManager.server.CreateEntity(prefab, pos, rot);
                                newNode.Spawn();
                                if (player != null && NotificationsOn(player)) Player.Message(player, lang.GetMessage("NodeSpawned", this, player.UserIDString), config.misc_settings.ChatID);
                            });
                        }
                    }
                    break;
                case "loot-barrel-1":
                case "loot-barrel-2":
                case "loot_barrel_1":
                case "loot_barrel_2":
                    AwardXP(player, config.xp_settings.xp_sources.Barrel, entity, false, false, entity.ShortPrefabName);
                    HandleLootPickup(player, (LootContainer)entity);
                    break;
                case "roadsign1":
                case "roadsign2":
                case "roadsign3":
                case "roadsign4":
                case "roadsign5":
                case "roadsign6":
                case "roadsign7":
                case "roadsign8":
                case "roadsign9":
                    AwardXP(player, config.xp_settings.xp_sources.RoadSign, entity, false, false, entity.ShortPrefabName);
                    HandleLootPickup(player, (LootContainer)entity);
                    break;
                case "oil_barrel":
                    AwardXP(player, config.xp_settings.xp_sources.Barrel, entity, false, false, entity.ShortPrefabName);
                    HandleLootPickup(player, (LootContainer)entity);
                    break;
                case "chicken":
                    AwardXP(player, config.xp_settings.xp_sources.SmallAnimal, entity as Chicken, false, false, entity.ShortPrefabName);
                    AddAnimalBuff(player, AnimalBuff.Chicken);
                    break;
                case "boar":
                    AwardXP(player, config.xp_settings.xp_sources.MediumAnimal, entity as Boar, false, false, entity.ShortPrefabName);
                    AddAnimalBuff(player, AnimalBuff.Boar);
                    break;
                case "stag":
                    AwardXP(player, config.xp_settings.xp_sources.MediumAnimal, entity as Stag, false, false, entity.ShortPrefabName);
                    AddAnimalBuff(player, AnimalBuff.Stag);
                    break;
                case "wolf":
                    AwardXP(player, config.xp_settings.xp_sources.MediumAnimal, entity as Wolf, false, false, entity.ShortPrefabName);
                    AddAnimalBuff(player, AnimalBuff.Wolf);
                    break;
                case "simpleshark":
                case "bear":
                    AwardXP(player, config.xp_settings.xp_sources.LargeAnimal, entity as Bear, false, false, entity.ShortPrefabName);
                    AddAnimalBuff(player, AnimalBuff.Bear);
                    break;
                case "polarbear":
                    AwardXP(player, config.xp_settings.xp_sources.LargeAnimal, entity, false, false, entity.ShortPrefabName);
                    AddAnimalBuff(player, AnimalBuff.PolarBear);
                    break;
                case "horse":
                case "testridablehorse":
                    AwardXP(player, config.xp_settings.xp_sources.LargeAnimal, entity, false, false, entity.ShortPrefabName);
                    break;
                case "bradleyapc":
                    if (config.xp_settings.UseLootDefender && LootDefender != null && LootDefender.IsLoaded) return;
                    AwardXP(player, config.xp_settings.xp_sources.BradleyAPC, entity, false, false, entity.ShortPrefabName);
                    break;
            }
        }

        private static bool InRange(Vector3 a, Vector3 b, float distance)
        {
            return (a - b).sqrMagnitude <= distance * distance;
        }

        void AddItemsToBarrel(ItemDefinition itemDef, int quantity, LootContainer entity)
        {
            entity.inventory.capacity++;
            var item = ItemManager.CreateByName(itemDef.shortname, quantity);
            if (!item.MoveToContainer(entity.inventory)) item.DropAndTossUpwards(entity.transform.position);
        }

        object OnPlayerAddModifiers(BasePlayer player, Item item, ItemModConsumable consumable)
        {
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - OnPlayerAddModifiers. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return null;
            }
            if (bd.buff_values.ContainsKey(Buff.Extra_Food_Water))
            {
                var gain = consumable.GetIfType(MetabolismAttribute.Type.Calories);
                if (gain > 0) player.metabolism.calories.value += bd.buff_values[Buff.Extra_Food_Water] * gain;
                gain = consumable.GetIfType(MetabolismAttribute.Type.Hydration);
                if (gain > 0) player.metabolism.hydration.value += bd.buff_values[Buff.Extra_Food_Water] * gain;
            }
            if (bd.buff_values.ContainsKey(Buff.Iron_Stomach))
            {
                if (consumable.GetIfType(MetabolismAttribute.Type.Poison) > 0)
                {
                    player.metabolism.poison.SetValue(player.metabolism.FindAttribute(MetabolismAttribute.Type.Poison).lastValue);
                    if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("IronTummy", this, player.UserIDString), item.info.displayName.english), config.misc_settings.ChatID);
                }
            }

            float value;
            if (bd.buff_values.TryGetValue(Buff.Extended_Tea_Duration, out value) && item.info.shortname.Contains("tea"))
            {
                string messageString = lang.GetMessage("ExtendedTeaDurationMessage", this, player.UserIDString);

                List<ModifierDefintion> mods = Pool.GetList<ModifierDefintion>();
                foreach (var mod in consumable.modifiers)
                {
                    var defaultDuration = GetTeaDuration(item.info.shortname);
                    var modifiedDuration = defaultDuration + (defaultDuration * value);
                    mods.Add(new ModifierDefintion
                    {
                        source = Modifier.ModifierSource.Tea,
                        type = mod.type,
                        duration = modifiedDuration,
                        value = mod.value
                    });
                    messageString += string.Format(lang.GetMessage("ExtendedTeaDurationMessageBody", this, player.UserIDString), mod.type.ToString().Replace('_', ' '), Math.Round(modifiedDuration / 60, 0));
                }

                player.modifiers.Add(mods);
                Pool.FreeList(ref mods);
                
                if (consumable.modifiers.Count > 0) Player.Message(player, messageString, config.misc_settings.ChatID);
                return true;
            }
            return null;
        }

        float GetTeaDuration(string shortname)
        {
            switch (shortname)
            {
                case "radiationresisttea.pure":
                case "radiationresisttea.advanced":
                case "radiationresisttea":
                case "oretea.pure":
                case "oretea.advanced":
                case "oretea":
                case "woodtea.pure":
                case "woodtea.advanced":
                case "woodtea":
                case "scraptea":
                    return 1800f;

                case "maxhealthtea.pure":
                case "maxhealthtea.advanced":
                case "maxhealthtea":
                    return 1200f;

                case "scraptea.pure":
                    return 3600f;

                case "scraptea.advanced":
                    return 2700f;

                case "supertea":
                    return 3600f;

                default: return 1800f;
            }
        }

        object OnPlayerWound(BasePlayer player, HitInfo info)
        {
            if (player.IsNpc || !player.userID.IsSteamId()) return null;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - OnPlayerWound. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return null;
            }
            if (bd.buff_values.ContainsKey(Buff.Wounded_Resist) && RollSuccessful(bd.buff_values[Buff.Wounded_Resist]))
            {
                if (NotificationsOn(player)) Player.Message(player, lang.GetMessage("WoundSave", this, player.UserIDString), config.misc_settings.ChatID);
                player.metabolism.radiation_level.SetValue(0);
                player.metabolism.radiation_poison.SetValue(0);
                player.metabolism.oxygen.SetValue(1);
                player.metabolism.temperature.SetValue(15);
                player.metabolism.bleeding.SetValue(0);
                player.health += 10;
                return false;
            }
            return null;
        }

        Dictionary<ulong, PlayerHelicopter> tracked_helis = new Dictionary<ulong, PlayerHelicopter>();
        Dictionary<ulong, MotorRowboat> tracked_rowboats = new Dictionary<ulong, MotorRowboat>();
        Dictionary<ulong, MotorRowboat> tracked_rhibs = new Dictionary<ulong, MotorRowboat>();

        // 444 == vehicle ultimate no damage.
        void AssignSkinToChildren(ulong id, List<BaseEntity> mountables)
        {
            if (mountables != null && mountables.Count > 0)
            {
                List<BaseEntity> tracked_children;
                if (!reduced_damage_entities.TryGetValue(id, out tracked_children)) reduced_damage_entities.Add(id, tracked_children = new List<BaseEntity>());
                foreach (var child in mountables)
                {
                    if (child == null || child.IsDestroyed) continue;
                    tracked_children.Add(child);
                    child.skinID = 444;
                    List<BaseEntity> children = Pool.GetList<BaseEntity>();
                    children.AddRange(child.children.Where(x => x.skinID != 444));
                    var parent = child.GetParentEntity();
                    if (parent != null && parent.skinID != 444) children.Add(parent);
                    AssignSkinToChildren(id, children);
                    Pool.FreeList(ref children);
                }
            }
        }

        void RestoreSkinToChildren(List<BaseEntity> mountables)
        {
            if (mountables == null || mountables.Count == 0) return;
            foreach (var entity in mountables)
            {
                if (entity != null && !entity.IsDestroyed)
                {
                    if (entity.skinID == 444) entity.skinID = 0;
                }
            }
            mountables.Clear();
        }

        Dictionary<ulong, List<BaseEntity>> reduced_damage_entities = new Dictionary<ulong, List<BaseEntity>>();

        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (player == null || entity == null || player.IsNpc || !player.userID.IsSteamId()) return;

            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - OnEntityMounted. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            if (bd.buff_values.ContainsKey(Buff.Vehicle_Ultimate) && IsUltimateEnabled(player, Buff.Vehicle_Ultimate))
            {
                if (entity.ShortPrefabName == "tugboatdriver") entity.GetParentEntity().skinID = 444;
                else
                {
                    List<BaseEntity> children = Pool.GetList<BaseEntity>();

                    if (entity.children != null) children.AddRange(entity.children);
                    var parent = entity.GetParentEntity();
                    if (parent != null) children.Add(parent);
                    AssignSkinToChildren(player.userID, children);
                    Pool.FreeList(ref children);
                }
            }

            var vehicle = entity.GetParentEntity();
            if (vehicle == null) return;
            var horse = vehicle as RidableHorse;
            if (horse != null && bd.buff_values.ContainsKey(Buff.Riding_Speed))
            {
                if (Interface.CallHook("STCanModifyHorse", player, horse, bd.buff_values[Buff.Riding_Speed]) != null) return;
                if (Cooking != null && Cooking.IsLoaded && Convert.ToBoolean(Cooking.Call("IsHorseBuffed", horse))) return;
                if (HorseStats.ContainsKey(horse.net.ID.Value)) RestoreHorseStats(horse);
                HorseStats.Add(horse.net.ID.Value, new HorseInfo()
                {
                    current_maxSpeed = horse.maxSpeed,
                    current_runSpeed = horse.runSpeed,
                    current_trotSpeed = horse.trotSpeed,
                    current_turnSpeed = horse.turnSpeed,
                    current_walkSpeed = horse.walkSpeed,
                    player = player,
                    horse = horse
                });
                var modifier = bd.buff_values[Buff.Riding_Speed];
                if (config.buff_settings.horse_buff_info.Increase_Horse_MaxSpeed) horse.maxSpeed += modifier * horse.maxSpeed;
                if (config.buff_settings.horse_buff_info.Increase_Horse_RunSpeed) horse.runSpeed += modifier * horse.runSpeed;
                if (config.buff_settings.horse_buff_info.Increase_Horse_TrotSpeed) horse.trotSpeed += modifier * horse.trotSpeed;
                if (config.buff_settings.horse_buff_info.Increase_Horse_TurnSpeed) horse.turnSpeed += modifier * horse.turnSpeed;
                if (config.buff_settings.horse_buff_info.Increase_Horse_WalkSpeed) horse.walkSpeed += modifier * horse.walkSpeed;
                return;
            }
            else if (vehicle is PlayerHelicopter)
            {
                var mini = vehicle as PlayerHelicopter;
                float value;
                if (bd.buff_values.TryGetValue(Buff.Heli_Fuel_Rate, out value))
                {
                    if (mini.fuelPerSec < 0.5f) return;
                    var fuelSystem = mini.GetFuelSystem();
                    if (fuelSystem == null || fuelSystem.nextFuelCheckTime == float.MaxValue || fuelSystem.GetFuelContainer().HasFlag(BaseEntity.Flags.Locked)) return;
                    if (tracked_helis.ContainsKey(mini.net.ID.Value)) return;
                    tracked_helis.Add(mini.net.ID.Value, mini);
                    var fuel_rate = default_heli_fuel_rate - (default_heli_fuel_rate * value);
                    mini.fuelPerSec = fuel_rate;
                }
                if (bd.buff_values.TryGetValue(Buff.Heli_Speed, out value))
                {
                    if (MiniStats.ContainsKey(mini.net.ID.Value)) return;
                    MiniStats.Add(mini.net.ID.Value, new MiniInfo(player, mini, mini.liftFraction, mini.engineThrustMax));
                    mini.liftFraction += mini.liftFraction * value;
                    mini.engineThrustMax += mini.engineThrustMax * value;
                    mini.SendNetworkUpdate();
                }
            }
            else if (vehicle is MotorRowboat)
            {
                var boat = vehicle as MotorRowboat;

                float value;
                if (bd.buff_values.TryGetValue(Buff.Boat_Fuel_Rate, out value))
                {
                    ModifyBoatFuelRate(boat, player, value);
                }
                if (config.buff_settings.boat_turbo_on_mount) IncreaseBoatSpeed(player, boat);
            }
        }

        void ModifyBoatFuelRate(MotorRowboat boat, BasePlayer player, float value)
        {
            if (boat is RHIB)
            {
                if (boat.fuelPerSec >= 0.25) return;

                var fuelSystem = boat.GetFuelSystem();
                if (fuelSystem == null || fuelSystem.nextFuelCheckTime == float.MaxValue || fuelSystem.GetFuelContainer().HasFlag(BaseEntity.Flags.Locked)) return;

                if (tracked_rhibs.ContainsKey(boat.net.ID.Value)) return;
                tracked_rhibs.Add(boat.net.ID.Value, boat);
                var fuel_rate = default_rhib_fuel_rate - (default_rhib_fuel_rate * value);
                boat.fuelPerSec = fuel_rate;
            }
            else
            {
                if (boat.fuelPerSec < 0.1) return;

                var fuelSystem = boat.GetFuelSystem();
                if (fuelSystem == null || fuelSystem.nextFuelCheckTime == float.MaxValue || fuelSystem.GetFuelContainer().HasFlag(BaseEntity.Flags.Locked)) return;

                if (tracked_rowboats.ContainsKey(boat.net.ID.Value)) return;
                tracked_rowboats.Add(boat.net.ID.Value, boat);
                var fuel_rate = default_rowboat_fuel_rate - (default_rowboat_fuel_rate * value);
                boat.fuelPerSec = fuel_rate;
            }
        }

        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Vehicle_Mechanic) && info?.HitEntity is BaseVehicle)
            {
                var vehicle = info.HitEntity as BaseVehicle;
                var amount = vehicle.MaxHealth() - vehicle.health;
                if (amount <= 0) return null;
                else
                {
                    vehicle.Heal(amount);
                    if (!string.IsNullOrEmpty(config.effect_settings.repair_effect)) EffectNetwork.Send(new Effect(config.effect_settings.repair_effect, player.transform.position, player.transform.position), player.net.connection);
                    return false;
                }
            }

            return null;
        }

        void RestoreHorseStats(RidableHorse horse, bool doRemove = true)
        {
            HorseInfo hd;
            if (HorseStats.TryGetValue(horse.net.ID.Value, out hd))
            {
                horse.maxSpeed = hd.current_maxSpeed;
                horse.runSpeed = hd.current_runSpeed;
                horse.trotSpeed = hd.current_trotSpeed;
                horse.turnSpeed = hd.current_turnSpeed;
                horse.walkSpeed = hd.current_walkSpeed;
            }
            if (doRemove) HorseStats.Remove(horse.net.ID.Value);
        }

        void RestoreMiniStats(PlayerHelicopter mini, BasePlayer dismounter, bool doRemove = true)
        {
            if (mini == null) return;
            MiniInfo data;
            if (!MiniStats.TryGetValue(mini.net.ID.Value, out data) || (dismounter != null && dismounter != data.player)) return;
            if (mini.IsAlive())
            {
                mini.liftFraction = data.old_lift_fraction;
                mini.engineThrustMax = data.old_engineThrustMax;
                mini.SendNetworkUpdate();
            }
            if (doRemove) MiniStats.Remove(mini.net.ID.Value);
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return;
            DestroyRaidBehaviour(player);

            var attacker = info?.Initiator ?? info?.WeaponPrefab?.creatorEntity ?? info?.ProjectilePrefab?.owner;

            RemoveAnimalBuff(player);
            if (HorseStats.Count > 0)
            {
                List<KeyValuePair<ulong, HorseInfo>> temp_horse_list = Pool.GetList<KeyValuePair<ulong, HorseInfo>>();
                temp_horse_list.AddRange(HorseStats);
                foreach (KeyValuePair<ulong, HorseInfo> kvp in temp_horse_list)
                {
                    if (kvp.Value.player == player)
                    {
                        if (kvp.Value.horse == null)
                        {
                            HorseStats.Remove(kvp.Key);
                            break;
                        }
                        var horse = kvp.Value.horse;
                        if (horse != null && horse.IsAlive())
                        {
                            RestoreHorseStats(horse);
                            break;
                        }
                    }
                }
                Pool.FreeList(ref temp_horse_list);
            }
            if (MiniStats.Count > 0)
            {
                List<KeyValuePair<ulong, MiniInfo>> temp_mini_list = Pool.GetList<KeyValuePair<ulong, MiniInfo>>();
                temp_mini_list.AddRange(MiniStats);
                foreach (var entry in MiniStats)
                {
                    if (entry.Value.player == player)
                    {
                        RestoreMiniStats(entry.Value.mini, player);
                        break;
                    }
                }
                Pool.FreeList(ref temp_mini_list);
            }
            if (config.general_settings.drop_bag_on_death && !permission.UserHasPermission(player.UserIDString, "skilltree.bag.keepondeath"))
            {
                PlayerInfo pi;
                if (pcdData.pEntity.TryGetValue(player.userID, out pi) && pi.pouch_items != null && pi.pouch_items.Count > 0 && Interface.CallHook("STOnPouchDrop", player) == null && !player.InSafeZone())
                {
                    var bag = GenerateBag(player, 42);
                    if (bag != null && bag.inventory?.itemList != null && bag.inventory.itemList.Count > 0)
                    {
                        var pos = player.transform.position;
                        var rot = player.transform.rotation;
                        timer.Once(0.1f, () =>
                        {
                            bag.inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", pos, rot, 0);
                            pi.pouch_items.Clear();
                            containers.Remove(bag.inventory.uid.Value);
                            bag.KillMessage();
                        });
                    }
                }
            }
            HandleResurrection(player, attacker);
            if (attacker == null) return;
            if (attacker is BasePlayer && player == (BasePlayer)attacker)
            {
                if (config.xp_settings.xp_loss_settings.suicide_death_penalty > 0) LoseXP(player, DeathType.Suicide);
                return;
            }
            // Attacker is real player
            if (info.InitiatorPlayer != null && !info.InitiatorPlayer.IsNpc && info.InitiatorPlayer.userID.IsSteamId())
            {
                switch (player.GetType().Name)
                {
                    case "ZombieNPC":
                        AwardXP(info.InitiatorPlayer, config.xp_settings.xp_sources.Zombie, player, false, false, "zombie");
                        break;

                    case "NpcRaider":
                    case "RandomRaider":
                        AwardXP(info.InitiatorPlayer, config.xp_settings.xp_sources.Raider, player, false, false, "raider");
                        break;

                    case "JetPilot":
                        AwardXP(info.InitiatorPlayer, config.xp_settings.xp_sources.JetPilot, player, false, false, "jetpilot");
                        break;

                    default:
                        if (config.xp_settings.xp_loss_settings.pvp_death_penalty > 0) LoseXP(player, DeathType.PVP);
                        break;
                }
            }
            else if (config.xp_settings.xp_loss_settings.pve_death_penalty > 0 && (attacker.IsNpc || attacker is BaseAnimalNPC))
            {
                LoseXP(player, DeathType.PVE);
            }
        }

        void HandleResurrection(BasePlayer player, BaseEntity attacker)
        {
            BuffDetails bd;
            if (((attacker != null && attacker != player) || !config.ultimate_settings.ultimate_medical.prevent_on_suicide) && buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Medical_Ultimate) && IsUltimateEnabled(player, Buff.Medical_Ultimate))
            {
                SendResurrectionButton(player, player.transform.position);
            }
        }

        void OnPlayerDeath(ScarecrowNPC scarecrow, HitInfo info)
        {
            if (scarecrow == null || info == null || info.InitiatorPlayer == null || info.InitiatorPlayer.IsNpc || !info.InitiatorPlayer.userID.IsSteamId()) return;
            AwardXP(info.InitiatorPlayer, config.xp_settings.xp_sources.Scarecrow, scarecrow, false, false, "scarecrow");
        }


        void OnPlayerDeath(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            var attacker = info.InitiatorPlayer;
            if (attacker == null || attacker.IsNpc) return;
            if (config.misc_settings.botRespawnSettings.enabled && BotReSpawn != null && BotReSpawn.IsLoaded && Convert.ToBoolean(BotReSpawn.Call("IsBotReSpawn", npc))) return;
            double betterNPCXP = 0;
            if (npc.skinID == 11162132011012 && config.betternpc_settings.NPC_xp_table.TryGetValue(npc.displayName, out betterNPCXP))
            {
                if (betterNPCXP == 0) betterNPCXP = config.xp_settings.xp_sources.ScientistNormal;
                AwardXP(attacker, betterNPCXP, npc);
            }
            else if (npc.ShortPrefabName == "scientistnpc_heavy")
            {
                AwardXP(attacker, config.xp_settings.xp_sources.ScientistHeavy, npc, false, false, npc.name ?? npc.ShortPrefabName);
            }
            else
            {
                AwardXP(attacker, config.xp_settings.xp_sources.ScientistNormal, npc, false, false, npc.name ?? npc.ShortPrefabName);
            }

        }

        void OnPlayerDeath(TunnelDweller npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            var attacker = info.InitiatorPlayer;
            if (attacker == null || attacker.IsNpc || !attacker.userID.IsSteamId()) return;
            AwardXP(attacker, config.xp_settings.xp_sources.TunnelDweller, npc, false, false, npc.name ?? npc.ShortPrefabName);
        }

        void OnPlayerDeath(UnderwaterDweller npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            var attacker = info.InitiatorPlayer;
            if (attacker == null || attacker.IsNpc || !attacker.userID.IsSteamId()) return;
            AwardXP(attacker, config.xp_settings.xp_sources.UnderwaterDweller, npc, false, false, npc.name ?? npc.ShortPrefabName);
        }

        void LoseXP(BasePlayer player, DeathType type)
        {
            if (config.xp_settings.xp_loss_settings.prevent_offline_xp_loss && !player.IsConnected) return;
            if (Interface.CallHook("STOnLoseXP", player) != null || (config.xp_settings.prevent_xp_loss && ((EventManager != null && EventManager.IsLoaded && Convert.ToBoolean(EventManager.Call("IsEventPlayer", player))) || (EventHelper != null && EventHelper.IsLoaded && Convert.ToBoolean(EventHelper.Call("EMPlayerDiedAtEvent", player)))))) return;
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return;
            var Level = config.level.GetLevel(playerData.xp);
            var LevelStartXP = config.level.GetLevelStartXP(Level) + 1;
            var xp_loss = GetXPLoss(player, (config.xp_settings.xp_loss_settings.percentage_of_current_xp ? playerData.xp - LevelStartXP : config.level.GetLevelStartXP(Level + 1) + 1 - LevelStartXP), type);

            if (xp_loss > 0 && (config.xp_settings.xp_loss_settings.no_xp_loss_time == 0 || playerData.last_xp_loss.AddSeconds(config.xp_settings.xp_loss_settings.no_xp_loss_time) < DateTime.Now))
            {
                playerData.last_xp_loss = DateTime.Now;
                if (playerData.xp - LevelStartXP > xp_loss)
                {
                    playerData.xp -= xp_loss;
                    Player.Message(player, string.Format(lang.GetMessage("LostXP", this, player.UserIDString), Math.Round(xp_loss, config.xp_settings.xp_rounding)), config.misc_settings.ChatID);
                }
                else
                {
                    if (config.xp_settings.xp_loss_settings.allow_xp_debt)
                    {
                        var xpLeft = playerData.xp - LevelStartXP;
                        var excess = xp_loss - xpLeft;
                        playerData.xp = LevelStartXP;
                        playerData.xp_debt += excess;

                        Player.Message(player, string.Format(lang.GetMessage("AccumulatedXPDebt", this, player.UserIDString), Math.Round(excess, config.xp_settings.xp_rounding), Math.Round(playerData.xp_debt, config.xp_settings.xp_rounding)), config.misc_settings.ChatID);

                    }
                    else
                    {
                        Player.Message(player, string.Format(lang.GetMessage("LostXP", this, player.UserIDString), playerData.xp - LevelStartXP), config.misc_settings.ChatID);
                        playerData.xp = LevelStartXP;
                    }
                }
            }
            CheckLevel(player);
            UpdateXP(player, playerData);
        }

        void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            if (player.IsNpc || !player.userID.IsSteamId() || entity == null || entity.GetParentEntity() == null) return;

            if (reduced_damage_entities.ContainsKey(player.userID)) RestoreSkinToChildren(reduced_damage_entities[player.userID]);

            var vehicle = entity.GetParentEntity();
            var horse = vehicle as RidableHorse;
            if (horse != null)
            {
                if (HorseStats.ContainsKey(horse.net.ID.Value))
                {
                    if (horse != null && horse.IsAlive()) RestoreHorseStats(horse);
                    else HorseStats.Remove(horse.net.ID.Value);
                }
            }
            else if (vehicle is BaseBoat)
            {
                var boat = vehicle as MotorRowboat;

                ResetBoatSpeed(boat, player);

                BuffDetails bd;
                if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Boat_Fuel_Rate))
                {

                    if (boat != null)
                    {
                        if (tracked_rowboats.ContainsKey(boat.net.ID.Value) && boat.IsAlive())
                        {
                            boat.fuelPerSec = default_rowboat_fuel_rate;
                            tracked_rowboats.Remove(boat.net.ID.Value);
                        }
                        else if (tracked_rhibs.ContainsKey(boat.net.ID.Value) && boat.IsAlive())
                        {
                            boat.fuelPerSec = default_rhib_fuel_rate;
                            tracked_rowboats.Remove(boat.net.ID.Value);
                        }
                    }
                }
            }
            else if (vehicle is Minicopter)
            {
                var mini = vehicle as Minicopter;
                BuffDetails bd;
                if (!buffDetails.TryGetValue(player.userID, out bd)) return;
                if (bd.buff_values.ContainsKey(Buff.Heli_Fuel_Rate))
                {
                    if (!tracked_helis.ContainsKey(mini.net.ID.Value)) return;
                    mini.fuelPerSec = default_heli_fuel_rate;
                    tracked_helis.Remove(mini.net.ID.Value);
                }
                if (bd.buff_values.ContainsKey(Buff.Heli_Speed))
                {
                    RestoreMiniStats(mini, player);
                }
            }
        }

        object OnPayForUpgrade(BasePlayer player, BuildingBlock block, ConstructionGrade gradeTarget)
        {
            BuffDetails bd;
            if (player != null && buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Upgrade_Refund) && RollSuccessful(bd.buff_values[Buff.Upgrade_Refund]))
            {
                if (NotificationsOn(player)) Player.Message(player, lang.GetMessage("FreeUpgrade", this, player.UserIDString), config.misc_settings.ChatID);
                return 0;
            }
            return null;
        }

        void ChangeBoatSpeedCMD(BasePlayer player)
        {
            var boat = player.GetMountedVehicle() as MotorRowboat;
            if (boat == null)
            {
                Player.Message(player, lang.GetMessage("NoBoatFound", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            if (Boats.ContainsKey(boat.net.ID.Value)) ResetBoatSpeed(boat, player);
            else IncreaseBoatSpeed(player, boat);
        }

        void IncreaseBoatSpeed(BasePlayer player, MotorRowboat boat)
        {
            if (boat == null) return;

            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd)) return;

            float value;
            if (!bd.buff_values.TryGetValue(Buff.Boat_Speed, out value)) return;

            if (Interface.CallHook("STOnModifyBoatSpeed", player, boat) != null) return;

            var defaultSpeed = DefaultBoatSpeed(boat.ShortPrefabName);

            BoatInfo bi;
            if (Boats.TryGetValue(boat.net.ID.Value, out bi))
            {
                if (boat.engineThrust > defaultSpeed) return;

                bi.defaultSpeed = defaultSpeed;
                boat.engineThrust += value * defaultSpeed;
                MessageMounted(boat, "TurboToggleOn");
                //Player.Message(player, lang.GetMessage("TurboToggleOn", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            Boats.Add(boat.net.ID.Value, new BoatInfo(player, boat, defaultSpeed));
            boat.engineThrust += value * defaultSpeed;
            MessageMounted(boat, "TurboToggleOn");
            //Player.Message(player, lang.GetMessage("TurboToggleOn", this, player.UserIDString), config.misc_settings.ChatID);
        }

        void MessageMounted(MotorRowboat boat, string langKey)
        {
            foreach (var entity in boat.children)
            {
                var seat = entity as BaseVehicleSeat;
                if (seat != null && seat._mounted != null) Player.Message(seat._mounted, lang.GetMessage(langKey, this, seat._mounted.UserIDString), config.misc_settings.ChatID);

            }
        }

        void ResetBoatSpeed(MotorRowboat boat, BasePlayer player = null, bool doRemove = true)
        {
            if (boat == null) return;
            BoatInfo bi;
            if (!Boats.TryGetValue(boat.net.ID.Value, out bi)) return;

            if (player != null && bi.player != player) return;

            var defaultSpeed = DefaultBoatSpeed(boat.ShortPrefabName);
            if (boat.engineThrust > defaultSpeed)
            {
                MessageMounted(boat, "TurboToggleOff");
                if (player != null) Player.Message(player, lang.GetMessage("TurboToggleOff", this, player.UserIDString), config.misc_settings.ChatID);
                boat.engineThrust = defaultSpeed;
            }

            if (doRemove) Boats.Remove(boat.net.ID.Value);
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.FIRE_THIRD) && player.isMounted)
            {
                var boat = player.GetMountedVehicle() as MotorRowboat;
                if (boat == null) return;

                if (Boats.ContainsKey(boat.net.ID.Value)) ResetBoatSpeed(boat, player, true);
                else IncreaseBoatSpeed(player, boat);
            }   
        }

        Dictionary<ResearchTable, BasePlayer> Researchers = new Dictionary<ResearchTable, BasePlayer>();
        object OnResearchCostDetermine(Item item)
        {
            ResearchTable researchTable = item.GetEntityOwner() as ResearchTable;
            if (researchTable == null) return null;
            var player = researchTable.user;
            if (player != null) Researchers[researchTable] = player;
            else if (Researchers.ContainsKey(researchTable)) player = Researchers[researchTable];
            if (!researchTable.IsResearching()) return null;
            BuffDetails bd;
            float value;
            if (player != null && buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.TryGetValue(Buff.Research_Refund, out value) && RollSuccessful(value))
            {
                if (NotificationsOn(player)) Player.Message(player, lang.GetMessage("ScrapRefund", this, player.UserIDString), config.misc_settings.ChatID);
                return 0;
            }
            return null;
        }

        void SaveNewNodesToConfig()
        {
            if (QueuedNodes == null || QueuedNodes.Count == 0) return;
            int count = 0;
            foreach (var tree in QueuedNodes)
            {
                foreach (var node in tree.Value)
                {
                    Configuration.TreeInfo treeData;
                    if (!config.trees.TryGetValue(tree.Key, out treeData)) continue;
                    if (!treeData.nodes.ContainsKey(node.Key))
                    {
                        treeData.nodes.Add(node.Key, node.Value);
                        count++;
                    }
                }
            }
            Puts($"Saved {count} new nodes. Reloading plugin.");
            SaveConfig();
            NewNodesAdded = false;
            QueuedNodes.Clear();

            Interface.Oxide.ReloadPlugin(Name);
        }

        void OnServerSave()
        {
            SaveData();
            if (NewNodesAdded)
            {
                try
                {
                    SaveNewNodesToConfig();
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        DoClear(player);
                        LoggingOff(player);
                        HandleNewConnection(player);
                    }
                }
                catch { }
            }
        }

        void HandleNewConnection(BasePlayer player)
        {
            SetupPlayer(player.userID, player.displayName);
            UpdateInstancedData(player);
            PlayerInfo playerData = pcdData.pEntity[player.userID];
            if (playerData.xp_hud) UpdateXP(player, playerData);
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Metabolism_Boost)) IncreaseCalories(player, bd.buff_values[Buff.Metabolism_Boost]);
            LoggedOn(player, playerData);
        }

        List<LootContainer> looted_containers = new List<LootContainer>();
        List<ItemDefinition> component_item_list = new List<ItemDefinition>();
        List<ItemDefinition> electrical_item_list = new List<ItemDefinition>();

        ItemDefinition GetRandomItemDef(ItemCategory category)
        {
            if (category == ItemCategory.Component) return component_item_list.GetRandom();
            if (category == ItemCategory.Electrical) return electrical_item_list.GetRandom();
            return null;
        }

        void AddItemToContainer(LootContainer container, ItemDefinition itemDef, int quantity)
        {
            if (container == null || container.inventory == null || itemDef == null) return;
            container.inventory.capacity++;
            container.inventorySlots++;
            ItemManager.CreateByName(itemDef.shortname, quantity)?.MoveToContainer(container.inventory);
        }

        void CanLootEntity(BasePlayer player, LootContainer container)
        {
            if (container == null || player == null) return;

            BuffDetails bd;
            if (player != null && buffDetails.TryGetValue(player.userID, out bd))
            {
                if (!looted_containers.Contains(container))
                {
                    if (Interface.CallHook("STCanReceiveBonusLootFromContainer", player, container) != null)
                    {
                        looted_containers.Add(container);
                        return;
                    }

                    float value;
                    if (config.loot_settings.loot_crate_whitelist.IsNullOrEmpty() || (config.loot_settings.loot_crate_whitelist.Contains(container.PrefabName)))
                    {
                        if (bd.buff_values.TryGetValue(Buff.Component_Chest, out value) && RollSuccessful(value) && container.inventorySlots < 12)
                        {
                            var itemDef = GetRandomItemDef(ItemCategory.Component);
                            var quantity = UnityEngine.Random.Range(config.buff_settings.min_components, config.buff_settings.max_components);
                            AddItemToContainer(container, itemDef, quantity);
                        }
                        if (bd.buff_values.TryGetValue(Buff.Electronic_Chest, out value) && RollSuccessful(value) && container.inventorySlots < 12)
                        {
                            var itemDef = GetRandomItemDef(ItemCategory.Electrical);
                            var quantity = UnityEngine.Random.Range(config.buff_settings.min_electrical_components, config.buff_settings.max_electrical_components);
                            AddItemToContainer(container, itemDef, quantity);
                        }
                        if (bd.buff_values.TryGetValue(Buff.Extra_Scrap_Crate, out value) && RollSuccessful(value))
                        {
                            var itemDef = ItemManager.FindItemDefinition("scrap");
                            var quantity = UnityEngine.Random.Range(config.buff_settings.min_extra_scrap, config.buff_settings.max_extra_scrap);
                            AddItemToContainer(container, itemDef, quantity);
                        }
                    }

                    if (bd.buff_values.TryGetValue(Buff.DeepSeaLooter, out value) && DeepSeaLooterLootTable.ContainsKey(container.PrefabName) && RollSuccessful(value))
                    {
                        var loot = DeepSeaLooterLootTable[container.PrefabName].GetRandom();
                        container.inventory.capacity++;
                        container.inventorySlots++;
                        ItemManager.CreateByName(loot.shortname, UnityEngine.Random.Range(loot.min, loot.max)).MoveToContainer(container.inventory);
                    }

                    if (bd.buff_values.TryGetValue(Buff.Tea_Looter, out value) && RollSuccessful(value) && IsFoodCrate(container.ShortPrefabName))
                    {
                        var item = ItemManager.CreateByName(RollTea(), config.buff_settings.tea_looter_settings.max_tea < 2 ? 1 : Math.Max(UnityEngine.Random.Range(config.buff_settings.tea_looter_settings.min_tea, config.buff_settings.tea_looter_settings.max_tea + 1), 1));
                        if (item != null)
                        {
                            Player.Message(player, string.Format(lang.GetMessage("TeaFound", this, player.UserIDString), item.amount, item.info.displayName.english));
                            container.inventory.capacity++;
                            container.inventorySlots++;
                            if (!item.MoveToContainer(container.inventory)) player.GiveItem(item);
                        }
                    }
                    looted_containers.Add(container);
                }
            }
        }

        string RollTea()
        {
            var totalWeight = config.buff_settings.tea_looter_settings.TeaDropTable.Sum(x => x.Value);
            var roll = UnityEngine.Random.Range(0, totalWeight + 1);

            var count = 0;
            foreach (var tea in config.buff_settings.tea_looter_settings.TeaDropTable)
            {
                if (tea.Value <= 0) continue;
                count += tea.Value;
                if (roll <= count) return tea.Key;
            }

            Puts("Error: Failed to find tea for some reason. Rolling a random tea.");
            List<string> randomTea = Pool.GetList<string>();
            foreach (var tea in config.buff_settings.tea_looter_settings.TeaDropTable)
                if (tea.Value > 0) randomTea.Add(tea.Key);

            var result = randomTea.GetRandom();
            Pool.FreeList(ref randomTea);

            return result;
        }

        bool IsFoodCrate(string shortname)
        {
            switch (shortname)
            {
                case "crate_normal_2_food":
                case "invisible_crate_normal_2_food":
                case "crate_food_1":
                case "crate_food_2":
                case "wagon_crate_normal_2_food":
                case "foodbox":
                case "invisible_foodbox":
                case "dmfood":
                    return true;

                default: return false;
            }
        }

        [PluginReference]
        private Plugin ImageLibrary, Economics, ServerRewards, EventManager, BotReSpawn, Cooking, UINotify, ZombieHorde, EventHelper, RaidableBases, LootDefender;

        Dictionary<Buff, BuffType> BuffBuffType = new Dictionary<Buff, BuffType>();

        private Dictionary<string, string> loadOrder = new Dictionary<string, string>();

        private KeyValuePair<string, string> ExtraPocketsImg;
        private KeyValuePair<string, ulong> ExtraPocketsImgSkin;
        Timer LogTimer;
        List<string> TrackedPermissionPerms = new List<string>();

        void OnServerInitialized(bool initial)
        {
            var foundNewContent = false;
            if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase) && ImageLibrary == null)
            {
                Puts("Setting cache type to skin as ImageLibrary is not loaded.");
                config.general_settings.image_cache_source = "skinid";
                foundNewContent = true;
            }
            bool allfalse = true;
            foreach (var tree in config.trees)
            {
                if (tree.Value.enabled) allfalse = false;
                foreach (var node in tree.Value.nodes)
                {
                    if (!string.IsNullOrEmpty(node.Value.required_permission))
                    {
                        if (!node.Value.required_permission.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase))
                        {
                            node.Value.required_permission = "skilltree." + node.Value.required_permission;
                            foundNewContent = true;
                        }
                        if (!permission.PermissionExists(node.Value.required_permission, this))
                        {
                            permission.RegisterPermission(node.Value.required_permission, this);
                        }
                        if (!TrackedPermissionPerms.Contains(node.Value.required_permission)) TrackedPermissionPerms.Add(node.Value.required_permission);
                    }

                    var defaultTree = DefaultTrees.ContainsKey(tree.Key) ? DefaultTrees[tree.Key] : null;
                    if (defaultTree == null) continue;
                    var defaultNode = defaultTree != null && defaultTree.nodes.ContainsKey(node.Key) ? defaultTree.nodes[node.Key] : null;
                    if (defaultNode == null) continue;
                    foreach (var link in OldLinks)
                    {
                        if (link.Equals(node.Value.icon_url, StringComparison.OrdinalIgnoreCase))
                        {
                            node.Value.icon_url = defaultNode.icon_url;
                            Puts($"Replacing the URL for {node.Key} with {node.Value.icon_url}");
                            foundNewContent = true;
                        }
                    }
                    if (node.Value.skin == 0 && config.misc_settings.update_skinIDs_from_default)
                    {
                        node.Value.skin = defaultNode.skin;
                        Puts($"Updating node skin id for: {node.Key}");
                        foundNewContent = true;
                    }
                }
            }

            if (!config.general_settings.require_tree_perms && TrackedPermissionPerms.Count == 0)
            {
                Unsubscribe(nameof(OnGroupPermissionRevoked));
                Unsubscribe(nameof(OnGroupPermissionGranted));
                Unsubscribe(nameof(OnUserPermissionRevoked));
                Unsubscribe(nameof(OnUserPermissionGranted));
                Unsubscribe(nameof(OnUserGroupAdded));
                Unsubscribe(nameof(OnUserGroupRemoved));
            }

            if (allfalse)
            {
                foreach (var tree in config.trees)
                    tree.Value.enabled = true;
                foundNewContent = true;
            }

            if (pcdData.highest_player == 0) bonus_given = true;

            if (config.general_settings.respec_cost_override.Count == 0)
            {
                config.general_settings.respec_cost_override.Add("vip", Math.Round(config.general_settings.respec_cost / 2, 0));
                foundNewContent = true;
                if (!permission.PermissionExists("skilltree.vip")) permission.RegisterPermission("skilltree.vip", this);
            }

            if (config.general_settings.max_skill_points_override.Count == 0)
            {
                config.general_settings.max_skill_points_override.Add("vip", config.general_settings.max_skill_points + (Convert.ToInt32(config.general_settings.max_skill_points * 0.2)));
                foundNewContent = true;
                if (!permission.PermissionExists("skilltree.vip")) permission.RegisterPermission("skilltree.vip", this);
            }

            if (config.xp_settings.xp_loss_settings.xp_loss_override.Count == 0)
            {
                config.xp_settings.xp_loss_settings.xp_loss_override.Add("vip", 0.5);
                foundNewContent = true;
                if (!permission.PermissionExists("skilltree.vip")) permission.RegisterPermission("skilltree.vip", this);
            }

            if (config.rested_xp_settings.rested_xp_modifier_perm_mod.Count == 0)
            {
                config.rested_xp_settings.rested_xp_modifier_perm_mod.Add("restedxp.10", 0.1f);
                foundNewContent = true;
                if (!permission.PermissionExists("skilltree.vip")) permission.RegisterPermission("skilltree.vip", this);
            }

            if (config.wipe_update_settings.starting_skill_point_overrides.Count == 0)
            {
                config.wipe_update_settings.starting_skill_point_overrides.Add("vip.starting.points", 5);
                foundNewContent = true;
                if (!permission.PermissionExists("skilltree.vip.starting.points")) permission.RegisterPermission("skilltree.vip.starting.points", this);
            }

            foreach (var perm in config.xp_settings.xp_loss_settings.xp_loss_override)
            {
                if (!permission.PermissionExists("skilltree." + perm.Key, this)) permission.RegisterPermission("skilltree." + perm.Key, this);
            }

            foreach (var perm in config.general_settings.level_requirement_override)
            {
                var permStr = perm.Key;
                if (!permStr.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase)) permStr = "skilltree." + perm.Key;
                permission.RegisterPermission(permStr, this);
            }

            foreach (var perm in config.general_settings.point_requirement_override)
            {
                var permStr = perm.Key;
                if (!permStr.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase)) permStr = "skilltree." + perm.Key;
                permission.RegisterPermission(permStr, this);
            }

            config.level.CalculateTable(config.general_settings.max_player_level);
            if (Configuration.ExperienceInfo.UpdatedTable) foundNewContent = true;
            if (BotReSpawn != null && config.misc_settings.botRespawnSettings.enabled)
            {
                var BotReSpawnBots = (Dictionary<string, List<ulong>>)BotReSpawn?.Call("BotReSpawnBots");
                foreach (var profile in BotReSpawnBots)
                {
                    if (!config.misc_settings.botRespawnSettings.botrespawn_profiles.ContainsKey(profile.Key))
                    {
                        config.misc_settings.botRespawnSettings.botrespawn_profiles.Add(profile.Key, config.xp_settings.xp_sources.default_botrespawn);
                        Puts($"Added new BotReSpawn profile: {profile.Key}. Allocated default xp value of: {config.xp_settings.xp_sources.default_botrespawn}.");
                        foundNewContent = true;
                    }
                }
            }
            else Unsubscribe(nameof(OnBotReSpawnNPCKilled));
            // Checks for new trees added to the plugin between updates and adds them to the users config.

            Dictionary<string, Configuration.TreeInfo> trees = DefaultTrees;
            foreach (var tree in DefaultTrees)
            {
                if (!config.trees.ContainsKey(tree.Key) && config.wipe_update_settings.auto_update_trees)
                {
                    config.trees.Add(tree.Key, tree.Value);
                    Puts($"Adding new tree: {tree.Key.ToString()}");
                    foundNewContent = true;
                }
                else
                {
                    foreach (var node in tree.Value.nodes)
                    {
                        if (!config.trees[tree.Key].nodes.ContainsKey(node.Key) && config.wipe_update_settings.auto_update_nodes)
                        {
                            Puts($"Adding new node: {node.Key}");
                            config.trees[tree.Key].nodes.Add(node.Key, node.Value);
                            foundNewContent = true;
                        }
                        var configNodes = config.trees[tree.Key].nodes;
                        if (config.general_settings.image_cache_source.Equals("skinid", StringComparison.OrdinalIgnoreCase) && configNodes.ContainsKey(node.Key) && configNodes[node.Key].skin < 1 && node.Value.skin > 0)
                        {
                            if (!config.misc_settings.update_skinIDs_from_default) continue;
                            configNodes[node.Key].skin = node.Value.skin;
                            foundNewContent = true;
                        }
                    }
                }
            }
            // Gets and stores all bp defs. Also stores category info.
            foreach (var itemDef in ItemManager.GetItemDefinitions())
            {
                ItemDefs.Add(itemDef.shortname, itemDef);
                if (itemDef.Blueprint != null && itemDef.Blueprint.userCraftable)
                {
                    if (!item_BPs.ContainsKey(itemDef.shortname)) item_BPs.Add(itemDef.shortname, itemDef.Blueprint);
                }

                if (itemDef.category == ItemCategory.Electrical && !config.tools_black_white_list_settings.comp_blacklist.Contains(itemDef.shortname)) electrical_item_list.Add(itemDef);
                else if (itemDef.category == ItemCategory.Component && !config.tools_black_white_list_settings.comp_blacklist.Contains(itemDef.shortname)) component_item_list.Add(itemDef);
            }
            Puts($"Blueprint count: {item_BPs.Count}");

            if (config.ultimate_settings.ultimate_skinning.enabled_buffs.Count == 0)
            {
                config.ultimate_settings.ultimate_skinning.enabled_buffs = DefaultAnimalBuffs;
                foundNewContent = true;
            }
            DeepSeaLooterLootTable = GetUnderwaterLoot();
            SharkLootTable = GetSharkLoot();

            if (config.buff_settings.durability_blacklist.Count == 0)
            {
                config.buff_settings.durability_blacklist = DefaultDurabilityBlacklist;
                foundNewContent = true;
            }

            if (config.loot_settings.mining_loot_table.Count == 0)
            {
                config.loot_settings.mining_loot_table = DefaultLootItems;
                foundNewContent = true;
            }

            if (config.loot_settings.wc_loot_table.Count == 0)
            {
                config.loot_settings.wc_loot_table = DefaultLootItems;
                foundNewContent = true;
            }

            if (config.loot_settings.skinning_loot_table.Count == 0)
            {
                config.loot_settings.skinning_loot_table = DefaultLootItems;
                foundNewContent = true;
            }

            if (config.loot_settings.fishing_loot_table.Count == 0)
            {
                config.loot_settings.fishing_loot_table = DefaultLootItems;
                foundNewContent = true;
            }

            if (config.ultimate_settings.ultimate_mining.trigger_on_item_change && config.ultimate_settings.ultimate_mining.tools_list.Count == 0)
            {
                config.ultimate_settings.ultimate_mining.tools_list = DefaultUltimateToolsList;
                foundNewContent = true;
            }

            if (string.IsNullOrEmpty(config.buff_settings.raid_perk_settings.Lock_Picker_settings.pick_command.Trim()))
            {
                config.buff_settings.raid_perk_settings.Lock_Picker_settings.pick_command = "picklock";
                foundNewContent = true;
            }
            cmd.AddChatCommand(config.buff_settings.raid_perk_settings.Lock_Picker_settings.pick_command.Trim(), this, nameof(SetPicker));

            if (string.IsNullOrEmpty(config.ultimate_settings.ultimate_raiding.command) || string.IsNullOrWhiteSpace(config.ultimate_settings.ultimate_raiding.command))
            {
                config.ultimate_settings.ultimate_raiding.command = "strike";
                foundNewContent = true;
            }

            if (string.IsNullOrEmpty(config.ultimate_settings.ultimate_cooking.command) || string.IsNullOrWhiteSpace(config.ultimate_settings.ultimate_cooking.command))
            {
                config.ultimate_settings.ultimate_cooking.command = "teatime";
                foundNewContent = true;
            }

            if (string.IsNullOrEmpty(config.chat_commands.track_animal_cmd))
            {
                config.chat_commands.track_animal_cmd = "track";
                foundNewContent = true;
            }
            cmd.AddChatCommand(config.chat_commands.track_animal_cmd, this, nameof(TrackAnimal));
            cmd.AddChatCommand(config.buff_settings.forager_settings.command, this, nameof(ForagerChatCMD));
            cmd.AddConsoleCommand(config.buff_settings.forager_settings.command, this, nameof(ForagerConsoleCMD));

            List<YieldTypes> yieldTypes = Pool.GetList<YieldTypes>();
            yieldTypes.AddRange(Enum.GetValues(typeof(YieldTypes)).Cast<YieldTypes>());

            foreach (var yieldType in yieldTypes)
            {
                float value;
                if (!config.base_yield_settings.multipliers.TryGetValue(yieldType, out value))
                {
                    config.base_yield_settings.multipliers.Add(yieldType, value = 1);
                    foundNewContent = true;
                }
                if (value < 1 || value > 1) BaseYieldOverrides.Add(yieldType, value);
            }

            if (config.base_yield_settings.adjust_base_yield && BaseYieldOverrides.Count > 0)
            {
                string message = $"Adjusting Yields for:\n";
                foreach (var type in BaseYieldOverrides)
                {
                    message += $"- {type.Key}: {type.Value}x\n";
                }
                Puts(message);
            }

            if (config.buff_settings.tea_looter_settings.containers.IsNullOrEmpty())
            {
                config.buff_settings.tea_looter_settings.containers = DefaultTeaContainers;
                foundNewContent = true;
            }

            if (config.buff_settings.tea_looter_settings.TeaDropTable.IsNullOrEmpty())
            {
                config.buff_settings.tea_looter_settings.TeaDropTable = DefaultTeaWeights;
                foundNewContent = true;
            }

            if (config.buff_settings.forager_settings.displayColours.IsNullOrEmpty())
            {
                config.buff_settings.forager_settings.displayColours = DefaultForagerColours;
                foundNewContent = true;
            }
            if (config.ultimate_settings.ultimate_cooking.tea_mods.IsNullOrEmpty())
            {
                config.ultimate_settings.ultimate_cooking.tea_mods = DefaultCookingUltimateMods;
                foundNewContent = true;
            }
            if (string.IsNullOrEmpty(config.buff_settings.raid_perk_settings.Trap_Spotter_settings.command))
            {
                config.buff_settings.raid_perk_settings.Trap_Spotter_settings.command = "traps";
                foundNewContent = true;
            }

            if (config.buff_settings.raid_perk_settings.Trap_Spotter_settings.trap_colours.IsNullOrEmpty())
            {
                config.buff_settings.raid_perk_settings.Trap_Spotter_settings.trap_colours = DefaultSpotterCols;
                foundNewContent = true;
            }

            if (foundNewContent) SaveConfig();

            // Chat command stuff
            foreach (var chatcommand in config.chat_commands.score_chat_cmd)
            {
                cmd.AddChatCommand(chatcommand, this, "CheckScoreBoard");
                cmd.AddConsoleCommand(chatcommand, this, "CheckScoreBoardConsole");
            }

            cmd.AddChatCommand(config.ultimate_settings.ultimate_raiding.command, this, nameof(CallRocketStrike));
            cmd.AddChatCommand(config.ultimate_settings.ultimate_cooking.command, this, nameof(AddTeaBuffsCMD));
            cmd.AddChatCommand(config.buff_settings.raid_perk_settings.Trap_Spotter_settings.command, this, nameof(SearchForTraps));
            cmd.AddConsoleCommand(config.buff_settings.raid_perk_settings.Trap_Spotter_settings.command, this, nameof(SearchForTrapsConsoleCMD));

            foreach (var chatcommand in config.chat_commands.chat_cmd)
            {
                cmd.AddChatCommand(chatcommand, this, "SendMenuCMD");
            }
            // Image lib stuff

            foreach (var tree in config.trees)
            {
                foreach (var node in tree.Value.nodes)
                {
                    if (!node.Value.enabled) continue;

                    if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase) || node.Value.skin == 0)
                    {
                        if (string.IsNullOrEmpty(ExtraPocketsImg.Key) && node.Value.buff_info.Key == Buff.ExtraPockets)
                        {
                            ExtraPocketsImg = new KeyValuePair<string, string>("ExtraPocketsButton", node.Value.icon_url);
                            loadOrder.Add("ExtraPocketsButton", node.Value.icon_url);
                        }
                        loadOrder.Add(node.Key, node.Value.icon_url);

                    }
                    else
                    {
                        if (string.IsNullOrEmpty(ExtraPocketsImgSkin.Key) && node.Value.buff_info.Key == Buff.ExtraPockets)
                        {
                            ExtraPocketsImgSkin = new KeyValuePair<string, ulong>("ExtraPocketsButton", node.Value.skin);
                        }
                    }
                    if (!NodeSkinDirectory.ContainsKey(node.Key)) NodeSkinDirectory.Add(node.Key, node.Value.skin);
                    if (!BuffBuffType.ContainsKey(node.Value.buff_info.Key)) BuffBuffType.Add(node.Value.buff_info.Key, node.Value.buff_info.Value);
                }
            }
            // Updates the players level based on the config.
            foreach (var kvp in pcdData.pEntity)
            {
                if (!config.xp_settings.xp_loss_settings.allow_xp_debt) kvp.Value.xp_debt = 0;
                kvp.Value.current_level = config.level.GetLevel(kvp.Value.xp);
                if (kvp.Value.achieved_level == 0 && kvp.Value.current_level != 0) kvp.Value.achieved_level = kvp.Value.current_level;

                if (kvp.Value.pouch_items?.Count > 0)
                {
                    List<ItemInfo> items = Pool.GetList<ItemInfo>();
                    items.AddRange(kvp.Value.pouch_items);
                    foreach (var item in items)
                    {
                        if (item.amount <= 0)
                        {
                            Puts($"Found an item with 0 quantity in {kvp.Value.name ?? kvp.Key.ToString()}'s pouch - Removing.");
                            kvp.Value.pouch_items.Remove(item);
                        }
                    }
                }
            }

            // If players are online when this is run, we set up their data.
            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    HandleNewConnection(player);
                }
            }

            if (!config.buff_settings.boat_turbo_on_mount) cmd.AddChatCommand(config.chat_commands.turbo_cmd, this, nameof(ChangeBoatSpeedCMD));
            if (!config.chat_commands.use_input_key_boat)
            {
                Unsubscribe("OnPlayerInput");
            }
            if (config.xp_settings.xp_sources.Mission == 0) Unsubscribe("OnMissionSucceeded");
            if (config.xp_settings.xp_sources.LootBradleyCrate == 0 && config.xp_settings.xp_sources.LootHackedCrate == 0 && config.xp_settings.xp_sources.LootHeliCrate == 0) Unsubscribe("OnLootEntity");
            if (config.xp_settings.xp_sources.Win_HungerGames == 0) Unsubscribe("HGWinner");
            if (config.xp_settings.xp_sources.Win_ScubaArena == 0) Unsubscribe("SAWinner");
            if (config.xp_settings.xp_sources.Win_Skirmish == 0)
            {
                Unsubscribe("SAWinner");
                Unsubscribe("SAWinners");
            }
            if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase))
            {
                loadOrder.Add("arrow_down_double", "https://www.dropbox.com/s/a1aysr6qmcuinyb/arrow_down_double.png?dl=1");
                loadOrder.Add("arrow_left_double", "https://www.dropbox.com/s/tx5vgr3m9bujvde/arrow_left_double.png?dl=1");
                loadOrder.Add("arrow_right_double", "https://www.dropbox.com/s/6ns3a41qwdn74h8/arrow_right_double.png?dl=1");
                loadOrder.Add("arrow_up_double", "https://www.dropbox.com/s/yqygkxfsyput635/arrow_up_double.png?dl=1");
                loadOrder.Add("arrow_down_single", "https://www.dropbox.com/s/jqi9ulzgj8pq024/arrow_down_single.png?dl=1");
                loadOrder.Add("arrow_left_single", "https://www.dropbox.com/s/ht2pol52oc4q5k9/arrow_left_single.png?dl=1");
                loadOrder.Add("arrow_right_single", "https://www.dropbox.com/s/aixjnroopq9vess/arrow_right_single.png?dl=1");
                loadOrder.Add("arrow_up_single", "https://www.dropbox.com/s/ud9fnx07bv724v2/arrow_up_single.png?dl=1");
            }
            if (loadOrder.Count > 0)
            {
                Puts($"Loading {loadOrder.Count} images into ImageLibrary");
                ImageLibrary?.Call("ImportImageList", this.Name, loadOrder, 0ul, config.general_settings.replace_on_reload, new Action(SkillTreeImagesReady));
            }
            else
            {
                NextTick(() => SkillTreeImagesReady());
            }
            LoadBuffs();

            if (pcdData.pEntity != null)
            {
                foreach (var kvp in pcdData.pEntity)
                {
                    var point_tally = kvp.Value.available_points;
                    if (kvp.Value.buff_values != null)
                    {
                        foreach (var point in kvp.Value.buff_values)
                        {
                            point_tally += point.Value;
                        }
                    }
                    var points_should_have = config.general_settings.points_per_level * kvp.Value.achieved_level;
                    if (point_tally < points_should_have)
                    {
                        kvp.Value.available_points += points_should_have - point_tally;
                        Puts($"{kvp.Key} had less points than they should. Added {points_should_have - point_tally} points to their pool.");
                    }
                }
            }
            UpdateScoreBoard();

            cmd.AddChatCommand(config.ultimate_settings.ultimate_mining.find_node_cmd, this, "TriggerMiningUltimateFromCMD");
            cmd.AddChatCommand(config.ultimate_settings.ultimate_harvesting.gene_chat_command, this, "SetPlantGenes");

            if (!string.IsNullOrEmpty(config.xp_settings.xp_display_col_modified)) ModifiedCol = config.xp_settings.xp_display_col_modified;
            else ModifiedCol = "00b6ff";

            if (!string.IsNullOrEmpty(config.xp_settings.xp_display_col_unmodified)) UnmodifiedCol = config.xp_settings.xp_display_col_unmodified;
            else UnmodifiedCol = "ffffff";

            CheckHarvestingBlacklist = config.buff_settings.harvest_yield_blacklist.Count > 0;

            if (!config.ultimate_settings.ultimate_mining.trigger_on_item_change) Unsubscribe(nameof(OnActiveItemChanged));

            Duds.AddRange(BaseNetworkable.serverEntities.OfType<DudTimedExplosive>());

            SetupRaidUltimateStatics();

            UWB_Anchor_Min = config.buff_settings.underwaterSettings.anchor_min;
            UWB_Anchor_Max = config.buff_settings.underwaterSettings.anchor_max;
            UWB_Offset_Min = config.buff_settings.underwaterSettings.offset_min;
            UWB_Offset_Max = config.buff_settings.underwaterSettings.offset_max;

            if (config.misc_settings.log_player_xp_gain)
            {
                LogTimer = timer.Every(60f, () => AddLogs());
            }

            if (!config.buff_settings.clone_yield) Unsubscribe(nameof(CanTakeCutting));

            if (!config.thirdPartyPluginSettings.survivalArenaSettings.disable_skinning_ultimate_buff_on_join && !config.thirdPartyPluginSettings.paintballSettings.disable_skinning_ultimate_buff_on_join) Unsubscribe(nameof(EMOnEventJoined));

            if (!config.buff_settings.prevent_flyhack_kick_fall_damage) Unsubscribe(nameof(OnPlayerViolation));
        }

        Dictionary<string, string> Logs = new Dictionary<string, string>();
        void AddLogs()
        {
            foreach (var kvp in Logs)
            {
                LogToFile($"XP_Logs_{kvp.Key}", kvp.Value, this, false, true);
            }
            Logs.Clear();
        }

        void AddXPLog(BasePlayer player, string text)
        {
            if (!Logs.ContainsKey(player.UserIDString)) Logs.Add(player.UserIDString, text + "\n");
            else Logs[player.UserIDString] += text + "\n";
        }

        Dictionary<string, ulong> ArrowSkins = new Dictionary<string, ulong>()
        {
            ["arrow_down_double"] = 2873060319,
            ["arrow_left_double"] = 2873060538,
            ["arrow_right_double"] = 2873060617,
            ["arrow_up_double"] = 2873060659,
            ["arrow_down_single"] = 2873060760,
            ["arrow_left_single"] = 2873060807,
            ["arrow_right_single"] = 2873060847,
            ["arrow_up_single"] = 2873060907
        };

        private bool ImagesLoaded;

        private void SkillTreeImagesReady()
        {
            loadOrder.Clear();
            loadOrder = null;
            ImagesLoaded = true;
            Puts($"Loaded all images for SkillTree.");
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (buffDetails.ContainsKey(player.userID) && buffDetails[player.userID].buff_values.ContainsKey(Buff.ExtraPockets) && pcdData.pEntity.ContainsKey(player.userID) && pcdData.pEntity[player.userID].extra_pockets_button) SendExtraPocketsButton(player);
            }
        }

        void OnPlayerConnected(BasePlayer player) => HandleNewConnection(player);

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            DoClear(player);
            LoggingOff(player);
        }

        void DoClear(BasePlayer player)
        {
            UpdatePlayerData(player.userID);
            TreeData.Remove(player.userID);
            //if (buffDetails.ContainsKey(player.userID)) buffDetails[player.userID].buff_values.Clear();
            buffDetails.Remove(player.userID);
            notifiedPlayers.Remove(player.userID);
            player.metabolism.calories.max = 500f;
            player.metabolism.hydration.max = 250f;
            player.SendNetworkUpdate();
            RemoveFromAllBuffs(player.userID);
            DestroyRaidBehaviour(player);
            RemoveAnimalBuff(player);
            DestroyRegen(player);
            DestroyWaterBreathing(player);
            DestroyInstantUntie(player);
            RemovePerms(player.UserIDString);
        }

        // UserIDString, Node name, List of perms.
        void RemovePerms(string id)
        {
            Dictionary<string, Dictionary<string, string>> perms;
            if (Tracked_perms.TryGetValue(id, out perms))
            {
                foreach (var node in perms.Values)
                {
                    foreach (var perm in node.Keys)
                    {
                        permission.RevokeUserPermission(id, perm.Trim());
                    }
                }
                Tracked_perms.Remove(id);
            }
        }

        #endregion

        #region Experience

        [ChatCommand("updatexptable")]
        void UpdateXPTable(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;
            if (config.level == null) config.level = new Configuration.ExperienceInfo();
            config.level.CalculateTable(config.general_settings.max_player_level > 0 ? config.general_settings.max_player_level : 100);
            SaveConfig();
            Puts("Updated xp table.");
        }

        int CheckLevel(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return 0;
            var level = config.level.GetLevel(playerData.xp);
            // If we are max level, we exit the method with the max level.
            if (config.general_settings.max_player_level > 0 && playerData.current_level >= config.general_settings.max_player_level) return config.general_settings.max_player_level;
            //We run the following block of code if our current level is less than the calculated level of our xp.
            if (playerData.current_level < level)
            {
                var max_skill_points = GetMaxSkillPoints(player);
                // If the configured max level is 0 OR our new level is less than/equal to the max level, we run the following code block.
                if (config.general_settings.max_player_level == 0 || level <= config.general_settings.max_player_level)
                {
                    // We check to see if the highest level achieved by the player is less than the new level.
                    Player.Message(player, string.Format(lang.GetMessage("LevelEarn", this, player.UserIDString), playerData.achieved_level < level ? (config.general_settings.points_per_level * (level - playerData.current_level)) : 0, level), config.misc_settings.ChatID);
                    if (config.notification_settings.discordSettings.send_level_up) SendDiscordMsg(string.Format(lang.GetMessage("LevelEarnDiscord", this), player.displayName, player.UserIDString, level));
                    if (playerData.achieved_level < level)
                    {
                        // After confirming the achieved level is < level, we check to see if the player has hit the maximum number of skill points, or if config max skill points is 0, then award them with skill points.
                        if (max_skill_points == 0 || (playerData.current_level * config.general_settings.points_per_level + GetStartingSkillPoints(player.UserIDString) < max_skill_points)) playerData.available_points += config.general_settings.points_per_level * (level - playerData.current_level);
                        // We set this current level as the maximum level achieved. This is to prevent skill points being awarded if they have reached this level before and somehow lost xp/level.
                        playerData.achieved_level = level;
                    }
                    // Increases the current level of the player to the new level.
                    Interface.CallHook("STOnPlayerLevel", player, playerData.current_level, level);
                    GiveLevelRewards(player, level, level - playerData.current_level);
                    playerData.current_level = level;
                    if (config.notification_settings.notifySettings.level_up_notification.Key != null)
                    {
                        var str = string.Format(lang.GetMessage(config.notification_settings.notifySettings.level_up_notification.Key, this, player.UserIDString), level, playerData.available_points);
                        SendNotify(player, str, config.notification_settings.notifySettings.level_up_notification.Value);
                    }
                }
                // Otherwise we assume we are hitting max level.
                else
                {
                    // We check the amount of levels the player gained from max level.
                    var levels_gained = config.general_settings.max_player_level - playerData.current_level;
                    Player.Message(player, string.Format(lang.GetMessage("LevelEarn", this, player.UserIDString), config.general_settings.points_per_level * levels_gained, config.general_settings.max_player_level), config.misc_settings.ChatID);
                    if (config.notification_settings.discordSettings.send_level_up) SendDiscordMsg(string.Format(lang.GetMessage("LevelEarnDiscord", this), player.displayName, player.UserIDString, config.general_settings.max_player_level));
                    // We check to see if the highest level achieved by the player is less than max level.
                    if (playerData.achieved_level < config.general_settings.max_player_level)
                    {
                        // After confirming the achieved level is < max level, we check to see if the player has hit the maximum number of skill points, or if config max skill points is 0, then award them with skill points.
                        if (max_skill_points == 0 || (playerData.current_level * config.general_settings.points_per_level < max_skill_points)) playerData.available_points += config.general_settings.points_per_level * levels_gained;
                        // We set this current level as the maximum level achieved. This is to prevent skill points being awarded if they have reached this level before and somehow lost xp/level.
                        playerData.achieved_level = config.general_settings.max_player_level;
                        GiveLevelRewards(player, config.general_settings.max_player_level, levels_gained);
                        if (config.notification_settings.notifySettings.level_up_notification.Key != null)
                        {
                            var str = string.Format(lang.GetMessage(config.notification_settings.notifySettings.level_up_notification.Key, this, player.UserIDString), config.general_settings.max_player_level, playerData.available_points);
                            SendNotify(player, str, config.notification_settings.notifySettings.level_up_notification.Value);
                        }
                    }
                    //We set the players level to max level.
                    playerData.current_level = config.general_settings.max_player_level;
                }
                // Sends a network effect ot the player only if configured to.
                if (!string.IsNullOrEmpty(config.effect_settings.level_effect)) EffectNetwork.Send(new Effect(config.effect_settings.level_effect, player.transform.position, player.transform.position), player.net.connection);
            }
            // We assume the players current level is higher than it should be and adjust it back to the new level. This could be due to an xp change.
            else if (playerData.current_level != level && (config.general_settings.max_player_level == 0 || config.general_settings.max_player_level <= level)) playerData.current_level = level;
            return level;
        }

        void SendNotify(BasePlayer player, string message, int type)
        {
            if (UINotify == null || !UINotify.IsLoaded || string.IsNullOrEmpty(message)) return;
            UINotify.Call("SendNotify", player.userID, type, message);
        }

        void GiveRewards(BasePlayer player, int level)
        {
            LevelReward _rewards;
            if (config.general_settings.level_rewards.TryGetValue(level, out _rewards))
            {
                foreach (var reward in _rewards.reward_commands)
                {
                    string[] command_string = reward.Key.Split(' ');
                    if (command_string != null && command_string.Length > 0)
                    {
                        if (command_string.Length == 1)
                        {
                            try
                            {
                                var str = reward.Key.Replace("{name}", player.displayName);
                                str = str.Replace("{id}", player.UserIDString);
                                Server.Command(str);
                            }
                            catch
                            {
                                //LogToFile("CommandFailureLog", $"[{DateTime.Now}] Failed to run command for {player.displayName}[{player.userID}] - Command: {reward.Key}", this, true);
                            }
                        }

                        else
                        {
                            string command = command_string[0];
                            List<string> args = Pool.GetList<string>();
                            foreach (var arg in command_string.Skip(1))
                            {
                                if (arg.Contains("{id}")) args.Add(arg.Replace("{id}", player.UserIDString));
                                else if (arg.Contains("{name}")) args.Add(arg.Replace("{name}", player.displayName));
                                else args.Add(arg);
                            }
                            if (command.Equals("say", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    Server.Command(command, string.Join(" ", args));
                                }
                                catch
                                {
                                    //LogToFile("CommandFailureLog", $"[{DateTime.Now}] Failed to run command for {player.displayName}[{player.userID}] - Command: {command} {string.Join(" ", args)}", this, true);
                                }
                            }
                            else
                            {
                                try
                                {
                                    Server.Command(command, args);
                                }
                                catch
                                {
                                    //LogToFile("CommandFailureLog", $"[{DateTime.Now}] Failed to run command for {player.displayName}[{player.userID}] - Command: {command} {string.Join(" ", args)}", this, true);
                                }
                            }

                            if (!string.IsNullOrEmpty(reward.Value)) Player.Message(player, reward.Value, config.misc_settings.ChatID);
                            Pool.FreeList(ref args);
                        }
                    }
                }
            }
        }

        void RunResetCommands(string playerID, int level_achieved)
        {
            foreach (var level in config.general_settings.level_rewards)
            {
                if (level.Key <= level_achieved)
                {
                    foreach (var _command in level.Value.reset_commands)
                    {
                        var command_string = _command.Split(' ');
                        if (command_string != null && command_string.Length > 0)
                        {
                            string command = command_string[0];
                            List<string> args = Pool.GetList<string>();
                            foreach (var arg in command_string.Skip(1))
                            {
                                if (arg.Contains("{id}")) args.Add(arg.Replace("{id}", playerID));
                                else if (arg.Contains("{name}"))
                                {
                                    string name = null;
                                    foreach (var player in BasePlayer.allPlayerList)
                                    {
                                        if (player.UserIDString == playerID)
                                        {
                                            name = player.displayName;
                                            break;
                                        }
                                    }
                                    args.Add(arg.Replace("{name}", name ?? playerID));
                                }
                                else args.Add(arg);
                            }

                            if (args == null || args.Count < 1)
                            {
                                try
                                {
                                    Server.Command(command);
                                }
                                catch
                                {
                                    //LogToFile("CommandFailureLog", $"[{DateTime.Now}] Failed to run command for {player.displayName}[{player.userID}] - Command: {command}", this, true);
                                }
                            }

                            else if (command.Equals("say", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    Server.Command(command, string.Join(" ", args));
                                }
                                catch
                                {
                                    //LogToFile("CommandFailureLog", $"[{DateTime.Now}] Failed to run command for {player.displayName}[{player.userID}] - Command: {command} args: {string.Join(" ", args)}", this, true);
                                }
                            }

                            else
                            {
                                try
                                {
                                    Server.Command(command, args);
                                }
                                catch
                                {
                                    //LogToFile("CommandFailureLog", $"[{DateTime.Now}] Failed to run command for {player.displayName}[{player.userID}] - Command: {command} args: {string.Join(" ", args)}", this, true);
                                }
                            }

                            Pool.FreeList(ref args);
                        }
                    }
                }
            }
        }

        [ConsoleCommand("stgiveitem")]
        void STGiveItem(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            //stgiveitem <id> <shortname> <quantity> <skinID> <displayName>
            if (arg.Args == null || arg.Args.Length < 4)
            {
                arg.ReplyWith(lang.GetMessage("stgiveitemUsage", this, player?.UserIDString ?? null));
                return;
            }
            if (!arg.Args[0].IsSteamId())
            {
                arg.ReplyWith(string.Format(lang.GetMessage("stgiveitemInvalidID", this, player?.UserIDString ?? null), arg.Args[0]));
                return;
            }
            var target = FindPlayerByID(arg.Args[0], player ?? null);
            if (target == null)
            {
                arg.ReplyWith(string.Format(lang.GetMessage("stgiveitemNoPlayerFound", this, player?.UserIDString ?? null), arg.Args[0]));
                return;
            }
            var def = ItemManager.FindItemDefinition(arg.Args[1]);
            if (def == null || string.IsNullOrEmpty(def.shortname))
            {
                arg.ReplyWith(string.Format(lang.GetMessage("stgiveitemInvalidShortname", this, player?.UserIDString ?? null), arg.Args[1]));
                return;
            }

            if (!arg.Args[2].IsNumeric())
            {
                arg.ReplyWith(string.Format(lang.GetMessage("stgiveitemQuantityInvalid", this, player?.UserIDString ?? null), arg.Args[2]));
                return;
            }
            var quantity = Convert.ToInt32(arg.Args[2]);
            quantity = Math.Max(quantity, 1);

            if (!arg.Args[3].IsNumeric())
            {
                arg.ReplyWith(string.Format(lang.GetMessage("stgiveitemSkinInvalid", this, player?.UserIDString ?? null), arg.Args[3]));
                return;
            }

            var skinID = Convert.ToUInt64(arg.Args[3]);

            string displayName = null;
            if (arg.Args.Length > 4) displayName = string.Join(" ", arg.Args.Skip(4));
            var item = ItemManager.CreateByName(arg.Args[1], quantity, skinID);
            if (!string.IsNullOrEmpty(displayName)) item.name = displayName;
            target.GiveItem(item);
            arg.ReplyWith($"Gave {item.amount}x {item.name ?? item.info.displayName.english} to {target.displayName}");
        }

        void GiveLevelRewards(BasePlayer player, int newLevel, int levelsGained = 1)
        {
            if (levelsGained == 0) return;
            else if (levelsGained == 1) GiveRewards(player, newLevel);
            else
            {
                int startLevel = newLevel - levelsGained;
                for (int i = startLevel + 1; i < newLevel + 1; i++)
                {
                    GiveRewards(player, i);
                }
            }
        }

        #endregion

        #region Helpers      

        bool CanCombatUltimateTrigger(BasePlayer player, HitInfo info, Rust.DamageType damageType)
        {
            if (!config.ultimate_settings.ultimate_combat.weapon_blacklist.IsNullOrEmpty() && info.WeaponPrefab != null && config.ultimate_settings.ultimate_combat.weapon_blacklist.Contains(info.WeaponPrefab.ShortPrefabName)) return false;
            if (damageType == Rust.DamageType.Heat && !config.ultimate_settings.ultimate_combat.heal_from_fire_damage) return false;
            return true;
        }

        private static List<T> FindResourceEntitiesOfType<T>(Vector3 pos, float radius, int m = -1) where T : EntityComponent<BaseEntity>
        {
            int hits = Physics.OverlapSphereNonAlloc(pos, radius, Vis.colBuffer, m, QueryTriggerInteraction.Collide);
            List<T> entities = new List<T>();
            for (int i = 0; i < hits; i++)
            {
                var entity = Vis.colBuffer[i]?.ToBaseEntity();
                if (entity is T) entities.Add(entity as T);
                Vis.colBuffer[i] = null;
            }
            return entities;
        }

        private static List<T> FindEntitiesOfType<T>(Vector3 pos, float radius, int m = -1) where T : BaseEntity
        {
            int hits = Physics.OverlapSphereNonAlloc(pos, radius, Vis.colBuffer, m, QueryTriggerInteraction.Collide);
            List<T> entities = Pool.GetList<T>();
            for (int i = 0; i < hits; i++)
            {
                var entity = Vis.colBuffer[i]?.ToBaseEntity();
                if (entity is T) entities.Add(entity as T);
                Vis.colBuffer[i] = null;
            }
            return entities;
        }

        void GiveItem(BasePlayer player, Item item)
        {
            if (player == null)
            {
                item.Remove();
                return;
            }
            if (player.inventory.containerMain.itemList != null)
            {
                var moved = false;
                int amount = item.amount;
                foreach (var _item in player.inventory.containerMain.itemList)
                {
                    if (_item.info.stackable < 2) continue;
                    if (_item.skin == item.skin && _item.info.shortname == item.info.shortname)
                    {
                        if ((item.name != null || _item.name != null) && item.name != _item.name) continue;
                        if (item.MoveToContainer(player.inventory.containerMain, _item.position)) moved = true;
                    }
                    if (moved)
                    {
                        player.Command("note.inv", new object[] { item.info.itemid, amount, item.name != null ? item.name : String.Empty, (int)BaseEntity.GiveItemReason.PickedUp });
                        return;
                    }

                }

                foreach (var _item in player.inventory.containerBelt.itemList)
                {
                    if (_item.info.stackable < 2) continue;
                    if (_item.skin == item.skin && _item.info.shortname == item.info.shortname)
                    {
                        if ((item.name != null || _item.name != null) && item.name != _item.name) continue;
                        if (item.MoveToContainer(player.inventory.containerBelt, _item.position)) moved = true;
                    }
                    if (moved)
                    {
                        player.Command("note.inv", new object[] { item.info.itemid, amount, item.name != null ? item.name : String.Empty, (int)BaseEntity.GiveItemReason.PickedUp });
                        return;
                    }

                }

                player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
            }

        }

        void LoggingOff(BasePlayer player, PlayerInfo pi = null)
        {
            if (pi == null && !pcdData.pEntity.TryGetValue(player.userID, out pi)) return;
            pi.logged_off = DateTime.Now;
        }

        void LoggedOn(BasePlayer player, PlayerInfo pi)
        {
            if (pi == null && !pcdData.pEntity.TryGetValue(player.userID, out pi))
            {
                SetupPlayer(player.userID, player.displayName);
                pi = pcdData.pEntity[player.userID];
            }
            if (pi.logged_off == null)
            {
                pi.logged_off = DateTime.Now;
                return;
            }
            if (config.rested_xp_settings.rested_xp_enabled)
            {
                var offlineHours = Convert.ToInt32((DateTime.Now - pi.logged_off).TotalHours);
                if (offlineHours > 0)
                {
                    pi.xp_bonus_pool += GetModifiedRestedXP(player, offlineHours * config.rested_xp_settings.rested_xp_per_hour);
                    if (config.rested_xp_settings.rested_xp_pool_max > 0 && pi.xp_bonus_pool > config.rested_xp_settings.rested_xp_pool_max) pi.xp_bonus_pool = config.rested_xp_settings.rested_xp_pool_max;
                    Player.Message(player, string.Format(lang.GetMessage("RestedNotification", this, player.UserIDString), config.rested_xp_settings.rested_xp_rate * 100, Math.Round(pi.xp_bonus_pool, config.xp_settings.xp_rounding)), config.misc_settings.ChatID);
                }
            }
            pi.logged_off = DateTime.Now;
        }

        double GetModifiedRestedXP(BasePlayer player, double rested_xp)
        {
            var mod = 0f;
            foreach (var perm in config.rested_xp_settings.rested_xp_modifier_perm_mod)
            {
                if (perm.Value > mod && permission.UserHasPermission(player.UserIDString, perm.Key))
                    mod = perm.Value;
            }

            return rested_xp + (rested_xp * mod);
        }

        [HookMethod("GiveSkillPoints")]
        public void GiveSkillPoints(BasePlayer player, int amount)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - GiveSkillPoints. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            playerData.available_points += amount;
            Player.Message(player, string.Format(lang.GetMessage("ReceivedSP", this, player.UserIDString), amount, playerData.available_points));
        }

        float DefaultBoatSpeed(string shortname)
        {
            if (shortname == "rhib") return 1500f;
            else if (shortname == "tugboat") return 200000f;
            else return 600f;
        }

        BasePlayer FindPlayerByID(string id, BasePlayer searchingPlayer = null)
        {
            if (!id.IsSteamId()) return null;
            var player = BasePlayer.activePlayerList.Where(x => x.UserIDString == id).FirstOrDefault();
            if (player == null)
            {
                if (searchingPlayer != null) PrintToChat(searchingPlayer, $"No player found matching ID: {id}");
                else Puts($"No player found matching ID: {id}");
            }
            return player ?? null;
        }

        private BasePlayer FindPlayerByName(string Playername, BasePlayer SearchingPlayer = null)
        {
            var lowered = Playername.ToLower();
            var targetList = BasePlayer.allPlayerList.Where(x => x.displayName.ToLower().Contains(lowered)).OrderBy(x => x.displayName.Length);
            if (targetList.Count() == 1)
            {
                return targetList.First();
            }
            if (targetList.Count() > 1)
            {
                if (targetList.First().displayName.Equals(Playername, StringComparison.OrdinalIgnoreCase))
                {
                    return targetList.First();
                }
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, string.Format(lang.GetMessage("MorePlayersFound", this, SearchingPlayer.UserIDString), String.Join(",", targetList.Select(x => x.displayName))));
                }
                else Puts(string.Format(lang.GetMessage("MorePlayersFound", this), String.Join(",", targetList.Select(x => x.displayName))));
                return null;
            }
            if (targetList.Count() == 0)
            {
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, string.Format(lang.GetMessage("NoMatch", this, SearchingPlayer.UserIDString), Playername));
                }
                else Puts(string.Format(lang.GetMessage("NoMatch", this), Playername));
                return null;
            }
            return null;
        }

        bool RollSuccessful(float luck)
        {
            var roll = UnityEngine.Random.Range(0f, 100f);
            return (roll >= 100f - (luck * 100));
        }

        double GetXPModifier(string id, PlayerInfo pi, out bool modified, bool noMod = false)
        {
            modified = false;
            if (noMod) return 1;
            bool hasModifier = false;
            double result = 0;
            // Checks each permission that you have created in the config for xp override.
            foreach (var perm in config.xp_settings.xp_perm_modifier)
            {
                // If the permissions value is greater than the value stored in result, result is set to the new value.
                if (permission.UserHasPermission(id, perm.Key.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase) ? perm.Key : "skilltree." + perm.Key) && perm.Value > result)
                {
                    result = perm.Value;
                    hasModifier = true;
                }
            }
            // If we don't have any special perms, we set the mod to default (1.0)
            if (!hasModifier) result = 1;
            if (config.rested_xp_settings.rested_xp_enabled && pi.xp_bonus_pool > 0)
            {
                // If rested XP is enabled, then we add the rested xp value on top of our result.
                result += config.rested_xp_settings.rested_xp_rate;
                modified = true;
            }
            if (TOD_Sky.Instance.IsNight && config.xp_settings.night_settings.night_xp_gain_modifier != 1)
            {
                // If night time xp gains are enabled, we add (or remove) that value onto our result as well.
                result += config.xp_settings.night_settings.night_xp_gain_modifier - 1;
                modified = true;
            }

            return result;
        }

        bool IsGodMode(BasePlayer player)
        {
            return player.IsGod();
        }

        [HookMethod("AwardXP")]
        public void AwardXP(BasePlayer player, double value, string plugin, bool noMod = false)
        {
            var hook = Interface.CallHook("STCanGainXP", player, plugin, value);
            if (hook != null)
            {
                if (hook != null)
                {
                    if (hook is bool) return;
                    else if (hook is double) value = (double)hook;
                    if (value == 0) return;
                }
            }

            AwardXP(player, value, null, noMod, true);
        }

        [HookMethod("AwardXP")]
        public void AwardXP(BasePlayer player, double value, BaseEntity source = null, bool noMod = false, bool hookAlreadyCalled = false, string source_string = null)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId() || value == 0) return;
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.xp") || value == 0) return;
            if (!hookAlreadyCalled)
            {
                var hook = Interface.CallHook("STCanGainXP", player, source ?? null, value);
                if (hook != null)
                {
                    if (hook is bool) return;
                    else if (hook is double)
                    {
                        value = (double)hook;
                    }
                    if (value == 0) return;
                }
            }
            if (!config.xp_settings.allow_godemode_xp && IsGodMode(player)) return;
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - AwardXP. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            bool modified;
            var exp = (value * GetXPModifier(player.UserIDString, playerData, out modified, noMod));

            double excessXP = GetXPAfterDebtCheck(player, playerData, exp);

            if (config.misc_settings.log_player_xp_gain) AddXPLog(player, $"Gained {excessXP} from: {(source != null ? source.ShortPrefabName : !string.IsNullOrEmpty(source_string) ? source_string : "no source provided")}");
            playerData.xp += excessXP;
            if (playerData.xp_bonus_pool > 0) playerData.xp_bonus_pool -= excessXP;
            if (playerData.xp_drops)
            {
                DisplayXPMenu(player, exp, modified);
            }
            CheckLevel(player);
            if (playerData.xp_hud) UpdateXP(player, playerData);
            if (permission.UserHasPermission(player.UserIDString, "skilltree.chat") && !notifiedPlayers.Contains(player.userID))
            {
                if (config.chat_commands.chat_cmd.Count > 1) Player.Message(player, string.Format(lang.GetMessage("AccessReminder", this, player.UserIDString), config.chat_commands.chat_cmd.First()), config.misc_settings.ChatID);
                notifiedPlayers.Add(player.userID);
            }
        }

        double GetXPAfterDebtCheck(BasePlayer player, PlayerInfo playerData, double xp)
        {
            if (playerData.xp_debt <= 0) return xp;
            var currentDebt = playerData.xp_debt;
            if (currentDebt - xp > 0)
            {
                playerData.xp_debt -= xp;
                return 0;
            }

            double excess = xp - currentDebt;
            playerData.xp_debt -= xp - excess;

            return excess;
        }

        void IncreaseCalories(BasePlayer player, float modifier)
        {
            player.metabolism.calories.max = 500 + (500 * modifier);
            player.metabolism.hydration.max = 250 + (250 * modifier);
            player.SendNetworkUpdate();
        }

        void LevelUpNode(BasePlayer player, string tree, string name)
        {
            TreeInfo ti;
            if (!TreeData.TryGetValue(player.userID, out ti))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - LevelUpNode. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            var max_skill_points = GetMaxSkillPoints(player);
            if (max_skill_points > 0 && ti.total_points_spent >= max_skill_points)
            {
                Player.Message(player, lang.GetMessage("MaxSP", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            NodesInfo nsi = ti.trees[tree];
            NodeInfo ni;
            if (ti.trees[tree].nodes.TryGetValue(name, out ni))
            {
                if (ni.level_current >= ni.level_max)
                {
                    Player.Message(player, lang.GetMessage("MaxedNode", this, player.UserIDString), config.misc_settings.ChatID);
                    return;
                }
                if ((ni.tier == 2 && nsi.points_spent < nsi.level_2_point_requirement) || (ni.tier == 3 && nsi.points_spent < nsi.level_3_point_requirement) || (ni.tier == 4 && nsi.points_spent < nsi.level_4_point_requirement))
                {
                    Player.Message(player, string.Format(lang.GetMessage("NoPrevTierIncUltimate", this, player.UserIDString), nsi.level_2_point_requirement, nsi.level_3_point_requirement, nsi.level_4_point_requirement), config.misc_settings.ChatID);
                    return;
                }
                PlayerInfo playerData;
                if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
                {
                    SetupPlayer(player.userID);
                    playerData = pcdData.pEntity[player.userID];
                }

                if (nsi.min_level > 0)
                {
                    var min_Level = GetLevelRequirement(player, tree, nsi.min_level);
                    if (playerData.current_level < min_Level)
                    {
                        Player.Message(player, string.Format(lang.GetMessage("FailMinLevel", this, player.UserIDString), nsi.min_level), config.misc_settings.ChatID);
                        return;
                    }
                }

                if (nsi.min_points > 0)
                {
                    var min_Points = GetPointRequirement(player, tree, nsi.min_points);
                    var totalPointsSpent = 0;
                    foreach (var t in ti.trees)
                        totalPointsSpent += t.Value.points_spent;

                    if (totalPointsSpent < min_Points)
                    {
                        Player.Message(player, string.Format(lang.GetMessage("FailMinPointsSpent", this, player.UserIDString), nsi.min_points), config.misc_settings.ChatID);
                        return;
                    }
                }

                if (playerData.available_points == 0)
                {
                    Player.Message(player, lang.GetMessage("MaxedSkillPoints", this, player.UserIDString), config.misc_settings.ChatID);
                    return;
                }
                if (max_skill_points > 0 && nsi.points_spent >= max_skill_points)
                {
                    Player.Message(player, lang.GetMessage("AssignedMaxedSkillPoints", this, player.UserIDString), config.misc_settings.ChatID);
                    return;
                }
                if (ni.level_current == 0)
                {
                    Player.Message(player, string.Format(lang.GetMessage("UnlockedFirstNode", this, player.UserIDString), lang.GetMessage(name, this, player.UserIDString), ni.level_max), config.misc_settings.ChatID);
                    if (!string.IsNullOrEmpty(config.effect_settings.skill_point_unlock_effect)) EffectNetwork.Send(new Effect(config.effect_settings.skill_point_unlock_effect, player.transform.position, player.transform.position), player.net.connection);
                    TriggerStuffOnAbilityUnlock(player, ni.buffInfo.Key, ni.value_per_buff, playerData);
                }
                else
                {
                    Player.Message(player, string.Format(lang.GetMessage("UnlockedNode", this, player.UserIDString), lang.GetMessage(name, this, player.UserIDString), ni.level_current + 1, ni.level_max), config.misc_settings.ChatID);
                    if (!string.IsNullOrEmpty(config.effect_settings.skill_point_level_effect)) EffectNetwork.Send(new Effect(config.effect_settings.skill_point_level_effect, player.transform.position, player.transform.position), player.net.connection);
                }

                ni.level_current++;
                playerData.available_points--;
                nsi.points_spent++;
                ti.total_points_spent++;
                if (!playerData.buff_values.ContainsKey(name)) playerData.buff_values.Add(name, ni.level_current);
                else playerData.buff_values[name] = ni.level_current;
                BuffDetails bd;
                if (!buffDetails.TryGetValue(player.userID, out bd)) buffDetails.Add(player.userID, bd = new BuffDetails());
                if (!bd.buff_values.ContainsKey(ni.buffInfo.Key)) bd.buff_values.Add(ni.buffInfo.Key, ni.level_current * ni.value_per_buff);
                else bd.buff_values[ni.buffInfo.Key] += ni.value_per_buff;
                if (ni.buffInfo.Key == Buff.Metabolism_Boost) IncreaseCalories(player, bd.buff_values[ni.buffInfo.Key]);
                else if (ni.buffInfo.Key == Buff.HealthRegen) UpdateRegen(player, bd.buff_values[ni.buffInfo.Key]);
                else if (ni.buffInfo.Key == Buff.WaterBreathing) UpdateWaterBreathing(player, bd.buff_values[ni.buffInfo.Key]);
                else if (ni.buffInfo.Key == Buff.InstantUntie) UpdateInstantUntie(player);
                AddBuffs(player.userID, ni.buffInfo.Key);

                switch (ni.buffInfo.Key)
                {
                    case Buff.Build_Craft_Ultimate:
                    case Buff.Combat_Ultimate:
                    case Buff.Harvester_Ultimate:
                    case Buff.Medical_Ultimate:
                    case Buff.Mining_Ultimate:
                    case Buff.Scavengers_Ultimate:
                    case Buff.Skinning_Ultimate:
                    case Buff.Vehicle_Ultimate:
                    case Buff.Woodcutting_Ultimate:
                    case Buff.Raiding_Ultimate:
                    case Buff.Cooking_Ultimate:
                        HandleUltimateToggle(player, ni.buffInfo.Key, playerData);
                        break;
                    case Buff.ExtraPockets:
                        SendExtraPocketsButton(player);
                        break;
                }

                HandlePerms(player, tree, name, ni.level_current);
            }
        }

        int GetLevelRequirement(BasePlayer player, string tree, int defaultvalue)
        {
            var lowest = defaultvalue;
            foreach (var perm in config.general_settings.level_requirement_override)
            {
                if (!permission.UserHasPermission(player.UserIDString, perm.Key)) continue;
                int value;
                if (!perm.Value.treeRequirementOverride.TryGetValue(tree, out value)) continue;
                if (value < lowest) lowest = value;
            }
            return lowest;
        }

        int GetPointRequirement(BasePlayer player, string tree, int defaultvalue)
        {
            var lowest = defaultvalue;
            foreach (var perm in config.general_settings.point_requirement_override)
            {
                if (!permission.UserHasPermission(player.UserIDString, perm.Key)) continue;
                int value;
                if (!perm.Value.treeRequirementOverride.TryGetValue(tree, out value)) continue;
                if (value < lowest) lowest = value;
            }
            return lowest;
        }

        void TriggerStuffOnAbilityUnlock(BasePlayer player, Buff buff, float value, PlayerInfo playerData)
        {
            switch (buff)
            {
                case Buff.ExtraPockets:
                    if (playerData.extra_pockets_button) SendExtraPocketsButton(player);
                    return;

                case Buff.Extended_Mag:
                    HandleWeaponMagExtension(player, value);
                    return;
            }

        }

        void HandleWeaponMagExtension(BasePlayer player, float ammoMod)
        {
            var weapon = player.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                DelayedModsChanged(weapon, Convert.ToInt32(weapon.primaryMagazine.definition.builtInSize * ammoMod));
                if (!ModifiedWeapons.ContainsKey(weapon.net.ID.Value)) ModifiedWeapons.Add(weapon.net.ID.Value, weapon);
            }
        }

        void HandlePerms(BasePlayer player, string tree, string node, int level) => HandlePerms(player.UserIDString, tree, node, level);

        // UserIDString, Node name, List of perms.
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> Tracked_perms = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

        void HandlePerms(string id, string tree, string node, int level)
        {
            Configuration.TreeInfo ti;
            Configuration.TreeInfo.NodeInfo ni;
            if (config.trees.TryGetValue(tree, out ti) && ti.nodes.TryGetValue(node, out ni) && ni.permissions != null)
            {
                if (ni.permissions.perms.Count == 0) return;
                if (level > 0 && !ni.permissions.perms.ContainsKey(level)) return;
                if (!Tracked_perms.ContainsKey(id)) Tracked_perms.Add(id, new Dictionary<string, Dictionary<string, string>>());
                Dictionary<string, string> tracked_perms;
                if (!Tracked_perms[id].TryGetValue(node, out tracked_perms)) Tracked_perms[id].Add(node, tracked_perms = new Dictionary<string, string>());

                foreach (var perm in ni.permissions.perms)
                {
                    foreach (var str in perm.Value.perms_list)
                    {
                        //Puts($"Revoked permission {str} from {id}.");
                        permission.RevokeUserPermission(id, str.Key.Trim());
                        tracked_perms.Remove(str.Key);
                    }
                }

                if (level > 0)
                {
                    foreach (var perm in ni.permissions.perms[level].perms_list)
                    {
                        //Puts($"Granted permission {perm} to {id}.");
                        permission.GrantUserPermission(id, perm.Key.Trim(), null);
                        tracked_perms.Add(perm.Key, perm.Value);
                    }
                }
            }
        }

        Buff GetBuffType(string name)
        {
            if (BuffTypes.ContainsKey(name)) return BuffTypes[name];
            else return 0;
        }

        float GetBuffModifier(ulong id, Buff buff)
        {
            if (buff == Buff.None) return 0;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(id, out bd) || !bd.buff_values.ContainsKey(buff)) return 0;
            return bd.buff_values[buff];
        }

        float GetBuffModifier(ulong id, string name)
        {
            var buff = GetBuffType(name);
            if (buff == Buff.None) return 0;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(id, out bd) || !bd.buff_values.ContainsKey(buff)) return 0;
            return bd.buff_values[buff];
        }

        int GetMaxSkillPoints(BasePlayer player)
        {
            var highest = config.general_settings.max_skill_points;
            if (highest == 0) return 0;
            foreach (var perm in config.general_settings.max_skill_points_override)
            {
                if (!permission.UserHasPermission(player.UserIDString, "skilltree." + perm.Key)) continue;
                if (perm.Value == 0) return 0;
                if (perm.Value > highest) highest = perm.Value;
            }
            return highest;
        }

        double GetRespecCost(BasePlayer player)
        {
            var lowest = config.general_settings.respec_cost;
            if (lowest == 0) return 0;
            foreach (var perm in config.general_settings.respec_cost_override)
            {
                if (!permission.UserHasPermission(player.UserIDString, "skilltree." + perm.Key)) continue;
                if (perm.Value == 0) return 0;
                if (perm.Value < lowest) lowest = perm.Value;
            }
            if (config.general_settings.respec_multiplier > 0)
            {
                PlayerInfo pi;
                if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) return lowest;
                lowest += Convert.ToDouble(lowest * pi.respec_multiplier);
            }
            return lowest;
        }

        double GetXPLoss(BasePlayer player, double xpToCalculateFrom, DeathType type)
        {
            if (permission.UserHasPermission(player.UserIDString, "skilltree.noxploss")) return 0;
            if (!config.xp_settings.xp_loss_settings.allow_xp_debt) return 0;

            double baseXPLossModifier = 0;
            switch (type)
            {
                case DeathType.PVE:
                    baseXPLossModifier = config.xp_settings.xp_loss_settings.pve_death_penalty;
                    break;

                case DeathType.PVP:
                    baseXPLossModifier = config.xp_settings.xp_loss_settings.pvp_death_penalty;
                    break;

                case DeathType.Suicide:
                    baseXPLossModifier = config.xp_settings.xp_loss_settings.suicide_death_penalty;
                    break;
            }

            if (baseXPLossModifier == 0) return 0;

            double modifier = 1;
            foreach (var perm in config.xp_settings.xp_loss_settings.xp_loss_override)
            {
                if (perm.Value < modifier && HasPermission(player.UserIDString, perm.Key))
                    modifier = perm.Value;
            }

            return Math.Max(xpToCalculateFrom * (baseXPLossModifier * modifier / 100), 0);
        }

        bool HasPermission(string id, string perm)
        {
            if (!perm.StartsWith("skilltree.")) return permission.UserHasPermission(id, "skilltree." + perm);
            return permission.UserHasPermission(id, perm);
        }

        void UpdateInstancedData(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return;

            TreeInfo ti;
            if (!TreeData.TryGetValue(player.userID, out ti) && !player.IsNpc && player.userID.IsSteamId()) TreeData.Add(player.userID, ti = new TreeInfo());
            if (ti == null) return;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd)) buffDetails.Add(player.userID, bd = new BuffDetails());
            foreach (var cfg in config.trees)
            {
                if (!cfg.Value.enabled) continue;

                if (!HasTreeAccess(player.UserIDString, cfg.Key))
                {
                    RefundTreePoints(player, cfg.Key);
                    continue;
                }
                NodesInfo ni;
                if (!ti.trees.TryGetValue(cfg.Key, out ni)) ti.trees.Add(cfg.Key, ni = new NodesInfo());
                ni.min_level = cfg.Value.min_level;
                ni.min_points = cfg.Value.min_points;
                ni.level_2_point_requirement = cfg.Value.level_2_point_requirement;
                ni.level_3_point_requirement = cfg.Value.level_3_point_requirement;
                ni.level_4_point_requirement = cfg.Value.level_4_point_requirement;
                foreach (var node in cfg.Value.nodes)
                {
                    if (!node.Value.enabled || (!string.IsNullOrEmpty(node.Value.required_permission) && !permission.UserHasPermission(player.UserIDString, node.Value.required_permission)))
                    {
                        int spentPoints = 0;
                        if (playerData.buff_values.TryGetValue(node.Key, out spentPoints))
                        {
                            playerData.available_points += spentPoints;
                            playerData.buff_values.Remove(node.Key);
                        }
                        continue;
                    }
                    NodeInfo nodeData;
                    if (!ni.nodes.TryGetValue(node.Key, out nodeData)) ni.nodes.Add(node.Key, nodeData = new NodeInfo());
                    nodeData.buffInfo = node.Value.buff_info;
                    nodeData.level_max = node.Value.max_level;
                    nodeData.tier = node.Value.tier;
                    nodeData.value_per_buff = node.Value.value_per_buff;
                    if (nodeData.tier == 4)
                    {
                        if (nodeData.buffInfo.Key == Buff.Build_Craft_Ultimate)
                        {
                            string ult_str = config.ultimate_settings.ultimate_buildCraft.success_chance < 100 ? string.Format(lang.GetMessage("Build_Craft_Ultimate_DescriptionAddition", this, player.UserIDString), config.ultimate_settings.ultimate_buildCraft.success_chance) : "";
                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), ult_str);
                        }
                        else if (nodeData.buffInfo.Key == Buff.Harvester_Ultimate)
                        {
                            string ult_str = config.ultimate_settings.ultimate_harvesting.cooldown > 0 ? string.Format(lang.GetMessage("Harvesting_Ultimate_DescriptionAddition", this, player.UserIDString), config.ultimate_settings.ultimate_harvesting.cooldown) : "";
                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), ult_str);
                        }
                        else if (nodeData.buffInfo.Key == Buff.Mining_Ultimate)
                        {
                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), Math.Round(config.ultimate_settings.ultimate_mining.distance_from_player, 0));
                        }
                        else if (nodeData.buffInfo.Key == Buff.Woodcutting_Ultimate)
                        {
                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), Math.Round(config.ultimate_settings.ultimate_woodcutting.distance_from_player, 0));
                        }
                        else if (nodeData.buffInfo.Key == Buff.Skinning_Ultimate)
                        {
                            string ult_str = config.ultimate_settings.ultimate_skinning.enabled_buffs.FirstOrDefault(x => x.Value > 0).Value > 0 ? string.Format(string.Join("</color>, <color=#42f105>", config.ultimate_settings.ultimate_skinning.enabled_buffs.Where(x => x.Value > 0).Select(x => lang.GetMessage(x.Key.ToString().ToLower(), this, player.UserIDString)))) : "";
                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), ult_str);
                        }
                        else if (nodeData.buffInfo.Key == Buff.Medical_Ultimate)
                        {
                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), Math.Round(config.ultimate_settings.ultimate_medical.resurrection_chance, 0));
                        }
                        else if (nodeData.buffInfo.Key == Buff.Combat_Ultimate)
                        {
                            string formattedString = "";
                            var active = 0;
                            if (config.ultimate_settings.ultimate_combat.scientists_enabled) active++;
                            if (config.ultimate_settings.ultimate_combat.players_enabled) active++;
                            if (config.ultimate_settings.ultimate_combat.scientists_enabled) active++;

                            if (config.ultimate_settings.ultimate_combat.scientists_enabled)
                            {
                                formattedString += lang.GetMessage("CombatUltimateScientists", this, player.UserIDString);
                                if (active == 2) formattedString += " and ";
                                if (active == 3) formattedString += ", ";
                            }
                            if (config.ultimate_settings.ultimate_combat.animals_enabled)
                            {
                                formattedString += lang.GetMessage("CombatUltimateAnimals", this, player.UserIDString);
                                if (active == 3) formattedString += " and ";
                            }
                            if (config.ultimate_settings.ultimate_combat.players_enabled) formattedString += lang.GetMessage("CombatUltimatePlayers", this, player.UserIDString);

                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), config.ultimate_settings.ultimate_combat.health_scale * 100, formattedString);
                        }
                        else if (nodeData.buffInfo.Key == Buff.Scavengers_Ultimate)
                        {
                            nodeData.description = lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString);
                        }
                        else if (nodeData.buffInfo.Key == Buff.Raiding_Ultimate)
                        {
                            nodeData.description = lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString);
                        }
                        else if (nodeData.buffInfo.Key == Buff.Vehicle_Ultimate)
                        {
                            nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), config.ultimate_settings.ultimate_vehicle.reduce_by * 100);
                        }
                        else if (nodeData.buffInfo.Key == Buff.Cooking_Ultimate)
                        {
                            string message = lang.GetMessage("Cooking_Ultimate_Description", this, player.UserIDString) + lang.GetMessage("CookingUltimateDescriptionSize", this, player.UserIDString);
                            foreach (var buff in config.ultimate_settings.ultimate_cooking.tea_mods)
                            {
                                message += string.Format(lang.GetMessage("CookingUltimateMod", this, player.UserIDString), lang.GetMessage(buff.Key.ToString(), this, player.UserIDString), buff.Value.modifier * 100);
                            }
                            message += String.Format(lang.GetMessage("CookingUltimateDescriptionBottom", this, player.UserIDString), config.ultimate_settings.ultimate_cooking.command, config.ultimate_settings.ultimate_cooking.buff_cooldown);
                            nodeData.description = message;
                        }
                        else
                        {
                            if (nodeData.buffInfo.Value == BuffType.Percentage) nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), nodeData.value_per_buff / 1 * 100);
                            else if (nodeData.buffInfo.Value == BuffType.IO) nodeData.description = lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString);
                            else nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), nodeData.value_per_buff);
                        }
                    }
                    else if (nodeData.buffInfo.Key == Buff.Boat_Speed && config.buff_settings.boat_turbo_on_mount)
                    {
                        nodeData.description = string.Format(lang.GetMessage("BoatSpeedAuto", this, player.UserIDString), nodeData.value_per_buff * 100);
                    }
                    else if (nodeData.buffInfo.Key == Buff.Forager)
                    {
                        nodeData.description = string.Format(lang.GetMessage("Forager", this, player.UserIDString), config.buff_settings.forager_settings.distance);
                    }
                    else if (nodeData.buffInfo.Key == Buff.Woodcutting_Hotspot)
                    {
                        if (nodeData.buffInfo.Value == BuffType.Percentage) nodeData.description = string.Format(lang.GetMessage("Woodcutting_Hotspot_Percentage", this, player.UserIDString), nodeData.value_per_buff * 100);
                        else nodeData.description = lang.GetMessage("Woodcutting_Hotspot_IO", this, player.UserIDString);
                    }
                    else if (nodeData.buffInfo.Key == Buff.Mining_Hotspot)
                    {
                        if (nodeData.buffInfo.Value == BuffType.Percentage) nodeData.description = string.Format(lang.GetMessage("Mining_Hotspot_Percentage", this, player.UserIDString), nodeData.value_per_buff * 100);
                        else nodeData.description = lang.GetMessage("Mining_Hotspot_IO", this, player.UserIDString);
                    }
                    else if (nodeData.buffInfo.Value == BuffType.Percentage) nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), nodeData.value_per_buff / 1 * 100);
                    else if (nodeData.buffInfo.Value == BuffType.IO) nodeData.description = lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString);
                    else nodeData.description = string.Format(lang.GetMessage(nodeData.buffInfo.Key.ToString(), this, player.UserIDString), nodeData.value_per_buff);
                    if (!playerData.buff_values.ContainsKey(node.Key)) nodeData.level_current = 0;
                    else nodeData.level_current = playerData.buff_values[node.Key];
                    ni.points_spent += nodeData.level_current;
                    ti.total_points_spent += nodeData.level_current;

                    if (nodeData.level_current > 0)
                    {
                        if (!bd.buff_values.ContainsKey(nodeData.buffInfo.Key)) bd.buff_values.Add(nodeData.buffInfo.Key, nodeData.level_current * nodeData.value_per_buff);
                        else bd.buff_values[nodeData.buffInfo.Key] += nodeData.level_current * nodeData.value_per_buff;
                        AddBuffs(player.userID, nodeData.buffInfo.Key);
                        switch (nodeData.buffInfo.Key)
                        {
                            case Buff.Build_Craft_Ultimate:
                            case Buff.Combat_Ultimate:
                            case Buff.Harvester_Ultimate:
                            case Buff.Medical_Ultimate:
                            case Buff.Mining_Ultimate:
                            case Buff.Scavengers_Ultimate:
                            case Buff.Skinning_Ultimate:
                            case Buff.Vehicle_Ultimate:
                            case Buff.Raiding_Ultimate:
                            case Buff.Woodcutting_Ultimate:
                                HandleUltimateToggle(player, nodeData.buffInfo.Key, playerData);
                                break;
                            case Buff.ExtraPockets:
                                if (playerData.extra_pockets_button) SendExtraPocketsButton(player);
                                break;
                        }
                        HandlePerms(player, cfg.Key, node.Key, nodeData.level_current);
                    }


                    if (!BuffTypes.ContainsKey(node.Key)) BuffTypes.Add(node.Key, nodeData.buffInfo.Key);
                }
                float value;
                if (bd.buff_values.TryGetValue(Buff.Metabolism_Boost, out value)) IncreaseCalories(player, value);
                if (bd.buff_values.TryGetValue(Buff.HealthRegen, out value)) UpdateRegen(player, value);
                if (bd.buff_values.TryGetValue(Buff.WaterBreathing, out value)) UpdateWaterBreathing(player, value);
                if (bd.buff_values.ContainsKey(Buff.InstantUntie)) UpdateInstantUntie(player);
                if (bd.buff_values.TryGetValue(Buff.Extended_Mag, out value)) HandleWeaponMagExtension(player, value);
            }
        }

        bool HasTreeAccess(string id, string tree)
        {
            if (!config.general_settings.require_tree_perms || permission.UserHasPermission(id, "skilltree." + tree) || permission.UserHasPermission(id, "skilltree.all")) return true;
            return false;
        }

        void RefundTreePoints(BasePlayer player, string tree)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return;
            List<string> delete = Pool.GetList<string>();
            var refund = 0;
            foreach (var node in config.trees[tree].nodes)
            {
                int spent;
                if (playerData.buff_values.TryGetValue(node.Key, out spent))
                {
                    refund += spent;
                    delete.Add(node.Key);
                }
            }
            playerData.available_points += refund;
            if (delete.Count > 0) Puts($"Found {delete.Count} nodes that {player.displayName} does not have permission for - deleting them.");
            foreach (var node in delete)
            {
                playerData.buff_values.Remove(node);
            }

            Pool.FreeList(ref delete);
        }

        int GetStartingSkillPoints(string userID)
        {
            int result = config.wipe_update_settings.starting_skill_points;
            foreach (var kvp in config.wipe_update_settings.starting_skill_point_overrides)
            {
                var permString = kvp.Key.StartsWith("skilltree.", StringComparison.OrdinalIgnoreCase) ? kvp.Key : "skilltree." + kvp.Key;
                if (permission.UserHasPermission(userID, permString) && kvp.Value > result) result = kvp.Value;
            }
            return result;
        }

        bool bonus_given;
        void SetupPlayer(ulong id, string name = null)
        {
            if (!id.IsSteamId()) return;
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(id, out playerData)) pcdData.pEntity.Add(id, playerData = new PlayerInfo() { xp_hud_pos = config.general_settings.pump_bar_settings.offset_default, xp_drops = config.xp_settings.enable_xp_drop_by_default, logged_off = DateTime.Now, available_points = GetStartingSkillPoints(id.ToString()) });
            if (!bonus_given && id == pcdData.highest_player)
            {
                playerData.available_points += config.wipe_update_settings.bonus_skill_points_amount;
                pcdData.highest_player = 0;
            }
            if (name != null) playerData.name = name;
        }

        // Only called when a player's data is being removed.
        void UpdatePlayerData(ulong id)
        {
            SetupPlayer(id);
            var playerData = pcdData.pEntity[id];
            TreeInfo pi;
            if (TreeData.TryGetValue(id, out pi))
            {
                foreach (var tree in pi.trees)
                {
                    foreach (var node in tree.Value.nodes)
                    {
                        HandlePerms(id.ToString(), tree.Key, node.Key, 0);
                        if (!playerData.buff_values.ContainsKey(node.Key)) continue;
                        else if (node.Value.level_current > 0) playerData.buff_values[node.Key] = node.Value.level_current;
                    }
                }
            }
        }

        void RespecPlayer(BasePlayer player)
        {
            var heldItem = player.GetHeldEntity();
            if (heldItem != null)
            {
                var rod = heldItem as BaseFishingRod;
                if (rod != null && TrackedRods.Contains(rod)) ResetRod(rod);
            }

            RemoveAnimalBuff(player);
            player.metabolism.calories.max = 500f;
            player.metabolism.hydration.max = 250f;
            player.SendNetworkUpdate();
            DestroyRegen(player);
            DestroyRaidBehaviour(player);
            DestroyWaterBreathing(player);
            DestroyInstantUntie(player);
            TreeInfo ti;
            if (!TreeData.TryGetValue(player.userID, out ti)) return;
            //TreeData, BuffDetails, PlayerData
            ti.total_points_spent = 0;
            foreach (var tree in ti.trees)
            {
                tree.Value.points_spent = 0;
                foreach (var node in tree.Value.nodes)
                {
                    HandlePerms(player, tree.Key, node.Key, 0);
                    node.Value.level_current = 0;
                }
            }
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd))
            {
                bd.buff_values.Clear();
            }
            PlayerInfo playerData;
            if (pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                if (config.ultimate_settings.ultimate_raiding.reset_strike_cooldown_on_respec) playerData.raiding_ultimate_used_time = DateTime.MinValue;
                var pointsBack = 0;
                if (playerData.buff_values.Count > 0)
                {
                    foreach (var buff in playerData.buff_values)
                        pointsBack += buff.Value;
                    playerData.available_points += pointsBack;
                }
                if (playerData.available_points < playerData.current_level * config.general_settings.points_per_level) playerData.available_points = playerData.current_level * config.general_settings.points_per_level + GetStartingSkillPoints(player.UserIDString);
                playerData.buff_values.Clear();
                if (playerData.pouch_items != null & playerData.pouch_items.Count > 0)
                {
                    var bag = GenerateBag(player, playerData.pouch_items.Count + 1);
                    if (bag.inventory?.itemList != null)
                    {
                        List<Item> giveItems = Pool.GetList<Item>();
                        giveItems.AddRange(bag.inventory.itemList);

                        foreach (var item in giveItems)
                        {
                            GiveItem(player, item);
                            //player.GiveItem(item);
                        }
                        playerData.pouch_items.Clear();
                        Pool.FreeList(ref giveItems);
                        Player.Message(player, lang.GetMessage("PouchItemsRemoved", this, player.UserIDString), config.misc_settings.ChatID);
                    }
                }
                playerData.ultimate_settings.Clear();
                CuiHelper.DestroyUi(player, "ExtraPocketsButton");
                RemoveFromAllBuffs(player.userID);
            }
        }

        #endregion

        #region Menu

        private void SkillTreeBackPanel(BasePlayer player)
        {
            if (!ImagesLoaded) Player.Message(player, "Still caching images...", config.misc_settings.ChatID);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.98" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.287 0.343", OffsetMax = "0.312 -0.337" }
            }, "Overlay", "SkillTreeBackPanel");

            CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
            CuiHelper.AddUi(player, container);
        }

        Dictionary<string, ulong> NodeSkinDirectory = new Dictionary<string, ulong>();

        void SendSkillTreeMenu(BasePlayer player, string tree = null, string selected_node = null)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.tree"))
            {
                Player.Message(player, lang.GetMessage("NoPermsTree", this, player.UserIDString), config.misc_settings.ChatID);
                CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
                CuiHelper.DestroyUi(player, "NavigationMenu");
                return;
            }
            TreeInfo ti;
            if (!TreeData.TryGetValue(player.userID, out ti))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - SendSkillTreeMenu. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (ti.trees == null || ti.trees.Count == 0)
            {
                CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
                CuiHelper.DestroyUi(player, "NavigationMenu");
                Player.Message(player, "You do not have any tree permissions. Please apply permission skilltree.<category> if you want to allocate individual trees, or skilltree.all if you want players to access all categories.", config.misc_settings.ChatID);
                return;
            }
            if (tree == null) tree = ti.trees.First().Key;
            var ni = ti.trees[tree];
            NodeInfo foundNode;

            if (selected_node != null)
            {
                foundNode = ni.nodes.Where(x => x.Key == selected_node).Select(x => x.Value).First();
            }
            else foundNode = null;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-0.351 -0.332", OffsetMax = "0.349 0.338" }
            }, "Overlay", "SkillTree");
            container.Add(new CuiElement
            {
                Name = "SkillTree_Title",
                Parent = "SkillTree",
                Components = {
                    new CuiTextComponent { Text = $"{lang.GetMessage(tree, this, player.UserIDString)} {lang.GetMessage("UISkillTree", this, player.UserIDString)}", Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-180 190.4", OffsetMax = "180 250.4" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "SkillTree_Points",
                Parent = "SkillTree",
                Components = {
                    new CuiTextComponent { Text = $"{lang.GetMessage("UITreePointsSpent", this, player.UserIDString)} <color=#42f105>{ti.trees[tree].points_spent}</color>", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-137 139.8", OffsetMax = "4.788 159.8" }
                }
            });
            var totalPoints = 0;
            foreach (var t in ti.trees)
            {
                totalPoints += t.Value.points_spent;
            }
            var ttps = $"{lang.GetMessage("UITotalPointsSpent", this, player.UserIDString)} <color=#42f105>{totalPoints}</color>";
            var max_skill_points = GetMaxSkillPoints(player);
            if (max_skill_points > 0) ttps = $"{lang.GetMessage("UITotalPointsSpent", this, player.UserIDString)} <color=#42f105>{totalPoints}/{max_skill_points}</color>";
            container.Add(new CuiElement
            {
                Name = "SkillTree_Points_global",
                Parent = "SkillTree",
                Components = {
                    new CuiTextComponent { Text = ttps, Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "4.788 139.8", OffsetMax = "146.578 159.8" }
                }
            });
            Dictionary<string, NodeInfo> T1_Nodes = new Dictionary<string, NodeInfo>();
            Dictionary<string, NodeInfo> T2_Nodes = new Dictionary<string, NodeInfo>();
            Dictionary<string, NodeInfo> T3_Nodes = new Dictionary<string, NodeInfo>();
            KeyValuePair<string, NodeInfo> Ultimate_node = new KeyValuePair<string, NodeInfo>();
            foreach (var n in ni.nodes)
            {
                if (n.Value.tier == 1) T1_Nodes.Add(n.Key, n.Value);
                else if (n.Value.tier == 2) T2_Nodes.Add(n.Key, n.Value);
                else if (n.Value.tier == 3) T3_Nodes.Add(n.Key, n.Value);
                else if (n.Value.tier == 4) Ultimate_node = new KeyValuePair<string, NodeInfo>(n.Key, n.Value);
            }
            var furthest_x = 0;
            List<int> largest = Pool.GetList<int>();
            largest.Add(T1_Nodes.Count);
            largest.Add(T2_Nodes.Count);
            largest.Add(T3_Nodes.Count);
            var biggest = largest.Max();

            Pool.FreeList(ref largest);
            var space = biggest <= 4 ? 50 : biggest == 5 ? 30 : biggest == 6 ? 15 : 4;
            ulong skinID;
            for (int i = 0; i < T1_Nodes.Count; i++)
            {
                var node = T1_Nodes.ElementAt(i);
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1698113 0.1698113 0.1698113 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-137 + (i * (space + 58))} {59}", OffsetMax = $"{-79 + (i * (space + 58))} {117}" }
                }, "SkillTree", "SkillTree_panel_1");

                if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase) || (NodeSkinDirectory.TryGetValue(node.Key, out skinID) && skinID == 0))
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_1",
                        Parent = "SkillTree_panel_1",
                        Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", node.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_1",
                        Parent = "SkillTree_panel_1",
                        Components = {
                        new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = skinID },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });
                }


                container.Add(new CuiElement
                {
                    Name = "SkillTree_points_assigned_1",
                    Parent = "SkillTree_panel_1",
                    Components = {
                    new CuiTextComponent { Text = node.Value.level_current.ToString(), Font = "robotocondensed-regular.ttf", FontSize = node.Value.level_current < 100 ? 20 : 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "0 0" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"stsendsubmenu {tree} {node.Key}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                }, "SkillTree_panel_1", "SkillTree_button_1");

                if (-79 + (i * (space + 58)) > furthest_x) furthest_x = -79 + (i * (space + 58));
            }

            for (int i = 0; i < T2_Nodes.Count; i++)
            {
                var node = T2_Nodes.ElementAt(i);
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1698113 0.1698113 0.1698113 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-137 + (i * (space + 58))} {-29}", OffsetMax = $"{-79 + (i * (space + 58))} {29}" }
                }, "SkillTree", "SkillTree_panel_5");

                if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase) || (NodeSkinDirectory.TryGetValue(node.Key, out skinID) && skinID == 0))
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_5",
                        Parent = "SkillTree_panel_5",
                        Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", node.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_5",
                        Parent = "SkillTree_panel_5",
                        Components = {
                        new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = skinID },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });
                }



                if ((ni.points_spent < ni.level_2_point_requirement))
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.3584906 0.3584906 0.3584906 0.8823529" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }, "SkillTree_panel_5", "SkillTree_Grey_Box_5");
                }

                container.Add(new CuiElement
                {
                    Name = "SkillTree_points_assigned_5",
                    Parent = "SkillTree_panel_5",
                    Components = {
                    new CuiTextComponent { Text = node.Value.level_current.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "0 0" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"stsendsubmenu {tree} {node.Key}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                }, "SkillTree_panel_5", "SkillTree_button_5");

                if (-79 + (i * (space + 58)) > furthest_x) furthest_x = -79 + (i * (space + 58));
            }

            //if (ni.points_spent < ni.level_2_point_requirement)
            //{
            //    container.Add(new CuiElement
            //    {
            //        Name = "Required_Points",
            //        Parent = "SkillTree",
            //        Components = {
            //            new CuiTextComponent { Text = $lang.GetMessage("UIUnlocksIn", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
            //            new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
            //            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-220 -10", OffsetMax = "-150 40" }
            //        }
            //    });

            //    container.Add(new CuiElement
            //    {
            //        Name = "Required_Points_Value",
            //        Parent = "Required_Points",
            //        Components = {
            //            new CuiTextComponent { Text = $"<color=#61e500>{ni.level_2_point_requirement - ni.points_spent}</color>", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
            //            new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
            //            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20 -40", OffsetMax = "20 0" }
            //        }
            //    });
            //}            

            for (int i = 0; i < T3_Nodes.Count; i++)
            {
                var node = T3_Nodes.ElementAt(i);
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1698113 0.1698113 0.1698113 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-137 + (i * (space + 58))} {-117}", OffsetMax = $"{-79 + (i * (space + 58))} {-59}" }
                }, "SkillTree", "SkillTree_panel_8");

                if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase) || (NodeSkinDirectory.TryGetValue(node.Key, out skinID) && skinID == 0))
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_8",
                        Parent = "SkillTree_panel_8",
                        Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", node.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_8",
                        Parent = "SkillTree_panel_8",
                        Components = {
                        new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = skinID },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });
                }



                if (ni.points_spent < ni.level_3_point_requirement)
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.3584906 0.3584906 0.3584906 0.8823529" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }, "SkillTree_panel_8", "SkillTree_Grey_Box_8");
                }

                container.Add(new CuiElement
                {
                    Name = "SkillTree_points_assigned_8",
                    Parent = "SkillTree_panel_8",
                    Components = {
                    new CuiTextComponent { Text = node.Value.level_current.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "0 0" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"stsendsubmenu {tree} {node.Key}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                }, "SkillTree_panel_8", "SkillTree_button_8");

                if (-79 + (i * (space + 58)) > furthest_x) furthest_x = -79 + (i * (space + 58));
            }

            List<string> categories = Pool.GetList<string>();
            categories.AddRange(ti.trees.Keys);

            if (categories.Count > 1 && categories.IndexOf(tree) > 0)
            {
                var prevTree = categories[categories.IndexOf(tree) - 1];
                container.Add(new CuiElement
                {
                    Name = "SkillTree_Back",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIBackArrow", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-137 -254.67", OffsetMax = "-79 -222.67" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"stmenuchangepage {prevTree}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
                }, "SkillTree_Back", "SkillTree_back_button");
            }
            if (categories.Count > 1 && categories.IndexOf(tree) < categories.Count - 1)
            {
                var nextTree = categories[categories.IndexOf(tree) + 1];
                container.Add(new CuiElement
                {
                    Name = "SkillTree_forward",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UINextArrow", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "79 -254.67", OffsetMax = "137 -222.67" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"stmenuchangepage {nextTree}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
                }, "SkillTree_forward", "SkillTree_forward_button");
            }
            Pool.FreeList(ref categories);
            container.Add(new CuiElement
            {
                Name = "SkillTree_close",
                Parent = "SkillTree",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIClose", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -254.67", OffsetMax = "29 -222.67" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"stmenuclosemain" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
            }, "SkillTree_close", "SkillTree_close_button");

            if (foundNode != null)
            {
                container.Add(new CuiElement
                {
                    Name = "SkillTree_node_name",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UISelectedNode", this, player.UserIDString), lang.GetMessage(selected_node, this, player.UserIDString)), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.04 127.8", OffsetMax = "-233.36 159.8" }
                }
                });

                string nodeDescription = foundNode.description;

                if (config.trees[tree].nodes[selected_node].permissions != null)
                {
                    nodeDescription += config.trees[tree].nodes[selected_node].permissions.description;
                }

                nodeDescription += AddAdditionalDescription(foundNode.buffInfo.Key, player.UserIDString);

                container.Add(new CuiElement
                {
                    Name = "SkillTree_node_description",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = nodeDescription, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.037 0", OffsetMax = "-233.363 105.3" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SkillTree_node_max_level",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UIMaxLevel", this, player.UserIDString), foundNode.level_max), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.037 -29", OffsetMax = "-233.363 -5.982" }
                }
                });
                if (foundNode.level_max != foundNode.level_current)
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.2358491 0.2358491 0.2358491 0.6176471" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-291.36 -33.491", OffsetMax = "-233.36 -1.491" }
                    }, "SkillTree", "Panel_3082");

                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_level",
                        Parent = "SkillTree",
                        Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILevelUpButton", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-291.36 -33.491", OffsetMax = "-233.36 -1.491" }
                    }
                    });

                    container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = $"stdolevel {tree} {selected_node}" },
                        Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
                    }, "SkillTree_level", "SkillTree_level_button");
                }
            }
            container.Add(new CuiElement
            {
                Name = "SkillTree_Points_available",
                Parent = "SkillTree",
                Components = {
                    new CuiTextComponent { Text = $"{lang.GetMessage("UIAvailablePoints", this, player.UserIDString)} <color=#42f105>{pcdData.pEntity[player.userID].available_points}</color>", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.04 -190.019", OffsetMax = "-284.04 -167.001" }
                }
            });
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values != null)
            {
                container.Add(new CuiElement
                {
                    Name = "SkillTree_buff_totals",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIBuffInformation", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{(furthest_x + 36 > 172.59 ? furthest_x + 36 : 172.59)} 127.8", OffsetMax = $"{(furthest_x + 36 + 166 > 338.59 ? furthest_x + 36 + 166 : 338.59)} 159.8" }
                }
                });
                StringBuilder sb = new StringBuilder("");
                StringBuilder sb2 = new StringBuilder("");
                var loopCount = 0;
                foreach (var buff in bd.buff_values)
                {
                    if (buff.Value == 0) continue;
                    if (BuffBuffType[buff.Key] == BuffType.Permission) continue;
                    string value = "";
                    if (BuffBuffType.ContainsKey(buff.Key))
                    {
                        if (BuffBuffType[buff.Key] == BuffType.Percentage) value = $"+{Math.Round(buff.Value / 1 * 100, 2)}%";
                        else if (BuffBuffType[buff.Key] == BuffType.Seconds) value = $"-{buff.Value} seconds";
                        else if (BuffBuffType[buff.Key] == BuffType.PerSecond) value = $"+{buff.Value} / second";
                        else if (BuffBuffType[buff.Key] == BuffType.Slots) value = $"+{buff.Value} slots";
                        else value = lang.GetMessage("UIEnabled", this, player.UserIDString);
                    }
                    if (loopCount < 38) sb.AppendFormat("{0} - <color=#42f105>{1}</color>\n", lang.GetMessage("UI" + buff.Key.ToString(), this, player.UserIDString), value);
                    else sb2.AppendFormat("{0} - <color=#42f105>{1}</color>\n", lang.GetMessage("UI" + buff.Key.ToString(), this, player.UserIDString), value);

                    loopCount++;
                }
                Dictionary<string, string> perms = new Dictionary<string, string>();
                if (Tracked_perms.ContainsKey(player.UserIDString))
                {
                    foreach (var list in Tracked_perms[player.UserIDString])
                    {
                        foreach (var entry in list.Value)
                        {
                            if (!perms.ContainsKey(entry.Key)) perms.Add(entry.Key, entry.Value);
                        }
                    }
                }

                int count = sb.ToString().Split('\n').Length;
                foreach (var perm in perms)
                {
                    if (count <= 38) sb.AppendFormat("Perm - <color=#42f105>{0}</color>\n", perm.Value ?? perm.Key);
                    else sb2.AppendFormat("Perm - <color=#42f105>{0}</color>\n", perm.Value ?? perm.Key);
                    count++;
                }
                //Tracked_perms
                container.Add(new CuiElement
                {
                    Name = "SkillTree_Buffs_displayed",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = sb.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{(furthest_x + 36 > 172.585 ? furthest_x + 36 : 172.585)} -295.889", OffsetMax = $"{(furthest_x + 36 + 143.704f > 316.289 ? furthest_x + 36 + 143.704f : 316.289)} 117" }
                }
                });
                container.Add(new CuiElement
                {
                    Name = "SkillTree_Buffs_displayed_second",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = sb2.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{(furthest_x + 36 + 143.704f > 316.289 ? furthest_x + 36 + 143.704f : 316.289)} -295.89", OffsetMax = $"{(furthest_x + 36 + 287.408f > 459.992 ? furthest_x + 36 + 287.408f : 459.992)} 117" }
                }
                });
                perms.Clear();
                if (config.general_settings.allow_respecs)
                {
                    var cost = (double)0;
                    var formatted = "";
                    var respec_cost = GetRespecCost(player);
                    if (respec_cost > 0) cost = Math.Round(totalPoints * respec_cost, 2);
                    if (config.general_settings.respec_currency.Equals("scrap", StringComparison.OrdinalIgnoreCase)) formatted = $"{cost} {lang.GetMessage("UIScrap", this, player.UserIDString)}";
                    else if (config.general_settings.respec_currency.Equals("economics", StringComparison.OrdinalIgnoreCase)) formatted = $"{lang.GetMessage("UIDollars", this, player.UserIDString)}{cost}";
                    else if (config.general_settings.respec_currency.Equals("srp", StringComparison.OrdinalIgnoreCase)) formatted = $"{cost} {lang.GetMessage("UIPoints", this, player.UserIDString)}";
                    else if (config.general_settings.respec_currency.Equals("custom", StringComparison.OrdinalIgnoreCase)) formatted = $"{cost} {config.general_settings.respec_currency_custom.displayName}";
                    else formatted = cost.ToString();
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_Respec_cost_title",
                        Parent = "SkillTree",
                        Components = {
                        new CuiTextComponent { Text = string.Format(lang.GetMessage("RespecCost", this, player.UserIDString), formatted), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.04 -77.108", OffsetMax = "-291.36 -54.091" }
                    }
                    });
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.2358491 0.2358491 0.2358491 0.6176471" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-291.36 -81.6", OffsetMax = "-233.36 -49.6" }
                    }, "SkillTree", "SkillTree_Respec_cost_button_panel");

                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_Respec_cost_label",
                        Parent = "SkillTree_Respec_cost_button_panel",
                        Components = {
                        new CuiTextComponent { Text = lang.GetMessage("RespecButton", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
                    }
                    });
                    container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = $"respecconfirmation {cost} {tree} {selected_node}" },
                        Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
                    }, "SkillTree_Respec_cost_label", "SkillTree_Respec_cost_button");
                }
            }
            PlayerInfo playerData;

            if (pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                if (config.rested_xp_settings.rested_xp_enabled)
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_Player_Rested_XP",
                        Parent = "SkillTree",
                        Components = {
                    new CuiTextComponent { Text = $"{lang.GetMessage("UIRestedXPPool", this, player.UserIDString)} <color=#03b2d9>{Math.Round(playerData.xp_bonus_pool, config.xp_settings.xp_rounding):###,###,##0}</color>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.04 -117.619", OffsetMax = "-291.36 -94.602" }
                    }
                    });
                }

                var level = config.general_settings.max_player_level > 0 && playerData.current_level > config.general_settings.max_player_level ? config.general_settings.max_player_level : playerData.current_level;
                container.Add(new CuiElement
                {
                    Name = "SkillTree_Player_Level",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = $"{lang.GetMessage("UICurrentLevel", this, player.UserIDString)} <color=#03b2d9>{level}</color>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.04 -142.309", OffsetMax = "-291.36 -119.292" }
                }
                });

                string debt = playerData.xp_debt > 0 ? string.Format(lang.GetMessage("UIDebtText", this, player.UserIDString), $"{Math.Round(playerData.xp_debt, config.xp_settings.xp_rounding)}") : null;
                //string debt = playerData.xp_debt > 0 ? $" [<color=#fb2a00>{Math.Round(playerData.xp_debt, config.xp_settings.xp_rounding):###,###,##0}</color>]" : null;
                container.Add(new CuiElement
                {
                    Name = "SkillTree_Player_XP",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = $"{lang.GetMessage("UIXP", this, player.UserIDString)} <color=#03b2d9>{Math.Round(playerData.xp, 2):###,###,##0}</color>/<color=#03b2d9>{Math.Round(config.level.GetLevelStartXP(playerData.current_level + 1), config.xp_settings.xp_rounding):###,###,##0}</color>" + debt, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.04 -166.999", OffsetMax = "-188.512 -143.982" }
                }
                });
            }

            if (ni.min_level > 0)
            {
                var minLevel = GetLevelRequirement(player, tree, ni.min_level);
                container.Add(new CuiElement
                {
                    Name = "SkillTree_MinLevel",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = $"{(playerData != null && playerData.current_level >= minLevel ? lang.GetMessage("MinLevelColHasLevel", this, player.UserIDString) : lang.GetMessage("MinLevelColUnderLevel", this, player.UserIDString))}{string.Format(lang.GetMessage("MinLevelString", this, player.UserIDString), minLevel)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-180 160.4", OffsetMax = "180 220.4" }
                }
                });
            }

            if (ni.min_points > 0)
            {
                var minPoints = GetPointRequirement(player, tree, ni.min_points);
                container.Add(new CuiElement
                {
                    Name = "SkillTree_MinPoints",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = $"{(totalPoints >= minPoints ? lang.GetMessage("MinLevelColHasLevel", this, player.UserIDString) : lang.GetMessage("MinLevelColUnderLevel", this, player.UserIDString))}{string.Format(lang.GetMessage("MinPointString", this, player.UserIDString), minPoints)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = ni.min_level > 0 ? "-180 140.4" : "-180 160.4", OffsetMax = ni.min_level > 0 ? "180 200.4" : "180 220.4" }
                }
                });
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2358491 0.2358491 0.2358491 0.3176471" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450.04 -254.67", OffsetMax = "-321.646 -222.67" }
            }, "SkillTree", "SkillTree_player_settings_panel");

            container.Add(new CuiElement
            {
                Name = "SkillTree_player_settings_text",
                Parent = "SkillTree_player_settings_panel",
                Components = {
                    new CuiTextComponent { Text = $"<color=#ffb600>{lang.GetMessage("ButtonPlayerSettings", this, player.UserIDString)}</color>", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.195 -16", OffsetMax = "64.195 16" }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"stsendplayersettingsmenu" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.195 -16", OffsetMax = "64.195 16" }
            }, "SkillTree_player_settings_text", "SkillTree_player_settings_button");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2358491 0.2358491 0.2358491 0.3176471" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-311.647 -254.67", OffsetMax = "-183.253 -222.67" }
            }, "SkillTree", "SkillTree_ultimate_settings_panel");

            container.Add(new CuiElement
            {
                Name = "SkillTree_ultimate_settings_text",
                Parent = "SkillTree_ultimate_settings_panel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UltimateSettings", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.195 -16", OffsetMax = "64.195 16" }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"stsendultimatesettingsmenu" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.195 -16", OffsetMax = "64.195 16" }
            }, "SkillTree_ultimate_settings_text", "SkillTree_ultimate_settings_button");

            if (Ultimate_node.Key != null && Ultimate_node.Value != null)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = config.ultimate_settings.ultimate_node_background_col },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{(biggest <= 4 ? -29 : -137)} -195", OffsetMax = $"{(biggest <= 4 ? 29 : -79)} -137" }
                }, "SkillTree", "SkillTree_panel_ultimate");

                if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase) || (NodeSkinDirectory.TryGetValue(Ultimate_node.Key, out skinID) && skinID == 0))
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_ultimate",
                        Parent = "SkillTree_panel_ultimate",
                        Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", Ultimate_node.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });

                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_img_ultimate",
                        Parent = "SkillTree_panel_ultimate",
                        Components = {
                        new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = skinID },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }
                    });
                }


                if (ni.points_spent < ni.level_4_point_requirement)
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.3584906 0.3584906 0.3584906 0.8823529" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                    }, "SkillTree_panel_ultimate", "SkillTree_Grey_Box_ultimate");

                }

                container.Add(new CuiElement
                {
                    Name = "SkillTree_points_assigned_ultimate",
                    Parent = "SkillTree_panel_ultimate",
                    Components = {
                    new CuiTextComponent { Text = Ultimate_node.Value.level_current.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "0 0" }
                }
                });
                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"stsendsubmenu {tree} {Ultimate_node.Key}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-27 -27", OffsetMax = "27 27" }
                }, "SkillTree_panel_ultimate", "SkillTree_button_ultimate");
            }

            if (ni.points_spent < ni.level_2_point_requirement)
            {
                container.Add(new CuiElement
                {
                    Name = "Required_Points_2",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIUnlocksIn", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-217 2", OffsetMax = "-137 22" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "Required_Points_Value",
                    Parent = "Required_Points_2",
                    Components = {
                    new CuiTextComponent { Text = $"<color=#61e500>{ni.level_2_point_requirement - ni.points_spent}</color>", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -38.1", OffsetMax = "40 -10.1" }
                }
                });
            }
            else if (ni.points_spent < ni.level_3_point_requirement)
            {
                container.Add(new CuiElement
                {
                    Name = "Required_Points_3",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIUnlocksIn", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-217 -87", OffsetMax = "-137 -67" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "Required_Points_Value",
                    Parent = "Required_Points_3",
                    Components = {
                    new CuiTextComponent { Text = $"<color=#61e500>{ni.level_3_point_requirement - ni.points_spent}</color>", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -38.1", OffsetMax = "40 -10.1" }
                }
                });
            }
            else if (ni.points_spent < ni.level_4_point_requirement)
            {
                container.Add(new CuiElement
                {
                    Name = "Required_Points_4",
                    Parent = "SkillTree",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIUnlocksIn", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-217 -165", OffsetMax = "-137 -145" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "Required_Points_Value",
                    Parent = "Required_Points_4",
                    Components = {
                    new CuiTextComponent { Text = $"<color=#61e500>{ni.level_4_point_requirement - ni.points_spent}</color>", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -38.1", OffsetMax = "40 -10.1" }
                }
                });
            }

            CuiHelper.DestroyUi(player, "SkillTree");
            CuiHelper.AddUi(player, container);
        }

        string AddAdditionalDescription(Buff buff, string userid)
        {
            switch (buff)
            {
                case Buff.Durability: return string.Format(lang.GetMessage("Buff.Durability_Extended.Description", this, userid), string.Join(", ", config.buff_settings.durability_blacklist));

                case Buff.Raiding_Ultimate: return string.Format(lang.GetMessage("Buff.Raiding_Ultimate_Extended.Description", this, userid), config.ultimate_settings.ultimate_raiding.command, config.ultimate_settings.ultimate_raiding.cooldown) + (config.ultimate_settings.ultimate_raiding.require_ammo ? string.Format(lang.GetMessage("Buff.Raiding_Ultimate_Ammo_Extended.Description", this, userid), MissileQuantity) : null);

                case Buff.Trap_Damage_Reduction: return config.buff_settings.raid_perk_settings.trap_damage_reduction_settings.raidable_bases_only && RaidableBases != null && RaidableBases.IsLoaded ? lang.GetMessage("OnlyWorksWithRaidableBases", this, userid) : null;

                case Buff.Trap_Damage_Increase: return config.buff_settings.raid_perk_settings.trap_damage_increase_settings.raidable_bases_only && RaidableBases != null && RaidableBases.IsLoaded ? lang.GetMessage("OnlyWorksWithRaidableBases", this, userid) : null;

                case Buff.Dudless_Explosive: return config.buff_settings.raid_perk_settings.Dudless_Explosiv_settings.raidable_bases_only && RaidableBases != null && RaidableBases.IsLoaded ? lang.GetMessage("OnlyWorksWithRaidableBases", this, userid) : null;

                case Buff.Personal_Explosive_Reduction:
                    string personal_explosive_reduction_string = string.Format(lang.GetMessage("Buff.Personal_Explosive_Reduction_Extended.Description.ReducesFireDamage", this, userid), config.buff_settings.raid_perk_settings.personal_explosive_reduction_settings.fire_damage_reduction);
                    if (config.buff_settings.raid_perk_settings.personal_explosive_reduction_settings.blacklist.Count > 0) personal_explosive_reduction_string += string.Format(lang.GetMessage("UIExcludesList", this, userid), string.Join(", ", config.buff_settings.raid_perk_settings.personal_explosive_reduction_settings.blacklist));
                    return personal_explosive_reduction_string;

                case Buff.Explosion_Radius:
                    string Explosion_Radius_string = null;
                    if (config.buff_settings.raid_perk_settings.Explosion_Radius_settings.raidable_bases_only && RaidableBases != null && RaidableBases.IsLoaded) Explosion_Radius_string += lang.GetMessage("OnlyWorksWithRaidableBases", this, userid);
                    if (config.buff_settings.raid_perk_settings.Explosion_Radius_settings.blacklist.Count > 0) Explosion_Radius_string += string.Format(lang.GetMessage("UIExcludesList", this, userid), string.Join(", ", config.buff_settings.raid_perk_settings.Explosion_Radius_settings.blacklist));
                    return Explosion_Radius_string;

                case Buff.Double_Explosion_Chance:
                    string Double_Explosion_Chance_string = null;
                    if (config.buff_settings.raid_perk_settings.Double_Explosion_chance_settings.raidable_bases_only && RaidableBases != null && RaidableBases.IsLoaded) Double_Explosion_Chance_string += lang.GetMessage("OnlyWorksWithRaidableBases", this, userid);
                    if (config.buff_settings.raid_perk_settings.Double_Explosion_chance_settings.blacklist.Count > 0) Double_Explosion_Chance_string += string.Format(lang.GetMessage("UIExcludesList", this, userid), string.Join(", ", config.buff_settings.raid_perk_settings.Double_Explosion_chance_settings.blacklist));
                    if (!config.ultimate_settings.ultimate_raiding.allow_doubling) Double_Explosion_Chance_string += lang.GetMessage("Buff.Double_Explosion_Chance_Extended.Description", this, userid);
                    return Double_Explosion_Chance_string;

                case Buff.Lock_Picker:
                    string Lock_Picker_string = config.buff_settings.raid_perk_settings.Lock_Picker_settings.raidable_bases_only && RaidableBases != null && RaidableBases.IsLoaded ? lang.GetMessage("OnlyWorksWithRaidableBases", this, userid) : null;
                    Lock_Picker_string += string.Format(lang.GetMessage("Buff.Lock_Picker_Extended.Description.Command", this, userid), config.buff_settings.raid_perk_settings.Lock_Picker_settings.pick_command);
                    Lock_Picker_string += string.Format(lang.GetMessage("Buff.Lock_Picker_Extended.Description.Cooldown", this, userid), config.buff_settings.raid_perk_settings.Lock_Picker_settings.use_delay);
                    return Lock_Picker_string;

                case Buff.Mining_Yield:
                    string miningString = string.Empty;
                    if (config.tools_black_white_list_settings.power_tool_modifier.mining_yield_modifier != 1) miningString += string.Format(lang.GetMessage("Buff.Mining_Yield_Extended.Description", this, userid), config.tools_black_white_list_settings.power_tool_modifier.mining_yield_modifier < 1 ? "-" : "+", (1 - config.tools_black_white_list_settings.power_tool_modifier.mining_yield_modifier) * 100);
                    return miningString;

                case Buff.Woodcutting_Luck:
                case Buff.Woodcutting_Coal:
                case Buff.Instant_Chop:
                case Buff.Regrowth:
                    return config.tools_black_white_list_settings.power_tool_modifier.woodcutting_luck_modifier != 1 ? string.Format(lang.GetMessage("WoodcuttingLuckModifierDescription", this, userid), config.tools_black_white_list_settings.power_tool_modifier.woodcutting_luck_modifier < 1 ? "-" : "+", (1 - config.tools_black_white_list_settings.power_tool_modifier.woodcutting_luck_modifier) * 100) : null;

                case Buff.Instant_Mine:
                case Buff.Smelt_On_Mine:
                case Buff.Node_Spawn_Chance:
                case Buff.Mining_Luck:
                    return config.tools_black_white_list_settings.power_tool_modifier.mining_luck_modifier != 1 ? string.Format(lang.GetMessage("MiningLuckModifierDescription", this, userid), config.tools_black_white_list_settings.power_tool_modifier.mining_luck_modifier < 1 ? "-" : "+", (1 - config.tools_black_white_list_settings.power_tool_modifier.mining_luck_modifier) * 100) : null;


                case Buff.Skinning_Luck:
                case Buff.Skin_Cook:
                case Buff.Instant_Skin:
                    return config.tools_black_white_list_settings.power_tool_modifier.skinning_luck_modifier != 1 ? string.Format(lang.GetMessage("SkinningLuckModifierDescription", this, userid), config.tools_black_white_list_settings.power_tool_modifier.skinning_luck_modifier < 1 ? "-" : "+", (1 - config.tools_black_white_list_settings.power_tool_modifier.skinning_luck_modifier) * 100) : null;

                case Buff.Woodcutting_Yield:
                    string woodcuttingString = string.Empty;
                    if (config.tools_black_white_list_settings.power_tool_modifier.woodcutting_yield_modifier != 1) woodcuttingString += string.Format(lang.GetMessage("Buff.Woodcutting_Yield.Description", this, userid), config.tools_black_white_list_settings.power_tool_modifier.woodcutting_yield_modifier < 1 ? "-" : "+", (1 - config.tools_black_white_list_settings.power_tool_modifier.woodcutting_yield_modifier) * 100);
                    return woodcuttingString;

                case Buff.Skinning_Yield:
                    string skinningString = string.Empty;
                    if (config.tools_black_white_list_settings.power_tool_modifier.skinning_yield_modifier != 1) skinningString += string.Format(lang.GetMessage("Buff.Skinning_Yield_Extended.Description", this, userid), config.tools_black_white_list_settings.power_tool_modifier.skinning_yield_modifier < 1 ? "-" : "+", (1 - config.tools_black_white_list_settings.power_tool_modifier.skinning_yield_modifier) * 100);
                    return skinningString;

                case Buff.AnimalTracker: return config.buff_settings.track_delay > 0 ? string.Format(lang.GetMessage("Buff.Cooldown", this, userid), config.buff_settings.track_delay, lang.GetMessage("Seconds", this, userid)) : null;

                case Buff.HealthRegen: return config.buff_settings.health_regen_combat_delay > 0 ? string.Format(lang.GetMessage("Buff.HealthRegen.Delay", this, userid), config.buff_settings.health_regen_combat_delay) : null;

                case Buff.PVP_Critical: return string.Format(lang.GetMessage("Buff.PVP_Critical.Amount", this, userid), config.buff_settings.pvp_critical_modifier * 100);

                case Buff.Loot_Pickup:
                    string Loot_Pickup_string = null;
                    if (config.buff_settings.lootPickupBuffMeleeOnly) Loot_Pickup_string += lang.GetMessage("Buff.Loot_Pickup.MeleeOnly", this, userid);
                    if (config.buff_settings.loot_pickup_buff_max_distance > 0) Loot_Pickup_string += string.Format(lang.GetMessage("Buff.Loot_Pickup.Distance", this, userid), config.buff_settings.loot_pickup_buff_max_distance);
                    return Loot_Pickup_string;

                case Buff.Animal_Damage_Resist: return string.Format(lang.GetMessage("Buff.Animal_Damage_Resist.Animals", this, userid), FormatAnimalNames(config.buff_settings.animals, userid));

                case Buff.ExtraPockets: return config.buff_settings.bag_cooldown_time > 0 ? string.Format(lang.GetMessage("Buff.Cooldown", this, userid), config.buff_settings.bag_cooldown_time, lang.GetMessage("Seconds", this, userid)) : null;

                case Buff.Harvest_Wild_Yield: return config.buff_settings.harvest_yield_blacklist.Count > 0 ? string.Format(lang.GetMessage("Buff.Excluded", this, userid), string.Join(", ", config.buff_settings.harvest_yield_blacklist)) : null;

                case Buff.Harvester_Ultimate: return string.Format(lang.GetMessage("Harvesting_Ultimate_Command", this, userid), config.ultimate_settings.ultimate_harvesting.gene_chat_command);

                case Buff.Forager: return string.Format(lang.GetMessage("Buff.Forager.Description", this, userid), config.buff_settings.forager_settings.command, config.buff_settings.forager_settings.cooldown);

                case Buff.Trap_Spotter: return string.Format(lang.GetMessage("Buff.Trap_Spotter.Description", this, userid), config.buff_settings.raid_perk_settings.Trap_Spotter_settings.command, config.buff_settings.raid_perk_settings.Trap_Spotter_settings.cooldown) + (config.buff_settings.raid_perk_settings.Trap_Spotter_settings.raidable_bases_only && RaidableBases != null && RaidableBases.IsLoaded ? lang.GetMessage("OnlyWorksWithRaidableBases", this, userid) : null);

                case Buff.UnderwaterDamageBonus: return "\n" + string.Format(lang.GetMessage("UnderwaterDamageBonusPVP", this, userid), config.buff_settings.UnderwaterDamageBonus_pvp);
                case Buff.Mining_Ultimate: return "\n" + string.Format(lang.GetMessage("Mining_Ultimate_Command", this, userid), config.ultimate_settings.ultimate_mining.find_node_cmd);
                default: return null;
            }
        }

        string FormatAnimalNames(List<string> stringArray, string userid)
        {
            var result = string.Empty;
            foreach (var animal in stringArray)
            {
                if (string.IsNullOrEmpty(result)) result = lang.GetMessage(animal, this, userid);
                else result += ", " + lang.GetMessage(animal, this, userid);
            }

            return result;
        }

        [ConsoleCommand("stsendplayersettingsmenu")]
        void SendPlayerSettings(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            SkillTree_PlayerMenu(player);
        }

        [ConsoleCommand("stsendultimatesettingsmenu")]
        void SendUltimateSettingsMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            SkillTree_UltimateMenu(player);
        }

        #endregion

        #region Navigation Menu

        private void NavigationMenu(BasePlayer player)
        {
            if (!config.general_settings.show_navigation_buttons) return;

            TreeInfo ti;
            if (!TreeData.TryGetValue(player.userID, out ti))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - SendSkillTreeMenu. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (ti.trees == null || ti.trees.Count == 0)
            {
                CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
                Player.Message(player, "You do not have any tree permissions. Please apply permission skilltree.<category> if you want to allocate individual trees, or skilltree.all if you want players to access all categories.", config.misc_settings.ChatID);
                return;
            }
            List<string> trees = Pool.GetList<string>();
            trees.AddRange(ti.trees.Keys);

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-30.175 -32", OffsetMax = "29.825 -12" }
            }, "Overlay", "NavigationMenu");

            var elementCount = trees.Count < 12 ? trees.Count : 12;
            var totalLength = 0 - ((elementCount * 80) + (elementCount * 10));
            var startOffsetModifier = totalLength / 2;
            var count = 0;
            var row = 0;

            for (int i = 0; i < trees.Count; i++)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1686275 0.1686275 0.1686275 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{startOffsetModifier + (count * 10) + (count * 80)} {-10 - (row * 5) - (row * 20)}", OffsetMax = $"{startOffsetModifier + (count * 10) + 80 + (count * 80)} {10 - (row * 5) - (row * 20)}" }
                }, "NavigationMenu", $"Tree_{i}");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.3568628 0.3568628 0.3568628 0.8784314", Command = $"navigatetotree {trees[i]}" },
                    Text = { Text = lang.GetMessage(trees[i], this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38 -8", OffsetMax = "38 8" }
                }, $"Tree_{i}", "Button");

                count++;
                if (count > 12)
                {
                    row++;
                    count = 0;
                }
            }

            Pool.FreeList(ref trees);
            CuiHelper.DestroyUi(player, "NavigationMenu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("navigatetotree")]
        void NavigateToTree(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            var tree = string.Join("_", arg.Args);
            SendSkillTreeMenu(player, tree);
        }

        #endregion

        #region XPMenu

        string UnmodifiedCol;
        string ModifiedCol;

        void DisplayXPMenu(BasePlayer player, double xp, bool modified)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "XP_Tick",
                Parent = "Overlay",
                Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("popupxpstring", this, player.UserIDString), modified ? ModifiedCol : UnmodifiedCol, Math.Round(xp, config.xp_settings.xp_rounding)), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26.83 244.369", OffsetMax = "26.83 268.631" }
                }
            });

            CuiHelper.DestroyUi(player, "XP_Tick");
            CuiHelper.AddUi(player, container);

            timer.Once(config.xp_settings.xp_display_time, () => CuiHelper.DestroyUi(player, "XP_Tick"));
        }

        #endregion

        #region respec confirmation menu

        void ConfirmRespec(BasePlayer player, double cost, string tree, string selected_node)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9907843" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0.011 0.009", OffsetMax = "0.011 0.339" }
            }, "Overlay", "respec_confirmation");

            string text = lang.GetMessage("UIAreYouSure", this, player.UserIDString);
            if (config.general_settings.respec_multiplier > 0)
            {
                PlayerInfo pi;
                if (pcdData.pEntity.TryGetValue(player.userID, out pi)) text += string.Format(lang.GetMessage("RespecMultiplierMessage", this, player.UserIDString), Mathf.Max(pi.respec_multiplier + config.general_settings.respec_multiplier, config.general_settings.respec_multiplier_max) * 100);
            }

            container.Add(new CuiElement
            {
                Name = "respec_confirmation_title",
                Parent = "respec_confirmation",
                Components = {
                    new CuiTextComponent { Text = text, Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-117.039 63.5", OffsetMax = "117.039 163.5" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2358491 0.2358491 0.2358491 0.3176471" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-117.039 -16", OffsetMax = "-59.039 16" }
            }, "respec_confirmation", "respec_confirmation_bttn_panel_yes");

            container.Add(new CuiElement
            {
                Name = "respec_confirmation_bttn_text_yes",
                Parent = "respec_confirmation_bttn_panel_yes",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("ButtonYes", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"dorespec {cost} {tree} {selected_node}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
            }, "respec_confirmation_bttn_text_yes", "respec_confirmation_bttn_yes");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2358491 0.2358491 0.2358491 0.3176471" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "59.039 -16", OffsetMax = "117.039 16" }
            }, "respec_confirmation", "respec_confirmation_bttn_panel_no");

            container.Add(new CuiElement
            {
                Name = "respec_confirmation_bttn_text_no",
                Parent = "respec_confirmation_bttn_panel_no",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("ButtonNo", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
                }
            });

            var cost_string = lang.GetMessage("UICost", this, player.UserIDString);
            if (config.general_settings.respec_currency.Equals("scrap", StringComparison.OrdinalIgnoreCase)) cost_string = string.Format(cost_string, $"{cost} {lang.GetMessage("UIScrap", this, player.UserIDString)}");
            else if (config.general_settings.respec_currency.Equals("economics", StringComparison.OrdinalIgnoreCase)) cost_string = string.Format(cost_string, $"{lang.GetMessage("UIDollars", this, player.UserIDString)}{cost}");
            else if (config.general_settings.respec_currency.Equals("srp", StringComparison.OrdinalIgnoreCase)) cost_string = string.Format(cost_string, $"{cost} {lang.GetMessage("UIPoints", this, player.UserIDString)}");
            else if (config.general_settings.respec_currency.Equals("custom", StringComparison.OrdinalIgnoreCase)) cost_string = string.Format(cost_string, $"{cost} {config.general_settings.respec_currency_custom.displayName}");

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"closerecpecconfirmation {tree} {selected_node}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
            }, "respec_confirmation_bttn_text_no", "respec_confirmation_bttn_no");

            container.Add(new CuiElement
            {
                Name = "respec_confirmation_cost",
                Parent = "respec_confirmation",
                Components = {
                    new CuiTextComponent { Text = cost_string, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-68.137 28.482", OffsetMax = "68.137 63.5" }
                }
            });

            CuiHelper.DestroyUi(player, "respec_confirmation");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Menu commands

        [ConsoleCommand("respecconfirmation")]
        void SendConfirmation(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var cost = Convert.ToDouble(arg.Args[0]);
            var tree = arg.Args[1];
            var name = string.Join(" ", arg.Args.Skip(2));
            //CuiHelper.DestroyUi(player, "SkillTree");
            ConfirmRespec(player, cost, tree, name);
        }

        [ConsoleCommand("closerecpecconfirmation")]
        void CloseRespecConfirmation(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "respec_confirmation");
            //var tree = Convert.ToInt32(arg.Args[0]);
            //if (arg.Args.Length == 1) SendSkillTreeMenu(player, (string)tree);
            //else
            //{
            //    var name = string.Join(" ", arg.Args.Where(x => !x.IsNumeric()));
            //    SendSkillTreeMenu(player, (string)tree, name);
            //}            
        }

        [ConsoleCommand("stsendsubmenu")]
        void SendSubMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var tree = arg.Args[0];
            var name = string.Join(" ", arg.Args.Skip(1));
            SendSkillTreeMenu(player, tree, name);
        }

        [ConsoleCommand("stmenuclosemain")]
        void CloseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "SkillTree");
            CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
            CuiHelper.DestroyUi(player, "NavigationMenu");
        }

        [ConsoleCommand("stmenuchangepage")]
        void SendNextPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var tree = arg.Args[0];
            SendSkillTreeMenu(player, tree);
        }

        [ConsoleCommand("stdolevel")]
        void DoLevel(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var tree = arg.Args[0];
            var name = string.Join(" ", arg.Args.Skip(1));
            LevelUpNode(player, tree, name);
            SendSkillTreeMenu(player, tree, name);
        }

        [ConsoleCommand("dorespec")]
        void DoRespec(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "respec_confirmation");
            var cost = Convert.ToDouble(arg.Args[0]);
            if (cost > 0)
            {
                if (config.general_settings.respec_currency.Equals("scrap", StringComparison.OrdinalIgnoreCase))
                {
                    var found = 0;
                    foreach (var item in player.inventory.AllItems())
                    {
                        if (item.info.shortname == "scrap") found += item.amount;
                        if (found >= cost) break;
                    }
                    if (found < cost)
                    {
                        Player.Message(player, lang.GetMessage("RespecNoScrap", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                    found = 0;
                    foreach (var item in player.inventory.AllItems())
                    {
                        if (item.info.shortname != "scrap") continue;
                        if (item.amount + found == cost)
                        {
                            item.Remove();
                            break;
                        }
                        if (item.amount + found < cost)
                        {
                            found += item.amount;
                            item.Remove();
                        }
                        else
                        {
                            item.UseItem(Convert.ToInt32(cost) - found);
                            break;
                        }
                    }
                }
                else if (config.general_settings.respec_currency.Equals("economics", StringComparison.OrdinalIgnoreCase))
                {
                    if (Economics == null)
                    {
                        Player.Message(player, lang.GetMessage("EconNotLoaded", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                    var playerBalance = Convert.ToDouble(Economics?.Call("Balance", player.userID));
                    if (playerBalance < cost)
                    {
                        Player.Message(player, lang.GetMessage("EconNoCash", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                    if (!Convert.ToBoolean(Economics?.Call("Withdraw", player.userID, cost)))
                    {
                        Player.Message(player, lang.GetMessage("EconErrorCash", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                }
                else if (config.general_settings.respec_currency.Equals("srp", StringComparison.OrdinalIgnoreCase))
                {
                    if (ServerRewards == null)
                    {
                        Player.Message(player, lang.GetMessage("SRNotLoaded", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                    var balance = Convert.ToInt32(ServerRewards.Call("CheckPoints", player.userID));
                    if (balance < cost)
                    {
                        Player.Message(player, lang.GetMessage("SRNoPoints", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                    var intCost = Convert.ToInt32(cost);
                    if (!Convert.ToBoolean(ServerRewards?.Call("TakePoints", player.userID, intCost)))
                    {
                        Player.Message(player, lang.GetMessage("SRPointError", this, player.UserIDString), config.misc_settings.ChatID);
                        return;
                    }
                }
                else if (config.general_settings.respec_currency.Equals("custom", StringComparison.OrdinalIgnoreCase))
                {
                    var found = 0;
                    foreach (var item in player.inventory.AllItems())
                    {
                        if (item.skin == config.general_settings.respec_currency_custom.skin && item.info.shortname == config.general_settings.respec_currency_custom.shortname) found += item.amount;
                        if (found > cost) break;
                    }
                    if (found < cost)
                    {
                        Player.Message(player, string.Format(lang.GetMessage("RespecNoCustom", this, player.UserIDString), config.general_settings.respec_currency_custom.displayName), config.misc_settings.ChatID);
                        return;
                    }
                    found = 0;
                    foreach (var item in player.inventory.AllItems())
                    {
                        if (item.skin != config.general_settings.respec_currency_custom.skin || item.info.shortname != config.general_settings.respec_currency_custom.shortname) continue;
                        if (item.amount + found == cost)
                        {
                            item.Remove();
                            break;
                        }
                        if (item.amount + found < cost)
                        {
                            found += item.amount;
                            item.Remove();
                        }
                        else
                        {
                            item.UseItem(Convert.ToInt32(cost) - found);
                            break;
                        }
                    }
                }
            }
            var tree = arg.Args[1];
            var name = "";
            if (arg.Args.Length > 2)
            {
                name = string.Join(" ", arg.Args.Skip(2));
            }
            if (string.IsNullOrEmpty(name)) name = null;

            if (config.general_settings.respec_multiplier > 0)
            {
                PlayerInfo pi;
                if (pcdData.pEntity.TryGetValue(player.userID, out pi))
                {
                    pi.respec_multiplier += config.general_settings.respec_multiplier;
                    if (config.general_settings.respec_multiplier_max > 0 && pi.respec_multiplier > config.general_settings.respec_multiplier_max) pi.respec_multiplier = config.general_settings.respec_multiplier_max;
                }
            }

            RespecPlayer(player);
            SendSkillTreeMenu(player, tree, name);
            Player.Message(player, string.Format(lang.GetMessage("PaidRespec", this, player.UserIDString), cost), config.misc_settings.ChatID);
        }

        #endregion

        #region Chat commands        

        const string perm_admin = "skilltree.admin";
        const string perm_no_scoreboard = "skilltree.noscoreboard";

        [ChatCommand("resetxpbars")]
        void ResetXPBars(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            foreach (var kvp in pcdData.pEntity)
            {
                kvp.Value.xp_hud_pos = config.general_settings.pump_bar_settings.offset_default;
            }

            foreach (var p in BasePlayer.activePlayerList)
            {
                PlayerInfo pi;
                if (!pcdData.pEntity.TryGetValue(p.userID, out pi)) continue;
                UpdateXP(p, pi);
            }

            Player.Message(player, "Reset all xp bars to default settings.", config.misc_settings.ChatID);
        }

        [ChatCommand("resetxpbar")]
        void ResetXPBar(BasePlayer player)
        {
            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi))
            {
                Player.Message(player, "No data detected. Please reconnect to the server.", config.misc_settings.ChatID);
                return;
            }

            pi.xp_hud_pos = config.general_settings.pump_bar_settings.offset_default;
            UpdateXP(player, pi);

            Player.Message(player, "Your xp bar has been reset to default settings.", config.misc_settings.ChatID);
        }

        [ChatCommand("resetallresteddata")]
        void ResetAllRestedData(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            foreach (var kvp in pcdData.pEntity)
            {
                kvp.Value.xp_bonus_pool = 0;
                kvp.Value.logged_off = DateTime.Now;
            }
        }

        [ChatCommand("movebar")]
        void MoveBar(BasePlayer player)
        {
            if (!config.xp_settings.allow_move_xp_bar)
            {
                Player.Message(player, lang.GetMessage("MoveXPDisabled", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            SendXPBarMoverMenu(player);
        }

        void SendMenuCMD(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.chat"))
            {
                Player.Message(player, lang.GetMessage("NoPermsChat", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            SkillTreeBackPanel(player);
            NavigationMenu(player);
            SendSkillTreeMenu(player);
        }

        [ChatCommand("togglebc")]
        void ToggleBetterChat(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - ToggleBetterChat. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (playerData.better_chat_enabled)
            {
                Player.Message(player, lang.GetMessage("BCToggleOff", this, player.UserIDString), config.misc_settings.ChatID);
                playerData.better_chat_enabled = false;
            }

            else
            {
                Player.Message(player, lang.GetMessage("BCToggleOff", this, player.UserIDString), config.misc_settings.ChatID);
                playerData.better_chat_enabled = true;
            }
        }

        [ChatCommand("togglexpdrops")]
        void ToggleXPDrops(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - ToggleXPDrops. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (playerData.xp_drops) playerData.xp_drops = false;
            else playerData.xp_drops = true;
        }

        [ChatCommand("togglexphud")]
        void ToggleXPHud(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - ToggleXPHud. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (playerData.xp_hud)
            {
                playerData.xp_hud = false;
                CuiHelper.DestroyUi(player, "SkillTreeXPBar");
            }
            else
            {
                UpdateXP(player, playerData);
                playerData.xp_hud = true;
            }
        }

        [ChatCommand("sttogglenotifications")]
        void ToggleNotifications(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - ToggleNotifications. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (playerData.notifications)
            {
                Player.Message(player, lang.GetMessage("notificationsOff", this, player.UserIDString), config.misc_settings.ChatID);
                playerData.notifications = false;
            }

            else
            {
                Player.Message(player, lang.GetMessage("notificationsOn", this, player.UserIDString), config.misc_settings.ChatID);
                playerData.notifications = true;
            }

        }

        [ConsoleCommand("givexp")]
        void GiveXPConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                arg.ReplyWith(lang.GetMessage("GiveXPUsage", this, player?.UserIDString ?? null));
                return;
            }
            if (!arg.Args.Last().IsNumeric())
            {
                arg.ReplyWith(lang.GetMessage("XPLastArg", this, player?.UserIDString ?? null));
                return;
            }
            var xp = Convert.ToDouble(arg.Args.Last());
            var name = String.Join(" ", arg.Args.Take(arg.Args.Length - 1));
            var target = name.IsNumeric() ? FindPlayerByID(name, player ?? null) : FindPlayerByName(name, player ?? null);
            if (target == null) return;
            AwardXP(target, xp, null, false, false, "console");
            PrintToChat(target, string.Format(lang.GetMessage("GaveXP", this, target.UserIDString), xp, player != null ? player.displayName : "Console"));
            arg.ReplyWith(string.Format(lang.GetMessage("ReceivedXP", this), target.displayName, xp));
        }

        [ChatCommand("givexp")]
        void GiveXPCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;
            if (args.Length == 0)
            {
                Player.Message(player, lang.GetMessage("GiveXPUsage", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (!args.Last().IsNumeric())
            {
                Player.Message(player, lang.GetMessage("XPLastArg", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            var xp = Convert.ToDouble(args.Last());
            var name = String.Join(" ", args.Take(args.Length - 1));
            var target = FindPlayerByName(name, player);
            if (target == null) return;
            AwardXP(target, xp, null, false, false, "chat command");
            PrintToChat(target, string.Format(lang.GetMessage("GaveXP", this, target.UserIDString), xp, player.displayName));
            Player.Message(player, string.Format(lang.GetMessage("ReceivedXP", this, player.UserIDString), target.displayName, xp), config.misc_settings.ChatID);
        }

        [ConsoleCommand("givesp")]
        void GiveSkillPointsConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;

            if (arg.Args.IsNullOrEmpty() || arg.Args.Length < 2 || !arg.Args.Last().IsNumeric())
            {
                arg.ReplyWith(lang.GetMessage("GiveSPUsage", this));
                return;
            }
            var amount = Convert.ToInt32(arg.Args.Last());
            var name = String.Join(" ", arg.Args.Take(arg.Args.Length - 1));
            var target = name.IsNumeric() ? FindPlayerByID(name, player ?? null) : FindPlayerByName(name, player ?? null);
            if (target == null) return;
            GiveSkillPoints(target, amount);
            arg.ReplyWith(string.Format(lang.GetMessage("GaveSP", this), amount, target.displayName));
        }


        [ChatCommand("givesp")]
        void GiveSkillPoints(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;
            if (args.Length < 2 || !args.Last().IsNumeric())
            {
                Player.Message(player, lang.GetMessage("GiveSPUsage", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            var amount = Convert.ToInt32(args.Last());
            var target = FindPlayerByName(String.Join(" ", args.Take(args.Length - 1)), player);
            if (target == null) return;

            GiveSkillPoints(target, amount);
            Player.Message(player, string.Format(lang.GetMessage("GaveSP", this, player.UserIDString), amount, target.displayName), config.misc_settings.ChatID);
        }

        [ConsoleCommand("resetdata")]
        void ResetXPConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                arg.ReplyWith(lang.GetMessage("ResetXPUsage", this));
                return;
            }
            var name = String.Join(" ", arg.Args);
            var target = name.IsNumeric() ? FindPlayerByID(name, player ?? null) : FindPlayerByName(name, player ?? null);
            if (target == null) return;
            DoClear(target);
            LoggingOff(target);
            PlayerInfo pi;
            if (pcdData.pEntity.TryGetValue(target.userID, out pi))
            {
                RunResetCommands(target.UserIDString, Math.Max(pi.current_level, pi.achieved_level));
                pcdData.pEntity.Remove(target.userID);
            }

            if (TreeData.ContainsKey(target.userID)) TreeData.Remove(target.userID);
            buffDetails.Remove(target.userID);
            HandleNewConnection(target);
            arg.ReplyWith(string.Format(lang.GetMessage("ResetData", this), target.displayName));
            LoadBuffs();
        }

        [ChatCommand("resetdata")]
        void ResetXP(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;
            if (args.Length == 0)
            {
                Player.Message(player, lang.GetMessage("ResetXPUsage", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            var target = FindPlayerByName(string.Join(" ", args), player);
            if (target == null) return;
            DoClear(target);
            LoggingOff(target);
            PlayerInfo pi;
            if (pcdData.pEntity.TryGetValue(target.userID, out pi))
            {
                RunResetCommands(target.UserIDString, Math.Max(pi.current_level, pi.achieved_level));
                pcdData.pEntity.Remove(target.userID);
            }
            if (TreeData.ContainsKey(target.userID)) TreeData.Remove(target.userID);
            buffDetails.Remove(target.userID);
            HandleNewConnection(target);
            Player.Message(player, string.Format(lang.GetMessage("ResetData", this, player.UserIDString), target.displayName), config.misc_settings.ChatID);
            LoadBuffs();
        }

        [ConsoleCommand("stresetalldata")]
        void ResetAllDataConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;
            foreach (var id in BasePlayer.allPlayerList)
            {
                RemovePerms(id.UserIDString);
                PlayerInfo pi;
                if (pcdData.pEntity.TryGetValue(id.userID, out pi))
                {
                    RunResetCommands(id.UserIDString, Math.Max(pi.current_level, pi.achieved_level));
                }
            }
            buffDetails.Clear();
            pcdData.pEntity.Clear();
            TreeData.Clear();
            arg.ReplyWith("Reset all data.");
            if (BasePlayer.activePlayerList != null && BasePlayer.activePlayerList.Count > 0)
            {
                foreach (var p in BasePlayer.activePlayerList)
                {
                    HandleNewConnection(p);
                }
            }
            SaveData();
            LoadBuffs();
        }

        [ChatCommand("stresetalldata")]
        void ResetAllData(BasePlayer player = null)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;
            foreach (var id in BasePlayer.allPlayerList)
            {
                RemovePerms(id.UserIDString);
                PlayerInfo pi;
                if (pcdData.pEntity.TryGetValue(id.userID, out pi))
                {
                    RunResetCommands(id.UserIDString, Math.Max(pi.current_level, pi.achieved_level));
                }
            }
            buffDetails.Clear();
            pcdData.pEntity.Clear();
            TreeData.Clear();
            if (player != null) Player.Message(player, "Reset all data.", config.misc_settings.ChatID);
            if (BasePlayer.activePlayerList != null && BasePlayer.activePlayerList.Count > 0)
            {
                foreach (var p in BasePlayer.activePlayerList)
                {
                    HandleNewConnection(p);
                }
            }
            SaveData();
            LoadBuffs();
        }

        [ConsoleCommand("strespecplayer")]
        void ResetSkillsConsoleSingle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;

            if (pcdData.pEntity.Count == 0)
            {
                arg.ReplyWith(lang.GetMessage("NoPlayersSetup", this));
                return;
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                arg.ReplyWith("Usage: strespecplayer <target name/ID>");
                return;
            }

            var target = BasePlayer.Find(arg.GetString(0));

            if (target != null)
            {
                RespecPlayer(target);
                arg.ReplyWith($"Reset skill points for {target.displayName}");
                if (target.IsConnected) Player.Message(target, "Your skill points were reset.", config.misc_settings.ChatID);
            }
            else
            {
                var ID = Convert.ToUInt64(string.Join(" ", arg.Args));
                if (!ID.IsSteamId())
                {
                    arg.ReplyWith($"{ID} is not a valid Steam ID");
                    return;
                }
                foreach (var p in BasePlayer.activePlayerList)
                {
                    if (p.userID == ID)
                    {
                        RespecPlayer(target);
                        arg.ReplyWith($"Reset skill points for {target.displayName}");
                        return;
                    }
                }
                buffDetails.Remove(ID);
                TreeData.Remove(ID);

                foreach (KeyValuePair<ulong, PlayerInfo> kvp in pcdData.pEntity)
                {
                    if (kvp.Key == ID)
                    {
                        int pointCount = 0;
                        foreach (var buff in kvp.Value.buff_values)
                        {
                            pointCount += buff.Value;
                        }
                        kvp.Value.available_points += pointCount;
                        if (config.general_settings.points_per_level * kvp.Value.current_level > kvp.Value.available_points) kvp.Value.available_points = config.general_settings.points_per_level * kvp.Value.current_level;
                        kvp.Value.buff_values.Clear();
                        RemovePerms(kvp.Key.ToString());
                        //kvp.Value.available_points = config.general_settings.points_per_level * kvp.Value.current_level;
                        Puts($"Finished respeccing data for {ID}");
                        return;
                    }
                }
            }

        }

        [ConsoleCommand("strespecallplayers")]
        void ResetSkillsCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;

            ResetSkills(player);
            arg.ReplyWith("Respecced all players.");
        }

        void ResetSkills(BasePlayer player = null)
        {
            if (pcdData.pEntity.Count == 0)
            {
                if (player != null) Player.Message(player, lang.GetMessage("NoPlayersSetup", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (BasePlayer.activePlayerList != null && BasePlayer.activePlayerList.Count > 0)
            {
                foreach (var p in BasePlayer.activePlayerList)
                {
                    DoClear(p);
                    LoggingOff(p);
                }
            }
            buffDetails.Clear();
            TreeData.Clear();
            foreach (KeyValuePair<ulong, PlayerInfo> kvp in pcdData.pEntity)
            {
                int pointCount = 0;
                foreach (var buff in kvp.Value.buff_values)
                {
                    pointCount += buff.Value;
                }
                kvp.Value.available_points += pointCount;
                if (config.general_settings.points_per_level * kvp.Value.current_level > kvp.Value.available_points) kvp.Value.available_points = config.general_settings.points_per_level * kvp.Value.current_level;
                kvp.Value.buff_values.Clear();
                kvp.Value.ultimate_settings.Clear();
                RemovePerms(kvp.Key.ToString());
                //kvp.Value.available_points = config.general_settings.points_per_level * kvp.Value.current_level;
            }

            if (BasePlayer.activePlayerList != null && BasePlayer.activePlayerList.Count > 0)
            {
                foreach (var p in BasePlayer.activePlayerList)
                {
                    HandleNewConnection(p);
                    Player.Message(p, lang.GetMessage("PointsRefunded", this, p.UserIDString), config.misc_settings.ChatID);
                }
            }
            if (player != null) Player.Message(player, lang.GetMessage("PointsRefundedAll", this, player.UserIDString), config.misc_settings.ChatID);

            SaveData();
            LoadBuffs();
        }

        #endregion

        #region API   

        object ELOnModifyBoatSpeed(BasePlayer player, MotorRowboat boat)
        {
            if (Boats.ContainsKey(boat.net.ID.Value))
            {
                return true;
            }
            return null;
        }

        void EMOnEventJoined(BasePlayer player, string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            switch (eventName.ToUpper())
            {
                case "SURVIVALARENA":
                    if (!config.thirdPartyPluginSettings.survivalArenaSettings.disable_skinning_ultimate_buff_on_join) return;
                    break;

                case "PAINTBALL":
                    if (!config.thirdPartyPluginSettings.paintballSettings.disable_skinning_ultimate_buff_on_join) return;
                    break;

                default: return;
            }
            RemoveAnimalBuff(player, true);
        }

        [HookMethod("GetLevelExp")]
        public double GetLevelExp(int level)
        {
            return config.level.GetLevelStartXP(level);
        }

        [HookMethod("GetPlayerLevel")]
        public int GetPlayerLevel(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return 0;
            return playerData.current_level;
        }

        [HookMethod("ForceDropPouch")]
        public void ForceDropPouch(BasePlayer player, bool bypassPerm)
        {
            if (!bypassPerm && (permission.UserHasPermission(player.UserIDString, "skilltree.bag.keepondeath") || Interface.CallHook("STOnPouchDrop", player) != null)) return;

            PlayerInfo pi;
            if (pcdData.pEntity.TryGetValue(player.userID, out pi) && pi.pouch_items != null && pi.pouch_items.Count > 0)
            {
                var bag = GenerateBag(player, 42);
                if (bag != null && bag.inventory?.itemList != null && bag.inventory.itemList.Count > 0)
                {
                    var pos = player.transform.position;
                    var rot = player.transform.rotation;
                    timer.Once(0.1f, () =>
                    {
                        bag.inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", pos, rot, 0);
                        pi.pouch_items.Clear();
                        containers.Remove(bag.inventory.uid.Value);
                        bag.KillMessage();
                    });
                }
            }
        }

        Dictionary<YieldTypes, float> BaseYieldOverrides = new Dictionary<YieldTypes, float>();

        [HookMethod("SetSkillTreeYields")]
        public void SetSkillTreeYields(Dictionary<int, float> dict)
        {
            foreach (var kvp in dict)
            {
                if (!Enum.IsDefined(typeof(YieldTypes), kvp.Key))
                {
                    Puts($"Attempted to define a yield type that is out of range: {kvp.Key}.");
                    return;
                }
                AssignYield((YieldTypes)kvp.Key, kvp.Value);
            }
        }

        [HookMethod("SetSkillTreeYield")]
        public void SetSkillTreeYield(int type, float multiplier)
        {
            if (!Enum.IsDefined(typeof(YieldTypes), type))
            {
                Puts($"Attempted to define a yield type that is out of range: {type}.");
                return;
            }

            AssignYield((YieldTypes)type, multiplier);
        }

        void AssignYield(YieldTypes type, float modifier)
        {
            if (modifier == 1) BaseYieldOverrides.Remove(type);
            else BaseYieldOverrides[type] = modifier;
        }

        int GetMultipliedItemAmount(Item item)
        {
            return GetMultipliedItemAmount(item.info.shortname, item.amount);
        }

        int GetMultipliedItemAmount(string shortname, float amount)
        {
            if (!config.base_yield_settings.adjust_base_yield) return Mathf.RoundToInt(amount);
            YieldTypes yieldType;
            switch (shortname)
            {
                case "wood":
                    yieldType = YieldTypes.Wood;
                    break;

                case "stones":
                    yieldType = YieldTypes.Stone;
                    break;

                case "metal.ore":
                    yieldType = YieldTypes.Metal;
                    break;

                case "sulfur.ore":
                    yieldType = YieldTypes.Sulfur;
                    break;

                case "corn":
                    yieldType = YieldTypes.Corn;
                    break;

                case "potato":
                    yieldType = YieldTypes.Potato;
                    break;

                case "pumpkin":
                    yieldType = YieldTypes.Pumpkin;
                    break;

                case "cloth":
                    yieldType = YieldTypes.Cloth;
                    break;

                case "diesel_barrel":
                    yieldType = YieldTypes.Diesel;
                    break;

                case "fat.animal":
                    yieldType = YieldTypes.AnimalFat;
                    break;

                case "bone.fragments":
                    yieldType = YieldTypes.Bones;
                    break;

                case "leather":
                    yieldType = YieldTypes.Leather;
                    break;

                case "fish.anchovy":
                case "fish.catfish":
                case "fish.herring":
                case "fish.minnows":
                case "fish.orangeroughy":
                case "fish.salmon":
                case "fish.sardine":
                case "fish.smallshark":
                case "fish.troutsmall":
                case "fish.yellowperch":
                    yieldType = YieldTypes.Fish;
                    break;

                case "seed.black.berry":
                case "seed.blue.berry":
                case "seed.green.berry":
                case "seed.red.berry":
                case "seed.white.berry":
                case "seed.yellow.berry":
                case "seed.corn":
                case "seed.hemp":
                case "seed.potato":
                case "seed.pumpkin":
                    yieldType = YieldTypes.Seed;
                    break;

                case "mushroom":
                    yieldType = YieldTypes.Mushroom;
                    break;

                case "black.berry":
                case "blue.berry":
                case "green.berry":
                case "red.berry":
                case "white.berry":
                case "yellow.berry":
                    yieldType = YieldTypes.Berry;
                    break;

                default: return Mathf.RoundToInt(amount);
            }

            float multiplier;
            if (!BaseYieldOverrides.TryGetValue(yieldType, out multiplier) || multiplier == 1) return Convert.ToInt32(amount);

            var rounded = Mathf.RoundToInt(amount);
            return Convert.ToInt32(amount * multiplier);
        }

        object QuickSortExcluded(BasePlayer player, BaseEntity entity)
        {
            if (entity != null && entity.net != null && containers.ContainsKey(entity.net.ID.Value)) return true;
            return null;
        }

        void OnMealConsumed(BasePlayer player, Item item, int buff_duration)
        {
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Rationer) && RollSuccessful(bd.buff_values[Buff.Rationer]))
            {
                var refunded_item = ItemManager.CreateByName(item.info.shortname, 1, item.skin);
                if (item.name != null) refunded_item.name = item.name;
                GiveItem(player, refunded_item);
                if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("Rationed", this, player.UserIDString), item.name ?? item.info.displayName.english), config.misc_settings.ChatID);
            }
        }

        object RecipeCanModifyHorse(RidableHorse horse)
        {
            if (HorseStats.ContainsKey(horse.net.ID.Value)) return true;
            else return null;
        }

        object ELCanModifyHorse(RidableHorse horse, float value)
        {
            if (horse == null) return null;
            var driver = horse.GetDriver();
            if (driver == null) return null;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(driver.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {driver.displayName}[{driver.UserIDString}] - ELCanModifyHorse. [Online = {driver.IsConnected}]", this, true);
                if (driver.IsConnected) PrintToChat(driver, lang.GetMessage("FailReload", this, driver.UserIDString));
                return null;
            }
            if (!bd.buff_values.ContainsKey(Buff.Riding_Speed)) return null;
            else if (bd.buff_values[Buff.Riding_Speed] > value) return true;
            RestoreHorseStats(horse);
            return null;
        }

        void OnBotReSpawnNPCKilled(ScientistNPC npc, string profile, string group, HitInfo info)
        {
            if (npc == null || string.IsNullOrEmpty(profile) || info == null || info.InitiatorPlayer == null || info.InitiatorPlayer.IsNpc || !info.InitiatorPlayer.userID.IsSteamId()) return;
            if (!config.misc_settings.botRespawnSettings.botrespawn_profiles.ContainsKey(profile))
            {
                config.misc_settings.botRespawnSettings.botrespawn_profiles.Add(profile, config.xp_settings.xp_sources.default_botrespawn);
                SaveConfig();
            }
            double xp;
            if (config.misc_settings.botRespawnSettings.botrespawn_profiles.TryGetValue(profile, out xp))
            {
                AwardXP(info.InitiatorPlayer, xp, npc ?? null, false, false, "BotRespawnNPC");
            }
        }

        void HGWinner(BasePlayer player)
        {
            AwardXP(player, config.xp_settings.xp_sources.Win_HungerGames, null, false, false, "Hunger Games Win");
        }

        void SAWinner(BasePlayer player)
        {
            AwardXP(player, config.xp_settings.xp_sources.Win_ScubaArena, null, false, false, "Scuba Arena Win");
        }

        void SKWinner(BasePlayer player)
        {
            AwardXP(player, config.xp_settings.xp_sources.Win_Skirmish, null, false, false, "Skirmish Win");
        }

        void SKWinners(List<BasePlayer> players)
        {
            if (players == null || players.Count == 0) return;
            foreach (var player in players)
            {
                AwardXP(player, config.xp_settings.xp_sources.Win_Skirmish, null, false, false, "Skirmish Win");
            }
        }

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (npc.displayName.Equals(config.misc_settings.npc_name, StringComparison.OrdinalIgnoreCase)) SendSkillTreeMenu(player);
        }

        private object OnBetterChat(Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(config.betterchat_settings.better_title_format)) return null;
            var player = (IPlayer)data["Player"];
            if (permission.UserHasPermission(player.Id, "skilltree.notitles")) return null;
            var id = Convert.ToUInt64(player.Id);
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(id, out playerData))
            {
                SetupPlayer(id);
                playerData = pcdData.pEntity[id];
            }
            if (!playerData.better_chat_enabled) return null;
            var col = config.betterchat_settings.better_title_default_col;
            if (config.general_settings.max_player_level > 0 && playerData.current_level >= config.general_settings.max_player_level) col = config.betterchat_settings.better_title_max_col;
            var title = string.Format(config.betterchat_settings.better_title_format, col, config.general_settings.max_player_level > 0 && playerData.current_level > config.general_settings.max_player_level ? config.general_settings.max_player_level : playerData.current_level);

            var titles = (List<string>)data["Titles"];
            titles.Add(title);
            data["Titles"] = titles;
            return data;
        }

        void OnMealCrafed(BasePlayer player, string name, Dictionary<string, int> ingredients_list, bool isIngredient)
        {
            if ((!config.xp_settings.cooking_award_xp_ingredients && isIngredient) || config.xp_settings.cooking_black_list.Contains(name)) return;
            var ingredient_count = 0;
            foreach (var ingredient in ingredients_list)
            {
                ingredient_count += ingredient.Value;
            }
            AwardXP(player, ingredient_count * config.xp_settings.xp_sources.CookingMealXP, null, false, false, "Cooking Meal Crafted");
        }

        [HookMethod("ST_GetPlayerLevel")]
        public string[] ST_GetPlayerLevel(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return new string[] { "0", "0" };
            var level = playerData.current_level;
            if (config.general_settings.max_player_level > 0 && playerData.current_level > config.general_settings.max_player_level) level = config.general_settings.max_player_level;
            return new string[] { level.ToString(), playerData.xp.ToString() };

        }

        private void OnHarborEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.Harbor_Event_Winner);
        private void OnJunkyardEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.Junkyard_Event_Winner);
        private void OnSatDishEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.Satellite_Event_Winner);
        private void OnWaterEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.Water_Event_Winner);
        private void OnAirEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.Air_Event_Winner);

        private void OnArcticBaseEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.ArcticBaseEvent_Winner);
        private void OnGasStationEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.GasStationEvent_Winner);
        private void OnSputnikEventWin(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.SputnikEvent_Winner);
        private void OnShipwreckEventWin(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.ShipWreckEvent_Winner);

        private void OnPowerPlantEventWinner(ulong winnerId) => AwardEventWinnerXP(winnerId, config.xp_settings.xp_sources.PowerPlant_Event_Winner);
        private void OnArmoredTrainEventWin(ulong winnerID) => AwardEventWinnerXP(winnerID, config.xp_settings.xp_sources.Armored_Train_Winner);
        private void OnConvoyEventWin(ulong userId) => AwardEventWinnerXP(userId, config.xp_settings.xp_sources.Convoy_Winner);
        private void OnSurvivalArenaWin(BasePlayer player) => AwardEventWinnerXP(player.userID, config.xp_settings.xp_sources.SurvivalArena_Winner);
        private void OnBossKilled(ScientistNPC boss, BasePlayer attacker) => AwardXP(attacker, config.xp_settings.xp_sources.boss_monster, boss);

        void AwardEventWinnerXP(ulong winnerID, double xp)
        {
            var player = BasePlayer.activePlayerList.FirstOrDefault(x => x.userID == winnerID);
            if (player != null) AwardXP(player, xp, null, false, false, "Event");
        }

        private void OnRaidableBaseCompleted(Vector3 Location, int mode, bool allowPVP, string id, float spawnTime, float despawnTime, float loadTime, ulong ownerid, BasePlayer owner, List<BasePlayer> raiders)
        {
            double xp;
            switch (mode)
            {
                case 0:
                    xp = config.xp_settings.xp_sources.RaidableBaseCompletion_Easy;
                    break;

                case 1:
                    xp = config.xp_settings.xp_sources.RaidableBaseCompletion_Medium;
                    break;

                case 2:
                    xp = config.xp_settings.xp_sources.RaidableBaseCompletion_Hard;
                    break;

                case 3:
                    xp = config.xp_settings.xp_sources.RaidableBaseCompletion_Expert;
                    break;

                case 4:
                    xp = config.xp_settings.xp_sources.RaidableBaseCompletion_Nightmare;
                    break;

                default:
                    xp = 0;
                    break;
            }
            if (raiders != null)
            {
                foreach (var player in raiders)
                {
                    AwardXP(player, xp, "RaidableBases");
                }
            }
        }

        [HookMethod("GetExcessXP")]
        private double GetExcessXP(BasePlayer player)
        {
            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - GetExcessXP. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return 0;
            }
            return pi.xp - config.level.GetLevelStartXP(pi.current_level);
        }

        [HookMethod("RemoveXP")]
        private void RemoveXP(BasePlayer player, double value)
        {
            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - RemoveXP. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            var level_start_xp = config.level.GetLevelStartXP(pi.current_level);
            if ((pi.xp - value) - level_start_xp < 0.1) pi.xp = level_start_xp + 0.1;
            else pi.xp -= value;

            CheckLevel(player);
            UpdateXP(player, pi);
        }

        [HookMethod("STGetHorseStats")]
        private object STGetHorseStats(BasePlayer player, ulong id)
        {
            HorseInfo stats;
            if (HorseStats.TryGetValue(id, out stats)) return new object[] { stats.horse, stats.current_maxSpeed, stats.current_runSpeed, stats.current_trotSpeed, stats.current_turnSpeed, stats.current_walkSpeed };
            else return null;
        }

        // Do a check when the player mounts the horse, see if the buff is higher than the horses modified value, and if so, modify it with the new value. When getting off the horse, if Cooking or SkillTree still have the horse stored, restore horse stats to their value and let them .

        void OpenExtraPocketsPouch(BasePlayer player)
        {
            OpenBag(player);
        }



        #endregion

        #region XP HUD       

        string GetPumpBarString(BasePlayer player, double levelStartXP, double xp, double cap, bool isDebt = false)
        {
            string xpString;
            switch (config.general_settings.pump_bar_settings.pump_bar_formatting)
            {
                case 2:
                    xpString = string.Format(lang.GetMessage(isDebt ? "PumpBarXPTextDebt" : "PumpBarXPText", this, player.UserIDString), $"{Math.Round(xp - levelStartXP, config.xp_settings.xp_rounding):###,###,##0}", $"{Math.Round(cap - levelStartXP, config.xp_settings.xp_rounding):###,###,##0}");
                    break;

                case 3:
                    xpString = $"{Math.Round((cap - levelStartXP) - (xp - levelStartXP), config.xp_settings.xp_rounding):###,###,##0} ({Math.Round(((xp - levelStartXP) / (cap - levelStartXP)) * 100, config.xp_settings.xp_rounding)}%)";
                    break;

                default:
                    xpString = string.Format(lang.GetMessage(isDebt ? "PumpBarXPTextDebt" : "PumpBarXPText", this, player.UserIDString), $"{Math.Round(xp, config.xp_settings.xp_rounding):###,###,##0}", $"{Math.Round(cap, config.xp_settings.xp_rounding):###,###,##0}");
                    break;
            }
            return xpString;
        }

        void UpdateXP(BasePlayer player, PlayerInfo playerData = null)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skilltree.xp") || !config.general_settings.pump_bar_settings.enabled) return;
            if (playerData == null)
            {
                if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
                {
                    SetupPlayer(player.userID);
                    playerData = pcdData.pEntity[player.userID];
                }
            }

            double pump_length = playerData.xp_hud_pos.max_x - playerData.xp_hud_pos.min_x;

            var cap = config.level.GetLevelStartXP(playerData.current_level + 1);
            //var xpString = string.Format(lang.GetMessage("PumpBarXPText", this, player.UserIDString), Math.Round(playerData.xp, config.xp_settings.xp_rounding), Math.Round(cap, config.xp_settings.xp_rounding));
            var container = new CuiElementContainer();
            var LevelStartXP = config.level.GetLevelStartXP(playerData.current_level);
            var pump_value = playerData.xp_debt <= 0 ? (((playerData.xp - LevelStartXP) / (cap - LevelStartXP)) * pump_length) - 2.001 : pump_length;
            var xpString = playerData.xp_debt <= 0 ? GetPumpBarString(player, LevelStartXP, playerData.xp, cap) : GetPumpBarString(player, 0, playerData.xp_debt, playerData.xp_debt, true);
            if (pump_value > pump_length) pump_value = pump_length;
            if (playerData.xp_hud_pos.min_x == 0 && playerData.xp_hud_pos.min_y == 0 && playerData.xp_hud_pos.max_x == 0 && playerData.xp_hud_pos.max_y == 0) playerData.xp_hud_pos = config.general_settings.pump_bar_settings.offset_default;
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = $"0.4245283 0.4245283 0.4245283 0.5019608" },
                RectTransform = { AnchorMin = config.general_settings.pump_bar_settings.anchor_default.anchor_min, AnchorMax = config.general_settings.pump_bar_settings.anchor_default.anchor_max, OffsetMin = $"{playerData.xp_hud_pos.min_x} {playerData.xp_hud_pos.min_y}", OffsetMax = $"{playerData.xp_hud_pos.max_x} {playerData.xp_hud_pos.max_y}" }
            }, "Hud", "SkillTreeXPBar");

            //130.001 - default value
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = playerData.xp_debt > 0 ? config.general_settings.pump_bar_settings.pump_bar_colour_debt : config.general_settings.pump_bar_settings.pump_bar_colour },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "2.001 -9", OffsetMax = $"{pump_value} 9" }
            }, "SkillTreeXPBar", "SkillTreeXPBarPump");

            container.Add(new CuiElement
            {
                Name = "SkillTreeXPBarCounter",
                Parent = "SkillTreeXPBar",
                Components = {
                    new CuiTextComponent { Text = xpString, Font = config.general_settings.pump_bar_settings.pump_bar_font, FontSize = config.general_settings.pump_bar_settings.pump_bar_font_size, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7843137" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-32.125 -11", OffsetMax = "61.858 11" }
                }
            });

            if (playerData.xp_debt > 0)
            {
                container.Add(new CuiElement
                {
                    Name = "SkillTreeXPBarTitle",
                    Parent = "SkillTreeXPBar",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("PumpBarDebtTitleText", this, player.UserIDString), Font = config.general_settings.pump_bar_settings.pump_bar_font, FontSize = config.general_settings.pump_bar_settings.pump_bar_font_size + 2, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7843137" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.213 -11", OffsetMax = "-32.125 11" }
                }
                });
            }
            else
            {
                var level = config.general_settings.max_player_level > 0 && playerData.current_level > config.general_settings.max_player_level ? config.general_settings.max_player_level : playerData.current_level;
                container.Add(new CuiElement
                {
                    Name = "SkillTreeXPBarTitle",
                    Parent = "SkillTreeXPBar",
                    Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("PumpBarLevelText", this, player.UserIDString), level), Font = config.general_settings.pump_bar_settings.pump_bar_font, FontSize = config.general_settings.pump_bar_settings.pump_bar_font_size + 2, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7843137" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -11", OffsetMax = "34 11" }
                }
                });
            }

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "openskilltreemenufrompumpbar" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.209 -11", OffsetMax = "66.211 11" }
            }, "SkillTreeXPBar", "button");

            CuiHelper.DestroyUi(player, "SkillTreeXPBar");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("openskilltreemenufrompumpbar")]
        void OpenSkillTreeMenuFromPumpBar(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            SendMenuCMD(player);
        }


        #endregion

        #region XPBar Mover

        void SendXPBarMoverMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0.5377358 0.5377358 0.5377358 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50.003 -50", OffsetMax = "49.997 50" }
            }, "Overlay", "ui_mover");

            if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase))
            {
                container.Add(new CuiElement
                {
                    Name = "ui_mover_up_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_up_double") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 34", OffsetMax = "8 50" }
                }
                });
                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui u2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_up_double_img", "ui_mover_up_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_up_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_up_single") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 13", OffsetMax = "8 29" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui u1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_up_single_img", "ui_mover_up_single_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_down_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_down_double") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -50", OffsetMax = "8 -34" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui d2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_down_double_img", "ui_mover_down_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_down_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_down_single") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -29", OffsetMax = "8 -13" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui d1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_down_single_img", "ui_mover_down_single_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_left_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_left_double") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -8", OffsetMax = "-34 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui l2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_left_double_img", "ui_mover_left_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_left_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_left_single") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -8", OffsetMax = "-13 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui l1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_left_single_img", "ui_mover_left_single_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_right_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_right_double") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -8", OffsetMax = "50 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui r2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_right_double_img", "ui_mover_right_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_right_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "arrow_right_single") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "13 -8", OffsetMax = "29 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui r1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_right_single_img", "ui_mover_right_single_button");
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "ui_mover_up_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_up_double"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 34", OffsetMax = "8 50" }
                }
                });
                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui u2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_up_double_img", "ui_mover_up_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_up_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_up_single"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 13", OffsetMax = "8 29" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui u1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_up_single_img", "ui_mover_up_single_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_down_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_down_double"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -50", OffsetMax = "8 -34" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui d2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_down_double_img", "ui_mover_down_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_down_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_down_single"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -29", OffsetMax = "8 -13" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui d1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_down_single_img", "ui_mover_down_single_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_left_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_left_double"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -8", OffsetMax = "-34 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui l2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_left_double_img", "ui_mover_left_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_left_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_left_single"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -8", OffsetMax = "-13 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui l1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_left_single_img", "ui_mover_left_single_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_right_double_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_right_double"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -8", OffsetMax = "50 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui r2" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_right_double_img", "ui_mover_right_double_button");

                container.Add(new CuiElement
                {
                    Name = "ui_mover_right_single_img",
                    Parent = "ui_mover",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ArrowSkins["arrow_right_single"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "13 -8", OffsetMax = "29 8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 0.6885808 0 0.2745098", Command = $"movexpbarui r1" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8 -8", OffsetMax = "8 8" }
                }, "ui_mover_right_single_img", "ui_mover_right_single_button");
            }


            container.Add(new CuiButton
            {
                Button = { Color = "0.1860092 0.1868992 0.1886792 1", Command = "closeuimover" },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 34", OffsetMax = "50 50" }
            }, "ui_mover", "ui_mover_close");

            CuiHelper.DestroyUi(player, "ui_mover");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closeuimover")]
        void CloseUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "ui_mover");
        }

        [ConsoleCommand("movexpbarui")]
        void MoveXPBar(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            PlayerInfo playerData;

            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                SetupPlayer(player.userID);
                playerData = pcdData.pEntity[player.userID];
            }

            switch (arg.Args[0])
            {
                case "u1":
                    playerData.xp_hud_pos.min_y += 5;
                    playerData.xp_hud_pos.max_y += 5;
                    break;
                case "u2":
                    playerData.xp_hud_pos.min_y += 20;
                    playerData.xp_hud_pos.max_y += 20;
                    break;
                case "d1":
                    playerData.xp_hud_pos.min_y -= 5;
                    playerData.xp_hud_pos.max_y -= 5;
                    break;
                case "d2":
                    playerData.xp_hud_pos.min_y -= 20;
                    playerData.xp_hud_pos.max_y -= 20;
                    break;
                case "l1":
                    playerData.xp_hud_pos.min_x -= 5;
                    playerData.xp_hud_pos.max_x -= 5;
                    break;
                case "l2":
                    playerData.xp_hud_pos.min_x -= 20;
                    playerData.xp_hud_pos.max_x -= 20;
                    break;
                case "r1":
                    playerData.xp_hud_pos.min_x += 5;
                    playerData.xp_hud_pos.max_x += 5;
                    break;
                case "r2":
                    playerData.xp_hud_pos.min_x += 20;
                    playerData.xp_hud_pos.max_x += 20;
                    break;
            }
            UpdateXP(player, playerData);
        }

        #endregion

        #region Furnace speed

        Dictionary<BaseOven, float> ovens = new Dictionary<BaseOven, float>();

        void OnFuelConsume(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            float modifier;
            if (!ovens.TryGetValue(oven, out modifier) || !RollSuccessful(modifier)) return;
            List<Item> remove_items = new List<Item>();
            foreach (var item in oven.inventory.itemList.ToList())
            {
                var itemModCookable = item.info.GetComponent<ItemModCookable>();
                if (itemModCookable?.becomeOnCooked == null || item.temperature < itemModCookable.lowTemp || item.temperature > itemModCookable.highTemp || itemModCookable.cookTime < 0) continue;
                var itemToGive = ItemManager.Create(itemModCookable.becomeOnCooked, itemModCookable.amountOfBecome);
                if (!itemToGive.MoveToContainer(oven.inventory))
                    itemToGive.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
                if (item.amount == 1) item.Remove();
                else item.SplitItem(1).Remove();
            }
            foreach (var item in remove_items.ToList())
            {
                item.Remove();
            }
        }

        void OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            if (oven.temperature != BaseOven.TemperatureType.Smelting) return;
            // Checks if the oven is on when the toggle occurs, and if it is, we exit because its being turned off.
            if (oven.IsOn())
            {
                if (ovens.ContainsKey(oven)) ovens.Remove(oven);
                return;
            }
            // See if the player has the buff assigned.
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Smelt_Speed))
            {
                if (oven.inventory.itemList == null || oven.inventory.itemList.Count == 0) return;
                ovens.Remove(oven);
                ovens.Add(oven, bd.buff_values[Buff.Smelt_Speed]);
            }
        }

        #endregion

        #region Subscriptions

        public Dictionary<string, Subscription> subscriptions = new Dictionary<string, Subscription>()
        {
            [nameof(OnEntityTakeDamage)] = new Subscription(false, new List<Buff>() { Buff.Animal_Damage_Resist, Buff.Barrel_Smasher, Buff.Fall_Damage_Reduction, Buff.Fire_Damage_Reduction, Buff.Melee_Resist, Buff.No_Cold_Damage, Buff.PVP_Critical, Buff.PVP_Damage, Buff.PVP_Shield, Buff.Radiation_Reduction, Buff.Loot_Pickup, Buff.Human_NPC_Damage, Buff.Human_NPC_Defence, Buff.Animal_NPC_Damage, Buff.Vehicle_Ultimate, Buff.Scavengers_Ultimate, Buff.Combat_Ultimate, Buff.SharkResistance, Buff.UnderwaterDamageBonus, Buff.Skinning_Ultimate, Buff.Trap_Damage_Reduction, Buff.Trap_Damage_Increase, Buff.Personal_Explosive_Reduction, Buff.Extra_Scrap_Barrel, Buff.Component_Barrel, Buff.Electronic_Barrel }),
            [nameof(OnItemUse)] = new Subscription(false, new List<Buff>() { Buff.Rationer }),
            [nameof(OnPlayerRevive)] = new Subscription(false, new List<Buff>() { Buff.Reviver }),
            [nameof(OnPlayerHealthChange)] = new Subscription(false, new List<Buff>() { Buff.Double_Bandage_Heal }),
            [nameof(OnLoseCondition)] = new Subscription(false, new List<Buff>() { Buff.Woodcutting_Tool_Durability, Buff.Mining_Tool_Durability, Buff.Skinning_Tool_Durability, Buff.Primitive_Expert, Buff.Durability }),
            [nameof(OnWeaponFired)] = new Subscription(false, new List<Buff>() { Buff.Free_Bullet_Chance }),
            [nameof(OnRecyclerToggle)] = new Subscription(false, new List<Buff>() { Buff.Recycler_Speed }),
            [nameof(OnPlayerAddModifiers)] = new Subscription(false, new List<Buff>() { Buff.Extra_Food_Water, Buff.Iron_Stomach, Buff.Extended_Tea_Duration }),
            [nameof(OnPlayerWound)] = new Subscription(false, new List<Buff>() { Buff.Wounded_Resist }),
            [nameof(OnEntityMounted)] = new Subscription(false, new List<Buff>() { Buff.Riding_Speed, Buff.Heli_Fuel_Rate, Buff.Boat_Fuel_Rate, Buff.Heli_Speed }),
            [nameof(OnEntityDismounted)] = new Subscription(false, new List<Buff>() { Buff.Boat_Fuel_Rate, Buff.Heli_Fuel_Rate, Buff.Riding_Speed, Buff.Boat_Speed, Buff.Heli_Speed, Buff.Boat_Speed }),
            [nameof(OnHammerHit)] = new Subscription(false, new List<Buff>() { Buff.Vehicle_Mechanic }),
            [nameof(OnPayForUpgrade)] = new Subscription(false, new List<Buff>() { Buff.Upgrade_Refund }),
            [nameof(OnPlayerInput)] = new Subscription(false, new List<Buff>() { Buff.Boat_Speed }),
            [nameof(OnResearchCostDetermine)] = new Subscription(false, new List<Buff>() { Buff.Research_Refund }),
            [nameof(CanLootEntity)] = new Subscription(false, new List<Buff>() { Buff.Component_Chest, Buff.Electronic_Chest, Buff.Extra_Scrap_Crate, Buff.DeepSeaLooter, Buff.Tea_Looter }),
            [nameof(OnPlayerRespawned)] = new Subscription(false, new List<Buff>() { Buff.Medical_Ultimate, Buff.Spawn_Health }),
            [nameof(OnItemRepair)] = new Subscription(false, new List<Buff>() { Buff.MaxRepair }),
            [nameof(CanUseLockedEntity)] = new Subscription(false, new List<Buff>() { Buff.Lock_Picker }),
            [nameof(OnTreeMarkerHit)] = new Subscription(false, new List<Buff>() { Buff.Woodcutting_Hotspot }),
            [nameof(OnMeleeAttack)] = new Subscription(false, new List<Buff>() { Buff.Mining_Hotspot }),
            [nameof(OnWeaponReload)] = new Subscription(false, new List<Buff>() { Buff.Extended_Mag }),
            [nameof(OnWeaponModChange)] = new Subscription(false, new List<Buff>() { Buff.Extended_Mag }),
            [nameof(OnFishCatch)] = new Subscription(false, new List<Buff>() { Buff.Rod_Tension_Bonus }),
            [nameof(OnFishingStopped)] = new Subscription(false, new List<Buff>() { Buff.Rod_Tension_Bonus })
        };


        public class Subscription
        {
            public bool isSubscribed;
            public List<Buff> buffs;
            public List<ulong> subscribers = new List<ulong>();
            public Subscription(bool isSubscribed, List<Buff> buffs)
            {
                this.isSubscribed = isSubscribed;
                this.buffs = buffs;
            }
            public void Subscribed()
            {
                this.isSubscribed = true;
                if (this.subscribers == null) this.subscribers = new List<ulong>();
            }
            public void Unsubscribed()
            {
                this.isSubscribed = false;
                this.subscribers.Clear();
            }
            public bool Required()
            {
                if (this.subscribers != null && this.subscribers.Count > 0) return true;
                return false;
            }
            public void AddPlayer(ulong id)
            {
                if (this.subscribers == null) this.subscribers = new List<ulong>();
                if (!this.subscribers.Contains(id)) this.subscribers.Add(id);
            }
            public void RemovePlayer(ulong id)
            {
                if (this.subscribers != null)
                {
                    this.subscribers.Remove(id);
                }
            }
        }

        void RemoveFromAllBuffs(ulong id)
        {
            foreach (var sub in subscriptions)
            {
                if (sub.Key == nameof(OnPlayerInput) && !config.chat_commands.use_input_key_boat) continue;
                sub.Value.subscribers.Remove(id);
                if (sub.Value.isSubscribed && !sub.Value.Required())
                {
                    OnUnsubscribe(sub.Key);
                    sub.Value.Unsubscribed();
                    Unsubscribe(sub.Key);
                }
            }
        }

        void OnUnsubscribe(string hook)
        {
            switch (hook)
            {
                case nameof(OnWeaponReload):
                    ResetWeaponCapacities(false, config.buff_settings.force_unload_extended_mag_weapons_unload);
                    ModifiedWeapons.Clear();
                    break;
            }
        }

        void AddBuffs(ulong id, Buff buff)
        {
            foreach (var sub in subscriptions)
            {
                if (sub.Key == nameof(OnPlayerInput) && !config.chat_commands.use_input_key_boat) continue;
                if (!sub.Value.buffs.Contains(buff)) continue;
                sub.Value.AddPlayer(id);
                if (!sub.Value.isSubscribed)
                {
                    sub.Value.Subscribed();
                    Subscribe(sub.Key);
                }
            }
        }

        void LoadBuffs()
        {
            if (config.buff_settings.boat_turbo_on_mount && !subscriptions[nameof(OnEntityMounted)].buffs.Contains(Buff.Boat_Speed)) subscriptions[nameof(OnEntityMounted)].buffs.Add(Buff.Boat_Speed);
            if (BasePlayer.activePlayerList == null || BasePlayer.activePlayerList.Count == 0 || buffDetails == null || buffDetails.Count == 0)
            {
                foreach (var sub in subscriptions)
                {
                    if (sub.Key == nameof(OnPlayerInput) && !config.chat_commands.use_input_key_boat) continue;
                    Unsubscribe(sub.Key);
                    sub.Value.Unsubscribed();
                }
            }
            else
            {
                foreach (var sub in subscriptions)
                {
                    if (sub.Key == nameof(OnPlayerInput) && !config.chat_commands.use_input_key_boat)
                    {
                        Unsubscribe(nameof(OnPlayerInput));
                        continue;
                    }
                    foreach (var player in buffDetails)
                    {
                        if (player.Value.buff_values == null || player.Value.buff_values.Count == 0) continue;
                        foreach (var buff in player.Value.buff_values)
                        {
                            if (sub.Value.buffs.Contains(buff.Key))
                            {
                                sub.Value.AddPlayer(player.Key);
                                sub.Value.Subscribed();
                            }
                        }
                    }
                    if (!sub.Value.Required())
                    {
                        sub.Value.Unsubscribed();
                        Unsubscribe(sub.Key);
                    }
                }
            }
        }

        #endregion

        #region Player Menu

        private void SkillTree_PlayerMenu(BasePlayer player)
        {
            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9803922" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0.025 -0.331", OffsetMax = "0.325 0.339" }
            }, "Overlay", "SkillTree_PlayerMenu");

            container.Add(new CuiElement
            {
                Name = "SkillTree_PlayerMenu_Title",
                Parent = "SkillTree_PlayerMenu",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIPlayerSettings", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 190.4", OffsetMax = "-180.18 250.4" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 132.4", OffsetMax = "-180.18 190.4" }
            }, "SkillTree_PlayerMenu", "SkillTree_PlayerMenu_tgl_pnl_bk_1");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2264151 0.2264151 0.2264151 0.9607843" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155.53 -27", OffsetMax = "155.53 27" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_1", "SkillTree_PlayerMenu_tgl_pnl_ft_1");

            container.Add(new CuiElement
            {
                Name = "SkillTree_PlayerMenu_tgl_des_1",
                Parent = "SkillTree_PlayerMenu_tgl_pnl_bk_1",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIToggleXP", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147 -26", OffsetMax = "52.24 26" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.53 -24", OffsetMax = "152.53 24" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_1", "SkillTree_PlayerMenu_tgl_bttn_pnl_1");

            var textCol = "0.0480598 0.6792453 0.1672014 1";
            if (!pi.xp_drops) textCol = "0.5943396 0.131764 0.1842591 1";

            container.Add(new CuiButton
            {
                Button = { Color = "0.1607843 0.1607843 0.1607843 1", Command = $"dotogglexpdrops" },
                Text = { Text = pi.xp_drops ? lang.GetMessage("ON", this, player.UserIDString) : lang.GetMessage("OFF", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = textCol },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68.53 -22", OffsetMax = "150.53 22" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_1", "SkillTree_PlayerMenu_tgl_bttn_1");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 54.4", OffsetMax = "-180.18 112.4" }
            }, "SkillTree_PlayerMenu", "SkillTree_PlayerMenu_tgl_pnl_bk_2");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2264151 0.2264151 0.2264151 0.9607843" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155.53 -27", OffsetMax = "155.53 27" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_2", "SkillTree_PlayerMenu_tgl_pnl_ft_2");

            container.Add(new CuiElement
            {
                Name = "SkillTree_PlayerMenu_tgl_des_2",
                Parent = "SkillTree_PlayerMenu_tgl_pnl_bk_2",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIToggleXPBar", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147 -26", OffsetMax = "52.24 26" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.53 -24", OffsetMax = "152.53 24" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_2", "SkillTree_PlayerMenu_tgl_bttn_pnl_2");

            textCol = "0.0480598 0.6792453 0.1672014 1";
            if (!pi.xp_hud) textCol = "0.5943396 0.131764 0.1842591 1";

            container.Add(new CuiButton
            {
                Button = { Color = "0.1607843 0.1607843 0.1607843 1", Command = "dotogglexphud" },
                Text = { Text = pi.xp_hud ? lang.GetMessage("ON", this, player.UserIDString) : lang.GetMessage("OFF", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = textCol },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68.53 -22", OffsetMax = "150.53 22" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_2", "SkillTree_PlayerMenu_tgl_bttn_2");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 -23.6", OffsetMax = "-180.18 34.4" }
            }, "SkillTree_PlayerMenu", "SkillTree_PlayerMenu_tgl_pnl_bk_3");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2264151 0.2264151 0.2264151 0.9607843" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155.53 -27", OffsetMax = "155.53 27" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_3", "SkillTree_PlayerMenu_tgl_pnl_ft_3");

            container.Add(new CuiElement
            {
                Name = "SkillTree_PlayerMenu_tgl_des_3",
                Parent = "SkillTree_PlayerMenu_tgl_pnl_bk_3",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("RepositionBar", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147 -26", OffsetMax = "52.24 26" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.53 -24", OffsetMax = "152.53 24" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_3", "SkillTree_PlayerMenu_tgl_bttn_pnl_3");

            container.Add(new CuiButton
            {
                Button = { Color = "0.1607843 0.1607843 0.1607843 1", Command = "strepositionhudfrommenu" },
                Text = { Text = lang.GetMessage("UIChange", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 0.7984455 0.3066038 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68.53 -22", OffsetMax = "150.53 22" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_3", "SkillTree_PlayerMenu_tgl_bttn_3");

            textCol = "0.0480598 0.6792453 0.1672014 1";
            if (!pi.extra_pockets_button) textCol = "0.5943396 0.131764 0.1842591 1";

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 -101.6", OffsetMax = "-180.18 -43.6" }
            }, "SkillTree_PlayerMenu", "SkillTree_PlayerMenu_tgl_pnl_bk_4");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2264151 0.2264151 0.2264151 0.9607843" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155.53 -27", OffsetMax = "155.53 27" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_4", "SkillTree_PlayerMenu_tgl_pnl_ft_4");

            container.Add(new CuiElement
            {
                Name = "SkillTree_PlayerMenu_tgl_des_4",
                Parent = "SkillTree_PlayerMenu_tgl_pnl_bk_4",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("ToggleBagButton", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147 -26", OffsetMax = "52.24 26" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.53 -24", OffsetMax = "152.53 24" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_4", "SkillTree_PlayerMenu_tgl_bttn_pnl_4");

            container.Add(new CuiButton
            {
                Button = { Color = "0.1607843 0.1607843 0.1607843 1", Command = "sttoggleextrapocketsbutton" },
                Text = { Text = pi.extra_pockets_button ? lang.GetMessage("ON", this, player.UserIDString) : lang.GetMessage("OFF", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = textCol },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68.53 -22", OffsetMax = "150.53 22" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_4", "SkillTree_PlayerMenu_tgl_bttn_4");

            textCol = "0.0480598 0.6792453 0.1672014 1";
            if (!pi.notifications) textCol = "0.5943396 0.131764 0.1842591 1";

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 -179.6", OffsetMax = "-180.18 -121.6" }
            }, "SkillTree_PlayerMenu", "SkillTree_PlayerMenu_tgl_pnl_bk_5");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2264151 0.2264151 0.2264151 0.9607843" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155.53 -27", OffsetMax = "155.53 27" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_5", "SkillTree_PlayerMenu_tgl_pnl_ft_5");

            container.Add(new CuiElement
            {
                Name = "SkillTree_PlayerMenu_tgl_des_5",
                Parent = "SkillTree_PlayerMenu_tgl_pnl_bk_5",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("ToggleNotifications", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147 -26", OffsetMax = "52.24 26" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.53 -24", OffsetMax = "152.53 24" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_5", "SkillTree_PlayerMenu_tgl_bttn_pnl_5");

            container.Add(new CuiButton
            {
                Button = { Color = "0.1607843 0.1607843 0.1607843 1", Command = "sttogglenotifications" },
                Text = { Text = pi.notifications ? lang.GetMessage("ON", this, player.UserIDString) : lang.GetMessage("OFF", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = textCol },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68.53 -22", OffsetMax = "150.53 22" }
            }, "SkillTree_PlayerMenu_tgl_pnl_bk_5", "SkillTree_PlayerMenu_tgl_bttn_5");


            container.Add(new CuiElement
            {
                Name = "SkillTree_PlayerMenu_close",
                Parent = "SkillTree_PlayerMenu",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIClose", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-366.71 -243", OffsetMax = "-308.71 -211" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"stcloseplayersettings" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
            }, "SkillTree_PlayerMenu_close", "SkillTree_PlayerMenu_close_button");

            CuiHelper.DestroyUi(player, "SkillTree_PlayerMenu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("sttoggleextrapocketsbutton")]
        void ToggleExtraPockets(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi))
            {
                SetupPlayer(player.userID);
                pi = pcdData.pEntity[player.userID];
            }
            if (pi.extra_pockets_button)
            {
                pi.extra_pockets_button = false;
                SkillTree_PlayerMenu(player);
                CuiHelper.DestroyUi(player, "ExtraPocketsButton");
            }
            else
            {
                if (buffDetails.ContainsKey(player.userID) && buffDetails[player.userID].buff_values.ContainsKey(Buff.ExtraPockets))
                {
                    SendExtraPocketsButton(player);
                }
                pi.extra_pockets_button = true;
                SkillTree_PlayerMenu(player);
            }
        }

        [ConsoleCommand("sttogglenotifications")]
        void ToggleNotifications(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            ToggleNotifications(player);
            SkillTree_PlayerMenu(player);
        }

        [ConsoleCommand("dotogglexpdrops")]
        void ToggleXPDropsFromMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            ToggleXPDrops(player);
            SkillTree_PlayerMenu(player);
        }

        [ConsoleCommand("dotogglexphud")]
        void ToggleXPHudFromMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            ToggleXPHud(player);
            SkillTree_PlayerMenu(player);
        }

        [ConsoleCommand("strepositionhudfrommenu")]
        void RepositionHudFromMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "SkillTree_PlayerMenu");
            CuiHelper.DestroyUi(player, "SkillTree");
            CuiHelper.DestroyUi(player, "SkillTreeBackPanel");
            CuiHelper.DestroyUi(player, "NavigationMenu");
            MoveBar(player);
        }

        [ConsoleCommand("stcloseplayersettings")]
        void ClosePlayerSettings(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "SkillTree_PlayerMenu");
        }

        #endregion

        #region Health Regen

        //private static Dictionary<BasePlayer, float> RegenAmount = new Dictionary<BasePlayer, float>();

        bool HasRegen(BasePlayer player)
        {
            return player.GetComponent<Regen>() != null;
        }

        void UpdateRegen(BasePlayer player, float value)
        {
            var gameObject = player.GetComponent<Regen>();
            if (gameObject == null) gameObject = player.gameObject.AddComponent<Regen>();
            gameObject.UpdateRegenRage(value);
            gameObject.name = "SkillTreeRegen";
        }

        static void DestroyRegen(BasePlayer player)
        {
            //if (RegenAmount.ContainsKey(player)) RegenAmount.Remove(player);
            var gameObject = player.GetComponent<Regen>();
            if (gameObject != null) UnityEngine.Object.DestroyImmediate(gameObject);
        }

        //public static Dictionary<ulong, float> took_damage = new Dictionary<ulong, float>();

        void AddRegenDelay(BasePlayer player)
        {
            if (config.buff_settings.health_regen_combat_delay <= 0) return;
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.ContainsKey(Buff.HealthRegen)) return;

            var gameObject = player.GetComponent<Regen>();
            if (gameObject == null) return;
            gameObject.AddDamageCooldown(config.buff_settings.health_regen_combat_delay);
        }

        public class Regen : MonoBehaviour
        {
            private BasePlayer player;
            private float regenDelay;
            private float _regenAmount;

            private float damageCooldownTime;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                regenDelay = Time.time + 1f;
                _regenAmount = 0;
            }

            public void FixedUpdate()
            {
                if (player == null || !player.IsConnected)
                {
                    Destroy(this);
                    return;
                }
                if (regenDelay < Time.time)
                {
                    regenDelay = Time.time + 1f;
                    if (damageCooldownTime > Time.time) return;
                    DoRegen();
                }
            }

            public void AddDamageCooldown(float time)
            {
                damageCooldownTime = Time.time + time;
            }

            public void UpdateRegenRage(float value)
            {
                _regenAmount = value;
            }

            public void DoRegen()
            {
                if (!player.IsAlive() || player.health == player.MaxHealth()) return;
                player.Heal(_regenAmount);
            }

            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
            }
        }

        #endregion

        #region AnimalTracker

        Dictionary<BasePlayer, float> track_delays = new Dictionary<BasePlayer, float>();

        void TrackAnimal(BasePlayer player)
        {
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd))
            {
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - TrackAnimal. [Online = {player.IsConnected}]", this, true);
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            if (!bd.buff_values.ContainsKey(Buff.AnimalTracker)) return;

            if (!track_delays.ContainsKey(player)) track_delays.Add(player, Time.time + config.buff_settings.track_delay);
            else if (track_delays[player] < Time.time) track_delays[player] = Time.time + config.buff_settings.track_delay;
            else
            {
                Player.Message(player, string.Format(lang.GetMessage("TrackWait", this, player.UserIDString), Math.Round(track_delays[player] - Time.time, 2)), config.misc_settings.ChatID);
                return;
            }

            var animals = FindEntitiesOfType<BaseAnimalNPC>(player.transform.position, 300f);
            BaseAnimalNPC animal = animals.Count > 0 ? animals.OrderBy(x => Vector3.Distance(x.transform.position, player.transform.position)).First() : null;

            if (animal == null)
            {
                Player.Message(player, lang.GetMessage("NoAnimals", this, player.UserIDString), config.misc_settings.ChatID);
                Pool.FreeList(ref animals);
                return;
            }

            var distance = Vector3.Distance(player.transform.position, animal.transform.position);
            string text;
            if (distance < 50) text = lang.GetMessage("TrackFresh", this, player.UserIDString);
            else if (distance < 100) text = lang.GetMessage("TrackOlder", this, player.UserIDString);
            else text = lang.GetMessage("TrackOldest", this, player.UserIDString);
            var direction = player.transform.position - animal.transform.position;
            direction.Normalize();
            Player.Message(player, string.Format(text, Direction(direction.ZX2D())), config.misc_settings.ChatID);
            Pool.FreeList(ref animals);
        }

        string Direction(Vector2 dir)
        {
            if (dir.x >= -1.0 && dir.x <= -0.8 && dir.y >= -0.5 && dir.y <= 0.5) return "North";
            if (dir.x >= -1.0 && dir.x <= -0.5 && dir.y >= 0.5 && dir.y <= 1.0) return "North-West";
            if (dir.x >= -0.5 && dir.x <= 0.5 && dir.y >= 0.8 && dir.y <= 1.0) return "West";
            if (dir.x >= 0.5 && dir.x <= 1.0 && dir.y >= 0.5 && dir.y <= 1.0) return "South-West";
            if (dir.x >= -0.5 && dir.x <= 0.5 && dir.y >= -1.0 && dir.y <= -0.8) return "East";
            if (dir.x >= -1.0 && dir.x <= -0.5 && dir.y >= -1.0 && dir.y <= -0.5) return "North-East";
            if (dir.x >= 0.5 && dir.x <= 1.0 && dir.y >= -1.0 && dir.y <= -0.5) return "South-East";
            if (dir.x >= 0.8 && dir.x <= 1.0 && dir.y >= -0.5 && dir.y <= 0.5) return "South";
            return null;
        }

        #endregion

        #region Extra Pockets

        public class ItemInfo
        {
            public string shortname;
            public ulong skin;
            public int amount;
            public float condition;
            public float maxCondition;
            public int ammo;
            public string ammotype;
            public int position;
            public int frequency;
            public Item.Flag flags;
            public KeyInfo instanceData;
            public class KeyInfo
            {
                public int dataInt;
                public int blueprintTarget;
                public int blueprintAmount;
            }
            public List<ItemInfo> item_contents;
            public string text;
            public string name;
        }

        Dictionary<ulong, float> bagCooldown = new Dictionary<ulong, float>();

        [ChatCommand("pouch")]
        void OpenBagCMD(BasePlayer player)
        {
            OpenBag(player);
        }

        [ConsoleCommand("pouch")]
        void OpenBagConsoleCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            OpenBag(player);
        }

        void OpenBag(BasePlayer player)
        {
            if (player.IsDead() || Interface.CallHook("STOnPouchOpen", player) != null) return;
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.ExtraPockets))
            {
                // Handle cooldown
                if (!bagCooldown.ContainsKey(player.userID)) bagCooldown.Add(player.userID, Time.time + config.buff_settings.bag_cooldown_time);
                else
                {
                    if (bagCooldown[player.userID] < Time.time) bagCooldown[player.userID] = Time.time + config.buff_settings.bag_cooldown_time;
                    else
                    {
                        Player.Message(player, string.Format(lang.GetMessage("BagCooldownMsg", this, player.UserIDString), Math.Round(bagCooldown[player.userID] - Time.time, 2)), config.misc_settings.ChatID);
                        return;
                    }
                }
                player.EndLooting();
                var bag = GenerateBag(player, Convert.ToInt32(bd.buff_values[Buff.ExtraPockets]));
                timer.Once(0.1f, () =>
                {
                    if (bag != null) bag.PlayerOpenLoot(player, "", false);
                    Interface.CallHook("STOnPouchOpened", player, bag);
                });
            }
            else
            {
                Player.Message(player, lang.GetMessage("NeedBagBuff", this, player.UserIDString), config.misc_settings.ChatID);
            }
        }

        bool FetchItems(BasePlayer player, ItemContainer container)
        {
            if (player.IsDead() || !player.IsConnected) return false;
            PlayerInfo playerData;
            if (pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                if (playerData.pouch_items == null || playerData.pouch_items.Count == 0) return true;
                foreach (var item in playerData.pouch_items)
                {
                    GetRestoreItem(player, container, item);
                }
                playerData.pouch_items.Clear();
            }

            return true;
        }

        Item GetRestoreItem(BasePlayer player, ItemContainer container, ItemInfo savedItem)
        {
            var item = ItemManager.CreateByName(savedItem.shortname, savedItem.amount, savedItem.skin);
            if (savedItem.name != null) item.name = savedItem.name;
            if (savedItem.text != null) item.text = savedItem.text;
            item.condition = savedItem.condition;
            item.maxCondition = savedItem.maxCondition;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                if (!string.IsNullOrEmpty(savedItem.ammotype))
                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(savedItem.ammotype);
                weapon.primaryMagazine.contents = savedItem.ammo;
            }
            FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
            if (flameThrower != null) flameThrower.ammo = savedItem.ammo;
            if (savedItem.instanceData != null)
            {
                item.instanceData = new ProtoBuf.Item.InstanceData();
                item.instanceData.ShouldPool = false;
                item.instanceData.dataInt = savedItem.instanceData.dataInt;
                item.instanceData.blueprintTarget = savedItem.instanceData.blueprintTarget;
                item.instanceData.blueprintAmount = savedItem.instanceData.blueprintAmount;
            }
            item.flags = savedItem.flags;
            if (savedItem.item_contents != null && savedItem.item_contents.Count > 0)
            {
                if (item.contents == null)
                {
                    item.contents = new ItemContainer();
                    item.contents.ServerInitialize(null, savedItem.item_contents.Count);
                    item.contents.GiveUID();
                    item.contents.parent = item;
                }
                savedItem.item_contents.RemoveAll(x => x.amount <= 0);

                foreach (var _item in savedItem.item_contents)
                {
                    GetRestoreItem(player, item.contents, _item);
                }
            }
            if (!item.MoveToContainer(container, savedItem.position)) player.GiveItem(item);
            return item;
        }

        StorageContainer GenerateBag(BasePlayer player, int slots)
        {
            var pos = new Vector3(player.transform.position.x, -100, player.transform.position.z);
            var storage = GameManager.server.CreateEntity(config.buff_settings.bag_prefab, pos) as StorageContainer;
            UnityEngine.Object.DestroyImmediate(storage.GetComponent<GroundWatch>());
            UnityEngine.Object.DestroyImmediate(storage.GetComponent<DestroyOnGroundMissing>());
            storage.Spawn();

            storage.inventory.capacity = slots;
            storage.inventorySlots = slots;

            FetchItems(player, storage.inventory);
            storage.OwnerID = player.userID;
            containers.Add(storage.inventory.uid.Value, new Containers(storage, player.UserIDString, player.userID));
            return storage;
        }

        //List<StorageContainer> containers = new List<StorageContainer>();

        Dictionary<ulong, Containers> containers = new Dictionary<ulong, Containers>();
        public class Containers
        {
            public StorageContainer container;
            public string userIDString;
            public ulong userID;
            public Containers(StorageContainer container, string userIDString, ulong userID)
            {
                this.container = container;
                this.userIDString = userIDString;
                this.userID = userID;
            }
        }

        private object False = false;
        private object True = true;

        [HookMethod("IsExtraPocketsContainer")]
        public object IsExtraPocketsContainer(ulong uid)
        {
            if (containers.ContainsKey(uid)) return True;
            return False;
        }

        [HookMethod("GetExtraPocketsContainerProvider")]
        public Func<ulong, bool> GetExtraPocketsContainerProvider()
        {
            return new Func<ulong, bool>(uid =>
            {
                if (containers.ContainsKey(uid)) return true;
                return false;
            });
        }

        [HookMethod("GetExtraPocketsOwnerIdProvider")]
        public Func<ulong, string> GetExtraPocketsOwnerIdProvider()
        {
            return new Func<ulong, string>(uid =>
            {
                Containers data;
                if (containers.TryGetValue(uid, out data)) return data.userIDString;
                return null;
            });
        }

        bool StorePlayerItems(BasePlayer player, StorageContainer container)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                if (player.IsConnected) Player.Message(player, lang.GetMessage("FailReload", this, player.UserIDString), config.misc_settings.ChatID);
                //LogToFile("DataFailure", $"[{DateTime.Now}] Failed to acquire data for {player.displayName}[{player.UserIDString}] - StorePlayerItems. [Online = {player.IsConnected}]", this, true);
                return false;
            }
            List<Item> items = Pool.GetList<Item>();
            items.AddRange(container.inventory?.itemList);
            var droppedItemsStr = "";
            foreach (var item in items)
            {
                if (config.tools_black_white_list_settings.white_list.Count > 0)
                {
                    if (!config.tools_black_white_list_settings.white_list.Contains(item.info.shortname))
                    {
                        player.GiveItem(item);
                        droppedItemsStr += $"{item.name ?? item.info.displayName.english}\n";
                    }
                }
                else if (config.tools_black_white_list_settings.black_list.Contains(item.info.shortname))
                {
                    player.GiveItem(item);
                    droppedItemsStr += $"{item.name ?? item.info.displayName.english}\n";
                }
            }

            if (!string.IsNullOrEmpty(droppedItemsStr))
            {
                if (config.tools_black_white_list_settings.white_list.Count > 0) Player.Message(player, string.Format(lang.GetMessage("WhitelistedItemsNotFound", this, player.UserIDString), droppedItemsStr), config.misc_settings.ChatID);
                else Player.Message(player, string.Format(lang.GetMessage("BlacklistedItemsFound", this, player.UserIDString), droppedItemsStr), config.misc_settings.ChatID);
            }

            Pool.FreeList(ref items);

            ItemManager.DoRemoves(); // Handles 0 amounts.
            playerData.pouch_items.AddRange(GetItems(player, container.inventory));

            containers.Remove(container.inventory.uid.Value);
            container.Invoke(container.KillMessage, 0.01f);

            return true;
        }

        List<ItemInfo> GetItems(BasePlayer player, ItemContainer container)
        {
            List<ItemInfo> result = new List<ItemInfo>();
            foreach (var item in container.itemList)
            {
                result.Add(new ItemInfo()
                {
                    shortname = item.info.shortname,
                    position = item.position,
                    amount = item.amount,
                    ammo = item.GetHeldEntity() is BaseProjectile ? (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : item.GetHeldEntity() is FlameThrower ? (item.GetHeldEntity() as FlameThrower).ammo : 0,
                    ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                    skin = item.skin,
                    condition = item.condition,
                    maxCondition = item.maxCondition,
                    flags = item.flags,
                    instanceData = item.instanceData != null ? new ItemInfo.KeyInfo()
                    {
                        dataInt = item.instanceData.dataInt,
                        blueprintTarget = item.instanceData.blueprintTarget,
                        blueprintAmount = item.instanceData.blueprintAmount,
                    }
                    : null,
                    name = item.name ?? null,
                    text = item.text ?? null,
                    item_contents = item.contents?.itemList != null ? GetItems(player, item.contents) : null
                });
            }
            return result;
        }

        void OnLootEntityEnd(BasePlayer player, StorageContainer container)
        {
            if (player == null || container == null || container.inventory == null) return;
            if (containers.ContainsKey(container.inventory.uid.Value))
            {
                StorePlayerItems(player, container);
            }
        }

        void OnEntityKill(BaseEntity entity)
        {
            var container = entity as StorageContainer;
            if (container != null)
            {
                var lootContainer = entity as LootContainer;
                if (lootContainer != null)
                {
                    looted_containers.Remove(lootContainer);
                    looted_crates.Remove(lootContainer);
                    return;
                }
                if (!containers.ContainsKey(container.inventory.uid.Value)) return;
                var p = BasePlayer.activePlayerList.Where(x => x.userID == container.OwnerID).FirstOrDefault();
                if (p == null) return;
                StorePlayerItems(p, container);

                return;
            }

            var player = entity.creatorEntity as BasePlayer;
            if (player == null) return;

            if (entity is DudTimedExplosive)
            {
                var dud = entity as DudTimedExplosive;
                if (Duds.Contains(dud))
                {
                    Duds.Remove(dud);
                    return;
                }
            }

            if (entity.ShortPrefabName == "grenade.supplysignal.deployed") return;

            BuffDetails bd;
            float value;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.TryGetValue(Buff.Double_Explosion_Chance, out value)) return;
            if (RollSuccessful(value))
            {
                if (config.buff_settings.raid_perk_settings.Double_Explosion_chance_settings.blacklist.Contains(entity.ShortPrefabName)) return;
                if (entity is MLRSRocket && !config.ultimate_settings.ultimate_raiding.allow_doubling) return;
                if (!PassRaidableBasesCheck(entity, Buff.Double_Explosion_Chance)) return;

                var modelTimedExplosive = entity as TimedExplosive;
                var explosive = GameManager.server.CreateEntity(entity.PrefabName, entity.transform.position, entity.transform.rotation) as TimedExplosive;
                if (explosive == null) return;
                explosive.creatorEntity = player;
                if (modelTimedExplosive != null)
                {
                    explosive.explosionRadius = modelTimedExplosive.explosionRadius;
                    explosive.minExplosionRadius = modelTimedExplosive.minExplosionRadius;
                }

                var parent = entity.GetParentEntity();
                if (parent != null)
                {
                    explosive.DoStick(entity.transform.position, entity.transform.localPosition, parent, null);
                }

                timer.Once(0.5f, () =>
                {
                    var timedExplosive = explosive as TimedExplosive;
                    if (timedExplosive != null)
                    {
                        Unsubscribe(nameof(OnEntityKill));
                        timedExplosive.Explode();
                        Subscribe(nameof(OnEntityKill));
                    }
                });
            }
        }

        List<DudTimedExplosive> Duds = new List<DudTimedExplosive>();

        private object OnExplosiveDud(DudTimedExplosive dudTimedExplosive)
        {
            var player = dudTimedExplosive.creatorEntity as BasePlayer;
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;

            BuffDetails bd;
            float value;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.TryGetValue(Buff.Dudless_Explosive, out value) && RollSuccessful(value) && PassRaidableBasesCheck(dudTimedExplosive, Buff.Dudless_Explosive))
            {
                PlayerInfo pi;
                if (pcdData.pEntity.TryGetValue(player.userID, out pi) && pi.notifications) Player.Message(player, lang.GetMessage("DudExplodedAnyway", this, player.UserIDString), config.misc_settings.ChatID);
                return false;
            }

            if (!Duds.Contains(dudTimedExplosive)) Duds.Add(dudTimedExplosive);

            return null;
        }

        void OnExplosiveThrown(BasePlayer player, TimedExplosive timedExplosive, ThrownWeapon item) => HandleExplosionRadius(player, timedExplosive);
        void OnExplosiveDropped(BasePlayer player, TimedExplosive timedExplosive, ThrownWeapon item) => HandleExplosionRadius(player, timedExplosive);
        void OnRocketLaunched(BasePlayer player, TimedExplosive entity) => HandleExplosionRadius(player, entity);
        void HandleExplosionRadius(BasePlayer player, TimedExplosive timedExplosive)
        {
            if (config.buff_settings.raid_perk_settings.Explosion_Radius_settings.blacklist.Contains(timedExplosive.ShortPrefabName)) return;
            BuffDetails bd;
            float value;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.TryGetValue(Buff.Explosion_Radius, out value) || !PassRaidableBasesCheck(timedExplosive, Buff.Explosion_Radius)) return;
            timedExplosive.explosionRadius += timedExplosive.explosionRadius * value;
            if (config.buff_settings.raid_perk_settings.Explosion_Radius_settings.add_to_minimum) timedExplosive.minExplosionRadius += timedExplosive.minExplosionRadius * value;
        }

        bool CheckedButtonReady = false;
        List<BasePlayer> waitingPlayers = new List<BasePlayer>();
        Timer checkTimer;

        void CheckButtonReady()
        {
            PlayerInfo playerData;
            if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase))
            {
                if (Convert.ToBoolean(ImageLibrary?.Call("HasImage", ExtraPocketsImg.Key)))
                {
                    CheckedButtonReady = true;
                    foreach (var player in waitingPlayers)
                    {
                        if (pcdData.pEntity.TryGetValue(player.userID, out playerData) && playerData.extra_pockets_button)
                            SendExtraPocketsButton(player);
                    }
                    waitingPlayers.Clear();
                    waitingPlayers = null;
                    checkTimer = null;
                }
                else
                {
                    checkTimer = timer.In(5f, CheckButtonReady);
                }
            }
            else
            {
                CheckedButtonReady = true;
                foreach (var player in waitingPlayers)
                {
                    if (pcdData.pEntity.TryGetValue(player.userID, out playerData) && playerData.extra_pockets_button)
                        SendExtraPocketsButton(player);
                }
                waitingPlayers.Clear();
                waitingPlayers = null;
                if (checkTimer != null && !checkTimer.Destroyed) checkTimer.Destroy();
                checkTimer = null;
            }
        }

        private void SendExtraPocketsButton(BasePlayer player)
        {
            if (string.IsNullOrEmpty(ExtraPocketsImg.Key) && string.IsNullOrEmpty(ExtraPocketsImgSkin.Key)) return;
            if (!CheckedButtonReady)
            {
                if (!waitingPlayers.Contains(player)) waitingPlayers.Add(player);
                if (checkTimer == null || checkTimer.Destroyed) CheckButtonReady();
                return;
            }

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.3207547 0.3207547 0.3207547 0.6705883" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = config.tools_black_white_list_settings.extra_pockets_button_anchor.x_min + " " + config.tools_black_white_list_settings.extra_pockets_button_anchor.y_min, OffsetMax = config.tools_black_white_list_settings.extra_pockets_button_anchor.x_max + " " + config.tools_black_white_list_settings.extra_pockets_button_anchor.y_max }
            }, "Overlay", "ExtraPocketsButton");

            var x_size = ((Convert.ToSingle(config.tools_black_white_list_settings.extra_pockets_button_anchor.x_max) - Convert.ToSingle(config.tools_black_white_list_settings.extra_pockets_button_anchor.x_min)) - 2) / 2;
            var y_size = ((Convert.ToSingle(config.tools_black_white_list_settings.extra_pockets_button_anchor.y_max) - Convert.ToSingle(config.tools_black_white_list_settings.extra_pockets_button_anchor.y_min)) - 2) / 2;

            if (config.general_settings.image_cache_source.Equals("url", StringComparison.OrdinalIgnoreCase) || ExtraPocketsImgSkin.Value == 0)
            {
                container.Add(new CuiElement
                {
                    Name = "Image_5410",
                    Parent = "ExtraPocketsButton",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", ExtraPocketsImg.Key) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-{x_size} -{y_size}", OffsetMax = $"{x_size} {y_size}" }
                }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "Image_5410",
                    Parent = "ExtraPocketsButton",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = 1751045826, SkinId = ExtraPocketsImgSkin.Value },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-{x_size} -{y_size}", OffsetMax = $"{x_size} {y_size}" }
                }
                });
            }

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"openextrapockets" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-{x_size} -{y_size}", OffsetMax = $"{x_size} {y_size}" }
            }, "ExtraPocketsButton", "Button_2451");

            CuiHelper.DestroyUi(player, "ExtraPocketsButton");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("openextrapockets")]
        void OpenExtraPocketsButton(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            OpenBag(player);
        }

        #endregion

        #region Scoreboard

        void CheckScoreBoardConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CheckScoreBoard(player);
        }

        void CheckScoreBoard(BasePlayer player)
        {
            if (ScoreBoard.lastChecked + 10f < Time.time) UpdateScoreBoard();
            ScoreBoardBackPanel(player);
            ScoreBoardPanel(player);
        }

        void UpdateScoreBoard()
        {
            if (pcdData.pEntity.Count == 0) return;
            ScoreBoard.scoreList = pcdData.pEntity.OrderByDescending(x => x.Value.xp).ToDictionary(i => i.Key, i => new ScoreboardInfo.ScoreInfo(i.Value.name ?? i.Key.ToString(), Math.Round(i.Value.xp, config.xp_settings.xp_rounding)));
            List<ulong> keys_to_remove = Pool.GetList<ulong>();
            foreach (var kvp in ScoreBoard.scoreList)
            {
                if (permission.UserHasPermission(kvp.Key.ToString(), perm_no_scoreboard))
                    keys_to_remove.Add(kvp.Key);
            }

            foreach (var key in keys_to_remove)
                ScoreBoard.scoreList.Remove(key);

            Pool.FreeList(ref keys_to_remove);
            ScoreBoard.lastChecked = Time.time;
        }

        private void ScoreBoardBackPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.99" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.287 0.343", OffsetMax = "0.312 -0.337" }
            }, "Overlay", "ScoreboardBackPanel");

            CuiHelper.DestroyUi(player, "ScoreboardBackPanel");
            CuiHelper.AddUi(player, container);
        }

        // Max 15 per page.
        private void ScoreBoardPanel(BasePlayer player, int lastElement = 0)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-231.299 -189.993", OffsetMax = "128.701 190.007" }
            }, "Overlay", "ScoreBoardPanel");
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1698113 0.1698113 0.1698113 0.6980392" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-176 -186.26", OffsetMax = "176 139.74" }
            }, "ScoreBoardPanel", "Panel_7832");
            container.Add(new CuiElement
            {
                Name = "Label_1767",
                Parent = "ScoreBoardPanel",
                Components = {
                    new CuiTextComponent { Text = "SCORES", Font = "robotocondensed-bold.ttf", FontSize = 32, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-116.001 144", OffsetMax = "115.999 186" }
                }
            });
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1226415 0.122063 0.122063 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-176.001 120.246", OffsetMax = "175.999 139.735" }
            }, "ScoreBoardPanel", "Panel_2861");
            container.Add(new CuiElement
            {
                Name = "Label_2820",
                Parent = "Panel_2861",
                Components = {
                    new CuiTextComponent { Text = "RANK", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.2832619 0.5943396 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-174.309 -9.741", OffsetMax = "-139.689 9.75" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Label_4187",
                Parent = "Panel_2861",
                Components = {
                    new CuiTextComponent { Text = "NAME", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.9150943 0.8035371 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-97.482 -9.746", OffsetMax = "2.518 9.745" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "Label_8268",
                Parent = "Panel_2861",
                Components = {
                    new CuiTextComponent { Text = "TOTAL XP", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "0 0.8679245 0.8500054 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "42.741 -9.746", OffsetMax = "176.001 9.745" }
                }
            });
            var count = 0;
            var lastEntry = lastElement;
            for (int i = lastElement; i < lastElement + 15; i++)
            {
                if (i < 0)
                {
                    continue;
                }
                if (ScoreBoard.scoreList.Count <= i)
                {
                    break;
                }
                container.Add(new CuiElement
                {
                    Name = "ScoreRank",
                    Parent = "ScoreBoardPanel",
                    Components = {
                    new CuiTextComponent { Text = $"{i+1}:", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "0.282353 0.5960785 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-172.019 {94.5 - (20 * count)}", OffsetMax = $"-141.981 {114.5 - (20 * count)}" }
                }
                });
                container.Add(new CuiElement
                {
                    Name = "ScoreName",
                    Parent = "ScoreBoardPanel",
                    Components = {
                    new CuiTextComponent { Text = ScoreBoard.scoreList.ElementAt(i).Value.name, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperCenter, Color = "0.9137255 0.8039216 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-137.399 {94.5 - (20 * count)}", OffsetMax = $"42.433 {114.5 - (20 * count)}" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "ScoreXP",
                    Parent = "ScoreBoardPanel",
                    Components = {
                    new CuiTextComponent { Text = $"{ScoreBoard.scoreList.ElementAt(i).Value.xp:###,###,##0}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "0 0.8666667 0.8509804 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"42.741 {94.5 - (20 * count)}", OffsetMax = $"175.999 {114.5 - (20 * count)}" }
                }
                });
                count++;
                lastEntry = i;
            }
            if (lastEntry - 15 > 0)
            {
                container.Add(new CuiElement
                {
                    Name = "leftarrow",
                    Parent = "ScoreBoardPanel",
                    Components = {
                    new CuiTextComponent { Text = "<<", Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-176 149", OffsetMax = "-144 181" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"scoreboardchangepage {lastElement - 15}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }, "leftarrow", "Button_4851");
            }
            if (ScoreBoard.scoreList.Count - 1 > lastEntry)
            {
                container.Add(new CuiElement
                {
                    Name = "rightarrow",
                    Parent = "ScoreBoardPanel",
                    Components = {
                    new CuiTextComponent { Text = ">>", Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "144 150.8", OffsetMax = "176 182.8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"scoreboardchangepage {lastEntry + 1}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }, "rightarrow", "Button_4851123123");
            }
            container.Add(new CuiElement
            {
                Name = "ScoreboardCloseLabel",
                Parent = "ScoreBoardPanel",
                Components = {
                    new CuiTextComponent { Text = "CLOSE", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26 -219.8", OffsetMax = "26 -195.8" }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "closescoreboard" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26 -12.001", OffsetMax = "26 12.001" }
            }, "ScoreboardCloseLabel", "ScoreboardCloseButton");
            CuiHelper.DestroyUi(player, "ScoreBoardPanel");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closescoreboard")]
        void CloseScoreBoard(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "ScoreboardBackPanel");
            CuiHelper.DestroyUi(player, "ScoreBoardPanel");
        }

        [ConsoleCommand("scoreboardchangepage")]
        void ChangeBoard(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            var startElement = Convert.ToInt32(arg.Args[0]);

            // if the element is on the first page, we just show the whole first page.
            if (startElement > 0 && startElement < 15) ScoreBoardPanel(player, 0);
            // If the element is larger than the list count, we send the last 15 players in the list.
            else if (startElement > ScoreBoard.scoreList.Count - 1) ScoreBoardPanel(player, ScoreBoard.scoreList.Count - 16);
            // Otherwise we send the scoreboard from the startElement.
            else ScoreBoardPanel(player, startElement);
        }

        #endregion

        #region Ultimates

        #region Ultimate settings

        string GetUltimateSettingsDescription(Buff buff)
        {
            switch (buff)
            {
                case Buff.Woodcutting_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Mining_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Combat_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Vehicle_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Harvester_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Medical_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Skinning_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Build_Craft_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), lang.GetMessage("Build_Craft_formatted", this));
                case Buff.Scavengers_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Raiding_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                case Buff.Cooking_Ultimate: return string.Format(lang.GetMessage("UltimateSettingsUIDescription", this), buff.ToString().Split('_')[0]);
                default: return "Custom Ultimate";
            }
        }

        private void SkillTree_UltimateMenu(BasePlayer player)
        {
            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9803922" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0.024 -0.331", OffsetMax = "0.325 0.339" }
            }, "Overlay", "SkillTree_UltimateMenu");

            container.Add(new CuiElement
            {
                Name = "SkillTreeUltimateMenu_Title",
                Parent = "SkillTree_UltimateMenu",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UltimateSettingsButton", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 190.4", OffsetMax = "-180.18 250.4" }
                }
            });

            var count = 0;
            var row = 0;
            if (pi.ultimate_settings.Count == 0)
            {
                container.Add(new CuiElement
                {
                    Name = "no_ultimates_unlocked",
                    Parent = "SkillTree_UltimateMenu",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UINoUltimatesUnlocked", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.9397521 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-495.24 158.268", OffsetMax = "-180.18 190.4" }
                }
                });
            }
            else foreach (var option in pi.ultimate_settings)
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-495.24 + (325.06 * row)} {132.4 - (68 * count)}", OffsetMax = $"{-180.18 + (325.06 * row)} {190.4 - (68 * count)}" }
                    }, "SkillTree_UltimateMenu", "SkillTree_UltimateMenu_tgl_pnl_bk_1");

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.2264151 0.2264151 0.2264151 0.9607843" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155.53 -27", OffsetMax = "155.53 27" }
                    }, "SkillTree_UltimateMenu_tgl_pnl_bk_1", "SkillTree_UltimateMenu_tgl_pnl_ft_1");

                    container.Add(new CuiElement
                    {
                        Name = "SkillTree_UltimateMenu_tgl_des_1",
                        Parent = "SkillTree_UltimateMenu_tgl_pnl_bk_1",
                        Components = {
                        new CuiTextComponent { Text = GetUltimateSettingsDescription(option.Key), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147 -26", OffsetMax = "52.24 26" }
                    }
                    });

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1132075 0.1073335 0.1073335 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.53 -24", OffsetMax = "152.53 24" }
                    }, "SkillTree_UltimateMenu_tgl_pnl_bk_1", "SkillTree_UltimateMenu_tgl_bttn_pnl_1");

                    container.Add(new CuiButton
                    {
                        Button = { Color = "0.1607843 0.1607843 0.1607843 1", Command = $"sttoggleultimate {option.Key}" },
                        Text = { Text = option.Value.enabled ? lang.GetMessage("ON", this, player.UserIDString) : lang.GetMessage("OFF", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = option.Value.enabled ? "0.1333333 0.5960785 0.1872104 1" : "0.5943396 0.131764 0.1842591 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68.53 -22", OffsetMax = "150.53 22" }
                    }, "SkillTree_UltimateMenu_tgl_pnl_bk_1", "SkillTree_UltimateMenu_tgl_bttn_1");

                    count++;
                    if (count >= 5)
                    {
                        row++;
                        count = 0;
                    }
                }

            container.Add(new CuiElement
            {
                Name = "SkillTree_UltimateMenu_close",
                Parent = "SkillTree_UltimateMenu",
                Components = {
                    new CuiTextComponent { Text = "CLOSE", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-366.71 -201.1", OffsetMax = "-308.71 -169.1" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"stcloseultimatesettings" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29 -16", OffsetMax = "29 16" }
            }, "SkillTree_UltimateMenu_close", "SkillTree_UltimateMenu_close_button");



            CuiHelper.DestroyUi(player, "SkillTree_UltimateMenu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("stcloseultimatesettings")]
        void CloseUltimateSettings(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "SkillTree_UltimateMenu");
        }

        [ConsoleCommand("sttoggleultimate")]
        void ToggleUltimate(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) return;
            Buff buff;
            if (!Enum.TryParse(arg.Args[0], out buff)) return;

            UltimatePlayerSettings ultimateData;
            if (!pi.ultimate_settings.TryGetValue(buff, out ultimateData)) pi.ultimate_settings.Add(buff, ultimateData = new UltimatePlayerSettings());
            if (ultimateData.enabled) ultimateData.enabled = false;
            else ultimateData.enabled = true;

            HandleUltimateToggle(player, buff, pi);

            SkillTree_UltimateMenu(player);
        }

        bool IsUltimateEnabled(BasePlayer player, Buff buff)
        {
            PlayerInfo playerData;
            UltimatePlayerSettings buffSettings;
            return pcdData.pEntity.TryGetValue(player.userID, out playerData) && playerData.ultimate_settings.TryGetValue(buff, out buffSettings) && buffSettings.enabled;
        }

        void HandleUltimateToggle(BasePlayer player, Buff buff, PlayerInfo pi)
        {
            // Add handles here.

            UltimatePlayerSettings ups;
            if (!pi.ultimate_settings.TryGetValue(buff, out ups)) pi.ultimate_settings.Add(buff, ups = new UltimatePlayerSettings());
            if (!ups.enabled) Player.Message(player, $"Ultimate: {string.Format(lang.GetMessage("UltimateDisabledMessage", this, player.UserIDString), lang.GetMessage("UI" + buff.ToString(), this, player.UserIDString))} has been disabled.", config.misc_settings.ChatID);

            if (buff == Buff.Mining_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnMining", this, player.UserIDString), config.ultimate_settings.ultimate_mining.distance_from_player, config.ultimate_settings.ultimate_mining.find_node_cmd, config.ultimate_settings.ultimate_mining.cooldown), config.misc_settings.ChatID);
                }
            }
            if (buff == Buff.Vehicle_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnVehicle", this, player.UserIDString), config.ultimate_settings.ultimate_vehicle.reduce_by * 100), config.misc_settings.ChatID);
                }
            }
            if (buff == Buff.Medical_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnMedical", this, player.UserIDString), config.ultimate_settings.ultimate_medical.resurrection_chance), config.misc_settings.ChatID);
                }
                else CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_ResurrectionButton");
            }
            if (buff == Buff.Harvester_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnHarvester", this, player.UserIDString), config.ultimate_settings.ultimate_harvesting.gene_chat_command, config.ultimate_settings.ultimate_harvesting.cooldown), config.misc_settings.ChatID);
                }
            }

            if (buff == Buff.Build_Craft_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnBuildCraft", this, player.UserIDString), config.ultimate_settings.ultimate_buildCraft.success_chance), config.misc_settings.ChatID);
                }
            }

            if (buff == Buff.Woodcutting_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnWoodcutting", this, player.UserIDString), config.ultimate_settings.ultimate_woodcutting.distance_from_player), config.misc_settings.ChatID);
                }
            }

            if (buff == Buff.Scavengers_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, lang.GetMessage("UltimateToggleOnScavengers", this, player.UserIDString), config.misc_settings.ChatID);
                }
            }

            if (buff == Buff.Combat_Ultimate)
            {
                if (ups.enabled)
                {
                    string formattedString = "";
                    var active = 0;
                    if (config.ultimate_settings.ultimate_combat.scientists_enabled) active++;
                    if (config.ultimate_settings.ultimate_combat.players_enabled) active++;
                    if (config.ultimate_settings.ultimate_combat.scientists_enabled) active++;

                    if (config.ultimate_settings.ultimate_combat.scientists_enabled)
                    {
                        formattedString += lang.GetMessage("CombatUltimateScientists", this, player.UserIDString);
                        if (active == 2) formattedString += lang.GetMessage("CombatUltimateAnd", this, player.UserIDString);
                        if (active == 3) formattedString += ", ";
                    }
                    if (config.ultimate_settings.ultimate_combat.animals_enabled)
                    {
                        formattedString += lang.GetMessage("CombatUltimateAnimals", this, player.UserIDString);
                        if (active == 3) formattedString += lang.GetMessage("CombatUltimateAnd", this, player.UserIDString);
                    }
                    if (config.ultimate_settings.ultimate_combat.players_enabled) formattedString += lang.GetMessage("CombatUltimatePlayers", this, player.UserIDString);
                    Player.Message(player, string.Format(lang.GetMessage("CombatUltimateToggleOnMessage", this, player.UserIDString), config.ultimate_settings.ultimate_combat.health_scale * 100, formattedString), config.misc_settings.ChatID);
                    //Player.Message(player, $"You will now receive <color=#DFF008>{config.ultimate_settings.ultimate_combat.health_scale * 100}%</color> of the damage as health when damaging {formattedString}.", config.misc_settings.ChatID);
                }
            }
            if (buff == Buff.Skinning_Ultimate)
            {
                if (ups.enabled)
                {
                    Player.Message(player, string.Format(lang.GetMessage("SkinningUltimateToggleText", this, player.UserIDString), string.Join("</color>, <color=#DFF008>", config.ultimate_settings.ultimate_skinning.enabled_buffs.Select(x => lang.GetMessage(x.Key.ToString().ToLower(), this, player.UserIDString)))), config.misc_settings.ChatID);
                }
                else
                {
                    RemoveAnimalBuff(player);
                }
            }

            if (buff == Buff.Raiding_Ultimate)
            {
                if (ups.enabled)
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnRaiding", this, player.UserIDString), config.ultimate_settings.ultimate_raiding.command, config.ultimate_settings.ultimate_raiding.cooldown), config.misc_settings.ChatID);
                else DestroyRaidBehaviour(player);
            }

            if (buff == Buff.Cooking_Ultimate)
            {
                if (ups.enabled)
                    Player.Message(player, string.Format(lang.GetMessage("UltimateToggleOnCooking", this, player.UserIDString), config.ultimate_settings.ultimate_cooking.command, config.ultimate_settings.ultimate_cooking.buff_cooldown), config.misc_settings.ChatID);
                else RemoveCookingUltimateBuffs(player);
            }
        }

        #endregion

        #region Forager

        void ForagerChatCMD(BasePlayer player) => HandleForagerBuff(player);
        void ForagerConsoleCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            HandleForagerBuff(player);
        }

        Dictionary<ulong, float> ForagerCooldown = new Dictionary<ulong, float>();
        void HandleForagerBuff(BasePlayer player)
        {
            if (!player.IsAdmin && !player.IsDeveloper && player.IsFlying)
            {
                Player.Message(player, lang.GetMessage("DisableNoclipCommand", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.ContainsKey(Buff.Forager)) return;

            if (config.buff_settings.forager_settings.cooldown > 0)
            {
                float time;
                if (ForagerCooldown.TryGetValue(player.userID, out time))
                {
                    if (time > Time.time)
                    {
                        Player.Message(player, string.Format(lang.GetMessage("ForageBuffCooldown", this, player.UserIDString), Math.Round(time - Time.time, 0)), config.misc_settings.ChatID);
                        return;
                    }
                    else ForagerCooldown[player.userID] = Time.time + config.buff_settings.forager_settings.cooldown;
                }
                else ForagerCooldown.Add(player.userID, Time.time + config.buff_settings.forager_settings.cooldown);
            }

            var collectibles = FindEntitiesOfType<CollectibleEntity>(player.transform.position, config.buff_settings.forager_settings.distance);
            collectibles.RemoveAll(x => config.buff_settings.forager_settings.blacklist.Contains(x.ShortPrefabName));

            if (collectibles.Count == 0)
            {
                Pool.FreeList(ref collectibles);
                return;
            }

            var wasAdmin = player.IsAdmin;
            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }

            string nodeName;
            foreach (var collectible in collectibles)
            {
                float[] cols;
                if (!config.buff_settings.forager_settings.displayColours.TryGetValue(collectible.ShortPrefabName, out cols)) cols = DefaultForagerCol;
                player.SendConsoleCommand("ddraw.text", config.buff_settings.forager_settings.time_on_screen, new Color(cols[0], cols[1], cols[2]), collectible.transform.position, CollectibleDisplayName(player, collectible.ShortPrefabName));
            }

            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }

            Pool.FreeList(ref collectibles);
        }

        float[] DefaultForagerCol = new float[3] { 0.960f, 0.802f, 0.230f };

        string CollectibleDisplayName(BasePlayer player, string shortname)
        {
            return lang.GetMessage(shortname, this, player.UserIDString);
        }

        #endregion

        #region Mining ultimate

        Dictionary<ulong, float> MiningUltimateCooldowns = new Dictionary<ulong, float>();

        void TriggerMiningUltimateFromItem(BasePlayer player) => TriggerMiningUltimateAction(player, false);
        void TriggerMiningUltimateFromCMD(BasePlayer player) => TriggerMiningUltimateAction(player, true);

        void TriggerMiningUltimateAction(BasePlayer player, bool from_command = true)
        {
            if (!player.IsAdmin && !player.IsDeveloper && player.IsFlying)
            {
                Player.Message(player, lang.GetMessage("DisableNoclipCommand", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Mining_Ultimate) && IsUltimateEnabled(player, Buff.Mining_Ultimate))
            {
                if (MiningUltimateCooldowns.ContainsKey(player.userID))
                {
                    if (MiningUltimateCooldowns[player.userID] > Time.time)
                    {
                        if (from_command) Player.Message(player, string.Format(lang.GetMessage("MiningUltimateCooldownMessage", this, player.UserIDString), Math.Round(MiningUltimateCooldowns[player.userID] - Time.time, 0)), config.misc_settings.ChatID);
                        return;
                    }
                    else MiningUltimateCooldowns[player.userID] = Time.time + config.ultimate_settings.ultimate_mining.cooldown;
                }
                else MiningUltimateCooldowns.Add(player.userID, Time.time + config.ultimate_settings.ultimate_mining.cooldown);
                List<BaseEntity> mining_nodes = Pool.GetList<BaseEntity>();
                var entities = FindEntitiesOfType<BaseEntity>(player.transform.position, config.ultimate_settings.ultimate_mining.distance_from_player);
                mining_nodes.AddRange(entities.Where(x => x.PrefabName.StartsWith("assets/bundled/prefabs/autospawn/resource/ores")));
                Pool.FreeList(ref entities);
                if (mining_nodes.Count > 0)
                {
                    var wasAdmin = player.IsAdmin;
                    if (!wasAdmin)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendNetworkUpdateImmediate();
                    }

                    string nodeName;
                    foreach (var node in mining_nodes)
                    {
                        nodeName = string.Format("<size={0}>{1}</size>", config.ultimate_settings.ultimate_mining.text_size, (node.ShortPrefabName == "metal-ore" ? lang.GetMessage("metal", this, player.UserIDString) : node.ShortPrefabName == "stone-ore" ? lang.GetMessage("stone", this, player.UserIDString) : lang.GetMessage("sulfur", this, player.UserIDString)) + (config.ultimate_settings.ultimate_mining.show_distance ? $" - Distance: {Mathf.Round(Vector3.Distance(player.transform.position, node.transform.position))}" : null));
                        player.SendConsoleCommand("ddraw.text", config.ultimate_settings.ultimate_mining.hud_time, GetNodeColor(node), node.transform.position, nodeName);
                    }

                    if (!wasAdmin)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                        player.SendNetworkUpdateImmediate();
                    }
                }
                Pool.FreeList(ref mining_nodes);
            }
        }

        Color GetNodeColor(BaseEntity entity)
        {
            if (entity == null) return Color.yellow;
            switch (entity.ShortPrefabName)
            {
                case "stone-ore": return GetColor(config.ultimate_settings.ultimate_mining.stone_colour);
                case "metal-ore": return GetColor(config.ultimate_settings.ultimate_mining.metal_colour);
                case "sulur-ore": return GetColor(config.ultimate_settings.ultimate_mining.sulfur_colour);
                default: return Color.yellow;
            }
        }

        Color GetColor(int type)
        {
            switch (type)
            {
                case 0: return Color.red;
                case 1: return Color.green;
                case 2: return Color.blue;
                case 3: return Color.white;
                case 4: return Color.black;
                case 5: return Color.yellow;
                case 6: return Color.cyan;
                case 7: return Color.magenta;
                default: return Color.yellow;
            }
        }

        #endregion

        #region Resurrection button

        private void SendResurrectionButton(BasePlayer player, Vector3 pos)
        {
            if (Resurrection_Cooldowns.ContainsKey(player.userID) && Resurrection_Cooldowns[player.userID] > Time.time) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1320755 0.1314525 0.1314525 1" },
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-104.9 86.718", OffsetMax = "-4.9 110.718" }
            }, "Overlay", "SkillTree_MedicalUltimate_ResurrectionButton");

            container.Add(new CuiButton
            {
                Button = { Color = "0.2352941 0.2705882 0.1607843 1", Command = $"stattemptresurrection {pos.x} {pos.y} {pos.z}" },
                Text = { Text = "RESURRECT", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.5803922 0.7294118 0.2588235 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-48 -10", OffsetMax = "48 10" }
            }, "SkillTree_MedicalUltimate_ResurrectionButton", "Button_2968");

            CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_ResurrectionButton");
            CuiHelper.AddUi(player, container);
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            BuffDetails bd;            
            if (!buffDetails.TryGetValue(player.userID, out bd)) return;
            float buffValue;
            if (bd.buff_values.ContainsKey(Buff.Medical_Ultimate)) CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_ResurrectionButton");
            if (bd.buff_values.TryGetValue(Buff.Spawn_Health, out buffValue))
            {
                Subscription subData;
                bool isSubbed = subscriptions.TryGetValue(nameof(OnPlayerHealthChange), out subData) && subData.isSubscribed;
                if (isSubbed) Unsubscribe(nameof(OnPlayerHealthChange));
                player.SetHealth(buffValue * 100);
                if (isSubbed) Subscribe(nameof(OnPlayerHealthChange));
            }
        }

        Dictionary<ulong, float> Resurrection_Cooldowns = new Dictionary<ulong, float>();

        [ConsoleCommand("stattemptresurrection")]
        void AttemptResurrection(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_ResurrectionButton");

            if (player.IsAlive() || !player.IsConnected) return;

            if (UnityEngine.Random.Range(0f, 100f) <= config.ultimate_settings.ultimate_medical.resurrection_chance)
            {
                var pos = new Vector3(Convert.ToSingle(arg.Args[0]), Convert.ToSingle(arg.Args[1]), Convert.ToSingle(arg.Args[2]));
                player.RespawnAt(pos, Quaternion.identity);
                if (Resurrection_Cooldowns.ContainsKey(player.userID)) Resurrection_Cooldowns[player.userID] = Time.time + config.ultimate_settings.ultimate_medical.resurrection_delay;
                else Resurrection_Cooldowns.Add(player.userID, Time.time + config.ultimate_settings.ultimate_medical.resurrection_delay);
            }
            else
            {
                SendResurrectionFailed(player);
                timer.Once(3f, () =>
                {
                    if (player != null)
                        CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_Failed");
                });
            }
        }

        private void SendResurrectionFailed(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "SkillTree_MedicalUltimate_Failed",
                Parent = "Overlay",
                Components = {
                    new CuiTextComponent { Text = "FAILED", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 0 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-104.57 66.393", OffsetMax = "-4.57 90.387" }
                }
            });

            CuiHelper.DestroyUi(player, "SkillTree_MedicalUltimate_Failed");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Harvester Ultimate

        bool FoundInvalidGeneLetter(string genes)
        {
            foreach (var c in genes.ToCharArray())
            {
                if (c != 'g' && c != 'e' && c != 'x' && c != 'w' && c != 'y' && c != 'h') return true;
            }
            return false;
        }

        void SetPlantGenes(BasePlayer player, string command, string[] args)
        {
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.ContainsKey(Buff.Harvester_Ultimate))
            {
                Player.Message(player, lang.GetMessage("RequireHarvestingUltimateMsg", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }
            Plant_Gene_Select_background(player);
            Plant_Gene_Select(player);
        }

        private void Plant_Gene_Select(BasePlayer player)
        {
            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9490196" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.675 0.01", OffsetMax = "0.325 0" }
            }, "Overlay", "Plant_Gene_Select");

            container.Add(new CuiElement
            {
                Name = "title",
                Parent = "Plant_Gene_Select",
                Components = {
                    new CuiTextComponent { Text = "PLANT GENE STRUCTURE", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-128 160.3", OffsetMax = "128 192.3" }
                }
            });

            var gene_array = pi.plant_genes.ToCharArray();
            char[] gene_chars = { 'g', 'x', 'w', 'y', 'h' };
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    string buttonCol = "0.2924528 0.2924528 0.2924528 1";
                    if (gene_array[i] == gene_chars[j]) buttonCol = "0.003070482 0.2169811 0.02914789 1";
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1698113 0.1698113 0.1698113 0.8" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-90 + (37 * j)} {120.2 - (37 * i)}", OffsetMax = $"{-58 + (37 * j)} {152.2 - (37 * i)}" }
                    }, "Plant_Gene_Select", "gene_button_panel");

                    container.Add(new CuiButton
                    {
                        Button = { Color = buttonCol, Command = $"setgenevalue {i} {gene_chars[j]}" },
                        Text = { Text = gene_chars[j].ToString().ToUpper(), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14 -14", OffsetMax = "14 14" }
                    }, "gene_button_panel", "gene_button");
                }
            }

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "closegenstructuremenu" },
                Text = { Text = "X", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "128 160.3", OffsetMax = "160 192.3" }
            }, "Plant_Gene_Select", "close");

            CuiHelper.DestroyUi(player, "Plant_Gene_Select");
            CuiHelper.AddUi(player, container);
        }

        private void Plant_Gene_Select_background(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9490196" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.675 0.01", OffsetMax = "0.325 0" }
            }, "Overlay", "Plant_Gene_Select_background");

            CuiHelper.DestroyUi(player, "Plant_Gene_Select_background");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closegenstructuremenu")]
        void CloseGeneMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Plant_Gene_Select");
            CuiHelper.DestroyUi(player, "Plant_Gene_Select_background");
        }

        [ConsoleCommand("setgenevalue")]
        void ChangeChar(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Plant_Gene_Select");

            var pos = Convert.ToInt32(arg.Args[0]);
            var c = Convert.ToChar(arg.Args[1]);

            PlayerInfo pi;
            if (pcdData.pEntity.TryGetValue(player.userID, out pi))
            {
                string newGene = "";
                for (int i = 0; i < 6; i++)
                {
                    if (i == pos) newGene += c;
                    else newGene += pi.plant_genes[i];
                }
                pi.plant_genes = newGene.ToString();
            }
            Plant_Gene_Select(player);
        }

        #endregion

        #region Scavengers Ultimate

        private const int maxRoll = 100;

        private void Card(BasePlayer player, int cardLevel, ulong cardReaderID)
        {
            if (Interface.CallHook("OnGainXPFromSwipeCard", player, cardLevel, cardReaderID) != null) return;
            if (config.xp_settings.swipe_card_xp_cooldown > 0)
            {
                if (!LastSwipe.ContainsKey(player)) LastSwipe.Add(player, Time.time + config.xp_settings.swipe_card_xp_cooldown);
                else if (LastSwipe[player] <= Time.time) LastSwipe[player] = Time.time + config.xp_settings.swipe_card_xp_cooldown;
                else
                {
                    if (NotificationsOn(player)) Player.Message(player, string.Format(lang.GetMessage("CardSwipeCooldownMessage", this, player.UserIDString), Math.Round(LastSwipe[player] - Time.time, 2)), config.misc_settings.ChatID);
                    return;
                }
            }
            switch (cardLevel)
            {
                case 1:
                    AwardXP(player, config.xp_settings.xp_sources.swipe_card_level_1, null, false, false, "green swipe");
                    break;
                case 2:
                    AwardXP(player, config.xp_settings.xp_sources.swipe_card_level_2, null, false, false, "blue swipe");
                    break;
                case 3:
                    AwardXP(player, config.xp_settings.xp_sources.swipe_card_level_3, null, false, false, "red swipe");
                    break;
            }
        }

        Dictionary<BasePlayer, float> LastSwipe = new Dictionary<BasePlayer, float>();

        private object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            Item item = card.GetItem();
            if (item == null || item.isBroken || cardReader == null || player == null) return null;

            BuffDetails bd;
            if (card.accessLevel != cardReader.accessLevel && buffDetails.TryGetValue(player.userID, out bd) && bd.buff_values.ContainsKey(Buff.Build_Craft_Ultimate) && IsUltimateEnabled(player, Buff.Build_Craft_Ultimate))
            {
                var roll = UnityEngine.Random.Range(1, maxRoll + 1);
                if (config.ultimate_settings.ultimate_buildCraft.success_chance < maxRoll && maxRoll - config.ultimate_settings.ultimate_buildCraft.success_chance < roll)
                {
                    if (config.ultimate_settings.ultimate_buildCraft.notify_fail)
                        Player.Message(player, lang.GetMessage("BuildCraftFailNotify", this, player.UserIDString), config.misc_settings.ChatID);
                    item.LoseCondition(1f);
                    Card(player, card.accessLevel, cardReader.net.ID.Value);
                    return null;
                }

                if (Interface.CallHook("OnSwipeAccessLevelBypass", player, cardReader, card) != null) return null;

                item.LoseCondition(1f);
                Card(player, card.accessLevel, cardReader.net.ID.Value);

                cardReader.Invoke(new Action(cardReader.GrantCard), 0.5f);
                return true;
            }

            if (card.accessLevel == cardReader.accessLevel)
            {
                var condition = item.condition;
                var accessLevel = card.accessLevel;
                var cardReaderID = cardReader.net.ID.Value;
                NextTick(() =>
                {
                    if (item == null || item.condition != condition)
                        Card(player, accessLevel, cardReaderID);
                });
            }

            return null;
        }


        #endregion

        #region Build_Craft Ultimate

        void ScrapItems(Item item, LootContainer container)
        {
            if ((!config.ultimate_settings.ultimate_scavenger.scrap_skinned_items && item.skin > 0) || (!config.ultimate_settings.ultimate_scavenger.scrap_named_items && !string.IsNullOrEmpty(item.name)) || config.ultimate_settings.ultimate_scavenger.item_blacklist.Contains(item.info.shortname)) return;
            var blueprint = item.info.Blueprint;
            if (blueprint == null) return;
            item.RemoveFromContainer();
            foreach (var ingredient in blueprint.ingredients)
            {
                int amount;
                if (ingredient.itemDef.shortname == "scrap") amount = blueprint.scrapFromRecycle;
                else amount = UnityEngine.Random.Range(0, 101) <= 50 ? Mathf.CeilToInt(ingredient.amount / 2) : Mathf.FloorToInt(ingredient.amount / 2);
                if (amount == 0) continue;
                var component = ItemManager.CreateByName(ingredient.itemDef.shortname, amount * item.amount);
                container.inventory.capacity++;
                if (!component.MoveToContainer(container.inventory)) component.DropAndTossUpwards(container.transform.position);
            }
        }


        #endregion

        #region Skinning Ultimate

        public enum AnimalBuff
        {
            Chicken, //No fall damage
            Wolf, // Healing increased for each member around you
            Boar, // Access to special loot table for certain items.
            Stag, // Every x seconds the player will get a hud message if a hostile is nearby.
            Bear, // If you attack an NPC, the NPC may not attack you back for a short period of time.
            PolarBear // Overshield
        }

        public class SkinningUltimateBuff
        {
            public AnimalBuff buff;
            public Timer _timer;
            public SkinningUltimateBuff(AnimalBuff buff)
            {
                this.buff = buff;
            }

            public void DestroyTimer()
            {
                if (_timer != null && !_timer.Destroyed) _timer.Destroy();
            }
        }

        public Dictionary<BasePlayer, SkinningUltimateBuff> BuffedPlayers = new Dictionary<BasePlayer, SkinningUltimateBuff>();
        Dictionary<BasePlayer, float> OverShields = new Dictionary<BasePlayer, float>();

        void AddAnimalBuff(BasePlayer player, AnimalBuff animal)
        {
            if (!IsUltimateEnabled(player, Buff.Skinning_Ultimate)) return;
            BuffDetails bd;
            if (buffDetails.TryGetValue(player.userID, out bd))
            {
                if (bd.buff_values.ContainsKey(Buff.Skinning_Ultimate) && IsUltimateEnabled(player, Buff.Skinning_Ultimate))
                {
                    if (!config.ultimate_settings.ultimate_skinning.enabled_buffs.ContainsKey(animal) || config.ultimate_settings.ultimate_skinning.enabled_buffs[animal] == 0) return;
                    Player.Message(player, GetAnimalBuffDescription(animal, player.UserIDString), config.misc_settings.ChatID);


                    if (BuffedPlayers.ContainsKey(player)) RemoveAnimalBuff(player);
                    SkinningUltimateBuff sub;
                    BuffedPlayers.Add(player, sub = new SkinningUltimateBuff(animal));

                    sub.DestroyTimer();

                    if (animal == AnimalBuff.Bear)
                    {
                        Subscribe("OnNpcTarget");
                        if (!CanNpcAttack_subbed.Contains(player)) CanNpcAttack_subbed.Add(player);
                    }
                    else if (animal == AnimalBuff.PolarBear)
                    {
                        if (!OverShields.ContainsKey(player)) OverShields.Add(player, config.ultimate_settings.ultimate_skinning.bear_overshield_max);
                        else OverShields[player] = config.ultimate_settings.ultimate_skinning.bear_overshield_max;
                        Overshield_main(player, config.ultimate_settings.ultimate_skinning.bear_overshield_max);
                    }
                    else if (animal == AnimalBuff.Stag)
                    {
                        Timer _t;
                        if (StagDangerTimers.TryGetValue(player, out _t))
                        {
                            if (_t != null && !_t.Destroyed) _t.Destroy();
                            StagDangerTimers.Remove(player);
                        }
                        StagDangerTimers.Add(player, timer.Every(config.ultimate_settings.ultimate_skinning.stag_timer, () =>
                        {
                            ProcessStagTimer(player);
                        }));
                    }
                    sub._timer = timer.Once(config.ultimate_settings.ultimate_skinning.enabled_buffs[animal], () =>
                    {
                        RemoveAnimalBuff(player, true);
                    });
                }
            }
        }

        string GetAnimalBuffDescription(AnimalBuff animal, string userid)
        {
            switch (animal)
            {
                case AnimalBuff.Chicken: return lang.GetMessage("AnimalBuffDescription_Chicken", this, userid);
                case AnimalBuff.Bear: return lang.GetMessage("AnimalBuffDescription_Bear", this, userid);
                case AnimalBuff.Wolf: return lang.GetMessage("AnimalBuffDescription_Wolf", this, userid);
                case AnimalBuff.Boar: return lang.GetMessage("AnimalBuffDescription_Boar", this, userid);
                case AnimalBuff.Stag: return lang.GetMessage("AnimalBuffDescription_Stag", this, userid);
                case AnimalBuff.PolarBear: return lang.GetMessage("AnimalBuffDescription_PolarBear", this, userid);
                default: return null;
            }
        }

        void RemoveAnimalBuff(BasePlayer player, bool timed_out = false)
        {
            SkinningUltimateBuff _buffs;
            if (BuffedPlayers.TryGetValue(player, out _buffs))
            {
                var buff = _buffs.buff;
                if (buff == AnimalBuff.PolarBear)
                {
                    OverShields.Remove(player);
                    CuiHelper.DestroyUi(player, "Overshield_main");
                }
                else if (buff == AnimalBuff.Bear)
                {
                    CanNpcAttack_subbed.Remove(player);
                    if (CanNpcAttack_subbed.Count == 0)
                    {
                        Unsubscribe("OnNpcTarget");
                    }
                }
                else if (buff == AnimalBuff.Stag)
                {
                    Timer _t;
                    if (StagDangerTimers.TryGetValue(player, out _t))
                    {
                        if (_t != null && !_t.Destroyed) _t.Destroy();
                        StagDangerTimers.Remove(player);
                    }
                    CuiHelper.DestroyUi(player, "StagDangerUI");
                }

                _buffs.DestroyTimer();
                BuffedPlayers.Remove(player);

                if (timed_out && player != null && player.IsAlive() && player.IsConnected) Player.Message(player, lang.GetMessage("AnimalBuffFinishedMsg", this, player.UserIDString), config.misc_settings.ChatID);
            }
        }

        Dictionary<BasePlayer, Timer> StagDangerTimers = new Dictionary<BasePlayer, Timer>();

        void ProcessStagTimer(BasePlayer player)
        {
            if (player.InSafeZone()) return;
            List<BasePlayer> neutral_players = Pool.GetList<BasePlayer>();
            var entities = FindEntitiesOfType<BasePlayer>(player.transform.position, config.ultimate_settings.ultimate_skinning.stag_danger_dist);
            neutral_players.AddRange(entities.Where(x => x != player && !x.InSafeZone() && (x.Team == null || player.Team == null || x.Team.teamID != player.Team.teamID)));
            Pool.FreeList(ref entities);
            if (neutral_players.Count > 0)
            {
                StagDangerUI(player);
                if (config.ultimate_settings.ultimate_skinning.stag_draw_enemy)
                {
                    var wasAdmin = player.IsAdmin;
                    if (!wasAdmin)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendNetworkUpdateImmediate();
                    }

                    foreach (var threat in neutral_players)
                    {
                        if (threat.limitNetworking) continue;
                        player.SendConsoleCommand("ddraw.text", 10f, Color.red, threat.transform.position, lang.GetMessage("DetectionText", this, player.UserIDString));
                    }

                    if (!wasAdmin)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                        player.SendNetworkUpdateImmediate();
                    }
                }
            }
            else CuiHelper.DestroyUi(player, "StagDangerUI");
            Pool.FreeList(ref neutral_players);
        }

        bool HasAnimalBuff(BasePlayer player, AnimalBuff animal)
        {
            SkinningUltimateBuff sub;
            return BuffedPlayers.TryGetValue(player, out sub) && sub.buff == animal;
        }

        List<BasePlayer> CanNpcAttack_subbed = new List<BasePlayer>();

        object OnNpcTarget(ScientistNPC npc, BasePlayer player)
        {
            if (HasAnimalBuff(player, AnimalBuff.Bear))
            {
                return true;
            }

            return null;
        }

        private void Overshield_main(BasePlayer player, float health)
        {
            if (health <= 0) return;

            var x_value = (100 / config.ultimate_settings.ultimate_skinning.bear_overshield_max) * health;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.3490566 0.3441171 0.3441171 0.5019608" },
                RectTransform = { AnchorMin = config.ultimate_settings.ultimate_skinning.overshield_anchor.anchorMin, AnchorMax = config.ultimate_settings.ultimate_skinning.overshield_anchor.anchorMax, OffsetMin = config.ultimate_settings.ultimate_skinning.overshield_anchor.offsetMin, OffsetMax = config.ultimate_settings.ultimate_skinning.overshield_anchor.offsetMax }
            }, "Hud", "Overshield_main");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.05660379 0.05660379 0.05660379 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -8", OffsetMax = "50 8" }
            }, "Overshield_main", "Overshield_empty");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2901961 0.4711963 0.5411765 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -8", OffsetMax = $"{-50 + x_value} 8" }
            }, "Overshield_main", "Overshield_pump");

            container.Add(new CuiElement
            {
                Name = "Label_4442",
                Parent = "Overshield_main",
                Components = {
                    new CuiTextComponent { Text = "Overshield", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -8", OffsetMax = "50 8" }
                }
            });

            CuiHelper.DestroyUi(player, "Overshield_main");
            CuiHelper.AddUi(player, container);
        }

        private void StagDangerUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "StagDangerUI",
                Parent = "Hud",
                Components = {
                    new CuiTextComponent { Text = $"NEUTRAL PLAYER DETECTED NEARBY", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 0 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = config.ultimate_settings.ultimate_skinning.stag_danger_icon_anchor.anchorMin, AnchorMax = config.ultimate_settings.ultimate_skinning.stag_danger_icon_anchor.anchorMax, OffsetMin = config.ultimate_settings.ultimate_skinning.stag_danger_icon_anchor.offsetMin, OffsetMax = config.ultimate_settings.ultimate_skinning.stag_danger_icon_anchor.offsetMax }
                    //new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-201.4 87.6", OffsetMax = "-97.4 107.6" }
                }
            });

            CuiHelper.DestroyUi(player, "StagDangerUI");
            CuiHelper.AddUi(player, container);
        }

        void RollBoarLoot(BasePlayer player, CollectibleEntity entity)
        {
            if (!IsUltimateEnabled(player, Buff.Skinning_Ultimate)) return;
            if (UnityEngine.Random.Range(0f, 100f) <= config.ultimate_settings.ultimate_skinning.boar_chance)
            {
                Player.Message(player, string.Format(lang.GetMessage("BoarLootMsg", this, player.UserIDString), (entity.PrefabName.StartsWith("assets/content/nature/plants/mushroom/") ? lang.GetMessage("Mushroom", this, player.UserIDString) : lang.GetMessage("BerryBush", this, player.UserIDString))), config.misc_settings.ChatID);
                List<ItemDefinition> items = Pool.GetList<ItemDefinition>();
                items.AddRange(component_item_list.Where(x => !config.ultimate_settings.ultimate_skinning.boar_blackList.Contains(x.shortname)));
                var itemDef = items.GetRandom();

                player.GiveItem(ItemManager.CreateByName(itemDef.shortname, UnityEngine.Random.Range(config.ultimate_settings.ultimate_skinning.boar_min_quantity, config.ultimate_settings.ultimate_skinning.boar_min_quantity)));
            }
        }


        #endregion

        #region Underwater breathing behaviour

        void DestroyWaterBreathing(BasePlayer player)
        {
            BreathTime.Remove(player);
            var gameObject = player.GetComponent<WaterBreathing>();
            if (gameObject != null) UnityEngine.GameObject.Destroy(gameObject);
            CuiHelper.DestroyUi(player, "UnderwaterBreathCounter");
        }

        void UpdateWaterBreathing(BasePlayer player, float value)
        {
            DestroyWaterBreathing(player);
            BreathTime.Add(player, value);
            player.gameObject.AddComponent<WaterBreathing>();
            player.GetComponent<WaterBreathing>();
        }

        private static Dictionary<BasePlayer, float> BreathTime = new Dictionary<BasePlayer, float>();

        public class WaterBreathing : MonoBehaviour
        {
            private BasePlayer player;

            private float checkDelay;

            private float max_breathing_time;
            private bool IsSwimming;
            private float time_breathing;
            private bool exceeded_breathing_time;

            // Awake() is part of the Monobehaviour class.
            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                checkDelay = Time.time + 3f;
                max_breathing_time = BreathTime[player];
            }

            // FixedUpdate() is also part of the monobehaviour class.
            public void FixedUpdate()
            {
                if (player == null) return;

                if (checkDelay < Time.time)
                {
                    ItemModGiveOxygen.AirSupplyType oxygenSource;
                    var beathLeft = player.GetOxygenTime(out oxygenSource);

                    checkDelay = Time.time + 2f;
                    if (player.IsDead()) return;


                    if (player.WaterFactor() > 0.85f && (oxygenSource == ItemModGiveOxygen.AirSupplyType.Lungs || beathLeft < 4))
                    {
                        if (exceeded_breathing_time) return;
                        if (!IsSwimming)
                        {
                            IsSwimming = true;
                        }
                        else
                        {
                            time_breathing += 2;
                        }

                        player.metabolism.oxygen.SetValue(1f);

                        if (time_breathing > max_breathing_time)
                        {
                            exceeded_breathing_time = true;
                        }
                        UnderwaterBreathCounter(player, Convert.ToInt32(max_breathing_time - time_breathing));


                    }
                    else
                    {
                        IsSwimming = false;
                        time_breathing = 0f;
                        exceeded_breathing_time = false;
                        CuiHelper.DestroyUi(player, "UnderwaterBreathCounter");
                    }
                }
            }

            // OnDestroy() built into the monobehaviour class.
            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
            }
        }

        static string UWB_Anchor_Min;
        static string UWB_Anchor_Max;
        static string UWB_Offset_Min;
        static string UWB_Offset_Max;

        private static void UnderwaterBreathCounter(BasePlayer player, float value)
        {
            if (value < 0) value = 0;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1981132 0.1981132 0.1981132 0.8" },
                RectTransform = { AnchorMin = UWB_Anchor_Min, AnchorMax = UWB_Anchor_Max, OffsetMin = UWB_Offset_Min, OffsetMax = UWB_Offset_Max }
            }, "Hud", "UnderwaterBreathCounter");

            container.Add(new CuiElement
            {
                Name = "Image_2618",
                Parent = "UnderwaterBreathCounter",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/lungs.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Label_6078",
                Parent = "UnderwaterBreathCounter",
                Components = {
                    new CuiTextComponent { Text = value.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerCenter, Color = "1 0.8178636 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }
            });

            CuiHelper.DestroyUi(player, "UnderwaterBreathCounter");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Raiding Ultimate

        #region Command

        [ConsoleCommand("setxp")]
        void SetXPConsoleCMD(ConsoleSystem.Arg arg)
        {
            var user = arg.Player();
            if (user != null && !permission.UserHasPermission(user.UserIDString, perm_admin)) return;

            if (arg.Args.IsNullOrEmpty() || arg.Args.Length < 2)
            {
                arg.ReplyWith("Usage: /setxp <target> <amount>");
                return;
            }

            var player = BasePlayer.Find(arg.Args[0]);
            if (player == null)
            {
                List<BasePlayer> foundPlayers = Pool.GetList<BasePlayer>();
                foreach (var p in BasePlayer.activePlayerList)
                {
                    if (p.displayName.Contains(arg.Args[0]))
                    {
                        foundPlayers.Add(p);
                    }
                }

                if (foundPlayers.Count == 0) arg.ReplyWith($"No players found that matched: {arg.Args[0]}");
                else if (foundPlayers.Count > 1) arg.ReplyWith($"Found multiple matches: {string.Join(", ", foundPlayers.Select(x => x.displayName))}");
                else player = foundPlayers[0];

                Pool.FreeList(ref foundPlayers);
                if (player == null) return;
            }

            PlayerInfo data;
            if (!pcdData.pEntity.TryGetValue(player.userID, out data))
            {
                arg.ReplyWith($"There is no data stored for {player.displayName}. They must connect to the server before attempting to set xp.");
                return;
            }

            var xp = Convert.ToDouble(arg.Args[1]);
            if (xp <= 0)
            {
                arg.ReplyWith($"{arg.Args[1]} is not a valid value. Must be above 1.");
                return;
            }

            if (xp < data.xp)
            {
                arg.ReplyWith("You cannot set a lower xp value for the player. Reset their data first then run the command again.");
                return;
            }

            data.xp = xp;
            var level = config.level.GetLevel(data.xp);
            data.current_level = level;
            data.achieved_level = level;
            data.available_points = config.general_settings.points_per_level * level + config.wipe_update_settings.starting_skill_points;

            if (player.IsConnected) UpdateXP(player, data);
            arg.ReplyWith($"Updated the xp for {player.displayName}.\nXP: {data.xp}\nLevel: {data.current_level}\nAvailable points: {data.available_points}");
        }

        // getplayerinfo <Name or ID>
        [ConsoleCommand("getplayerinfo")]
        void GetPlayerInfo(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            if (arg.Args.IsNullOrEmpty())
            {
                arg.ReplyWith("Usage: getplayerinfo <Name or ID>");
                return;
            }

            var joinedArgs = string.Join(" ", arg.Args);

            PlayerInfo playerData = null;
            ulong key = 0;
            if (joinedArgs.IsNumeric())
            {
                var id = Convert.ToUInt64(joinedArgs);
                if (!pcdData.pEntity.TryGetValue(id, out playerData))
                {
                    arg.ReplyWith($"Failed to find data that matched: {joinedArgs}");
                    return;
                }
            }
            else
            {
                List<string> foundPlayers = Pool.GetList<string>();
                foreach (var kvp in pcdData.pEntity)
                {
                    if (kvp.Value.name.Contains(joinedArgs))
                    {
                        playerData = kvp.Value;
                        key = kvp.Key;
                        foundPlayers.Add(kvp.Value.name);
                    }
                }
                if (foundPlayers.Count > 1)
                {
                    arg.ReplyWith($"Found multiple players that matched: {string.Join(", ", foundPlayers)}");
                    Pool.FreeList(ref foundPlayers);
                    return;
                }
                else if (foundPlayers.Count == 0 || playerData == null)
                {
                    arg.ReplyWith($"Failed to find data that matched: {joinedArgs}");
                    Pool.FreeList(ref foundPlayers);
                    return;
                }
                Pool.FreeList(ref foundPlayers);
            }

            int pointsSpent = 0;
            foreach (var point in playerData.buff_values.Values)
                pointsSpent += point;

            arg.ReplyWith($"Data for {playerData.name} [{key}]\n- XP: {playerData.xp}\n- Current Level: {playerData.current_level}\n- Highest level achieved this wipe: {playerData.achieved_level}\n- Unspent points: {playerData.available_points}\n- Spent points: {pointsSpent}\n- Pouch items count: {playerData.pouch_items?.Count ?? 0}");
        }

        [ConsoleCommand("stresetxpdebt")]
        void ResetXPDebt(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                arg.ReplyWith(lang.GetMessage("ResetXPDebtUsage", this));
                return;
            }
            var name = String.Join(" ", arg.Args);
            var target = name.IsNumeric() ? FindPlayerByID(name, player ?? null) : FindPlayerByName(name, player ?? null);
            if (target == null) return;

            PlayerInfo pi;
            if (pcdData.pEntity.TryGetValue(target.userID, out pi))
            {
                pi.xp_debt = 0;
                UpdateXP(player, pi);
            }

            arg.ReplyWith($"Reset the xp debt for {target.displayName}");
        }

        [ChatCommand("resetmlrs")]
        void ResetMLRS(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) return;

            pi.raiding_ultimate_used_time = DateTime.MinValue;

            Player.Message(player, "Your MLRS timer has been reset.", config.misc_settings.ChatID);
        }

        [ConsoleCommand("globalresetmlrs")]
        void GlobalResetMLRS(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            foreach (var pi in pcdData.pEntity)
                pi.Value.raiding_ultimate_used_time = DateTime.MinValue;

            arg.ReplyWith($"Reset the MLRS cooldown globally.");
        }

        void CallRocketStrike(BasePlayer player)
        {
            if (config.ultimate_settings.ultimate_raiding.wipe_prevention_time > 0 && pcdData.wipeTime != DateTime.MinValue)
            {
                var timeSpan = (pcdData.wipeTime.AddHours(config.ultimate_settings.ultimate_raiding.wipe_prevention_time) - DateTime.Now).TotalMinutes;
                if (timeSpan > 0)
                {
                    //Player.Message(player, $"The server does not allow the MLRS strike to be used so close to wipe. This ability will be available in {(timeSpan > 120 ? $"{Math.Round(timeSpan / 60, 0)} hours." : timeSpan > 1 ? $"{Math.Round(timeSpan, 0)} minutes." : $"{Math.Round(timeSpan, 0)} seconds.")}");
                    Player.Message(player, string.Format(lang.GetMessage("RocketStrikeCooldownMsg", this, player.UserIDString), timeSpan > 120 ? $"{Math.Round(timeSpan / 60, 0)} {lang.GetMessage("Hours", this, player.UserIDString)}." : timeSpan > 1 ? $"{Math.Round(timeSpan, 0)} {lang.GetMessage("Minutes", this, player.UserIDString)}." : $"{Math.Round(timeSpan, 0)} {lang.GetMessage("Seconds", this, player.UserIDString)}."));
                    return;
                }
            }

            if (HasRaidBehaviour(player))
            {
                Player.Message(player, lang.GetMessage("RaidingUltimateAlreadyActive", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            if (!HasAmmo(player))
            {
                Player.Message(player, string.Format(lang.GetMessage("RaidingMissingAmmo", this, player.UserIDString), MissileQuantity));
                return;
            }

            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.ContainsKey(Buff.Raiding_Ultimate))
            {
                Player.Message(player, lang.GetMessage("RaidingUltimateNotUnlocked", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }


            if (player.inventory.containerBelt.IsFull())
            {
                Player.Message(player, lang.GetMessage("RaidingUltimateNoFreeSlot", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            PlayerInfo pi;

            if (pcdData.pEntity.TryGetValue(player.userID, out pi))
            {
                if (pi.raiding_ultimate_used_time.AddMinutes(config.ultimate_settings.ultimate_raiding.cooldown) > DateTime.Now)
                {
                    var endTime = pi.raiding_ultimate_used_time.AddMinutes(config.ultimate_settings.ultimate_raiding.cooldown);
                    TimeSpan span = endTime.Subtract(DateTime.Now);
                    Player.Message(player, string.Format(lang.GetMessage("RaidingUltimateCooldown", this, player.UserIDString), span.TotalMinutes > 1 ? Math.Round(span.TotalMinutes, 0) + " " + lang.GetMessage("minutes", this, player.UserIDString) : Math.Round(span.TotalSeconds, 0) + " " + lang.GetMessage("seconds", this, player.UserIDString)), config.misc_settings.ChatID);
                    return;
                }
                else pi.raiding_ultimate_used_time = DateTime.Now;
            }
            else return;

            var item = ItemManager.CreateByName("tool.binoculars", 1);
            item.name = "MLRS Strike";

            if (!item.MoveToContainer(player.inventory.containerBelt, -1, false))
            {
                Player.Message(player, lang.GetMessage("BinocularGiveFail", this, player.UserIDString));
                item.Remove();
                return;
            }

            NextTick(() =>
            {
                item.MarkDirty();
                item.LockUnlock(true);
            });

            Player.Message(player, lang.GetMessage("RaidingUltimateChatInstructions", this, player.UserIDString), config.misc_settings.ChatID);

            AddRaidBehaviour(player);
        }

        [ConsoleCommand("stversion")]
        void PrintVersion(ConsoleSystem.Arg arg)
        {
            arg.ReplyWith($"SkillTree version: {this.Version}");
        }

        #endregion

        #region CUI

        static private void SendInstructions(BasePlayer player, string message)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "RaidUltimateSetTargetMessage",
                Parent = "Overlay",
                Components = {
                    new CuiTextComponent { Text = message, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-299.664 -176.8", OffsetMax = "300.336 -76.8" }
                }
            });

            CuiHelper.DestroyUi(player, "RaidUltimateSetTargetMessage");
            CuiHelper.AddUi(player, container);
        }

        static private void LaunchProgress(BasePlayer player, int bars, int durationTicks)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2169811 0.2169811 0.2169811 1" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-101.664 114.3", OffsetMax = "102.336 148.3" }
            }, "Overlay", "LaunchProgress");
            var progress = (200 / durationTicks) * bars;
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1686275 0.3518807 0.7254902 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.338 -15", OffsetMax = $"{-100.338 + progress} 15" }
            }, "LaunchProgress", "bar");

            container.Add(new CuiElement
            {
                Name = "text",
                Parent = "LaunchProgress",
                Components = {
                    new CuiTextComponent { Text = "PROGRESS...", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.337 -15", OffsetMax = "100.003 15" }
                }
            });

            CuiHelper.DestroyUi(player, "LaunchProgress");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Methods

        bool HasRaidBehaviour(BasePlayer player)
        {
            var gameObject = player.GetComponent<RaidBehaviour>();
            return gameObject != null;
        }

        void AddRaidBehaviour(BasePlayer player)
        {
            DestroyRaidBehaviour(player);
            var component = player.gameObject.AddComponent<RaidBehaviour>();
            component.Instance = this;
            component.showTimeRemaining = config.ultimate_settings.ultimate_raiding.show_time_remaining;
            component.timeRemainingMessage = lang.GetMessage("MLRSTimeLeft", this, player.UserIDString);
        }

        void DestroyRaidBehaviour(BasePlayer player)
        {
            if (player.IsNpc) return;
            var gameObject = player.GetComponent<RaidBehaviour>();
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        void ResetStrikeCooldown(ulong userid, BasePlayer player = null)
        {
            pcdData.pEntity[userid].raiding_ultimate_used_time = DateTime.MinValue;
            if (player != null) Player.Message(player, lang.GetMessage("RaidingUltimateResetTimeout", this, player.UserIDString), config.misc_settings.ChatID);
        }

        #endregion

        #region Behaviour

        void SetupRaidUltimateStatics()
        {
            MaxDuration = config.ultimate_settings.ultimate_raiding.max_duration;
            DurationTicks = config.ultimate_settings.ultimate_raiding.ticks_required;
            CheckInterval = config.ultimate_settings.ultimate_raiding.tick_interval;
            MissileQuantity = config.ultimate_settings.ultimate_raiding.missile_amount;
            MissileFireDelay = config.ultimate_settings.ultimate_raiding.delay_between_rockets;
            MissileFireConfirmationEffect = config.ultimate_settings.ultimate_raiding.missile_fire_confirmation_effect;

            RaidBehaviourExpiredMessage = lang.GetMessage("RaidBehaviourExpiredMessage", this);
            RaidBehaviourSuccessMessage = lang.GetMessage("RaidBehaviourSuccessMessage", this);
        }

        public float MaxDuration;
        public int DurationTicks;
        public float CheckInterval;
        public string RaidBehaviourExpiredMessage;
        public string RaidBehaviourSuccessMessage;
        public int MissileQuantity;
        public float MissileFireDelay;
        public string MissileFireConfirmationEffect;

        // DateTime = DateTime.Now + prevention seconds
        public Dictionary<Vector3, DateTime> StrikedLocations = new Dictionary<Vector3, DateTime>();

        public Dictionary<ulong, bool> StrikeFired = new Dictionary<ulong, bool>();

        //CreateGameTip
        public class RaidBehaviour : MonoBehaviour
        {
            public SkillTree Instance;
            private BasePlayer player;
            private ulong userid;
            private int pressedDuration;
            private float nextCheck;

            public bool showTimeRemaining;
            public string timeRemainingMessage;

            private Vector3 targetPoint;
            private int fired;

            private bool striking = false;
            private Item binoculars;

            private bool wsAdmin;
            private bool sendhudhint = false;
            private float startTime;
            private bool sentInstructions = false;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                wsAdmin = player.IsAdmin;
                startTime = Time.time;
                nextCheck = Time.time + 0.5f;
                userid = player.userID;
                foreach (var item in player.inventory.containerBelt.itemList)
                {
                    if (item.info.shortname == "tool.binoculars" && item.name != null)
                    {
                        binoculars = item;
                        break;
                    }
                }
            }

            public void FixedUpdate()
            {
                if (!sentInstructions)
                {
                    SendInstructions(player, Instance.lang.GetMessage("UIBinocularsMessage", Instance, player.UserIDString));
                    sentInstructions = true;
                }
                if (player == null || !player.IsConnected)
                {
                    Destroy(this);
                    return;
                }

                if (Time.time > startTime + Instance.MaxDuration && !striking)
                {
                    CreateGameTip(Instance.RaidBehaviourExpiredMessage, player, 10);
                    CuiHelper.DestroyUi(player, "PendingTimer");
                    //player.ChatMessage(RaidBehaviourExpiredMessage);
                    DisableStrike();
                    return;
                }

                var activeItem = player.GetActiveItem();

                if (showTimeRemaining && !striking) Instance.PendingTimer(player, string.Format(timeRemainingMessage, Convert.ToInt32(startTime + Instance.MaxDuration - Time.time)));

                if (!player.serverInput.IsDown(BUTTON.FIRE_SECONDARY) || !player.serverInput.IsDown(BUTTON.USE) || activeItem == null || activeItem != binoculars)
                {
                    nextCheck = Time.time + Instance.CheckInterval;
                    pressedDuration = 0;
                    if (!player.serverInput.IsDown(BUTTON.FIRE_SECONDARY)) CuiHelper.DestroyUi(player, "LaunchProgress");
                    else LaunchProgress(player, 0, Instance.DurationTicks);
                    return;
                }

                if (Time.time > nextCheck)
                {
                    nextCheck = Time.time + Instance.CheckInterval;
                    pressedDuration++;
                    LaunchProgress(player, pressedDuration, Instance.DurationTicks);

                    RaycastHit raycastHit;
                    bool flag = Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 5000, LAYER_TARGET);
                    targetPoint = flag ? raycastHit.point : Vector3.zero;

                    if (Interface.CallHook("OnRaidingUltimateTargetAcquire", player, targetPoint) != null)
                        targetPoint = Vector3.zero;

                    if (!Instance.PassRaidableBasesCheck(targetPoint, Buff.Raiding_Ultimate))
                        targetPoint = Vector3.zero;

                    if (targetPoint == Vector3.zero)
                    {
                        ResetLaunchProgress();
                        return;
                    }

                    if (!wsAdmin)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendNetworkUpdateImmediate();
                    }
                    player.SendConsoleCommand("ddraw.text", Instance.CheckInterval, Color.red, targetPoint, "X");
                    if (!wsAdmin)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                        player.SendNetworkUpdateImmediate();
                    }

                    if (pressedDuration >= Instance.DurationTicks)
                    {
                        DoStrike();
                    }
                }
            }

            public void ResetLaunchProgress()
            {
                pressedDuration = 0;
                if (!player.serverInput.IsDown(BUTTON.FIRE_SECONDARY)) CuiHelper.DestroyUi(player, "LaunchProgress");
                else
                {
                    if (!sendhudhint)
                    {
                        CreateGameTip("Invalid target location.", player, 5);
                        sendhudhint = true;
                    }
                    LaunchProgress(player, 0, Instance.DurationTicks);
                }
            }

            public bool ValidateStrikeLocation(Vector3 loc)
            {
                if (!Instance.config.ultimate_settings.ultimate_raiding.prevent_mlrs_spamming) return true;
                foreach (var pos in Instance.StrikedLocations)
                {
                    if (InRange(pos.Key, loc, Instance.config.ultimate_settings.ultimate_raiding.prevention_radius))
                    {
                        if (pos.Value > DateTime.Now)
                        {
                            //Instance.Player.Message(player, $"Strike cancelled - location too closed to a previous strike zone. Available in: {Math.Floor((pos.Value - DateTime.Now).TotalMinutes)} minutes.", Instance.config.misc_settings.ChatID);
                            Instance.Player.Message(player, string.Format(Instance.lang.GetMessage("BinocularGiveFail", Instance, player.UserIDString), Math.Floor((pos.Value - DateTime.Now).TotalMinutes)), Instance.config.misc_settings.ChatID);
                            return false;
                        }
                        Instance.StrikedLocations.Remove(pos.Key);
                        return true;
                    }
                }
                return true;
            }

            public void DoStrike()
            {
                if (targetPoint == Vector3.zero || !ValidateStrikeLocation(targetPoint))
                {
                    ResetLaunchProgress();
                    return;
                }
                if (!Instance.HasAmmo(player, true))
                {
                    CreateGameTip($"Not enough MLRS rockets in inventory. Required amount: {Instance.MissileQuantity}", player, 7);
                    ResetLaunchProgress();
                    return;
                }
                if (!string.IsNullOrEmpty(Instance.MissileFireConfirmationEffect)) EffectNetwork.Send(new Effect(Instance.MissileFireConfirmationEffect, player.transform.position, player.transform.position), player.net.connection);
                CreateGameTip(Instance.RaidBehaviourSuccessMessage, player, 7);
                CuiHelper.DestroyUi(player, "RaidUltimateSetTargetMessage");
                CuiHelper.DestroyUi(player, "LaunchProgress");
                CuiHelper.DestroyUi(player, "PendingTimer");
                striking = true;
                Instance.StrikeFired[userid] = true;
                RemoveBinoculars();
                player.inventory.containerBelt.SetLocked(false);
                if (Instance.config.ultimate_settings.ultimate_raiding.prevent_mlrs_spamming) Instance.StrikedLocations.Add(targetPoint, DateTime.Now.AddSeconds(Instance.config.ultimate_settings.ultimate_raiding.prevention_duration));
                InvokeRepeating(nameof(FireRocket), 0f, Instance.MissileFireDelay);
            }

            public void FireRocket()
            {
                Vector3 firePoint = player.transform.position + new Vector3(0, 100, 0);
                Vector3 nVector = targetPoint - firePoint;
                var rocket = GameManager.server.CreateEntity("assets/content/vehicles/mlrs/rocket_mlrs.prefab", firePoint, Quaternion.EulerAngles(nVector)) as MLRSRocket;
                if (rocket == null)
                {
                    return;
                }
                rocket.creatorEntity = player;
                rocket.OwnerID = player.userID;

                var proj = rocket.GetComponent<ServerProjectile>();
                if (proj == null) return;
                proj.InitializeVelocity(nVector);
                rocket.Spawn();
                fired++;
                if (fired >= Instance.MissileQuantity)
                {
                    DisableStrike();
                }
            }

            public void RemoveBinoculars()
            {
                binoculars?.Remove();
            }

            public void DisableStrike()
            {
                CancelInvoke(nameof(FireRocket));
                Destroy(this);
                CuiHelper.DestroyUi(player, "RaidUltimateSetTargetMessage");
                CuiHelper.DestroyUi(player, "LaunchProgress");
            }

            private void OnDestroy()
            {
                if (player != null && player.inventory != null)
                {
                    foreach (var item in player.inventory.AllItems())
                    {
                        if (item.info.shortname == "tool.binoculars" && item.IsLocked())
                            item.Remove();
                    }
                }
                if (!Instance.StrikeFired.ContainsKey(userid) || !Instance.StrikeFired[userid]) Instance.ResetStrikeCooldown(userid, player);
                Instance.StrikeFired.Remove(player.userID);
                CuiHelper.DestroyUi(player, "RaidUltimateSetTargetMessage");
                CuiHelper.DestroyUi(player, "LaunchProgress");
                CuiHelper.DestroyUi(player, "PendingTimer");
                RemoveBinoculars();
                enabled = false;
                CancelInvoke();
            }
        }

        private Dictionary<ulong, Timer> timers = new Dictionary<ulong, Timer>();

        private static void CreateGameTip(string text, BasePlayer player, float length = 10f)
        {
            if (player == null)
                return;
            Timer timer;
            if (Instance.timers.TryGetValue(player.userID, out timer))
            {
                timer.Destroy();
            }
            player.SendConsoleCommand("gametip.hidegametip");
            player.SendConsoleCommand("gametip.showgametip", text);
            Instance.timers[player.userID] = Instance.timer.Once(length, () => player?.SendConsoleCommand("gametip.hidegametip"));
        }

        private static SkillTree Instance { get; set; }

        public bool HasAmmo(BasePlayer player, bool take = false)
        {
            if (!config.ultimate_settings.ultimate_raiding.require_ammo) return true;
            List<Item> mlrsItems = Pool.GetList<Item>();
            int ammo = 0;
            foreach (var item in player.inventory.AllItems())
            {
                if (item.info.shortname == "ammo.rocket.mlrs")
                {
                    mlrsItems.Add(item);
                    ammo += item.amount;
                }

                if (ammo >= Instance.MissileQuantity) break;
            }

            if (!take)
            {
                Pool.FreeList(ref mlrsItems);
                return ammo >= Instance.MissileQuantity;
            }
            else if (ammo < Instance.MissileQuantity) return false;

            var taken = 0;
            foreach (var item in mlrsItems)
            {
                if (item.amount == Instance.MissileQuantity - taken)
                {
                    item.Remove();
                    taken = Instance.MissileQuantity;
                }
                else if (item.amount > Instance.MissileQuantity - taken)
                {
                    item.UseItem(Instance.MissileQuantity - taken);
                    taken = Instance.MissileQuantity;
                }
                else
                {
                    taken += item.amount;
                    item.Remove();
                }
                if (taken >= Instance.MissileQuantity) break;
            }
            return true;
        }

        #endregion

        #endregion

        #endregion

        #region Instant Untie

        public class InstantUntie : MonoBehaviour
        {
            private BasePlayer player;
            private float checkDelay;
            private bool hitSuccess;

            // Awake() is part of the Monobehaviour class.
            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                checkDelay = Time.time + 0.1f;
            }

            // FixedUpdate() is also part of the monobehaviour class.
            public void FixedUpdate()
            {
                if (player == null || player.WaterFactor() < 1f) return;
                if (checkDelay < Time.time)
                {
                    if (player.serverInput.IsDown(BUTTON.USE) && !hitSuccess)
                    {
                        var target = GetTargetEntity(player);
                        if (target != null && target is FreeableLootContainer)
                        {
                            var lootContainer = target as FreeableLootContainer;
                            if (!lootContainer.IsTiedDown()) return;
                            lootContainer.buoyancy.buoyancyScale = 1;
                            lootContainer.GetRB().isKinematic = false;
                            lootContainer.buoyancy.enabled = true;
                            lootContainer.SetFlag(BaseEntity.Flags.Reserved8, false);
                            lootContainer.SendNetworkUpdate();

                            hitSuccess = true;
                        }
                    }
                    if (hitSuccess) checkDelay = Time.time + 2.0f;
                    else checkDelay = Time.time + 0.1f;
                    hitSuccess = false;
                }
            }

            // OnDestroy() built into the monobehaviour class.
            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
            }
        }

        void DestroyInstantUntie(BasePlayer player)
        {
            BreathTime.Remove(player);
            var gameObject = player.GetComponent<InstantUntie>();
            if (gameObject != null) GameObject.Destroy(gameObject);
        }

        void UpdateInstantUntie(BasePlayer player)
        {
            DestroyInstantUntie(player);
            player.gameObject.AddComponent<InstantUntie>();
        }

        private const int LAYER_TARGET = ~(1 << 2 | 1 << 3 | 1 << 4 | 1 << 10 | 1 << 18 | 1 << 28 | 1 << 29);
        private static BaseEntity GetTargetEntity(BasePlayer player)
        {
            RaycastHit raycastHit;
            bool flag = Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 5, LAYER_TARGET);
            var targetEntity = flag ? raycastHit.GetEntity() : null;
            return targetEntity;
        }

        #endregion

        #region Handle API Nodes

        bool NewNodesAdded = false;
        public Dictionary<string, Dictionary<string, Configuration.TreeInfo.NodeInfo>> QueuedNodes = new Dictionary<string, Dictionary<string, Configuration.TreeInfo.NodeInfo>>();

        // AddNode(string tree, string node, bool enabled, int max_level, int tier, float value_per_buff, string buff, string buffType, string icon_url, object[] perms { string perms_description, Dictionary<int, List<string>> tiers_and_perms })
        [HookMethod("AddNode")]
        public void AddNode(string tree, string node, bool enabled, int max_Level, int tier, float value_Per_Buff, string _buff, string _buffType, string icon_url, object[] _perms = null, ulong skin = 0, bool overwrite = false)
        {
            Buff buff;
            if (!Enum.TryParse(_buff, out buff) || buff == Buff.None) return;
            BuffType buffType;
            if (!Enum.TryParse(_buffType, out buffType)) return;
            if (string.IsNullOrEmpty(tree)) return;
            if (!config.trees.ContainsKey(tree)) return;
            if (string.IsNullOrEmpty(node)) return;
            if (config.trees[tree].nodes.ContainsKey(node) && !overwrite) return;
            if (string.IsNullOrEmpty(icon_url) && skin == 0)
            {
                Puts($"Error adding skill - requires a skinid or url");
                return;
            }

            Permissions perms = null;

            if (_perms != null && _perms.Length == 2 && !string.IsNullOrEmpty((string)_perms[0]) && (Dictionary<int, Dictionary<string, string>>)_perms[1] != null)
            {
                Dictionary<int, PermissionInfo> perms_to_add = new Dictionary<int, PermissionInfo>();
                foreach (var kvp in (Dictionary<int, Dictionary<string, string>>)_perms[1])
                {
                    perms_to_add.Add(kvp.Key, new PermissionInfo(kvp.Value));
                }
                perms = new Permissions((string)_perms[0], perms_to_add);
            }
            //new Configuration.TreeInfo(new Dictionary<string, Configuration.TreeInfo.NodeInfo>()            
            var NodeData = new Configuration.TreeInfo.NodeInfo(enabled, max_Level, tier, value_Per_Buff, new KeyValuePair<Buff, BuffType>(buff, buffType), icon_url, skin, perms);

            if (overwrite && config.trees.ContainsKey(tree) && config.trees[tree].nodes.ContainsKey(node) && IsDataIdentical(NodeData, config.trees[tree].nodes[node]))
            {
                return;
            }

            Puts($"Queuing new node {node}, to be added to tree: {tree} - Buff: {buff} - Type: {buffType}");
            if (!QueuedNodes.ContainsKey(tree)) QueuedNodes.Add(tree, new Dictionary<string, Configuration.TreeInfo.NodeInfo>());
            if (!QueuedNodes[tree].ContainsKey(node))
            {
                QueuedNodes[tree].Add(node, NodeData);
                NewNodesAdded = true;
            }

            if (overwrite && config.trees[tree].nodes.ContainsKey(node))
            {
                Puts($"Overwriting node: {node}.");
                var nodeData = config.trees[tree].nodes[node];
                if (nodeData.permissions != null && !nodeData.permissions.perms.IsNullOrEmpty())
                {
                    List<string> permsToRevoke = Pool.GetList<string>();
                    try
                    {
                        foreach (var num in nodeData.permissions.perms)
                        {
                            foreach (var perm in num.Value.perms_list)
                            {
                                permsToRevoke.Add(perm.Key);
                            }
                        }
                    }
                    catch { }

                    Puts($"Found {permsToRevoke.Count} perms to revoke. Revoking from all active players.");
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        try
                        {
                            foreach (var perm in permsToRevoke)
                                if (permission.UserHasPermission(player.UserIDString, perm)) permission.RevokeUserPermission(player.UserIDString, perm);
                        }
                        catch { }
                    }
                    Pool.FreeList(ref permsToRevoke);
                }

                config.trees[tree].nodes.Remove(node);
                OnServerSave();
            }
            //config.trees[tree].nodes.Add(node, new Configuration.TreeInfo.NodeInfo(enabled, max_level, tier, value_per_buff, new KeyValuePair<Buff, BuffType>(buff, buffType), icon_url, perms));            
        }

        bool IsDataIdentical(Configuration.TreeInfo.NodeInfo oldNodeData, Configuration.TreeInfo.NodeInfo newNodeData)
        {
            if (oldNodeData.enabled != newNodeData.enabled) return false;
            if (oldNodeData.buff_info.Key != newNodeData.buff_info.Key || oldNodeData.buff_info.Value != newNodeData.buff_info.Value) return false;
            if (oldNodeData.value_per_buff != newNodeData.value_per_buff) return false;
            if (oldNodeData.tier != newNodeData.tier) return false;
            if (oldNodeData.skin != newNodeData.skin) return false;
            if (oldNodeData.icon_url != newNodeData.icon_url) return false;
            if (oldNodeData.max_level != newNodeData.max_level) return false;
            if (oldNodeData.permissions.description != newNodeData.permissions.description) return false;
            if (oldNodeData.permissions.perms.Count != newNodeData.permissions.perms.Count) return false;
            foreach (var num in oldNodeData.permissions.perms)
            {
                if (!newNodeData.permissions.perms.ContainsKey(num.Key)) return false;
                foreach (var perm in num.Value.perms_list)
                {
                    if (!newNodeData.permissions.perms[num.Key].perms_list.ContainsKey(perm.Key)) return false;
                    else if (newNodeData.permissions.perms[num.Key].perms_list[perm.Key] != perm.Value) return false;
                }
            }
            return true;
        }

        [ConsoleCommand("addtestpermsnode")]
        void AddTestPermsNodeConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            AddTestPermsNode(player ?? null);
        }

        [ChatCommand("addtestpermsnode")]
        void AddTestPermsNode(BasePlayer player)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, "skilltree.admin")) return;
            if (!config.trees.ContainsKey("Cooking")) return;
            if (config.trees["Cooking"].nodes.ContainsKey("Test perms node")) return;

            config.trees["Cooking"].nodes.Add("Test perms node", new Configuration.TreeInfo.NodeInfo(false, 2, 2, 1, new KeyValuePair<Buff, BuffType>(Buff.Permission, BuffType.Permission), "https://www.dropbox.com/s/6blc3eiarm07rku/cooking%20tree%20example.v1.png?dl=1", 2936558732, new Permissions("This is a test node. You can add your description here. Level 1 gives instant cooking. Level 2 gives free cooking.", new Dictionary<int, PermissionInfo>()
            {
                [1] = new PermissionInfo(new Dictionary<string, string>() { ["cooking.instant"] = "Instant Cooking" }),
                [2] = new PermissionInfo(new Dictionary<string, string>() { ["cooking.instant"] = "Instant Cooking", ["cooking.free"] = "Free Cooking" })
            })));

            SaveConfig();
            if (player != null) Player.Message(player, "Saved new node called 'Test perms node' in the Cooking tree.", config.misc_settings.ChatID);
            else Puts("Saved new node called 'Test perms node' in the Cooking tree.");
        }

        #endregion

        #region Discord

        public void SendDiscordMsg(string message)
        {
            if (string.IsNullOrEmpty(config.notification_settings.discordSettings.webhook)) return;
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Content-Type", "application/json");
                string payload = "{\"content\": \"" + message + "\"}";
                webrequest.Enqueue(config.notification_settings.discordSettings.webhook, payload, (code, response) =>
                {
                    if (code != 200 && code != 204)
                    {
                        if (response == null)
                        {
                            PrintWarning($"Discord didn't respond. Error Code: {code}. Try removing escape characters from your string such as \\n.");
                        }
                    }
                }, this, Core.Libraries.RequestMethod.POST, headers);
            }
            catch (Exception e)
            {
                Puts($"Failed. Error: {e?.Message}.\nIf this error was related to Bad Request, you may need to remove any escapes from your lang such as \\n.");
            }

        }

        #endregion

        #region Cooking Ultimate

        void AddTeaBuffsCMD(BasePlayer player)
        {
            BuffDetails bd;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.ContainsKey(Buff.Cooking_Ultimate))
            {
                Player.Message(player, lang.GetMessage("CookingUltimateNotUnlocked", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            if (!IsUltimateEnabled(player, Buff.Cooking_Ultimate))
            {
                Player.Message(player, string.Format(lang.GetMessage("CookingUltimateNotEnabled", this, player.UserIDString), lang.GetMessage("UltimateSettingsButton", this, player.UserIDString)), config.misc_settings.ChatID);
                return;
            }

            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return;

            if (playerData.cooking_ultimate_used_time.AddMinutes(config.ultimate_settings.ultimate_cooking.buff_cooldown) > DateTime.Now)
            {
                var endTime = playerData.cooking_ultimate_used_time.AddMinutes(config.ultimate_settings.ultimate_cooking.buff_cooldown);
                TimeSpan span = endTime.Subtract(DateTime.Now);
                //Player.Message(player, $"You cannot use this ultimate again until <color=#ffae00>{pi.raiding_ultimate_used_time.AddMinutes(config.ultimate_settings.ultimate_raiding.cooldown).ToString("hh:mm:ss tt")}</color>.");
                Player.Message(player, string.Format(lang.GetMessage("CookingUltimateCooldownReminder", this, player.UserIDString), span.TotalMinutes > 1 ? Math.Round(span.TotalMinutes, 0) + " minutes" : Math.Round(span.TotalSeconds, 0) + " seconds"), config.misc_settings.ChatID);
                return;
            }
            playerData.cooking_ultimate_used_time = DateTime.Now;

            AddTeaBuffs(player);
        }

        void AddTeaBuffs(BasePlayer player)
        {
            List<Modifier> ExistingModifiers = Pool.GetList<Modifier>();
            List<Modifier.ModifierType> EMods = Pool.GetList<Modifier.ModifierType>();
            ExistingModifiers.AddRange(player.modifiers.All);
            foreach (var mod in ExistingModifiers)
            {
                CookingUltimate.ModifierValues entryValue;
                if (!config.ultimate_settings.ultimate_cooking.tea_mods.TryGetValue(mod.Type, out entryValue) || (mod.Value > entryValue.modifier && !config.ultimate_settings.ultimate_cooking.override_better_mod) || (mod.Duration > entryValue.duration && !config.ultimate_settings.ultimate_cooking.override_better_duration)) EMods.Add(mod.Type);
            }
            Pool.FreeList(ref ExistingModifiers);

            List<ModifierDefintion> mods = Pool.GetList<ModifierDefintion>();
            foreach (var entry in config.ultimate_settings.ultimate_cooking.tea_mods)
            {
                if (EMods.Contains(entry.Key)) continue;
                mods.Add(new ModifierDefintion
                {
                    type = entry.Key,
                    value = entry.Value.modifier,
                    duration = entry.Value.duration,
                    source = Modifier.ModifierSource.Tea
                });
            }
            Pool.FreeList(ref EMods);

            Subscription sub;
            var wasSubbed = subscriptions.TryGetValue(nameof(OnPlayerAddModifiers), out sub) ? sub.isSubscribed : false;
            if (wasSubbed) Unsubscribe(nameof(OnPlayerAddModifiers));
            player.modifiers.Add(mods);
            player.modifiers.SendChangesToClient();
            if (wasSubbed) Subscribe(nameof(OnPlayerAddModifiers));

            var messageString = lang.GetMessage("AppliedTeaMessage", this, player.UserIDString);
            foreach (var mod in config.ultimate_settings.ultimate_cooking.tea_mods)
            {
                messageString += string.Format(lang.GetMessage("AppliedTeaMessageModString", this, player.UserIDString), mod.Key.ToString().Replace('_', ' '), mod.Value.modifier * 100, mod.Value.duration / 60);
            }
            Player.Message(player, messageString, config.misc_settings.ChatID);

            Pool.FreeList(ref mods);
        }

        void RemoveCookingUltimateBuffs(BasePlayer player)
        {
            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return;

            if (playerData.cooking_ultimate_used_time.AddMinutes(config.ultimate_settings.ultimate_cooking.buff_cooldown) <= DateTime.Now)
            {
                Player.Message(player, lang.GetMessage("CookingUltimateToggleOffCooldownFail", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            List<ModifierDefintion> modsToKeep = Pool.GetList<ModifierDefintion>();

            foreach (var mod in player.modifiers.All)
            {
                CookingUltimate.ModifierValues modData;
                if (!config.ultimate_settings.ultimate_cooking.tea_mods.TryGetValue(mod.Type, out modData) || mod.Value != modData.modifier) AddModToList(mod, modsToKeep);
            }
            player.modifiers.RemoveAll();
            if (modsToKeep.Count > 0) player.modifiers.Add(modsToKeep);
            player.modifiers.SendChangesToClient();

            Pool.FreeList(ref modsToKeep);
        }

        List<ModifierDefintion> AddModToList(Modifier mod, List<ModifierDefintion> mods)
        {
            mods.Add(new ModifierDefintion
            {
                type = mod.Type,
                value = mod.Value,
                duration = mod.TimeRemaining,
                source = mod.Source
            });
            return mods;
        }

        [ChatCommand("resetcookingultimate")]
        void ResetCookingUltimate(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) return;

            pi.cooking_ultimate_used_time = DateTime.MinValue;

            Player.Message(player, "Your Cooking Ultimate timer has been reset.", config.misc_settings.ChatID);
        }

        #endregion

        #region Trap_Spotter

        void SearchForTrapsConsoleCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null) SearchForTraps(player);
        }

        void SearchForTraps(BasePlayer player)
        {
            if (!player.IsAdmin && !player.IsDeveloper && player.IsFlying)
            {
                Player.Message(player, "You cannot use this command while in noclip.", config.misc_settings.ChatID);
                return;
            }

            if (config.buff_settings.raid_perk_settings.Trap_Spotter_settings.raidable_bases_only && !PassRaidableBasesCheck(player, Buff.Trap_Spotter)) return;

            BuffDetails bd;
            float value;
            if (!buffDetails.TryGetValue(player.userID, out bd) || !bd.buff_values.TryGetValue(Buff.Trap_Spotter, out value))
            {
                Player.Message(player, lang.GetMessage("TrapSpotterNotUnlocked", this, player.UserIDString), config.misc_settings.ChatID);
                return;
            }

            PlayerInfo playerData;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return;

            if (playerData.Trap_Spotter_used_time.AddSeconds(config.buff_settings.raid_perk_settings.Trap_Spotter_settings.cooldown) > DateTime.Now)
            {
                var endTime = playerData.Trap_Spotter_used_time.AddSeconds(config.buff_settings.raid_perk_settings.Trap_Spotter_settings.cooldown);
                TimeSpan span = endTime.Subtract(DateTime.Now);
                Player.Message(player, string.Format(lang.GetMessage("TrapSpotterCooldownReminder", this, player.UserIDString), span.TotalMinutes > 1 ? Math.Round(span.TotalMinutes, 0) + " minutes" : Math.Round(span.TotalSeconds, 0) + " seconds"), config.misc_settings.ChatID);
                return;
            }
            playerData.Trap_Spotter_used_time = DateTime.Now;

            var wasAdmin = player.IsAdmin;
            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }

            var entities = FindEntitiesOfType<BaseCombatEntity>(player.transform.position, config.buff_settings.raid_perk_settings.Trap_Spotter_settings.distance);
            foreach (var entity in entities)
            {
                if (!IsTrap(entity)) continue;
                if (value < 1 && !RollSuccessful(value)) continue;

                float[] cols = config.buff_settings.raid_perk_settings.Trap_Spotter_settings.show_names ? TrapDisplayColour(entity.ShortPrefabName) : DefaultSpotterCol;
                player.SendConsoleCommand("ddraw.text", config.buff_settings.raid_perk_settings.Trap_Spotter_settings.time_on_screen, new Color(cols[0], cols[1], cols[2]), entity.transform.position, config.buff_settings.raid_perk_settings.Trap_Spotter_settings.show_names ? TrapDisplayName(entity.ShortPrefabName, player.UserIDString) : lang.GetMessage("Trap", this, player.UserIDString));
            }

            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }

            Pool.FreeList(ref entities);
        }

        string TrapDisplayName(string shortname, string id)
        {
            switch (shortname)
            {
                case "flameturret.deployed":
                case "autoturret_deployed":
                case "spikes.floor":
                case "teslacoil.deployed":
                case "beartrap":
                case "landmine":
                case "guntrap.deployed":
                    return lang.GetMessage(shortname, this, id);
                default: return lang.GetMessage("Trap", this, id);
            }
        }

        float[] TrapDisplayColour(string shortname)
        {
            float[] cols;
            if (config.buff_settings.raid_perk_settings.Trap_Spotter_settings.trap_colours.TryGetValue(shortname, out cols) && cols.Length >= 3) return cols;
            return DefaultSpotterCol;
        }

        float[] DefaultSpotterCol = new float[3] { 0.702f, 0.082f, 0.298f };

        Dictionary<string, float[]> DefaultSpotterCols
        {
            get
            {
                return new Dictionary<string, float[]>()
                {
                    ["flameturret.deployed"] = new float[3] { 0.851f, 0.514f, 0.055f },
                    ["autoturret_deployed"] = new float[3] { 0.463f, 0.506f, 0.522f },
                    ["spikes.floor"] = new float[3] { 0.741f, 0.592f, 0.604f },
                    ["teslacoil.deployed"] = new float[3] { 0.165f, 0.675f, 0.741f },
                    ["beartrap"] = new float[3] { 0.835f, 0.902f, 0.173f },
                    ["landmine"] = new float[3] { 0.969f, 0.286f, 0.847f },
                    ["guntrap.deployed"] = new float[3] { 0.357f, 0.969f, 0.286f },
                };
            }
        }

        #endregion

        #region Harmony

#if CARBON
		private Harmony _harmony;
#else
        private HarmonyInstance _harmony;
#endif

        private void Loaded()
        {
            LoadData();
#if CARBON
            _harmony = new Harmony(Name + "Patch");
            //AccessTools.Inner(typeof(SkillTree), "FishingRod_Cast_Patch");
            _harmony.PatchAll();
            // This doesn't really change.
            //foreach (var t in patchType) { new PatchProcessor(_harmony, t, HarmonyMethod.Merge(t.GetHarmonyMethods())).Patch(); }
#else
            _harmony = HarmonyInstance.Create(Name + "Patch");
            System.Type[] patchType =
            {
                AccessTools.Inner(typeof(SkillTree), "FishingRod_Cast_Patch")
            };
            // This doesn't really change.
            foreach (var t in patchType) { new PatchProcessor(_harmony, t, HarmonyMethod.Merge(t.GetHarmonyMethods())).Patch(); }
#endif
        }

        [HarmonyPatch(typeof(BaseFishingRod), "Server_RequestCast")]
        internal class FishingRod_Cast_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(BaseFishingRod __instance, ref TimeUntil ___catchTime)
            {
                if (__instance == null) return;
                var player = __instance.GetOwnerPlayer();
                BuffDetails bd;
                if (!Instance.buffDetails.TryGetValue(player.userID, out bd)) return;
                float value;

                if (bd.buff_values.TryGetValue(Buff.Bite_Speed, out value))
                {
                    ___catchTime = (TimeUntil)Mathf.Max(___catchTime - (___catchTime * value), 0.1f);
                }
                if (bd.buff_values.TryGetValue(Buff.Rod_Tension_Bonus, out value)) Instance.ApplyRodStrength(__instance, value);
            }
        }

        #endregion
    }
}
