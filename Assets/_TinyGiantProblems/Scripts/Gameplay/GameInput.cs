using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyGiantProblems
{
    // Single access point for the player's controls. Bindings live in
    // Resources/Input/TinyGiantControls.inputactions (keyboard/mouse + gamepad)
    // and any player rebinds are stored as JSON overrides in PlayerPrefs.
    public static class GameInput
    {
        const string OverridesKey = "TGP.Bindings.v1";
        static InputActionAsset asset;
        static InputActionMap map;

        public static InputActionAsset Asset { get { Ensure(); return asset; } }
        public static InputAction Move => Get("Move");
        public static InputAction Look => Get("Look");
        public static InputAction Jump => Get("Jump");
        public static InputAction Sprint => Get("Sprint");
        public static InputAction Brace => Get("Brace");
        public static InputAction Use => Get("Use");
        public static InputAction Interact => Get("Interact");
        public static InputAction Reel => Get("Reel");
        public static InputAction Drop => Get("Drop");
        public static InputAction PickUp => Get("PickUp");
        public static InputAction Backpack => Get("Backpack");
        public static InputAction HotbarNext => Get("HotbarNext");
        public static InputAction HotbarPrev => Get("HotbarPrev");
        public static InputAction HotbarScroll => Get("HotbarScroll");
        public static InputAction Zoom => Get("Zoom");
        public static InputAction ZoomModifier => Get("ZoomModifier");
        public static InputAction Recover => Get("Recover");
        public static InputAction Pause => Get("Pause");
        public static InputAction Slot(int index) => Get("Slot" + (index + 1));

        // True when the last input came from a gamepad (for prompts and look scaling).
        public static bool UsingGamepad { get; private set; }

        public static InputAction Get(string name) { Ensure(); return map.FindAction(name, true); }

        static void Ensure()
        {
            if (map != null) return;
            var source = Resources.Load<InputActionAsset>("Input/TinyGiantControls");
            asset = source ? Object.Instantiate(source) : ScriptableObject.CreateInstance<InputActionAsset>();
            var json = PlayerPrefs.GetString(OverridesKey, "");
            if (!string.IsNullOrEmpty(json)) { try { asset.LoadBindingOverridesFromJson(json); } catch { } }
            map = asset.FindActionMap("Player", true);
            map.Enable();
            InputSystem.onActionChange += (obj, change) =>
            {
                if (change != InputActionChange.ActionPerformed || !(obj is InputAction a)) return;
                var device = a.activeControl?.device;
                if (device != null) UsingGamepad = device is Gamepad;
            };
        }

        public static void SaveOverrides() { PlayerPrefs.SetString(OverridesKey, Asset.SaveBindingOverridesAsJson()); PlayerPrefs.Save(); }
        public static void ResetOverrides() { Asset.RemoveAllBindingOverrides(); PlayerPrefs.DeleteKey(OverridesKey); PlayerPrefs.Save(); }

        // Human-readable binding for prompts, e.g. "E" or "X".
        public static string Display(InputAction action)
        {
            int index = action.GetBindingIndex(InputBinding.MaskByGroup(UsingGamepad ? "Gamepad" : "Keyboard&Mouse"));
            if (index < 0) index = 0;
            return action.GetBindingDisplayString(index);
        }
    }
}
