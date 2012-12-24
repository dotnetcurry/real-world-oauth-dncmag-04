using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using RealWorldOAuthDemo.Twitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RealWorldOAuthDemo.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public static XDocument GetUpdates(ConsumerBase twitter, string accessToken)
        {
            IncomingWebResponse response = twitter.PrepareAuthorizedRequestAndSend(GetFriendTimelineStatusEndpoint, accessToken);
            return XDocument.Load(XmlReader.Create(response.GetResponseReader()));
        }

        private static readonly MessageReceivingEndpoint GetFriendTimelineStatusEndpoint = new MessageReceivingEndpoint("https://api.twitter.com/1/statuses/friends_timeline.xml", HttpDeliveryMethods.GetRequest);
    }
}
