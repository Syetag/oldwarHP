using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Concurrent;
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
                Rocket.Core.Logging.Logger.LogError("[oldwarHP] Player not found!");
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
                Rocket.Core.Logging.Logger.LogException(ex, "OnPlayerUpdateGesture");
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
                if (TryDisplayEntityHP(player, hitTransform, out string name, out ushort currentHealth, out ushort maxHealth))
                {
                    DisplayHP(player, currentHealth, maxHealth, name);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "TryDisplayHP");
            }
        }

        private bool TryDisplayEntityHP(UnturnedPlayer player, Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            return TryDisplayBarricadeHP(player, hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayStructureHP(player, hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayVehicleHP(player, hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayDoorHP(player, hitTransform, out name, out currentHealth, out maxHealth);
        }

        private bool TryDisplayBarricadeHP(UnturnedPlayer player, Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            try
            {
                var barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(hitTransform);
                if (barricadeDrop != null)
                {
                    var barricadeData = barricadeDrop.GetServersideData();
                    if (barricadeData != null)
                    {
                        name = GetItemNameWithColor(barricadeDrop.asset);
                        currentHealth = barricadeData.barricade.health;
                        maxHealth = barricadeDrop.asset.health;
                        return true;
                    }

                    Rocket.Core.Logging.Logger.LogError("[oldwarHP] Failed to get barricade data!");
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "TryDisplayBarricadeHP");
            }

            return false;
        }

        private bool TryDisplayStructureHP(UnturnedPlayer player, Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            try
            {
                var structureDrop = StructureManager.FindStructureByRootTransform(hitTransform);
                if (structureDrop != null)
                {
                    var structureData = structureDrop.GetServersideData();
                    if (structureData != null)
                    {
                        name = GetItemNameWithColor(structureDrop.asset);
                        currentHealth = structureData.structure.health;
                        maxHealth = structureDrop.asset.health;
                        return true;
                    }

                    Rocket.Core.Logging.Logger.LogError("[oldwarHP] Failed to get structure data!");
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "TryDisplayStructureHP");
            }

            return false;
        }

        private bool TryDisplayVehicleHP(UnturnedPlayer player, Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            try
            {
                var vehicle = hitTransform.GetComponent<InteractableVehicle>();

                if (vehicle != null && vehicle.asset != null)
                {
                    name = GetVehicleNameWithColor(vehicle.asset);
                    currentHealth = vehicle.health;
                    maxHealth = vehicle.asset.health;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "TryDisplayVehicleHP");
            }

            return false;
        }

        private bool TryDisplayDoorHP(UnturnedPlayer player, Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            try
            {
                var doorHinge = hitTransform.GetComponent<InteractableDoorHinge>();

                if (doorHinge != null && doorHinge.door != null)
                {
                    var door = doorHinge.door;
                    var barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(door.transform);

                    if (barricadeDrop != null)
                    {
                        var barricadeData = barricadeDrop.GetServersideData();
                        if (barricadeData != null)
                        {
                            name = GetItemNameWithColor(barricadeDrop.asset);
                            currentHealth = barricadeData.barricade.health;
                            maxHealth = barricadeDrop.asset.health;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "TryDisplayDoorHP");
            }

            return false;
        }

        private void DisplayHP(UnturnedPlayer player, ushort currentHealth, ushort maxHealth, string name)
        {
            if (player == null)
            {
                Rocket.Core.Logging.Logger.LogError("[oldwarHP] Player not found!");
                return;
            }

            try
            {
                EffectManager.sendUIEffect(Configuration.Instance.EffectID, (short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true);

                float percentage = (float)currentHealth / maxHealth;
                int numSpaces = (int)Math.Ceiling(percentage * 57);
                string scrollText = new string(' ', numSpaces);

                string color = percentage > 0.66f ? "#82d057" : percentage > 0.33f ? "#f4b43f" : "#fa3216"; // Green, Yellow, Red

                string currentHealthFormatted = currentHealth >= 1000 ?
                    (currentHealth / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "K" :
                    currentHealth.ToString(CultureInfo.InvariantCulture);

                string maxHealthFormatted = maxHealth >= 1000 ?
                    (maxHealth / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "K" :
                    maxHealth.ToString(CultureInfo.InvariantCulture);

                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true, "Scroll", scrollText);
                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true, "HP", $"<color={color}>{currentHealthFormatted} / {maxHealthFormatted}</color>");
                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true, "NAME", name);
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"Error in DisplayHP: {ex.Message}");
            }
        }

        private string GetItemNameWithColor(ItemAsset itemAsset)
        {
            try
            {
                Color rarityColor = ItemTool.getRarityColorUI(itemAsset.rarity);
                string hexColor = Palette.hex(rarityColor);
                return $"<color={hexColor}>{itemAsset.itemName}</color>";
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "GetItemNameWithColor");
                return itemAsset.itemName;
            }
        }

        private string GetVehicleNameWithColor(VehicleAsset vehicleAsset)
        {
            try
            {
                Color rarityColor = ItemTool.getRarityColorUI(vehicleAsset.rarity);
                string hexColor = Palette.hex(rarityColor);
                return $"<color={hexColor}>{vehicleAsset.vehicleName}</color>";
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "GetVehicleNameWithColor");
                return vehicleAsset.vehicleName;
            }
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
                Rocket.Core.Logging.Logger.LogException(ex, "GetData");
                return null;
            }
        }
    }
}
