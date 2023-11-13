using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("WPKits", "David", "1.2.21")]
    public class WPKits : RustPlugin
    {   
        [PluginReference]Plugin Kits, WelcomePanel, ImageLibrary, Notifications;
    
        #region [Further Customization]

        //Kit Anchors
        private string[] anchors = {
    
            "0.105 0.7-0.480 0.95",
            "0.520 0.7-0.895 0.95",
            "0.105 0.4-0.480 0.65",
            "0.520 0.4-0.895 0.65",
            "0.105 0.10-0.480 0.35",
            "0.520 0.10-0.895 0.35",
        };

        private Dictionary<string, string> _p = new Dictionary<string, string>
        {       
                // "NEXT" BUTTON
                { "bn_Amin", "0.93 0.43" },    
                { "bn_Amax", "0.97 0.62" }, 
                // "PREVIOUS" BUTTON
                { "bp_Amin", "0.03 0.43" },    
                { "bp_Amax", "0.07 0.62" },  
                // SUB PANELS
                    //LOGO PANEL
                    { "lp_Amin", "0.03 0.33" },
                    { "lp_Amax", "0.32 0.94" },
                    //TEXT PANEL
                    { "tp_Amin", "0.34 0.33" },
                    { "tp_Amax", "0.97 0.94" },
                    //BUTTON CLAIM
                    { "btn_Amin", "0.34 0.05" },
                    { "btn_Amax", "0.97 0.28" },     
                    //BUTTON INFO
                    { "btn1_Amin", "0.03 0.05" },
                    { "btn1_Amax", "0.32 0.28" },   
        };

        #endregion

        #region [Hooks]

        private void OnServerInitialized()
        {       
            LoadConfig();    
            DownloadImages();  

            foreach (string kit in config.kits.Keys)
                kitsList.Add(kit);
        }

        private void Loaded()
        {
            if (WelcomePanel == null) Puts(" Core plugin WelcomePanel is not loaded.");
            if (Kits == null) Puts(" Core plugin Kits is not loaded.");
        }

        private void Unload()
        {   
            foreach (var player in BasePlayer.activePlayerList)
                CloseKits_API(player);
        }

        #endregion

        #region [Image Handling]

        private void DownloadImages()
        {   
            foreach (string kit in config.kits.Keys)
            {
                ImageLibrary.Call("AddImage", config.kits[kit].image, config.kits[kit].image); 
            }
        }

        private string Img(string url)
        {
            if (ImageLibrary != null) {
                
                if (!(bool) ImageLibrary.Call("HasImage", url))
                    return url;
                else
                    return (string) ImageLibrary?.Call("GetImage", url);
            }
            else return url;
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("getkit")]
        private void getkit(ConsoleSystem.Arg arg)
        { 
            var player = arg?.Player(); 
            var args = arg.Args;
            if (arg.Player() == null) return;
            if (args.Length < 1) return;
            if (player.IsDead()) return;

            string kitname = "";
            int page = 0;
            
            for (var i = 0; i < args.Count(); i++)
            {   
                if (i == args.Count() - 1)
                    page = Convert.ToInt32(args[i]);
                else
                    kitname += args[i] + " ";
            }

            if ((bool) Kits.Call("TryClaimKit", player, kitname.Remove(kitname.Length-1), false))
            {   
                NextTick(() => { 
                    if (!config.otherSet.closeAfterClaim) {
                        ShowKits(player, page);
                        
                        if (Notifications != null)
                        Notifications.Call("Run",
                            player,
                            5,
                            $"<size=14>You successfuly claimed <b>{config.kits[kitname.Remove(kitname.Length-1)].displayName}</b>.</size>",
                            "0.482 0.675 0.251 1.00",
                            Img(config.kits[kitname.Remove(kitname.Length-1)].image),
                            true
                        );
                    }
                    else {
                        player.SendConsoleCommand("welcomepanel_close");
                        if (Notifications != null)
                        Notifications.Call("Run",
                            player,
                            5,
                            $"<size=14>You successfuly claimed <b>{config.kits[kitname.Remove(kitname.Length-1)].displayName}</b>.</size>",
                            "0.482 0.675 0.251 1.00",
                            Img(config.kits[kitname.Remove(kitname.Length-1)].image),
                            true
                        );
                    }
                });
            } else 
            {   
                NextTick(() => { 
                    ShowKits(player, page);
                });
            }
        }
    
        [ConsoleCommand("notif_cmd_close")]
        private void notif_cmd_close(ConsoleSystem.Arg arg)
        { 
            var player = arg?.Player(); 
            if (arg.Player() == null) return;
            CloseNotif(player);
        }

        [ConsoleCommand("wpkits_page")]
        private void wpkits_page(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            var args = arg.Args;
            if (arg.Player() == null) return;
            if (args.Length > 1) return;

            ShowKits(player, Convert.ToInt32(args[0]));
        }

        [ConsoleCommand("wpkits_info")]
        private void wpkits_info(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            var args = arg.Args;
            if (arg.Player() == null) return;
            //if (args.Length > 1) return;

            string kitname = "";
            
            for (var i = 0; i < args.Count(); i++)
                kitname += args[i] + " ";

            CreateNotif(player, config.kits[kitname.Remove(kitname.Length-1)].desc, config.kits[kitname.Remove(kitname.Length-1)].image, kitname.Remove(kitname.Length-1));
        }

        [ConsoleCommand("wpkits_import")]
        private void wpkits_import(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            var args = arg.Args;
            if (arg.Player() != null) 
            {
                if (!player.IsAdmin)
                    return;
            }
            
            if (args == null)  
            {
                ImportAllKits();
                OnServerInitialized();
                return;
            }

            /* if (args.Length > 1) 
            {   
                Puts($"Kit name can't contain spaces, it has to be one word. Display name can be changed later.");

                if (arg.Player() != null) 
                    SendReply(player, $"<color=#DA6D11>Kit name can't contain spaces, it has to be one word. Display name can be changed later.</color>");  

                return;
            } */

            if (args.Length == 1) 
            {
                JObject kit = (JObject) Kits.Call("GetKitObject", $"{args[0]}");

                if (kit == null)
                {
                    Puts($"Kit \"{args[0]}\" NOT FOUND");

                    SendReply(player, $"<size=12>KIT</size> \"{args[0]}\" <size=12><color=#DA6D11>NOT FOUND</color></size>");  
                    return;
                }

                if (!config.kits.ContainsKey(args[0]))
                {
                    config.kits.Add(args[0], new WPKits.Configuration.KitProperties());
                    config.kits[args[0]].displayName = $"{kit.SelectToken("Name")}"; config.kits[args[0]].displayName = config.kits[args[0]].displayName.Replace("_", " ");
                    config.kits[args[0]].image = $"{kit.SelectToken("KitImage")}";
                    config.kits[args[0]].text = "Short kit description.\nCOOLDOWN: <b><color=#1175A5>{kitCooldown}</color></b> \nUSES LEFT: <b><color=#1175A5>{kitUsesLeft}</color></b>";
                    config.kits[args[0]].desc = $"<size=15><b>{config.kits[args[0]].displayName}</b></size>" + "\n\n{kitDescription}\n\n{kitItemList}";
                    SaveConfig();
                    OnServerInitialized();
                    Puts($"Kit \"{args[0]}\" IMPORTED");

                    SendReply(player, $"<size=12>KIT</size> \"{args[0]}\" <size=12><color=#3CB201>IMPORTED</color></size>");  
                }
                else
                {
                    Puts($"Kit \"{args[0]}\" ALREADY IMPORTED");

                    SendReply(player, $"<size=12>KIT</size> \"{args[0]}\" <size=12><color=#DA6D11>ALREADY IMPORTED</color></size>");  
                }
            }


        }

        #endregion
           
        #region [Kits API] 

        private string KitUsesLeft(BasePlayer player, string _kitName) 
        {   
            int _kitMaxUses = Convert.ToInt32(Kits?.CallHook("GetKitMaxUses", _kitName));
            int _kitTimesUsed = Convert.ToInt32(Kits?.CallHook("GetPlayerKitUses", player.userID, _kitName));
            string _kitUsesLeft = Convert.ToString(_kitMaxUses - _kitTimesUsed);
            return _kitUsesLeft;
        }

        private bool IsKitOnCd(BasePlayer player, string _kitName) 
        {
            double _currentCd = Convert.ToDouble(Kits?.CallHook("GetPlayerKitCooldown", player.userID, _kitName));
            if ( _currentCd == 0)
            {
                return false;
            }
            return true;
        }

        private string GetKitCd(string _kitName) 
        {
            int _kitCdInt = Convert.ToInt32(Kits?.CallHook("GetKitCooldown", _kitName)); 
        
            TimeSpan cooldownTS = TimeSpan.FromSeconds(_kitCdInt); 
            string cooldownFormated = string.Format("{0:D1}D {1:D2}:{2:D2}:{3:D2}", cooldownTS.Days, cooldownTS.Hours, cooldownTS.Minutes, cooldownTS.Seconds);
            if (_kitCdInt < 86400) cooldownFormated = string.Format("{0:D2}:{1:D2}:{2:D2}", cooldownTS.Hours, cooldownTS.Minutes, cooldownTS.Seconds);
            
            string _kitCd = $"{cooldownFormated}";
            return _kitCd; 
        }

        private string GetCurrentKitCd(BasePlayer player, string _kitName) 
        { 
            int _currentCdInt = Convert.ToInt32(Kits?.CallHook("GetPlayerKitCooldown", player.userID, _kitName));
            
            TimeSpan cooldownTS = TimeSpan.FromSeconds(_currentCdInt); 
            string cooldownFormated = string.Format("{0:D1}D {1:D2}:{2:D2}:{3:D2}", cooldownTS.Days, cooldownTS.Hours, cooldownTS.Minutes, cooldownTS.Seconds);
            if (_currentCdInt < 86400) cooldownFormated = string.Format("{0:D2}:{1:D2}:{2:D2}", cooldownTS.Hours, cooldownTS.Minutes, cooldownTS.Seconds);
                        
            string _currentCd = $"{cooldownFormated}";
            return _currentCd; 
        }

        private string GetKitDesc(string _kitName)
        {
            return Convert.ToString(Kits?.CallHook("GetKitDescription", _kitName)); ; 
        }


        private string Replace_Text(BasePlayer player, string _kitName, string _text)
        {   
            return _text.Replace("{kitItemList}", KitItemList(_kitName)).Replace("{kitUsesLeft}", 
                KitUsesLeft(player, _kitName)).Replace("{kitCooldown}", GetKitCd(_kitName)).Replace("{kitDescription}", 
                GetKitDesc(_kitName)).Replace("Full desciption of Kit", " ");
        }
        
        private string Button_Cooldown_Text(BasePlayer player, string _kitName)
        {
            bool _isOnCd = IsKitOnCd(player, _kitName);
            string _playerCd = GetCurrentKitCd(player, _kitName);
            var kit = (JObject) Kits.Call("GetKitObject", _kitName);
            int cost = Convert.ToInt32(kit.SelectToken("Cost"));
            string perm = (string) kit.SelectToken("RequiredPermission");
            int kitUseLimit = Convert.ToInt32(Kits?.CallHook("GetKitMaxUses", _kitName));
            int kitTimesUsed = Convert.ToInt32(Kits?.CallHook("GetPlayerKitUses", player.userID, _kitName));
             
            if (_isOnCd)
            {
                return _playerCd;
            }   
            else
            {   
                if (perm != "" && perm != null)
                {
                    if (!permission.UserHasPermission(player.UserIDString, perm)) 
                        return $"{gl("noPerms")}";
                }
                
                if (kitUseLimit != 0)
                {   
                    if (kitUseLimit - kitTimesUsed < 1)
                        return $"{gl("noUses")}";
                }
                
                if (cost != 0)
                {
                        return $"{cost} {config.otherSet.kitCurrency}";
                }
                
                return $"{gl("claim")}";
            }
        }

        private string Button_ColorChange(BasePlayer player, string _kitName)
        {
            bool isOnCd = IsKitOnCd(player, _kitName);

            var kit = (JObject) Kits.Call("GetKitObject", _kitName);
            string perm = (string) kit.SelectToken("RequiredPermission");
            int kitUseLimit = Convert.ToInt32(Kits?.CallHook("GetKitMaxUses", _kitName));
            int kitTimesUsed = Convert.ToInt32(Kits?.CallHook("GetPlayerKitUses", player.userID, _kitName));

            string red = config.otherSet.oncdBtnColor;
            string green = config.otherSet.claimBtnColor;

            if (isOnCd)
            {
                return red;
            }  

            if (perm != "" && perm != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, perm)) 
                    return config.otherSet.infoBtnColor;
            }

            if (kitUseLimit != 0)
            {   
                if (kitUseLimit - kitTimesUsed < 1)
                    return red;
            }

            return green;
        }

        #endregion

        #region [Cui API]

        private void ShowKits_Page1_API(BasePlayer player) => ShowKits(player);

        private void ImportAllKits()
        {
            if (Kits != null && Kits.IsLoaded)
            {
                var kits = Kits.Call<string[]>("GetAllKits");
                if (kits != null)
                {
                    foreach (var kit in kits) 
                        ImportKitFromPlugin(kit);

                    SaveConfig();
                }
            }
            else if (Interface.Oxide.DataFileSystem.ExistsDatafile($"Kits/kits_data"))
            {   
                try 
                {
                    var kitdata = Interface.Oxide.DataFileSystem.GetDatafile("Kits/kits_data");  
                    string json = JsonConvert.SerializeObject(kitdata.Get("_kits"));  
                    JObject _json = JObject.Parse(json);
                    
                    foreach (var item in _json) 
                        ImportKitFromPlugin(item.Key);

                    SaveConfig();
                }
                catch
                {
                    Puts("Something went wrong while importing kits, make sure your kits_data.json is valid and without errors.");

                    foreach (var player in BasePlayer.activePlayerList)
                    { if(player.IsAdmin) SendReply(player, "Something went wrong while importing kits, make sure your kits_data.json is valid and without errors.");  }
                }
            }
            else
            {
                Puts("data/Kits/kits_data.json was not found.");

                foreach (var player in BasePlayer.activePlayerList)
                { if(player.IsAdmin) SendReply(player, "data/Kits/kits_data.json was not found.");  }
            }
        }

        private void ImportKitFromPlugin(string kitName)
        {
            /* if (item.Key.Contains(" "))
            {
                Puts($"Kit \"{item.Key}\" NOT IMPORTED (reason: name contains spaces)");

                foreach (var player in BasePlayer.activePlayerList)
                { if(player.IsAdmin) SendReply(player, $"<size=12>KIT</size> \"{item.Key}\" <size=12><color=#E33B30>NOT IMPORTED</color></size> (reason: name contains spaces)");  }

                continue;
            } */
            
            var kit = (JObject) Kits.Call("GetKitObject", $"{kitName}");
            if (!config.kits.ContainsKey(kitName))
            {
                config.kits.Add(kitName, new WPKits.Configuration.KitProperties());
                config.kits[kitName].displayName = $"{kit.SelectToken("Name")}";
                config.kits[kitName].displayName = config.kits[kitName].displayName.Replace("_", " ");
                config.kits[kitName].image = $"{kit.SelectToken("KitImage")}";
                config.kits[kitName].text =
                    "Short kit description.\nCOOLDOWN: <b><color=#1175A5>{kitCooldown}</color></b> \nUSES LEFT: <b><color=#1175A5>{kitUsesLeft}</color></b>";
                config.kits[kitName].desc = $"<size=15><b>{config.kits[kitName].displayName}</b></size>" +
                                         "\n\n{kitDescription}\n\n{kitItemList}";

                Puts($"Kit \"{kitName}\" IMPORTED");

                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin)
                        SendReply(player,
                            $"<size=12>KIT</size> \"{kitName}\" <size=12><color=#3CB201>IMPORTED</color></size>");
                }
            }
            else
            {
                Puts($"Kit \"{kitName}\" ALREADY IMPORTED");

                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin)
                        SendReply(player,
                            $"<size=12>KIT</size> \"{kitName}\" <size=12><color=#DA6D11>ALREADY IMPORTED</color></size>");
                }
            }
        }

        private string KitItemList(string kitName)
        {
            JObject kit = (JObject) Kits.Call("GetKitObject", $"{kitName}");
            if (kit == null)
            {
                Puts($"(!ERROR) Kit '{kitName}' does not exist.");
                return "Something went wrong while trying to import item list.";
            }

            string result = "";

            var _belt = kit.SelectToken("BeltItems");
            foreach (var item in _belt)
            {   
                string shortname = $"{item.SelectToken("Shortname")}";
                string amount = $"{item.SelectToken("Amount")}";
                var itemDef = ItemManager.FindItemDefinition(shortname);
                if (itemDef == null) 
                    result += $"{amount}x <WRONG SHORTNAME>\n";
                else
                    result += $"{amount}x <b>{itemDef.displayName.translated}</b>\n";
            }

            var _main = kit.SelectToken("MainItems");
            foreach (var item in _main)
            {   
                string shortname = $"{item.SelectToken("Shortname")}";
                string amount = $"{item.SelectToken("Amount")}";
                var itemDef = ItemManager.FindItemDefinition(shortname);
                if (itemDef == null) 
                    result += $"{amount}x <WRONG SHORTNAME>\n";
                else
                    result += $"{amount}x <b>{itemDef.displayName.translated}</b>\n";
            }

            var _wear = kit.SelectToken("WearItems");
            foreach (var item in _wear)
            {   
                string shortname = $"{item.SelectToken("Shortname")}";
                string amount = $"{item.SelectToken("Amount")}";
                var itemDef = ItemManager.FindItemDefinition(shortname);
                if (itemDef == null) 
                    result += $"{amount}x <WRONG SHORTNAME>\n";
                else
                    result += $"{amount}x <b>{itemDef.displayName.translated}</b>\n";
            }

            return result;
        }

        List<string> kitsList = new List<string>();

        private void ShowKits(BasePlayer player, int page = 0)
        {   
            int startingPos = page * 6;
            int itemsLeft = kitsList.Count - startingPos;
            int itemsToShow = itemsLeft;
            if (itemsToShow > 6)
                itemsToShow = 6;

            var container = CUIClass.CreateOverlay("empty", "0 0 0 0.0", "0 0", "0 0", false, 0.0f, $"assets/icons/iconmaterial.mat");

            if (config.kits.Count() < 1)
            {
                CUIClass.CreateText(ref container, "noKits_text", "WelcomePanel_content", "1 1 1 1", "<size=20>Your kit list in config is empty.</size>\nIf you wish to import all kits from RustKits plugin, use console command 'wpkits_import'", 17, "0 0", "1 1", TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", config.otherSet.fontOutlineColor, $"{config.otherSet.fontOutlineThickness} {config.otherSet.fontOutlineThickness}");
                CuiHelper.DestroyUi(player, "noKits_text");
                CuiHelper.AddUi(player, container);        
            }
            
            for (var i = 0; i < itemsToShow; i++)
            {   
                string kitName = kitsList[i + startingPos];
                string[] splitA = anchors[i].Split('-'); 

                if ((bool) Kits.Call("IsKit", kitName))
                {
                    string shortText = Replace_Text(player, kitName, config.kits[kitName].text);
                    string buttonText = Button_Cooldown_Text(player, kitName);  
                    string buttonColor = Button_ColorChange(player, kitName); 
                    
                    
                    CUIClass.CreatePanel(ref container, $"kit_panel{i}", "WelcomePanel_content", config.otherSet.mainColor, splitA[0], splitA[1], false, config.otherSet.fade, "assets/content/ui/uibackgroundblur.mat");
                        CUIClass.CreatePanel(ref container, "logo_panel", $"kit_panel{i}", config.otherSet.secColor, _p["lp_Amin"], _p["lp_Amax"], false, config.otherSet.fade, "assets/content/ui/uibackgroundblur.mat");
                        CUIClass.CreatePanel(ref container, "text_panel", $"kit_panel{i}", config.otherSet.secColor, _p["tp_Amin"], _p["tp_Amax"], false, config.otherSet.fade, "assets/content/ui/uibackgroundblur.mat");
                        CUIClass.CreateButton(ref container, "btn_panel", $"kit_panel{i}", $"{buttonColor}", buttonText, 11, _p["btn_Amin"], _p["btn_Amax"], $"getkit {kitName} {page}", "", "1 1 1 1", config.otherSet.fade, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");
                        CUIClass.CreateButton(ref container, "btn_panel", $"kit_panel{i}", config.otherSet.infoBtnColor, $"{gl("info")}", 11, _p["btn1_Amin"], _p["btn1_Amax"], $"wpkits_info {kitName}", "", "1 1 1 1", config.otherSet.fade, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");
                            CUIClass.CreateText(ref container, "name", "text_panel", "1 1 1 1", config.kits[kitName].displayName, 20, "0.040 0", "1 0.95", TextAnchor.UpperLeft, $"robotocondensed-bold.ttf", config.otherSet.fontOutlineColor, $"{config.otherSet.fontOutlineThickness} {config.otherSet.fontOutlineThickness}");
                            CUIClass.CreateText(ref container, "text", "text_panel", "1 1 1 1", shortText, 10, "0.040 0", "1 0.60", TextAnchor.UpperLeft, $"robotocondensed-regular.ttf", config.otherSet.fontOutlineColor, $"{config.otherSet.fontOutlineThickness} {config.otherSet.fontOutlineThickness}");
                            CUIClass.CreateImage(ref container, "logo_panel", Img($"{config.kits[kitName].image}"), "0.05 0.05", "0.95 0.95", config.otherSet.fade);
                }
                else
                {
                    CUIClass.CreatePanel(ref container, $"kit_panel{i}", "WelcomePanel_content", config.otherSet.mainColor, splitA[0], splitA[1], false, config.otherSet.fade, "assets/content/ui/uibackgroundblur.mat");
                        CUIClass.CreateText(ref container, "name", $"kit_panel{i}", "1 1 1 1", $"Kit '{kitName}' does not exist.", 15, "0.0 0", "1 1", TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", config.otherSet.fontOutlineColor, $"{config.otherSet.fontOutlineThickness} {config.otherSet.fontOutlineThickness}"); 
                }
            }

            if (page != 0)
                CUIClass.CreateButton(ref container, "btn_prev", "WelcomePanel_content", config.otherSet.mainColor, "<", 12, _p["bp_Amin"], _p["bp_Amax"], $"wpkits_page {page - 1}", "", "1 1 1 1", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");
            
            if (itemsLeft > 6)
                CUIClass.CreateButton(ref container, "btn_next", "WelcomePanel_content", config.otherSet.mainColor, ">", 15, _p["bn_Amin"], _p["bn_Amax"], $"wpkits_page {page + 1}", "", "1 1 1 1", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");
            
            CuiHelper.DestroyUi(player, "btn_prev");
            CuiHelper.DestroyUi(player, "btn_next");
            CuiHelper.DestroyUi(player, "empty");

            for (var i = 0; i < 7; i++)
                CuiHelper.DestroyUi(player, $"kit_panel{i}");

            CuiHelper.AddUi(player, container); 
            
        }

        private void CloseKits_API(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "btn_prev");
            CuiHelper.DestroyUi(player, "btn_next");
            CuiHelper.DestroyUi(player, "main_not");    
            CuiHelper.DestroyUi(player, "empty");
            

            for (var i = 0; i < 7; i++)
                CuiHelper.DestroyUi(player, $"kit_panel{i}");
        }

        private void CreateNotif(BasePlayer player, string _text, string _icon, string _kitName)
        {   
            string _textReplaced = Replace_Text(player, _kitName, _text);
            var _uiNotification = CUIClass.CreateOverlay("main_not", "0.19 0.19 0.19 0.90", "0 0", "1 1", true, 0.0f, $"assets/content/ui/uibackgroundblur.mat");
            CUIClass.CreatePanel(ref _uiNotification, "blurr2", "main_not", "0 0 0 0.95", "0 0", "1 1", false, 0f, "assets/content/ui/uibackgroundblur.mat");
            
            CUIClass.CreatePanel(ref _uiNotification, "main_panel", "main_not", "0.19 0.19 0.19 0.99", "0.35 0.25", "0.65 0.75", false, 0f, "assets/content/ui/uibackgroundblur.mat");
            CUIClass.CreateText(ref _uiNotification, "text_title", "main_panel", "1 1 1 1", _textReplaced, 12, "0.1 0", "0.9 0.75", TextAnchor.UpperCenter, $"robotocondensed-regular.ttf", config.otherSet.fontOutlineColor, $"{config.otherSet.fontOutlineThickness} {config.otherSet.fontOutlineThickness}");
            CUIClass.CreateImage(ref _uiNotification, "main_panel", Img($"{_icon}"), "0.435 0.8", "0.565 0.95");
            if (config.kits[_kitName].infoImage != null)
                CUIClass.CreateImage(ref _uiNotification, "main_panel", Img(config.kits[_kitName].infoImage), "0 0", "1 1");

            CUIClass.CreateButton(ref _uiNotification, "close_btn", "main_panel", "0.56 0.20 0.15 1.0", "✘", 11, "0.9 0.9", "0.97 0.97", "notif_cmd_close", "", "1 1 1 1", 0f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf");       
            
            CloseNotif(player);
            CuiHelper.AddUi(player, _uiNotification); 
        }

        private void CloseNotif(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "main_not");
        }

        #endregion 

        #region [Cui Class]

        public class CUIClass
        {
            public static CuiElementContainer CreateOverlay(string _name, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 1f, string _mat ="")
            {   

            
                var _element = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = _color, Material = _mat, FadeIn = _fade},
                            RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                            CursorEnabled = _cursorOn
                        },
                        new CuiElement().Parent = "Overlay",
                        _name
                    }
                };
                return _element;
            }

            public static void CreatePanel(ref CuiElementContainer _container, string _name, string _parent, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 1f, string _mat2 ="")
            {
                _container.Add(new CuiPanel
                {
                    Image = { Color = _color, Material = _mat2, FadeIn = _fade },
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                    CursorEnabled = _cursorOn
                },
                _parent,
                _name);
            }

            public static void CreateImage(ref CuiElementContainer _container, string _parent, string _image, string _anchorMin, string _anchorMax, float _fade = 0f)
            {
                if (_image.StartsWith("http") || _image.StartsWith("www"))
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Url = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
                else
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Png = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
            }

            public static void CreateInput(ref CuiElementContainer _container, string _name, string _parent, string _color, int _size, string _anchorMin, string _anchorMax, string _font = "permanentmarker.ttf", string _command = "command.processinput", TextAnchor _align = TextAnchor.MiddleCenter)
            {
                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,

                    Components =
                    {
                        new CuiInputFieldComponent
                        {

                            Text = "0",
                            CharsLimit = 11,
                            Color = _color,
                            IsPassword = false,
                            Command = _command,
                            Font = _font,
                            FontSize = _size,
                            Align = _align
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = _anchorMin,
                            AnchorMax = _anchorMax

                        }

                    },
                });
            }

            public static void CreateText(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "robotocondensed-bold.ttf", string _outlineColor = "", string _outlineScale ="")
            {   
               

                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = _text,
                            FontSize = _size,
                            Font = _font,
                            Align = _align,
                            Color = _color,
                            FadeIn = 0f,
                        },

                        new CuiOutlineComponent
                        {
                            
                            Color = _outlineColor,
                            Distance = _outlineScale
                            
                        },

                        new CuiRectTransformComponent
                        {
                             AnchorMin = _anchorMin,
                             AnchorMax = _anchorMax
                        }
                    },
                });
            }

            public static void CreateButton(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, string _command = "", string _close = "", string _textColor = "0.843 0.816 0.78 1", float _fade = 1f, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "")
            {       
               
                _container.Add(new CuiButton
                {
                    Button = { Close = _close, Command = _command, Color = _color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", FadeIn = _fade},
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                    Text = { Text = _text, FontSize = _size, Align = _align, Color = _textColor, Font = _font, FadeIn = _fade}
                },
                _parent,
                _name);
            }

        }
        #endregion

        #region [Config] 

        private Configuration config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.CreateConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        

        class Configuration
        {   

            [JsonProperty(PropertyName = "Settings")]
            public OtherSet otherSet { get; set; }

            public class OtherSet
            {       
                [JsonProperty("Kits Currency Name")]
                public string kitCurrency { get; set; }

                [JsonProperty("Close WelcomePanel after claiming kit")]
                public bool closeAfterClaim { get; set; }

                [JsonProperty("Main Panel Color")]
                public string mainColor { get; set; }

                [JsonProperty("Secondary Panel Color")]
                public string secColor { get; set; }

                [JsonProperty("Claim Button Color")]
                public string claimBtnColor { get; set; }

                [JsonProperty("On Cooldown Button Color")]
                public string oncdBtnColor { get; set; }

                [JsonProperty("Info Button Color")]
                public string infoBtnColor { get; set; }

                [JsonProperty("Fade In Value")]
                public float fade { get; set; }

                [JsonProperty("Font Outline Color")]
                public string fontOutlineColor { get; set; }

                [JsonProperty("Font Outline Thickness")]
                public string fontOutlineThickness { get; set; }
            }

            [JsonProperty(PropertyName = "Kits")]
            public Dictionary<string, KitProperties> kits { get; set; }

            public class KitProperties
            {       
                [JsonProperty("» Display Name")]
                public string displayName { get; set; }

                [JsonProperty("» Image")]
                public string image { get; set; }

                [JsonProperty("» Big Image")]
                public string infoImage { get; set; }

                [JsonProperty("» Short Text")]
                public string text { get; set; }

                [JsonProperty("» Description")]
                public string desc { get; set; }
            }

            
            public static Configuration CreateConfig()
            {
                return new Configuration
                {   
                    otherSet = new WPKits.Configuration.OtherSet  
                    { 
                        kitCurrency = "scrap",
                        fontOutlineColor = "0 0 0 1",
                        fontOutlineThickness = "1",
                        fade = 0.1f,
                        mainColor = "0.25 0.25 0.25 0.45",
                        secColor = "0.19 0.19 0.19 0.65",
                        claimBtnColor = "0.31 0.37 0.20 1.0",
                        oncdBtnColor = "0.56 0.20 0.15 1.0",
                        infoBtnColor = "0.19 0.19 0.19 0.65",
                    },

                    kits = new Dictionary<string, KitProperties>{},
                };
            }
        }

        #endregion
    
        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["info"] = "KIT INFO",
                ["claim"] = "CLAIM KIT",
                ["noPerms"] = "NO PERMISSION",
                ["noUses"] = "NO USES LEFT",

            }, this);
        }

        private string gl(string _message) => lang.GetMessage(_message, this);

        #endregion
    }
}