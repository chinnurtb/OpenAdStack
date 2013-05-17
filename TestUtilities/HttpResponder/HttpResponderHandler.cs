//-----------------------------------------------------------------------
// <copyright file="HttpResponderHandler.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace HttpResponder
{
    /// <summary>
    /// Handler class for HttpResponse utility
    /// </summary>
    public class HttpResponderHandler : IHttpHandler
    {
        /// <summary>
        /// Variable to store deserialized data from ConfigurationMapping.xml
        /// </summary>
        private List<ResponseMapping> mappingList;

        /// <summary>
        /// Initializes a new instance of the HttpResponderHandler class.
        /// </summary>
        public HttpResponderHandler()
        {
            // Reading ConfigurationMapping.xml file from embedded resources
            using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpResponder.Resources.ConfigurationMapping.xml"))
            {                
                using (var xmlDictionaryReader = XmlDictionaryReader.CreateTextReader(resourceStream,  new XmlDictionaryReaderQuotas()))
                {
                    // Deserialize the data and read it from the instance.
                    var serializer = new DataContractSerializer(typeof(List<ResponseMapping>));
                    this.mappingList = (List<ResponseMapping>)serializer.ReadObject(xmlDictionaryReader, true);
                }
            }
        }

        /// <summary>
        ///  Gets a value indicating whether handler to pool or not
        /// </summary>
        public bool IsReusable
        {
            get { return true; }
        }

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the IHttpHandler interface.
        /// </summary>
        /// <param name="context">Http Context</param>
        public void ProcessRequest(HttpContext context)
        {
            // variables to read request and response from HttpContext
            var request = context.Request;
            var response = context.Response;

            ResponseMapping mapping = null;

            try
            {
                // Get matched mapping after matching request with mapping list
                mapping = this.FindMapping(request);

                // Reading matched resouce and putting it to Http Response
                var textStreamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(mapping.ResourceName));
                response.Write(textStreamReader.ReadToEnd());
            }
            catch (InvalidOperationException)
            {
                // Setting the response status code
                if (mapping == null)
                {
                    response.StatusCode = 404;
                }
                else
                {
                    response.StatusCode = mapping.ResponseStatusCode;
                }
            }
        }

        /// <summary>
        /// Finds matched mapping
        /// </summary>
        /// <param name="request">Http Request</param>
        /// <returns>Matched ResponseMapping</returns>
        private ResponseMapping FindMapping(HttpRequest request)
        {
            // Getting requestBody from Http request
            var requestBody = new StreamReader(request.InputStream).ReadToEnd();

            // Finding matched mapping
            var mapping = this.mappingList.Where(m => request.RequestType.Equals(m.HttpVerb)
                                            && (string.IsNullOrWhiteSpace(m.HeadersRegex) || Regex.IsMatch(request.Headers.ToString(), m.HeadersRegex))
                                            && (string.IsNullOrWhiteSpace(m.HeadersContains) || request.Headers.ToString().Contains(m.HeadersContains))
                                            && (string.IsNullOrWhiteSpace(m.BodyRegex) || Regex.IsMatch(requestBody, m.BodyRegex))
                                            && (string.IsNullOrWhiteSpace(m.BodyContains) || requestBody.Contains(m.BodyContains))
                                            && (string.IsNullOrWhiteSpace(m.UrlRegex) || Regex.IsMatch(request.RawUrl, m.UrlRegex))
                                            && (string.IsNullOrWhiteSpace(m.UrlContains) || request.RawUrl.Contains(m.UrlContains))).Single();
            
            return mapping;
        }
    }
}