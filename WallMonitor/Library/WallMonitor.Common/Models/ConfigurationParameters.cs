using WallMonitor.Common.Sdk;

namespace WallMonitor.Common.Models
{
    public class ConfigurationParameters : IConfigurationParameters
	{
        public HashSet<IConfigurationParameter> Parameters { get; set; }

        public ConfigurationParameters()
		{
			Parameters = new HashSet<IConfigurationParameter>();
		}

        public ConfigurationParameters(IEnumerable<IConfigurationParameter> parameters)
        {
            Parameters = new HashSet<IConfigurationParameter>();
            foreach (var p in parameters)
                Parameters.Add(p);
        }

		public bool Contains(string parameterName) => Parameters.Any(x => x.Name.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase));
        
        public bool Any() => Parameters.Any();

        /// <summary>
		/// Get value from config
		/// </summary>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		public dynamic? Get(string parameterName)
		{
			return Get(parameterName, default);
		}

        /// <summary>
        /// Get value from config
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public dynamic? Get(string parameterName, dynamic? defaultValue)
        {
            var param = Parameters.FirstOrDefault(x => x.Name.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase));
            if (param != null)
                return param.Value;
            return defaultValue;
        }

        /// <summary>
        /// Get value from config
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        public T Get<T>(string parameterName)
        {
            var val = Get<T?>(parameterName, default);
            if (val == null)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)string.Empty;
                return Activator.CreateInstance<T>();
            }

            return val;
        }

		/// <summary>
		/// Get value from config
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parameterName"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public T? Get<T>(string parameterName, T? defaultValue)
        {
			var param = Parameters.FirstOrDefault(x => x.Name.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase));
			if (param != null && param.Value != null && param.Value.GetType() == typeof(T))
				return (T)param.Value;
			else if (param != null && typeof(T).IsEnum)
            {
				if (Enum.TryParse(typeof(T), param.Value.ToString(), ignoreCase: true, out var result))
					return (T)Convert.ChangeType(result, typeof(T));
            }
			else if(param != null && param.Value != null)
			{
				switch (typeof(T).FullName)
				{
					case "System.Int16":
						short.TryParse(param.Value.ToString(), out var int16);
						return (T)Convert.ChangeType(int16, typeof(T));
					case "System.UInt16":
						ushort.TryParse(param.Value.ToString(), out var uint16);
						return (T)Convert.ChangeType(uint16, typeof(T));
					case "System.Int32":
						int.TryParse(param.Value.ToString(), out var int32);
						return (T)Convert.ChangeType(int32, typeof(T));
					case "System.UInt32":
						uint.TryParse(param.Value.ToString(), out var uint32);
						return (T)Convert.ChangeType(uint32, typeof(T));
					case "System.Int64":
						long.TryParse(param.Value.ToString(), out var int64);
						return (T)Convert.ChangeType(int64, typeof(T));
					case "System.UInt64":
						ulong.TryParse(param.Value.ToString(), out var uint64);
						return (T)Convert.ChangeType(uint64, typeof(T));
					case "System.Double":
						double.TryParse(param.Value.ToString(), out var intDouble);
						return (T)Convert.ChangeType(intDouble, typeof(T));
					case "System.Decimal":
						decimal.TryParse(param.Value.ToString(), out var intDecimal);
						return (T)Convert.ChangeType(intDecimal, typeof(T));
					case "System.Byte":
						byte.TryParse(param.Value.ToString(), out var byteVal);
						return (T)Convert.ChangeType(byteVal, typeof(T));
					case "System.Single":
						float.TryParse(param.Value.ToString(), out var floatVal);
						return (T)Convert.ChangeType(floatVal, typeof(T));
					case "System.Guid":
						Guid.TryParse(param.Value.ToString(), out var guidVal);
						return (T)Convert.ChangeType(guidVal, typeof(T));
					case "System.Boolean":
                        var boolean = param.Value.ToString()?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true || param.Value.ToString() == "1";
                        return (T)Convert.ChangeType(boolean, typeof(T));
					case "System.String":
						return (T?)Convert.ChangeType(param.Value.ToString(), typeof(T));
				}
			}
			return defaultValue;
		}
	}
}
