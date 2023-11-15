using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Facepunch;
using System.IO;
using Rust;
using Oxide.Plugins.DefendableBasesExtensionMethods;

namespace Oxide.Plugins
{
    [Info("DefendableBases", "KpucTaJl", "1.1.9")]
    internal class DefendableBases : RustPlugin
    {
        #region Config
        private const bool En = true;

        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a default config...");
            _config = PluginConfig.DefaultConfig();
            _config.PluginVersion = Version;
            SaveConfig();
            Puts("Creation of the default config completed!");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.PluginVersion < Version) UpdateConfigValues();
        }

        private void UpdateConfigValues()
        {
            Puts("Config update detected! Updating config values...");
            if (_config.PluginVersion < new VersionNumber(1, 0, 4))
            {
                _config.Urls = new HashSet<ImageURL>
                {
                    new ImageURL { Name = "Tab_KpucTaJl", Url = "Images/Tab_KpucTaJl.png" },
                    new ImageURL { Name = "Clock_KpucTaJl", Url = "Images/Clock_KpucTaJl.png" },
                    new ImageURL { Name = "Plus_KpucTaJl", Url = "Images/Plus_KpucTaJl.png" },
                    new ImageURL { Name = "Bookmark_KpucTaJl", Url = "Images/Bookmark_KpucTaJl.png" },
                    new ImageURL { Name = "Tablet_KpucTaJl", Url = "Images/Tablet_KpucTaJl.png" },
                    new ImageURL { Name = "Lock_KpucTaJl", Url = "Images/Lock_KpucTaJl.png" },
                    new ImageURL { Name = "Economic", Url = "Images/Economic.png" },
                    new ImageURL { Name = "2777422016", Url = "Images/2777422016.png" },
                    new ImageURL { Name = "2777422268", Url = "Images/2777422268.png" },
                    new ImageURL { Name = "2803087918", Url = "Images/2803087918.png" },
                    new ImageURL { Name = "2777422630", Url = "Images/2777422630.png" },
                    new ImageURL { Name = "2777422812", Url = "Images/2777422812.png" },
                    new ImageURL { Name = "autoturret", Url = "Images/autoturret.png" },
                    new ImageURL { Name = "flameturret", Url = "Images/flameturret.png" },
                    new ImageURL { Name = "guntrap", Url = "Images/guntrap.png" },
                    new ImageURL { Name = "ammo.rifle", Url = "Images/ammo.rifle.png" },
                    new ImageURL { Name = "ammo.rifle.hv", Url = "Images/ammo.rifle.hv.png" },
                    new ImageURL { Name = "ammo.rifle.incendiary", Url = "Images/ammo.rifle.incendiary.png" },
                    new ImageURL { Name = "ammo.rifle.explosive", Url = "Images/ammo.rifle.explosive.png" },
                    new ImageURL { Name = "lowgradefuel", Url = "Images/lowgradefuel.png" },
                    new ImageURL { Name = "ammo.handmade.shell", Url = "Images/ammo.handmade.shell.png" },
                    new ImageURL { Name = "blueberries", Url = "Images/blueberries.png" },
                    new ImageURL { Name = "cctv.camera", Url = "Images/cctv.camera.png" },
                    new ImageURL { Name = "gears", Url = "Images/gears.png" },
                    new ImageURL { Name = "grenade.f1", Url = "Images/grenade.f1.png" },
                    new ImageURL { Name = "gunpowder", Url = "Images/gunpowder.png" },
                    new ImageURL { Name = "largemedkit", Url = "Images/largemedkit.png" },
                    new ImageURL { Name = "metal.fragments", Url = "Images/metal.fragments.png" },
                    new ImageURL { Name = "metal.refined", Url = "Images/metal.refined.png" },
                    new ImageURL { Name = "rope", Url = "Images/rope.png" },
                    new ImageURL { Name = "scrap", Url = "Images/scrap.png" },
                    new ImageURL { Name = "stones", Url = "Images/stones.png" },
                    new ImageURL { Name = "sulfur", Url = "Images/sulfur.png" },
                    new ImageURL { Name = "syringe.medical", Url = "Images/syringe.medical.png" },
                    new ImageURL { Name = "targeting.computer", Url = "Images/targeting.computer.png" },
                    new ImageURL { Name = "wood", Url = "Images/wood.png" }
                };
            }
            if (_config.PluginVersion < new VersionNumber(1, 1, 1))
            {
                _config.Juggernaut = new JuggernautConfig
                {
                    WearItems = new List<NpcWear>
                    {
                        new NpcWear { ShortName = "frankensteins.monster.03.head", SkinID = 0 },
                        new NpcWear { ShortName = "frankensteins.monster.03.torso", SkinID = 0 },
                        new NpcWear { ShortName = "frankensteins.monster.03.legs", SkinID = 0 }
                    },
                    Weapons = new List<ProjectileBelt>
                    {
                        new ProjectileBelt { ShortName = "hmlmg", SkinID = 0, Ammo = string.Empty, Mods = new List<string> { "weapon.mod.holosight", "weapon.mod.flashlight" }, Chance = 100f, DamageScale = 0.4f }
                    },
                    BeancanGrenade = 15f,
                    Health = 250f,
                    AttackRangeMultiplier = 1f,
                    AimConeScale = 1.3f,
                    Speed = 5f,
                    TurretDamageScale = 0.25f
                };
                _config.WeaponToScaleDamageNpc = new Dictionary<string, float>
                {
                    ["grenade.beancan.deployed"] = 0.5f,
                    ["grenade.f1.deployed"] = 0.5f,
                    ["explosive.satchel.deployed"] = 0.5f,
                    ["explosive.timed.deployed"] = 0.5f,
                    ["rocket_hv"] = 0.5f,
                    ["rocket_basic"] = 0.5f,
                    ["40mm_grenade_he"] = 0.5f
                };
            }
            if (_config.PluginVersion < new VersionNumber(1, 1, 3))
            {
                _config.Urls.Add(new ImageURL { Name = "Npc_KpucTaJl", Url = "Images/Npc_KpucTaJl.png" });
                _config.Commands = new HashSet<string>
                {
                    "/remove",
                    "remove.toggle"
                };
            }
            if (_config.PluginVersion < new VersionNumber(1, 1, 8))
            {
                _config.Bomber = new BomberConfig
                {
                    WearItems = new List<NpcWear>
                    {
                        new NpcWear { ShortName = "scarecrow.suit", SkinID = 0 },
                        new NpcWear { ShortName = "metal.plate.torso", SkinID = 860210174 },
                    },
                    Health = 100f,
                    AttackRangeMultiplier = 1f,
                    Speed = 5.5f,
                    DamageBarricade = 200f,
                    DamageGeneral = 60f,
                    DamagePlayer = 60f
                };
            }
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class NpcWear
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
        }

        public class SledgeBelt
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Probability Percent [0.0-100.0]" : "Вероятность [0.0-100.0]")] public float Chance { get; set; }
        }

        public class SledgeConfig
        {
            [JsonProperty(En ? "Worn items" : "Одежда")] public List<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Weapons" : "Оружие")] public List<SledgeBelt> Weapons { get; set; }
            [JsonProperty(En ? "The probability of the appearance of Beancan Grenades" : "Вероятность появления бобовой гранаты")] public float BeancanGrenade { get; set; }
            [JsonProperty(En ? "Health Points" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Damage Scale" : "Множитель урона")] public float DamageScale { get; set; }
            [JsonProperty(En ? "Speed" : "Скорость")] public float Speed { get; set; }
            [JsonProperty(En ? "NPC damage each second to Barricade or The General" : "Кол-во урона, которое наносит NPC по баррикаде или генералу в секунду")] public float DamagePerSec { get; set; }
            [JsonProperty(En ? "Turret damage scale" : "Множитель урона от турелей")] public float TurretDamageScale { get; set; }
        }

        public class ProjectileBelt
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Mods" : "Модификации на оружие")] public List<string> Mods { get; set; }
            [JsonProperty(En ? "Ammo" : "Боеприпасы")] public string Ammo { get; set; }
            [JsonProperty(En ? "Probability Percent [0.0-100.0]" : "Вероятность [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Damage Scale" : "Множитель урона")] public float DamageScale { get; set; }
        }

        public class BlazerConfig
        {
            [JsonProperty(En ? "Worn items" : "Одежда")] public List<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Weapons" : "Оружие")] public List<ProjectileBelt> Weapons { get; set; }
            [JsonProperty(En ? "Health Points" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float AttackRangeMultiplier { get; set; }
            [JsonProperty(En ? "Aim Cone Scale" : "Множитель разброса")] public float AimConeScale { get; set; }
            [JsonProperty(En ? "Speed" : "Скорость")] public float Speed { get; set; }
        }

        public class JuggernautConfig
        {
            [JsonProperty(En ? "Worn items" : "Одежда")] public List<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Weapons" : "Оружие")] public List<ProjectileBelt> Weapons { get; set; }
            [JsonProperty(En ? "The probability of the appearance of Beancan Grenades" : "Вероятность появления бобовой гранаты")] public float BeancanGrenade { get; set; }
            [JsonProperty(En ? "Health Points" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float AttackRangeMultiplier { get; set; }
            [JsonProperty(En ? "Aim Cone Scale" : "Множитель разброса")] public float AimConeScale { get; set; }
            [JsonProperty(En ? "Speed" : "Скорость")] public float Speed { get; set; }
            [JsonProperty(En ? "Turret damage scale" : "Множитель урона от турелей")] public float TurretDamageScale { get; set; }
        }

        public class BomberConfig
        {
            [JsonProperty(En ? "Worn items" : "Одежда")] public List<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Health Points" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float AttackRangeMultiplier { get; set; }
            [JsonProperty(En ? "Speed" : "Скорость")] public float Speed { get; set; }
            [JsonProperty(En ? "NPC damage to Barricade" : "Кол-во урона, которое наносит NPC по баррикаде")] public float DamageBarricade { get; set; }
            [JsonProperty(En ? "NPC damage to The General" : "Кол-во урона, которое наносит NPC по генералу")] public float DamageGeneral { get; set; }
            [JsonProperty(En ? "NPC damage to player" : "Кол-во урона, которое наносит NPC по игроку")] public float DamagePlayer { get; set; }
        }

        public class ColorConfig
        {
            [JsonProperty("r")] public float R { get; set; }
            [JsonProperty("g")] public float G { get; set; }
            [JsonProperty("b")] public float B { get; set; }
        }

        public class MarkerConfig
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Radius" : "Радиус")] public float Radius { get; set; }
            [JsonProperty(En ? "Transparency" : "Прозрачность")] public float Alpha { get; set; }
            [JsonProperty(En ? "Marker color" : "Цвет маркера")] public ColorConfig Color { get; set; }
        }

        public class GuiAnnouncementsConfig
        {
            [JsonProperty(En ? "Do you use the GUI Announcements plugin? [true/false]" : "Использовать ли плагин GUI Announcements? [true/false]")] public bool IsGuiAnnouncements { get; set; }
            [JsonProperty(En ? "Banner color" : "Цвет баннера")] public string BannerColor { get; set; }
            [JsonProperty(En ? "Text color" : "Цвет текста")] public string TextColor { get; set; }
            [JsonProperty(En ? "Adjust Vertical Position" : "Отступ от верхнего края")] public float ApiAdjustVPosition { get; set; }
        }

        public class NotifyConfig
        {
            [JsonProperty(En ? "Do you use the Notify plugin? [true/false]" : "Использовать ли плагин Notify? [true/false]")] public bool IsNotify { get; set; }
            [JsonProperty(En ? "Type" : "Тип")] public string Type { get; set; }
        }

        public class DiscordConfig
        {
            [JsonProperty(En ? "Do you use the Discord Messages plugin? [true/false]" : "Использовать ли плагин Discord Messages? [true/false]")] public bool IsDiscord { get; set; }
            [JsonProperty("Webhook URL")] public string WebhookUrl { get; set; }
            [JsonProperty(En ? "Embed Color (DECIMAL)" : "Цвет полосы (DECIMAL)")] public int EmbedColor { get; set; }
            [JsonProperty(En ? "Keys of required messages" : "Ключи необходимых сообщений")] public HashSet<string> Keys { get; set; }
        }

        public class BarricadeConfig
        {
            [JsonProperty(En ? "Level" : "Уровень")] public int Level { get; set; }
            [JsonProperty(En ? "Hit points" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Price for repairs (per hammer hit)" : "Цена за ремонт (один удар киянкой)")] public PriceConfig Price { get; set; }
            [JsonProperty(En ? "Hit points recovered (per hammer hit)" : "Кол-во ХП ремонта (один удар киянкой)")] public float HealthRepair { get; set; }
        }

        public class ScaleDamageConfig
        {
            [JsonProperty(En ? "Type of target" : "Тип цели")] public string Type { get; set; }
            [JsonProperty(En ? "Damage Multiplier" : "Множитель урона")] public float Scale { get; set; }
        }

        public class PveModeConfig
        {
            [JsonProperty(En ? "Use the PVE mode of the plugin? [true/false]" : "Использовать PVE режим работы плагина? [true/false]")] public bool Pve { get; set; }
            [JsonProperty(En ? "The amount of damage that the player has to do to become the Event Owner" : "Кол-во урона, которое должен нанести игрок, чтобы стать владельцем ивента")] public float Damage { get; set; }
            [JsonProperty(En ? "Damage coefficients for calculate to become the Event Owner" : "Коэффициенты урона для подсчета, чтобы стать владельцем события")] public HashSet<ScaleDamageConfig> ScaleDamage { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event loot the crates? [true/false]" : "Может ли не владелец ивента грабить ящики? [true/false]")] public bool LootCrate { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event hack locked crates? [true/false]" : "Может ли не владелец ивента взламывать заблокированные ящики? [true/false]")] public bool HackCrate { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event loot NPC corpses? [true/false]" : "Может ли не владелец ивента грабить трупы NPC? [true/false]")] public bool LootNpc { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event deal damage to the NPC? [true/false]" : "Может ли не владелец ивента наносить урон по NPC? [true/false]")] public bool DamageNpc { get; set; }
            [JsonProperty(En ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool TargetNpc { get; set; }
            [JsonProperty(En ? "Allow the non-owner of the event to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента не владельцу ивента? [true/false]")] public bool CanEnter { get; set; }
            [JsonProperty(En ? "Allow a player who has an active cooldown of the Event Owner to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента игроку, у которого активен кулдаун на получение статуса владельца ивента? [true/false]")] public bool CanEnterCooldownPlayer { get; set; }
            [JsonProperty(En ? "The time that the Event Owner may not be inside the event zone [sec.]" : "Время, которое владелец ивента может не находиться внутри зоны ивента [сек.]")] public int TimeExitOwner { get; set; }
            [JsonProperty(En ? "The time until the end of Event Owner status when it is necessary to warn the player [sec.]" : "Время таймера до окончания действия статуса владельца ивента, когда необходимо предупредить игрока [сек.]")] public int AlertTime { get; set; }
            [JsonProperty(En ? "Prevent the actions of the RestoreUponDeath plugin in the event zone? [true/false]" : "Запрещать работу плагина RestoreUponDeath в зоне действия ивента? [true/false]")] public bool RestoreUponDeath { get; set; }
            [JsonProperty(En ? "The time that the player can`t become the Event Owner, after the end of the event and the player was its owner [sec.]" : "Время, которое игрок не сможет стать владельцем ивента, после того как ивент окончен и игрок был его владельцем [sec.]")] public double CooldownOwner { get; set; }
            [JsonProperty(En ? "Darkening the dome (0 - disables the dome)" : "Затемнение купола (0 - отключает купол)")] public int Darkening { get; set; }
        }

        public class ImageURL
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Path" : "Путь")] public string Url { get; set; }
        }
        
        private class PluginConfig
        {
            [JsonProperty(En ? "NPC Configuration - Sledge" : "Конфигурация NPC - Sledge")] public SledgeConfig Sledge { get; set; }
            [JsonProperty(En ? "NPC Configuration - Blazer" : "Конфигурация NPC - Blazer")] public BlazerConfig Blazer { get; set; }
            [JsonProperty(En ? "NPC Configuration - Juggernaut" : "Конфигурация NPC - Juggernaut")] public JuggernautConfig Juggernaut { get; set; }
            [JsonProperty(En ? "NPC Configuration - Bomber" : "Конфигурация NPC - Bomber")] public BomberConfig Bomber { get; set; }
            [JsonProperty(En ? "NPC damage multipliers depending on the attacker's weapon" : "Множители урона по NPC в зависимости от оружия атакующего")] public Dictionary<string, float> WeaponToScaleDamageNpc { get; set; }
            [JsonProperty(En ? "Custom Barricades" : "Настройки баррикад")] public HashSet<BarricadeConfig> CustomBarricades { get; set; }
            [JsonProperty(En ? "List of paths to GUI images" : "Список путей на картинки для GUI")] public HashSet<ImageURL> Urls { get; set; }
            [JsonProperty(En ? "Minimum time between events [sec.]" : "Минимальное время между ивентами [sec.]")] public float MinStartTime { get; set; }
            [JsonProperty(En ? "Maximum time between events [sec.]" : "Максимальное время между ивентами [sec.]")] public float MaxStartTime { get; set; }
            [JsonProperty(En ? "Is there a timer to start the event? [true/false]" : "Активен ли таймер для запуска ивента? [true/false]")] public bool EnabledTimer { get; set; }
            [JsonProperty(En ? "Time before the starting of the event after receiving a chat message [sec.]" : "Время до начала ивента после сообщения в чате [sec.]")] public float PreStartTime { get; set; }
            [JsonProperty(En ? "Time of notification signaling the end of the event [sec.]" : "Время оповещения до окончания ивента [sec.]")] public int PreFinishTime { get; set; }
            [JsonProperty(En ? "The duration of the event, if no one calls The General for help [sec.]" : "Время существования ивента, если никто не вызвал подмогу генералу [sec.]")] public int Duration { get; set; }
            [JsonProperty(En ? "Marker configuration on the map" : "Настройка маркера на карте")] public MarkerConfig Marker { get; set; }
            [JsonProperty(En ? "Prefix of chat messages" : "Префикс сообщений в чате")] public string Prefix { get; set; }
            [JsonProperty(En ? "Do you use global chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool IsChat { get; set; }
            [JsonProperty(En ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GuiAnnouncementsConfig GuiAnnouncements { get; set; }
            [JsonProperty(En ? "Notify setting" : "Настройка Notify")] public NotifyConfig Notify { get; set; }
            [JsonProperty(En ? "Discord setting (only for users DiscordMessages plugin)" : "Настройка оповещений в Discord (только для тех, кто использует плагин DiscordMessages)")] public DiscordConfig Discord { get; set; }
            [JsonProperty(En ? "List of prefabs to be deleted in the event zone" : "Список prefab-ов, которые необходимо удалять в зоне ивента")] public HashSet<string> DeletePrefabs { get; set; }
            [JsonProperty(En ? "Do you create a PVP zone in the event area? (only for users TruePVE plugin) [true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool IsCreateZonePvp { get; set; }
            [JsonProperty(En ? "PVE Mode Setting (only for users PveMode plugin)" : "Настройка PVE режима работы плагина (только для тех, кто использует плагин PveMode)")] public PveModeConfig PveMode { get; set; }
            [JsonProperty(En ? "Interrupt the teleport in the event area? (only for users NTeleportation plugin) [true/false]" : "Запрещать телепорт в зоне проведения ивента? (только для тех, кто использует плагин NTeleportation) [true/false]")] public bool NTeleportationInterrupt { get; set; }
            [JsonProperty(En ? "Disable NPCs from the BetterNpc plugin on the monument while the event is on? [true/false]" : "Отключать NPC из плагина BetterNpc на монументе пока проходит ивент? [true/false]")] public bool RemoveBetterNpc { get; set; }
            [JsonProperty(En ? "List of commands banned in the event zone" : "Список команд запрещенных в зоне ивента")] public HashSet<string> Commands { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Sledge = new SledgeConfig
                    {
                        WearItems = new List<NpcWear>
                        {
                            new NpcWear { ShortName = "frankensteins.monster.01.head", SkinID = 0 },
                            new NpcWear { ShortName = "frankensteins.monster.01.torso", SkinID = 0 },
                            new NpcWear { ShortName = "frankensteins.monster.01.legs", SkinID = 0 }
                        },
                        Weapons = new List<SledgeBelt>
                        {
                            new SledgeBelt { ShortName = "paddle", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "spear.stone", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "bone.club", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "knife.combat", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "longsword", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "pitchfork", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "knife.bone", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "mace", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "salvaged.cleaver", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "knife.butcher", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "machete", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "salvaged.sword", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "spear.wooden", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "stone.pickaxe", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "torch", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "axe.salvaged", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "pickaxe", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "hammer.salvaged", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "hatchet", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "stonehatchet", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "icepick.salvaged", SkinID = 0, Chance = 100f },
                            new SledgeBelt { ShortName = "sickle", SkinID = 0, Chance = 100f }
                        },
                        BeancanGrenade = 15f,
                        Health = 50f,
                        DamageScale = 0.6f,
                        Speed = 8f,
                        DamagePerSec = 10f,
                        TurretDamageScale = 0.25f
                    },
                    Blazer = new BlazerConfig
                    {
                        WearItems = new List<NpcWear>
                        {
                            new NpcWear { ShortName = "frankensteins.monster.02.head", SkinID = 0 },
                            new NpcWear { ShortName = "frankensteins.monster.02.torso", SkinID = 0 },
                            new NpcWear { ShortName = "frankensteins.monster.02.legs", SkinID = 0 }
                        },
                        Weapons = new List<ProjectileBelt>
                        {
                            new ProjectileBelt { ShortName = "bow.compound", SkinID = 0, Ammo = "arrow.fire", Mods = new List<string>(), Chance = 100f, DamageScale = 0.07f },
                            new ProjectileBelt { ShortName = "shotgun.waterpipe", SkinID = 0, Ammo = "ammo.shotgun.slug", Mods = new List<string> { "weapon.mod.small.scope" }, Chance = 100f, DamageScale = 0.3f }
                        },
                        Health = 40f,
                        AttackRangeMultiplier = 10f,
                        AimConeScale = 2f,
                        Speed = 7.5f
                    },
                    Juggernaut = new JuggernautConfig
                    {
                        WearItems = new List<NpcWear>
                        {
                            new NpcWear { ShortName = "frankensteins.monster.03.head", SkinID = 0 },
                            new NpcWear { ShortName = "frankensteins.monster.03.torso", SkinID = 0 },
                            new NpcWear { ShortName = "frankensteins.monster.03.legs", SkinID = 0 }
                        },
                        Weapons = new List<ProjectileBelt>
                        {
                            new ProjectileBelt { ShortName = "hmlmg", SkinID = 0, Ammo = string.Empty, Mods = new List<string> { "weapon.mod.holosight", "weapon.mod.flashlight" }, Chance = 100f, DamageScale = 0.4f }
                        },
                        BeancanGrenade = 15f,
                        Health = 250f,
                        AttackRangeMultiplier = 1f,
                        AimConeScale = 1.3f,
                        Speed = 5f,
                        TurretDamageScale = 0.25f
                    },
                    Bomber = new BomberConfig
                    {
                        WearItems = new List<NpcWear>
                        {
                            new NpcWear { ShortName = "scarecrow.suit", SkinID = 0 },
                            new NpcWear { ShortName = "metal.plate.torso", SkinID = 860210174 },
                        },
                        Health = 100f,
                        AttackRangeMultiplier = 1f,
                        Speed = 5.5f,
                        DamageBarricade = 200f,
                        DamageGeneral = 60f,
                        DamagePlayer = 60f
                    },
                    WeaponToScaleDamageNpc = new Dictionary<string, float>
                    {
                        ["grenade.beancan.deployed"] = 0.5f,
                        ["grenade.f1.deployed"] = 0.5f,
                        ["explosive.satchel.deployed"] = 0.5f,
                        ["explosive.timed.deployed"] = 0.5f,
                        ["rocket_hv"] = 0.5f,
                        ["rocket_basic"] = 0.5f,
                        ["40mm_grenade_he"] = 0.5f
                    },
                    CustomBarricades = new HashSet<BarricadeConfig>
                    {
                        new BarricadeConfig
                        {
                            Level = 0,
                            Health = 500f,
                            Price = new PriceConfig
                            {
                                Type = 3,
                                CountEconomy = 0,
                                Items = new List<ItemPriceConfig>
                                {
                                    new ItemPriceConfig
                                    {
                                        ShortName = "wood",
                                        Amount = 30,
                                        SkinID = 0
                                    }
                                }
                            },
                            HealthRepair = 50f
                        },
                        new BarricadeConfig
                        {
                            Level = 1,
                            Health = 1000f,
                            Price = new PriceConfig
                            {
                                Type = 3,
                                CountEconomy = 0,
                                Items = new List<ItemPriceConfig>
                                {
                                    new ItemPriceConfig
                                    {
                                        ShortName = "wood",
                                        Amount = 50,
                                        SkinID = 0
                                    }
                                }
                            },
                            HealthRepair = 50f
                        },
                        new BarricadeConfig
                        {
                            Level = 2,
                            Health = 1500f,
                            Price = new PriceConfig
                            {
                                Type = 3,
                                CountEconomy = 0,
                                Items = new List<ItemPriceConfig>
                                {
                                    new ItemPriceConfig
                                    {
                                        ShortName = "wood",
                                        Amount = 50,
                                        SkinID = 0
                                    },
                                    new ItemPriceConfig
                                    {
                                        ShortName = "stones",
                                        Amount = 50,
                                        SkinID = 0
                                    }
                                }
                            },
                            HealthRepair = 75f
                        },
                        new BarricadeConfig
                        {
                            Level = 3,
                            Health = 2000f,
                            Price = new PriceConfig
                            {
                                Type = 3,
                                CountEconomy = 0,
                                Items = new List<ItemPriceConfig>
                                {
                                    new ItemPriceConfig
                                    {
                                        ShortName = "wood",
                                        Amount = 50,
                                        SkinID = 0
                                    },
                                    new ItemPriceConfig
                                    {
                                        ShortName = "metal.fragments",
                                        Amount = 100,
                                        SkinID = 0
                                    }
                                }
                            },
                            HealthRepair = 100f
                        },
                        new BarricadeConfig
                        {
                            Level = 4,
                            Health = 2500f,
                            Price = new PriceConfig
                            {
                                Type = 3,
                                CountEconomy = 0,
                                Items = new List<ItemPriceConfig>
                                {
                                    new ItemPriceConfig
                                    {
                                        ShortName = "wood",
                                        Amount = 50,
                                        SkinID = 0
                                    },
                                    new ItemPriceConfig
                                    {
                                        ShortName = "metal.refined",
                                        Amount = 2,
                                        SkinID = 0
                                    }
                                }
                            },
                            HealthRepair = 100f
                        }
                    },
                    Urls = new HashSet<ImageURL>
                    {
                        new ImageURL { Name = "Tab_KpucTaJl", Url = "Images/Tab_KpucTaJl.png" },
                        new ImageURL { Name = "Clock_KpucTaJl", Url = "Images/Clock_KpucTaJl.png" },
                        new ImageURL { Name = "Plus_KpucTaJl", Url = "Images/Plus_KpucTaJl.png" },
                        new ImageURL { Name = "Npc_KpucTaJl", Url = "Images/Npc_KpucTaJl.png" },
                        new ImageURL { Name = "Bookmark_KpucTaJl", Url = "Images/Bookmark_KpucTaJl.png" },
                        new ImageURL { Name = "Tablet_KpucTaJl", Url = "Images/Tablet_KpucTaJl.png" },
                        new ImageURL { Name = "Lock_KpucTaJl", Url = "Images/Lock_KpucTaJl.png" },
                        new ImageURL { Name = "Economic", Url = "Images/Economic.png" },
                        new ImageURL { Name = "2777422016", Url = "Images/2777422016.png" },
                        new ImageURL { Name = "2777422268", Url = "Images/2777422268.png" },
                        new ImageURL { Name = "2803087918", Url = "Images/2803087918.png" },
                        new ImageURL { Name = "2777422630", Url = "Images/2777422630.png" },
                        new ImageURL { Name = "2777422812", Url = "Images/2777422812.png" },
                        new ImageURL { Name = "autoturret", Url = "Images/autoturret.png" },
                        new ImageURL { Name = "flameturret", Url = "Images/flameturret.png" },
                        new ImageURL { Name = "guntrap", Url = "Images/guntrap.png" },
                        new ImageURL { Name = "ammo.rifle", Url = "Images/ammo.rifle.png" },
                        new ImageURL { Name = "ammo.rifle.hv", Url = "Images/ammo.rifle.hv.png" },
                        new ImageURL { Name = "ammo.rifle.incendiary", Url = "Images/ammo.rifle.incendiary.png" },
                        new ImageURL { Name = "ammo.rifle.explosive", Url = "Images/ammo.rifle.explosive.png" },
                        new ImageURL { Name = "lowgradefuel", Url = "Images/lowgradefuel.png" },
                        new ImageURL { Name = "ammo.handmade.shell", Url = "Images/ammo.handmade.shell.png" },
                        new ImageURL { Name = "blueberries", Url = "Images/blueberries.png" },
                        new ImageURL { Name = "cctv.camera", Url = "Images/cctv.camera.png" },
                        new ImageURL { Name = "gears", Url = "Images/gears.png" },
                        new ImageURL { Name = "grenade.f1", Url = "Images/grenade.f1.png" },
                        new ImageURL { Name = "gunpowder", Url = "Images/gunpowder.png" },
                        new ImageURL { Name = "largemedkit", Url = "Images/largemedkit.png" },
                        new ImageURL { Name = "metal.fragments", Url = "Images/metal.fragments.png" },
                        new ImageURL { Name = "metal.refined", Url = "Images/metal.refined.png" },
                        new ImageURL { Name = "rope", Url = "Images/rope.png" },
                        new ImageURL { Name = "scrap", Url = "Images/scrap.png" },
                        new ImageURL { Name = "stones", Url = "Images/stones.png" },
                        new ImageURL { Name = "sulfur", Url = "Images/sulfur.png" },
                        new ImageURL { Name = "syringe.medical", Url = "Images/syringe.medical.png" },
                        new ImageURL { Name = "targeting.computer", Url = "Images/targeting.computer.png" },
                        new ImageURL { Name = "wood", Url = "Images/wood.png" }
                    },
                    MinStartTime = 10800f,
                    MaxStartTime = 10800f,
                    EnabledTimer = true,
                    PreStartTime = 300f,
                    PreFinishTime = 300,
                    Duration = 1200,
                    Marker = new MarkerConfig
                    {
                        Name = "DefendableBases ({wave} wave)",
                        Radius = 0.4f,
                        Alpha = 0.6f,
                        Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
                    },
                    Prefix = "[DefendableBases]",
                    IsChat = true,
                    GuiAnnouncements = new GuiAnnouncementsConfig
                    {
                        IsGuiAnnouncements = false,
                        BannerColor = "Orange",
                        TextColor = "White",
                        ApiAdjustVPosition = 0.03f
                    },
                    Notify = new NotifyConfig
                    {
                        IsNotify = false,
                        Type = "0"
                    },
                    Discord = new DiscordConfig
                    {
                        IsDiscord = false,
                        WebhookUrl = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                        EmbedColor = 13516583,
                        Keys = new HashSet<string>
                        {
                            "PreStart",
                            "Start",
                            "CallForAssistance",
                            "UpgradeBase",
                            "GeneralKill",
                            "Finish",
                            "GeneralEvacuationStart",
                            "GeneralEvacuationEnd",
                            "DoorOpen",
                            "AllPassword"
                        }
                    },
                    DeletePrefabs = new HashSet<string>
                    {
                        "minicopter.entity",
                        "douglas_fir_d",
                        "american_beech_b",
                        "pine_c",
                        "american_beech_a",
                        "sulfur-collectable",
                        "stone-collectable",
                        "metal-collectable",
                        "loot-barrel-1",
                        "loot-barrel-2",
                        "bush_spicebush_d",
                        "scraptransporthelicopter",
                        "hotairballoon",
                        "rowboat",
                        "rhib",
                        "submarinesolo.entity",
                        "submarineduo.entity",
                        "sled.deployed",
                        "magnetcrane.entity",
                        "sedantest.entity",
                        "2module_car_spawned.entity",
                        "3module_car_spawned.entity",
                        "4module_car_spawned.entity",
                        "wolf",
                        "chicken",
                        "boar",
                        "stag",
                        "bear",
                        "testridablehorse",
                        "servergibs_bradley",
                        "servergibs_patrolhelicopter"
                    },
                    IsCreateZonePvp = false,
                    PveMode = new PveModeConfig
                    {
                        Pve = false,
                        Damage = 500f,
                        ScaleDamage = new HashSet<ScaleDamageConfig>
                        {
                            new ScaleDamageConfig { Type = "NPC", Scale = 1f }
                        },
                        LootCrate = false,
                        HackCrate = false,
                        LootNpc = false,
                        DamageNpc = false,
                        TargetNpc = false,
                        CanEnter = false,
                        CanEnterCooldownPlayer = true,
                        TimeExitOwner = 300,
                        AlertTime = 60,
                        RestoreUponDeath = true,
                        CooldownOwner = 86400,
                        Darkening = 12
                    },
                    NTeleportationInterrupt = true,
                    RemoveBetterNpc = true,
                    Commands = new HashSet<string>
                    {
                        "/remove",
                        "remove.toggle"
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion Config

        #region Data
        public class ItemConfig
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int MinAmount { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int MaxAmount { get; set; }
            [JsonProperty(En ? "Chance probability [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Is this a blueprint? [true/false]" : "Это чертеж? [true/false]")] public bool IsBluePrint { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Name (empty - default)" : "Название (empty - default)")] public string Name { get; set; }
        }

        public class LootTableConfig
        {
            [JsonProperty(En ? "Minimum number of items" : "Минимальное кол-во элементов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum number of items" : "Максимальное кол-во элементов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of items" : "Список предметов")] public List<ItemConfig> Items { get; set; }
        }

        public class PrefabConfig
        {
            [JsonProperty(En ? "Chance probability [0.0-100.0]" : "Шанс выпадения [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Prefab path" : "Путь к prefab-у")] public string PrefabDefinition { get; set; }
        }

        public class PrefabLootTableConfig
        {
            [JsonProperty(En ? "Minimum number of prefabs" : "Минимальное кол-во prefab-ов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum number of prefabs" : "Максимальное кол-во prefab-ов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of prefabs" : "Список prefab-ов")] public List<PrefabConfig> Prefabs { get; set; }
        }

        public class CrateConfig
        {
            [JsonProperty("Prefab")] public string Prefab { get; set; }
            [JsonProperty(En ? "Position" : "Позиция")] public string Position { get; set; }
            [JsonProperty(En ? "Rotation" : "Вращение")] public string Rotation { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own custom loot table; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table using Rust prefabs; 5 - combination of method 1 and 4)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class ItemPriceConfig
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Amount" : "Кол-во")] public int Amount { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
        }

        public class PriceConfig
        {
            [JsonProperty(En ? "Which economy should the plugin use? (0 - Economics; 1 - Server Rewards; 2 - IQEconomic; 3 - custom list of items)" : "Какой тип цены необходимо использовать? (0 - Economics; 1 - Server Rewards; 2 - IQEconomic; 3 - список предметов)")] public int Type { get; set; }
            [JsonProperty(En ? "Price (if the economy method is 0, 1, 2)" : "Кол-во экономики (если тип цены - 0, 1, 2)")] public double CountEconomy { get; set; }
            [JsonProperty(En ? "List of items, 3 maximum (if the economy method is 3)" : "Список предметов. Не более 3 предметов (если тип цены - 3)")] public List<ItemPriceConfig> Items { get; set; }
        }

        public class RoomConfig
        {
            [JsonProperty(En ? "Door position (not edited)" : "Расположение двери (не редактируется)")] public string DoorPosition { get; set; }
            [JsonProperty(En ? "List of crates" : "Список ящиков")] public HashSet<CrateConfig> Crates { get; set; }
            [JsonProperty(En ? "The door opens after this attack wave" : "Уровень волны атаки, после завершения которой открывается дверь")] public int LevelWave { get; set; }
        }

        public class AmmoConfig
        {
            [JsonProperty(En ? "Name (not edited)" : "Название (не редактируется)")] public string Name { get; set; }
            [JsonProperty(En ? "Enabled? [true/false]" : "Включен? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Price" : "Цена")] public PriceConfig Price { get; set; }
            [JsonProperty(En ? "Maximum number of ammo" : "Максимальное кол-во патронов")] public int MaxCount { get; set; }
            [JsonProperty(En ? "Ammo amount each purchase" : "Кол-во патронов за указанную цену")] public int Count { get; set; }
        }

        public class AmmoStartConfig
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Amount" : "Кол-во")] public int Amount { get; set; }
        }

        public class TurretConfig
        {
            [JsonProperty(En ? "Name (not edited)" : "Название (не редактируется)")] public string Name { get; set; }
            [JsonProperty(En ? "Enabled? [true/false]" : "Включен? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Price to unlock" : "Цена разблокировки")] public PriceConfig Price { get; set; }
            [JsonProperty(En ? "List of doors that open when unlocking (not edited)" : "Список дверей, которые открываются при разблокировки (не редактируется)")] public HashSet<string> Doors { get; set; }
            [JsonProperty(En ? "Amount of ammo when turret is unlocked" : "Кол-во патронов во время разблокировки турели")] public HashSet<AmmoStartConfig> AmmoStart { get; set; }
            [JsonProperty(En ? "List of ammo" : "Список патронов")] public HashSet<AmmoConfig> Ammo { get; set; }
        }

        public class BarricadeConfigGui
        {
            [JsonProperty(En ? "Name (not edited)" : "Название (не редактируется)")] public string Name { get; set; }
            [JsonProperty(En ? "Enabled? [true/false]" : "Включен? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "ShortName (not edited)" : "ShortName (не редактируется)")] public string ShortName { get; set; }
            [JsonProperty(En ? "SkinID (not edited)" : "SkinID (не редактируется)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Price" : "Цена")] public PriceConfig Price { get; set; }
        }

        public class SkinPrefabConfig
        {
            [JsonProperty("Prefab")] public string Prefab { get; set; }
            [JsonProperty(En ? "List of SkinIDs" : "Список SkinID")] public List<ulong> SkinIDs { get; set; }
        }

        public class LightBarricade
        {
            [JsonProperty(En ? "Light position" : "Позиция лампы")] public string Light { get; set; }
            [JsonProperty(En ? "Barricade position" : "Позиция баррикады")] public string Barricade { get; set; }
        }

        public class CounterBarricade
        {
            [JsonProperty(En ? "Counter position" : "Позиция счетчика")] public string Counter { get; set; }
            [JsonProperty(En ? "Barricade position" : "Позиция баррикады")] public string Barricade { get; set; }
        }

        public class LocationConfig
        {
            [JsonProperty(En ? "Position" : "Позиция")] public string Position { get; set; }
            [JsonProperty(En ? "Rotation" : "Вращение")] public string Rotation { get; set; }
        }

        public class LocationsConfig
        {
            [JsonProperty(En ? "Position" : "Позиция")] public string Position { get; set; }
            [JsonProperty(En ? "Rotation" : "Вращение")] public string Rotation { get; set; }
            [JsonProperty(En ? "Radius around the base for Blazer (not edited)" : "Радиус вокруг базы для Blazer (не редактируется)")] public float BlazerRadius { get; set; }
            [JsonProperty(En ? "List of positions for NPCs" : "Список позиций для NPC")] public List<string> NpcPositions { get; set; }
        }

        public class MonumentLocationsConfig
        {
            [JsonProperty(En ? "Name of monument" : "Название монумента")] public string Name { get; set; }
            [JsonProperty(En ? "List of locations" : "Список расположений")] public HashSet<LocationsConfig> Locations { get; set; }
        }

        public class LootTableMissionConfig
        {
            [JsonProperty(En ? "Minimum number of items" : "Минимальное кол-во элементов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum number of items" : "Максимальное кол-во элементов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of items" : "Список предметов")] public List<ItemComputer> Items { get; set; }
        }

        public class MissionConfig
        {
            [JsonProperty(En ? "Type" : "Тип")] public int Type { get; set; }
            [JsonProperty(En ? "Probability of occurrence [0.0-100.0]" : "Вероятность появления [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Number of levels" : "Кол-во уровней")] public int Levels { get; set; }
            [JsonProperty(En ? "Reward for completing the mission" : "Награда за выполнение")] public LootTableMissionConfig Reward { get; set; }
        }

        public class WaveConfig
        {
            [JsonProperty(En ? "Level" : "Уровень")] public int Level { get; set; }
            [JsonProperty(En ? "Preparation time [sec.]" : "Время подготовки [sec.]")] public int TimeToStart { get; set; }
            [JsonProperty(En ? "Duration [sec.]" : "Длительность [sec.]")] public int Duration { get; set; }
            [JsonProperty(En ? "Additional missions" : "Дополнительные задания")] public List<MissionConfig> Missions { get; set; }
            [JsonProperty(En ? "Time until appearance of new NPCs [sec.]" : "Время появления новых NPC [sec.]")] public int TimerNpc { get; set; }
            [JsonProperty(En ? "NPC Sets" : "Наборы NPC")] public HashSet<PresetConfig> Presets { get; set; }
        }

        public class AmountConfig
        {
            [JsonProperty(En ? "Chance probability [0.0-100.0]" : "Шанс [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Count" : "Кол-во")] public int Count { get; set; }
        }

        public class PresetConfig
        {
            [JsonProperty(En ? "Type of NPC" : "Тип NPC")] public string ShortName { get; set; }
            [JsonProperty(En ? "Setting the number of NPCs depending on the probability" : "Настройка кол-ва NPC в зависимoсти от вероятности")] public List<AmountConfig> AmountConfig { get; set; }
        }

        public class ItemComputer
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int MinAmount { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int MaxAmount { get; set; }
            [JsonProperty(En ? "Chance probability [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Name (empty - default)" : "Название (empty - default)")] public string Name { get; set; }
        }

        public class BaseConfig
        {
            [JsonProperty(En ? "Name (not edited)" : "Название (не редактируется)")] public string Name { get; set; }
            [JsonProperty(En ? "List of monument locations" : "Список расположений на монументах")] public HashSet<MonumentLocationsConfig> Monuments { get; set; }
            [JsonProperty(En ? "Radius of the base zone (not edited)" : "Радиус зоны базы (не редактируется)")] public float Radius { get; set; }
            [JsonProperty(En ? "Health points for The General" : "Кол-во ХП генерала")] public float GeneralHealth { get; set; }
            [JsonProperty(En ? "List of rooms" : "Список комнат")] public HashSet<RoomConfig> Rooms { get; set; }
            [JsonProperty(En ? "List of doorway positions for installing barricades (not edited)" : "Список позиций дверных проемов для установки баррикад (не редактируется)")] public HashSet<string> BarricadePositions { get; set; }
            [JsonProperty(En ? "List of counters to display Health Points for barricade (not edited)" : "Список счетчиков для отображения HP баррикады (не редактируется)")] public HashSet<CounterBarricade> Counters { get; set; }
            [JsonProperty(En ? "List of active indicators in the absence of a barricade (not edited)" : "Список включаемых индикаторов при отсутствии баррикады (не редактируется)")] public HashSet<LightBarricade> Lights { get; set; }
            [JsonProperty(En ? "Does the Recycler appear? [true/false]" : "Работает ли переработчик? [true/false]")] public bool IsRecycler { get; set; }
            [JsonProperty(En ? "Does the Repair Bench appear? [true/false]" : "Работает ли ремонтный верстак? [true/false]")] public bool IsRepairBench { get; set; }
            [JsonProperty(En ? "Does the Workbench appear? [true/false]" : "Работает ли верстак? [true/false]")] public bool IsWorkbench { get; set; }
            [JsonProperty(En ? "List of turrets" : "Список турелей")] public HashSet<TurretConfig> Turrets { get; set; }
            [JsonProperty(En ? "List of barricades to buy" : "Список баррикад для покупки")] public HashSet<BarricadeConfigGui> BarricadesToBuy { get; set; }
            [JsonProperty(En ? "A list of items that are in the laptop inventory when the base appears" : "Список предметов, которые находятся в инвентаре ноутбука, когда появляется база")] public HashSet<ItemComputer> ItemsStart { get; set; }
            [JsonProperty(En ? "List of prefabs with changed SkinID" : "Список prefab-ов с измененным SkinID")] public HashSet<SkinPrefabConfig> SkinPrefabs { get; set; }
            [JsonProperty(En ? "Level of building blocks (0 - Twigs, 1 - Wood, 2 - Stone, 3 - Sheet Metal 4 - Armored)" : "Уровень улучшения строительных блоков (0 - Солома, 1 - Дерево, 2 - Камень, 3 - Железо, 4 - МВК)")] public int BuildingGrade { get; set; }
            [JsonProperty(En ? "SkinID of building blocks" : "SkinID строительных блоков")] public ulong BuildingSkin { get; set; }
            [JsonProperty(En ? "Color of building blocks" : "Цвет строительных блоков")] public uint BuildingColor { get; set; }
            [JsonProperty(En ? "List of doors that open at the beginning of evacuation (not edited)" : "Список дверей, которые открываются при начале эвакуации (не редактируется)")] public HashSet<string> EvacuationDoors { get; set; }
            [JsonProperty(En ? "Lift height during evacuation (not edited)" : "Высота подъема лифта во время эвакуации (не редактируется)")] public float ElevatorHeight { get; set; }
            [JsonProperty(En ? "Helicopter position and rotation during evacuation (not edited)" : "Позиция и вращение вертолета при эвакуации (не редактируется)")] public LocationConfig HelicopterLocation { get; set; }
            [JsonProperty(En ? "The General's evacuation position (not edited)" : "Позиция эвакуации генерала (не редактируется)")] public string EvacuationPosition { get; set; }
            [JsonProperty(En ? "List of attack waves" : "Список волн атаки")] public HashSet<WaveConfig> Waves { get; set; }
        }

        public class Prefab { public string prefab; public string pos; public string rot; }

        internal Dictionary<string, HashSet<Prefab>> Prefabs = new Dictionary<string, HashSet<Prefab>>();

        private void LoadPrefabs()
        {
            Puts("Loading files on the /oxide/data/DefendableBases/Base/ path has started...");
            foreach (string name in Interface.Oxide.DataFileSystem.GetFiles("DefendableBases/Base/"))
            {
                string fileName = name.Split('/').Last().Split('.').First();
                HashSet<Prefab> prefabs = Interface.Oxide.DataFileSystem.ReadObject<HashSet<Prefab>>($"DefendableBases/Base/{fileName}");
                if (prefabs != null && prefabs.Count > 0)
                {
                    Puts($"File {fileName} has been loaded successfully!");
                    Prefabs.Add(fileName, prefabs);
                }
                else PrintError($"File {fileName} is corrupted and cannot be loaded!");
            }
        }

        internal List<BaseConfig> Configs = new List<BaseConfig>();

        private void LoadConfigs()
        {
            Puts("Loading files on the /oxide/data/DefendableBases/Config/ path has started...");
            foreach (string name in Interface.Oxide.DataFileSystem.GetFiles("DefendableBases/Config/"))
            {
                string fileName = name.Split('/').Last().Split('.').First();
                BaseConfig config = Interface.Oxide.DataFileSystem.ReadObject<BaseConfig>($"DefendableBases/Config/{fileName}");
                if (config != null)
                {
                    Puts($"File {fileName} has been loaded successfully!");
                    Configs.Add(config);
                    foreach (RoomConfig room in config.Rooms)
                    {
                        foreach (CrateConfig crate in room.Crates)
                        {
                            CheckPrefabLootTable(crate.PrefabLootTable);
                            CheckLootTable(crate.OwnLootTable);
                        }
                    }
                    foreach (WaveConfig wave in config.Waves)
                    {
                        wave.Missions = wave.Missions.OrderByQuickSort(x => x.Chance);
                        foreach (MissionConfig mission in wave.Missions)
                        {
                            mission.Reward.Items = mission.Reward.Items.OrderByQuickSort(x => x.Chance);
                            if (mission.Reward.Max > mission.Reward.Items.Count) mission.Reward.Max = mission.Reward.Items.Count;
                            if (mission.Reward.Min > mission.Reward.Max) mission.Reward.Min = mission.Reward.Max;
                        }
                        foreach (PresetConfig preset in wave.Presets) preset.AmountConfig = preset.AmountConfig.OrderByQuickSort(x => x.Chance);
                    }
                    Interface.Oxide.DataFileSystem.WriteObject($"DefendableBases/Config/{fileName}", config);
                }
                else PrintError($"File {fileName} is corrupted and cannot be loaded!");
            }
        }
        #endregion Data

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} After <color=#55aaff>{1} sec.</color> the general <color=#738d43>will begin</color> evacuating the island at one of the checkpoints",
                ["Start"] = "{0} The general's guards are killed, and the general is seriously wounded! Help him to evacuate from the island at grid <color=#55aaff>{1}</color>\nWe need to <color=#55aaff>call a rescue helicopter</color> at the checkpoint control center",
                ["CallForAssistance"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>called</color> a rescue helicopter for the general at grid <color=#55aaff>{2}</color>!",
                ["CallForAssistancePlayer"] = "{0} You <color=#738d43>have called</color> a rescue helicopter for the general! Help has already <color=#738d43>flown</color> to the island\nOvercome the checkpoint. The general should not die!\nYou can <color=#55aaff>install barricades</color> in doorways, as well as <color=#55aaff>improve the base with turrets</color>. This will help you to restrain the enemy",
                ["PreparationTime"] = "{0} Preparations <color=#738d43>have begun</color> for wave <color=#55aaff>{1}</color> of enemy attack! Air defense during the preparation phase <color=#55aaff>strengthens the checkpoint</color>. Close the doorways with barricades and improve the base with turrets",
                ["AttackWave"] = "{0} Zombies are <color=#ce3f27>attacking</color>. Don't let them pass your fortification!\n<color=#55aaff>Protect the general</color>, he must not die!",
                ["UpgradeBase"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>improved</color> the checkpoint in grid <color=#55aaff>{2}</color>. Turrets <color=#738d43>installed</color> - <color=#55aaff>{3}</color>",
                ["GeneralKill"] = "{0} The General <color=#ce3f27>has been killed</color>! The rescue helicopter <color=#ce3f27>will not arrive</color> at the checkpoint. After <color=#55aaff>{1} sec.</color> the checkpoint <color=#ce3f27>will be destroyed</color>",
                ["PreFinishNoCall"] = "{0} After <color=#55aaff>{1} sec.</color> the checkpoint in the square <color=#55aaff>{2}</color> <color=#ce3f27>will be destroyed</color> if no one calls help for General",
                ["Finish"] = "{0} The checkpoint in grid <color=#55aaff>{1}</color> <color=#ce3f27>has been destroyed</color>!",
                ["GeneralEvacuationStart"] = "{0} The last wave of the attack <color=#738d43>has been successfully contained</color>. The General is <color=#738d43>alive</color>! The rescue helicopter is already near the island and is <color=#738d43>flying</color> to the checkpoint to evacuate the general",
                ["GeneralEvacuationEnd"] = "{0} General <color=#738d43>successfully evacuated</color>! After <color=#55aaff>{1} sec.</color> the checkpoint <color=#ce3f27>will be destroyed</color>",
                ["DoorOpen"] = "{0} At the location of the checkpoint, one of the supply room doors <color=#738d43>has opened</color>! The access to the drawers in this room is <color=#738d43>granted</color>",
                ["FalsePassword"] = "{0} You entered the <color=#ce3f27>wrong</color> password!",
                ["TruePassword"] = "{0} You entered the <color=#738d43>correct</color> password!\nThe process of completing an additional mission is <color=#55aaff>{1}</color>",
                ["AllPassword"] = "{0} <color=#55aaff>{1}</color> entered all passwords <color=#738d43>correctly</color>!\nA reward for completing an additional mission is <color=#738d43>available</color> in the checkpoint control center",
                ["EventActive"] = "{0} This event is active now. To finish this event (<color=#55aaff>/warstop</color>), then (<color=#55aaff>/warstart</color>) to start the next one!",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have left</color> the PVP zone, now other players <color=#738d43>cannot damage</color> you!",
                ["NTeleportation"] = "{0} You <color=#ce3f27>cannot</color> teleport into the event zone!",
                ["NoCommand"] = "{0} You <color=#ce3f27>cannot</color> use this command in the event zone!",
                ["Description"] = "Defendable Bases - The battles in this war take place in an indestructible fortress, where the human race must battle against relentless hordes of rotting flesh. According to the engineers who build these fortresses, there are several entrances around the perimeter, each leading to the main room through a single wall frame. These points create excellent places to erect a barricade with whatever scrap and junk you can find, bottleneck the hordes into these entrances and give them hell. The engineers have also provided auxiliary traps that can be activated via the computer on site. There are not many choices currently, but we have created automatic turrets, flame turrets, and shotgun traps to help rid us of these walking corpses. Let it be known, that for a dense meat grinder, and a hellish relentless onslaught of stinking undead, that the reward will be worth the stench, sweat, blood, and tears. Ready yourself humans, these fortresses are our last hope!",
                ["GUI_PROTECTION"] = "PROTECTION",
                ["GUI_NOTES"] = "NOTES",
                ["GUI_INFO"] = "INFO",
                ["GUI_INVENTORY"] = "INVENTORY",
                ["GUI_CONFIRM"] = "CONFIRM",
                ["GUI_CALL_FOR_ASSISTANCE"] = "CALL FOR ASSISTANCE",
                ["GUI_BUY"] = "BUY",
                ["GUI_UPGRADE"] = "UPGRADE",
                ["GUI_AMMUNITION"] = "AMMUNITION",
                ["GUI_PUT"] = "PUT",
                ["GUI_MAXIMUM"] = "MAXIMUM",
                ["GUI_NO_ITEMS"] = "NO ITEMS"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} Через <color=#55aaff>{1} сек.</color> генерал <color=#738d43>начнет</color> эвакуацию с острова на одном из блокпостов",
                ["Start"] = "{0} Охрана генерала убита, а генерал тяжело ранен! Помогите ему эвакуироваться с острова в квадрате <color=#55aaff>{1}</color>\nВам необходимо <color=#55aaff>вызвать спасательный вертолет</color> в центре управления блокпостом",
                ["CallForAssistance"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>вызвал</color> спасательный вертолет для генерала в квадрате <color=#55aaff>{2}</color>!",
                ["CallForAssistancePlayer"] = "{0} Вы <color=#738d43>вызвали</color> спасательный вертолет для генерала! Помощь уже <color=#738d43>вылетела</color> к острову\nОбороняйте блокпост. Генерал не должен умереть!\nВы можете <color=#55aaff>устанавливать баррикады</color> в дверные проемы, а также <color=#55aaff>улучшать базу турелями</color>. Это поможет вам сдержать противника",
                ["PreparationTime"] = "{0} <color=#738d43>Началась</color> подготовка к <color=#55aaff>{1}</color> волне атаки противника! Во время фазы подготовки <color=#55aaff>укрепите блокпост</color>. Закройте дверные проемы барикадами и улучшайте базу турелями",
                ["AttackWave"] = "{0} Зомби <color=#ce3f27>атакуют</color>. Не дай им пройти ваше укрепление!\n<color=#55aaff>Защищайте генерала</color>, он не должен умереть!",
                ["UpgradeBase"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>улучшил</color> блокпост в квадрате <color=#55aaff>{2}</color>. <color=#738d43>Установлены</color> турели - <color=#55aaff>{3}</color>",
                ["GeneralKill"] = "{0} Генерал <color=#ce3f27>убит</color>! Спасательный вертолет <color=#ce3f27>не прилетит</color> к блокпосту. Через <color=#55aaff>{1} сек.</color> блокпост будет <color=#ce3f27>уничтожен</color>",
                ["PreFinishNoCall"] = "{0} Через <color=#55aaff>{1} сек.</color> блокпост в квадрате <color=#55aaff>{2}</color> будет <color=#ce3f27>уничтожен</color>, если никто не вызовет подмогу для генерала",
                ["Finish"] = "{0} Блокпост в квадрате <color=#55aaff>{1}</color> <color=#ce3f27>уничтожен</color>!",
                ["GeneralEvacuationStart"] = "{0} Последняя волна атаки <color=#738d43>успешно сдержана</color>. Генерал <color=#738d43>жив</color>! Спасательный вертолет уже около острова и <color=#738d43>летит</color> к блокпосту, чтобы эвакуировать генерала",
                ["GeneralEvacuationEnd"] = "{0} Генерал <color=#738d43>успешно эвакуирован</color>! Через <color=#55aaff>{1} сек.</color> блокпост будет <color=#ce3f27>уничтожен</color>",
                ["DoorOpen"] = "{0} На территории блокпоста <color=#738d43>открылась</color> одна из дверей в комнату с припасами! Доступ к ящикам в данной комнате <color=#738d43>открыт</color>",
                ["FalsePassword"] = "{0} Вы ввели <color=#ce3f27>неверный</color> пароль!",
                ["TruePassword"] = "{0} Вы ввели <color=#738d43>правильный</color> пароль!\nПроцесс выполнения дополнительной миссии - <color=#55aaff>{1}</color>",
                ["AllPassword"] = "{0} <color=#55aaff>{1}</color> ввел все пароли <color=#738d43>верно</color>!\nВ центре управления блокпостом <color=#738d43>доступно</color> вознаграждение за выполнение дополнительной миссии",
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#55aaff>/warstop</color>), чтобы начать следующий!",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["NTeleportation"] = "{0} Вы <color=#ce3f27>не можете</color> телепортироваться в зоне ивента!",
                ["NoCommand"] = "{0} Вы <color=#ce3f27>не можете</color> использовать данную команду в зоне ивента!",
                ["Description"] = "Defendable Bases - The battles in this war take place in an indestructible fortress, where the human race must battle against relentless hordes of rotting flesh. According to the engineers who build these fortresses, there are several entrances around the perimeter, each leading to the main room through a single wall frame. These points create excellent places to erect a barricade with whatever scrap and junk you can find, bottleneck the hordes into these entrances and give them hell. The engineers have also provided auxiliary traps that can be activated via the computer on site. There are not many choices currently, but we have created automatic turrets, flame turrets, and shotgun traps to help rid us of these walking corpses. Let it be known, that for a dense meat grinder, and a hellish relentless onslaught of stinking undead, that the reward will be worth the stench, sweat, blood, and tears. Ready yourself humans, these fortresses are our last hope!",
                ["GUI_PROTECTION"] = "PROTECTION",
                ["GUI_NOTES"] = "NOTES",
                ["GUI_INFO"] = "INFO",
                ["GUI_INVENTORY"] = "INVENTORY",
                ["GUI_CONFIRM"] = "CONFIRM",
                ["GUI_CALL_FOR_ASSISTANCE"] = "CALL FOR ASSISTANCE",
                ["GUI_BUY"] = "BUY",
                ["GUI_UPGRADE"] = "UPGRADE",
                ["GUI_AMMUNITION"] = "AMMUNITION",
                ["GUI_PUT"] = "PUT",
                ["GUI_MAXIMUM"] = "MAXIMUM",
                ["GUI_NO_ITEMS"] = "NO ITEMS"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, _ins, userID);

        private string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Oxide Hooks
        private static DefendableBases _ins;

        private void Init()
        {
            _ins = this;
            Unsubscribes();
        }

        private void OnServerInitialized()
        {
            foreach (BaseEntity entity in GetEntities<BaseEntity>(Vector3.zero, 2f, 1 << 8)) if (entity.ShortPrefabName == "elevator_lift.static" && entity.IsExists()) entity.Kill();

            LoadDefaultMessages();

            LoadPrefabs();

            LoadConfigs();
            _monuments = TerrainMeta.Path.Monuments.Where(IsNecessaryMonument);

            LoadIDs();
            LoadCustomMapLocations();

            if (_monuments.Count == 0 && _customMaps.Count == 0)
            {
                PrintError("There are no places for the base to appear on the current map. You need to add other monuments in the plugin configuration (oxide/data/DefendableBases/Config)");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if (_monuments.Count > 0)
            {
                string message = "All places for the appearance of bases on monuments:";
                foreach (MonumentInfo monument in _monuments) message += $"\n- {PhoneController.PositionToGridCoord(monument.transform.position)}";
                Puts(message);
            }
            if (_customMaps.Count > 0)
            {
                string message = "All places for the appearance of bases on custom map:";
                foreach (CustomMapConfig config in _customMaps)
                {
                    foreach (CustomMapBaseLocationsConfig baseLocations in config.Bases)
                    {
                        message += $"\n* {baseLocations.NameBase}";
                        foreach (LocationsConfig location in baseLocations.Locations) message += $"\n- {PhoneController.PositionToGridCoord(location.Position.ToVector3())}";
                    }
                }
                Puts(message);
            }

            DownloadImage();

            if (_config.EnabledTimer)
            {
                timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
                {
                    if (!_active) Start();
                    else Puts("This event is active now. To finish this event (warstop), then to start the next one");
                });
            }
        }

        private void Unload()
        {
            if (_controller != null) Finish();
            _ins = null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (_controller.Entities.Contains(entity)) return true;
            if (_controller.CustomBarricades.Any(x => x.Entities.Contains(entity))) return true;
            if (entity is FlasherLight && _controller.Flashers.Contains(entity as FlasherLight)) return true;
            if (entity is SirenLight && _controller.Siren.IsExists() && entity as SirenLight == _controller.Siren) return true;
            if (entity is BasePlayer && _controller.General != null && (entity as BasePlayer) == _controller.General) return true;
            if (entity is ScientistNPC && _controller.Scientists.Contains(entity as ScientistNPC))
            {
                BaseEntity weaponPrefab = info.WeaponPrefab;
                if (info.InitiatorPlayer.IsPlayer() && (weaponPrefab == null || weaponPrefab.ShortPrefabName == "grenade.molotov.deployed" || weaponPrefab.ShortPrefabName == "rocket_fire") && info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Heat) return true;
                if (weaponPrefab != null && _config.WeaponToScaleDamageNpc.ContainsKey(weaponPrefab.ShortPrefabName)) info.damageTypes.ScaleAll(_config.WeaponToScaleDamageNpc[weaponPrefab.ShortPrefabName]);
            }
            return null;
        }

        private object OnEntityKill(BaseEntity entity)
        {
            if (entity == null || _controller.KillEntities) return null;
            if (_controller.Entities.Contains(entity)) return true;
            if (_controller.CustomBarricades.Any(x => x.Entities.Contains(entity))) return true;
            return null;
        }

        private object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player.IsPlayer() && _controller.Players.Contains(player))
            {
                BaseEntity entity = info.HitEntity;
                if (entity != null)
                {
                    CustomBarricade customBarricade = _controller.CustomBarricades.FirstOrDefault(x => x.Entities.Contains(entity));
                    if (customBarricade != null)
                    {
                        BarricadeConfig barricadeConfig = _config.CustomBarricades.FirstOrDefault(x => x.Level == customBarricade.Level);
                        if (barricadeConfig != null)
                        {
                            if (CanBuy(player, barricadeConfig.Price))
                            {
                                if (customBarricade.MainHealth < customBarricade.MaximumHealth)
                                {
                                    RemovePrice(player, barricadeConfig.Price);
                                    customBarricade.HitRepair();
                                }
                            }
                            else CreateNoRepair(player, barricadeConfig.Price);
                        }
                    }
                }
            }
            return null;
        }

        private object OnCounterModeToggle(PowerCounter counter, BasePlayer player, bool flag)
        {
            if (player.IsPlayer() && _controller.Players.Contains(player)) return true;
            else return null;
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return null;
            if (player == _controller.General) return true;
            if (_controller.Players.Contains(player))
            {
                _controller.Players.Remove(player);
                CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
                CuiHelper.DestroyUi(player, "MessagePassword_DefendableBases");
                CuiHelper.DestroyUi(player, "NoRepair_DefendableBases");
                if (player == _controller.OpenComputerPlayer)
                {
                    _controller.OpenComputerPlayer = null;
                    CuiHelper.DestroyUi(player, "BG_DefendableBases");
                }
                if (_controller.CaptureSphereCollider != null && _controller.CaptureSphereCollider.Players.Contains(player)) _controller.CaptureSphereCollider.ExitPlayer(player);
            }
            return null;
        }

        private object CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            if (target == null) return null;
            if (target == _controller.General) return false;
            else return null;
        }

        private object OnPlayerAssist(BasePlayer target, BasePlayer player)
        {
            if (target == null) return null;
            if (target == _controller.General) return true;
            else return null;
        }

        private object OnPlayerRevive(BasePlayer player, BasePlayer target)
        {
            if (target == null) return null;
            if (target == _controller.General) return true;
            else return null;
        }

        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null || !player.IsPlayer() || _controller == null || _controller.Computer == null) return null;
            if (item == _controller.Computer.item)
            {
                if (_controller.OpenComputerPlayer == null)
                {
                    CreateLaptop(player);
                    _controller.OpenComputerPlayer = player;
                }
                return true;
            }
            return null;
        }

        private object OnEntityEnter(TriggerBase trigger, BasePlayer basePlayer)
        {
            if (trigger == null || basePlayer == null) return null;
            BaseEntity entity = trigger.GetComponentInParent<BaseEntity>();
            if (entity == null) return null;
            if (entity is AutoTurret)
            {
                AutoTurret turret = entity as AutoTurret;
                if (_controller.AutoTurrets.Contains(turret))
                {
                    if (basePlayer.displayName == "Sledge" || basePlayer.displayName == "Juggernaut") return null;
                    else return true;
                }
                else return null;
            }
            if (entity is FlameTurret)
            {
                FlameTurret turret = entity as FlameTurret;
                if (_controller.FlameTurrets.Contains(turret))
                {
                    if (basePlayer.displayName == "Sledge" || basePlayer.displayName == "Juggernaut") return null;
                    else return true;
                }
                else return null;
            }
            if (entity is GunTrap)
            {
                GunTrap turret = entity as GunTrap;
                if (_controller.GunTraps.Contains(turret))
                {
                    if (basePlayer.displayName == "Sledge" || basePlayer.displayName == "Juggernaut") return null;
                    else return true;
                }
                else return null;
            }
            else return null;
        }

        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null) return;
            if (_controller.Scientists.Contains(entity))
            {
                if (entity.displayName == "Sledge")
                {
                    foreach (CustomBarricade barricade in _controller.CustomBarricades) if (barricade.Zombies.Contains(entity.net.ID.Value)) barricade.ExitSledge(entity);
                    if (_controller.GeneralSphere.Zombies.Contains(entity.net.ID.Value)) _controller.GeneralSphere.ExitSledge(entity);
                }
                else if (entity.displayName == "Bomber")
                {
                    Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", entity.transform.position + new Vector3(0f, 1f, 0f), Vector3.up, null, true);
                    OnBomberExplosion(entity, null);
                }
                _controller.Scientists.Remove(entity);
                NextTick(() =>
                {
                    if (corpse == null) return;
                    corpse.containers[0].ClearItemsContainer();
                    if (!corpse.IsDestroyed) corpse.Kill();
                });
            }
        }

        private object OnBradleyApcInitialize(BradleyAPC apc)
        {
            if (Vector3.Distance(apc.transform.position, _controller.transform.position) < 250f) NextTick(() => { if (apc.IsExists()) apc.Kill(); });
            return null;
        }

        private object OnNpcTarget(BaseEntity attacker, BasePlayer victim)
        {
            if (attacker == null || victim == null) return null;
            if (victim == _controller.General) return true;
            return null;
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (player != null && _controller.Players.Contains(player))
            {
                command = "/" + command;
                if (_config.Commands.Contains(command.ToLower()))
                {
                    AlertToPlayer(player, GetMessage("NoCommand", player.UserIDString, _config.Prefix));
                    return true;
                }
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;
            BasePlayer player = arg.Player();
            if (player != null && _controller.Players.Contains(player))
            {
                if (_config.Commands.Contains(arg.cmd.Name.ToLower()) || _config.Commands.Contains(arg.cmd.FullName.ToLower()))
                {
                    AlertToPlayer(player, GetMessage("NoCommand", player.UserIDString, _config.Prefix));
                    return true;
                }
            }
            return null;
        }
        #endregion Oxide Hooks

        #region Controller
        private ControllerDefendableBase _controller;
        private bool _active = false;
        internal string BaseName = string.Empty;

        private void Start()
        {
            if (!plugins.Exists("NpcSpawn"))
            {
                PrintError("NpcSpawn plugin doesn`t exist! Please read the file ReadMe.txt");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            _active = true;
            AlertToAllPlayers("PreStart", _config.Prefix, _config.PreStartTime);
            timer.In(_config.PreStartTime, () =>
            {
                Puts("DefendableBases has begun");
                _controller = new GameObject().AddComponent<ControllerDefendableBase>();
                Subscribes();
                if (_config.PveMode.Pve && plugins.Exists("PveMode"))
                {
                    JObject config = new JObject
                    {
                        ["Damage"] = _config.PveMode.Damage,
                        ["ScaleDamage"] = new JArray { _config.PveMode.ScaleDamage.Select(x => new JObject { ["Type"] = x.Type, ["Scale"] = x.Scale }) },
                        ["LootCrate"] = _config.PveMode.LootCrate,
                        ["HackCrate"] = _config.PveMode.HackCrate,
                        ["LootNpc"] = _config.PveMode.LootNpc,
                        ["DamageNpc"] = _config.PveMode.DamageNpc,
                        ["DamageTank"] = false,
                        ["TargetNpc"] = _config.PveMode.TargetNpc,
                        ["TargetTank"] = false,
                        ["CanEnter"] = _config.PveMode.CanEnter,
                        ["CanEnterCooldownPlayer"] = _config.PveMode.CanEnterCooldownPlayer,
                        ["TimeExitOwner"] = _config.PveMode.TimeExitOwner,
                        ["AlertTime"] = _config.PveMode.AlertTime,
                        ["RestoreUponDeath"] = _config.PveMode.RestoreUponDeath,
                        ["CooldownOwner"] = _config.PveMode.CooldownOwner,
                        ["Darkening"] = _config.PveMode.Darkening
                    };
                    PveMode.Call("EventAddPveMode", Name, config, _controller.transform.position, _controller.Config.Radius, new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), null);
                }
                foreach (BradleyAPC apc in BaseNetworkable.serverEntities.OfType<BradleyAPC>()) if (apc.IsExists() && Vector3.Distance(apc.transform.position, _controller.transform.position) < 250f) apc.Kill();
                if (!string.IsNullOrEmpty(_controller.MonumentName) && _config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("DestroyController", _controller.MonumentName);
                AlertToAllPlayers("Start", _config.Prefix, PhoneController.PositionToGridCoord(_controller.transform.position));
            });
        }

        private void Finish()
        {
            Unsubscribes();
            AlertToAllPlayers("Finish", _config.Prefix, PhoneController.PositionToGridCoord(_controller.transform.position));
            if (!string.IsNullOrEmpty(_controller.MonumentName) && _config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("CreateController", _controller.MonumentName);
            if (_config.PveMode.Pve && plugins.Exists("PveMode")) PveMode.Call("EventRemovePveMode", Name, true);
            if (_controller != null) UnityEngine.Object.Destroy(_controller.gameObject);
            _active = false;
            BaseName = string.Empty;
            Interface.Oxide.CallHook("OnDefendableBasesEnd");
            foreach (KeyValuePair<BasePlayer, Timer> dic in _playerToTimer)
            {
                dic.Value.Destroy();
                CuiHelper.DestroyUi(dic.Key, "NoRepair_DefendableBases");
            }
            _playerToTimer.Clear();
            Puts("DefendableBases has ended");
            if (_config.EnabledTimer)
            {
                timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
                {
                    if (!_active) Start();
                    else Puts("This event is active now. To finish this event (warstop), then to start the next one");
                });
            }
        }

        internal class ControllerDefendableBase : FacepunchBehaviour
        {
            private MapMarkerGenericRadius _mapmarker;
            private VendingMachineMapMarker _vendingMarker;

            private SphereCollider _sphereCollider;

            internal BaseConfig Config;

            public class EntityLocation { public Vector3 Position; public Quaternion Rotation; }

            private readonly HashSet<EntityLocation> _autoTurretsLocations = new HashSet<EntityLocation>();
            private readonly HashSet<EntityLocation> _flameTurretsLocations = new HashSet<EntityLocation>();
            private readonly HashSet<EntityLocation> _gunTrapLocations = new HashSet<EntityLocation>();
            private readonly Dictionary<string, HashSet<Door>> _turretDoors = new Dictionary<string, HashSet<Door>>();
            internal HashSet<AutoTurret> AutoTurrets = new HashSet<AutoTurret>();
            internal HashSet<FlameTurret> FlameTurrets = new HashSet<FlameTurret>();
            internal HashSet<GunTrap> GunTraps = new HashSet<GunTrap>();

            private readonly HashSet<string> _cctvNames = new HashSet<string>();

            internal HashSet<BaseEntity> Entities = new HashSet<BaseEntity>();
            internal bool KillEntities = false;
            internal Dictionary<int, HashSet<LootContainer>> RoomCrates = new Dictionary<int, HashSet<LootContainer>>();
            internal Dictionary<ulong, int> CrateToTypeLootTable = new Dictionary<ulong, int>();
            private readonly Dictionary<int, Door> _roomDoors = new Dictionary<int, Door>();

            private Coroutine _spawnEntitiesCoroutine = null;
            private Coroutine _startCoroutine = null;
            private Coroutine _waveCoroutine = null;
            private Coroutine _finishCoroutine = null;

            private readonly HashSet<Door> _evacuationDoors = new HashSet<Door>();

            private ElevatorLiftStatic _elevator = null;

            internal DroppedItem Computer = null;
            internal BasePlayer OpenComputerPlayer = null;

            internal readonly Dictionary<ulong, SimpleLight> Lights = new Dictionary<ulong, SimpleLight>();
            internal readonly Dictionary<ulong, PowerCounter> Counters = new Dictionary<ulong, PowerCounter>();

            internal readonly List<BuildingBlock> WallFrames = new List<BuildingBlock>();
            internal readonly HashSet<CustomBarricade> CustomBarricades = new HashSet<CustomBarricade>();

            internal List<ItemInventory> Inventory = new List<ItemInventory>();
            public class ItemInventory { public string shortname; public int amount; public ulong skinId; public string name; }

            internal HashSet<BasePlayer> Players = new HashSet<BasePlayer>();

            internal BasePlayer General = null;
            internal GeneralSphereCollider GeneralSphere = null;
            internal float GeneralHealth = 0f;

            private List<Vector3> _npcSpawnPositions = new List<Vector3>();
            private float _blazerRadius = 0f;
            internal HashSet<ScientistNPC> Scientists = new HashSet<ScientistNPC>();

            internal bool CallForAssistance = false;

            internal int Seconds = 0;
            internal int MaxSeconds = 0;

            private readonly List<int> Digits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            private readonly List<char> Symbols = new List<char> { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            internal string MessagePassword = string.Empty;
            internal string KeyPassword = string.Empty;
            internal List<int> Password = new List<int>();

            private PFXRepeatingFirework _romancandle;
            private BaseFirework _volcan;
            internal SirenLight Siren;
            internal HashSet<FlasherLight> Flashers = new HashSet<FlasherLight>();
            internal CaptureSphereCollider CaptureSphereCollider;

            internal int CompleteStageMission = 0;
            internal int AllStageMission = 0;
            private LootTableMissionConfig _rewardMission = null;

            internal CH47Helicopter Ch47;
            private CH47HelicopterAIController _ch47Ai;
            internal int StageCh47;
            private Vector3 _spawnCh47Pos;
            private Vector3 _targetCh47Pos;
            private Quaternion _targetCh47Rot;

            internal string MonumentName;

            private void Awake()
            {
                Config = string.IsNullOrEmpty(_ins.BaseName) ? _ins.Configs.GetRandom() : _ins.Configs.FirstOrDefault(x => x.Name == _ins.BaseName);

                GiveInventoryItems(Config.ItemsStart);

                Location location = _ins.GetLocation(Config.Name);
                transform.position = location.Position;
                transform.rotation = location.Rotation;
                _blazerRadius = location.BlazerRadius;
                _npcSpawnPositions = location.NpcPositions;
                MonumentName = location.Name;

                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = Config.Radius;

                SpawnMapMarker();
                UpdateMapMarker(0);

                ClearEntities();

                _spawnEntitiesCoroutine = ServerMgr.Instance.StartCoroutine(SpawnEntities());
            }

            private void OnDestroy()
            {
                if (_spawnEntitiesCoroutine != null) ServerMgr.Instance.StopCoroutine(_spawnEntitiesCoroutine);
                if (_startCoroutine != null) ServerMgr.Instance.StopCoroutine(_startCoroutine);
                if (_waveCoroutine != null) ServerMgr.Instance.StopCoroutine(_waveCoroutine);
                if (_finishCoroutine != null) ServerMgr.Instance.StopCoroutine(_finishCoroutine);

                if (_mapmarker.IsExists()) _mapmarker.Kill();
                if (_vendingMarker.IsExists()) _vendingMarker.Kill();

                KillEntities = true;
                foreach (BaseEntity entity in Entities) if (entity.IsExists()) entity.Kill();
                if (Computer.IsExists()) Computer.Kill();

                DestroyCapture();

                foreach (KeyValuePair<int, HashSet<LootContainer>> dic in RoomCrates) foreach (LootContainer crate in dic.Value) if (crate.IsExists()) crate.Kill();

                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();

                Destroy(GeneralSphere);
                if (General.IsExists()) General.Kill();

                CancelInvoke(UpdateCh47);
                if (Ch47.IsExists()) Ch47.Kill();

                foreach (BasePlayer player in Players)
                {
                    CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
                    CuiHelper.DestroyUi(player, "MessagePassword_DefendableBases");
                    CuiHelper.DestroyUi(player, "BG_DefendableBases");
                }
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Add(player);
                    if (AllStageMission != 0) _ins.MessagePassword(player);
                    if (_ins._config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("EnterPVP", player.UserIDString, _ins._config.Prefix));
                }
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Remove(player);
                    CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
                    CuiHelper.DestroyUi(player, "MessagePassword_DefendableBases");
                    if (_ins._config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("ExitPVP", player.UserIDString, _ins._config.Prefix));
                }
            }

            private static void GetGlobal(Transform Transform, Vector3 localPosition, Vector3 localRotation, out Vector3 globalPosition, out Quaternion globalRotation)
            {
                globalPosition = Transform.TransformPoint(localPosition);
                globalRotation = Transform.rotation * Quaternion.Euler(localRotation);
            }

            internal Vector3 GetGlobalPos(Vector3 localPos) => transform.TransformPoint(localPos);

            private static bool IsEqualVector3(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.1f;

            internal void AddInventory(string shortname, int amount, ulong skinId, string name)
            {
                ItemInventory itemInventory = Inventory.FirstOrDefault(x => x.shortname == shortname && x.skinId == skinId);
                if (itemInventory == null) Inventory.Add(new ItemInventory { shortname = shortname, amount = amount, skinId = skinId, name = name });
                else itemInventory.amount += amount;
            }

            private IEnumerator SpawnEntities()
            {
                Dictionary<SimpleLight, Vector3> lights = new Dictionary<SimpleLight, Vector3>();

                Dictionary<PowerCounter, Vector3> counters = new Dictionary<PowerCounter, Vector3>();

                ComputerStation computer = null;

                foreach (Prefab prefab in _ins.Prefabs[Config.Name])
                {
                    Vector3 localPos = prefab.pos.ToVector3();
                    Vector3 pos; Quaternion rot;
                    GetGlobal(transform, localPos, prefab.rot.ToVector3(), out pos, out rot);

                    if (prefab.prefab.Contains("autoturret_deployed"))
                    {
                        _autoTurretsLocations.Add(new EntityLocation { Position = pos, Rotation = rot });
                        continue;
                    }

                    if (prefab.prefab.Contains("flameturret.deployed"))
                    {
                        _flameTurretsLocations.Add(new EntityLocation { Position = pos, Rotation = rot });
                        continue;
                    }

                    if (prefab.prefab.Contains("guntrap.deployed"))
                    {
                        _gunTrapLocations.Add(new EntityLocation { Position = pos, Rotation = rot });
                        continue;
                    }

                    if (prefab.prefab.Contains("targeting_computer.worldmodel"))
                    {
                        Computer = SpawnDroppedItem("targeting.computer", pos, rot, true);
                        continue;
                    }

                    BaseEntity entity = SpawnEntity(prefab.prefab, pos, rot);

                    SkinPrefabConfig skinConfig = Config.SkinPrefabs.FirstOrDefault(x => x.Prefab == prefab.prefab);
                    if (skinConfig != null)
                    {
                        entity.skinID = skinConfig.SkinIDs.GetRandom();
                        entity.SendNetworkUpdate();
                    }

                    if (entity is BuildingBlock)
                    {
                        BuildingBlock buildingBlock = entity as BuildingBlock;
                        buildingBlock.ChangeGradeAndSkin(Config.BuildingGrade == 0 ? BuildingGrade.Enum.Twigs : Config.BuildingGrade == 1 ? BuildingGrade.Enum.Wood : Config.BuildingGrade == 2 ? BuildingGrade.Enum.Stone : Config.BuildingGrade == 3 ? BuildingGrade.Enum.Metal : BuildingGrade.Enum.TopTier, Config.BuildingSkin);
                        buildingBlock.SetCustomColour(Config.BuildingColor);
                        if (buildingBlock.ShortPrefabName == "wall.frame" && Config.BarricadePositions.Any(x => IsEqualVector3(x.ToVector3(), localPos))) WallFrames.Add(buildingBlock);
                    }

                    if (entity is Workbench && !Config.IsWorkbench)
                    {
                        entity.SetFlag(BaseEntity.Flags.Locked, true);
                        (entity as Workbench).Workbenchlevel = 0;
                    }

                    if (entity is RepairBench && !Config.IsRepairBench) entity.SetFlag(BaseEntity.Flags.Locked, true);

                    if (entity is Recycler && !Config.IsRecycler) entity.SetFlag(BaseEntity.Flags.Locked, true);

                    if (entity is SimpleLight)
                    {
                        SimpleLight light = entity as SimpleLight;
                        if (entity.ShortPrefabName == "industrial.wall.lamp.red.deployed")
                        {
                            LightBarricade lightBarricade = Config.Lights.FirstOrDefault(x => IsEqualVector3(x.Light.ToVector3(), localPos));
                            lights.Add(light, transform.TransformPoint(lightBarricade.Barricade.ToVector3()));
                            light.UpdateFromInput(1, 0);
                        }
                        else if (entity.ShortPrefabName == "simplelight") light.UpdateFromInput(1, 0);
                    }

                    if (entity is ComputerStation) computer = entity as ComputerStation;

                    if (entity is CCTV_RC)
                    {
                        CCTV_RC cctv = entity as CCTV_RC;
                        cctv.UpdateFromInput(5, 0);
                        string name = $"{Config.Name}0{_cctvNames.Count + 1}";
                        cctv.rcIdentifier = name;
                        _cctvNames.Add(name);
                    }

                    if (entity is Door)
                    {
                        Door door = entity as Door;

                        RoomConfig room = Config.Rooms.FirstOrDefault(x => IsEqualVector3(x.DoorPosition.ToVector3(), localPos));
                        if (room != null)
                        {
                            door.canTakeCloser = false;
                            door.canTakeKnocker = false;
                            door.canTakeLock = false;
                            door.canHandOpen = false;
                            door.hasHatch = false;
                            _roomDoors.Add(room.LevelWave, door);
                        }

                        TurretConfig turret = Config.Turrets.FirstOrDefault(x => x.Doors.Any(y => IsEqualVector3(y.ToVector3(), localPos)));
                        if (turret != null)
                        {
                            door.canTakeCloser = false;
                            door.canTakeKnocker = false;
                            door.canTakeLock = false;
                            door.canHandOpen = false;
                            door.hasHatch = false;
                            if (_turretDoors.ContainsKey(turret.Name)) _turretDoors[turret.Name].Add(door);
                            else _turretDoors.Add(turret.Name, new HashSet<Door> { door });
                        }

                        if (Config.EvacuationDoors.Any(x => IsEqualVector3(x.ToVector3(), localPos)))
                        {
                            door.canTakeCloser = false;
                            door.canTakeKnocker = false;
                            door.canTakeLock = false;
                            door.canHandOpen = false;
                            door.hasHatch = false;
                            _evacuationDoors.Add(door);
                        }
                    }

                    if (entity is PowerCounter)
                    {
                        PowerCounter counter = entity as PowerCounter;
                        CounterBarricade counterBarricade = Config.Counters.FirstOrDefault(x => IsEqualVector3(x.Counter.ToVector3(), localPos));
                        counters.Add(counter, transform.TransformPoint(counterBarricade.Barricade.ToVector3()));
                    }

                    if (prefab.prefab.Contains("elevator.static.top"))
                    {
                        List<ElevatorLiftStatic> list = Pool.GetList<ElevatorLiftStatic>();
                        Vis.Entities<ElevatorLiftStatic>(pos, 3f, list, 1 << 8);
                        _elevator = list[0];
                        Pool.FreeList(ref list);
                    }

                    Entities.Add(entity);

                    yield return CoroutineEx.waitForSeconds(0.001f);
                }

                foreach (KeyValuePair<SimpleLight, Vector3> dic in lights)
                {
                    BuildingBlock wallFrame = WallFrames.FirstOrDefault(x => IsEqualVector3(x.transform.position, dic.Value));
                    Lights.Add(wallFrame.net.ID.Value, dic.Key);
                }

                foreach (KeyValuePair<PowerCounter, Vector3> dic in counters)
                {
                    BuildingBlock wallFrame = WallFrames.FirstOrDefault(x => IsEqualVector3(x.transform.position, dic.Value));
                    Counters.Add(wallFrame.net.ID.Value, dic.Key);
                }

                if (computer != null) foreach (string cctvName in _cctvNames) computer.ForceAddBookmark(cctvName);

                _ins.NpcSpawn.Call("SetWallFramesPos", WallFrames.Select(x => x.transform.position));

                SpawnCrates();

                SpawnGeneral();

                _startCoroutine = ServerMgr.Instance.StartCoroutine(StartEvent());
            }

            private void SpawnCrates()
            {
                foreach (RoomConfig room in Config.Rooms)
                {
                    foreach (CrateConfig crateConfig in room.Crates)
                    {
                        Vector3 pos; Quaternion rot;
                        GetGlobal(transform, crateConfig.Position.ToVector3(), crateConfig.Rotation.ToVector3(), out pos, out rot);
                        LootContainer crate = GameManager.server.CreateEntity(crateConfig.Prefab, pos, rot) as LootContainer;
                        crate.enableSaving = false;
                        crate.Spawn();
                        if (RoomCrates.ContainsKey(room.LevelWave)) RoomCrates[room.LevelWave].Add(crate);
                        else RoomCrates.Add(room.LevelWave, new HashSet<LootContainer> { crate });
                        crate.SetFlag(BaseEntity.Flags.Locked, true);
                        CrateToTypeLootTable.Add(crate.net.ID.Value, crateConfig.TypeLootTable);
                        if (crateConfig.TypeLootTable == 1 || crateConfig.TypeLootTable == 4 || crateConfig.TypeLootTable == 5)
                        {
                            _ins.NextTick(() =>
                            {
                                crate.inventory.ClearItemsContainer();
                                if (crateConfig.TypeLootTable == 4 || crateConfig.TypeLootTable == 5) _ins.AddToContainerPrefab(crate.inventory, crateConfig.PrefabLootTable);
                                if (crateConfig.TypeLootTable == 1 || crateConfig.TypeLootTable == 5) _ins.AddToContainerItem(crate.inventory, crateConfig.OwnLootTable);
                            });
                        }
                    }
                }
            }

            private void SpawnGeneral()
            {
                General = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", _elevator.transform.position - new Vector3(0f, 1f, 0f)) as BasePlayer;
                General.enableSaving = false;
                General.Spawn();

                General.displayName = "General Newman";

                GeneralHealth = Config.GeneralHealth;

                ItemManager.CreateByName("burlap.headwrap", 1, 1694253807).MoveToContainer(General.inventory.containerWear);
                ItemManager.CreateByName("burlap.gloves", 1, 0).MoveToContainer(General.inventory.containerWear);
                ItemManager.CreateByName("hoodie", 1, 1700935391).MoveToContainer(General.inventory.containerWear);
                ItemManager.CreateByName("pants", 1, 1700938224).MoveToContainer(General.inventory.containerWear);
                ItemManager.CreateByName("shoes.boots", 1, 0).MoveToContainer(General.inventory.containerWear);

                General.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, true);

                _ins.NpcSpawn.Call("SetGeneralPos", General.transform.position);

                GeneralSphere = new GameObject().AddComponent<GeneralSphereCollider>();
            }

            private IEnumerator StartEvent()
            {
                Interface.Oxide.CallHook("OnDefendableBasesStart", Entities, transform.position, Config.Radius);
                Seconds = MaxSeconds = _ins._config.Duration;
                while (Seconds > 0)
                {
                    if (CallForAssistance)
                    {
                        _waveCoroutine = ServerMgr.Instance.StartCoroutine(ProcessWave(1));
                        yield break;
                    }
                    if (Seconds == _ins._config.PreFinishTime) _ins.AlertToAllPlayers("PreFinishNoCall", _ins._config.Prefix, _ins._config.PreFinishTime, PhoneController.PositionToGridCoord(transform.position));
                    foreach (BasePlayer player in Players) _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat() });
                    if (OpenComputerPlayer != null) _ins.UpdateInfo(OpenComputerPlayer);
                    yield return CoroutineEx.waitForSeconds(1f);
                    Seconds--;
                }
                _ins.Finish();
            }

            internal IEnumerator ProcessWave(int level)
            {
                UpdateMapMarker(level);

                WaveConfig wave = Config.Waves.FirstOrDefault(x => x.Level == level);

                if (level != 1) foreach (BasePlayer player in Players) _ins.AlertToPlayer(player, _ins.GetMessage("PreparationTime", player.UserIDString, _ins._config.Prefix, level));

                Seconds = MaxSeconds = wave.TimeToStart;
                while (Seconds > 0)
                {
                    if (General == null) yield break;
                    foreach (BasePlayer player in Players) _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(), ["Plus_KpucTaJl"] = $"{(int)GeneralHealth} HP" });
                    if (OpenComputerPlayer != null) _ins.UpdateInfo(OpenComputerPlayer);
                    yield return CoroutineEx.waitForSeconds(1f);
                    Seconds--;
                }

                ClearEntities();

                float chanceMission = UnityEngine.Random.Range(0.0f, 100.0f);
                foreach (MissionConfig mission in wave.Missions)
                {
                    if (chanceMission <= mission.Chance)
                    {
                        AllStageMission = mission.Levels;
                        _rewardMission = mission.Reward;
                        if (mission.Type == 0)
                        {
                            SetPassword();
                            foreach (BasePlayer player in Players) _ins.MessagePassword(player);
                        }
                        else if (mission.Type == 1) SpawnCapturePoint();
                        break;
                    }
                }

                foreach (BasePlayer player in Players) _ins.AlertToPlayer(player, _ins.GetMessage("AttackWave", player.UserIDString, _ins._config.Prefix));

                int timerNpc = Seconds = MaxSeconds = wave.Duration;
                while (Seconds > 0)
                {
                    if (General == null) yield break;
                    foreach (BasePlayer player in Players)
                    {
                        if (AllStageMission == 0) _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(), ["Plus_KpucTaJl"] = $"{(int)GeneralHealth} HP", ["Npc_KpucTaJl"] = $"{Scientists.Count}" });
                        else _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(), ["Plus_KpucTaJl"] = $"{(int)GeneralHealth} HP", ["Npc_KpucTaJl"] = $"{Scientists.Count}", ["Bookmark_KpucTaJl"] = $"{CompleteStageMission} / {AllStageMission}" });
                    }
                    if (OpenComputerPlayer != null) _ins.UpdateInfo(OpenComputerPlayer);
                    if (timerNpc == Seconds)
                    {
                        timerNpc = Seconds - wave.TimerNpc;
                        float wait = 0f;
                        foreach (PresetConfig preset in wave.Presets)
                        {
                            float chance = UnityEngine.Random.Range(0.0f, 100.0f);
                            AmountConfig amountConfig = preset.AmountConfig.FirstOrDefault(x => chance <= x.Chance);
                            if (amountConfig == null) continue;
                            int count = amountConfig.Count;
                            JObject objectConfig = preset.ShortName == "Sledge" ? GetSledgeObjectConfig() : preset.ShortName == "Blazer" ? GetBlazerObjectConfig() : preset.ShortName == "Juggernaut" ? GetJuggernautObjectConfig() : preset.ShortName == "Bomber" ? GetBomberObjectConfig() : null;
                            if (objectConfig == null) continue;
                            for (int i = 0; i < count; i++)
                            {
                                if (preset.ShortName == "Sledge") SetSledgeWeapons(objectConfig);
                                else if (preset.ShortName == "Blazer") SetBlazerWeapons(objectConfig);
                                else if (preset.ShortName == "Juggernaut") SetJuggernautWeapons(objectConfig);
                                ScientistNPC npc = (ScientistNPC)_ins.NpcSpawn.Call("SpawnNpc", _npcSpawnPositions.GetRandom(), objectConfig);
                                if (npc != null)
                                {
                                    Scientists.Add(npc);
                                    if (preset.ShortName == "Juggernaut") SetJuggernautGuardTarget(npc);
                                    else if (preset.ShortName == "Bomber")
                                    {
                                        Item item = ItemManager.CreateByName("explosive.timed");
                                        if (!item.MoveToContainer(npc.inventory.containerBelt)) item.Remove();
                                        else _ins.NpcSpawn.Call("SetCurrentWeapon", npc, item);
                                    }
                                }
                                yield return CoroutineEx.waitForSeconds(0.01f);
                            }
                            wait += 0.01f * count;
                        }
                        if (_ins._config.PveMode.Pve && _ins.plugins.Exists("PveMode")) _ins.PveMode.Call("EventAddScientists", _ins.Name, Scientists.Select(x => x.net.ID.Value));
                        if (wait < 1f) yield return CoroutineEx.waitForSeconds(1f - wait);
                        else if (wait >= 2f) Seconds -= (int)(wait - 1f);
                    }
                    else yield return CoroutineEx.waitForSeconds(1f);
                    Seconds--;
                }

                ClearNpc();

                foreach (string shortname in new HashSet<string> { "ammo.rifle", "ammo.rifle.explosive", "ammo.rifle.hv", "ammo.rifle.incendiary" }) UpdateAmmoAutoTurret(shortname, 0);
                UpdateAmmoGunTrap(0);
                UpdateAmmoGunTrap(0);

                if (AllStageMission != 0) CompleteStageMission = AllStageMission = 0;

                if (!string.IsNullOrEmpty(KeyPassword))
                {
                    MessagePassword = string.Empty;
                    KeyPassword = string.Empty;
                    Password.Clear();
                    _rewardMission = null;
                    foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "MessagePassword_DefendableBases");
                }

                if (CaptureSphereCollider != null)
                {
                    DestroyCapture();
                    _rewardMission = null;
                }

                if (_roomDoors.ContainsKey(level))
                {
                    foreach (BasePlayer player in Players) _ins.AlertToPlayer(player, _ins.GetMessage("DoorOpen", player.UserIDString, _ins._config.Prefix));
                    _roomDoors[level].SetOpen(true);
                }
                if (RoomCrates.ContainsKey(level))
                {
                    foreach (LootContainer crate in RoomCrates[level])
                    {
                        crate.SetFlag(BaseEntity.Flags.Locked, false);
                        if (_ins._config.PveMode.Pve && _ins.plugins.Exists("PveMode")) _ins.PveMode.Call("EventAddCrates", _ins.Name, new HashSet<ulong> { crate.net.ID.Value });
                    }
                }

                if (level < Config.Waves.Count) _waveCoroutine = ServerMgr.Instance.StartCoroutine(ProcessWave(level + 1));
                else
                {
                    foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
                    Evacuation();
                    _ins.AlertToAllPlayers("GeneralEvacuationStart", _ins._config.Prefix);
                }
            }

            internal string GetTimeFormat()
            {
                if (Seconds <= 60) return $"{Seconds} sec.";
                else
                {
                    int sec = Seconds % 60;
                    int min = (Seconds - sec) / 60;
                    return $"{min} min. {sec} sec.";
                }
            }

            private static void SetSledgeWeapons(JObject objectConfig)
            {
                float chance = UnityEngine.Random.Range(0.0f, 100.0f);
                SledgeBelt belt = _ins._config.Sledge.Weapons.WhereToList(x => chance <= x.Chance).GetRandom();
                JArray result = new JArray { new JObject { ["ShortName"] = belt.ShortName, ["Amount"] = 1, ["SkinID"] = belt.SkinID, ["Mods"] = new JArray(), ["Ammo"] = "" } };
                if (chance <= _ins._config.Sledge.BeancanGrenade) result.Add(new JObject { ["ShortName"] = "grenade.beancan", ["Amount"] = 1, ["SkinID"] = 0, ["Mods"] = new JArray(), ["Ammo"] = "" });
                objectConfig["BeltItems"] = result;
            }

            private JObject GetSledgeObjectConfig()
            {
                return new JObject
                {
                    ["Name"] = "Sledge",
                    ["WearItems"] = new JArray { _ins._config.Sledge.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = "",
                    ["Health"] = _ins._config.Sledge.Health,
                    ["RoamRange"] = 4f,
                    ["ChaseRange"] = Config.Radius,
                    ["SenseRange"] = Config.Radius / 2f,
                    ["ListenRange"] = Config.Radius / 4f,
                    ["AttackRangeMultiplier"] = 1f,
                    ["CheckVisionCone"] = false,
                    ["VisionCone"] = 135f,
                    ["HostileTargetsOnly"] = false,
                    ["DamageScale"] = _ins._config.Sledge.DamageScale,
                    ["TurretDamageScale"] = _ins._config.Sledge.TurretDamageScale,
                    ["AimConeScale"] = 2f,
                    ["DisableRadio"] = true,
                    ["CanRunAwayWater"] = false,
                    ["CanSleep"] = false,
                    ["SleepDistance"] = 100f,
                    ["Speed"] = _ins._config.Sledge.Speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = General.transform.position.ToString(),
                    ["MemoryDuration"] = 10f,
                    ["States"] = new JArray { "RoamState", "ChaseState", "CombatState", "SledgeState" }
                };
            }

            private static void SetBlazerWeapons(JObject objectConfig)
            {
                float chance = UnityEngine.Random.Range(0.0f, 100.0f);
                ProjectileBelt belt = _ins._config.Blazer.Weapons.WhereToList(x => chance <= x.Chance).GetRandom();
                objectConfig["BeltItems"] = new JArray { new JObject { ["ShortName"] = belt.ShortName, ["Amount"] = 1, ["SkinID"] = belt.SkinID, ["Mods"] = new JArray { belt.Mods }, ["Ammo"] = belt.Ammo } };
                objectConfig["DamageScale"] = belt.DamageScale;
            }

            private JObject GetBlazerObjectConfig()
            {
                return new JObject
                {
                    ["Name"] = "Blazer",
                    ["WearItems"] = new JArray { _ins._config.Blazer.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = "",
                    ["Health"] = _ins._config.Blazer.Health,
                    ["RoamRange"] = 4f,
                    ["ChaseRange"] = Config.Radius,
                    ["SenseRange"] = Config.Radius,
                    ["ListenRange"] = Config.Radius / 2f,
                    ["AttackRangeMultiplier"] = _ins._config.Blazer.AttackRangeMultiplier,
                    ["CheckVisionCone"] = false,
                    ["VisionCone"] = _blazerRadius,
                    ["HostileTargetsOnly"] = false,
                    ["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = _ins._config.Blazer.AimConeScale,
                    ["DisableRadio"] = true,
                    ["CanRunAwayWater"] = false,
                    ["CanSleep"] = false,
                    ["SleepDistance"] = 100f,
                    ["Speed"] = _ins._config.Blazer.Speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = General.transform.position.ToString(),
                    ["MemoryDuration"] = 10f,
                    ["States"] = new JArray { "RoamState", "ChaseState", "CombatState", "BlazerState" }
                };
            }

            private static void SetJuggernautWeapons(JObject objectConfig)
            {
                float chance = UnityEngine.Random.Range(0.0f, 100.0f);
                ProjectileBelt belt = _ins._config.Juggernaut.Weapons.WhereToList(x => chance <= x.Chance).GetRandom();
                objectConfig["BeltItems"] = new JArray { new JObject { ["ShortName"] = belt.ShortName, ["Amount"] = 1, ["SkinID"] = belt.SkinID, ["Mods"] = new JArray { belt.Mods }, ["Ammo"] = belt.Ammo } };
                objectConfig["DamageScale"] = belt.DamageScale;
            }

            private JObject GetJuggernautObjectConfig()
            {
                return new JObject
                {
                    ["Name"] = "Juggernaut",
                    ["WearItems"] = new JArray { _ins._config.Juggernaut.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = "",
                    ["Health"] = _ins._config.Juggernaut.Health,
                    ["RoamRange"] = 4f,
                    ["ChaseRange"] = Config.Radius,
                    ["SenseRange"] = Config.Radius,
                    ["ListenRange"] = Config.Radius / 2f,
                    ["AttackRangeMultiplier"] = _ins._config.Juggernaut.AttackRangeMultiplier,
                    ["CheckVisionCone"] = false,
                    ["VisionCone"] = 135f,
                    ["HostileTargetsOnly"] = false,
                    ["TurretDamageScale"] = _ins._config.Juggernaut.TurretDamageScale,
                    ["AimConeScale"] = _ins._config.Juggernaut.AimConeScale,
                    ["DisableRadio"] = true,
                    ["CanRunAwayWater"] = false,
                    ["CanSleep"] = false,
                    ["SleepDistance"] = 100f,
                    ["Speed"] = _ins._config.Juggernaut.Speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = General.transform.position.ToString(),
                    ["MemoryDuration"] = 10f,
                    ["States"] = new JArray { "RoamState", "ChaseState", "CombatState" }
                };
            }

            private JObject GetBomberObjectConfig()
            {
                return new JObject
                {
                    ["Name"] = "Bomber",
                    ["WearItems"] = new JArray { _ins._config.Bomber.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = "",
                    ["Health"] = _ins._config.Bomber.Health,
                    ["RoamRange"] = 4f,
                    ["ChaseRange"] = Config.Radius,
                    ["SenseRange"] = Config.Radius / 2f,
                    ["ListenRange"] = Config.Radius / 4f,
                    ["AttackRangeMultiplier"] = _ins._config.Bomber.AttackRangeMultiplier,
                    ["CheckVisionCone"] = false,
                    ["VisionCone"] = 135f,
                    ["HostileTargetsOnly"] = false,
                    ["DamageScale"] = 1f,
                    ["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = 2f,
                    ["DisableRadio"] = true,
                    ["CanRunAwayWater"] = false,
                    ["CanSleep"] = false,
                    ["SleepDistance"] = 100f,
                    ["Speed"] = _ins._config.Bomber.Speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = General.transform.position.ToString(),
                    ["MemoryDuration"] = 10f,
                    ["States"] = new JArray { "RoamState", "ChaseState", "CombatState", "SledgeState" }
                };
            }

            internal void SetJuggernautGuardTarget(ScientistNPC npc)
            {
                HashSet<ScientistNPC> sledges = Scientists.Where(x => x.IsExists() && x.displayName == "Sledge");
                if (sledges != null && sledges.Count > 0) _ins.NpcSpawn.Call("AddTargetGuard", npc, sledges.Min(x => Vector3.Distance(npc.transform.position, x.transform.position)));
            }

            private void GiveInventoryItems(HashSet<ItemComputer> items)
            {
                foreach (ItemComputer item in items)
                {
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                    {
                        Inventory.Add(new ItemInventory
                        {
                            shortname = item.ShortName,
                            amount = UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1),
                            skinId = item.SkinID,
                            name = item.Name
                        });
                    }
                }
            }

            internal void OpenTurrets(TurretConfig config)
            {
                if (config.Name == "Auto Turret")
                {
                    SpawnAutoTurrets();
                    foreach (AmmoStartConfig ammo in config.AmmoStart) UpdateAmmoAutoTurret(ammo.ShortName, ammo.Amount);
                }
                else if (config.Name == "Flame Turret")
                {
                    SpawnFlameTurrets();
                    foreach (AmmoStartConfig ammo in config.AmmoStart) UpdateAmmoFlameTurret(ammo.Amount);
                }
                else if (config.Name == "Shotgun Trap")
                {
                    SpawnGunTraps();
                    foreach (AmmoStartConfig ammo in config.AmmoStart) UpdateAmmoGunTrap(ammo.Amount);
                }
                if (_turretDoors.ContainsKey(config.Name)) foreach (Door door in _turretDoors[config.Name]) door.SetOpen(true);
            }

            private void SpawnAutoTurrets()
            {
                foreach (EntityLocation location in _autoTurretsLocations)
                {
                    AutoTurret entity = SpawnEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", location.Position, location.Rotation) as AutoTurret;
                    Entities.Add(entity);
                    AutoTurrets.Add(entity);
                    entity.inventory.Insert(ItemManager.CreateByName("rifle.ak"));
                    entity.SendNetworkUpdate();
                    entity.UpdateFromInput(10, 0);
                }
            }

            private void SpawnFlameTurrets()
            {
                foreach (EntityLocation location in _flameTurretsLocations)
                {
                    FlameTurret entity = SpawnEntity("assets/prefabs/npc/flame turret/flameturret.deployed.prefab", location.Position, location.Rotation) as FlameTurret;
                    entity.SetFlag(BaseEntity.Flags.Locked, true);
                    Entities.Add(entity);
                    FlameTurrets.Add(entity);
                }
            }

            private void SpawnGunTraps()
            {
                foreach (EntityLocation location in _gunTrapLocations)
                {
                    GunTrap entity = SpawnEntity("assets/prefabs/deployable/single shot trap/guntrap.deployed.prefab", location.Position, location.Rotation) as GunTrap;
                    entity.SetFlag(BaseEntity.Flags.Locked, true);
                    Entities.Add(entity);
                    GunTraps.Add(entity);
                }
            }

            internal void UpdateAmmoAutoTurret(string shortname, int amount)
            {
                if (AutoTurrets.Count == 0) return;
                foreach (AutoTurret turret in AutoTurrets)
                {
                    while (turret.inventory.itemList.Any(x => x.info.shortname == shortname))
                    {
                        Item item = turret.inventory.itemList.FirstOrDefault(x => x.info.shortname == shortname);
                        amount += item.amount;
                        item.RemoveFromContainer();
                        item.Remove();
                    }
                }
                if (amount < 1) return;
                int amountTurrets = AutoTurrets.Count;
                int amountInventory = amount % amountTurrets;
                if (amountInventory > 0) AddInventory(shortname, amountInventory, 0, "");
                int amountPerTurret = (amount - amountInventory) / amountTurrets;
                if (amountPerTurret > 0)
                {
                    foreach (AutoTurret turret in AutoTurrets)
                    {
                        turret.inventory.Insert(ItemManager.CreateByName(shortname, amountPerTurret));
                        turret.Reload();
                    }
                }
            }

            internal void UpdateAmmoFlameTurret(int amount)
            {
                if (FlameTurrets.Count == 0) return;
                foreach (FlameTurret turret in FlameTurrets)
                {
                    for (int i = turret.inventory.itemList.Count - 1; i >= 0; i--)
                    {
                        Item item = turret.inventory.itemList[i];
                        amount += item.amount;
                        item.RemoveFromContainer();
                        item.Remove();
                    }
                }
                if (amount < 1) return;
                int amountTurrets = FlameTurrets.Count;
                int amountInventory = amount % amountTurrets;
                if (amountInventory > 0) AddInventory("lowgradefuel", amountInventory, 0, "");
                int amountPerTurret = (amount - amountInventory) / amountTurrets;
                if (amountPerTurret > 0) foreach (FlameTurret turret in FlameTurrets) turret.inventory.Insert(ItemManager.CreateByName("lowgradefuel", amountPerTurret));
            }

            internal void UpdateAmmoGunTrap(int amount)
            {
                if (GunTraps.Count == 0) return;
                foreach (GunTrap turret in GunTraps)
                {
                    for (int i = turret.inventory.itemList.Count - 1; i >= 0; i--)
                    {
                        Item item = turret.inventory.itemList[i];
                        amount += item.amount;
                        item.RemoveFromContainer();
                        item.Remove();
                    }
                }
                if (amount < 1) return;
                int amountTurrets = GunTraps.Count;
                int amountInventory = amount % amountTurrets;
                if (amountInventory > 0) AddInventory("ammo.handmade.shell", amountInventory, 0, "");
                int amountPerTurret = (amount - amountInventory) / amountTurrets;
                if (amountPerTurret > 0) foreach (GunTrap turret in GunTraps) turret.inventory.Insert(ItemManager.CreateByName("ammo.handmade.shell", amountPerTurret));
            }

            internal int GetCountAmmoAutoTurrets(string shortname)
            {
                if (AutoTurrets.Count == 0) return 0;
                int amount = 0;
                foreach (AutoTurret turret in AutoTurrets)
                {
                    if (turret == null) continue;
                    foreach (Item item in turret.inventory.itemList) if (item.info.shortname == shortname) amount += item.amount;
                    BaseProjectile baseProjectile = turret.AttachedWeapon as BaseProjectile;
                    if (baseProjectile == null) continue;
                    if (baseProjectile.primaryMagazine.ammoType.shortname == shortname) amount += baseProjectile.primaryMagazine.contents;
                }
                return amount;
            }

            internal int GetCountAmmoFlameTurrets()
            {
                if (FlameTurrets.Count == 0) return 0;
                int amount = 0;
                foreach (FlameTurret turret in FlameTurrets) foreach (Item item in turret.inventory.itemList) amount += item.amount;
                return amount;
            }

            internal int GetCountAmmoGunTraps()
            {
                if (GunTraps.Count == 0) return 0;
                int amount = 0;
                foreach (GunTrap turret in GunTraps) foreach (Item item in turret.inventory.itemList) amount += item.amount;
                return amount;
            }

            private void Evacuation()
            {
                Destroy(GeneralSphere);
                Vector3 pos; Quaternion rot;
                GetGlobal(transform, Config.HelicopterLocation.Position.ToVector3(), Config.HelicopterLocation.Rotation.ToVector3(), out pos, out rot);
                _targetCh47Pos = pos;
                _targetCh47Rot = rot;
                _spawnCh47Pos = new Vector3(_targetCh47Pos.x > 0f ? World.Size / 2 : -World.Size / 2, 200f, _targetCh47Pos.z > 0f ? World.Size / 2 : -World.Size / 2);
                InvokeRepeating(UpdateCh47, 0f, 1f);
            }

            private void UpdateCh47()
            {
                if (StageCh47 == 0)
                {
                    SpawnNewCh47(_spawnCh47Pos, Quaternion.identity, new Vector3(_targetCh47Pos.x, 200f, _targetCh47Pos.z));
                    StageCh47++;
                }
                else if (StageCh47 == 1)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), new Vector2(_targetCh47Pos.x, _targetCh47Pos.z)) < 1f)
                    {
                        SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, _targetCh47Pos);
                        foreach (Door door in _evacuationDoors) door.SetOpen(true);
                        _elevator.gameObject.AddComponent<AnimationLift>();
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 2)
                {
                    if (Ch47.transform.position.y - _targetCh47Pos.y < 100f)
                    {
                        Ch47.transform.rotation = _targetCh47Rot;
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 3)
                {
                    if (Ch47.transform.position.y - _ch47Ai.currentDesiredAltitude < 1f)
                    {
                        _ch47Ai.AiAltitudeForce = 0f;
                        _ch47Ai.rigidBody.AddForce(Vector3.down * 10000f, ForceMode.Force);
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 4)
                {
                    _ch47Ai.rigidBody.AddForce(Vector3.down * 10000f, ForceMode.Force);
                    if (Ch47.transform.position.y - _targetCh47Pos.y < 1.5f)
                    {
                        _ch47Ai.rigidBody.isKinematic = true;
                        Ch47.transform.position = _targetCh47Pos;
                        General.gameObject.AddComponent<AnimationGeneral>();
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 5)
                {
                    if (General == null)
                    {
                        _ins.AlertToAllPlayers("GeneralEvacuationEnd", _ins._config.Prefix, _ins._config.PreFinishTime);
                        SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, _spawnCh47Pos);
                        StageCh47++;
                        _finishCoroutine = ServerMgr.Instance.StartCoroutine(ProcessFinishEvent());
                    }
                }
                else if (StageCh47 == 6)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), new Vector2(_spawnCh47Pos.x, _spawnCh47Pos.z)) < 1f)
                    {
                        if (Ch47.IsExists()) Ch47.Kill();
                        CancelInvoke(UpdateCh47);
                    }
                }
            }

            private void SpawnNewCh47(Vector3 pos, Quaternion rot, Vector3 landingTarget)
            {
                CH47Helicopter ch47New = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", pos, rot) as CH47Helicopter;
                CH47HelicopterAIController ch47AInew = ch47New.GetComponent<CH47HelicopterAIController>();
                ch47AInew.SetLandingTarget(landingTarget);
                if (Ch47.IsExists()) Ch47.Kill();
                Ch47 = ch47New;
                _ch47Ai = ch47AInew;
                Ch47.Spawn();
                _ch47Ai.CancelInvoke(_ch47Ai.SpawnScientists);
                Ch47.rigidBody.detectCollisions = false;
                _ch47Ai.numCrates = 0;
                _ch47Ai.SetMinHoverHeight(0f);
            }

            private void ClearNpc()
            {
                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();
                foreach (CustomBarricade barricade in CustomBarricades) barricade.ClearAllZombies();
                GeneralSphere.ClearAllZombies();
                Scientists.Clear();
            }

            internal void KillGeneral()
            {
                GeneralHealth = 0f;
                General.Kill();
                General = null;
                Interface.CallHook("OnGeneralKill");
                _ins.AlertToAllPlayers("GeneralKill", _ins._config.Prefix, _ins._config.PreFinishTime);
                ClearNpc();
                if (AllStageMission != 0) CompleteStageMission = AllStageMission = 0;
                if (!string.IsNullOrEmpty(KeyPassword))
                {
                    MessagePassword = string.Empty;
                    KeyPassword = string.Empty;
                    Password.Clear();
                    _rewardMission = null;
                    foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "MessagePassword_DefendableBases");
                }
                _finishCoroutine = ServerMgr.Instance.StartCoroutine(ProcessFinishEvent());
            }

            private IEnumerator ProcessFinishEvent()
            {
                Seconds = MaxSeconds = _ins._config.PreFinishTime;
                while (Seconds > 0)
                {
                    foreach (BasePlayer player in Players) _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat() });
                    yield return CoroutineEx.waitForSeconds(1f);
                    Seconds--;
                }
                _ins.Finish();
            }

            private List<Vector3> GetPositionCapture()
            {
                RaycastHit raycastHit;
                int attempts = 0;
                while (attempts < 100)
                {
                    attempts++;

                    float radius = UnityEngine.Random.Range(0f, _blazerRadius);
                    float degrees = UnityEngine.Random.Range(0f, 360f);

                    Vector3 center = new Vector3(transform.position.x + radius * Mathf.Sin(degrees * Mathf.Deg2Rad), 500f, transform.position.z + radius * Mathf.Cos(degrees * Mathf.Deg2Rad));
                    if (!Physics.Raycast(center, Vector3.down, out raycastHit, 500f, 1 << 16 | 1 << 23)) continue;
                    center.y = raycastHit.point.y;

                    if (Physics.OverlapSphere(center, 4f).Any(s => s.ToBaseEntity().IsExists() && s.ToBaseEntity().ShortPrefabName != "autoturret_deployed")) continue;

                    List<Vector3> result = new List<Vector3> { center };

                    bool isContinue = false;
                    for (int i = 1; i <= 12; i++)
                    {
                        Vector3 pos = new Vector3(center.x + 2f * Mathf.Sin(i * 30f * Mathf.Deg2Rad), 500f, center.z + 2f * Mathf.Cos(i * 30f * Mathf.Deg2Rad));
                        if (!Physics.Raycast(pos, Vector3.down, out raycastHit, 500f, 1 << 16 | 1 << 23))
                        {
                            isContinue = true;
                            break;
                        }
                        pos.y = raycastHit.point.y;
                        if (Math.Abs(center.y - pos.y) > 1f)
                        {
                            isContinue = true;
                            break;
                        }
                        result.Add(pos);
                    }
                    if (isContinue) continue;

                    return result;
                }
                return null;
            }

            private void SpawnCapturePoint()
            {
                List<Vector3> positions = GetPositionCapture();
                if (positions == null) return;
                for (int i = 1; i < positions.Count; i++)
                {
                    Vector3 pos = positions[i];
                    FlasherLight flasher = SpawnEntity("assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos - new Vector3(0f, 0.2f, 0f), Quaternion.identity) as FlasherLight;
                    Flashers.Add(flasher);
                    flasher.UpdateFromInput(1, 0);
                }
                Siren = SpawnEntity("assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab", positions[0] - new Vector3(0f, 0.4f, 0f), Quaternion.identity) as SirenLight;
                CaptureSphereCollider = new GameObject().gameObject.AddComponent<CaptureSphereCollider>();
                CaptureSphereCollider.transform.position = positions[0];
                _romancandle = SpawnEntity("assets/prefabs/deployable/fireworks/romancandle.prefab", positions[0], Quaternion.identity) as PFXRepeatingFirework;
                _volcan = SpawnEntity("assets/prefabs/deployable/fireworks/volcanofirework-red.prefab", positions[0], Quaternion.identity) as BaseFirework;
                _romancandle.Ignite(Vector3.zero);
                _volcan.Ignite(Vector3.zero);
            }

            internal void SuccessfulCapture()
            {
                DestroyCapture();
                CompleteStageMission++;
                if (CompleteStageMission == AllStageMission)
                {
                    GiveRewardMission();
                    _rewardMission = null;
                }
                else SpawnCapturePoint();
            }

            private void DestroyCapture()
            {
                if (CaptureSphereCollider != null) Destroy(CaptureSphereCollider.gameObject);
                if (Siren.IsExists()) Siren.Kill();
                if (_romancandle.IsExists()) _romancandle.Kill();
                if (_volcan.IsExists()) _volcan.Kill();
                foreach (FlasherLight flasher in Flashers) if (flasher.IsExists()) flasher.Kill();
                Flashers.Clear();
            }

            internal void SuccessfulPassword()
            {
                CompleteStageMission++;
                if (CompleteStageMission == AllStageMission)
                {
                    MessagePassword = string.Empty;
                    KeyPassword = string.Empty;
                    Password.Clear();
                    foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "MessagePassword_DefendableBases");
                    GiveRewardMission();
                    _rewardMission = null;
                }
                else
                {
                    SetPassword();
                    foreach (BasePlayer player in Players) _ins.MessagePassword(player);
                }
            }

            private void GiveRewardMission()
            {
                if (_rewardMission.UseCount)
                {
                    int count = UnityEngine.Random.Range(_rewardMission.Min, _rewardMission.Max + 1);
                    HashSet<int> indexMove = new HashSet<int>();
                    while (indexMove.Count < count)
                    {
                        foreach (ItemComputer item in _rewardMission.Items)
                        {
                            if (indexMove.Contains(_rewardMission.Items.IndexOf(item))) continue;
                            if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                            {
                                AddInventory(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID, item.Name);
                                indexMove.Add(_rewardMission.Items.IndexOf(item));
                                if (indexMove.Count == count) return;
                            }
                        }
                    }
                }
                else
                {
                    HashSet<int> indexMove = new HashSet<int>();
                    foreach (ItemComputer item in _rewardMission.Items)
                    {
                        if (indexMove.Contains(_rewardMission.Items.IndexOf(item))) continue;
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                        {
                            AddInventory(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID, item.Name);
                            indexMove.Add(_rewardMission.Items.IndexOf(item));
                        }
                    }
                }
            }

            internal void SetPassword()
            {
                KeyPassword = $"{GetRandomSymbol}{GetRandomDigit}{GetRandomDigit}";
                Password.Clear();
                for (int i = 0; i < 6; i++) Password.Add(GetRandomDigit);
                MessagePassword = "";
                int pos = UnityEngine.Random.Range(0, 3);
                for (int i = 0; i < 4; i++)
                {
                    MessagePassword += i == pos ? $"{KeyPassword} - {Password[0]}{Password[1]}{Password[2]}{Password[3]}{Password[4]}{Password[5]}" : $"{GetRandomSymbol}{GetRandomDigit}{GetRandomDigit} - {GetRandomDigit}{GetRandomDigit}{GetRandomDigit}{GetRandomDigit}{GetRandomDigit}{GetRandomDigit}";
                    if (i != 3) MessagePassword += " | ";
                }
            }

            internal bool CorrectPassword()
            {
                if (_ins._password.Count < 6) return false;
                for (int i = 0; i < 6; i++) if (_ins._password[i] != Password[i]) return false;
                return true;
            }

            private char GetRandomSymbol => Symbols.GetRandom();

            private int GetRandomDigit => Digits.GetRandom();

            private void SpawnMapMarker()
            {
                _mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position) as MapMarkerGenericRadius;
                _mapmarker.Spawn();
                _mapmarker.radius = _ins._config.Marker.Radius;
                _mapmarker.alpha = _ins._config.Marker.Alpha;
                _mapmarker.color1 = new Color(_ins._config.Marker.Color.R, _ins._config.Marker.Color.G, _ins._config.Marker.Color.B);

                _vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position) as VendingMachineMapMarker;
                _vendingMarker.markerShopName = _ins._config.Marker.Name.Replace("{wave}", "0");
                _vendingMarker.Spawn();
            }

            private void UpdateMapMarker(int wave)
            {
                _mapmarker.SendUpdate();
                _vendingMarker.markerShopName = _ins._config.Marker.Name.Replace("{wave}", $"{wave}");
                _vendingMarker.SendNetworkUpdate();
            }

            private void ClearEntities() { foreach (BaseEntity entity in GetEntities<BaseEntity>(transform.position, Config.Radius, -1)) if (_ins._config.DeletePrefabs.Contains(entity.ShortPrefabName) && entity.IsExists() && !Entities.Contains(entity)) entity.Kill(); }
        }
        #endregion Controller

        #region General Sphere Collider
        internal class GeneralSphereCollider : FacepunchBehaviour
        {
            private SphereCollider _sphereCollider;
            internal HashSet<ulong> Zombies = new HashSet<ulong>();
            private float _damagePerSec = 0f;

            private void Awake()
            {
                transform.position = _ins._controller.General.transform.position;
                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = 1.5f;
            }

            private void OnDestroy()
            {
                CancelInvoke(TakeDamage);
                Destroy(_sphereCollider);
            }

            private void OnTriggerEnter(Collider other)
            {
                ScientistNPC npc = other.GetComponentInParent<ScientistNPC>();
                if (npc.IsExists() && npc.displayName == "Sledge")
                {
                    Zombies.Add(npc.net.ID.Value);
                    if (_damagePerSec == 0f) InvokeRepeating(TakeDamage, 1f, 1f);
                    _damagePerSec += _ins._config.Sledge.DamagePerSec;
                }
            }

            private void OnTriggerExit(Collider other)
            {
                ScientistNPC npc = other.GetComponentInParent<ScientistNPC>();
                if (npc.IsExists() && npc.displayName == "Sledge") ExitSledge(npc);
            }

            private void TakeDamage()
            {
                _ins._controller.GeneralHealth -= _damagePerSec;
                UpdateHealth();
            }

            internal void UpdateHealth()
            {
                if (_ins._controller.GeneralHealth <= 0f)
                {
                    CancelInvoke(TakeDamage);
                    _ins._controller.KillGeneral();
                    Destroy(this);
                }
            }

            internal void ExitSledge(ScientistNPC npc)
            {
                _damagePerSec -= _ins._config.Sledge.DamagePerSec;
                if (_damagePerSec <= 0f)
                {
                    _damagePerSec = 0f;
                    CancelInvoke(TakeDamage);
                }
                Zombies.Remove(npc.net.ID.Value);
            }

            internal void ClearAllZombies()
            {
                CancelInvoke(TakeDamage);
                Zombies.Clear();
                _damagePerSec = 0f;
            }
        }
        #endregion General Sphere Collider

        #region Capture Sphere Collider
        internal class CaptureSphereCollider : FacepunchBehaviour
        {
            private SphereCollider _sphereCollider;
            internal readonly HashSet<BasePlayer> Players = new HashSet<BasePlayer>();
            private float _percentPerSec = 15f;
            private float _allPercent = 0f;

            private void Awake()
            {
                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = 2f;
            }

            private void OnDestroy()
            {
                CancelInvoke(Progress);
                foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "Capture_DefendableBases");
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Add(player);
                    if (_allPercent == 0f)
                    {
                        InvokeRepeating(Progress, 1f, 1f);
                        _ins._controller.Siren.UpdateFromInput(1, 0);
                    }
                }
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer()) ExitPlayer(player);
            }

            private void Progress()
            {
                if (Players.Count == 0)
                {
                    _allPercent -= _percentPerSec;
                    if (_allPercent <= 0f)
                    {
                        _allPercent = 0f;
                        _ins._controller.Siren.UpdateFromInput(0, 0);
                        CancelInvoke(Progress);
                    }
                }
                else
                {
                    int scale = Players.Count > 3 ? 3 : Players.Count;
                    _allPercent += _percentPerSec * scale;
                    foreach (BasePlayer player in Players) _ins.UpdateCaptureGUI(player, _allPercent);
                    if (_allPercent >= 100f)
                    {
                        CancelInvoke(Progress);
                        _ins._controller.SuccessfulCapture();
                    }
                }
            }

            internal void ExitPlayer(BasePlayer player)
            {
                Players.Remove(player);
                CuiHelper.DestroyUi(player, "Capture_DefendableBases");
            }
        }
        #endregion Capture Sphere Collider

        #region Animations
        internal class AnimationGeneral : FacepunchBehaviour
        {
            private float _secondsTaken;
            private float _secondsToTake;
            private float _waypointDone;
            private Vector3 _startPos;
            private Vector3 _endPos;
            private BasePlayer _general;

            private void Awake()
            {
                _general = GetComponent<BasePlayer>();
                _startPos = _general.transform.position;
                _endPos = _ins._controller.GetGlobalPos(_ins._controller.Config.EvacuationPosition.ToVector3());
                _secondsToTake = Vector3.Distance(_endPos, _startPos) / 0.5f;
            }

            private void FixedUpdate()
            {
                _secondsTaken += Time.deltaTime;
                _waypointDone = Mathf.InverseLerp(0f, _secondsToTake, _secondsTaken);
                _general.transform.position = Vector3.Lerp(_startPos, _endPos, _waypointDone);
                if (_waypointDone >= 0.5f) _general.viewAngles = Quaternion.LookRotation(_endPos - _startPos).eulerAngles;
                _general.TransformChanged();
                _general.SendNetworkUpdate();
                if (_waypointDone >= 1f)
                {
                    if (_general.IsExists()) _general.Kill();
                    return;
                }
            }
        }

        internal class AnimationLift : FacepunchBehaviour
        {
            private float _secondsTaken;
            private float _secondsToTake;
            private float _waypointDone;
            private Vector3 _startPos;
            private Vector3 _endPos;
            private ElevatorLiftStatic _lift;

            private void Awake()
            {
                _lift = GetComponent<ElevatorLiftStatic>();
                _startPos = _lift.transform.position;
                _endPos = _startPos + new Vector3(0f, _ins._controller.Config.ElevatorHeight, 0f);
                _secondsToTake = Vector3.Distance(_endPos, _startPos) / 1f;
            }

            private void FixedUpdate()
            {
                _secondsTaken += Time.deltaTime;
                _waypointDone = Mathf.InverseLerp(0f, _secondsToTake, _secondsTaken);
                _lift.transform.position = Vector3.Lerp(_startPos, _endPos, _waypointDone);
                if (_waypointDone >= 1f)
                {
                    Destroy(this);
                    return;
                }
            }
        }
        #endregion Animations

        #region Spawn Position
        private HashSet<MonumentInfo> _monuments = new HashSet<MonumentInfo>();

        private readonly HashSet<string> _unnecessaryMonuments = new HashSet<string>
        {
            "Substation",
            "Outpost",
            "Bandit Camp",
            "Fishing Village",
            "Large Fishing Village",
            "Ranch",
            "Large Barn",
            "Ice Lake",
            "Mountain"
        };

        private static string GetNameMonument(MonumentInfo monument)
        {
            if (monument.name.Contains("harbor_1")) return "Small " + monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("harbor_2")) return "Large " + monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("desert_military_base_a")) return monument.displayPhrase.english.Replace("\n", string.Empty) + " A";
            if (monument.name.Contains("desert_military_base_b")) return monument.displayPhrase.english.Replace("\n", string.Empty) + " B";
            if (monument.name.Contains("desert_military_base_c")) return monument.displayPhrase.english.Replace("\n", string.Empty) + " C";
            if (monument.name.Contains("desert_military_base_d")) return monument.displayPhrase.english.Replace("\n", string.Empty) + " D";
            return monument.displayPhrase.english.Replace("\n", string.Empty);
        }

        private bool IsNecessaryMonument(MonumentInfo monument)
        {
            string name = GetNameMonument(monument);
            if (string.IsNullOrEmpty(name) || _unnecessaryMonuments.Contains(name)) return false;
            return Configs.Any(x => x.Monuments.Any(y => y.Name == name));
        }

        public class Location { public Vector3 Position; public Quaternion Rotation; public float BlazerRadius; public List<Vector3> NpcPositions; public List<Vector3> CapturePositions; public string Name; }

        private Location GetLocation(string name)
        {
            BaseConfig config = Configs.FirstOrDefault(x => x.Name == name);

            List<Location> list = new List<Location>();

            foreach (MonumentInfo monument in _monuments)
            {
                MonumentLocationsConfig monumentConfig = config.Monuments.FirstOrDefault(x => x.Name == GetNameMonument(monument));
                if (monumentConfig == null) continue;
                foreach (LocationsConfig location in monumentConfig.Locations)
                {
                    list.Add(new Location
                    {
                        Position = monument.transform.TransformPoint(location.Position.ToVector3()),
                        Rotation = monument.transform.rotation * Quaternion.Euler(location.Rotation.ToVector3()),
                        BlazerRadius = location.BlazerRadius,
                        NpcPositions = location.NpcPositions.Select(x => monument.transform.TransformPoint(x.ToVector3())),
                        Name = GetNameMonument(monument)
                    });
                }
            }

            foreach (CustomMapConfig customMap in _customMaps)
                foreach (CustomMapBaseLocationsConfig customMapBase in customMap.Bases)
                    if (customMapBase.NameBase == name)
                        foreach (LocationsConfig location in customMapBase.Locations)
                            list.Add(new Location
                            {
                                Position = location.Position.ToVector3(),
                                Rotation = Quaternion.Euler(location.Rotation.ToVector3()),
                                BlazerRadius = location.BlazerRadius,
                                NpcPositions = location.NpcPositions.Select(x => x.ToVector3()),
                                Name = string.Empty
                            });

            return list.GetRandom();
        }

        public class CustomMapBaseLocationsConfig
        {
            [JsonProperty(En ? "Name of the base" : "Название базы")] public string NameBase { get; set; }
            [JsonProperty(En ? "List of locations" : "Список расположений")] public HashSet<LocationsConfig> Locations { get; set; }
        }

        public class CustomMapConfig
        {
            [JsonProperty(En ? "ID" : "Идентификатор")] public string ID { get; set; }
            [JsonProperty(En ? "List of bases" : "Список баз")] public HashSet<CustomMapBaseLocationsConfig> Bases { get; set; }
        }

        private void LoadCustomMapLocations()
        {
            Puts("Loading files on the /oxide/data/DefendableBases/CustomMap/ path has started...");
            foreach (string name in Interface.Oxide.DataFileSystem.GetFiles("DefendableBases/CustomMap/"))
            {
                string fileName = name.Split('/').Last().Split('.').First();
                CustomMapConfig config = Interface.Oxide.DataFileSystem.ReadObject<CustomMapConfig>($"DefendableBases/CustomMap/{fileName}");
                if (config == null)
                {
                    PrintError($"File {fileName} is corrupted and cannot be loaded!");
                    continue;
                }
                if (!_ids.Any(x => Math.Abs(x - Convert.ToSingle(config.ID)) < 0.001f))
                {
                    PrintWarning($"File {fileName} cannot be loaded on the current map!");
                    continue;
                }
                Puts($"File {fileName} has been loaded successfully!");
                _customMaps.Add(config);
            }
        }

        private readonly HashSet<CustomMapConfig> _customMaps = new HashSet<CustomMapConfig>();

        private readonly HashSet<float> _ids = new HashSet<float>();

        private void LoadIDs() { foreach (RANDSwitch entity in BaseNetworkable.serverEntities.OfType<RANDSwitch>()) _ids.Add(entity.transform.position.x + entity.transform.position.y + entity.transform.position.z); }
        #endregion Spawn Position

        #region Custom Barricade
        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (!player.IsPlayer()) return;

            bool isNewItemCustomBarricade = IsCustomBarricadeItem(newItem);
            bool isOldItemCustomBarricade = IsCustomBarricadeItem(oldItem);
            bool isController = _controllersCustomBarricade.ContainsKey(player.userID);

            if (isNewItemCustomBarricade && !isOldItemCustomBarricade && !isController)
            {
                ControllerPlayerCustomBarricade controller = player.gameObject.AddComponent<ControllerPlayerCustomBarricade>();
                _controllersCustomBarricade.Add(player.userID, controller);
            }
            else if (!isNewItemCustomBarricade && isOldItemCustomBarricade && isController)
            {
                UnityEngine.Object.Destroy(_controllersCustomBarricade[player.userID]);
                _controllersCustomBarricade.Remove(player.userID);
            }
        }

        private static bool IsCustomBarricadeItem(Item item) => item != null && item.info.shortname == "wall.frame.netting" && (item.skin == 2777422016 || item.skin == 2777422268 || item.skin == 2803087918 || item.skin == 2777422630 || item.skin == 2777422812);

        private bool IsCustomBarricade(ulong id) => _controller.CustomBarricades.Any(x => x.NetID == id);

        internal class CustomBarricade : BuildingBlock
        {
            internal ulong NetID;
            internal int Level;
            private SphereCollider _sphereCollider;
            internal HashSet<BaseEntity> Entities = new HashSet<BaseEntity>();
            internal HashSet<ulong> Zombies = new HashSet<ulong>();
            private SimpleLight _light = null;
            private PowerCounter _counter = null;
            private float _damagePerSec = 0f;
            private float _healthPerHit = 0f;
            internal float MaximumHealth = 0f;
            internal float MainHealth = 0f;

            private void Awake()
            {
                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = 1.5f;
            }

            internal void Init(int level, SimpleLight light, PowerCounter counter, ulong netID)
            {
                NetID = netID;

                _ins._controller.CustomBarricades.Add(this);

                Level = level;

                BarricadeConfig config = _ins._config.CustomBarricades.FirstOrDefault(x => x.Level == Level);
                MainHealth = MaximumHealth = config.Health;
                _healthPerHit = config.HealthRepair;

                _light = light;
                if (_light != null) _light.UpdateFromInput(0, 0);

                _counter = counter;
                if (_counter != null)
                {
                    _counter.UpdateFromInput(1, 0);
                    SetHealthCounter();
                }

                Entities.Add(SpawnEntity("assets/prefabs/deployable/barricades/barricade.concrete.prefab", GetGlobalPos(new Vector3(0f, 0f, 0f)), GetGlobalRot(new Vector3(0f, 90f, 0f))));
                if (level == 0) Entities.Add(SpawnEntity("assets/prefabs/deployable/door barricades/door_barricade_dbl_b.prefab", GetGlobalPos(new Vector3(0f, 0.5f, 0f)), GetGlobalRot(new Vector3(0f, 270f, 0f))));
                else if (level == 1)
                {
                    Entities.Add(SpawnEntity("assets/prefabs/deployable/door barricades/door_barricade_dbl_b.prefab", GetGlobalPos(new Vector3(0f, 0.5f, 0f)), GetGlobalRot(new Vector3(0f, 270f, 0f))));
                    Entities.Add(SpawnEntity("assets/prefabs/deployable/playerioents/generators/solar_panels_roof/solarpanel.large.deployed.prefab", GetGlobalPos(new Vector3(0.313f, 0.236f, -0.397f)), GetGlobalRot(new Vector3(0f, 0f, 120f))));
                    Entities.Add(SpawnEntity("assets/prefabs/deployable/playerioents/generators/solar_panels_roof/solarpanel.large.deployed.prefab", GetGlobalPos(new Vector3(0.313f, 0.236f, 0.368f)), GetGlobalRot(new Vector3(0f, 0f, 120f))));
                    Entities.Add(SpawnEntity("assets/prefabs/deployable/playerioents/generators/solar_panels_roof/solarpanel.large.deployed.prefab", GetGlobalPos(new Vector3(-0.299f, 0.236f, 0.368f)), GetGlobalRot(new Vector3(0f, 180f, 120f))));
                    Entities.Add(SpawnEntity("assets/prefabs/deployable/playerioents/generators/solar_panels_roof/solarpanel.large.deployed.prefab", GetGlobalPos(new Vector3(-0.299f, 0.236f, -0.397f)), GetGlobalRot(new Vector3(0f, 180f, 120f))));
                    Entities.Add(SpawnEntity("assets/prefabs/misc/xmas/neon_sign/sign.neon.xl.prefab", GetGlobalPos(new Vector3(0.164f, 0.245f, 0f)), GetGlobalRot(new Vector3(0f, 270f, 0f))));
                }
                else if (level == 2)
                {
                    BuildingBlock entity_1 = SpawnEntity("assets/prefabs/building core/floor.triangle.frame/floor.triangle.frame.prefab", GetGlobalPos(new Vector3(0f, 2.875f, 0f)), GetGlobalRot(new Vector3(90f, 90f, 0f))) as BuildingBlock;
                    entity_1.SetGrade(BuildingGrade.Enum.Stone);
                    entity_1.SetHealthToMax();
                    Entities.Add(entity_1);

                    BuildingBlock entity_2 = SpawnEntity("assets/prefabs/building core/floor.frame/floor.frame.prefab", GetGlobalPos(new Vector3(0f, 1.375f, 0f)), GetGlobalRot(new Vector3(90f, 90f, 0f))) as BuildingBlock;
                    entity_2.SetGrade(BuildingGrade.Enum.Stone);
                    entity_2.SetHealthToMax();
                    Entities.Add(entity_2);

                    BuildingBlock entity_3 = SpawnEntity("assets/prefabs/building core/floor.frame/floor.frame.prefab", GetGlobalPos(new Vector3(0f, 0.875f, 0f)), GetGlobalRot(new Vector3(90f, 90f, 0f))) as BuildingBlock;
                    entity_3.SetGrade(BuildingGrade.Enum.Stone);
                    entity_3.SetHealthToMax();
                    Entities.Add(entity_3);

                    BuildingBlock entity_4 = SpawnEntity("assets/prefabs/building core/floor.frame/floor.frame.prefab", GetGlobalPos(new Vector3(0f, 0.375f, 0f)), GetGlobalRot(new Vector3(90f, 90f, 0f))) as BuildingBlock;
                    entity_4.SetGrade(BuildingGrade.Enum.Stone);
                    entity_4.SetHealthToMax();
                    Entities.Add(entity_4);

                    BuildingBlock entity_5 = SpawnEntity("assets/prefabs/building core/floor.frame/floor.frame.prefab", GetGlobalPos(new Vector3(0f, -0.125f, 0f)), GetGlobalRot(new Vector3(90f, 90f, 0f))) as BuildingBlock;
                    entity_5.SetGrade(BuildingGrade.Enum.Stone);
                    entity_5.SetHealthToMax();
                    Entities.Add(entity_5);

                    Entities.Add(SpawnEntity("assets/prefabs/deployable/door barricades/door_barricade_dbl_b.prefab", GetGlobalPos(new Vector3(-0.032f, -0.738f, 0f)), GetGlobalRot(new Vector3(0f, 270f, 0f))));
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign1.prefab", GetGlobalPos(new Vector3(0.097f, -2.013f, 0f)), GetGlobalRot(new Vector3(13.172f, 180f, 0f))));
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign6.prefab", GetGlobalPos(new Vector3(0.097f, -1.289f, 0f)), GetGlobalRot(new Vector3(0f, 180f, 0f))));
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign9.prefab", GetGlobalPos(new Vector3(0.097f, -2.013f, 0f)), GetGlobalRot(new Vector3(346.296f, 180f, 0f))));
                    Entities.Add(SpawnEntity("assets/prefabs/misc/xmas/neon_sign/sign.neon.xl.prefab", GetGlobalPos(new Vector3(0.054f, 0.268f, 0f)), GetGlobalRot(new Vector3(0f, 270f, 0f))));
                }
                else if (level == 3)
                {
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign1.prefab", GetGlobalPos(new Vector3(0.096f, -1.949f, 0.045f)), GetGlobalRot(new Vector3(7.47f, 180f, 0f))));
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign9.prefab", GetGlobalPos(new Vector3(0.071f, -0.603f, -0.861f)), GetGlobalRot(new Vector3(329.49f, 180f, 0f))));
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign2.prefab", GetGlobalPos(new Vector3(0.071f, -0.556f, 0.79f)), GetGlobalRot(new Vector3(30.51f, 180f, 0f))));
                    Entities.Add(SpawnEntity("assets/prefabs/misc/xmas/neon_sign/sign.neon.xl.prefab", GetGlobalPos(new Vector3(0f, 0.383f, 0.28f)), GetGlobalRot(new Vector3(0f, 90f, 0f))));
                    Entities.Add(SpawnEntity("assets/prefabs/misc/xmas/neon_sign/sign.neon.xl.prefab", GetGlobalPos(new Vector3(0f, 0.383f, -0.249f)), GetGlobalRot(new Vector3(0f, 90f, 0f))));
                    Entities.Add(SpawnEntity("assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.a.prefab", GetGlobalPos(new Vector3(-0.11f, 1.184f, 0f)), GetGlobalRot(new Vector3(0f, 0f, 0f))));
                }
                else if (level == 4)
                {
                    BuildingBlock entity_1 = SpawnEntity("assets/prefabs/building core/wall.window/wall.window.prefab", GetGlobalPos(new Vector3(0f, 0f, 0f)), GetGlobalRot(new Vector3(0f, 180f, 0f))) as BuildingBlock;
                    entity_1.SetGrade(BuildingGrade.Enum.TopTier);
                    entity_1.SetHealthToMax();
                    Entities.Add(entity_1);

                    Entities.Add(SpawnEntity("assets/prefabs/building/wall.window.bars/wall.window.bars.toptier.prefab", GetGlobalPos(new Vector3(0f, 1f, 0f)), GetGlobalRot(new Vector3(0f, 0f, 0f))));
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign4.prefab", GetGlobalPos(new Vector3(0.096f, -0.729f, 0.95f)), GetGlobalRot(new Vector3(30.027f, 180f, 0.25f))));
                    Entities.Add(SpawnEntity("assets/content/props/roadsigns/roadsign6.prefab", GetGlobalPos(new Vector3(0.096f, -1.063f, -0.742f)), GetGlobalRot(new Vector3(332.73f, 180f, 0.505f))));
                    Entities.Add(SpawnEntity("assets/prefabs/misc/halloween/skull_door_knocker/skull_door_knocker.deployed.prefab", GetGlobalPos(new Vector3(0f, 2.576f, 0f)), GetGlobalRot(new Vector3(0f, 270f, 180f))));
                }

                Interface.CallHook("OnCustomBarricadeSpawn", transform.position);
            }

            private void OnDestroy()
            {
                CancelInvoke(TakeDamage);
                Destroy(_sphereCollider);
                foreach (BaseEntity entity in Entities) if (entity.IsExists()) entity.Kill(DestroyMode.Gib);
                if (_light != null) _light.UpdateFromInput(1, 0);
                if (_counter != null) _counter.UpdateFromInput(0, 0);
            }

            private void OnTriggerEnter(Collider other)
            {
                ScientistNPC npc = other.GetComponentInParent<ScientistNPC>();
                if (npc.IsExists() && npc.displayName == "Sledge")
                {
                    Zombies.Add(npc.net.ID.Value);
                    if (_damagePerSec == 0f) InvokeRepeating(TakeDamage, 1f, 1f);
                    _damagePerSec += _ins._config.Sledge.DamagePerSec;
                }
            }

            private void OnTriggerExit(Collider other)
            {
                ScientistNPC npc = other.GetComponentInParent<ScientistNPC>();
                if (npc.IsExists() && npc.displayName == "Sledge") ExitSledge(npc);
            }

            private Vector3 GetGlobalPos(Vector3 localPos) => transform.TransformPoint(localPos);

            private Quaternion GetGlobalRot(Vector3 localRot) => transform.rotation * Quaternion.Euler(localRot);

            private void TakeDamage()
            {
                MainHealth -= _damagePerSec;
                UpdateHealth();
            }

            internal void UpdateHealth()
            {
                SetHealthCounter();
                if (MainHealth <= 0f)
                {
                    CancelInvoke(TakeDamage);
                    _ins._controller.CustomBarricades.Remove(this);
                    Interface.CallHook("OnCustomBarricadeKill", transform.position);
                    Destroy(this);
                }
            }

            internal void HitRepair()
            {
                MainHealth += _healthPerHit;
                if (MainHealth > MaximumHealth) MainHealth = MaximumHealth;
                SetHealthCounter();
            }

            internal void ExitSledge(ScientistNPC npc)
            {
                _damagePerSec -= _ins._config.Sledge.DamagePerSec;
                if (_damagePerSec <= 0f)
                {
                    _damagePerSec = 0f;
                    CancelInvoke(TakeDamage);
                }
                Zombies.Remove(npc.net.ID.Value);
            }

            internal void ClearAllZombies()
            {
                CancelInvoke(TakeDamage);
                Zombies.Clear();
                _damagePerSec = 0f;
            }

            private void SetHealthCounter()
            {
                _counter.counterNumber = (int)MainHealth;
                _counter.MarkDirty();
                _counter.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }

        private readonly Dictionary<ulong, ControllerPlayerCustomBarricade> _controllersCustomBarricade = new Dictionary<ulong, ControllerPlayerCustomBarricade>();

        internal class ControllerPlayerCustomBarricade : FacepunchBehaviour
        {
            private BasePlayer _player;
            private InputState _serverInput;

            private float DistanceToPlayer(Vector3 position) => Vector3.Distance(position, _player.transform.position);

            private float DotVisionPlayer(Vector3 position) => Vector3.Dot((position - _player.eyes.position).normalized, _player.eyes.BodyForward());

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();
                _serverInput = _player.serverInput;
            }

            private void FixedUpdate()
            {
                if (_serverInput.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    Item activeItem = _player.GetActiveItem();
                    if (activeItem == null) return;

                    int level = activeItem.skin == 2777422016 ? 0 :
                        activeItem.skin == 2777422268 ? 1 :
                        activeItem.skin == 2803087918 ? 2 :
                        activeItem.skin == 2777422630 ? 3 :
                        activeItem.skin == 2777422812 ? 4 : -1;
                    if (level == -1) return;

                    HashSet<BuildingBlock> wallFramesRadiusVision = _ins._controller.WallFrames.Where(x => DistanceToPlayer(x.transform.position) < 3f && DotVisionPlayer(x.transform.position + new Vector3(0f, 1.5f, 0f)) > 0f && !_ins.IsCustomBarricade(x.net.ID.Value));
                    if (wallFramesRadiusVision.Count == 0) return;
                    BuildingBlock wallFrame = wallFramesRadiusVision.Max(x => DotVisionPlayer(x.transform.position + new Vector3(0f, 1.5f, 0f)));
                    ulong wallFrameNetID = wallFrame.net.ID.Value;

                    CustomBarricade customBarricade = wallFrame.gameObject.AddComponent<CustomBarricade>();
                    customBarricade.Init(level, _ins._controller.Lights.ContainsKey(wallFrameNetID) ? _ins._controller.Lights[wallFrameNetID] : null, _ins._controller.Counters[wallFrameNetID], wallFrameNetID);

                    if (activeItem.amount > 1)
                    {
                        activeItem.amount--;
                        activeItem.MarkDirty();
                    }
                    else activeItem.Remove();
                }
            }
        }

        private static Item GiveBarricade(int level, int amount)
        {
            Item result = ItemManager.CreateByName("wall.frame.netting", amount, level == 0 ? 2777422016 : level == 1 ? 2777422268 : level == 2 ? 2803087918 : level == 3 ? 2777422630 : 2777422812);
            result.name = $"Barricade Tier {level}";
            return result;
        }
        #endregion Custom Barricade

        #region Spawn Loot
        #region Crates
        private object CanPopulateLoot(LootContainer container)
        {
            if (container == null || _controller == null) return null;
            if (_controller.CrateToTypeLootTable.ContainsKey(container.net.ID.Value))
            {
                if (_controller.CrateToTypeLootTable[container.net.ID.Value] == 2) return null;
                else return true;
            }
            else return null;
        }

        private object OnCustomLootContainer(NetworkableId netID)
        {
            if (_controller == null) return null;
            if (_controller.CrateToTypeLootTable.ContainsKey(netID.Value))
            {
                if (_controller.CrateToTypeLootTable[netID.Value] == 3) return null;
                else return true;
            }
            else return null;
        }

        private object OnContainerPopulate(LootContainer container)
        {
            if (container == null || _controller == null) return null;
            if (_controller.CrateToTypeLootTable.ContainsKey(container.net.ID.Value))
            {
                if (_controller.CrateToTypeLootTable[container.net.ID.Value] == 6) return null;
                else return true;
            }
            else return null;
        }
        #endregion Crates

        private void AddToContainerPrefab(ItemContainer container, PrefabLootTableConfig lootTable)
        {
            HashSet<string> prefabsInContainer = new HashSet<string>();
            if (lootTable.UseCount)
            {
                int count = 0, max = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (count < max)
                {
                    foreach (PrefabConfig prefab in lootTable.Prefabs)
                    {
                        if (prefabsInContainer.Count < lootTable.Prefabs.Count && prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                        if (UnityEngine.Random.Range(0f, 100f) > prefab.Chance) continue;
                        SpawnIntoContainer(container, prefab.PrefabDefinition);
                        if (!prefabsInContainer.Contains(prefab.PrefabDefinition)) prefabsInContainer.Add(prefab.PrefabDefinition);
                        count++;
                        if (count == max)
                        {
                            prefabsInContainer = null;
                            return;
                        }
                    }
                }
            }
            else
            {
                foreach (PrefabConfig prefab in lootTable.Prefabs)
                {
                    if (prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                    if (UnityEngine.Random.Range(0f, 100f) > prefab.Chance) continue;
                    SpawnIntoContainer(container, prefab.PrefabDefinition);
                    prefabsInContainer.Add(prefab.PrefabDefinition);
                }
            }
            prefabsInContainer = null;
        }

        private void SpawnIntoContainer(ItemContainer container, string prefab)
        {
            if (_allLootSpawnSlots.ContainsKey(prefab))
            {
                foreach (LootContainer.LootSpawnSlot lootSpawnSlot in _allLootSpawnSlots[prefab])
                    for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                        if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                            lootSpawnSlot.definition.SpawnIntoContainer(container);
            }
            else _allLootSpawn[prefab].SpawnIntoContainer(container);
        }

        private void AddToContainerItem(ItemContainer container, LootTableConfig lootTable)
        {
            HashSet<int> indexMove = new HashSet<int>();
            if (lootTable.UseCount)
            {
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (indexMove.Count < count)
                {
                    foreach (ItemConfig item in lootTable.Items)
                    {
                        if (indexMove.Contains(lootTable.Items.IndexOf(item))) continue;
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                        {
                            Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                            if (newItem == null)
                            {
                                PrintWarning($"Failed to create item! ({item.ShortName})");
                                continue;
                            }
                            if (item.IsBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                            if (!string.IsNullOrEmpty(item.Name)) newItem.name = item.Name;
                            if (container.capacity < container.itemList.Count + 1) container.capacity++;
                            if (!newItem.MoveToContainer(container)) newItem.Remove();
                            else
                            {
                                indexMove.Add(lootTable.Items.IndexOf(item));
                                if (indexMove.Count == count) return;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (ItemConfig item in lootTable.Items)
                {
                    if (indexMove.Contains(lootTable.Items.IndexOf(item))) continue;
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                    {
                        Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                        if (newItem == null)
                        {
                            PrintWarning($"Failed to create item! ({item.ShortName})");
                            continue;
                        }
                        if (item.IsBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                        if (!string.IsNullOrEmpty(item.Name)) newItem.name = item.Name;
                        if (container.capacity < container.itemList.Count + 1) container.capacity++;
                        if (!newItem.MoveToContainer(container)) newItem.Remove();
                        else indexMove.Add(lootTable.Items.IndexOf(item));
                    }
                }
            }
        }

        private static void CheckLootTable(LootTableConfig lootTable)
        {
            lootTable.Items = lootTable.Items.OrderByQuickSort(x => x.Chance);
            if (lootTable.Max > lootTable.Items.Count) lootTable.Max = lootTable.Items.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
        }

        private void CheckPrefabLootTable(PrefabLootTableConfig lootTable)
        {
            List<PrefabConfig> prefabs = Pool.GetList<PrefabConfig>();
            foreach (PrefabConfig prefabConfig in lootTable.Prefabs)
            {
                if (prefabs.Any(x => x.PrefabDefinition == prefabConfig.PrefabDefinition)) PrintWarning($"Duplicate prefab removed from loot table! ({prefabConfig.PrefabDefinition})");
                else
                {
                    GameObject gameObject = GameManager.server.FindPrefab(prefabConfig.PrefabDefinition);
                    global::HumanNPC humanNpc = gameObject.GetComponent<global::HumanNPC>();
                    ScarecrowNPC scarecrowNPC = gameObject.GetComponent<ScarecrowNPC>();
                    LootContainer lootContainer = gameObject.GetComponent<LootContainer>();
                    if (humanNpc != null && humanNpc.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, humanNpc.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (scarecrowNPC != null && scarecrowNPC.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, scarecrowNPC.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (lootContainer != null && lootContainer.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, lootContainer.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (lootContainer != null && lootContainer.lootDefinition != null)
                    {
                        if (!_allLootSpawn.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawn.Add(prefabConfig.PrefabDefinition, lootContainer.lootDefinition);
                        prefabs.Add(prefabConfig);
                    }
                    else PrintWarning($"Unknown prefab removed! ({prefabConfig.PrefabDefinition})");
                }
            }
            lootTable.Prefabs = prefabs.OrderByQuickSort(x => x.Chance).ToList();
            Pool.FreeList(ref prefabs);
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
        }

        private readonly Dictionary<string, LootSpawn> _allLootSpawn = new Dictionary<string, LootSpawn>();

        private readonly Dictionary<string, LootContainer.LootSpawnSlot[]> _allLootSpawnSlots = new Dictionary<string, LootContainer.LootSpawnSlot[]>();
        #endregion Spawn Loot

        #region MoveItem
        private static void MoveItem(BasePlayer player, Item item)
        {
            int spaceCountItem = GetSpaceCountItem(player, item.info.shortname, item.MaxStackable(), item.skin);
            int inventoryItemCount;
            if (spaceCountItem > item.amount) inventoryItemCount = item.amount;
            else inventoryItemCount = spaceCountItem;

            if (inventoryItemCount > 0)
            {
                Item itemInventory = ItemManager.CreateByName(item.info.shortname, inventoryItemCount, item.skin);
                if (item.skin != 0) itemInventory.name = item.name;

                item.amount -= inventoryItemCount;
                MoveInventoryItem(player, itemInventory);
            }

            if (item.amount > 0) MoveOutItem(player, item);
        }

        private static int GetSpaceCountItem(BasePlayer player, string shortname, int stack, ulong skinID)
        {
            int slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            int taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
            int result = (slots - taken) * stack;
            foreach (Item item in player.inventory.AllItems()) if (item.info.shortname == shortname && item.skin == skinID && item.amount < stack) result += stack - item.amount;
            return result;
        }

        private static void MoveInventoryItem(BasePlayer player, Item item)
        {
            if (item.amount <= item.MaxStackable())
            {
                foreach (Item itemInv in player.inventory.AllItems())
                {
                    if (itemInv.info.shortname == item.info.shortname && itemInv.skin == item.skin && itemInv.amount < itemInv.MaxStackable())
                    {
                        if (itemInv.amount + item.amount <= itemInv.MaxStackable())
                        {
                            itemInv.amount += item.amount;
                            itemInv.MarkDirty();
                            return;
                        }
                        else
                        {
                            item.amount -= itemInv.MaxStackable() - itemInv.amount;
                            itemInv.amount = itemInv.MaxStackable();
                        }
                    }
                }
                if (item.amount > 0) player.inventory.GiveItem(item);
            }
            else
            {
                while (item.amount > item.MaxStackable())
                {
                    Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                    if (item.skin != 0) thisItem.name = item.name;
                    player.inventory.GiveItem(thisItem);
                    item.amount -= item.MaxStackable();
                }
                if (item.amount > 0) player.inventory.GiveItem(item);
            }
        }

        private static void MoveOutItem(BasePlayer player, Item item)
        {
            if (item.amount <= item.MaxStackable()) item.Drop(player.transform.position, Vector3.up);
            else
            {
                while (item.amount > item.MaxStackable())
                {
                    Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                    if (item.skin != 0) thisItem.name = item.name;
                    thisItem.Drop(player.transform.position, Vector3.up);
                    item.amount -= item.MaxStackable();
                }
                if (item.amount > 0) item.Drop(player.transform.position, Vector3.up);
            }
        }
        #endregion MoveItem

        #region RemoveItem
        private int GetCountItem(BasePlayer player, string shortname, ulong skinID = 0)
        {
            int result = 0;
            foreach (Item item in player.inventory.AllItems()) if (item.info.shortname == shortname && item.skin == skinID) result += item.amount;
            return result;
        }

        private void RemoveItem(BasePlayer player, string shortname, int count, ulong skinID = 0)
        {
            foreach (Item item in player.inventory.AllItems())
            {
                if (item.info.shortname == shortname && item.skin == skinID)
                {
                    if (item.amount == count)
                    {
                        item.Remove();
                        break;
                    }
                    else if (item.amount < count)
                    {
                        count -= item.amount;
                        item.Remove();
                    }
                    else if (item.amount > count)
                    {
                        item.amount -= count;
                        item.MarkDirty();
                        break;
                    }
                }
            }
        }
        #endregion RemoveItem

        #region Helpers
        [PluginReference] private readonly Plugin NpcSpawn, BetterNpc, PveMode, Economics, ServerRewards, IQEconomic;

        private static DroppedItem SpawnDroppedItem(string shortname, Vector3 pos, Quaternion rot, bool allowPickup = false)
        {
            DroppedItem droppedItem = GameManager.server.CreateEntity("assets/prefabs/misc/burlap sack/generic_world.prefab", pos, rot) as DroppedItem;
            droppedItem.InitializeItem(ItemManager.CreateByName(shortname));
            droppedItem.enableSaving = false;
            droppedItem.Spawn();

            UnityEngine.Object.Destroy(droppedItem.GetComponent<PhysicsEffects>());
            UnityEngine.Object.Destroy(droppedItem.GetComponent<EntityCollisionMessage>());
            Rigidbody rigidbody = droppedItem.GetComponent<Rigidbody>();
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.isKinematic = true;

            droppedItem.CancelInvoke(droppedItem.IdleDestroy);

            droppedItem.allowPickup = allowPickup;

            return droppedItem;
        }

        private static BaseEntity SpawnEntity(string prefab, Vector3 pos, Quaternion rot)
        {
            BaseEntity entity = GameManager.server.CreateEntity(prefab, pos, rot);
            entity.enableSaving = false;

            GroundWatch groundWatch = entity.GetComponent<GroundWatch>();
            if (groundWatch != null) UnityEngine.Object.DestroyImmediate(groundWatch);

            DestroyOnGroundMissing destroyOnGroundMissing = entity.GetComponent<DestroyOnGroundMissing>();
            if (destroyOnGroundMissing != null) UnityEngine.Object.DestroyImmediate(destroyOnGroundMissing);

            entity.Spawn();

            if (entity is StabilityEntity) (entity as StabilityEntity).grounded = true;
            if (entity is BaseCombatEntity) (entity as BaseCombatEntity).pickup.enabled = false;

            return entity;
        }

        private static HashSet<T> GetEntities<T>(Vector3 position, float radius, int layerMask) where T : BaseEntity
        {
            HashSet<T> result = new HashSet<T>();
            foreach (Collider collider in Physics.OverlapSphere(position, radius, layerMask))
            {
                BaseEntity entity = collider.ToBaseEntity();
                if (entity.IsExists() && entity is T) result.Add(entity as T);
            }
            return result;
        }

        private bool CanBuy(BasePlayer player, PriceConfig price, int scale = 1)
        {
            if (price.Type == 0 && plugins.Exists("Economics")) return (double)Economics.Call("Balance", player.UserIDString) >= price.CountEconomy * scale;
            else if (price.Type == 1 && plugins.Exists("ServerRewards")) return (int)ServerRewards.Call("CheckPoints", player.userID) >= price.CountEconomy * scale;
            else if (price.Type == 2 && plugins.Exists("IQEconomic")) return (bool)IQEconomic.Call("API_IS_REMOVED_BALANCE", player.userID, (int)(price.CountEconomy * scale));
            else if (price.Type == 3)
            {
                foreach (ItemPriceConfig item in price.Items) if (GetCountItem(player, item.ShortName, item.SkinID) < item.Amount * scale) return false;
                return true;
            }
            else return false;
        }

        private bool CanPut(BasePlayer player, string shortname, int amount) => GetCountItem(player, shortname) >= amount;

        private void RemovePrice(BasePlayer player, PriceConfig config, int scale = 1)
        {
            if (config.Type == 0 && plugins.Exists("Economics")) Economics.Call("Withdraw", player.UserIDString, config.CountEconomy * scale);
            else if (config.Type == 1 && plugins.Exists("ServerRewards")) ServerRewards.Call("TakePoints", player.userID, (int)(config.CountEconomy * scale));
            else if (config.Type == 2 && plugins.Exists("IQEconomic")) IQEconomic.Call("API_REMOVE_BALANCE", player.userID, (int)(config.CountEconomy * scale));
            else if (config.Type == 3) foreach (ItemPriceConfig item in config.Items) RemoveItem(player, item.ShortName, item.Amount, item.SkinID);
        }

        private void OnCustomNpcGuardTargetEnd(ScientistNPC npc)
        {
            if (_controller == null || !npc.IsExists() || npc.displayName != "Juggernaut") return;
            _controller.SetJuggernautGuardTarget(npc);
        }

        private void OnBomberExplosion(ScientistNPC npc, BaseEntity target)
        {
            float radius = 2f;

            foreach (CustomBarricade barricade in _controller.CustomBarricades.ToHashSet())
            {
                float distance = Vector3.Distance(npc.transform.position, barricade.transform.position);
                if (distance < radius)
                {
                    barricade.MainHealth -= _config.Bomber.DamageBarricade;
                    barricade.UpdateHealth();
                }
                else if (distance < radius * 2f)
                {
                    barricade.MainHealth -= _config.Bomber.DamageBarricade * 0.5f;
                    barricade.UpdateHealth();
                }
                else if (distance < radius * 3f)
                {
                    barricade.MainHealth -= _config.Bomber.DamageBarricade * 0.25f;
                    barricade.UpdateHealth();
                }
            }

            foreach (BasePlayer player in _controller.Players.ToHashSet())
            {
                float distance = Vector3.Distance(npc.transform.position, player.transform.position);
                if (distance < radius) player.Hurt(_config.Bomber.DamagePlayer, DamageType.Explosion, npc, false);
                else if (distance < radius * 2f) player.Hurt(_config.Bomber.DamagePlayer * 0.5f, DamageType.Explosion, npc, false);
                else if (distance < radius * 3f) player.Hurt(_config.Bomber.DamagePlayer * 0.25f, DamageType.Explosion, npc, false);
            }

            float distanceGeneral = Vector3.Distance(npc.transform.position, _controller.General.transform.position);
            if (distanceGeneral < radius)
            {
                _controller.GeneralHealth -= _config.Bomber.DamageGeneral;
                _controller.GeneralSphere.UpdateHealth();
            }
            else if (distanceGeneral < radius * 2f)
            {
                _controller.GeneralHealth -= _config.Bomber.DamageGeneral * 0.5f;
                _controller.GeneralSphere.UpdateHealth();
            }
            else if (distanceGeneral < radius * 3f)
            {
                _controller.GeneralHealth -= _config.Bomber.DamageGeneral * 0.25f;
                _controller.GeneralSphere.UpdateHealth();
            }

            _controller.Scientists.Remove(npc);
        }

        private readonly HashSet<string> _hooks = new HashSet<string>
        {
            "OnEntityTakeDamage",
            "OnEntityKill",
            "OnPlayerDeath",
            "CanLootPlayer",
            "OnPlayerAssist",
            "OnPlayerRevive",
            "OnItemPickup",
            "OnEntityEnter",
            "OnCorpsePopulate",
            "OnActiveItemChanged",
            "OnHammerHit",
            "OnCounterModeToggle",
            "OnBradleyApcInitialize",
            "OnNpcTarget",
            "OnPlayerCommand",
            "OnServerCommand",
            "OnBotReSpawnNPCTarget",
            "CanEntityTakeDamage",
            "CanTeleport",
            "CanPopulateLoot",
            "OnCustomLootContainer",
            "OnContainerPopulate",
            "OnCustomNpcGuardTargetEnd",
            "OnBomberExplosion"
        };

        private void Unsubscribes() { foreach (string hook in _hooks) Unsubscribe(hook); }

        private void Subscribes()
        {
            foreach (string hook in _hooks)
            {
                if (hook == "CanEntityTakeDamage" && !_config.IsCreateZonePvp) continue;
                if (hook == "CanTeleport" && !_config.NTeleportationInterrupt) continue;
                Subscribe(hook);
            }
        }
        #endregion Helpers

        #region Colors
        private string Белый = "1 1 1 1";

        private string ТемныйСерый = "0.18 0.18 0.18 1";
        private string СреднийСерый = "0.24 0.24 0.24 1";
        private string СветлыйСерый = "0.38 0.38 0.38 1";

        private string ТемныйЗеленый = "0.25 0.3 0.15 1";
        private string СреднийЗеленый = "0.4 0.45 0.27 1";
        private string СветлыйЗеленый = "0.63 1 0 1";

        private string ТемныйКрасный = "0.67 0.28 0.21 1";
        private string СветлыйКрасный = "1 0.65 0.58 1";

        private string ТемныйСиний = "0.19 0.31 0.38 1";
        private string СветлыйСиний = "0.27 0.65 0.91 1";

        private string Черный = "0.08 0.08 0.08 1";
        #endregion Colors

        #region GUI
        private readonly Dictionary<string, string> _shortnames = new Dictionary<string, string>
        {
            ["Auto Turret"] = "autoturret",
            ["Flame Turret"] = "flameturret",
            ["Shotgun Trap"] = "guntrap",
            ["5.56 Rifle Ammo"] = "ammo.rifle",
            ["HV 5.56 Rifle Ammo"] = "ammo.rifle.hv",
            ["Incendiary 5.56 Rifle Ammo"] = "ammo.rifle.incendiary",
            ["Explosive 5.56 Rifle Ammo"] = "ammo.rifle.explosive",
            ["Low Grade Fuel"] = "lowgradefuel",
            ["Handmade Shell"] = "ammo.handmade.shell"
        };

        private readonly HashSet<string> _failedImages = new HashSet<string>();

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>();

        private void DownloadImage()
        {
            ImageURL image = _config.Urls.FirstOrDefault(x => !_images.ContainsKey(x.Name) && !_failedImages.Contains(x.Name));
            if (image != null)
            {
                Puts($"Downloading image {image.Name}...");
                ServerMgr.Instance.StartCoroutine(ProcessDownloadImage(image));
            }
            else
            {
                HashSet<string> images = new HashSet<string>();
                foreach (string name in new HashSet<string> { "2777422016", "2777422268", "2777422630", "2777422812", "2803087918", "ammo.handmade.shell", "ammo.rifle.explosive", "ammo.rifle.hv", "ammo.rifle.incendiary", "ammo.rifle", "autoturret", "Bookmark_KpucTaJl", "Clock_KpucTaJl", "Economic", "flameturret", "guntrap", "Lock_KpucTaJl", "lowgradefuel", "Plus_KpucTaJl", "Tab_KpucTaJl", "Tablet_KpucTaJl" }) if (!_images.ContainsKey(name) && !images.Contains(name)) images.Add(name);
                foreach (BarricadeConfig barricadeConfig in _config.CustomBarricades)
                {
                    if (barricadeConfig.Price.Type == 3)
                    {
                        foreach (ItemPriceConfig itemPriceConfig in barricadeConfig.Price.Items)
                        {
                            string name = itemPriceConfig.SkinID == 0 ? itemPriceConfig.ShortName : itemPriceConfig.SkinID.ToString();
                            if (!_images.ContainsKey(name) && !images.Contains(name)) images.Add(name);
                        }
                    }
                }
                foreach (BaseConfig baseConfig in Configs)
                {
                    foreach (TurretConfig turretConfig in baseConfig.Turrets)
                    {
                        if (turretConfig.Price.Type == 3)
                        {
                            foreach (ItemPriceConfig itemPriceConfig in turretConfig.Price.Items)
                            {
                                string name = itemPriceConfig.SkinID == 0 ? itemPriceConfig.ShortName : itemPriceConfig.SkinID.ToString();
                                if (!_images.ContainsKey(name) && !images.Contains(name)) images.Add(name);
                            }
                        }
                        foreach (AmmoConfig ammoConfig in turretConfig.Ammo)
                        {
                            if (ammoConfig.Price.Type == 3)
                            {
                                foreach (ItemPriceConfig itemPriceConfig in ammoConfig.Price.Items)
                                {
                                    string name = itemPriceConfig.SkinID == 0 ? itemPriceConfig.ShortName : itemPriceConfig.SkinID.ToString();
                                    if (!_images.ContainsKey(name) && !images.Contains(name)) images.Add(name);
                                }
                            }
                        }
                    }
                    foreach (BarricadeConfigGui barricadeConfig in baseConfig.BarricadesToBuy)
                    {
                        if (barricadeConfig.Price.Type == 3)
                        {
                            foreach (ItemPriceConfig itemPriceConfig in barricadeConfig.Price.Items)
                            {
                                string name = itemPriceConfig.SkinID == 0 ? itemPriceConfig.ShortName : itemPriceConfig.SkinID.ToString();
                                if (!_images.ContainsKey(name) && !images.Contains(name)) images.Add(name);
                            }
                        }
                    }
                    foreach (ItemComputer itemComputer in baseConfig.ItemsStart)
                    {
                        string name = itemComputer.SkinID == 0 ? itemComputer.ShortName : itemComputer.SkinID.ToString();
                        if (!_images.ContainsKey(name) && !images.Contains(name)) images.Add(name);
                    }
                    foreach (WaveConfig waveConfig in baseConfig.Waves)
                    {
                        foreach (MissionConfig missionConfig in waveConfig.Missions)
                        {
                            foreach (ItemComputer itemComputer in missionConfig.Reward.Items)
                            {
                                string name = itemComputer.SkinID == 0 ? itemComputer.ShortName : itemComputer.SkinID.ToString();
                                if (!_images.ContainsKey(name) && !images.Contains(name)) images.Add(name);
                            }
                        }
                    }
                }
                if (images.Count > 0)
                {
                    foreach (string name in images) PrintError($"Image {name} was not found. Maybe you didn't upload it to the data/Images/ folder or didn't write it in the plugin configuration");
                    Interface.Oxide.UnloadPlugin(Name);
                }
            }
        }

        IEnumerator ProcessDownloadImage(ImageURL image)
        {
            string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + image.Url;
            using (WWW www = new WWW(url))
            {
                yield return www;
                if (www.error != null)
                {
                    _failedImages.Add(image.Name);
                    PrintError($"Failed to download image {image.Name}. File address invalid! ({url})");
                }
                else
                {
                    Texture2D tex = www.texture;
                    _images.Add(image.Name, FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                    Puts($"Image {image.Name} download is complete");
                    UnityEngine.Object.DestroyImmediate(tex);
                }
                DownloadImage();
            }
        }

        private void CreateTabs(BasePlayer player, Dictionary<string, string> tabs)
        {
            CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");

            CuiElementContainer container = new CuiElementContainer();

            float border = 52.5f + 54.5f * (tabs.Count - 1);
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"{-border} -56", OffsetMax = $"{border} -36" },
                CursorEnabled = false,
            }, "Hud", "Tabs_KpucTaJl");

            int i = 0;

            foreach (var dic in tabs)
            {
                i++;
                float xmin = 109f * (i - 1);
                container.Add(new CuiElement
                {
                    Name = $"Tab_{i}_KpucTaJl",
                    Parent = "Tabs_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Tab_KpucTaJl"] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{xmin} 0", OffsetMax = $"{xmin + 105f} 20" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"Tab_{i}_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images[dic.Key] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "9 3", OffsetMax = "23 17" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"Tab_{i}_KpucTaJl",
                    Components =
                    {
                        new CuiTextComponent() { Color = "1 1 1 1", Text = dic.Value, Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "28 0", OffsetMax = "100 20" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }

        private void UpdateCaptureGUI(BasePlayer player, float percent)
        {
            CuiHelper.DestroyUi(player, "Capture_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = СветлыйСерый },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199 -278", OffsetMax = "181 -268" },
                CursorEnabled = false,
            }, "Hud", "Capture_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "Capture_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйКрасный },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"{percent / 100f} 0.9" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "Capture_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = $"{Math.Round(percent, 1)}%", Align = TextAnchor.MiddleCenter, FontSize = 8 },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        private void MessagePassword(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "MessagePassword_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-640 -86", OffsetMax = "640 -56" },
                CursorEnabled = false,
            }, "Hud", "MessagePassword_DefendableBases");
            container.Add(new CuiElement
            {
                Parent = "MessagePassword_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 1", FadeIn = 0f, Text = _controller.MessagePassword, FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        private readonly Dictionary<BasePlayer, Timer> _playerToTimer = new Dictionary<BasePlayer, Timer>();

        private void CreateNoRepair(BasePlayer player, PriceConfig price)
        {
            Timer timerGui = null;
            if (_playerToTimer.TryGetValue(player, out timerGui))
            {
                timerGui.Destroy();
                _playerToTimer.Remove(player);
            }

            CuiHelper.DestroyUi(player, "NoRepair_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            int countBlocks = price.Type == 3 ? price.Items.Count : 1;

            container.Add(new CuiPanel
            {
                Image = { Color = "0.08 0.08 0.08 0.85" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-65 -236", OffsetMax = $"65 {-236 + countBlocks * 24 + 2}" },
                CursorEnabled = false,
            }, "Hud", "NoRepair_DefendableBases");

            if (price.Type == 3)
            {
                int index = 0;
                foreach (ItemPriceConfig itemPrice in price.Items)
                {
                    int countInPlayer = GetCountItem(player, itemPrice.ShortName, itemPrice.SkinID);
                    container.Add(new CuiElement
                    {
                        Parent = "NoRepair_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = countInPlayer >= itemPrice.Amount ? ТемныйЗеленый : ТемныйКрасный },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"2 {2 + index * 24}", OffsetMax = $"128 {24 + index * 24}" }
                        }
                    });
                    if (countInPlayer < itemPrice.Amount)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "NoRepair_DefendableBases",
                            Components =
                            {
                                new CuiImageComponent { Color = СреднийСерый },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"24 {4 + index * 24}", OffsetMax = $"126 {22 + index * 24}" }
                            }
                        });
                        float xmax = countInPlayer >= itemPrice.Amount ? 126 : 24f + 102f * ((float)countInPlayer / (float)itemPrice.Amount);
                        container.Add(new CuiElement
                        {
                            Parent = "NoRepair_DefendableBases",
                            Components =
                            {
                                new CuiImageComponent { Color = СветлыйСерый },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"24 {4 + index * 24}", OffsetMax = $"{xmax} {22 + index * 24}" }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "NoRepair_DefendableBases",
                            Components =
                            {
                                new CuiImageComponent { Color = СреднийЗеленый },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"24 {4 + index * 24}", OffsetMax = $"126 {22 + index * 24}" }
                            }
                        });
                    }
                    container.Add(new CuiElement
                    {
                        Parent = "NoRepair_DefendableBases",
                        Components =
                        {
                            new CuiTextComponent() { Color = Белый, Text = $"{countInPlayer} / {itemPrice.Amount}", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"24 {4 + index * 24}", OffsetMax = $"126 {22 + index * 24}" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = "NoRepair_DefendableBases",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImageItem(itemPrice.ShortName, itemPrice.SkinID) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"4 {4 + index * 24}", OffsetMax = $"22 {22 + index * 24}" }
                        }
                    });
                    index++;
                }
            }
            else
            {
                int countInPlayer = 0;
                if (price.Type == 0 && plugins.Exists("Economics")) countInPlayer = (int)Economics.Call("Balance", player.UserIDString);
                else if (price.Type == 1 && plugins.Exists("ServerRewards")) countInPlayer = (int)ServerRewards.Call("CheckPoints", player.userID);
                else if (price.Type == 2 && plugins.Exists("IQEconomic")) countInPlayer = (int)IQEconomic.Call("API_GET_BALANCE", player.userID);
                container.Add(new CuiElement
                {
                    Parent = "NoRepair_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = countInPlayer >= price.CountEconomy ? СветлыйЗеленый : СветлыйКрасный },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "128 24" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "NoRepair_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = countInPlayer >= price.CountEconomy ? ТемныйЗеленый : ТемныйКрасный },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "24 4", OffsetMax = "126 22" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "NoRepair_DefendableBases",
                    Components =
                    {
                        new CuiTextComponent() { Color = countInPlayer >= price.CountEconomy ? СветлыйЗеленый : СветлыйКрасный, Text = $"{countInPlayer} / {price.CountEconomy}", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "24 4", OffsetMax = "126 22" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "NoRepair_DefendableBases",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Economic"] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "4 4", OffsetMax = "22 22" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);

            _playerToTimer.Add(player, timer.In(3f, () =>
            {
                CuiHelper.DestroyUi(player, "NoRepair_DefendableBases");
                _playerToTimer.Remove(player);
            }));
        }

        private void CreateLaptop(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BG_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", "BG_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "CloseGui_DefendableBases" },
                Text = { Text = "" }
            }, "BG_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "BG_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-350 -262", OffsetMax = "350 262" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйКрасный },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "319 231", OffsetMax = "350 262" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images["Tablet_KpucTaJl"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-358.5 -270.5", OffsetMax = "358.5 270.5" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "BG_Main_DefendableBases",
                Parent = "BG_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-350 -262", OffsetMax = "350 262" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = Белый, Sprite = "assets/icons/close.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "327 239", OffsetMax = "342 254" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "319 231", OffsetMax = "350 262" },
                Button = { Color = "0 0 0 0", Command = "CloseGui_DefendableBases" },
                Text = { Text = "" }
            }, "BG_DefendableBases");

            #region Titles
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = "DEFENDABLE BASES", Align = TextAnchor.MiddleLeft, FontSize = 24, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "251 -58", OffsetMax = "454 -26" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = GetMessage("GUI_PROTECTION", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "100 -85", OffsetMax = "202 -65" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = GetMessage("GUI_NOTES", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "378 -85", OffsetMax = "433 -65" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = GetMessage("GUI_INFO", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "581 -85", OffsetMax = "623 -65" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = GetMessage("GUI_INVENTORY", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "555 -195", OffsetMax = "647 -175" }
                }
            });
            #endregion Titles

            #region Lines
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -59", OffsetMax = "699 -58" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "306 -524", OffsetMax = "307 -59" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "503 -524", OffsetMax = "504 -59" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "503 -169", OffsetMax = "699 -168" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "306 -308", OffsetMax = "503 -307" }
                }
            });
            #endregion Lines

            #region Description
            container.Add(new CuiElement
            {
                Name = "Description_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "326 -255", OffsetMax = "484 -90" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "Description_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = "157 164" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "Description_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = GetMessage("Description", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 6, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95" }
                }
            });

            if (!_controller.CallForAssistance)
            {
                container.Add(new CuiElement
                {
                    Name = "CallForAssistance_DefendableBases",
                    Parent = "BG_Main_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = СветлыйСиний },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "326 -295", OffsetMax = "484 -259" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "CallForAssistance_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = ТемныйСиний },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = "157 35" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "CallForAssistance_DefendableBases",
                    Components =
                    {
                        new CuiTextComponent() { Color = Белый, Text = GetMessage("GUI_CALL_FOR_ASSISTANCE", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = "Cmd_Gui_CallForAssistance_DefendableBases" },
                    Text = { Text = "" }
                }, "CallForAssistance_DefendableBases");
            }
            #endregion Description

            #region Info
            container.Add(new CuiElement
            {
                Name = "BG_IconHealth_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "520 -110", OffsetMax = "540 -90" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_IconHealth_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images["Plus_KpucTaJl"], Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "BG_IconTime_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "520 -133", OffsetMax = "540 -113" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_IconTime_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images["Clock_KpucTaJl"], Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "BG_IconMission_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "520 -156", OffsetMax = "540 -136" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_IconMission_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images["Bookmark_KpucTaJl"], Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                }
            });
            #endregion Info

            #region Inventory
            container.Add(new CuiElement
            {
                Name = "Button_Confirm_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СреднийЗеленый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "520 -508", OffsetMax = "682 -474" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "Button_Confirm_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйЗеленый },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = "161 33" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "Button_Confirm_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = СветлыйЗеленый, Text = GetMessage("GUI_CONFIRM", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "Cmd_Gui_ConfirmInventory_DefendableBases" },
                Text = { Text = "" }
            }, "Button_Confirm_DefendableBases");
            #endregion Inventory

            #region Password
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "339 -508", OffsetMax = "469 -337" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = Черный },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "340 -507", OffsetMax = "468 -338" }
                }
            });

            if (!string.IsNullOrEmpty(_controller.KeyPassword))
            {
                container.Add(new CuiElement
                {
                    Parent = "BG_Main_DefendableBases",
                    Components =
                    {
                        new CuiTextComponent() { Color = Белый, Text = _controller.KeyPassword, Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "359 -362", OffsetMax = "449 -342" }
                    }
                });
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "353 -494", OffsetMax = "403 -470" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 0" },
                Text = { Color = Белый, Text = "0", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "353 -468", OffsetMax = "377 -444" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 1" },
                Text = { Color = Белый, Text = "1", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "379 -468", OffsetMax = "403 -444" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 2" },
                Text = { Color = Белый, Text = "2", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "405 -468", OffsetMax = "429 -444" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 3" },
                Text = { Color = Белый, Text = "3", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "353 -442", OffsetMax = "377 -418" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 4" },
                Text = { Color = Белый, Text = "4", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "379 -442", OffsetMax = "403 -418" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 5" },
                Text = { Color = Белый, Text = "5", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "405 -442", OffsetMax = "429 -418" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 6" },
                Text = { Color = Белый, Text = "6", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "353 -416", OffsetMax = "377 -392" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 7" },
                Text = { Color = Белый, Text = "7", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "379 -416", OffsetMax = "403 -392" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 8" },
                Text = { Color = Белый, Text = "8", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "405 -416", OffsetMax = "429 -392" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_AddSymbolPassword_DefendableBases 9" },
                Text = { Color = Белый, Text = "9", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "405 -494", OffsetMax = "429 -470" },
                Button = { Color = ТемныйСерый, Command = "Cmd_Gui_RemoveSymbolPassword_DefendableBases" },
                Text = { Color = Белый, Text = "←", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "431 -494", OffsetMax = "455 -418" },
                Button = { Color = ТемныйЗеленый, Command = $"Cmd_Gui_AcceptPassword_DefendableBases" },
                Text = { Color = СветлыйЗеленый, Text = "↵", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "431 -416", OffsetMax = "455 -392" },
                Button = { Color = ТемныйКрасный, Command = "Cmd_Gui_ClearPassword_DefendableBases" },
                Text = { Color = СветлыйКрасный, Text = "C", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" }
            }, "BG_Main_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "353 -390", OffsetMax = "455 -366" }
                }
            });

            if (string.IsNullOrEmpty(_controller.KeyPassword))
            {
                container.Add(new CuiElement
                {
                    Parent = "BG_Main_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = "0.08 0.08 0.08 0.85" },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "340 -507", OffsetMax = "468 -338" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "BG_Main_DefendableBases",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Lock_KpucTaJl"], Color = "1 1 1 0.25" },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "372 -454", OffsetMax = "436 -390" }
                    }
                });
            }
            #endregion Password

            CuiHelper.AddUi(player, container);

            int line = 1;

            foreach (TurretConfig turret in _controller.Config.Turrets)
            {
                if (turret.Enabled)
                {
                    UpdateTurret(player, turret, line);
                    line++;
                }
            }

            int column = 1;

            foreach (BarricadeConfigGui barricade in _controller.Config.BarricadesToBuy)
            {
                if (barricade.Enabled)
                {
                    UpdateBarricade(player, barricade, line, column);
                    column++;
                }
            }

            UpdateInfo(player);

            UpdateInventory(player);

            if (_controller.Password.Count > 0) UpdatePassword(player);
        }

        private void DestroyLaptop(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BG_DefendableBases");
            _selectItems.Clear();
            _password.Clear();
            _controller.OpenComputerPlayer = null;
        }

        private void CreateCell(CuiElementContainer container, string name, string color1, string color2, float xmin, float ymax, string image, string text, string command, bool locked)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = color1 },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xmin} {ymax - 52f}", OffsetMax = $"{xmin + 52f} {ymax}" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = name,
                Components =
                {
                    new CuiImageComponent { Color = color2 },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = "51 51" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = name,
                Components =
                {
                    new CuiRawImageComponent { Png = image },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "50 50" }
                }
            });
            if (!string.IsNullOrEmpty(text))
            {
                container.Add(new CuiElement
                {
                    Parent = name,
                    Components =
                    {
                        new CuiTextComponent() { Color = Белый, Text = text, Align = TextAnchor.LowerRight, FontSize = 9, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 2", OffsetMax = "50 52" }
                    }
                });
            }
            if (locked)
            {
                container.Add(new CuiElement
                {
                    Parent = name,
                    Components =
                    {
                        new CuiImageComponent { Color = "0.08 0.08 0.08 0.85" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = "51 51" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = name,
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Lock_KpucTaJl"], Color = "1 1 1 0.25" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "13 13", OffsetMax = "39 39" }
                    }
                });
            }
            if (!string.IsNullOrEmpty(command))
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = command },
                    Text = { Text = "" }
                }, name);
            }
        }

        private void UpdateTurret(BasePlayer player, TurretConfig config, int line)
        {
            float ymax = -(90f + (line - 1) * 55f);
            string name = config.Name.Replace(" ", string.Empty);
            bool locked = config.Name == "Auto Turret" ? _controller.AutoTurrets.Count == 0 : config.Name == "Flame Turret" ? _controller.FlameTurrets.Count == 0 : config.Name == "Shotgun Trap" ? _controller.GunTraps.Count == 0 : true;

            CuiHelper.DestroyUi(player, $"{name}_Main_DefendableBases");
            for (int i = 1; i <= config.Ammo.Count; i++) CuiHelper.DestroyUi(player, $"{name}_Ammo_{i}_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            CreateCell(container, $"{name}_Main_DefendableBases", СветлыйСерый, Черный, 20f, ymax, _images[_shortnames[config.Name]], string.Empty, locked ? $"Cmd_Gui_OpenTurret_DefendableBases {config.Name}" : string.Empty, locked);

            int pos = 1;
            foreach (AmmoConfig ammo in config.Ammo)
            {
                if (ammo.Enabled)
                {
                    string shortname = _shortnames[ammo.Name];
                    int amount = config.Name == "Auto Turret" ? _controller.GetCountAmmoAutoTurrets(shortname) : config.Name == "Flame Turret" ? _controller.GetCountAmmoFlameTurrets() : config.Name == "Shotgun Trap" ? _controller.GetCountAmmoGunTraps() : 0;
                    CreateCell(container, $"{name}_Ammo_{pos}_DefendableBases", СветлыйСерый, Черный, 20f + pos * 51f, ymax, _images[shortname], locked ? string.Empty : $"x{amount}", locked ? string.Empty : $"Cmd_Gui_OpenAmmo_DefendableBases {ammo.Name}", locked);
                    pos++;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        private void UpdateBarricade(BasePlayer player, BarricadeConfigGui config, int line, int column)
        {
            float ymax = -(90f + (line - 1) * 55f);
            float xmin = 20f + (column - 1) * 51f;
            string name = config.Name.Replace(" ", string.Empty);

            CuiHelper.DestroyUi(player, $"{name}_Main_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            CreateCell(container, $"{name}_Main_DefendableBases", СветлыйСерый, Черный, xmin, ymax, _images[config.SkinID.ToString()], string.Empty, $"Cmd_Gui_OpenBarricade_DefendableBases {config.Name}", false);

            CuiHelper.AddUi(player, container);
        }

        private void UpdateInfo(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BG_Health_DefendableBases");
            CuiHelper.DestroyUi(player, "BG_Time_DefendableBases");
            CuiHelper.DestroyUi(player, "BG_Mission_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "BG_Health_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "543 -110", OffsetMax = "682 -90" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Health_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйКрасный },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = $"{_controller.GeneralHealth / _controller.Config.GeneralHealth * 137f + 1f} 19" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Health_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = $"{(int)_controller.GeneralHealth} HP", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "BG_Time_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "543 -133", OffsetMax = "682 -113" }
                }
            });
            float progress = _controller.MaxSeconds == 0 ? 138f : (float)_controller.Seconds / (float)_controller.MaxSeconds * 137f + 1f;
            container.Add(new CuiElement
            {
                Parent = "BG_Time_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0.22 0.75 0.21 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = $"{progress} 19" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Time_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = _controller.GetTimeFormat(), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "BG_Mission_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "543 -156", OffsetMax = "682 -136" }
                }
            });
            progress = _controller.AllStageMission == 0 ? 138f : (float)_controller.CompleteStageMission / (float)_controller.AllStageMission * 137f + 1f;
            container.Add(new CuiElement
            {
                Parent = "BG_Mission_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСиний },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = $"{progress} 19" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_Mission_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = $"{_controller.CompleteStageMission} / {_controller.AllStageMission}", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        private HashSet<int> _selectItems = new HashSet<int>();

        private void UpdateInventory(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BG_Inventory_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "BG_Inventory_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "520 -472", OffsetMax = "682 -200" }
                }
            });

            int countItems = _controller.Inventory.Count;

            for (int line = 1; line <= 5; line++)
            {
                for (int column = 1; column <= 3; column++)
                {
                    int pos = (line - 1) * 3 + column;
                    bool selectItem = _selectItems.Contains(pos);
                    float xmin = (column - 1) * 55f;
                    float ymax = -(line - 1) * 55f;
                    if (pos <= countItems)
                    {
                        ControllerDefendableBase.ItemInventory item = _controller.Inventory[pos - 1];
                        container.Add(new CuiElement
                        {
                            Name = $"Inventory_Cell_{pos}_DefendableBases",
                            Parent = "BG_Inventory_DefendableBases",
                            Components =
                            {
                                new CuiImageComponent { Color = selectItem ? СреднийЗеленый : СветлыйСерый },
                                new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xmin} {ymax - 52}", OffsetMax = $"{xmin + 52} {ymax}" }
                            }
                        });
                        container.Add(new CuiElement
                        {
                            Parent = $"Inventory_Cell_{pos}_DefendableBases",
                            Components =
                            {
                                new CuiImageComponent { Color = selectItem ? ТемныйЗеленый : СреднийСерый },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = "51 51" }
                            }
                        });
                        container.Add(new CuiElement
                        {
                            Parent = $"Inventory_Cell_{pos}_DefendableBases",
                            Components =
                            {
                                new CuiRawImageComponent { Png = GetImageItem(item.shortname, item.skinId) },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "50 50" }
                            }
                        });
                        container.Add(new CuiElement
                        {
                            Parent = $"Inventory_Cell_{pos}_DefendableBases",
                            Components =
                            {
                                new CuiTextComponent() { Color = Белый, Text = $"x{item.amount}", Align = TextAnchor.LowerRight, FontSize = 9, Font = "robotocondensed-bold.ttf" },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 2", OffsetMax = "50 52" }
                            }
                        });
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                            Button = { Color = "0 0 0 0", Command = $"Cmd_Gui_Inventory_DefendableBases {pos}" },
                            Text = { Text = "" }
                        }, $"Inventory_Cell_{pos}_DefendableBases");
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Name = $"Inventory_Cell_{pos}_DefendableBases",
                            Parent = "BG_Inventory_DefendableBases",
                            Components =
                            {
                                new CuiImageComponent { Color = СветлыйСерый },
                                new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xmin} {ymax - 52}", OffsetMax = $"{xmin + 52} {ymax}" }
                            }
                        });
                        container.Add(new CuiElement
                        {
                            Parent = $"Inventory_Cell_{pos}_DefendableBases",
                            Components =
                            {
                                new CuiImageComponent { Color = СреднийСерый },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "1 1", OffsetMax = "51 51" }
                            }
                        });
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        private List<int> _password = new List<int>();

        private void UpdatePassword(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Password_DefendableBases");

            string password = "";
            if (_password.Count > 0)
            {
                for (int i = 1; i <= 6; i++)
                {
                    if (i > _password.Count) password += " ∗";
                    else password += i == 1 ? $"{_password[0]}" : $" {_password[i - 1]}";
                }
            }
            else password = "∗ ∗ ∗ ∗ ∗ ∗";

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "Password_DefendableBases",
                Parent = "BG_Main_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = password, Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "353 -390", OffsetMax = "455 -366" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        private void CreateFirstPanel(BasePlayer player, PriceConfig price, string image, string name, bool isTurret)
        {
            CuiHelper.DestroyUi(player, "BG_FirstPanel_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "BG_FirstPanel_DefendableBases",
                Parent = "BG_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "CloseGui_FirstPanel_DefendableBases" },
                Text = { Text = "" }
            }, "BG_FirstPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "BG_FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0.14 0.14 0.14 0.9", Material = "assets/content/ui/uibackgroundblur.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-350 -262", OffsetMax = "350 262" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_FirstPanel_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images["Tablet_KpucTaJl"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-358.5 -270.5", OffsetMax = "358.5 270.5" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "FirstPanel_DefendableBases",
                Parent = "BG_FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-350 -262", OffsetMax = "350 262" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "245 -344", OffsetMax = "455 -175" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "247 -342", OffsetMax = "453 -177" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = isTurret ? GetMessage("GUI_UPGRADE", player.UserIDString) : GetMessage("GUI_BUY", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "310 -197", OffsetMax = "388 -177" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "247 -199", OffsetMax = "453 -198" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйКрасный },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "432 -198", OffsetMax = "453 -177" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = Белый, Sprite = "assets/icons/close.png" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "437 -193", OffsetMax = "448 -182" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "432 -198", OffsetMax = "453 -177" },
                Button = { Color = "0 0 0 0", Command = "CloseGui_FirstPanel_DefendableBases" },
                Text = { Text = "" }
            }, "FirstPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "258 -261", OffsetMax = "316 -203" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "260 -259", OffsetMax = "314 -205" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = image },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "261 -258", OffsetMax = "313 -206" }
                }
            });

            bool canBuy = CanBuy(player, price);
            container.Add(new CuiElement
            {
                Name = "Button_FirstPanel_DefendableBases",
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = canBuy ? СреднийЗеленый : СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "318 -261", OffsetMax = "442 -203" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "Button_FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = canBuy ? ТемныйЗеленый : СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "122 56" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "Button_FirstPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = canBuy ? СветлыйЗеленый : Белый, Text = isTurret ? GetMessage("GUI_UPGRADE", player.UserIDString) : GetMessage("GUI_BUY", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            if (canBuy)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = isTurret ? $"Cmd_Gui_UpgradeTurret_DefendableBases {name}" : $"Cmd_Gui_BuyBarricade_DefendableBases {name}" },
                    Text = { Text = "" }
                }, "Button_FirstPanel_DefendableBases");
            }

            container.Add(new CuiElement
            {
                Parent = "FirstPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = Белый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "286 -273", OffsetMax = "288 -261" }
                }
            });

            if (price.Type == 3)
            {
                int countItems = price.Items.Count;
                if (countItems > 1)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "FirstPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = Белый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "288 -268", OffsetMax = "414 -266" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = "FirstPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = Белый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "412 -273", OffsetMax = "414 -268" }
                        }
                    });
                }
                if (countItems > 2)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "FirstPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = Белый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "349 -273", OffsetMax = "351 -268" }
                        }
                    });
                }
                for (int i = 0; i < countItems; i++)
                {
                    ItemPriceConfig item = price.Items[i];
                    float xmin = i == 0 ? 258 : i == 1 ? 384 : 321;
                    container.Add(new CuiElement
                    {
                        Name = $"BG_FirstPanelPrice{i}_DefendableBases",
                        Parent = "FirstPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = СветлыйСерый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xmin} -331", OffsetMax = $"{xmin + 58} -273" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = $"BG_FirstPanelPrice{i}_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = СреднийСерый },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "56 56" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = $"BG_FirstPanelPrice{i}_DefendableBases",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImageItem(item.ShortName, item.SkinID) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "3 3", OffsetMax = "55 55" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = $"BG_FirstPanelPrice{i}_DefendableBases",
                        Components =
                        {
                            new CuiTextComponent() { Color = Белый, Text = $"x{item.Amount}", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 3", OffsetMax = "55 58" }
                        }
                    });
                }
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "BG_FirstPanelPrice0_DefendableBases",
                    Parent = "FirstPanel_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = СветлыйСерый },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "258 -331", OffsetMax = "316 -273" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "BG_FirstPanelPrice0_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = СреднийСерый },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "56 56" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "BG_FirstPanelPrice0_DefendableBases",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Economic"] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "3 3", OffsetMax = "55 55" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "BG_FirstPanelPrice0_DefendableBases",
                    Components =
                    {
                        new CuiTextComponent() { Color = Белый, Text = $"x{price.CountEconomy}", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 3", OffsetMax = "55 58" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }

        private void CreateSecondPanel(BasePlayer player, AmmoConfig config, int scaleBuy, int scalePut)
        {
            string shortname = _shortnames[config.Name];

            int countAmmoInTurret = config.Name == "Low Grade Fuel" ? _controller.GetCountAmmoFlameTurrets() : config.Name == "Handmade Shell" ? _controller.GetCountAmmoGunTraps() : _controller.GetCountAmmoAutoTurrets(shortname);

            CuiHelper.DestroyUi(player, "BG_SecondPanel_DefendableBases");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "BG_SecondPanel_DefendableBases",
                Parent = "BG_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "CloseGui_SecondPanel_DefendableBases" },
                Text = { Text = "" }
            }, "BG_SecondPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "BG_SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0.14 0.14 0.14 0.9", Material = "assets/content/ui/uibackgroundblur.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-350 -262", OffsetMax = "350 262" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "BG_SecondPanel_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images["Tablet_KpucTaJl"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-358.5 -270.5", OffsetMax = "358.5 270.5" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "SecondPanel_DefendableBases",
                Parent = "BG_SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-350 -262", OffsetMax = "350 262" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "148 -354", OffsetMax = "552 -185" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "150 -352", OffsetMax = "550 -187" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = GetMessage("GUI_AMMUNITION", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "297 -207", OffsetMax = "404 -187" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "350 -352", OffsetMax = "351 -209" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "150 -209", OffsetMax = "550 -208" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйКрасный },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "529 -208", OffsetMax = "550 -187" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = Белый, Sprite = "assets/icons/close.png" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "534 -203", OffsetMax = "545 -192" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "529 -208", OffsetMax = "550 -187" },
                Button = { Color = "0 0 0 0", Command = "CloseGui_SecondPanel_DefendableBases" },
                Text = { Text = "" }
            }, "SecondPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "161 -271", OffsetMax = "219 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "163 -269", OffsetMax = "217 -215" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images[shortname] },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "164 -268", OffsetMax = "216 -216" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = $"x{config.Count * scaleBuy}", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "164 -268", OffsetMax = "216 -216" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйКрасный },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "221 -237", OffsetMax = "245 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = СветлыйКрасный, Text = "-", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "221 -237", OffsetMax = "245 -213" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "221 -237", OffsetMax = "245 -213" },
                Button = { Color = "0 0 0 0", Command = $"Cmd_Gui_UpdateScaleAmmo_DefendableBases {scaleBuy - 1} {scalePut} {config.Name}" },
                Text = { Text = "" }
            }, "SecondPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйЗеленый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "321 -237", OffsetMax = "345 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = СветлыйЗеленый, Text = "+", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "321 -237", OffsetMax = "345 -213" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "321 -237", OffsetMax = "345 -213" },
                Button = { Color = "0 0 0 0", Command = $"Cmd_Gui_UpdateScaleAmmo_DefendableBases {scaleBuy + 1} {scalePut} {config.Name}" },
                Text = { Text = "" }
            }, "SecondPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "247 -237", OffsetMax = "319 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "249 -235", OffsetMax = "317 -215" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = $"{config.Count * scaleBuy}", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "247 -237", OffsetMax = "319 -213" }
                }
            });

            bool canBuy = CanBuy(player, config.Price, scaleBuy);
            bool canCountBuy = countAmmoInTurret + config.Count * scaleBuy <= config.MaxCount;
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = canBuy && canCountBuy ? СреднийЗеленый : СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "221 -271", OffsetMax = "345 -239" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = canBuy && canCountBuy ? ТемныйЗеленый : СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "223 -269", OffsetMax = "343 -241" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = canBuy && canCountBuy ? СветлыйЗеленый : Белый, Text = !canBuy ? GetMessage("GUI_NO_ITEMS", player.UserIDString) : !canCountBuy ? GetMessage("GUI_MAXIMUM", player.UserIDString) : GetMessage("GUI_BUY", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "221 -271", OffsetMax = "345 -239" }
                }
            });
            if (canBuy && canCountBuy)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "221 -271", OffsetMax = "345 -239" },
                    Button = { Color = "0 0 0 0", Command = $"Cmd_Gui_BuyAmmo_DefendableBases {scaleBuy} {config.Name}" },
                    Text = { Text = "" }
                }, "SecondPanel_DefendableBases");
            }

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = Белый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "189 -283", OffsetMax = "191 -271" }
                }
            });

            if (config.Price.Type == 3)
            {
                int countItems = config.Price.Items.Count;
                if (countItems > 1)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "SecondPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = Белый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "191 -278", OffsetMax = "317 -276" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = "SecondPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = Белый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "315 -283", OffsetMax = "317 -278" }
                        }
                    });
                }
                if (countItems > 2)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "SecondPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = Белый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "252 -283", OffsetMax = "254 -278" }
                        }
                    });
                }
                for (int i = 0; i < countItems; i++)
                {
                    ItemPriceConfig item = config.Price.Items[i];
                    float xmin = i == 0 ? 161 : i == 1 ? 287 : 224;
                    container.Add(new CuiElement
                    {
                        Name = $"BG_SecondPanelPrice{i}_DefendableBases",
                        Parent = "SecondPanel_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = СветлыйСерый },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xmin} -341", OffsetMax = $"{xmin + 58} -283" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = $"BG_SecondPanelPrice{i}_DefendableBases",
                        Components =
                        {
                            new CuiImageComponent { Color = СреднийСерый },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "56 56" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = $"BG_SecondPanelPrice{i}_DefendableBases",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImageItem(item.ShortName, item.SkinID) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "3 3", OffsetMax = "55 55" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = $"BG_SecondPanelPrice{i}_DefendableBases",
                        Components =
                        {
                            new CuiTextComponent() { Color = Белый, Text = $"x{item.Amount * scaleBuy}", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 3", OffsetMax = "55 58" }
                        }
                    });
                }
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "BG_SecondPanelPrice0_DefendableBases",
                    Parent = "SecondPanel_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = СветлыйСерый },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "161 -341", OffsetMax = "219 -283" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "BG_SecondPanelPrice0_DefendableBases",
                    Components =
                    {
                        new CuiImageComponent { Color = СреднийСерый },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "56 56" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "BG_SecondPanelPrice0_DefendableBases",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Economic"] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "3 3", OffsetMax = "55 55" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "BG_SecondPanelPrice0_DefendableBases",
                    Components =
                    {
                        new CuiTextComponent() { Color = Белый, Text = $"x{config.Price.CountEconomy * scaleBuy}", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 3", OffsetMax = "55 58" }
                    }
                });
            }

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "355 -271", OffsetMax = "413 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "357 -269", OffsetMax = "411 -215" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiRawImageComponent { Png = _images[shortname] },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "358 -268", OffsetMax = "410 -216" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = $"x{config.Count * scalePut}", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "358 -267", OffsetMax = "409 -216" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйКрасный },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "415 -237", OffsetMax = "439 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = СветлыйКрасный, Text = "-", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "415 -237", OffsetMax = "439 -213" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "415 -237", OffsetMax = "439 -213" },
                Button = { Color = "0 0 0 0", Command = $"Cmd_Gui_UpdateScaleAmmo_DefendableBases {scaleBuy} {scalePut - 1} {config.Name}" },
                Text = { Text = "" }
            }, "SecondPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = ТемныйЗеленый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "515 -237", OffsetMax = "539 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = СветлыйЗеленый, Text = "+", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "515 -237", OffsetMax = "539 -213" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "515 -237", OffsetMax = "539 -213" },
                Button = { Color = "0 0 0 0", Command = $"Cmd_Gui_UpdateScaleAmmo_DefendableBases {scaleBuy} {scalePut + 1} {config.Name}" },
                Text = { Text = "" }
            }, "SecondPanel_DefendableBases");

            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "441 -237", OffsetMax = "513 -213" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "443 -235", OffsetMax = "511 -215" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = Белый, Text = $"{config.Count * scalePut}", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "441 -237", OffsetMax = "513 -213" }
                }
            });

            bool canPut = CanPut(player, shortname, config.Count * scalePut);
            bool canCountPut = countAmmoInTurret + config.Count * scalePut <= config.MaxCount;
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = canPut && canCountPut ? СреднийЗеленый : СветлыйСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "415 -271", OffsetMax = "539 -239" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiImageComponent { Color = canPut && canCountPut ? ТемныйЗеленый : СреднийСерый },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "417 -269", OffsetMax = "537 -241" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = "SecondPanel_DefendableBases",
                Components =
                {
                    new CuiTextComponent() { Color = canPut && canCountPut ? СветлыйЗеленый : Белый, Text = !canPut ? GetMessage("GUI_NO_ITEMS", player.UserIDString) : !canCountPut ? GetMessage("GUI_MAXIMUM", player.UserIDString) : GetMessage("GUI_PUT", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "415 -271", OffsetMax = "539 -239" }
                }
            });
            if (canPut && canCountPut)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "415 -271", OffsetMax = "539 -239" },
                    Button = { Color = "0 0 0 0", Command = $"Cmd_Gui_PutAmmo_DefendableBases {scalePut} {config.Name}" },
                    Text = { Text = "" }
                }, "SecondPanel_DefendableBases");
            }

            CuiHelper.AddUi(player, container);
        }

        private string GetImageItem(string shortname, ulong skinID)
        {
            if (skinID == 0) return _images[shortname];
            else return _images[skinID.ToString()];
        }
        #endregion GUI

        #region Commands GUI
        [ConsoleCommand("CloseGui_DefendableBases")]
        private void Cmd_CloseGui_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            DestroyLaptop(player);
        }

        [ConsoleCommand("CloseGui_FirstPanel_DefendableBases")]
        private void CloseGui_FirstPanel_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "BG_FirstPanel_DefendableBases");
        }

        [ConsoleCommand("CloseGui_SecondPanel_DefendableBases")]
        private void CloseGui_SecondPanel_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "BG_SecondPanel_DefendableBases");
        }

        [ConsoleCommand("Cmd_Gui_CallForAssistance_DefendableBases")]
        private void Cmd_Gui_CallForAssistance_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            _controller.CallForAssistance = true;

            DestroyLaptop(player);

            AlertToAllPlayers("CallForAssistance", _config.Prefix, player.displayName, PhoneController.PositionToGridCoord(_controller.transform.position));
            AlertToPlayer(player, GetMessage("CallForAssistancePlayer", player.UserIDString, _config.Prefix));
        }

        [ConsoleCommand("Cmd_Gui_Inventory_DefendableBases")]
        private void Cmd_Gui_Inventory_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            int pos = Convert.ToInt32(arg.Args[0]);

            if (_selectItems.Contains(pos)) _selectItems.Remove(pos);
            else _selectItems.Add(pos);

            UpdateInventory(player);
        }

        [ConsoleCommand("Cmd_Gui_ConfirmInventory_DefendableBases")]
        private void Cmd_Gui_ConfirmInventory_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            if (_selectItems.Count == 0) return;

            HashSet<ControllerDefendableBase.ItemInventory> items = new HashSet<ControllerDefendableBase.ItemInventory>();
            foreach (int pos in _selectItems) items.Add(_controller.Inventory[pos - 1]);
            _selectItems.Clear();
            foreach (ControllerDefendableBase.ItemInventory itemInventory in items)
            {
                Item item = ItemManager.CreateByName(itemInventory.shortname, itemInventory.amount, itemInventory.skinId);
                if (!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                _controller.Inventory.Remove(itemInventory);
                MoveItem(player, item);
            }

            DestroyLaptop(player);
        }

        [ConsoleCommand("Cmd_Gui_OpenTurret_DefendableBases")]
        private void Cmd_Gui_OpenTurret_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            string name = GetNameArgs(arg.Args, 0);

            TurretConfig config = _controller.Config.Turrets.FirstOrDefault(x => x.Name == name);

            CreateFirstPanel(player, config.Price, _images[_shortnames[config.Name]], config.Name, true);
        }

        [ConsoleCommand("Cmd_Gui_OpenBarricade_DefendableBases")]
        private void Cmd_Gui_OpenBarricade_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            string name = GetNameArgs(arg.Args, 0);

            BarricadeConfigGui config = _controller.Config.BarricadesToBuy.FirstOrDefault(x => x.Name == name);

            CreateFirstPanel(player, config.Price, _images[config.SkinID.ToString()], config.Name, false);
        }

        [ConsoleCommand("Cmd_Gui_UpgradeTurret_DefendableBases")]
        private void Cmd_Gui_UpgradeTurret_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            string name = GetNameArgs(arg.Args, 0);

            TurretConfig config = _controller.Config.Turrets.FirstOrDefault(x => x.Name == name);

            RemovePrice(player, config.Price);

            _controller.OpenTurrets(config);

            DestroyLaptop(player);

            AlertToAllPlayers("UpgradeBase", _config.Prefix, player.displayName, PhoneController.PositionToGridCoord(_controller.transform.position), config.Name);
        }

        [ConsoleCommand("Cmd_Gui_BuyBarricade_DefendableBases")]
        private void Cmd_Gui_BuyBarricade_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            string name = GetNameArgs(arg.Args, 0);

            BarricadeConfigGui config = _controller.Config.BarricadesToBuy.FirstOrDefault(x => x.Name == name);

            RemovePrice(player, config.Price);

            _controller.AddInventory(config.ShortName, 1, config.SkinID, config.Name);

            CuiHelper.DestroyUi(player, "BG_FirstPanel_DefendableBases");
            UpdateInventory(player);
        }

        [ConsoleCommand("Cmd_Gui_OpenAmmo_DefendableBases")]
        private void Cmd_Gui_OpenAmmo_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            string name = GetNameArgs(arg.Args, 0);

            AmmoConfig config = _controller.Config.Turrets.FirstOrDefault(x => x.Ammo.Any(y => y.Name == name)).Ammo.FirstOrDefault(x => x.Name == name);

            CreateSecondPanel(player, config, 1, 1);
        }

        [ConsoleCommand("Cmd_Gui_UpdateScaleAmmo_DefendableBases")]
        private void Cmd_Gui_UpdateScaleAmmo_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 3) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            int scaleBuy = Convert.ToInt32(arg.Args[0]);
            int scalePut = Convert.ToInt32(arg.Args[1]);
            string name = GetNameArgs(arg.Args, 2);

            if (scaleBuy < 1 || scalePut < 1) return;

            AmmoConfig config = _controller.Config.Turrets.FirstOrDefault(x => x.Ammo.Any(y => y.Name == name)).Ammo.FirstOrDefault(x => x.Name == name);

            CreateSecondPanel(player, config, scaleBuy, scalePut);
        }

        [ConsoleCommand("Cmd_Gui_BuyAmmo_DefendableBases")]
        private void Cmd_Gui_BuyAmmo_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            int scale = Convert.ToInt32(arg.Args[0]);
            string name = GetNameArgs(arg.Args, 1);

            AmmoConfig config = _controller.Config.Turrets.FirstOrDefault(x => x.Ammo.Any(y => y.Name == name)).Ammo.FirstOrDefault(x => x.Name == name);

            RemovePrice(player, config.Price, scale);

            if (config.Name == "Low Grade Fuel") _controller.UpdateAmmoFlameTurret(config.Count * scale);
            else if (config.Name == "Handmade Shell") _controller.UpdateAmmoGunTrap(config.Count * scale);
            else _controller.UpdateAmmoAutoTurret(_shortnames[config.Name], config.Count * scale);

            DestroyLaptop(player);
        }

        [ConsoleCommand("Cmd_Gui_PutAmmo_DefendableBases")]
        private void Cmd_Gui_PutAmmo_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            int scale = Convert.ToInt32(arg.Args[0]);
            string name = GetNameArgs(arg.Args, 1);

            AmmoConfig config = _controller.Config.Turrets.FirstOrDefault(x => x.Ammo.Any(y => y.Name == name)).Ammo.FirstOrDefault(x => x.Name == name);
            string shortname = _shortnames[config.Name];

            RemoveItem(player, shortname, config.Count * scale);

            if (config.Name == "Low Grade Fuel") _controller.UpdateAmmoFlameTurret(config.Count * scale);
            else if (config.Name == "Handmade Shell") _controller.UpdateAmmoGunTrap(config.Count * scale);
            else _controller.UpdateAmmoAutoTurret(shortname, config.Count * scale);

            DestroyLaptop(player);
        }

        [ConsoleCommand("Cmd_Gui_AddSymbolPassword_DefendableBases")]
        private void Cmd_Gui_AddSymbolPassword_DefendableBases(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1) return;

            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            if (_password.Count == 6) return;

            int symbol = Convert.ToInt32(arg.Args[0]);

            _password.Add(symbol);

            UpdatePassword(player);
        }

        [ConsoleCommand("Cmd_Gui_RemoveSymbolPassword_DefendableBases")]
        private void Cmd_Gui_RemoveSymbolPassword_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            if (_password.Count == 0) return;

            int symbol = _password.Last();
            _password.Remove(symbol);

            UpdatePassword(player);
        }

        [ConsoleCommand("Cmd_Gui_ClearPassword_DefendableBases")]
        private void Cmd_Gui_ClearPassword_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            _password.Clear();

            UpdatePassword(player);
        }

        [ConsoleCommand("Cmd_Gui_AcceptPassword_DefendableBases")]
        private void Cmd_Gui_AcceptPassword_DefendableBases(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            if (_controller.CorrectPassword())
            {
                _controller.SuccessfulPassword();
                if (_controller.CompleteStageMission == _controller.AllStageMission) AlertToAllPlayers("AllPassword", _config.Prefix, player.displayName);
                else AlertToPlayer(player, GetMessage("TruePassword", player.UserIDString, _config.Prefix, $"{_controller.CompleteStageMission} / {_controller.AllStageMission}"));
            }
            else AlertToPlayer(player, GetMessage("FalsePassword", player.UserIDString, _config.Prefix));

            DestroyLaptop(player);
        }

        private string GetNameArgs(string[] args, int first)
        {
            string result = "";
            for (int i = first; i < args.Length; i++) result += i == first ? args[i] : $" {args[i]}";
            return result;
        }
        #endregion Commands GUI

        #region BotReSpawn
        private object OnBotReSpawnNPCTarget(ScientistNPC npc, BasePlayer player)
        {
            if (_controller == null || _controller.General == null) return null;
            if (player == _controller.General) return true;
            return null;
        }
        #endregion BotReSpawn

        #region TruePVE
        private object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!_config.IsCreateZonePvp || victim == null || hitinfo == null || _controller == null) return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (_controller.Players.Contains(victim) && (attacker == null || _controller.Players.Contains(attacker))) return true;
            else return null;
        }
        #endregion TruePVE

        #region NTeleportation
        private object CanTeleport(BasePlayer player, Vector3 to)
        {
            if (_config.NTeleportationInterrupt && _controller != null && (_controller.Players.Contains(player) || Vector3.Distance(_controller.transform.position, to) < _controller.Config.Radius)) return GetMessage("NTeleportation", player.UserIDString, _config.Prefix);
            else return null;
        }
        #endregion NTeleportation

        #region Alerts
        [PluginReference] private readonly Plugin GUIAnnouncements, DiscordMessages;

        private string ClearColorAndSize(string message)
        {
            message = message.Replace("</color>", string.Empty);
            message = message.Replace("</size>", string.Empty);
            while (message.Contains("<color="))
            {
                int index = message.IndexOf("<color=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            while (message.Contains("<size="))
            {
                int index = message.IndexOf("<size=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            if (!string.IsNullOrEmpty(_config.Prefix)) message = message.Replace(_config.Prefix + " ", string.Empty);
            return message;
        }

        private bool CanSendDiscordMessage() => _config.Discord.IsDiscord && !string.IsNullOrEmpty(_config.Discord.WebhookUrl) && _config.Discord.WebhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        private void AlertToAllPlayers(string langKey, params object[] args)
        {
            if (CanSendDiscordMessage() && _config.Discord.Keys.Contains(langKey))
            {
                object fields = new[] { new { name = Title, value = ClearColorAndSize(GetMessage(langKey, null, args)), inline = false } };
                DiscordMessages?.Call("API_SendFancyMessage", _config.Discord.WebhookUrl, "", _config.Discord.EmbedColor, JsonConvert.SerializeObject(fields), null, this);
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList) AlertToPlayer(player, GetMessage(langKey, player.UserIDString, args));
        }

        private void AlertToPlayer(BasePlayer player, string message)
        {
            if (_config.IsChat) PrintToChat(player, message);
            if (_config.GuiAnnouncements.IsGuiAnnouncements) GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(message), _config.GuiAnnouncements.BannerColor, _config.GuiAnnouncements.TextColor, player, _config.GuiAnnouncements.ApiAdjustVPosition);
            if (_config.Notify.IsNotify) player.SendConsoleCommand($"notify.show {_config.Notify.Type} {ClearColorAndSize(message)}");
        }
        #endregion Alerts

        #region Commands
        [ChatCommand("warstart")]
        private void ChatStartEvent(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            if (_active)
            {
                PrintToChat(player, GetMessage("EventActive", player.UserIDString, _config.Prefix));
                return;
            }

            if (args != null && args.Length > 0)
            {
                BaseName = GetNameArgs(args, 0);
                if (!Configs.Any(x => x.Name == BaseName)) BaseName = string.Empty;
            }

            Start();
        }

        [ChatCommand("warstop")]
        private void ChatStopEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (_controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }

        [ConsoleCommand("warstart")]
        private void ConsoleStartEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;

            if (_active)
            {
                Puts("This event is active now. To finish this event (warstop), then to start the next one");
                return;
            }

            if (arg.Args != null && arg.Args.Length > 0)
            {
                BaseName = GetNameArgs(arg.Args, 0);
                if (!Configs.Any(x => x.Name == BaseName)) BaseName = string.Empty;
            }

            Start();
        }

        [ConsoleCommand("warstop")]
        private void ConsoleStopEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (_controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }

        [ConsoleCommand("givebarricade")]
        private void ConsoleGiveBarricade(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;

            if (arg.Args == null || arg.Args.Length < 3) return;

            ulong id = Convert.ToUInt64(arg.Args[0]);
            int level = Convert.ToInt32(arg.Args[1]);
            int amount = Convert.ToInt32(arg.Args[2]);

            if (amount < 1 || level < 0 || level > 4) return;

            BasePlayer target = BasePlayer.FindByID(id);

            if (target == null)
            {
                Puts($"Player with SteamID {id} not found!");
                return;
            }

            MoveItem(target, GiveBarricade(level, amount));

            Puts($"Player {target.displayName} has successfully received {amount} x Barricade Tier {level}");
        }
        #endregion Commands
    }
}

