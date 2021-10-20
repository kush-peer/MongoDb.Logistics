namespace MongoDb.Logistics.Logging
{
	/// <summary>
	/// This type eliminates the need to depend directly on the ASP.NET Core logging types. we can easily extend it as per our need
	/// </summary>
	public interface IAppLogger<T>
	{
		void LogInformation(string message, params object[] args);
		void LogWarning(string message, params object[] args);
		void LogError(string message, params object[] args);
	}
}
