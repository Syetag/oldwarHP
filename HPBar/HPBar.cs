using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using static Rocket.Unturned.Events.UnturnedPlayerEvents;

namespace oldwar
{
    public class oldwarHP : RocketPlugin<Config>
    {
        public static oldwarHP Instance;

        private static ConcurrentDictionary<CSteamID, PlayerData> playersData = new ConcurrentDictionary<CSteamID, PlayerData>();
        public class PlayerData
        {
            public SemaphoreSlim InteractSemaphore { get; set; } = new SemaphoreSlim(1, 1);
        }
        protected override void Load()
        {
            Instance = this;
            UnturnedPlayerEvents.OnPlayerUpdateGesture += OnPlayerUpdateGesture;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            Rocket.Core.Logging.Logger.Log("oldwarHP loaded! Created by SyetaG");
        }

        protected override void Unload()
        {
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= OnPlayerUpdateGesture;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            Instance = null;
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (playersData.TryRemove(player.CSteamID, out var data))
            {
                data.InteractSemaphore.Dispose();
            }
        }

        private void OnPlayerUpdateGesture(UnturnedPlayer player, PlayerGesture gesture)
        {
            if (player == null)
            {
                Rocket.Core.Logging.Logger.LogWarning("Player not found!");
                return;
            }

            if (gesture != PlayerGesture.PunchLeft && gesture != PlayerGesture.PunchRight && gesture != PlayerGesture.Point)
            {
                return;
            }

            var playerData = GetData(player);
            try
            {
                playerData.InteractSemaphore.Wait();

                if (Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var hit, Configuration.Instance.RaycastDistance, RayMasks.BARRICADE_INTERACT | RayMasks.STRUCTURE_INTERACT | RayMasks.VEHICLE))
                {
                    TryDisplayHP(player, hit.transform);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"Error in OnPlayerUpdateGesture: {ex.Message}");
            }
            finally
            {
                playerData.InteractSemaphore.Release();
            }
        }

        private void TryDisplayHP(UnturnedPlayer player, Transform hitTransform)
        {
            try
            {
                if (TryDisplayBarricadeHP(player, hitTransform) || TryDisplayStructureHP(player, hitTransform) || TryDisplayVehicleHP(player, hitTransform))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"Error in TryDisplayHP: {ex.Message}");
            }
        }

        private bool TryDisplayBarricadeHP(UnturnedPlayer player, Transform hitTransform)
        {
            try
            {
                var barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(hitTransform);
                if (barricadeDrop != null)
                {
                    var barricadeData = barricadeDrop.GetServersideData();
                    if (barricadeData != null)
                    {
                        string itemNameWithColor = GetItemNameWithColor(barricadeDrop.asset);
                        DisplayHP(player, barricadeData.barricade.health, barricadeDrop.asset.health, itemNameWithColor);
                        return true;
                    }

                    Rocket.Core.Logging.Logger.LogWarning("Failed to get barricade data!");
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"Error in TryDisplayBarricadeHP: {ex.Message}");
            }

            return false;
        }

        private bool TryDisplayStructureHP(UnturnedPlayer player, Transform hitTransform)
        {
            try
            {
                var structureDrop = StructureManager.FindStructureByRootTransform(hitTransform);
                if (structureDrop != null)
                {
                    var structureData = structureDrop.GetServersideData();
                    if (structureData != null)
                    {
                        string itemNameWithColor = GetItemNameWithColor(structureDrop.asset);
                        DisplayHP(player, structureData.structure.health, structureDrop.asset.health, itemNameWithColor);
                        return true;
                    }

                    Rocket.Core.Logging.Logger.LogWarning("Failed to get structure data!");
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"Error in TryDisplayStructureHP: {ex.Message}");
            }

            return false;
        }

        private bool TryDisplayVehicleHP(UnturnedPlayer player, Transform hitTransform)
        {
            try
            {
                List<InteractableVehicle> vehiclesInRadius = new List<InteractableVehicle>();
                VehicleManager.getVehiclesInRadius(hitTransform.position, 1f, vehiclesInRadius);

                if (vehiclesInRadius.Count > 0)
                {
                    var vehicle = vehiclesInRadius[0];

                    if (vehicle.asset != null)
                    {
                        ushort currentHealth = vehicle.health;
                        ushort maxHealth = vehicle.asset.health;
                        string itemNameWithColor = GetVehicleNameWithColor(vehicle.asset);

                        DisplayHP(player, currentHealth, maxHealth, itemNameWithColor);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"Error in TryDisplayVehicleHP: {ex.Message}");
            }

            return false;
        }

        private void DisplayHP(UnturnedPlayer player, ushort currentHealth, ushort maxHealth, string name)
        {
            if (player == null)
            {
                Rocket.Core.Logging.Logger.LogWarning("Player not found!");
                return;
            }

            try
            {
                EffectManager.sendUIEffect(Configuration.Instance.EffectID, (short)Configuration.Instance.UIKey, player.CSteamID, true);

                float percentage = (float)currentHealth / maxHealth;
                int numSpaces = (int)Math.Ceiling(percentage * 57);
                string scrollText = new string(' ', numSpaces);

                string color = percentage > 0.66f ? "#82d057" : percentage > 0.33f ? "#f4b43f" : "#fa3216"; // Green, Yellow, Red

                string currentHealthFormatted = currentHealth >= 1000 ?
                    (currentHealth / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "K" :
                    currentHealth.ToString();

                string maxHealthFormatted = maxHealth >= 1000 ?
                    (maxHealth / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "K" :
                    maxHealth.ToString();

                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.CSteamID, true, "Scroll", scrollText);
                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.CSteamID, true, "HP", $"<color={color}>{currentHealthFormatted} / {maxHealthFormatted}</color>");
                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.CSteamID, true, "NAME", $"{name}");
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"Error in DisplayHP: {ex.Message}");
            }
        }

        private string GetItemNameWithColor(ItemAsset itemAsset)
        {
            Color rarityColor = ItemTool.getRarityColorUI(itemAsset.rarity);
            string hexColor = Palette.hex(rarityColor);
            return $"<color={hexColor}>{itemAsset.itemName}</color>";
        }

        private string GetVehicleNameWithColor(VehicleAsset vehicleAsset)
        {
            Color rarityColor = ItemTool.getRarityColorUI(vehicleAsset.rarity);
            string hexColor = Palette.hex(rarityColor);
            return $"<color={hexColor}>{vehicleAsset.vehicleName}</color>";
        }

        private PlayerData GetData(UnturnedPlayer player)
        {
            try
            {
                if (!playersData.TryGetValue(player.CSteamID, out var data))
                {
                    data = new PlayerData();
                    playersData.TryAdd(player.CSteamID, data);
                }
                return data;
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError(ex.ToString());
                return null;
            }
        }
    }
}