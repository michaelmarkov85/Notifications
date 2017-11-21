namespace Notifications
{
	public interface IIdentityProvider
	{
		string GetRecipient(string token);
	}
}
