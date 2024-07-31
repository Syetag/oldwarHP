using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Globalization;
using UnityEngine;
using static Rocket.Unturned.Events.UnturnedPlayerEvents;

namespace oldwar
{
    public class HPBar : RocketPlugin<Config>
    {
        public static HPBar Instance;

        protected override void Load()
        {
            Instance = this;
            UnturnedPlayerEvents.OnPlayerUpdateGesture += OnPlayerUpdateGesture;
            Rocket.Core.Logging.Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} loaded! Created by SyetaG");
        }

        protected override void Unload()
        {
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= OnPlayerUpdateGesture;

            Rocket.Core.Logging.Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} unloaded! Created by SyetaG");
            Instance = null;
        }

        private void OnPlayerUpdateGesture(UnturnedPlayer player, PlayerGesture gesture)
        {
            if (player == null || (gesture != PlayerGesture.PunchLeft && gesture != PlayerGesture.PunchRight && gesture != PlayerGesture.Point))
            {
                return;
            }

            try
            {
                if (Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var hit, Configuration.Instance.RaycastDistance, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE | RayMasks.RESOURCE))
                {
                    TryDisplayHP(player, hit.transform);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.OnPlayerUpdateGesture");
            }
        }

        private void TryDisplayHP(UnturnedPlayer player, Transform hitTransform)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                if (TryDisplayEntityHP(hitTransform, out string name, out ushort currentHealth, out ushort maxHealth))
                {
                    DisplayHP(player, currentHealth, maxHealth, name);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayHP");
            }
        }

        private bool TryDisplayEntityHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            return TryDisplayBarricadeHP(hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayStructureHP(hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayVehicleHP(hitTransform, out name, out currentHealth, out maxHealth) ||
                   TryDisplayResourceHP(hitTransform, out name, out currentHealth, out maxHealth);
        }

        private bool TryDisplayBarricadeHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (hitTransform == null)
            {
                return false;
            }

            try
            {
                var hitBarricade = DamageTool.getBarricadeRootTransform(hitTransform);
                if (hitBarricade != null)
                {
                    var barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(hitBarricade);
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
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayBarricadeHP");
            }

            return false;
        }

        private bool TryDisplayStructureHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (hitTransform == null)
            {
                return false;
            }

            try
            {
                var hitStructure = DamageTool.getStructureRootTransform(hitTransform);
                if (hitStructure != null)
                {
                    var structureDrop = StructureManager.FindStructureByRootTransform(hitStructure);
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
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayStructureHP");
            }

            return false;
        }

        private bool TryDisplayVehicleHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (hitTransform == null)
            {
                return false;
            }

            try
            {
                var vehicle = DamageTool.getVehicle(hitTransform);
                if (vehicle != null)
                {
                    name = GetVehicleNameWithColor(vehicle.asset);
                    currentHealth = vehicle.health;
                    maxHealth = vehicle.asset.health;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayVehicleHP");
            }

            return false;
        }

        private bool TryDisplayResourceHP(Transform hitTransform, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (hitTransform == null)
            {
                return false;
            }

            try
            {
                var hitResource = DamageTool.getResourceRootTransform(hitTransform);
                if (hitResource != null)
                {
                    if (ResourceManager.tryGetRegion(hitResource, out byte x, out byte y, out ushort index))
                    {
                        ResourceSpawnpoint resource = ResourceManager.getResourceSpawnpoint(x, y, index);
                        if (resource != null)
                        {
                            name = resource.asset.resourceName;
                            currentHealth = resource.health;
                            maxHealth = resource.asset.health;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayResourceHP");
                return false;
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

                string currentHealthFormatted = currentHealth >= 1000 ? (currentHealth / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "K" : currentHealth.ToString(CultureInfo.InvariantCulture);

                string maxHealthFormatted = maxHealth >= 1000 ? (maxHealth / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "K" : maxHealth.ToString(CultureInfo.InvariantCulture);

                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true, "Scroll", scrollText);
                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true, "HP", $"<color={color}>{currentHealthFormatted} / {maxHealthFormatted}</color>");
                EffectManager.sendUIEffectText((short)Configuration.Instance.UIKey, player.Player.channel.owner.transportConnection, true, "NAME", name);
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.DisplayHP");
            }
        }

        private string GetItemNameWithColor(ItemAsset itemAsset)
        {
            if (itemAsset == null)
            {
                return "Unknown";
            }

            try
            {
                Color rarityColor = ItemTool.getRarityColorUI(itemAsset.rarity);
                string hexColor = Palette.hex(rarityColor);
                return $"<color={hexColor}>{itemAsset.itemName}</color>";
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.GetItemNameWithColor");
                return itemAsset.itemName;
            }
        }

        private string GetVehicleNameWithColor(VehicleAsset vehicleAsset)
        {
            if (vehicleAsset == null)
            {
                return "Unknown";
            }

            try
            {
                Color rarityColor = ItemTool.getRarityColorUI(vehicleAsset.rarity);
                string hexColor = Palette.hex(rarityColor);
                return $"<color={hexColor}>{vehicleAsset.vehicleName}</color>";
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.GetVehicleNameWithColor");
                return vehicleAsset.vehicleName;
            }
        }
    }
}