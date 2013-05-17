using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzurePublishHelpers
{
    public class Connection
    {
        public string Name { get; set; }
        public string SubscriptionId { get; set; }
        public string ServiceEndpoint { get; set; }
        public string CertificateThumbprint { get; set; }
    }
}
