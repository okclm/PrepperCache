// using Harmony;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop;
using Il2CppTLD.Gameplay;
using Il2CppTLD.Scenes;
using Il2CppVLB;

//using Il2CppInterop.Runtime;
//using Il2CppInterop.Runtime.Injection;
//using Il2CppNewtonsoft.Json.Linq;
//using Il2CppTLD.Logging;
//using Il2CppTLD.Stats;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
//using System.Diagnostics;
//using System.Globalization;
//using System.Reflection;
using System.Runtime.CompilerServices;
//using System.Text.RegularExpressions;
//using System.Xml.Linq;

using System.Collections.Generic;

using UnityEngine;
using static Il2CppAK.STATES;

//using Il2CppTLD.Gameplay;
using Gameplay = Il2CppTLD.Gameplay;
//using UnityEngine.Playables;
//using static Il2CppSystem.Net.ServicePointManager;
using Scene = UnityEngine.SceneManagement;
//using System.Linq;

namespace PrepperCache
{
    public class PrepperCacheMain : MelonMod
    {
        // *** Stuff for capturing telemtry data every waitTime seconds ***
        // private float waitTime = 10.0f; // This is a parameter controlled in the options menu.
        public static float timer = 0.0f;

        // *** Stuff for capturing telemtry data when player's distance from previous position is greater than distance threshold ***
        public static Vector3 previousPosition = new Vector3(0, 0, 0); // This tracks the previous player x,z position logged.

        // Are we in the game menu?
        public static bool inMenu = true;

        public const string MOD_VERSION_NUMBER = "Version 1.2.0 - 03/17/2026";      // The version # of the mod.
        internal const string DEFAULT_FILE_NAME = "PrepperCache.log";               // The log file is written in the MODS folder for TLD  (i.e. D:\Program Files (x86)\Steam\steamapps\common\TheLongDark\Mods)
        //internal const string DEFAULT_FILE_NAME = "Telemetry.log";                // The log file is written in the MODS folder for TLD  (i.e. D:\Program Files (x86)\Steam\steamapps\common\TheLongDark\Mods)

        static string Log_File_Format_Version_Number = "1.1";   // The version # of the log file format.  This is used to determine if the log file format has changed and we need to update the code to read it.
        static string logData_filename = DEFAULT_FILE_NAME;     // This is the logData log file.

        public static string hudMessage = $"undefined";

        private static string GetFilePath() => Path.GetFullPath(Path.Combine(MelonEnvironment.ModsDirectory, logData_filename));
        
        // Create a case-insensitive dictionary to map region to prepper cache name
        public static Dictionary<string, string> mappingsRegion2PrepperCacheName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> mappingsPrepperCache = new Dictionary<string, string>
        {
            { "PrepperCacheAEmpty", "Empty Prepper Cache A" },
            { "PrepperCacheBEmpty", "Empty Prepper Cache B" },
            { "PrepperCacheCEmpty", "Empty Prepper Cache C" },
            { "PrepperCacheDEmpty", "Empty Prepper Cache D" },
            { "PrepperCacheEEmpty", "Empty Prepper Cache E" },
            { "PrepperCacheFEmpty", "Empty Prepper Cache F" },
            { "PrepperCacheA", "Conspiracy Prepper Cache" },
            { "PrepperCacheB", "Stockpile (Kitchen) Prepper Cache" },
            { "PrepperCacheC", "Hunter Prepper Cache" },
            { "PrepperCacheD", "Stockpile (Kitchen) Prepper Cache" },
            { "PrepperCacheE", "Hunting Prepper Cache" },
            { "PrepperCacheF", "Conspiracy Prepper Cache" },
            { "PrepperCacheBInterloper", "Prepper Cache B Interloper" },
            { "PrepperCacheEmpty", "Prepper's Abandoned Cache (Pleasant Valley)" }
        // Add more mappings as needed.
        };

