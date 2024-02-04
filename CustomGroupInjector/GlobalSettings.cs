using Newtonsoft.Json;

namespace CustomGroupInjector
{
    public class GlobalSettings 
    {
        public bool IsActive() => GroupSettings.Values.Any(g => g >= 0);

        public HashSet<string> RandomizedPacks = [];
        public Dictionary<string, int> GroupSettings = [];

        public void SetPackRandomization(string name, bool value)
        {
            if (value)
            {
                RandomizedPacks.Add(name);
            }
            else
            {
                RandomizedPacks.Remove(name);
            }
        }

        public int GetGroupSetting(string name)
        {
            if (!GroupSettings.TryGetValue(name, out int value))
            {
                GroupSettings.Add(name, value = -1);
            }
            return value;
        }

        public void SetGroupSetting(string name, int value)
        {
            GroupSettings[name] = value;
        }

        public bool IsGroupEnabled(string group) => GroupSettings.TryGetValue(group, out int value) && value >= 0;
        public bool IsGroupRandomizable(string group) => GroupSettings.TryGetValue(group, out int value) && 0 <= value && value <= 2;
        public bool IsPackRandomized(string pack) => RandomizedPacks.Contains(pack);
        public bool IsPackEnabled(CustomGroupPack pack) => pack.GetGroupNames().Any(IsGroupEnabled);

        public void CleanData()
        {
            RandomizedPacks.IntersectWith(CustomGroupInjectorMod.Packs.Select(p => p.Name));
            HashSet<string> illegalKeys = new(GroupSettings.Keys);
            illegalKeys.ExceptWith(CustomGroupInjectorMod.Packs.SelectMany(p => p.GetGroupNames()));
            foreach (string s in illegalKeys) GroupSettings.Remove(s);
        }

        public GlobalSettings GetDisplayableSettings()
        {
            HashSet<string> randomizedPacks = [];
            Dictionary<string, int> groupSettings = [];
            foreach (CustomGroupPack pack in CustomGroupInjectorMod.Packs.Where(IsPackEnabled))
            {
                foreach (string s in pack.GetGroupNames())
                {
                    if (GroupSettings.TryGetValue(s, out int value)) groupSettings[s] = value;
                }
                if (RandomizedPacks.Contains(pack.Name)) randomizedPacks.Add(pack.Name);
            }
            return new() { RandomizedPacks = randomizedPacks, GroupSettings = groupSettings };
        }
    }
}
