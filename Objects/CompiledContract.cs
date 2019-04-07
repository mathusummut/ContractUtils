namespace ContractUtils {
	/// <summary>
	/// Holds the data that defines a compiled contract
	/// </summary>
	public class CompiledContract {
		/// <summary>
		/// The ABI of the contract
		/// </summary>
		public string Abi {
			get;
			private set;
		}

		/// <summary>
		/// The compiled bytecode of the contract
		/// </summary>
		public string ByteCode {
			get;
			private set;
		}

		/// <summary>
		/// Initalizes a new compiled contract
		/// </summary>
		/// <param name="abi">The ABI of the contract</param>
		/// <param name="bytecode">The compiled bytecode of the contract</param>
		public CompiledContract(string abi, string bytecode) {
			Abi = abi;
			ByteCode = bytecode;
		}
	}
}