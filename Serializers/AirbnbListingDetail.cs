using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace monthlybnb.Serializers
{
    public class AirbnbListingDetail
    {
        
        public class Metadata
        {
        }

        public class Listing
        {
           
            public double lat { get; set; }
            public double lng { get; set; }        
            public string name { get; set; }          
            public int bedrooms { get; set; }
            public string room_type { get; set; }
            public string room_type_category { get; set; }
            public double price { get; set; }
            public double monthly_price_factor { get; set; }

        }

        public class RootObject
        {
            public Metadata metadata { get; set; }
            public Listing listing { get; set; }
        }
        
    }
}