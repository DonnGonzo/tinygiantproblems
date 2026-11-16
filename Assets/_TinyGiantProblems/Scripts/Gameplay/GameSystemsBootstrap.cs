using UnityEngine;

namespace TinyGiantProblems
{
    // Installs the economy, mess, debris, hotbar, shop, station and animation
    // systems into the existing Mossback scene at load. Keeping this in code
    // means the authored scene does not need to be rebuilt or hand-edited.
    public static class GameSystemsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var game = Object.FindAnyObjectByType<Expedition>();
            if (!game || game.GetComponent<GuildEconomy>()) return;
            game.gameObject.AddComponent<GuildEconomy>();
            game.gameObject.AddComponent<MessField>();
            game.gameObject.AddComponent<DebrisSystem>();

            var player = game.player;
            if (!player.GetComponent<ToolAbilities>()) player.gameObject.AddComponent<ToolAbilities>().motor = player;
            if (!player.GetComponent<TechnicianAnimator>()) player.gameObject.AddComponent<TechnicianAnimator>().motor = player;
            if (!player.GetComponentInChildren<TechnicianToolVisuals>(true))
            {
                // Scenes built before the GiantTiny models were applied have no held-tool rig.
                var holder = new GameObject("Imported held tools").transform;
                holder.SetParent(player.model, false);
                holder.localPosition = new Vector3(.4f, .9f, .25f);
                holder.gameObject.AddComponent<TechnicianToolVisuals>().motor = player;
            }

            var ui = new GameObject("Guild UI");
            ui.AddComponent<HotbarUI>();
            ui.AddComponent<GuildShopUI>();
            WorldStations.Build(game);
        }
    }
}
