using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ControlWinds.Patches;
using HarmonyLib;
using SailwindModdingHelper;
using System.Reflection;
using UnityEngine;

namespace ControlWinds
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(SailwindModdingHelperMain.GUID, "2.0.0")]
    public class ControlWindsMain : BaseUnityPlugin
    {
        public const string GUID = "com.app24.controlwinds";
        public const string NAME = "Control Winds";
        public const string VERSION = "1.0.5";

        internal static ManualLogSource logSource;

        private ConfigEntry<KeyboardShortcut> toggleSpeedyWindKeybind;
        private ConfigEntry<KeyboardShortcut> resetSpeedKeybind;
        private ConfigEntry<KeyboardShortcut> toggleWindDirectionKeybind;
        private ConfigEntry<KeyboardShortcut> windSpeedupKeybind;
        private ConfigEntry<KeyboardShortcut> windSpeeddownKeybind;

        private ConfigEntry<float> windSpeedIncrease;

        private void Awake()
        {
            logSource = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);

            toggleSpeedyWindKeybind = Config.Bind("Hotkeys", "Toggle Wind Velocity Key", new KeyboardShortcut(KeyCode.V));
            resetSpeedKeybind = Config.Bind("Hotkeys", "Reset Wind Velocity Key", new KeyboardShortcut(KeyCode.J));
            toggleWindDirectionKeybind = Config.Bind("Hotkeys", "Toggle Wind Direction Key", new KeyboardShortcut(KeyCode.H));
            windSpeedupKeybind = Config.Bind("Hotkeys", "Increase Wind Velocity Key", new KeyboardShortcut(KeyCode.KeypadPlus));
            windSpeeddownKeybind = Config.Bind("Hotkeys", "Decrease Wind Velocity Key", new KeyboardShortcut(KeyCode.KeypadMinus));

            windSpeedIncrease = Config.Bind("Values", "Wind Increment", 5f);

            GameEvents.OnPlayerInput += (_, __) =>
            {
                if (toggleSpeedyWindKeybind.Value.IsDown())
                {
                    ControlWindsPatches.speedyWind = !ControlWindsPatches.speedyWind;
                    NotificationUi.instance.ShowNotification($"Fast Wind: {(ControlWindsPatches.speedyWind ? "On" : "Off")}");
                    UISoundPlayer.instance.PlayParchmentSound();
                    if (!ControlWindsPatches.speedyWind)
                    {
                        Wind.currentWind = Vector3.zero;
                    }
                }

                if (toggleWindDirectionKeybind.Value.IsDown())
                {
                    ControlWindsPatches.windDirection = ControlWindsPatches.windDirection.Next();
                    NotificationUi.instance.ShowNotification($"Current Wind Direction: {ControlWindsPatches.windDirection}");
                    UISoundPlayer.instance.PlayCloseSound();
                }

                if (resetSpeedKeybind.Value.IsDown())
                {
                    ControlWindsPatches.speed = 3;
                    NotificationUi.instance.ShowNotification($"Speed reset");
                }

                if (windSpeedupKeybind.Value.IsPressed())
                {
                    ControlWindsPatches.speed += windSpeedIncrease.Value * Time.deltaTime;
                    NotificationUi.instance.ShowNotification($"Current Wind Velocity: {ControlWindsPatches.speed}");
                }

                if (windSpeeddownKeybind.Value.IsPressed())
                {
                    ControlWindsPatches.speed -= windSpeedIncrease.Value * Time.deltaTime;
                    NotificationUi.instance.ShowNotification($"Current Wind Velocity: {ControlWindsPatches.speed}");
                }
            };
        }
    }
}
