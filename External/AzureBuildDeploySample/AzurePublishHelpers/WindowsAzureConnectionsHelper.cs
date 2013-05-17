using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AzurePublishHelpers
{
    public class WindowsAzureConnectionsHelper
    {
        private const string vsLocationRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\10.0";
        private const string vsLocationRegValue = "VisualStudioLocation";
        private const string defaultVSLocation = @"%USERPROFILE%\Documents\Visual Studio 2010";
        private const string settingsFolder = "Settings";
        private const string savedConnectionsFile = "Windows Azure Connections.xml";

        private string connectionsFile;

        public string ConnectionsFile
        {
            get 
            {
                if (connectionsFile == null)
                {
                    string location = (string)Registry.GetValue(vsLocationRegKey, vsLocationRegValue, defaultVSLocation);
                    if (location == null)
                    {
                        location = defaultVSLocation;
                    }
            
                    string file = Path.Combine(Path.Combine(location, settingsFolder), savedConnectionsFile);
                    connectionsFile = Environment.ExpandEnvironmentVariables(file);
                }
                return connectionsFile;
            }
            set
            {
                connectionsFile = value;
            }
        }

        private XDocument GetConnectionsDocument()
        {
            if (File.Exists(ConnectionsFile))
            {
                return XDocument.Load(ConnectionsFile);
            }
            else
            {
                return new XDocument(
                    new XElement("NamedCredentials"));
            }
        }

        public void SaveConnection(Connection connection)
        {
            var doc = GetConnectionsDocument();

            // Remove the credential if it's already stored
            var existingNamedCredentialXPath = String.Format("/NamedCredentials/Items/NamedCredential[SubscriptionId='{0}']", connection.SubscriptionId);
            var existingNamedCredentialElement = doc.XPathSelectElement(existingNamedCredentialXPath);
            if (existingNamedCredentialElement != null)
            {
                existingNamedCredentialElement.Remove();
            }

            var itemsElement = doc.XPathSelectElement("/NamedCredentials/Items");
            if (itemsElement == null)
            {
                itemsElement = new XElement("Items");
                doc.Root.Add(itemsElement);
            }

            itemsElement.Add(
                new XElement("NamedCredential",
                    new XElement("SubscriptionId", connection.SubscriptionId),
                    new XElement("IsImported", "true"),
                    new XElement("CertificateThumbprint", connection.CertificateThumbprint),
                    new XElement("ServiceEndpoint", connection.ServiceEndpoint),
                    new XElement("Name", connection.Name)
                    ));

            doc.Save(ConnectionsFile);

        }

        public Connection GetConnection(string name)
        {
            var doc = GetConnectionsDocument();
            var query = from x in doc.XPathSelectElements("/NamedCredentials/Items/NamedCredential")
                        where (string) x.Element("Name") == name
                        select new Connection() {
                            Name = name,
                            CertificateThumbprint = (string)x.Element("CertificateThumbprint"),
                            ServiceEndpoint = (string)x.Element("ServiceEndpoint"),
                                                        SubscriptionId = (string)x.Element("SubscriptionId"),
                        };
            return query.FirstOrDefault();

        }

        public IEnumerable<Connection> GetAllConnections()
        {
            var doc = GetConnectionsDocument();
            var query = from x in doc.XPathSelectElements("/NamedCredentials/Items/NamedCredential")
                        select new Connection()
                        {
                            Name = (string)x.Element("Name"),
                            CertificateThumbprint = (string)x.Element("CertificateThumbprint"),
                            ServiceEndpoint = (string)x.Element("ServiceEndpoint"),
                            SubscriptionId = (string)x.Element("SubscriptionId"),
                        };
            return query.ToList();

        }

    }
}
