namespace CustomGroupInjector
{
    public abstract class CustomGroupPack
    {
        public string Name;
        public abstract IEnumerable<string> GetGroupNames();
        public abstract void LoadIntoSplitGroups(Dictionary<string, Dictionary<int, double>> itemWeights, Dictionary<string, Dictionary<int, double>> locationWeights);

        protected void ApplySplitGroupCounts(Dictionary<string, Dictionary<int, double>> receiver, Dictionary<string, List<string>> source)
        {
            foreach (KeyValuePair<string, List<string>> kvp in source)
            {
                if (!CustomGroupInjectorMod.GS.GroupSettings.TryGetValue(kvp.Key, out int splitID))
                {
                    throw new ArgumentException($"Unrecognized group {kvp.Key} found in counts file.");
                }
                else if (splitID < 0) continue;

                foreach (string item in kvp.Value)
                {
                    if (!receiver.TryGetValue(item, out Dictionary<int, double> groupWeights))
                    {
                        receiver.Add(item, groupWeights = []);
                    }
                    groupWeights.TryGetValue(splitID, out double weight);
                    weight += 1.0;
                    groupWeights[splitID] = weight;
                }
            }
        }

        protected void ApplySplitGroupWeights(Dictionary<string, Dictionary<int, double>> receiver, Dictionary<string, Dictionary<string, double>> source)
        {
            foreach (KeyValuePair<string, Dictionary<string, double>> kvp in source)
            {
                if (!receiver.TryGetValue(kvp.Key, out Dictionary<int, double> groupWeights))
                {
                    receiver[kvp.Key] = groupWeights = [];
                }
                foreach (KeyValuePair<string, double> wp in kvp.Value)
                {
                    if (!CustomGroupInjectorMod.GS.GroupSettings.TryGetValue(wp.Key, out int splitID))
                    {
                        throw new ArgumentException($"Unrecognized group {wp.Key} found in weights file.");
                    }
                    else if (splitID < 0) continue;

                    groupWeights.TryGetValue(splitID, out double weight);
                    groupWeights[splitID] = weight + wp.Value;
                }
            }
        }
    }

    public class LocalCustomGroupPack : CustomGroupPack
    {
        public List<CustomGroupFile> Files;
        public List<string> GroupNames;

        public override IEnumerable<string> GetGroupNames() => GroupNames;
        public override void LoadIntoSplitGroups(Dictionary<string, Dictionary<int, double>> itemWeights, Dictionary<string, Dictionary<int, double>> locationWeights)
        {
            foreach (CustomGroupFile file in Files)
            {
                try
                {
                    switch (file.JsonType)
                    {
                        case JsonType.LocationCounts:
                            ApplySplitGroupCounts(locationWeights, file.Deserialize<Dictionary<string, List<string>>>());
                            break;
                        case JsonType.LocationWeights:
                            ApplySplitGroupWeights(locationWeights, file.Deserialize<Dictionary<string, Dictionary<string, double>>>());
                            break;
                        case JsonType.ItemCounts:
                            ApplySplitGroupCounts(itemWeights, file.Deserialize<Dictionary<string, List<string>>>());
                            break;
                        case JsonType.ItemWeights:
                            ApplySplitGroupWeights(itemWeights, file.Deserialize<Dictionary<string, Dictionary<string, double>>>());
                            break;
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error loading CustomGroupFile {file.FileName} of pack {Name}.", e);
                }
            }
        }
    }

    public class RemoteCustomGroupPack : CustomGroupPack
    {
        public List<Dictionary<string, Dictionary<string, double>>> ItemWeightFiles = [];
        public List<Dictionary<string, Dictionary<string, double>>> LocationWeightFiles = [];
        public List<Dictionary<string, List<string>>> ItemCountFiles = [];
        public List<Dictionary<string, List<string>>> LocationCountFiles = [];
        public List<string> GroupNames;
        public List<(string, JsonType, int)> Files = [];

        public RemoteCustomGroupPack() { }

        public RemoteCustomGroupPack(LocalCustomGroupPack pack)
        {
            base.Name = pack.Name;
            GroupNames = pack.GroupNames;
            foreach (CustomGroupFile cgf in pack.Files)
            {
                switch (cgf.JsonType)
                {
                    case JsonType.ItemCounts:
                        Files.Add((cgf.FileName, cgf.JsonType, ItemCountFiles.Count));
                        ItemCountFiles.Add(cgf.Deserialize<Dictionary<string, List<string>>>());
                        break;
                    case JsonType.LocationCounts:
                        Files.Add((cgf.FileName, cgf.JsonType, LocationCountFiles.Count));
                        LocationCountFiles.Add(cgf.Deserialize<Dictionary<string, List<string>>>());
                        break;
                    case JsonType.LocationWeights:
                        Files.Add((cgf.FileName, cgf.JsonType, LocationWeightFiles.Count));
                        LocationWeightFiles.Add(cgf.Deserialize<Dictionary<string, Dictionary<string, double>>>());
                        break;
                    case JsonType.ItemWeights:
                        Files.Add((cgf.FileName, cgf.JsonType, ItemWeightFiles.Count));
                        ItemWeightFiles.Add(cgf.Deserialize<Dictionary<string, Dictionary<string, double>>>());
                        break;
                }
            }
        }

        public override IEnumerable<string> GetGroupNames()
        {
            return GroupNames;
        }

        public override void LoadIntoSplitGroups(Dictionary<string, Dictionary<int, double>> itemWeights, Dictionary<string, Dictionary<int, double>> locationWeights)
        {
            foreach ((string name, JsonType type, int index) in Files)
            {
                try
                {
                    switch (type)
                    {
                        case JsonType.ItemWeights:
                            ApplySplitGroupWeights(itemWeights, ItemWeightFiles[index]);
                            break;
                        case JsonType.LocationWeights:
                            ApplySplitGroupWeights(locationWeights, LocationWeightFiles[index]);
                            break;
                        case JsonType.LocationCounts:
                            ApplySplitGroupCounts(locationWeights, LocationCountFiles[index]);
                            break;
                        case JsonType.ItemCounts:
                            ApplySplitGroupCounts(itemWeights, ItemCountFiles[index]);
                            break;
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error loading CustomGroupFile {name} of pack {Name}.", e);
                }
            }
        }
    }

}
