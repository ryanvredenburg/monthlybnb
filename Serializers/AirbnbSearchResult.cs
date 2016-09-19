using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace monthlybnb.Serializers
{
    
    public class AirbnbSearchResult
    {

        public class Metadata
        {

        }

        public class Listing
        {

            public int bedrooms { get; set; }
            public int id { get; set; } 
            public double lat { get; set; }
            public double lng { get; set; }
            public string name { get; set; }
            public string room_type_category { get; set; }
  
        }


        public class SearchResult
        {
            public Listing listing { get; set; }
        }

        public class RootObject
        {
            public Metadata metadata { get; set; }
            public List<SearchResult> search_results { get; set; }
        }


    }
}