using System;
namespace OnlineMarket.Core.Common.Entities
{
	public class Package
	{
		// PK
		public int shipment_id;
		public int package_id;

        // FK
        // product identification
        public int seller_id;
        public int product_id;

        public float freight_value;

		// date the shipment has actually been performed
		public int shipping_date;

        // delivery date
        public int delivery_date;
		// public int estimated_delivery_date;

		// delivery to carrier date
		// seller must deliver to carrier
		// public int delivered_carrier_date;

		public int quantity;

		public PackageStatus status { get; set; }
    }
}

