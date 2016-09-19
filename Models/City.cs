using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Device.Location;


namespace monthlybnb.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public double Radius { get; set; }
        public double MonthlyAveragePrice { get; set; }
        public DateTime LastUpdated { get; set; }

        public virtual List<Listing> Listings { get; set; }

        public bool inRange(double listingLat, double listingLong)
        {
            var cityCoord = new GeoCoordinate(Lat, Long);
            var listingCoord = new GeoCoordinate(listingLat, listingLong);
            double distance = cityCoord.GetDistanceTo(listingCoord) / 1000;

            if (distance > Radius)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        public void updateMonthlyAveragePrice()
        {
            DataContext db = new DataContext();
            City city = db.Cities.Find(Id);  
            int count = 0;
            double sum = 0;
            foreach (Listing listing in city.Listings)
            {
                count++;
                sum += listing.MonthlyPrice;
            }
            if (count == 0)
            {
                city.MonthlyAveragePrice = 0;
            }
            else
            {
                city.MonthlyAveragePrice = Math.Round(sum / count, 2);
            }
                

            db.SaveChanges();
        }

        public double[] quintileAveragePrices()
        {

            double[] quintileSums = new double[5];
            double[] quintileCounts = new double[5];
            double[] quintileAverages = new double[5];
            int totalListings = Listings.Count;
            int count = 0; 
            foreach (Listing listing in Listings.OrderBy(o => o.MonthlyPrice))
            {
                count++;
                if (count < totalListings * .2)
                {
                    quintileCounts[0] += 1;
                    quintileSums[0] += listing.MonthlyPrice;
                }
                else if (count < totalListings * .4)
                {
                    quintileCounts[1] += 1;
                    quintileSums[1] += listing.MonthlyPrice;
                }
                else if (count < totalListings * .6)
                {
                    quintileCounts[2] += 1;
                    quintileSums[2] += listing.MonthlyPrice;
                }
                else if (count < totalListings *.8)
                {
                    quintileCounts[3] += 1;
                    quintileSums[3] += listing.MonthlyPrice;

                }
                else
                {
                    quintileCounts[4] += 1;
                    quintileSums[4] += listing.MonthlyPrice;
                }


            }
            quintileAverages[0] = Math.Round(quintileSums[0] / quintileCounts[0], 2);
            quintileAverages[1] = Math.Round(quintileSums[1] / quintileCounts[1], 2);
            quintileAverages[2] = Math.Round(quintileSums[2] / quintileCounts[2], 2);
            quintileAverages[3] = Math.Round(quintileSums[3] / quintileCounts[3], 2);
            quintileAverages[4] = Math.Round(quintileSums[4] / quintileCounts[4], 2);

            return quintileAverages;
        }
    }
}