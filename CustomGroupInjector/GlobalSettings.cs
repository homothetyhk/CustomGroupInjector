namespace CustomGroupInjector
{
    public class GlobalSettings 
    {
        public HashSet<string> RandomizedPacks = new();
        public Dictionary<string, int> GroupSettings = new();

        public GlobalSettings GetDisplayableSettings()
        {
            Dictionary<string, CustomGroupPack> packs = CustomGroupInjectorMod.Packs.ToDictionary(p => p.Name);
            HashSet<string> randomizedPacks = new(RandomizedPacks.Where(p => packs.ContainsKey(p)));
            Dictionary<string, int> groupSettings = new();
            foreach (CustomGroupPack pack in packs.Values)
            {
                foreach (string s in pack.GroupNames)
                {
                    if (GroupSettings.TryGetValue(s, out int value)) groupSettings[s] = value;
                }
            }
            return new() { RandomizedPacks = randomizedPacks, GroupSettings = groupSettings };
        }
    }
}
