using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.DynamicData;

namespace RealWorldOAuthDemo.Models.Account
{
    public class ExtraData
    {
        public int Id { get; set; }
        public string AccessToken { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public UserProfile ParentUserProfile { get; set; }
        public int UserProfileUserId { get; set; }
    }
}