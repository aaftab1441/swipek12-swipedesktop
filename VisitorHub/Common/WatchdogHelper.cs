using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FamilyWatchdogProxy.us.familywatchdog.services;
using ServiceStack.ServiceClient.Web;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;
using SwipeK12.NextGen.ReadServices.Messages;

namespace SwipeDesktop.Common
{
    class WatchdogHelper
    {
       
        public static bool Search(VisitorScanViewModel model)
        {
          
            var data = model;

            string completionMessage = string.Empty;
            bool potentialMatch = false;

            var resultList = new List<OffenderSearchResult>();
            try
            {

                using (var Client = new JsonServiceClient(Settings.Default.JsonUrl))
                {
                    var results = Client.Post<Response<Swipe.Common.Helper.Offender[]>>("OffenderSearch",
                        new {data.State, data.LastName, data.FirstName, Dob = data.DateOfBirth.Value});

                    if (results.data == null || !results.data.Any())
                    {
                        completionMessage = "Search did not yield any offenses.";
                    }
                    else
                    {
                        potentialMatch = true;
                        completionMessage = "Potential offender match:";


                        foreach (Swipe.Common.Helper.Offender result in results.data)
                        {

                            var offender = new OffenderSearchResult();
                            offender.OffenderSearchId = result.OffenderId;
                            offender.Name = result.Name;
                            offender.HtmlLink = result.Photo; //GetHtmlLink(new Uri(result.Photo));
                            offender.Image = LoadImage(offender.HtmlLink);
                            offender.BirthDate = result.Dob;

                            offender.DemographicData = new DemographicInformation();
                            offender.DemographicData.EyeColor = result.Eye;
                            offender.DemographicData.HairColor = result.Hair;
                            offender.DemographicData.Height = result.Height;
                            offender.DemographicData.Race = result.Race;
                            offender.DemographicData.Gender = result.Sex;
                            offender.DemographicData.Weight = result.Weight;

                            resultList.Add(offender);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                completionMessage = ex.Message;
            }

            if (potentialMatch)
            {
                var status = Notifications.SexOffenderNotification(model, resultList, resultList.Count);
                /*foreach (var hit in resultList)
                {
                    var status = Notifications.SexOffenderNotification(model, hit);
                }*/
            }

            return potentialMatch;
            //todo: send screening results message
            //_viewModel.RaiseOffenderDialog(new NotificationEventArgs<List<OffenderSearchResult>>(completionMessage, resultList));
        }

        static string GetHtmlLink(Uri uri)
        {
            var request = HttpWebRequest.Create(uri);
            request.Timeout = 1000;

            var response = (HttpWebResponse)request.GetResponse();

            var reader = new StreamReader(response.GetResponseStream(), new UTF8Encoding());

            string html = reader.ReadToEnd();
            string matchString = Regex.Match(html, "<img.+?src=[\"'](.+?)[\"'].+?>", RegexOptions.IgnoreCase).Groups[1].Value;

            return matchString;
        }
        static BitmapImage LoadImage(string url)
        {

            var image = new BitmapImage();
            int BytesToRead = 250;

            WebRequest request = WebRequest.Create(url);
            request.Timeout = 3000;

            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            BinaryReader reader = new BinaryReader(responseStream);
            MemoryStream memoryStream = new MemoryStream();

            byte[] bytebuffer = new byte[BytesToRead];
            int bytesRead = reader.Read(bytebuffer, 0, BytesToRead);

            while (bytesRead > 0)
            {
                memoryStream.Write(bytebuffer, 0, bytesRead);
                bytesRead = reader.Read(bytebuffer, 0, BytesToRead);
            }

            image.BeginInit();
            memoryStream.Seek(0, SeekOrigin.Begin);

            image.StreamSource = memoryStream;
            image.EndInit();

            return image;

        }
    }
}
