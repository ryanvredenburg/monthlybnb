using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace monthlybnb.Models
{
    public class Listing
    {
        public int Id { get; set; }
        public int AirbnbId { get; set; }
        public int Bedrooms { get; set; }
        public double MonthlyPrice { get; set; }
        public double MonthlyMultiplier { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        
        public virtual City City { get; set; }

    }
}