using System;

namespace Messages
{
	public class NotificationBackendMessage
	{
		public string Recipient { get; set; }

		public string Topic { get; set; }
		public string Timestamp { get; set; }

		public bool IsValid
		{
			get
			{
				return !string.IsNullOrWhiteSpace(Recipient) && Guid.TryParse(Recipient, out var f)
					&& !string.IsNullOrEmpty(Topic);
			}
		}

		public override string ToString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

	}
}
