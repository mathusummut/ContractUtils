using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;

namespace ContractUtils {
	/// <summary>
	/// Represents a deployed contract
	/// </summary>
	public class DeployedContract {
		/// <summary>
		/// The contract object that can be interacted with
		/// </summary>
		public Contract Contract {
			get;
			private set;
		}

		/// <summary>
		/// The transaction receipt received at the creation of the contract
		/// </summary>
		public TransactionReceipt Receipt {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new deployed contract
		/// </summary>
		/// <param name="contract">The contract object that was created</param>
		/// <param name="receipt">The transaction receipt received at the creation of the contract</param>
		public DeployedContract(Contract contract, TransactionReceipt receipt) {
			Contract = contract;
			Receipt = receipt;
		}

		/// <summary>
		/// Gets the deployed contract instance
		/// </summary>
		/// <param name="contract">The instance whose contract to return</param>
		public static implicit operator Contract(DeployedContract contract) {
			return contract.Contract;
		}
	}
}