        private static readonly Dictionary<string, string> mappingsRegion = new Dictionary<string, string>
    {
        { "AirfieldRegion", "Forsaken Airfield" },
        { "AshCanyonRegion", "Ash Canyon" },
        { "BlackrockRegion", "Blackrock" },
        { "BlackrockTransitionZone", "Keeper's Pass North" },
        { "CanneryRegion", "Bleak Inlet" },
        { "CanyonRoadTransitionZone", "Keeper's Pass South" },
        { "CoastalRegion", "Coastal Highway" },
        { "CrashMountainRegion", "Timberwolf Mountain" },
        { "DamRiverTransitionZoneB", "Winding River" },
        { "HighwayTransitionZone", "Crumbling Highway" },
        { "HubRegion", "Transfer Pass" },
        { "LakeRegion", "Mystery Lake" },
        { "LongRailTransitionZone", "Far Range Branch Line" },
        { "MarshRegion", "Forlorn Muskeg" },
        { "MiningRegion", "Zone Of Contamination" },
        { "MountainPassRegion", "Sundered Pass" },
        { "MountainTownRegion", "Mountain Town" },
        { "RavineTransitionZone", "Ravine" },
        { "RiverValleyRegion", "Hushed River Valley" },
        { "RuralRegion", "Pleasant Valley" },
        { "TracksRegion", "Broken Railroad" },
        { "WhalingStationRegion", "Desolation Point" }

        // Add more mappings as needed
    };

        // A place to store the prepper cache scene names for quick reference.
        // This is populated as we read the bunker replacement data for the current game and
        // can be used to quickly check if a given scene is one of the bunker replacement scenes.
        internal static HashSet<string> BunkerSceneNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public static void LogData(string text)
        {
            // if text is null, do nothing.  Don't log anything, but also don't throw an exception.
            if (text is null) return;

            // if text is empty, just log the empty line without timestamp.  This allows for better formatting of the log file with blank lines.
            if (text == "")
            {
                File.AppendAllLines(GetFilePath(), new string[] { text });
                return;
            }

            string msg = text;

            // Uncomment next lines to check if text is not empty, log with timestamp.
            // Deterine IRL time.  We use this to timestamp the data with the current IRL time.
            // string irlDateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            // msg = $"; {irlDateTime} {text}";

            File.AppendAllLines(GetFilePath(), new string[] { msg });
        }

        public static void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null)
        {
            // *** This if DEBUG statement will enable all LogMessage text going to the MelonLogger logger.
            // *** Otherwise, we just log to the Telemetry log file.
            #if DEBUG
                // Use static MelonLogger so this method can be called from static contexts (Harmony hooks, etc.)
                MelonLogger.Msg("." + caller + "." + lineNumber + ": " + message);
            #else
                // In release, write lightweight telemetry to file so logs are still available without requiring Melon instance.
                try
                {
                    LogData(";" + caller + "." + lineNumber + ": " + message);
                }
                catch
                {
                    // swallow — don't let logging break the game
                }
            #endif
        }

        public static string SanitizeFileName(string filename)
        {
            // Get a list of invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();

            // Remove any invalid characters from the input
            var sanitized = new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());