namespace Oxide.Plugins.DefendableBasesExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }

        public static List<TSource> WhereToList<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            List<TSource> result = new List<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        public static List<TResult> Select<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> predicate)
        {
            List<TResult> result = new List<TResult>();
            for (int i = 0; i < source.Count; i++)
            {
                TSource element = source[i];
                result.Add(predicate(element));
            }
            return result;
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static TSource First<TSource>(this IList<TSource> source) => source[0];

        public static TSource Last<TSource>(this IList<TSource> source) => source[source.Count - 1];

        public static TSource Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue < resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static HashSet<T> OfType<T>(this IEnumerable<BaseNetworkable> source)
        {
            HashSet<T> result = new HashSet<T>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (enumerator.Current is T) result.Add((T)(object)enumerator.Current);
            return result;
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> result = new List<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        private static void Replace<TSource>(this IList<TSource> source, int x, int y)
        {
            TSource t = source[x];
            source[x] = source[y];
            source[y] = t;
        }

        private static List<TSource> QuickSort<TSource>(this List<TSource> source, Func<TSource, float> predicate, int minIndex, int maxIndex)
        {
            if (minIndex >= maxIndex) return source;

            int pivotIndex = minIndex - 1;
            for (int i = minIndex; i < maxIndex; i++)
            {
                if (predicate(source[i]) < predicate(source[maxIndex]))
                {
                    pivotIndex++;
                    source.Replace(pivotIndex, i);
                }
            }
            pivotIndex++;
            source.Replace(pivotIndex, maxIndex);

            QuickSort(source, predicate, minIndex, pivotIndex - 1);
            QuickSort(source, predicate, pivotIndex + 1, maxIndex);

            return source;
        }

        public static List<TSource> OrderByQuickSort<TSource>(this List<TSource> source, Func<TSource, float> predicate) => source.QuickSort(predicate, 0, source.Count - 1);

        public static TSource Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue > resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            int movements = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (movements == index) return enumerator.Current;
                    movements++;
                }
            }
            return default(TSource);
        }

        public static bool IsPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;

        public static void ClearItemsContainer(this ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }
    }
}