using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using monthlybnb.Models;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace monthlybnb.Controllers
{
    public class CitiesController : Controller
    {
        private DataContext db = new DataContext();



        // GET: Cities/DatasetUpdate/5
        public ActionResult DatasetUpdate(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            City city = db.Cities.Find(id);
            if (city == null)
            {
                return HttpNotFound();
            }


            string resultURL = "You've triggered a manual update of " + city.Name + " The update can take up to 30 minutes to complete. No need to run it again. In real production this would be replaced with a scheduled task, but it's convenient to manually trigger for now";
            //because this could take considerable time, spawn and asyncronous thread to handle it
            AirbnbAsync(id);

            return Content(resultURL);
        }


        static async Task AirbnbAsync(int? id)
        {
            DataContext db = new DataContext();
            City updateCity = db.Cities.Find(id);

            //delete previous airbnb data
            db.Listings.RemoveRange(updateCity.Listings);
            updateCity.MonthlyAveragePrice = 0;


            int resultsPerRequest = 10;
            int maximumRequests = 400;

            List<int> airbnbListings = new List<int>();

            string baseUrl = "https://api.airbnb.com/v2/";
            string searchFields = "search_results?client_id=3092nxybyb0otqw18e8nh5nty&locale=en-US&currency=USD&guests=2";
            searchFields += "&location=" + System.Uri.EscapeDataString(updateCity.Name);
            searchFields += "&user_lat=" + updateCity.Lat;
            searchFields += "&user_lng=" + updateCity.Long;
            searchFields += "&_limit=" + resultsPerRequest;

            //perform a City search against AirBnB API
            //keep a list of all IDs that are "entire_home", and within range of the City.Radius
            for (int i = 1; i <= maximumRequests; i++)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync(searchFields + "&_offset=" + (i - 1) * resultsPerRequest).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            monthlybnb.Serializers.AirbnbSearchResult.RootObject airbnbSearchResult = await response.Content.ReadAsAsync<monthlybnb.Serializers.AirbnbSearchResult.RootObject>();

                            foreach (monthlybnb.Serializers.AirbnbSearchResult.SearchResult searchResult in airbnbSearchResult.search_results)
                            {
                                monthlybnb.Serializers.AirbnbSearchResult.Listing basicListing = searchResult.listing;
                                if (!airbnbListings.Exists(x => x == basicListing.id))
                                {
                                    if (basicListing.room_type_category == "entire_home" && updateCity.inRange(basicListing.lat, basicListing.lng))
                                    {
                                        airbnbListings.Add(basicListing.id);
                                    }
                                }


                            }
                        }
                        catch { }

                    }
                    else //if the API request errors, AirBnB is out of listings before maximumRequests is reached
                    {
                        i = maximumRequests + 1;
                    }

                }
                //because AirBnB API is only semi-official, pause execution after each call to not hammer it
                System.Threading.Thread.Sleep(1000);
            }


            //Call AirBnB API for detailed information of each listing that matched criteria above
            //Save all listings that have monthly pricing discounts to database
            foreach (int listingID in airbnbListings)
            {
                baseUrl = "https://api.airbnb.com/v2/listings/";
                searchFields = listingID.ToString();
                searchFields += "?client_id=3092nxybyb0otqw18e8nh5nty&_format=v1_legacy_for_p3&number_of_guests=2";

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync(searchFields).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            monthlybnb.Serializers.AirbnbListingDetail.RootObject airbnbListingResult = await response.Content.ReadAsAsync<monthlybnb.Serializers.AirbnbListingDetail.RootObject>();

                            Listing dbListing = new Listing();
                            dbListing.AirbnbId = listingID;
                            dbListing.Lat = airbnbListingResult.listing.lat;
                            dbListing.Long = airbnbListingResult.listing.lng;
                            dbListing.Bedrooms = airbnbListingResult.listing.bedrooms;
                            dbListing.MonthlyPrice = Math.Round(airbnbListingResult.listing.price * 28 * airbnbListingResult.listing.monthly_price_factor, 2);
                            dbListing.MonthlyMultiplier = airbnbListingResult.listing.monthly_price_factor;

                            //Listings with MonthlyMultiplier of 1 are optimized for daily, and not monthly rentals
                            if (dbListing.MonthlyMultiplier < 1)
                            {
                                updateCity.Listings.Add(dbListing);
                            }

                        }
                        catch { }

                    }
                    //because AirBnB API is only semi-official, pause execution after each call to not hammer it
                    System.Threading.Thread.Sleep(1000);

                }
            }
            updateCity.LastUpdated = DateTime.Now;
            db.SaveChanges();
            updateCity.updateMonthlyAveragePrice();

        }


        /////////// Everything below is generic scaffolding


        // GET: Cities
        public ActionResult Index()
        {
            return View(db.Cities.ToList());
        }

        // GET: Cities/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            City city = db.Cities.Find(id);
            if (city == null)
            {
                return HttpNotFound();
            }
            return View(city);
        }

        // GET: Cities/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Cities/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Lat,Long,Radius,MonthlyAveragePrice,LastUpdated")] City city)
        {
            if (ModelState.IsValid)
            {
                db.Cities.Add(city);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(city);
        }

        // GET: Cities/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            City city = db.Cities.Find(id);
            if (city == null)
            {
                return HttpNotFound();
            }
            return View(city);
        }

        // POST: Cities/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Lat,Long,Radius,MonthlyAveragePrice,LastUpdated")] City city)
        {
            if (ModelState.IsValid)
            {
                db.Entry(city).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(city);
        }

        // GET: Cities/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            City city = db.Cities.Find(id);
            if (city == null)
            {
                return HttpNotFound();
            }
            return View(city);
        }

        // POST: Cities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            City city = db.Cities.Find(id);
            db.Cities.Remove(city);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
