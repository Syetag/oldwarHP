using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using static Rocket.Unturned.Events.UnturnedPlayerEvents;

namespace HPBar
{
    public class HPBar : RocketPlugin<Config>
    {
        public static HPBar Instance;

        protected override void Load()
        {
            Instance = this;
            UnturnedPlayerEvents.OnPlayerUpdateGesture += OnPlayerUpdateGesture;
            Rocket.Core.Logging.Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} loaded! Created by iche");
        }

        protected override void Unload()
        {
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= OnPlayerUpdateGesture;
            Rocket.Core.Logging.Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} unloaded! Created by iche");
            Instance = null;
        }

        private void OnPlayerUpdateGesture(UnturnedPlayer player, PlayerGesture gesture)
        {
            if (player == null)
            {
                return;
            }

            if (gesture != PlayerGesture.PunchLeft && gesture != PlayerGesture.PunchRight && gesture != PlayerGesture.Point)
            {
                return;
            }

            try
            {
                if (!Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var hit, Configuration.Instance.RaycastDistance, RayMasks.DAMAGE_PHYSICS))
                {
                    return;
                }

                TryDisplayHP(player, hit);
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.OnPlayerUpdateGesture");
            }
        }

        private void TryDisplayHP(UnturnedPlayer player, RaycastHit hit)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                var transformHit = hit.transform;
                if (transformHit == null)
                {
                    return;
                }

                if (TryDisplayEntityHP(transformHit, hit, out string name, out ushort currentHealth, out ushort maxHealth))
                {
                    DisplayHP(player, currentHealth, maxHealth, name);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayHP");
            }
        }

        private bool TryDisplayEntityHP(Transform transformHit, RaycastHit hit, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (transformHit == null)
            {
                return false;
            }

            if (hit.transform == null)
            {
                return false;
            }

            try
            {
                var colliderHit = hit.collider;
                if (colliderHit == null)
                {
                    return false;
                }

                switch (colliderHit.tag)
                {
                    case "Barricade":
                        return Configuration.Instance.ShowBarricadeHP && TryDisplayBarricadeHP(transformHit, out name, out currentHealth, out maxHealth);
                    case "Structure":
                        return Configuration.Instance.ShowStructureHP && TryDisplayStructureHP(transformHit, out name, out currentHealth, out maxHealth);
                    case "Vehicle":
                        return Configuration.Instance.ShowVehicleHP && TryDisplayVehicleHP(transformHit, out name, out currentHealth, out maxHealth);
                    case "Resource":
                        return Configuration.Instance.ShowResourceHP && TryDisplayResourceHP(transformHit, out name, out currentHealth, out maxHealth);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayEntityHP");
                return false;
            }
        }

        private bool TryDisplayBarricadeHP(Transform transformHit, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (transformHit == null)
            {
                return false;
            }

            try
            {
                var hitBarricade = DamageTool.getBarricadeRootTransform(transformHit);
                if (hitBarricade == null)
                {
                    return false;
                }

                var barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(hitBarricade);
                if (barricadeDrop == null)
                {
                    return false;
                }

                var barricadeData = barricadeDrop.GetServersideData();
                if (barricadeData == null)
                {
                    return false;
                }

                name = GetItemNameWithColor(barricadeDrop.asset);
                currentHealth = barricadeData.barricade.health;
                maxHealth = barricadeDrop.asset.health;
                return true;

            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayBarricadeHP");
                return false;
            }
        }

        private bool TryDisplayStructureHP(Transform transformHit, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (transformHit == null)
            {
                return false;
            }

            try
            {
                var hitStructure = DamageTool.getStructureRootTransform(transformHit);
                if (hitStructure == null)
                {
                    return false;
                }

                var structureDrop = StructureManager.FindStructureByRootTransform(hitStructure);
                if (structureDrop == null)
                {
                    return false;
                }

                var structureData = structureDrop.GetServersideData();
                if (structureData == null)
                {
                    return false;
                }

                name = GetItemNameWithColor(structureDrop.asset);
                currentHealth = structureData.structure.health;
                maxHealth = structureDrop.asset.health;
                return true;

            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayStructureHP");
                return false;
            }
        }

        private bool TryDisplayVehicleHP(Transform transformHit, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (transformHit == null)
            {
                return false;
            }

            try
            {
                var vehicle = DamageTool.getVehicle(transformHit);
                if (vehicle == null)
                {
                    return false;
                }

                name = GetItemNameWithColor(vehicle.asset);
                currentHealth = vehicle.health;
                maxHealth = vehicle.asset.health;
                return true;
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayVehicleHP");
                return false;
            }

        }

        private bool TryDisplayResourceHP(Transform transformHit, out string name, out ushort currentHealth, out ushort maxHealth)
        {
            name = string.Empty;
            currentHealth = 0;
            maxHealth = 0;

            if (transformHit == null)
            {
                return false;
            }

            try
            {
                var hitResource = DamageTool.getResourceRootTransform(transformHit);
                if (hitResource == null)
                {
                    return false;
                }

                if (!ResourceManager.tryGetRegion(hitResource, out byte x, out byte y, out ushort index))
                {
                    return false;
                }

                var resource = ResourceManager.getResourceSpawnpoint(x, y, index);
                if (resource == null)
                {
                    return false;
                }

                name = resource.asset.resourceName;
                currentHealth = resource.health;
                maxHealth = resource.asset.health;
                return true;

            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.TryDisplayResourceHP");
                return false;
            }
        }

        private void DisplayHP(UnturnedPlayer player, ushort currentHealth, ushort maxHealth, string name)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                var connection = player.Player.channel.owner.transportConnection;

                var percentage = (float)currentHealth / maxHealth;
                var numSpaces = (int)Math.Ceiling(percentage * 57);
                var scrollText = new string(' ', numSpaces);
                var color = percentage > 0.66f ? "#82d057" : percentage > 0.33f ? "#f4b43f" : "#fa3216";

                var currentHealthFormatted = Configuration.Instance.ApplyHealthScaling ? FormatHealthValue(currentHealth) : currentHealth.ToString(CultureInfo.InvariantCulture);
                var maxHealthFormatted = Configuration.Instance.ApplyHealthScaling ? FormatHealthValue(maxHealth) : maxHealth.ToString(CultureInfo.InvariantCulture);

                if (!Configuration.Instance.ShowColoredNames)
                {
                    name = Regex.Replace(name, "<.*?>", string.Empty);
                }

                EffectManager.sendUIEffect(Configuration.Instance.EffectID, Configuration.Instance.EffectKey, connection, true);
                EffectManager.sendUIEffectText(Configuration.Instance.EffectKey, connection, true, "Scroll", scrollText);
                EffectManager.sendUIEffectText(Configuration.Instance.EffectKey, connection, true, "HP", $"<color={color}>{currentHealthFormatted} / {maxHealthFormatted}</color>");
                EffectManager.sendUIEffectText(Configuration.Instance.EffectKey, connection, true, "NAME", name);
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.DisplayHP");
            }
        }

        private string GetItemNameWithColor(Asset asset)
        {
            if (asset == null)
            {
                return "Unknown";
            }

            try
            {
                if (asset is ItemAsset itemAsset)
                {
                    var rarityColor = ItemTool.getRarityColorUI(itemAsset.rarity);
                    var hexColor = Palette.hex(rarityColor);
                    return $"<color={hexColor}>{itemAsset.itemName}</color>";
                }
                else if (asset is VehicleAsset vehicleAsset)
                {
                    var rarityColor = ItemTool.getRarityColorUI(vehicleAsset.rarity);
                    var hexColor = Palette.hex(rarityColor);
                    return $"<color={hexColor}>{vehicleAsset.vehicleName}</color>";
                }
                else
                {
                    return asset.name;
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.GetItemNameWithColor");
                return asset.name;
            }
        }

        private string FormatHealthValue(ushort health)
        {
            try
            {
                if (health >= 1000)
                {
                    return (health / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "K";
                }
                else
                {
                    return health.ToString(CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "HPBar.FormatHealthValue");
                return health.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}