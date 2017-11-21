namespace Notifications.Services
{
	public class IdentityProvider : IIdentityProvider
	{
		public string GetRecipient(string token)
		{
			// Implement more sophisticated logic
			return token;
		}
	}
}