            return sanitized;
        }

        public override void OnInitializeMelon()
        {
            // Debug break: in DEBUG builds this will prompt to attach a debugger (Launch) then break.
            // Replace or remove the #if block if you want this in release.

            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch(); // prompts to attach a debugger
            //            }
            //            else
            //            {
            //                Debugger.Break(); // break into already attached debugger
            //            }
            //#endif

            LogMessage("Initializing Melon, starting PrepperCache Mod: " + MOD_VERSION_NUMBER);

            PrepperCache.Settings.OnLoad();
        }

        public override void OnUpdate()
        {
            // return; // TEMP.  Comment out to enable OnUpdate.

            //timer += Time.deltaTime;

            // Are we enabled?
            if (!Settings.enablePrepperCacheDataCapture) // Not enabled.  Nothing to do here.
            {
                // LogMessage($"; Prepper Cache data capture not enabled.  No action taken.");
                return; 
            }

            // We need to determine if we're in a game scene or in a menu (main menu, inventory, map, etc.).
            // We only want to capture data when we're in a game scene.  The reason is that the BunkerDistributor and related data is only relevant when we're in a game scene.
            // If we're in a menu, we don't want to capture data because it may not be relevant and it may also cause errors if we try to access game data that is not available in the menu.
            if (IsGameScene())
            {
                inMenu = false;
            }
            else
            {
                inMenu = true;
                return; // If we're in a menu, exit the OnUpdate.  We only want to capture data when we're in a game scene.
            }

            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / OnUpdate. inMenu={inMenu}]");

            if (GameManager.GetVpFPSPlayer() && (!inMenu))
            {
                // Is the log key pressed?
                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.captureKey))
                {
                    string triggerCode = "K";   // trigger key pressed

                    // Ok, let;s see if we can get the BunkerDistributor stuff...
                    try
                    {
                        ExperienceModeType? emt = ExperienceModeManager.GetCurrentExperienceModeType();
                        LogMessage($"Current experience mode type: {emt?.ToString() ?? "<null>"}");

                        LogMessage($"Current localized game mode: {ExperienceModeManager.s_CurrentGameMode.m_ModeName.m_LocalizationID ?? "<null>"}");

                        if (!emt.HasValue)
                            return;

                        //var stalker = ExperienceModeManager.GetGameModeFromName("Stalker").Cast<SandboxConfig>();
                        //var interloper = ExperienceModeManager.GetGameModeFromName("Interloper").Cast<SandboxConfig>();

                        //var experienceManager = GameManager.GetExperienceModeManagerComponent();
                        ExperienceModeManager experienceManager = GameManager.GetExperienceModeManagerComponent();

                        //var availableModes = experienceManager?.GetAvailableGameModes();
                        //List<GameModeConfig> availableModes = (List<GameModeConfig>)(experienceManager?.GetAvailableGameModes());
                        //var availableModes = experienceManager?.GetAvailableGameModes();
                        //IList<GameModeConfig>? availableModes = (IList<GameModeConfig>?)(experienceManager?.GetAvailableGameModes());
                        //IList<GameModeConfig>? availableModes = (IList<GameModeConfig>?)(experienceManager?.GetAvailableGameModes());
                        IList<GameModeConfig>? availableModes = (experienceManager?.GetAvailableGameModes()) as IList<GameModeConfig>;

                        // <PackageReference Include="STBlade.Modding.TLD.Il2CppAssemblies.Windows" Version="2.51.0" />
                        IList<GameModeConfig>? _testing = GameManager.GetExperienceModeManagerComponent()?.GetAvailableGameModes() as IList<GameModeConfig>;
                        LogMessage($"_testng available game modes count: {_testing?.Count ?? 0}");

                        LogMessage($"Available game modes count: {availableModes?.Count ?? 0}");

                        if (availableModes == null)
                        {
                            LogMessage("Available game modes list is null.");
                            return;
                        }

                        // Find the matching mode
                        GameModeConfig currentModeConfig = null;
                        for (int i = 0; i < availableModes.Count; i++)
                        {
                            var mode = availableModes[i];
                            if (mode != null && mode.name == emt.Value.ToString())
                            {
                                currentModeConfig = mode;
                                break;
                            }
                        }

                        if (currentModeConfig == null)
                        {
                            LogMessage("Current experience mode not found in available modes.");
                            return;
                        }

                        LogMessage($"Found current experience mode: {currentModeConfig.name}");
                        //if (Settings.enablePrepperCacheHUDDisplay)
                        //    HUDMessage.AddMessage($"Current experience mode: {currentModeConfig.name}", false, true);

                        var currentSandbox = ExperienceModeManager
                            .GetGameModeFromName(currentModeConfig.name)
                            ?.Cast<SandboxConfig>();

                        if (currentSandbox == null)
                        {
                            LogMessage("Failed to get SandboxConfig.");
                            return;
                        }

                        var bunkerSetup = currentSandbox.m_BunkerSetup;

                        if (bunkerSetup == null)
                        {
                            LogMessage("Bunker setup is null.");
                            return;
                        }

                        // Determine the hours played.  This is a float and we can use it as a timestamp for the data.
                        float gameTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                        // Sometimes the gameTime is zero here.  Seen most often when entering a new scene (indoors).  Check for gameTime zero and skip logging if so.
                        if (gameTime == 0f)
                        {
                            LogMessage($"; Detected gameTime of zero likely due to scene change timing.  No action taken.");
                            return;     // When gameTime is zero, nothing to do here.
                        }

                        // Setup logging filename with user-defined save name for better identification of which save the data is from.
                        // Sanitize to remove invalid filename characters and replace spaces with underscores.
                        //string ssUDF = SanitizeFileName(SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName);   // Example: "FAR TERRITORY"

                        //LogMessage($"DEBUG: SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName:\"{SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName}\"");

                        //string ssUDFnew = SanitizeFileName(SaveGameSystem.GetCurrentSaveName());
                        //LogMessage($"DEBUG: SaveGameSystem.GetCurrentSaveName():\"{SaveGameSystem.GetCurrentSaveName()}\"");
                        //LogMessage($"DEBUG: SanitizeFileName(SaveGameSystem.GetCurrentSaveName()):\"{SanitizeFileName(SaveGameSystem.GetCurrentSaveName())}\"");

                        // Update the logData filename with the current user-defined save name.

                        // Uncomment next lines to update the logData filename with the experience mode name.  i.e. Voyageur_PrepperCache.log 
                        //ssUDF = SanitizeFileName(currentModeConfig.name);
                        //logData_filename = ssUDF.Replace(" ", "_") + "_" + DEFAULT_FILE_NAME;

                        // If the data file does not exist, write a block of comments as the 1st lines of the file with useful information (i.e. file format version #).
                        if (File.Exists(GetFilePath()) == false)
                        {
                            LogData("; Data file format version: " + Log_File_Format_Version_Number);
                            LogData("; PrepperCache Mod: " + MOD_VERSION_NUMBER);
                            LogData(";");
                            LogData("; Fields are separated by the \"|\" character:");
                            LogData(";   irlDateTime: Real-world date and time when the data was recorded (MM/DD/YYYY HH:MM:SS)");
                            LogData(";   gameTime: In-game hours played (float)");
                            //LogData(";   bunkerInteriorCount: Number of prepper cache interiors");
                            //LogData(";   bunkerReplacementCount: Number of prepper cache replacements");
                            LogData(";   experienceMode: Experience mode for this game (Pilgrim, Voyageur, etc)");
                            LogData(";   gameName: User name for this game");
                            //LogData(";   bunkerNumber: Number of the bunker (typically 0..8)");
                            LogData(";      bunkerNumber.m_Interior.name: Bunker interior name (i.e. PrepperCacheC)");
                            LogData(";      bunkerNumber.m_Interior.name: User-friendly bunker interior name (i.e. (Hunter Prepper Cache)");
                            LogData(";      bunkerNumber.m_LocationReference.SceneName: Name of the scene for this replacement bunker (i.e. BlackrockRegion_SANDBOX)");
                            LogData(";      bunkerNumber.m_LocationReference.SceneName: User-friendly name of the scene for this replacement bunker (i.e. Blackrock)");
                            LogData(";   triggerCode: Code indicating what triggered the data capture (K=Keypress)");
                            LogData(";");

                            // Let's add column headers as well for easier reading of the log file.  This also makes it easier to import into Excel or other tools for analysis.
                            // LogData("irlDateTime|gameTime|experienceMode|gameName|bunkerNumber.m_Interior.name.0|bunkerNumber.m_Interior.userFriendlyName.0|bunkerNumber.m_LocationReference.SceneName.0|bunkerNumber.m_LocationReference.userFriendlySceneName.0|triggerCode");

                            string logDataHeaderBuffer = ";irlDateTime|gameTime|experienceMode|gameName";
                            for (int i = 0; i <= 8 && i <= 8; i++)
                            {
                                logDataHeaderBuffer +=
                                    $"|bunkerName.{i}|bunkerFriendlyName.{i}|sceneName.{i}|sceneNameFriendly.{i}";
                            }
                            logDataHeaderBuffer += "|triggerCode";
                            LogData(logDataHeaderBuffer);
                        }   // If the data file does not exist, write a block of comments as the 1st lines of the file with useful information (i.e. file format version #).

                        // Deterine IRL time.  We use this to timestamp the data with the current IRL time.
                        string irlDateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

                        var interiors = bunkerSetup.m_BunkerInteriors;
                        var replacements = bunkerSetup.m_Replacements;

                        if (interiors == null || replacements == null)
                            return;

                        LogMessage($"Saved game name: {SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName}");
                        LogMessage($"Bunker interior count: {interiors?.Count ?? 0}, Bunker replacement count: {replacements?.Count ?? 0}");
                        //if (Settings.enablePrepperCacheHUDDisplay)
                        //    HUDMessage.AddMessage($"Bunker interior count: {interiors?.Count ?? 0}, Replacement count: {replacements?.Count ?? 0}", false, true);

                        string logDataBuffer = irlDateTime +
                            $"|{gameTime:F10}" +
                            $"|{currentModeConfig.name}" +
                            $"|{SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName}";

                        for (int i = 0; i < interiors.Count && i < replacements.Count; i++)
                        {
                            logDataBuffer += 
                                $"|{interiors[i].m_Interior.name}" +
                                $"|{TranslatePrepperCacheName(interiors[i].m_Interior.name)}" +
                                $"|{replacements[i].m_LocationReference.SceneName}" +
                                $"|{GetHumanFriendlyRegionName(replacements[i].m_LocationReference.SceneName)}";

                            LogMessage($"Bunker #{i}: " +
                                       $"name=\"{interiors[i].m_Interior.name}\" " +
                                       $"({TranslatePrepperCacheName(interiors[i].m_Interior.name)}), " +
                                       $"replacement=\"{replacements[i].m_LocationReference.SceneName}\" " +
                                       $"({GetHumanFriendlyRegionName(replacements[i].m_LocationReference.SceneName)})");
                        }

                        // Last field to write is the trigger code, which indicates what triggered the data capture.  (In this case, it's a key press.)
                        logDataBuffer += $"|{triggerCode}";

                        // Write the logDataBuffer to the log file.
                        LogData(logDataBuffer);

                        //// Are we enabled to capture data to the log file?  If so, write the logDataBuffer to the log file.
                        //if (Settings.enablePrepperCacheDataCapture)
                        //{
                        //   // Now write the logDataBuffer to the log file in one line.  This is more efficient than writing each field separately and ensures that the data for each capture is all on one line in the log file.
                        //   LogData(logDataBuffer);
                        //}

                        // If HUD display is enabled, show a message on the screen with the current experience mode and bunker setup info.
                        //string hudMessage = $"Prepper Caches: {currentModeConfig.name} ";
                        hudMessage = $"Prepper Cache Inventory: ({SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName})\n";
                        var hardModes = new[] { "Interloper", "Misery" };
                        var easyModes = new[] { "Pilgrim", "Voyageur", "Stalker" };

                        // Clear the mappingsRegion2PrepperCacheName dictionary and populate it based on the current bunker setup.
                        // This allows us to map from region name to prepper cache name for the current game mode.
                        mappingsRegion2PrepperCacheName.Clear();
                        if (hardModes.Contains(currentModeConfig.name))
                        {
                            // Hard modes
                            hudMessage += $"Stockpile (Kitchen)->{GetHumanFriendlyRegionName(replacements[0].m_LocationReference.SceneName)}\n" +
                                          $"Hunter->{GetHumanFriendlyRegionName(replacements[3].m_LocationReference.SceneName)}\n" +
                                          $"Conspiracy->{GetHumanFriendlyRegionName(replacements[6].m_LocationReference.SceneName)}";
                            //mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[0].m_LocationReference.SceneName)] = "Stockpile Bunker";
                            //mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[3].m_LocationReference.SceneName)] = "Hunter Bunker";
                            //mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[6].m_LocationReference.SceneName)] = "Conspiracy Bunker";
                        } else if (easyModes.Contains(currentModeConfig.name))
                        {
                            // Easy modes
                            hudMessage += $"Stockpile (Kitchen)->{GetHumanFriendlyRegionName(replacements[6].m_LocationReference.SceneName)}\n" +
                                          $"Hunter->{GetHumanFriendlyRegionName(replacements[7].m_LocationReference.SceneName)}\n" +
                                          $"Conspiracy->{GetHumanFriendlyRegionName(replacements[8].m_LocationReference.SceneName)}";
                            //mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[6].m_LocationReference.SceneName)] = "Stockpile Bunker";
                            //mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[7].m_LocationReference.SceneName)] = "Hunter Bunker";
                            //mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[8].m_LocationReference.SceneName)] = "Conspiracy Bunker";
                        }
                        else
                        {
                            // Something else (custom modes, challenges, etc.)
                        }

                        LogMessage(hudMessage);
                        // Iff the HUD dissplay is enabled and the display duration is greater than zero, show the HUD message.
                        if (Settings.enablePrepperCacheHUDDisplay && Settings.hudDisplayDuration > 0)
                            HUDMessage.AddMessage(hudMessage, Settings.hudDisplayDuration, false, true);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error creating BunkerDistributor: {ex}");
                    }

                    // Reset the wait timer to (near) zero.  Subtracting the waitTime is more accurate over time than resetting to zero.
                    //timer = timer - Settings.waitTime;
                }   // if log key pressed
            }   // if player exists and we're not in a menu
        }   // OnUpdate

        // Helper function to map Bunker name to a more user-friendly name.
        // This is based on the BunkerDistributor code and may need to be updated if the game code changes.
        // Example: "PrepperCacheAEmpty" -> "Empty Prepper"
        //          "PrepperCacheF"      -> "Conspiracy Prepper Cache"

        public static string TranslatePrepperCacheName(string key)
        {
            // Check if the key exists in the mappings dictionary
            if (mappingsPrepperCache.TryGetValue(key, out string? value))
            {
                return value;
            }

            // Return the key if the key is not found
            return key;
        }   // Helper function to map region name to a more user-friendly name.

        // Helper function to map region name to a more user-friendly name.
        public static string GetHumanFriendlyRegionName(string regionName)
        {
            // Iterate over the keys in the mappings to find if regionName contains any base name.
            foreach (var key in mappingsRegion.Keys)
            {
                // if (regionName.Contains(key))
                // Perform a case-insensitive check
                if (regionName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return mappingsRegion[key];
                }
            }

            // Return the key if no matching region is found
            return regionName;
        }   // Helper function to map region name to a more user-friendly name.

        // Helper method to determine if the current scene is a game scene versus a menu or empty or boot scene.
        public static bool IsGameScene(string? sceneName = null)
        {
            //sceneName = string.IsNullOrEmpty(sceneName) ? Name().ToLowerInvariant() : sceneName.ToLowerInvariant();
            sceneName = string.IsNullOrEmpty(sceneName) ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLowerInvariant() : sceneName.ToLowerInvariant();
            if (
                GameManager.BOOT.ToLowerInvariant() == sceneName
                || GameManager.EMPTY.ToLowerInvariant() == sceneName
                || GameManager.GetTargetMainMenuSceneName().ToLowerInvariant() == sceneName
                || string.IsNullOrEmpty(sceneName)
                )
            {
                return false;
            }
            return true;
        } // IsGameScene

        // Hook the UIWorldMap._LoadMap_b__6_0 method, which is called when the world map is loaded and the region names are populated.
        // This allows us to modify the region names on the world map to include the prepper cache information for the current game mode.
        [HarmonyPatch(typeof(UIWorldMap), "_LoadMap_b__6_0")] // _LoadMap_b__6_0(AsyncOperationHandle<Texture2D> op)
        public class WorldMap_LoadMap_b__6_0
        {
            public static void Postfix(UIWorldMap __instance)
            {
                LogMessage($"World map {__instance.name} _LoadMap_b__6_0.");
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    LogMessage($"Region {i + 1}: {__instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().text}");
                    //__instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().text += " (_LoadMap_b__6_0.Postfix)";
                    //__instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().mText += " (_LoadMap_b__6_0.Postfix)";
                    //__instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().ProcessText(true, true);
                    //__instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().ProcessText();
                }
            }
            public static void Prefix(UIWorldMap __instance)
            {
                LogMessage($"World map {__instance.name} _LoadMap_b__6_0.");

                if (LoadPrepperCacheDictionary())
                {
                    LogMessage($"Prepper cache dictionary loaded successfully with {mappingsRegion2PrepperCacheName.Count} entries.");
                }
                else
                {
                    LogMessage($"Failed to load prepper cache dictionary.");
                    return; // If we fail to load the prepper cache dictionary, we can't proceed with modifying the region names, so we exit the Prefix.
                }

                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    LogMessage($"Region {i + 1}: {__instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().text}");

                    // Todo: The region names on the world map are displayed as a single line within a shaded box (LabelBG?).
                    // If we append the prepper cache name as a 2nd line, it does not fit within the shaded box.
                    // Need to see if we can have the shaded box adjust in size so that the multiple lines (2) fit centered within the shaded box.
                    // We may need to adjust the UI to allow for multi-line text or increase the size of the box shade to accommodate the additional text.
                    // This will require some experimentation and may involve modifying the UI prefab for the world map region labels.
                    // For now, we will just append the prepper cache name to the region name and see how it looks.  We can adjust the UI later if needed.

                    // Check if the region name maps to a prepper cache name for the current game mode, and if so, append the prepper cache name to the region name on the world map.
                    string labelText = __instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().text;
                    if (mappingsRegion2PrepperCacheName.TryGetValue(labelText, out string? updatedLabelText))
                    {
                        // Use updatedLabelText here; it contains the prepper cache name
                        __instance.transform.GetChild(i).GetChild(1).GetChild(0).GetComponent<UILabel>().text += $" ({updatedLabelText})";
                    }
                    else
                    {
                        // Handle the case when the key is not found
                        updatedLabelText = null; // or any default value you want to use
                    }
                }   // for each region on the world map
            }   // Prefix for world map loading to modify region names with prepper cache info.
        }   // Hook for world map loading to modify region names with prepper cache info.

        // Helper function to determine the Prepper Caches within the current game and populate the mappingsRegion2PrepperCacheName dictionary.
        // This allows us to map from region name to prepper cache name for the current game mode.
        public static bool LoadPrepperCacheDictionary()
        {
            // Todo:
            // If we have an existing Prepper Cache Dictionary and it reflects the data for the current save game, we can just return true and skip the process of loading the data again.
            // We can check if the dictionary is populated and if the current save game name matches the save game name associated with the data in the dictionary.
            // If so, we can assume that the dictionary is already loaded with the correct data for the current save game and we can just return true.
            // Need a way to track which save game the currently loaded dictionary data is associated with.
            // This could be done with a separate variable that stores the save game name when we load the dictionary,
            // and then we can compare it to the current save game name when this function is called.
            try
            {
                LogMessage($"Current save game name: {SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName}");

                LogMessage($"Current localized game mode: {ExperienceModeManager.s_CurrentGameMode.m_ModeName.m_LocalizationID}");


                ExperienceModeType? emt = ExperienceModeManager.GetCurrentExperienceModeType();
                LogMessage($"Current experience mode type: {emt?.ToString() ?? "<null>"}");
                if (!emt.HasValue) return false;

                //var experienceManager = GameManager.GetExperienceModeManagerComponent();
                ExperienceModeManager experienceManager = GameManager.GetExperienceModeManagerComponent();

                //IList<GameModeConfig> availableModes = (IList<GameModeConfig>)(experienceManager?.GetAvailableGameModes());
                //Il2CppSystem.Collections.Generic.IList<GameModeConfig>? availableModes = (Il2CppSystem.Collections.Generic.IList<GameModeConfig>?)((experienceManager?.GetAvailableGameModes()) as IList<GameModeConfig>);
                //System.Collections.Generic.IList<GameModeConfig>? availableModes = (System.Collections.Generic.IList<GameModeConfig>?)((experienceManager?.GetAvailableGameModes()) as IList<GameModeConfig>);
                //IList<GameModeConfig>? availableModes = (IList<GameModeConfig>?)(experienceManager?.GetAvailableGameModes());
                //Il2CppSystem.Collections.Generic.IList<GameModeConfig>? availableModes = experienceManager?.GetAvailableGameModes();
                //IList<GameModeConfig>? availableModes = (IList<GameModeConfig>?)(experienceManager?.GetAvailableGameModes());
                //IList<GameModeConfig>? availableModes = (IList<GameModeConfig>?)(experienceManager?.GetAvailableGameModes());
                //ICollection<GameModeConfig> availableModes2 = (ICollection<GameModeConfig>)(experienceManager.GetAvailableGameModes());
                IList<GameModeConfig>? availableModes = (experienceManager?.GetAvailableGameModes()) as IList<GameModeConfig>;

                if (availableModes == null)
                {
                    LogMessage("Available game modes list is null.");
                    return false;
                }

                // Find the matching mode
                GameModeConfig? currentModeConfig = null;
                //for (int i = 0; i < availableModes?.Count; i++)
                //{
                //    var mode = availableModes[i];
                //    if (mode != null && mode.name == emt.Value.ToString())
                //    {
                //        currentModeConfig = mode;
                //        break;
                //    }
                //}
                if (currentModeConfig == null)
                {
                    LogMessage("Current experience mode not found in available modes.");
                    return false;
                }

                LogMessage($"Found current experience mode: {currentModeConfig.name}");
                //if (Settings.enablePrepperCacheHUDDisplay)
                //    HUDMessage.AddMessage($"Current experience mode: {currentModeConfig.name}", false, true);

                var currentSandbox = ExperienceModeManager
                    .GetGameModeFromName(currentModeConfig.name)
                    ?.Cast<SandboxConfig>();
                if (currentSandbox == null)
                {
                    LogMessage("Failed to get SandboxConfig.");
                    return false;
                }

                var bunkerSetup = currentSandbox.m_BunkerSetup;
                if (bunkerSetup == null)
                {
                    LogMessage("Bunker setup is null.");
                    return false;
                }

                var interiors = bunkerSetup.m_BunkerInteriors;
                var replacements = bunkerSetup.m_Replacements;
                if (interiors == null || replacements == null)
                {
                    LogMessage("Bunker interiors or replacements are null.");
                    return false;
                }

                // Let's build a string list for the "hard" and "easy" experience modes.
                var hardModes = new[] { "Interloper", "Misery" };
                var easyModes = new[] { "Pilgrim", "Voyageur", "Stalker" };

                // Clear the mappingsRegion2PrepperCacheName dictionary and populate it based on the current bunker setup.
                // This allows us to map from region name to prepper cache name for the current game mode.
                mappingsRegion2PrepperCacheName.Clear();
                if (hardModes.Contains(currentModeConfig.name))
                {
                    // Hard modes
                    mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[0].m_LocationReference.SceneName)] = "Stockpile Bunker";
                    mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[3].m_LocationReference.SceneName)] = "Hunter Bunker";
                    mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[6].m_LocationReference.SceneName)] = "Conspiracy Bunker";
                }
                else if (easyModes.Contains(currentModeConfig.name))
                {
                    // Easy modes
                    mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[6].m_LocationReference.SceneName)] = "Stockpile Bunker";
                    mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[7].m_LocationReference.SceneName)] = "Hunter Bunker";
                    mappingsRegion2PrepperCacheName[GetHumanFriendlyRegionName(replacements[8].m_LocationReference.SceneName)] = "Conspiracy Bunker";
                }
                else
                {
                    // Something else (custom modes, challenges, etc.)
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error determining Prepper Cache Inventory: {ex}");
                return false;
            }

            return true;
        }   // Helper function to determine the Prepper Caches within the current game and populate the mappingsRegion2PrepperCacheName dictionary.

        // Helper function to load the bunker scene names for the current game mode.
        // This is used to determine if we're in a scene that contains a prepper cache, so that we can display the
        // prepper cache information on the world map and other UI elements when we're in those scenes.
        private static void CollectBunkerSceneNames(SandboxConfig sbc)
        {
            BunkerSceneNames.Clear();
            Il2CppTLD.Gameplay.BunkerInteriorSpecification[] interiors = sbc.m_BunkerSetup.m_BunkerInteriors;
            if (interiors == null) return;

            foreach (Il2CppTLD.Gameplay.BunkerInteriorSpecification spec in interiors)
            {
                if (spec?.m_Interior == null) continue;
                string sceneName = spec.m_Interior.name;
                if (string.IsNullOrEmpty(sceneName)) continue;
                BunkerSceneNames.Add(sceneName);
                LogMessage($"Registered bunker scene: {sceneName}");
            }
        }

    }   // PrepperCacheMain

}   // namespace
