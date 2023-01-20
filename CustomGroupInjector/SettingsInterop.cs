using Modding;
using MonoMod.ModInterop;

namespace CustomGroupInjector
{
    internal class SettingsInterop
    {
        [ModImportName("RandoSettingsManager")]
        internal static class RSMImport
        {
            public static Action<Mod, Type, Delegate, Delegate>? RegisterConnectionSimple = null;
            static RSMImport() => typeof(RSMImport).ModInterop();
        }

        public class RSMData
        {
            public RSMData() { }
            public RSMData(GlobalSettings gs)
            {
                Packs = CustomGroupInjectorMod.Packs.Where(gs.IsPackEnabled)
                    .Select(p => p as RemoteCustomGroupPack ?? new RemoteCustomGroupPack((LocalCustomGroupPack)p))
                    .ToList();
                RandomizedPacks = new(Packs.Select(p => p.Name).Where(gs.IsPackRandomized));
                GroupSettings = Packs.SelectMany(p => p.GetGroupNames()).ToDictionary(s => s, gs.GetGroupSetting);
            }

            public HashSet<string> RandomizedPacks;
            public Dictionary<string, int> GroupSettings;
            public List<RemoteCustomGroupPack> Packs;
        }

        internal static void Setup(Mod mod)
        {
            RSMImport.RegisterConnectionSimple?.Invoke(mod, typeof(RSMData), ReceiveSettings, SendSettings);
        }

        internal static void ReceiveSettings(RSMData? data)
        {
            if (data is null)
            {
                MenuHolder.Instance.ToggleAllOff();
            }
            else
            {
                CustomGroupInjectorMod.GS.RandomizedPacks.Clear();
                CustomGroupInjectorMod.GS.GroupSettings.Clear();
                CustomGroupInjectorMod.Packs.Clear();

                CustomGroupInjectorMod.Packs.AddRange(data.Packs);
                CustomGroupInjectorMod.GS.RandomizedPacks.UnionWith(data.RandomizedPacks);
                foreach (var kvp in data.GroupSettings) CustomGroupInjectorMod.GS.GroupSettings.Add(kvp.Key, kvp.Value);

                MenuHolder.Instance.ReconstructMenu();
                MenuHolder.Instance.CreateRestoreLocalPacksButton();
            }
        }

        internal static RSMData? SendSettings()
        {
            if (!CustomGroupInjectorMod.GS.IsActive())
            {
                return null;
            }
            return new(CustomGroupInjectorMod.GS);
        }
    }
}
