using UnityEngine;
using ModSettings;
using MelonLoader;
using System.Diagnostics.Contracts;
using System.Reflection;
using Il2Cpp;
using PrepperCache;
using System.Runtime.CompilerServices;
using System.Data.SqlTypes;


namespace PrepperCache
{
    internal class PrepperCacheSettings : JsonModSettings
    {     
		[Section("General (" + PrepperCacheMain.MOD_VERSION_NUMBER + ")")]

        [Name("Enable Prepper Cache data capture")]
        [Description("Enable/Disable Prepper Cache data capture in PrepperCache.log")]
        public bool enablePrepperCacheDataCapture = false;

        [Name("Capture Prepper Cache Key")]
        [Description("Which key you press to manually capture Prepper Cache data")]
        public KeyCode captureKey = KeyCode.Keypad1;

        [Name("Enable HUD display messages")]
        [Description("Enable/Disable Prepper Cache informational messages on HUD display")]
        public bool enablePrepperCacheHUDDisplay = true;

        [Name("HUD Prepper Cache Inventory display duration")]
        [Description("Seconds to display the HUD Prepper Cache Inventory message")]
        [Slider(0, 30)] // 0, 1, ..., 29, 30
        public int hudDisplayDuration = 10;

        [Name("Enable World Map Prepper Cache Inventory Display")]
        [Description("Enable/Disable Prepper Cache informational display on World Map")]
        public bool enablePrepperCacheWorldMapDisplay = true;

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();

            Settings.enablePrepperCacheDataCapture = enablePrepperCacheDataCapture;
            Settings.enablePrepperCacheHUDDisplay = enablePrepperCacheHUDDisplay;
            Settings.hudDisplayDuration = hudDisplayDuration;
            Settings.enablePrepperCacheWorldMapDisplay = enablePrepperCacheWorldMapDisplay;

            // Reset the odometer.  Set the previous position to the player current position
            PrepperCacheMain.previousPosition = GameManager.GetVpFPSPlayer().transform.position;

            // Reset the wait timer to (near) zero.  Subtracting the waitTime is more accurate over time than resetting to zero.
            // PrepperCacheMain.timer = PrepperCacheMain.timer - Settings.waitTime;
        }
    }

     internal static class Settings
    {
        public static PrepperCacheSettings? options;

        public static int waitTime = 60;                    // This is the wait time in seconds required to log the current player position.
        public static bool enablePrepperCacheDataCapture = false;
        public static bool enablePrepperCacheHUDDisplay = true;
        public static int hudDisplayDuration = 10;          // This is the seconds to display the HUD Prepper Cache Inventory message.
        public static bool enablePrepperCacheWorldMapDisplay = true;

        public static void OnLoad()
        {
            options = new PrepperCacheSettings();
            options.AddToModSettings("Prepper Cache Capture");

            // Initialize option variables
            enablePrepperCacheDataCapture = options.enablePrepperCacheDataCapture;
            enablePrepperCacheHUDDisplay = options.enablePrepperCacheHUDDisplay;
            hudDisplayDuration = options.hudDisplayDuration;
            enablePrepperCacheWorldMapDisplay = options.enablePrepperCacheWorldMapDisplay;
       }
    }
}
