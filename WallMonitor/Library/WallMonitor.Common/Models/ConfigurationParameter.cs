using WallMonitor.Common.Sdk;

namespace WallMonitor.Common.Models
{
    public class ConfigurationParameter : IConfigurationParameter
    {
        public string Name { get; set; }

        public object? Value { get; set; }

        public ConfigurationParameter(string name, object? value)
        {
            Name = name;
            Value = value;
        }
    }
}
