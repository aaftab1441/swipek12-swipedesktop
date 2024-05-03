using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Common
{
    internal class Notifications
    {
        public static string USERNAME = Settings.Default.elastic_email_user;
        public static string API_KEY = Settings.Default.elastic_email_api_key;

        public static string SendNotification(VisitorScanViewModel model)
        {
            //TODO: get notification details from file
            var ini = new IniFile(string.Format(@"{0}\{1}.txt", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), model.VisitLocation));
            //ini.IniWriteValue("Default", "test", "test");
            var addresses = ini.IniReadValue("Default", "Addresses");
            var subject = string.Format(ini.IniReadValue("Default", "Subject"));
            var bodyText = string.Format(ini.IniReadValue("Default", "Text"), model.FullName, model.VisitLocation);
            var html = string.Format(ini.IniReadValue("Default", "Html"), model.FullName, model.VisitLocation);

            string from = Settings.Default.sender_email;
            string fromName = Settings.Default.sender_name;
            string to = addresses;
            //string subject = subject;
            string bodyHtml = !string.IsNullOrEmpty(html) ? html : null;
            //string bodyText = "Text Body";

            string status = string.Empty;
            if (!string.IsNullOrEmpty(addresses))
            {
                try
                {
                    status = SendEmail(to, subject, bodyText, bodyHtml, from, fromName);
                }
                catch (Exception ex) { /*todo: log ex*/}
            }

            return status;
        }

        public static string SexOffenderNotification(VisitorScanViewModel model, List<OffenderSearchResult> offenderResult, int countResults)
        {
            //TODO: get notification details from Application.Properties
            var ini = new IniFile(string.Format(@"{0}\{1}.txt", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Sex Offender"));
            //ini.IniWriteValue("Default", "test", "test");
            var addresses = ini.IniReadValue("Default", "Addresses");
            var subject = string.Format(ini.IniReadValue("Default", "Subject"));

            string links = string.Empty;

            foreach (var rslt in offenderResult)
            {
                links = links + rslt.HtmlLink + @"\n";
            }

            var bodyText = string.Format(ini.IniReadValue("Default", "Text"), model.FullName, model.VisitLocation, links) + $" {countResults} potential result(s) found.";
            var html = string.Format(ini.IniReadValue("Default", "Html"), model.FullName, model.VisitLocation, links.Replace(@"\n", "<BR/>"));

            string bodyHtml = !string.IsNullOrEmpty(html) ? html : null;
          
            string status = string.Empty;
            if (!string.IsNullOrEmpty(addresses))
            {
                try
                {
                    status = SendEmail(addresses, subject, bodyText, bodyHtml, Settings.Default.sender_email, Settings.Default.sender_name);
                }
                catch (Exception ex) { /*todo: log ex*/}
            }

            return status;
        }


        public static string SendEmail(string to, string subject, string bodyText, string bodyHtml, string from, string fromName)
        {

            var client = new WebClient();
            var values = new NameValueCollection();
            values.Add("username", USERNAME);
            values.Add("api_key", API_KEY);
            values.Add("from", from);
            values.Add("from_name", fromName);
            values.Add("subject", subject);
            if (bodyHtml != null)
                values.Add("body_html", bodyHtml);
            else
            {
                values.Add("body_text", bodyText);
            }
            values.Add("to", to);

            byte[] response = client.UploadValues("https://api.elasticemail.com/mailer/send", values);
            return Encoding.UTF8.GetString(response);
        }
    }
}
