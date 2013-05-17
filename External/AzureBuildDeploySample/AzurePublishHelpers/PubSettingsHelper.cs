using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Security.Cryptography.X509Certificates;

namespace AzurePublishHelpers
{
    // Based on sample from http://www.wadewegner.com/2011/11/programmatically-installing-and-using-your-management-certificate-with-the-new-publishsettings-file/ 
    
    public class PubSettingsHelper
    {
        public IEnumerable<Connection> ImportPubSettingsFile(string publishSettingsFile, bool installCertificate, StoreLocation storeLocation)
        {
            XDocument xdoc = XDocument.Load(publishSettingsFile);
            var profileRoot = xdoc.XPathSelectElement("/PublishData/PublishProfile[@PublishMethod='AzureServiceManagementAPI']");

            if (profileRoot == null)
            {
                throw new InvalidOperationException("publishSettings file does not contain Azure profile information.");
            }

            var managementCertbase64string =
                profileRoot.Attribute("ManagementCertificate").Value;

            var keyStoreLocation = storeLocation == StoreLocation.CurrentUser ?
                X509KeyStorageFlags.UserKeySet : X509KeyStorageFlags.MachineKeySet;

            var importedCert = new X509Certificate2(
                Convert.FromBase64String(managementCertbase64string), 
                string.Empty,
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable | keyStoreLocation);
            
            if (installCertificate)
            {
                X509Store store = new X509Store(StoreName.My, storeLocation);
                store.Open(OpenFlags.ReadWrite);
                store.Add(importedCert);
                store.Close();
            }

            var connections = from sub in profileRoot.XPathSelectElements("Subscription")
                              select new Connection
                              {
                                  Name = sub.Attribute("Name").Value,
                                  SubscriptionId = sub.Attribute("Id").Value,
                                  CertificateThumbprint = importedCert.Thumbprint,
                                  ServiceEndpoint = profileRoot.Attribute("Url").Value,
                              };

            return connections.ToList();

        }
    }
}

