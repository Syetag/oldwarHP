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

        private static readonly ConcurrentDictionary<CSteamID, PlayerData> playersData = new ConcurrentDictionary<CSteamID, PlayerData>();

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

        private static PlayerData GetData(UnturnedPlayer player)
        {
            if (player == null)
            {
                return null;
            }

            try
            {
                return playersData.GetOrAdd(player.CSteamID, _ => new PlayerData());
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.GetData");
                return null;
            }
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
            if (player == null || (gesture != PlayerGesture.PunchLeft && gesture != PlayerGesture.PunchRight && gesture != PlayerGesture.Point))
            {
                return;
            }

            var playerData = GetData(player);
            if (playerData == null)
            {
                return;
            }

            try
            {
                if (!playerData.InteractSemaphore.Wait(8000))
                {
                    Rocket.Core.Logging.Logger.LogError($"[oldwarHP] Timeout while waiting for semaphore for player {player.DisplayName} ({player.CSteamID}). Possible deadlock in oldwarHP.OnPlayerUpdateGesture.");
                    return;
                }

                if (Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var hit, Configuration.Instance.RaycastDistance, RayMasks.BARRICADE_INTERACT | RayMasks.STRUCTURE_INTERACT | RayMasks.VEHICLE | RayMasks.RESOURCE))
                {
                    TryDisplayHP(player, hit.transform);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.OnPlayerUpdateGesture");
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
                if (TryDisplayEntityHP(hitTransform, out string name, out ushort currentHealth, out ushort maxHealth))
                {
                    DisplayHP(player, currentHealth, maxHealth, name);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.TryDisplayHP");
            }
        }

        private bool TryDisplayEntityHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            return TryDisplayBarricadeHP(hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayStructureHP(hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayVehicleHP(hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayDoorHP(hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayResourceHP(hitTransform, out name, out currentHealth, out maxHealth);
        }

        private bool TryDisplayBarricadeHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
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

                    Rocket.Core.Logging.Logger.LogError("[oldwarHP] Failed to get barricade data in oldwarHP.TryDisplayBarricadeHP!");
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.TryDisplayBarricadeHP");
            }

            return false;
        }

        private bool TryDisplayStructureHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
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

                    Rocket.Core.Logging.Logger.LogError("[oldwarHP] Failed to get structure data in oldwarHP.TryDisplayStructureHP!!");
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.TryDisplayStructureHP");
            }

            return false;
        }

        private bool TryDisplayVehicleHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
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
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.TryDisplayVehicleHP");
            }

            return false;
        }

        private bool TryDisplayDoorHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
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
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.TryDisplayDoorHP");
            }

            return false;
        }

        private bool TryDisplayResourceHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (ResourceManager.tryGetRegion(hitTransform, out byte x, out byte y, out ushort index))
            {
                ResourceSpawnpoint resource = ResourceManager.getResourceSpawnpoint(x, y, index);
                if (resource != null)
                {
                    name = resource.asset.resourceName;
                    currentHealth = (ushort)resource.health;
                    maxHealth = resource.asset.health;
                    return true;
                }
            }

            return false;
        }

        private void DisplayHP(UnturnedPlayer player, ushort currentHealth, ushort maxHealth, string name)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                EffectManager.sendUIEffect(Configuration.Instance.EffectID, (short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true);

                float percentage = (float)currentHealth / maxHealth;
                int numSpaces = (int)Math.Ceiling(percentage * 57);
                string scrollText = new string(' ', numSpaces);

                string color = percentage > 0.66f ? "#82d057" : percentage > 0.33f ? "#f4b43f" : "#fa3216";

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
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.DisplayHP");
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
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.GetItemNameWithColor");
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
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarHP] oldwarHP.GetVehicleNameWithColor");
                return vehicleAsset.vehicleName;
            }
        }
    }
}