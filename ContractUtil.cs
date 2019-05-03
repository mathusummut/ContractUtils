using Nethereum.Contracts;
using Nethereum.Geth;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace ContractUtils {
	/// <summary>
	/// Contains useful functions to deploy and manage Ethereum contracts and function calls using Nethereum
	/// </summary>
	public static class ContractUtil {
		private static DateTime utcZero = new DateTime(1970, 1, 1, 0, 0, 0);
		/// <summary>
		/// The Web3 object used to interface with 
		/// </summary>
		public static readonly Web3 Web3 = new Web3(ConfigParams.NodeUrl);
		/// <summary>
		/// The Ethereum node interface
		/// </summary>
		public static readonly Web3Geth Node = new Web3Geth(ConfigParams.NodeUrl);
		/// <summary>
		/// Contains conversion functions between common Ethereum units
		/// </summary>
		public static UnitConversion Convert = UnitConversion.Convert;

		/// <summary>
		/// Gets the current UTC time stamp
		/// </summary>
		public static long Utc {
			get {
				return ToUtc(DateTime.UtcNow);
			}
		}

		/// <summary>
		/// Initializes Web3 configuration
		/// </summary>
		static ContractUtil() {
			Web3.TransactionManager.DefaultGas = ConfigParams.DefaultGas;
			Web3.TransactionManager.DefaultGasPrice = ConfigParams.DefaultGasPrice;
		}

		/// <summary>
		/// Gets the UTC timestamp of the specified DateTime
		/// </summary>
		/// <param name="date">The date whose timestamp to obtain</param>
		private static long ToUtc(this DateTime date) {
			return (long) (date - utcZero).TotalSeconds;
		}

		/// <summary>
		/// Gets the specified Ganache-Cli wallet address
		/// </summary>
		/// <param name="path">The path of the file containing the Ganache-Cli output</param>
		/// <param name="walletIndex">The wallet index between 0 and 9 (the default is 0)</param>
		/// <param name="timeoutSeconds">The timeout in seconds for the file to be found (default is 120)</param>
		public static Wallet GetWalletAddressFromGanacheLog(string path, int walletIndex = 0, int timeoutSeconds = 120) {
			string content, searchString;
			int index, end;
			for (int i = 0; i < timeoutSeconds; i++) {
				using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					using (StreamReader reader = new StreamReader(stream))
						content = reader.ReadToEnd();
				}
				searchString = "(" + walletIndex + ")";
				index = content.IndexOf(searchString);
				if (index == -1)
					Thread.Sleep(1000);
				else {
					index += searchString.Length + 1;
					end = index;
					while ((end < content.Length) && !(string.IsNullOrWhiteSpace(content.Substring(end, 1))))
						end++;
					return new Wallet(content.Substring(index, end - index).Trim());
				}
			}
			throw new Exception("Wallet address could not be loaded, operation timeout exceeded");
		}

		/// <summary>
		/// Unlocks the specified wallet using the given password
		/// </summary>
		/// <param name="wallet">The address of the wallet to unlock</param>
		/// <param name="password">The password of the wallet</param>
		/// <param name="timeoutSeconds">The operation timeout in seconds</param>
		public static Task<bool> UnlockWallet(this Wallet wallet, string password, int timeoutSeconds = 120) {
			return Web3.Personal.UnlockAccount.SendRequestAsync(wallet.Address, password, new HexBigInteger(new BigInteger(timeoutSeconds)));
		}

		/// <summary>
		/// Gets the transaction receipt from the specified transaction
		/// </summary>
		/// <param name="transactionHash">The hash of the transaction whose receipt to obtain</param>
		/// <param name="retryCount">The maximum number of retries if an error occurs</param>
		public static async Task<TransactionReceipt> GetReceipt(string transactionHash, int retryCount = 10) {
			TransactionReceipt receipt = null;
			for (int i = 0; i < retryCount && receipt == null; i++)
				receipt = await Web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
			return receipt;
		}

		private static object[] PrepareParams(object[] items) {
			if (items == null || items.Length == 0)
				return null;
			object[] fixedItems = new object[items.Length];
			object current;
			for (int i = 0; i < items.Length; i++) {
				current = items[i];
				if (current == null)
					fixedItems[i] = null;
				else if (current is Contract)
					fixedItems[i] = ((Contract) current).Address;
				else if (current is DeployedContract)
					fixedItems[i] = ((DeployedContract) current).Contract.Address;
				else if (current is Wallet)
					fixedItems[i] = ((Wallet) current).Address;
				else if (current is TransactionReceipt)
					fixedItems[i] = ((TransactionReceipt) current).TransactionHash;
				else
					fixedItems[i] = items[i];
			}
			return fixedItems;
		}

		/// <summary>
		/// Deploys the specified contract into the current node specified in the config file
		/// </summary>
		/// <param name="abi">The JSON ABI of the contract</param>
		/// <param name="byteCode">The compiled bytecode of the contract</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="gas">The gas value</param>
		/// <param name="gasPrice">The gas price</param>
		/// <param name="value">The initial value of the contract</param>
		/// <param name="retryCount">The maximum number of retries if an error occurs</param>
		/// <param name="constructorParams">The values to pass to the contract constructor</param>
		public static async Task<DeployedContract> DeployContract(string abi, string byteCode, Wallet wallet, HexBigInteger gas, HexBigInteger gasPrice, HexBigInteger value, int retryCount = 10, params object[] constructorParams) {
			string transactionHash = await Web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, wallet.Address, gas, gasPrice, value, PrepareParams(constructorParams));
			TransactionReceipt receipt = await GetReceipt(transactionHash, retryCount);
			return new DeployedContract(Web3.Eth.GetContract(abi, receipt.ContractAddress), receipt);
		}

		/// <summary>
		/// Deploys the specified contract into the current node specified in the config file
		/// </summary>
		/// <param name="contract">The compiled contract</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="gas">The gas value</param>
		/// <param name="gasPrice">The gas price</param>
		/// <param name="value">The initial value of the contract</param>
		/// <param name="retryCount">The maximum number of retries if an error occurs</param>
		/// <param name="constructorParams">The values to pass to the contract constructor</param>
		public static Task<DeployedContract> DeployContract(this CompiledContract contract, Wallet wallet, HexBigInteger gas, HexBigInteger gasPrice, HexBigInteger value, int retryCount = 10, params object[] constructorParams) {
			return DeployContract(contract.Abi, contract.ByteCode, wallet, gas, gasPrice, value, retryCount, constructorParams);
		}

		/// <summary>
		/// Initalizes a compiled contract from the JSON output of the smart contract compiled by Truffle
		/// </summary>
		/// <param name="jsonPath">The path to the compiled JSON file</param>
		public static CompiledContract GetCompiledContract(string jsonPath) {
			JObject json = (JObject) JsonConvert.DeserializeObject(File.ReadAllText(jsonPath));
			return new CompiledContract(json["abi"].ToString(), json["bytecode"].ToString());
		}

		/// <summary>
		/// Deploys the specified contract into the current node specified in the config file
		/// </summary>
		/// <param name="jsonPath">The path to the JSON ABI of the contract that is generated by Truffle</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="gas">The gas value</param>
		/// <param name="gasPrice">The gas price</param>
		/// <param name="value">The initial value of the contract</param>
		/// <param name="retryCount">The maximum number of retries if an error occurs</param>
		/// <param name="constructorParams">The values to pass to the contract constructor</param>
		public static Task<DeployedContract> DeployContract(string jsonPath, Wallet wallet, HexBigInteger gas, HexBigInteger gasPrice, HexBigInteger value, int retryCount = 10, params object[] constructorParams) {
			return DeployContract(GetCompiledContract(jsonPath), wallet, gas, gasPrice, value, retryCount, constructorParams);
		}

		/// <summary>
		/// Deploys the specified contract using default values into the current node specified in the config file
		/// </summary>
		/// <param name="jsonPath">The path to the JSON ABI of the contract that is generated by Truffle</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="constructorParams">The values to pass to the contract constructor</param>
		public static Task<DeployedContract> DeployContract(string jsonPath, Wallet wallet, params object[] constructorParams) {
			return DeployContract(jsonPath, wallet, ConfigParams.DefaultGas, ConfigParams.DefaultGasPrice, ConfigParams.DefaultValue, 10, constructorParams);
		}

		/// <summary>
		/// Deploys the specified contract using default values into the current node specified in the config file
		/// </summary>
		/// <param name="contract">The compiled contract</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="constructorParams">The values to pass to the contract constructor</param>
		public static Task<DeployedContract> DeployContract(this CompiledContract contract, Wallet wallet, params object[] constructorParams) {
			return DeployContract(contract, wallet, ConfigParams.DefaultGas, ConfigParams.DefaultGasPrice, ConfigParams.DefaultValue, 10, constructorParams);
		}

		/// <summary>
		/// Gets the contract instance from the template ABI and the address at which it was deployed
		/// </summary>
		/// <param name="template">The contract template</param>
		/// <param name="address">The address at which the contract was deployed</param>
		public static Contract GetContractFromAddress(this CompiledContract template, string address) {
			return GetContractFromAddress(template.Abi, address);
		}

		/// <summary>
		/// Gets the contract instance from the ABI and the address at which it was deployed
		/// </summary>
		/// <param name="abi">The contract ABI</param>
		/// <param name="address">The address at which the contract was deployed</param>
		public static Contract GetContractFromAddress(string abi, string address) {
			return Web3.Eth.GetContract(abi, address);
		}

		/// <summary>
		/// Calls the specified function as a read operation and returns a generic object
		/// </summary>
		/// <param name="contract">The contract instance on which to invoke the function</param>
		/// <param name="functionName">The name of the function to invoke</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<object> CallRead(this Contract contract, string functionName, params object[] functionParams) {
			return CallRead<object>(contract, functionName, functionParams);
		}

		/// <summary>
		/// Calls the specified function as a read operation
		/// </summary>
		/// <typeparam name="T">The return type of the function</typeparam>
		/// <param name="contract">The contract instance on which to invoke the function</param>
		/// <param name="functionName">The name of the function to invoke</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<T> CallRead<T>(this Contract contract, string functionName, params object[] functionParams) {
			return CallRead<T>(contract.GetFunction(functionName), functionParams);
		}

		/// <summary>
		/// Calls the specified function as a read operation and returns a generic object
		/// </summary>
		/// <param name="function">The isntance of the function to invoke, which is the object returned by contractInstance.GetFunction("functionName")</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<object> CallRead(this Function function, params object[] functionParams) {
			return CallRead<object>(function, functionParams);
		}

		/// <summary>
		/// Calls the specified function as a read operation
		/// </summary>
		/// <typeparam name="T">The return type of the function</typeparam>
		/// <param name="function">The isntance of the function to invoke, which is the object returned by contractInstance.GetFunction("functionName")</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<T> CallRead<T>(this Function function, params object[] functionParams) {
			return function.CallAsync<T>(PrepareParams(functionParams));
		}

		/// <summary>
		/// Calls the specified function as a write operation
		/// </summary>
		/// <param name="contract">The contract instance on which to invoke the function</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="gas">The gas value</param>
		/// <param name="gasPrice">The gas price</param>
		/// <param name="value">The initial value of the contract</param>
		/// <param name="functionName">The name of the function to invoke</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<string> CallWrite(this Contract contract, string functionName, Wallet wallet, HexBigInteger gas, HexBigInteger gasPrice, HexBigInteger value, params object[] functionParams) {
			return CallWrite(contract.GetFunction(functionName), wallet, gas, gasPrice, value, functionParams);
		}

		/// <summary>
		/// Calls the specified function as a read operation
		/// </summary>
		/// <param name="function">The instance of the function to invoke, which is the object returned by contractInstance.GetFunction("functionName")</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="gas">The gas value</param>
		/// <param name="gasPrice">The gas price</param>
		/// <param name="value">The initial value of the contract</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<string> CallWrite(this Function function, Wallet wallet, HexBigInteger gas, HexBigInteger gasPrice, HexBigInteger value, params object[] functionParams) {
			return function.SendTransactionAsync(wallet.Address, gas, gasPrice, value, PrepareParams(functionParams));
		}

		/// <summary>
		/// Calls the specified function as a write operation using default config
		/// </summary>
		/// <param name="contract">The contract instance on which to invoke the function</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="functionName">The name of the function to invoke</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<string> CallWrite(this Contract contract, string functionName, Wallet wallet, params object[] functionParams) {
			return CallWrite(contract, functionName, wallet, ConfigParams.DefaultGas, ConfigParams.DefaultGasPrice, ConfigParams.DefaultValue, functionParams);
		}

		/// <summary>
		/// Calls the specified function as a read operation using default config
		/// </summary>
		/// <param name="function">The instance of the function to invoke, which is the object returned by contractInstance.GetFunction("functionName")</param>
		/// <param name="wallet">The wallet address of the contract owner</param>
		/// <param name="functionParams">The parameters to pass to the function</param>
		public static Task<string> CallWrite(this Function function, Wallet wallet, params object[] functionParams) {
			return CallWrite(function, wallet, ConfigParams.DefaultGas, ConfigParams.DefaultGasPrice, ConfigParams.DefaultValue, functionParams);
		}

		/// <summary>
		/// Gets the balance of the specified contract
		/// </summary>
		/// <param name="contract">The contract whose balance to obtain</param>
		public static Task<HexBigInteger> GetBalance(this Contract contract) {
			return GetBalance(contract.Address);
		}

		/// <summary>
		/// Gets the balance of the specified wallet or contract address
		/// </summary>
		/// <param name="address">The address of the wallet or contract</param>
		public static Task<HexBigInteger> GetBalance(string address) {
			return Web3.Eth.GetBalance.SendRequestAsync(address);
		}

		/// <summary>
		/// Sends a signal to the associated node to start mining
		/// </summary>
		public static Task<bool> StartMining() {
			return Node.Miner.Start.SendRequestAsync(6);
		}

		/// <summary>
		/// Sends a signal to the associated node to stop mining
		/// </summary>
		public static Task<bool> StopMining() {
			return Node.Miner.Stop.SendRequestAsync();
		}

		/// <summary>
		/// Should only be used for live demo scripting purposes
		/// Waits for a task to finish
		/// </summary>
		/// <param name="task">The task to await</param>
		public static void Await(this Task task) {
			task.Wait();
		}

		/// <summary>
		/// Should only be used for live demo scripting purposes.
		/// Waits for a task to finish and returns the result
		/// </summary>
		/// <param name="task">The task to await</param>
		public static T Await<T>(this Task<T> task) {
			task.Wait();
			return task.Result;
		}
	}
}