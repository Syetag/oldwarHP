using Rocket.API;
using SDG.Unturned;

namespace oldwar
{
    public class Config : IRocketPluginConfiguration
    {
        public ushort EffectID;
        public ushort UIKey;
        public float RaycastDistance;

        public void LoadDefaults()
        {
            EffectID = 14014;
            UIKey = 14;
            RaycastDistance = 5f;
        }
    }
}