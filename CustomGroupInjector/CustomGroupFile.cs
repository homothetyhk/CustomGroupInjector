using RandomizerCore.Json;

namespace CustomGroupInjector
{
    public class CustomGroupFile
    {
        public string FileName;
        public JsonType JsonType;
        internal string directoryName;

        internal T Deserialize<T>() where T : class
        {
            string filePath = Path.Combine(CustomGroupInjectorMod.ModDirectory, directoryName, FileName);
            return JsonUtil.DeserializeFromFile<T>(filePath);
        }
    }
}
