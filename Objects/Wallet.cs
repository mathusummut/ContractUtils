using Nethereum.Hex.HexTypes;
using System.Threading.Tasks;

namespace ContractUtils {
	/// <summary>
	/// Represents an Ethereum wallet
	/// </summary>
	public class Wallet {
		/// <summary>
		/// Gets the address of the wallet
		/// </summary>
		public string Address {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new wallet
		/// </summary>
		/// <param name="address">The address of the wallet</param>
		public Wallet(string address) {
			if (address == null)
				address = "0x0";
			Address = address;
		}

		/// <summary>
		/// Gets the balance of the current wallet
		/// </summary>
		public Task<HexBigInteger> GetBalance() {
			return ContractUtil.GetBalance(Address);
		}

		/// <summary>
		/// Sends Ether from the current wallet to the specified address
		/// </summary>
		/// <param name="to">The target address of the Ether</param>
		/// <param name="amount">The amount to send in Wei</param>
		public void Send(string to, HexBigInteger amount) {
			ContractUtil.Web3.TransactionManager.SendTransactionAsync(Address, to, amount);
		}

		/// <summary>
		/// Gets the address of a wallet
		/// </summary>
		/// <param name="wallet">The wallet whose address to obtain</param>
		public static implicit operator string(Wallet wallet) {
			return wallet.Address;
		}

		/// <summary>
		/// Initializes a new wallet from the specified address
		/// </summary>
		/// <param name="address">The address of the wallet</param>
		public static implicit operator Wallet(string address) {
			return new Wallet(address);
		}
	}
}