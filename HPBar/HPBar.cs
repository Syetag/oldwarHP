using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using UnityEngine;
using static Rocket.Unturned.Events.UnturnedPlayerEvents;

namespace oldwar
{
    public class BuildHPBarPlugin : RocketPlugin
    {
        public static BuildHPBarPlugin Instance;

        protected override void Load()
        {
            base.Load();
            Instance = this;
            UnturnedPlayerEvents.OnPlayerUpdateGesture += OnPlayerGesture;
        }

        protected override void Unload()
        {
            base.Unload();
            Instance = null;
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= OnPlayerGesture;
        }

        private void OnPlayerGesture(UnturnedPlayer player, PlayerGesture gesture)
        {
            // Проверка: игрок существует?
            if (player == null)
            {
                Rocket.Core.Logging.Logger.LogWarning("[BuildHPBarPlugin] Игрок не найден!");
                return;
            }

            if ((EPlayerGesture)gesture != EPlayerGesture.POINT) return;

            if (Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var hit, 5f, RayMasks.BARRICADE_INTERACT | RayMasks.STRUCTURE_INTERACT))
            {
                // Проверка: найден ли объект?
                if (hit.transform == null)
                {
                    return;
                }

                // Поиск баррикады
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
                        Rocket.Core.Logging.Logger.LogWarning("[BuildHPBarPlugin] Не удалось получить данные баррикады!");
                    }
                }

                // Поиск структуры, если баррикада не найдена
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
                        Rocket.Core.Logging.Logger.LogWarning("[BuildHPBarPlugin] Не удалось получить данные структуры!");
                    }
                }
            }
        }

        private void DisplayHP(UnturnedPlayer player, ushort currentHealth, ushort maxHealth, string name)
        {
            // Проверка: игрок существует?
            if (player == null)
            {
                Rocket.Core.Logging.Logger.LogWarning("[BuildHPBarPlugin] Игрок не найден!");
                return;
            }

            EffectManager.sendUIEffect(14014, 14, player.CSteamID, true);

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

            EffectManager.sendUIEffectText(14, player.CSteamID, true, "Scroll", scrollText);
            EffectManager.sendUIEffectText(14, player.CSteamID, true, "HP", $"<color={color}>{currentHealth} / {maxHealth}</color>");
            EffectManager.sendUIEffectText(14, player.CSteamID, true, "NAME", $"{name}");
        }
    }
}
