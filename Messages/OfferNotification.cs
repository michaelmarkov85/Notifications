using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
	public class OfferNotification
	{
		public string MerchantId { get; set; }
		public string OfferId { get; set; }
		public int Edition { get; set; }

		public bool IsValid
		{
			get
			{
				return !string.IsNullOrWhiteSpace(MerchantId) && Guid.TryParse(MerchantId, out var m)
					&& !string.IsNullOrWhiteSpace(OfferId) && Guid.TryParse(OfferId, out var o);
			}
		}
	}
}
