using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
	public class OfferCommentNotification: OfferNotification
	{
		public string PurchaseId { get; set; }
		public string UserId { get; set; }
		public long Cost { get; set; }


		public new bool IsValid
		{
			get
			{
				return !string.IsNullOrWhiteSpace(PurchaseId) && Guid.TryParse(PurchaseId, out var m)
					&& !string.IsNullOrWhiteSpace(UserId) && Guid.TryParse(UserId, out var o)
					&& base.IsValid;
			}
		}
	}
}
