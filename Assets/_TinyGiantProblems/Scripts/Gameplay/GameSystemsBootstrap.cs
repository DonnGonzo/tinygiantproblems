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
            game.gameObject.AddComponent<MissionManager>();
            game.gameObject.AddComponent<GuildEconomy>();
            game.gameObject.AddComponent<MessField>();
            game.gameObject.AddComponent<DebrisSystem>();

            GameSettings.Apply();
            var player = game.player;
            if (!player.GetComponent<ToolAbilities>()) player.gameObject.AddComponent<ToolAbilities>().motor = player;
            if (!player.GetComponentInChildren<TechnicianToolVisuals>(true))
            {
                // Scenes built before the GiantTiny models were applied have no held-tool rig.
                var holder = new GameObject("Imported held tools").transform;
                holder.SetParent(player.model, false);
                holder.localPosition = new Vector3(.4f, .9f, .25f);
                holder.gameObject.AddComponent<TechnicianToolVisuals>().motor = player;
            }
            if (!player.GetComponent<ClimbingClaw>()) player.gameObject.AddComponent<ClimbingClaw>().motor = player;
            if (!player.GetComponent<FallHurt>()) player.gameObject.AddComponent<FallHurt>().motor = player;
            TechnicianModelInstaller.Install(player);
            if (!player.GetComponent<PlacementSystem>()) player.gameObject.AddComponent<PlacementSystem>().motor = player;
            if (!player.GetComponent<GameFeel>()) player.gameObject.AddComponent<GameFeel>();
            MapBoundary.Build(game);

            var ui = new GameObject("Guild UI");
            ui.AddComponent<HotbarUI>();
            ui.AddComponent<GuildShopUI>();
            ui.AddComponent<GuildMemberEditor>();
            ui.AddComponent<MenuUI>();
            WorldStations.Build(game);
        }
    }
}
