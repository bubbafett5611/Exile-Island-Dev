# [US] PvExile Island Survival - Carbon Test Server
This is the Carbon test branch for the **[US] PvExile Island Survival - Carbon Test Server**.

---

#### Current Task
- Copy all functionality from production Exile Island Server
- Finish new Welcome Panel config via Web UI
- Minimize number of plugins installed
- Create more purchasable kits for Web Store

---

#### Target Functionaly
- [ ] Raidable Bases - Pull from production
- [ ] Custom Commands - In Progress
    - [ ] mydrop
    - [ ] vipdrop
    - [ ] giverec
- [ ] Faster Crafting - Built into carbon
- [ ] Larger Stacks - Built into carbon
- [ ] Faster Gather Rates - Built into carbon
- [ ] Better NPC - Pull from production
- [ ] Teleport - Pull from production
- [ ] Skill Tree - Pull from production
- [ ] Defendable Bases - Pull from production
- [ ] PookEvent - Pull from production
- [ ] ShoppyStock - New Project
- [ ] Daily Rewards - New Project
- [ ] Welcome Panel - In progress with v4 of WelcomePanel
- [ ] Virtual Quarries - Test diesel
- [ ] Timed Messages - Smartchat bot still needed?
- [ ] Better Chat - Pull from production

---

## Carbon Console Commands
``c.version``

``c.shutdown`` - Completely unloads Carbon from the game, rendering it fully vanilla.

``c.help`` - Returns a brief introduction to Carbon.

``c.plugins`` - Prints the list of mods and their loaded plugins.

``c.pluginsunloaded`` - Prints the list of unloaded plugins.

``c.pluginsfailed`` - Prints the list of plugins that failed to load (most likely due to compilation issues).

``c.pluginwarns`` - Prints the list of warnings of a specific plugin (or all if no arguments are set).

``c.find`` - Searches through Carbon-processed console commands.

``c.findchat`` - Searches through Carbon-processed chat commands.

``c.addconditional`` - Adds a new conditional compilation symbol to the compiler.

``c.remconditional`` - Removes an existent conditional compilation symbol from the compiler.

``c.conditionals`` - Prints a list of all conditional compilation symbols used by the compiler.

``c.loadconfig`` - Loads Carbon config from file.

``c.saveconfig`` - Saves Carbon config to file.

``c.wipeui`` - Clears the entire CUI containers and their elements from the caller's client.
``c.reloadextensions`` - Fully reloads all extensions.

``c.extensions`` - Prints a list of all currently loaded extensions.

``c.wipemarkers`` - Removes all markers of the calling player or argument filter.

``c.setmodule`` - Enables or disables Carbon modules. Visit root/carbon/modules and use the config file names as IDs.

``c.saveallmodules`` - Saves the configs and data files of all available modules.

``c.savemoduleconfig`` - Saves Carbon module config & data file.

``c.loadmoduleconfig`` - Loads Carbon module config & data file.

``c.modules`` - Prints a list of all available modules.

``c.modulesmanaged`` - Prints a list of all currently loaded extensions.

``c.moduleinfo`` - Prints advanced information about a currently loaded module. From hooks, hook times, hook memory usage and other things.

``c.reloadmodules`` - Fully reloads all modules.

``c.reloadmodule`` - Reloads a currently loaded module assembly entirely.

``c.grant`` - Grant one or more permissions to users or groups. Do 'c.grant' for syntax info.

``c.revoke`` - Revoke one or more permissions from users or groups. Do 'c.revoke' for syntax info.

``c.show`` - Displays information about a specific player or group (incl. permissions, groups and user list). Do 'c.show' for syntax info.

``c.usergroup`` - Adds or removes a player from a group. Do 'c.usergroup' for syntax info.

``c.group`` - Adds or removes a group. Do 'c.group' for syntax info.

``c.reload`` - Reloads all or specific mods / plugins. E.g 'c.reload * <except[]>'' to reload everything.

``c.load`` - Loads all mods and/or plugins. E.g 'c.load * <except[]>'' to load everything you've unloaded.

``c.unload`` - Unloads all mods and/or plugins. E.g 'c.unload * <except[]>' to unload everything. They'll be marked as 'ignored'.

``c.plugininfo`` - Prints advanced information about a currently loaded plugin. From hooks, hook times, hook memory usage and other things.

``c.reloadconfig`` - Reloads a plugin's config file. This might have unexpected results, use cautiously.

``c.uninstallplugin`` - Unloads and uninstalls (moves the file to the backup folder) the plugin with the name.

``c.installplugin`` - Looks up the backups directory and moves the plugin back in the plugins folder installing it with the name.

``c.report`` - Reloads all current plugins, and returns a report based on them at the output path.

``c.recycletick -1`` - Configures the recycling ticks speed.

``c.researchduration -1`` - The duration of waiting whenever researching blueprints.

``c.vendingmachinebuyduration -1`` - The duration of transaction delay when buying from vending machines.

``c.craftingspeedmultiplier -1`` - The time multiplier of crafting items.

``c.mixingspeedmultiplier -1`` - The speed multiplier of mixing table crafts.

``c.exacavatorresourcetickrate -1`` - Excavator resource tick rate.

``c.excavatortimeforfullresources -1`` - Excavator time for processing full resources.

``c.excavatorbeltspeedmax -1`` - Excavator belt maximum speed.

``c.defaultserverchatname -1`` - Default server chat name.

``c.defaultserverchatcolor -1`` - Default server chat message name color.

``c.defaultserverchatid -1`` - Default server chat icon SteamID.

``c.oldrecoil False`` - Used by Carbon (client) servers. Any Carbon client that joins will use old properties version of recoil.

``c.modding True`` - Mark this server as modded or not.

``c.scriptwatchers True`` - When disabled, you must load/unload plugins manually with `c.load` or `c.unload`.

``c.scriptwatchersoption 0`` - Indicates wether the script watcher (whenever enabled) listens to the 'carbon/plugins' folder only, or its subfolders. (0 = Top-only directories, 1 = All directories)

``c.debug 0`` - The level of debug logging for Carbon. Helpful for very detailed logs in case things break. (Set it to -1 to disable debug logging.)

``c.logfiletype 2`` - The mode for writing the log to file. (0=disabled, 1=saves updates every 5 seconds, 2=saves immediately)

``c.unitystacktrace True`` - Enables a big chunk of detail of Unity's default stacktrace. Recommended to be disabled as a lot of it is internal and unnecessary for the average user.

``c.filenamecheck True`` - It checks if the file name and the plugin name matches. (only applies to scripts)

``c.language en`` - Server language used by the Language API.

``c.bypassadmincooldowns False`` - Bypasses the command cooldowns for admin-authed players.

``c.logsplitsize 2.5`` - The size for each log (in megabytes) required for it to be split into separate chunks.

``c.ocommandchecks True`` - Prints a reminding warning if RCON/console attempts at calling an o.* command.

``c.defaultplayergroup default`` - The default group for any player with the regular authority level they get assigned to.

``c.defaultadmingroup admin`` - The default group players with the admin flag get assigned to.

``ccc.resetcooldowns``

``ccc.resetmaxuses``

