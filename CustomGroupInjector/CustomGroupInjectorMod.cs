using MenuChanger;
using Modding;
using Newtonsoft.Json;
using RandomizerMod;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using static RandomizerMod.RC.RequestBuilder;
using JsonUtil = RandomizerCore.Json.JsonUtil;

namespace CustomGroupInjector
{
    public class CustomGroupInjectorMod : Mod, IGlobalSettings<GlobalSettings>
    {
        public static string ModDirectory { get; }
        public static GlobalSettings GS { get; private set; } = new();
        public static readonly List<CustomGroupPack> Packs = new();

        public override void Initialize()
        {
            MenuChangerMod.OnExitMainMenu += MenuHolder.OnExitMenu;
            RandomizerMod.Menu.RandomizerMenuAPI.AddMenuPage(MenuHolder.ConstructMenu, MenuHolder.TryGetMenuButton);
            RequestBuilder.OnUpdate.Subscribe(-500f, ApplyActiveGroups);
            LoadFiles();
            RandomizerMod.Logging.SettingsLog.AfterLogSettings += LogSettings;
        }

        public override string GetVersion()
        {
            return "1.0.0";
        }

        public static void LoadFiles()
        {
            DirectoryInfo main = new(ModDirectory);

            foreach (DirectoryInfo di in main.EnumerateDirectories())
            {
                Dictionary<string, FileInfo> jsons = di.GetFiles("*.json").ToDictionary(fi => fi.Name.Substring(0, fi.Name.Length - 5).ToLower());
                if (jsons.TryGetValue("pack", out FileInfo fi))
                {
                    try
                    {
                        using StreamReader sr = new(fi.OpenRead());
                        using JsonTextReader jtr = new(sr);
                        CustomGroupPack pack = JsonUtil.Deserialize<CustomGroupPack>(jtr);
                        foreach (CustomGroupFile cgf in pack.Files) cgf.directoryName = di.Name;
                        Packs.Add(pack);
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogError($"Error deserializing pack.json in subdirectory {di.Name}:\n{e}");
                    }
                }
            }
            Packs.Sort((p, q) => p.Name.CompareTo(q.Name));
        }

