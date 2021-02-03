using Nito.AsyncEx;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Web;
using System.Runtime.InteropServices;

namespace EZPragmatik
{
   class Program
   {
      static void Main(string[] args)
      {
         AsyncContext.Run(() => MainAsync(args));
      }

      static async void MainAsync(string[] args)
      {
         try
         {
            Task<string> asyncResult = GetJiraContent();
            string result = await asyncResult;

            XmlReader reader = XmlReader.Create(new StringReader(result));
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();

            foreach (SyndicationItem item in feed.Items)
            {
               string sentence = BuildPragmatikSentence(item.Title.Text);
               if (!string.IsNullOrEmpty(sentence))
               {
                  Console.WriteLine(sentence);
               }
            }
         }
         catch(Exception e)
         {
            Console.WriteLine("Error: " + e.Message + "\n" + "It worked on my machine. ¯\\_(ツ)_/¯");
         }
      }

      public static string BuildPragmatikSentence(string s)
      {
         string verb = string.Empty;
         string ticketName = string.Empty;
         int startIndex = 0;
         int endIndex = 0;

         // remove first <a> attribute
         startIndex = s.IndexOf("</a>") + 5;
         s = s.Substring(startIndex);

         // get verb
         startIndex = 0;
         endIndex = s.IndexOf("<");
         verb = GetVerb(s.Substring(startIndex, endIndex));
         s = s.Substring(endIndex);

         // get ticket name
         endIndex = 0;
         startIndex = s.IndexOf(">") + 1;
         s = s.Substring(startIndex);
         startIndex = 0;
         endIndex = s.IndexOf("<");
         ticketName = s.Substring(startIndex, endIndex);

         // decode html characters out of ticket title
         ticketName = HttpUtility.HtmlDecode(ticketName);


         if (string.IsNullOrWhiteSpace(verb))
         {
            return "";
         }
         else
         {
            return "I" + verb + ticketName;
         }
      }

      public static string GetVerb(string s)
      {
         if (s.ToLower().Contains("changed the status to ready for testing on"))
         {
            return " finished code reviewing ";
         }
         else if (s.ToLower().Contains("changed the status to code reviewing on "))
         {
            return " began code reviewing ";
         }
         else if (s.ToLower().Contains("changed the status to ready for cr on "))
         {
            return " finished developement work on ";
         }
         else
         {
            return "";
         }
      }

      public static async Task<string> GetJiraContent()
      {
         string jiraUsername = ConfigurationManager.AppSettings["JiraUsername"];
         string jiraPassword = ConfigurationManager.AppSettings["JiraPassword"];
         string entriesToReturn = ConfigurationManager.AppSettings["EntriesToReturn"];
         string url = "https://jira.lcs.com/activity?maxResults=" + entriesToReturn + "&streams=user+IS+" + jiraUsername + "&os_authType=basic&title=undefined";

         var byteArray = Encoding.ASCII.GetBytes(jiraUsername + ":" + jiraPassword);
         string encodedAuthenticaion = Convert.ToBase64String(byteArray);

         HttpClient client = new HttpClient();
         client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedAuthenticaion);
         return await client.GetStringAsync(url);
      }
   }
}
