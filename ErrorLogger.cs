using System.IO;
using System.Text;

namespace System.Diagnostics {
	/// <summary>
	/// Represents an error logger. To use your own, implement IErrorLogger and set System.Diagnostics.ErrorHandler.Logger to a new instance of your logger
	/// </summary>
	public interface IErrorLogger {
		/// <summary>
		/// Handles an exception that was thrown
		/// </summary>
		/// <param name="ex">The first exception that was thrown</param>
		/// <param name="errorLog">A full description of the error that occurred (pre-formatted for logging)</param>
		void LogException(Exception ex, string errorLog);
	}

	/// <summary>
	/// The default error logger. To use your own, inherit this class or implement IErrorLogger and override LogException(),
	/// and set System.Diagnostics.ErrorHandler.Logger to a new instance of your logger
	/// </summary>
	public class ErrorLogger : IErrorLogger {
		private int maxLogSize = 1048576;

		/// <summary>
		/// Gets or sets the maximum log file size in kibibytes
		/// </summary>
		public int MaxLogSizeKiB {
			get {
				return maxLogSize / 1024;
			}
			set {
				if (value < 1)
					value = 1;
				maxLogSize = value * 1024;
			}
		}

		/// <summary>
		/// Handles an exception that was thrown
		/// </summary>
		/// <param name="ex">The first exception that was thrown</param>
		/// <param name="errorLog">A full description of the error that occurred (pre-formatted for logging)</param>
		public virtual void LogException(Exception ex, string errorLog) {
			if (errorLog == null)
				return;
			errorLog = errorLog.Replace("\r", string.Empty);
			if (errorLog.Length == 0)
				return;
			errorLog = errorLog[0] == '\n' ? errorLog : "\n" + errorLog;
			Console.WriteLine(RemoveConsecutiveDuplicates(errorLog, '\n'));
			try {
				string filename;
				try {
					filename = GetFileNameWithoutExtension(Reflection.Assembly.GetEntryAssembly().Location) + "_errors.log";
				} catch {
					filename = "Exceptions.log";
				}
				File.AppendAllText(filename, errorLog);
				if (new FileInfo(filename).Length > maxLogSize) {
					int halfSize = maxLogSize / 2;
					string allText = File.ReadAllText(filename);
					File.WriteAllText(filename, allText.Substring(halfSize));
				}
			} catch {
			}
		}

		/// <summary>
		/// Removes consecutive duplicates of the specified character from the string.
		/// </summary>
		/// <param name="str">The string whose duplicate consecutive characters to remove.</param>
		/// <param name="character">The character whose duplicates to remove.</param>
		private static string RemoveConsecutiveDuplicates(string str, char character) {
			if (string.IsNullOrEmpty(str))
				return str;
			StringBuilder builder = new StringBuilder(str.Length);
			char current = str[0];
			builder.Append(current);
			for (int i = 1; i < str.Length; i++) {
				if (str[i] == current) {
					if (character != current)
						builder.Append(current);
				} else {
					current = str[i];
					builder.Append(current);
				}
			}
			return builder.ToString();
		}

		/// <summary>
		/// Returns the file name of the specified path string without the extension.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		private static string GetFileNameWithoutExtension(string path) {
			int dotIndex = -1;
			char chr;
			for (int i = path.Length - 1; i >= 0; i--) {
				chr = path[i];
				if (chr == '/' || chr == '\\')
					return path;
				else if (chr == '.') {
					dotIndex = i;
					break;
				}
			}
			if (dotIndex == -1)
				return path;
			int startOfFileName = 0;
			for (int i = dotIndex - 1; i >= 0; i--) {
				chr = path[i];
				if (chr == '/' || chr == '\\') {
					startOfFileName = i + 1;
					break;
				}
			}
			return path.Substring(startOfFileName, dotIndex - startOfFileName);
		}
	}
}