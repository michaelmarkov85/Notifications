using System;
using Newtonsoft.Json;

namespace Messages
{
	[Serializable]
	public class FrontendMessage
	{
		public string Type { get; set; }
		public dynamic Data { get; set; }

		public FrontendMessage()
		{ }

		public FrontendMessage(string type, dynamic data)
		{
			Type = type;
			Data = data;
		}


		[JsonIgnore]
		public bool IsValid => !string.IsNullOrWhiteSpace(Type) && Data != null;

		/// <summary>
		/// Deserializes string into ClientMesage Object if valid string. 
		/// Otherwise throws exception;
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static FrontendMessage Parse(string message)
		{
			return JsonConvert.DeserializeObject<FrontendMessage>(message);
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
