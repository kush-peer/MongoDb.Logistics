
using Microsoft.Extensions.Logging;

namespace MongoDb.Logistics.Logging
{
	public class LoggerAdapter<T> : IAppLogger<T>
	{
		private readonly ILogger<T> _logger;
		/// <summary>
		/// Constructor for logger adapter
		/// </summary>
		/// <param name="loggerFactory">inject  interface for logger factory</param>
		public LoggerAdapter(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<T>();
		}
		/// <summary>
		/// method for warnings
		/// </summary>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public void LogWarning(string message, params object[] args)
		{
			_logger.LogWarning(message, args);
		}
		/// <summary>
		/// method for Information
		/// </summary>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public void LogInformation(string message, params object[] args)
		{
			_logger.LogInformation(message, args);
		}
		/// <summary>
		/// method for Error
		/// </summary>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public void LogError(string message, params object[] args)
		{
			_logger.LogError(message, args);
		}
	}
}
