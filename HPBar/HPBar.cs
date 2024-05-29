using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using UnityEngine;
using static Rocket.Unturned.Events.UnturnedPlayerEvents;
using Logger = Rocket.Core.Logging.Logger;

namespace oldwar
{
    public class oldwarHP : RocketPlugin<Config>
    {
        public static oldwarHP Instance;

        protected override void Load()
        {
            base.Load();
            Instance = this;
            UnturnedPlayerEvents.OnPlayerUpdateGesture += OnPlayerGesture;
            Logger.Log("oldwarHP loaded! Created by SyetaG");
        }

        protected override void Unload()
        {
            base.Unload();
            Instance = null;
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= OnPlayerGesture;
        }

        private void OnPlayerGesture(UnturnedPlayer player, PlayerGesture gesture)
        {
            if (player == null)
            {
                Logger.LogWarning("[oldwarHP] Player not found!");
                return;
            }

            if ((EPlayerGesture)gesture != EPlayerGesture.PUNCH_LEFT &&
                (EPlayerGesture)gesture != EPlayerGesture.PUNCH_RIGHT &&
                (EPlayerGesture)gesture != EPlayerGesture.POINT)
                return;

            if (Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var hit, Configuration.Instance.RaycastDistance, RayMasks.BARRICADE_INTERACT | RayMasks.STRUCTURE_INTERACT))
            {
                if (hit.transform == null)
                {
                    return;
                }

                var barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(hit.transform);
                if (barricadeDrop != null)
                {
                    var barricadeData = barricadeDrop.GetServersideData();
                    if (barricadeData != null)
                    {
                        DisplayHP(player, barricadeData.barricade.health, barricadeDrop.asset.health, barricadeDrop.asset.itemName);
                        return;
                    }
                    else
                    {
                        Logger.LogWarning("[oldwarHP] Failed to get barricade data!");
                    }
                }

                var structureDrop = StructureManager.FindStructureByRootTransform(hit.transform);
                if (structureDrop != null)
                {
                    var structureData = structureDrop.GetServersideData();
                    if (structureData != null)
                    {
                        DisplayHP(player, structureData.structure.health, structureDrop.asset.health, structureDrop.asset.itemName);
                    }
                    else
                    {
                        Logger.LogWarning("[oldwarHP] Failed to get structure data!");
                    }
                }
            }
        }

        private void DisplayHP(UnturnedPlayer player, ushort currentHealth, ushort maxHealth, string name)
        {
            if (player == null)
            {
                Logger.LogWarning("[oldwarHP] Player not found!");
                return;
            }

            EffectManager.sendUIEffect(Configuration.Instance.EffectID, (short)Configuration.Instance.UIKey, player.CSteamID, true);

            float percentage = (float)currentHealth / maxHealth;
            int numSpaces = (int)Math.Ceiling(percentage * 57);

            string scrollText = new string(' ', numSpaces);

            string color;
            if (percentage > 0.66f)
            {
                color = "#00FF00";
            }
            else if (percentage > 0.33f)
            {
                color = "yellow";
            }
            else
            {
                color = "red";
            }

            EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.CSteamID, true, "Scroll", scrollText);
            EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.CSteamID, true, "HP", $"<color={color}>{currentHealth} / {maxHealth}</color>");
            EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.CSteamID, true, "NAME", $"{name}");
        }
    }
}