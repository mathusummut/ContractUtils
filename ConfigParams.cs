using Nethereum.Hex.HexTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;

namespace ContractUtils {
	/// <summary>
	/// Contains the assembly-wide configuration parameters specified in the accompanying config file
	/// </summary>
	public static class ConfigParams {
		/// <summary>
		/// The default gas value (0x6691b7)
		/// </summary>
		public static HexBigInteger DefaultGas = new HexBigInteger("0x6691b7");
		/// <summary>
		/// The default gas price (0x77359400)
		/// </summary>
		public static HexBigInteger DefaultGasPrice = new HexBigInteger("0x77359400");
		/// <summary>
		/// The default transaction value (zero)
		/// </summary>
		public static HexBigInteger DefaultValue = new HexBigInteger("0x0");
		/// <summary>
		/// The default Ethereum node url
		/// </summary>
		public static string NodeUrl = "http://127.0.0.1:8545";

		/// <summary>
		/// Initializes the class configuration
		/// </summary>
		static ConfigParams() {
			LoadConfigFromFile();
		}

		/// <summary>
		/// Loads the default configuration from contract_defaults.json
		/// </summary>
		public static void LoadConfigFromFile() {
			try {
				LoadConfigFromFile("contract_defaults.json");
			} catch {
				try {
					LoadConfigFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "contract_defaults.json"));
				} catch {
					try {
						LoadConfigFromFile(Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "contract_defaults.json"));
					} catch {
						Console.WriteLine("Failed to load config from contract_defaults.json");
					}
				}
			}
		}

		/// <summary>
		/// Loads the default configuration from the specified JSON file
		/// </summary>
		/// <param name="path">The path to the JSON file</param>
		public static void LoadConfigFromFile(string path) {
			Console.WriteLine("Attempting to load config from " + path + "...");
			string json = null;
			try {
				using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					using (StreamReader reader = new StreamReader(stream))
						json = reader.ReadToEnd();
				}
			} catch {
			}
			if (json == null) {
				path = Path.Combine("scriptcs_bin", path);
				Console.WriteLine("Attempting to load config from " + path + "...");
				using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					using (StreamReader reader = new StreamReader(stream))
						json = reader.ReadToEnd();
				}
			}
			LoadConfig(json);
		}

		/// <summary>
		/// Loads the configuration from the specified JSON object
		/// </summary>
		/// <param name="json">The JSON object string</param>
		private static void LoadConfig(string json) {
			JObject config = (JObject) JsonConvert.DeserializeObject(json);
			DefaultGas = new HexBigInteger(config["default_gas"].ToString().Trim());
			DefaultGasPrice = new HexBigInteger(config["default_gas_price"].ToString().Trim());
			DefaultValue = new HexBigInteger(config["default_value"].ToString().Trim());
			NodeUrl = config["node_url"].ToString().Trim();
		}
	}
}