        public static void ApplyActiveGroups(RequestBuilder rb)
        {
            Dictionary<int, ItemGroupBuilder> splitGroups = new();
            splitGroups.Add(0, rb.MainItemGroup);
            foreach (ItemGroupBuilder igb in rb.EnumerateItemGroups())
            {
                if (igb.label.StartsWith(RBConsts.SplitGroupPrefix) && int.TryParse(igb.label.Substring(RBConsts.SplitGroupPrefix.Length), out int splitGroupIndex) && splitGroupIndex > 0)
                {
                    splitGroups[splitGroupIndex] = igb;
                }
            }
            foreach (CustomGroupPack pack in Packs)
            {
                if (GS.RandomizedPacks.Contains(pack.Name))
                {
                    foreach (string s in pack.GroupNames)
                    {
                        if (GS.GroupSettings.TryGetValue(s, out int setting) && (setting < 0 || setting > 2)) continue;
                        GS.GroupSettings[s] = rb.rng.Next(3);
                    }
                }

                foreach (string s in pack.GroupNames)
                {
                    if (GS.GroupSettings.TryGetValue(s, out int splitID) && splitID > 0 && !splitGroups.ContainsKey(splitID))
                    {
                        splitGroups[splitID] = rb.MainItemStage.AddItemGroup(RBConsts.SplitGroupPrefix + splitID);
                    }
                }
            }

            Dictionary<string, Dictionary<int, double>> itemWeights = new();
            Dictionary<string, Dictionary<int, double>> locationWeights = new();
            foreach (CustomGroupPack pack in Packs)
            {
                foreach (CustomGroupFile cgf in pack.Files) cgf.LoadIntoSplitGroups(itemWeights, locationWeights);
            }
            foreach (PoolDef def in Data.Pools)
            {
                if (rb.gs.SplitGroupSettings.TryGetValue(def, out int splitID))
                {
                    if (!splitGroups.ContainsKey(splitID))
                    {
                        splitGroups.Add(splitID, rb.MainItemStage.AddItemGroup(RBConsts.SplitGroupPrefix + splitID));
                    }
                    foreach (string s in def.IncludeItems)
                    {
                        if (!itemWeights.TryGetValue(s, out Dictionary<int, double> groupWeights)) itemWeights.Add(s, groupWeights = new());
                        groupWeights.TryGetValue(splitID, out double weight);
                        groupWeights[splitID] = weight + 1.0;
                    }
                    foreach (string s in def.IncludeLocations)
                    {
                        if (!locationWeights.TryGetValue(s, out Dictionary<int, double> groupWeights)) locationWeights.Add(s, groupWeights = new());
                        groupWeights.TryGetValue(splitID, out double weight);
                        groupWeights[splitID] = weight + 1.0;
                    }
                }
            }

            CDFWeightedArray<ItemGroupBuilder> ToCDF(Dictionary<int, double> weights)
            {
                ItemGroupBuilder[] values = new ItemGroupBuilder[weights.Count];
                double[] cdf = new double[weights.Count];

                int i = -1;
                double previous = 0.0;
                double total = weights.Values.Sum();

                foreach (KeyValuePair<int, double> kvp in weights)
                {
                    i++;
                    values[i] = splitGroups[kvp.Key];
                    previous = cdf[i] = previous + (kvp.Value / total);
                }
                return new(values, cdf);
            }

            Dictionary<string, CDFWeightedArray<ItemGroupBuilder>> itemGroups = new();
            foreach (KeyValuePair<string, Dictionary<int, double>> kvp in itemWeights)
            {
                itemGroups.Add(kvp.Key, ToCDF(kvp.Value));
            }
            Dictionary<string, CDFWeightedArray<ItemGroupBuilder>> locationGroups = new();
            foreach (var kvp in locationWeights)
            {
                locationGroups.Add(kvp.Key, ToCDF(kvp.Value));
            }

            bool TryGetSplitGroup(RequestBuilder rb, string item, ElementType type, out GroupBuilder gb)
            {
                if (type == ElementType.Unknown)
                {
                    if (locationGroups.ContainsKey(item)) type = ElementType.Location;
                    else if (itemGroups.ContainsKey(item)) type = ElementType.Item;
                }
                if (type == ElementType.Item)
                {
                    if (itemGroups.TryGetValue(item, out CDFWeightedArray<ItemGroupBuilder> arr))
                    {
                        gb = arr.Next(rb.rng);
                        return true;
                    }
                }
                if (type == ElementType.Location)
                {
                    if (locationGroups.TryGetValue(item, out CDFWeightedArray<ItemGroupBuilder> arr))
                    {
                        gb = arr.Next(rb.rng);
                        return true;
                    }
                }
                gb = null;
                return false;
            }
            rb.OnGetGroupFor.Subscribe(-1f, TryGetSplitGroup);
        }

        private static void LogSettings(RandomizerMod.Logging.LogArguments arg1, TextWriter tw)
        {
            tw.WriteLine("Logging CustomGroupInjector settings:");
            using JsonTextWriter jtw = new(tw) { CloseOutput = false, };
            RandomizerMod.RandomizerData.JsonUtil._js.Serialize(jtw, GS);
            tw.WriteLine();
        }

        void IGlobalSettings<GlobalSettings>.OnLoadGlobal(GlobalSettings s)
        {
            GS = s ?? new();
        }

        GlobalSettings IGlobalSettings<GlobalSettings>.OnSaveGlobal()
        {
            return GS;
        }

        static CustomGroupInjectorMod()
        {
            ModDirectory = Path.GetDirectoryName(typeof(CustomGroupInjectorMod).Assembly.Location);
        }
    }
}
