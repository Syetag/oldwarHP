using Rocket.API;

namespace HPBar
{
    public class Config : IRocketPluginConfiguration
    {
        public ushort EffectID;
        public short EffectKey;
        public float RaycastDistance;

        public bool ShowColoredNames;
        public bool ApplyHealthScaling;

        public bool ShowBarricadeHP;
        public bool ShowStructureHP;
        public bool ShowVehicleHP;
        public bool ShowResourceHP;

        public void LoadDefaults()
        {
            EffectID = 14014;
            EffectKey = 14;
            RaycastDistance = 5f;

            ShowColoredNames = true;
            ApplyHealthScaling = true;

            ShowBarricadeHP = true;
            ShowStructureHP = true;
            ShowVehicleHP = true;
            ShowResourceHP = true;
        }
    }
}