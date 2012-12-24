using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace RealWorldOAuthDemo.Twitter
{
public class TwitterClientEx : OAuthClient
    {
        #region Constants and Fields

        /// <summary>
        /// The description of Twitter's OAuth protocol URIs for use with their "Sign in with Twitter" feature.
        /// </summary>
        public static readonly ServiceProviderDescription TwitterServiceDescription = new ServiceProviderDescription
        {
            RequestTokenEndpoint =
                new MessageReceivingEndpoint(
                    "https://api.twitter.com/oauth/request_token",
                    HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            UserAuthorizationEndpoint =
                new MessageReceivingEndpoint(
                    "https://api.twitter.com/oauth/authenticate",
                    HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            AccessTokenEndpoint =
                new MessageReceivingEndpoint(
                    "https://api.twitter.com/oauth/access_token",
                    HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() },
        };

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterClient"/> class with the specified consumer key and consumer secret.
        /// </summary>
        /// <remarks>
        /// Tokens exchanged during the OAuth handshake are stored in cookies.
        /// </remarks>
        /// <param name="consumerKey">
        /// The consumer key. 
        /// </param>
        /// <param name="consumerSecret">
        /// The consumer secret. 
        /// </param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose the object because we still need it through the app lifetime.")]
        public TwitterClientEx(string consumerKey, string consumerSecret)
            : this(consumerKey, consumerSecret, new AuthenticationOnlyCookieOAuthTokenManager()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterClient"/> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        /// <param name="tokenManager">The token manager.</param>
        public TwitterClientEx(string consumerKey, string consumerSecret, IOAuthTokenManager tokenManager)
            : base("twitter", TwitterServiceDescription, new SimpleConsumerTokenManager(consumerKey, consumerSecret, tokenManager))
        {
        }

        #endregion


        #region Methods

        /// <summary>
        /// Check if authentication succeeded after user is redirected back from the service provider.
        /// </summary>
        /// <param name="response">
        /// The response token returned from service provider 
        /// </param>
        /// <returns>
        /// Authentication result 
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't care if the request for additional data fails.")]
        protected override AuthenticationResult VerifyAuthenticationCore(AuthorizedTokenResponse response)
        {
            string accessToken = response.AccessToken;
            string userId = response.ExtraData["user_id"];
            string userName = response.ExtraData["screen_name"];

            var profileRequestUrl = new Uri("https://api.twitter.com/1/users/show.xml?user_id="
                                       + EscapeUriDataStringRfc3986(userId));
            var profileEndpoint = new MessageReceivingEndpoint(profileRequestUrl, HttpDeliveryMethods.GetRequest);
            HttpWebRequest request = this.WebWorker.PrepareAuthorizedRequest(profileEndpoint, accessToken);

            var extraData = new Dictionary<string, string>();
            extraData.Add("accesstoken", accessToken);
            try
            {
                using (WebResponse profileResponse = request.GetResponse())
                {
                    using (Stream responseStream = profileResponse.GetResponseStream())
                    {
                        XDocument document = LoadXDocumentFromStream(responseStream);
                        foreach (var node in document.Descendants("user").Nodes<XElement>())
                        {
                            if (node.NodeType == XmlNodeType.Element)
                            {
                                extraData.AddDataIfNotEmpty(document, ((System.Xml.Linq.XElement)(node)).Name.LocalName);
                            }
                        }
                        //extraData.AddDataIfNotEmpty(document, "location");
                        //extraData.AddDataIfNotEmpty(document, "description");
                        //extraData.AddDataIfNotEmpty(document, "url");
                    }
                }
            }
            catch (Exception)
            {
                // At this point, the authentication is already successful.
                // Here we are just trying to get additional data if we can.
                // If it fails, no problem.
            }

            return new AuthenticationResult(
                isSuccessful: true, provider: this.ProviderName, providerUserId: userId, userName: userName, extraData: extraData);
        }

        internal static XDocument LoadXDocumentFromStream(Stream stream)
        {
            const int MaxChars = 0x10000; // 64k

            var settings = CreateUntrustedXmlReaderSettings();
            settings.MaxCharactersInDocument = MaxChars;
            return XDocument.Load(XmlReader.Create(stream, settings));
        }

        internal static XmlReaderSettings CreateUntrustedXmlReaderSettings()
        {
            return new XmlReaderSettings
            {
                MaxCharactersFromEntities = 1024,
                XmlResolver = null,
#if CLR4
				DtdProcessing = DtdProcessing.Prohibit,
#else
                ProhibitDtd = true,
#endif
            };
        }

        internal static string EscapeUriDataStringRfc3986(string value)
        {
            //Requires.NotNull(value, "value");

            // Start with RFC 2396 escaping by calling the .NET method to do the work.
            // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
            // If it does, the escaping we do that follows it will be a no-op since the
            // characters we search for to replace can't possibly exist in the string.
            StringBuilder escaped = new StringBuilder(Uri.EscapeDataString(value));

            // Upgrade the escaping to RFC 3986, if necessary.
            for (int i = 0; i < UriRfc3986CharsToEscape.Length; i++)
            {
                escaped.Replace(UriRfc3986CharsToEscape[i], Uri.HexEscape(UriRfc3986CharsToEscape[i][0]));
            }

            // Return the fully-RFC3986-escaped string.
            return escaped.ToString();
        }
        private static readonly string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };

        #endregion
        
    }
    	/// <summary>
	/// The dictionary extensions.
	/// </summary>
	internal static class DictionaryExtensions {
		/// <summary>
		/// Adds the value from an XDocument with the specified element name if it's not empty.
		/// </summary>
		/// <param name="dictionary">
		/// The dictionary. 
		/// </param>
		/// <param name="document">
		/// The document. 
		/// </param>
		/// <param name="elementName">
		/// Name of the element. 
		/// </param>
		public static void AddDataIfNotEmpty(
			this Dictionary<string, string> dictionary, XDocument document, string elementName) {
			var element = document.Root.Element(elementName);
			if (element != null) {
				dictionary.AddItemIfNotEmpty(elementName, element.Value);
			}
		}
        public static void AddItemIfNotEmpty(this IDictionary<string, string> dictionary, string key, string value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (!string.IsNullOrEmpty(value))
            {
                dictionary[key] = value;
            }
        }
    }


}