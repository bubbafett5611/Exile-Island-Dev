using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("RedeemStorageAPI", "ThePitereq", "1.0.1")]
    public class RedeemStorageAPI : RustPlugin
    {
        [PluginReference] private readonly Plugin PopUpAPI;
        private static readonly Dictionary<BasePlayer, BoxStorage> openedInventories = new Dictionary<BasePlayer, BoxStorage>();

        private void OnServerInitialized()
        {
            permission.RegisterPermission("redeemstorageapi.admin", this);
            config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(config);
            LoadMessages();
            LoadData();
            foreach (var command in config.commands)
                cmd.AddChatCommand(command, this, nameof(RedeemCommand));
            if (config.itemReminder > 0)
                timer.Every(config.itemReminder, () =>
                {
                    foreach (var player in BasePlayer.activePlayerList)
                        foreach (var storage in storedItems)
                            if (storage.Value.ContainsKey(player.userID) && storage.Value[player.userID].Any())
                                PopUpAPI?.Call("ShowPopUp", player, config.popUpPreset, Lang("UnredeemedItemsRemind", player.UserIDString, config.commands.First(), storage.Key));
                });
        }

        private void Unload()
        {
            SaveData();
            foreach (var inventory in openedInventories)
                inventory.Value?.Kill();
        }

        private void RedeemCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                foreach (var storage in config.inventories)
                    if (storage.Value.defaultInventory)
                    {
                        OpenRedeemInventory(player, storage.Key);
                        return;
                    }
            }
            else if (args.Length == 1)
            {
                string toLower = args[0].ToLower();
                if (config.inventories.ContainsKey(toLower))
                    OpenRedeemInventory(player, toLower);
                else
                    SendReply(player, Lang("StorageNotFound", player.UserIDString, toLower));
            }
            else if (args.Length == 2)
            {
                if (!permission.UserHasPermission(player.UserIDString, "redeemstorageapi.admin"))
                {
                    SendReply(player, Lang("NoPermission", player.UserIDString));
                    return;
                }
                string toLower = args[0].ToLower();
                if (!config.inventories.ContainsKey(toLower))
                    SendReply(player, Lang("StorageNotFound", player.UserIDString, toLower));
                ulong userId;
                if (ulong.TryParse(args[1], out userId))
                {
                    SendReply(player, Lang("AdminOpening", player.UserIDString, toLower, userId));
                    OpenRedeemInventory(player, toLower, userId);
                    return;
                }
                else
                {
                    string userToLower = args[1].ToLower();
                    foreach (var oPlayer in BasePlayer.activePlayerList)
                        if (oPlayer.displayName.ToLower().Contains(userToLower))
                        {
                            SendReply(player, Lang("AdminOpening", player.UserIDString, toLower, oPlayer.userID));
                            OpenRedeemInventory(player, toLower, oPlayer.userID);
                            return;
                        }
                }
                SendReply(player, Lang("UserNotFound", player.UserIDString));
            }
        }

        private void OnLootEntityEnd(BasePlayer player, BoxStorage storage)
        {
            if (!openedInventories.ContainsKey(player)) return;
            openedInventories[player]?.Kill();
            openedInventories.Remove(player);
        }

        private void OpenRedeemInventory(BasePlayer player, string name, ulong ownerId = 0)
        {
            if (!permission.UserHasPermission(player.UserIDString, "redeemstorageapi.admin"))
            {
                bool privil = false;
                bool safeZone = player.InSafeZone();
                BuildingPrivlidge priv = player.GetBuildingPrivilege();
                if (priv != null && priv.IsAuthed(player.userID))
                    privil = true;
                int trueCount = 0;
                if (config.inventories[name].authed && privil)
                    trueCount++;
                if (config.inventories[name].safezone && safeZone)
                    trueCount++;
                if (trueCount == 0 && (config.inventories[name].authed || config.inventories[name].safezone))
                {
                    if (config.inventories[name].authed && !privil)
                        PopUpAPI?.Call("ShowPopUp", player, config.popUpPreset, Lang("NotAuthToRefund", player.UserIDString));
                    else if (config.inventories[name].safezone && !safeZone)
                        PopUpAPI?.Call("ShowPopUp", player, config.popUpPreset, Lang("NotInSafeZone", player.UserIDString));
                    return;
                }
            }
            ulong id = ownerId == 0 ? player.userID : ownerId;
            if (!storedItems[name].ContainsKey(id) || !storedItems[name][id].Any())
            {
                SendReply(player, Lang("StorageEmpty", player.UserIDString));
                return;
            }
            BoxStorage container = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", player.transform.position + new Vector3(0, -400)) as BoxStorage;
            container.Spawn();
            openedInventories.TryAdd(player, container);
            openedInventories[player] = container;
            container.inventory.capacity = 48;
            UnityEngine.Object.Destroy(container.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.Destroy(container.GetComponent<GroundWatch>());
            foreach (var dataItem in storedItems[name][id])
            {
                Item item = dataItem.ToItem();
                if (item != null)
                    item.MoveToContainer(container.inventory);
            }
            player.EndLooting();
            container.inventory.onDirty += () => {
                ulong owner = id;
                List<ItemData> items = new List<ItemData>();
                foreach (var item in container.inventory.itemList.ToList())
                    items.Add(ItemData.FromItem(item));
                storedItems[name][id] = items;
            };
            container.inventory.canAcceptItem = (item, i) => false;
            timer.Once(0.3f, () =>
            {
                player.inventory.loot.AddContainer(container.inventory);
                player.inventory.loot.entitySource = container;
                player.inventory.loot.PositionChecks = false;
                player.inventory.loot.MarkDirty();
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic_resizable");
                if (config.inventories[name].message)
                    PopUpAPI?.Call("ShowPopUp", player, config.popUpPreset, Lang($"OpenMessage_{name}", player.UserIDString));
            });
        }

        private void AddItem(ulong userId, string name, Item item, bool popUp = false)
        {
            if (!storedItems.ContainsKey(name))
            {
                PrintWarning($"Player {userId} tried to add item to storage named '{name}' but it doesn't exist in configuration file!");
                return;
            }
            storedItems[name].TryAdd(userId, new List<ItemData>());
            storedItems[name][userId].Add(ItemData.FromItem(item));
            if (popUp)
            {
                BasePlayer player = BasePlayer.FindByID(userId);
                if (player != null)
                {
                    string itemName = item.name != null && item.name != "" ? item.name : item.info.displayName.english;
                    PopUpAPI?.Call("ShowPopUp", player, config.popUpPreset, Lang("NewItemInStorage", player.UserIDString, name, itemName, config.commands.First()));
                }
            }
        }

        private void LoadMessages()
        {
            Dictionary<string, string> langFile = new Dictionary<string, string>()
            {
                ["StorageNotFound"] = "Storage with name <color=#5c81ed>{0}</color> has not been found.",
                ["NoPermission"] = "You don't have permission to use this command!",
                ["AdminOpening"] = "Opening <color=#5c81ed>{0}</color> inventory of <color=#5c81ed>{1}</color>...",
                ["UserNotFound"] = "User has not been found.",
                ["NotAuthToRefund"] = "You are <color=#5c81ed>not authorized in Cupboard</color>. You cannot open this redeem inventory!",
                ["NotInSafeZone"] = "You are <color=#5c81ed>not in safe zone</color>. You cannot open this redeem inventory!",
                ["StorageEmpty"] = "This storage is <color=#5c81ed>empty</color>...",
                ["NewItemInStorage"] = "You've got new item!\nIt's <color=#5c81ed>{1}</color>! You can redeem it by typing <color=#5c81ed>/{2} {0}</color>.",
                ["UnredeemedItemsRemind"] = "You have unredeemed items in your storage!\nRun <color=#5c81ed>/{0} {1}</color> to get your items."
            };
            foreach (var storage in config.inventories)
                if (storage.Value.message)
                    langFile.TryAdd($"OpenMessage_{storage.Key}", "Default Open Message!");
            lang.RegisterMessages(langFile, this);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private PluginConfig config = new PluginConfig();

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(config = new PluginConfig()
            {
                commands = new List<string>() { "redeem", "red" },
                inventories = new Dictionary<string, InventoryConfig>()
                {
                    { "default", new InventoryConfig() { defaultInventory = true, safezone = true, authed = true } },
                    { "shop", new InventoryConfig() { message = true } }
                }
            }, true);
        }

        private class PluginConfig
        {
            [JsonProperty("Redeem Commands")]
            public List<string> commands = new List<string>();

            [JsonProperty("PopUp API Preset")]
            public string popUpPreset = "Legacy";

            [JsonProperty("Redeem Storage Item Reminder (in seconds, 0 to disable)")]
            public int itemReminder = 600;

            [JsonProperty("Redeem Inventories")]
            public Dictionary<string, InventoryConfig> inventories = new Dictionary<string, InventoryConfig>();
        }

        private class InventoryConfig
        {
            [JsonProperty("Default Redeem Inventory (only one)")]
            public bool defaultInventory = false;

            [JsonProperty("PopUp Message (configurable in lang file)")]
            public bool message = false;

            [JsonProperty("Redeem Only In Safezone")]
            public bool safezone = false;

            [JsonProperty("Redeem Only If Authed")]
            public bool authed = false;
        }

        private static readonly Dictionary<string, Dictionary<ulong, List<ItemData>>> storedItems = new Dictionary<string, Dictionary<ulong, List<ItemData>>>();


        private void LoadData()
        {
            foreach (var storage in config.inventories)
            {
                storedItems.TryAdd(storage.Key, new Dictionary<ulong, List<ItemData>>());
                storedItems[storage.Key] = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<ItemData>>>($"{Name}/{storage.Key}");
            }
            timer.Every(Core.Random.Range(500, 700), SaveData);
        }

        private void SaveData()
        {
            foreach (var storage in config.inventories)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/{storage.Key}", storedItems[storage.Key]);
        }

        private class ItemData
        {
            public string Shortname;
            public int Amount;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsBlueprint;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int BlueprintTarget;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public ulong Skin;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float Fuel;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int FlameFuel;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float Condition;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float MaxCondition = -1;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int Ammo;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int AmmoType;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int DataInt;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Name;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Text;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<ItemData> Contents = new List<ItemData>();

            public Item ToItem()
            {
                if (Amount == 0)
                    return null;
                Item item = ItemManager.CreateByName(Shortname, Amount, Skin);
                if (IsBlueprint)
                {
                    item.blueprintTarget = BlueprintTarget;
                    return item;
                }
                item.fuel = Fuel;
                item.condition = Condition;
                if (MaxCondition != -1)
                    item.maxCondition = MaxCondition;
                if (Contents != null)
                {
                    if (Contents.Count > 0)
                    {
                        if (item.contents == null)
                        {
                            item.contents = new ItemContainer();
                            item.contents.ServerInitialize(null, Contents.Count);
                            item.contents.GiveUID();
                            item.contents.parent = item;
                        }
                        foreach (var contentItem in Contents)
                            contentItem.ToItem().MoveToContainer(item.contents);
                    }
                }
                else
                    item.contents = null;
                BaseProjectile.Magazine magazine = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine;
                FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
                if (magazine != null)
                {
                    magazine.contents = Ammo;
                    magazine.ammoType = ItemManager.FindItemDefinition(AmmoType);
                }
                if (flameThrower != null)
                    flameThrower.ammo = FlameFuel;
                if (DataInt > 0)
                {
                    item.instanceData = new ProtoBuf.Item.InstanceData
                    {
                        ShouldPool = false,
                        dataInt = DataInt
                    };
                }
                item.text = Text;
                if (Name != null)
                    item.name = Name;
                return item;
            }

            public static ItemData FromItem(Item item) => new ItemData
            {
                Shortname = item.info.shortname,
                Ammo = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.contents ?? 0,
                AmmoType = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.ammoType?.itemid ?? 0,
                Amount = item.amount,
                Condition = item.condition,
                MaxCondition = item.maxCondition,
                Fuel = item.fuel,
                Skin = item.skin,
                Contents = item.contents?.itemList?.Select(FromItem).ToList(),
                FlameFuel = item.GetHeldEntity()?.GetComponent<FlameThrower>()?.ammo ?? 0,
                IsBlueprint = item.IsBlueprint(),
                BlueprintTarget = item.blueprintTarget,
                DataInt = item.instanceData?.dataInt ?? 0,
                Name = item.name,
                Text = item.text
            };
        }
    }
}