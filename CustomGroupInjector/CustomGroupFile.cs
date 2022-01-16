using Newtonsoft.Json;
using RandomizerCore.Json;

namespace CustomGroupInjector
{
    public class CustomGroupFile
    {
        public string FileName;
        public JsonType JsonType;

        internal string directoryName;

        public void LoadIntoSplitGroups(Dictionary<string, Dictionary<int, double>> itemWeights, Dictionary<string, Dictionary<int, double>> locationWeights)
        {
            try
            {
                switch (JsonType)
                {
                    case JsonType.LocationCounts:
                        LoadSplitGroupCounts(locationWeights);
                        break;
                    case JsonType.ItemCounts:
                        LoadSplitGroupCounts(itemWeights);
                        break;
                    case JsonType.LocationWeights:
                        LoadSplitGroupWeights(locationWeights);
                        break;
                    case JsonType.ItemWeights:
                        LoadSplitGroupWeights(itemWeights);
                        break;
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error loading CustomGroupFile {FileName} in subdirectory {directoryName}.", e);
            }
        }

        public void LoadSplitGroupCounts(Dictionary<string, Dictionary<int, double>> weights)
        {
            foreach (KeyValuePair<string, List<string>> kvp in Deserialize<Dictionary<string, List<string>>>())
            {
                if (!CustomGroupInjectorMod.GS.GroupSettings.TryGetValue(kvp.Key, out int splitID))
                {
                    throw new ArgumentException($"Unrecognized group found in file {FileName} in subdirectory {directoryName}");
                }
                else if (splitID < 0) continue;

                foreach (string item in kvp.Value)
                {
                    if (!weights.TryGetValue(item, out Dictionary<int, double> groupWeights))
                    {
                        weights.Add(item, groupWeights = new());
                    }
                    groupWeights.TryGetValue(splitID, out double weight);
                    weight += 1.0;
                    groupWeights[splitID] = weight;
                }
            }
        }


        public void LoadSplitGroupWeights(Dictionary<string, Dictionary<int, double>> weights)
        {
            foreach (KeyValuePair<string, Dictionary<string, double>> kvp in Deserialize<Dictionary<string, Dictionary<string, double>>>())
            {
                if (!weights.TryGetValue(kvp.Key, out Dictionary<int, double> groupWeights))
                {
                    weights[kvp.Key] = groupWeights = new();
                }
                foreach (KeyValuePair<string, double> wp in kvp.Value)
                {
                    if (!CustomGroupInjectorMod.GS.GroupSettings.TryGetValue(wp.Key, out int splitID))
                    {
                        throw new ArgumentException($"Unrecognized group found in file {FileName} in subdirectory {directoryName}");
                    }
                    else if (splitID < 0) continue;

                    groupWeights.TryGetValue(splitID, out double weight);
                    groupWeights[splitID] = weight + wp.Value;
                }
            }
        }

        public T Deserialize<T>() where T : class
        {
            string filePath = Path.Combine(CustomGroupInjectorMod.ModDirectory, directoryName, FileName);
            using StreamReader sr = new(File.OpenRead(filePath));
            using JsonTextReader jtr = new(sr);
            return JsonUtil.Deserialize<T>(jtr);
        }

    }

}
