using System;
using Newtonsoft.Json;

namespace Messages
{
	[Serializable]
	public class BackendMessage
	{
		public string EventType { get; set; }
		public dynamic Data { get; set; }

		public BackendMessage()
		{ }

		public BackendMessage(string type, dynamic data)
		{
			EventType = type;
			Data = data;
		}


		[JsonIgnore]
		public bool IsValid => !string.IsNullOrWhiteSpace(EventType) && Data != null;

		/// <summary>
		/// Deserializes string into ClientMesage Object if valid string. 
		/// Otherwise throws exception;
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static BackendMessage Parse(string message)
		{
			return JsonConvert.DeserializeObject<BackendMessage>(message);
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
