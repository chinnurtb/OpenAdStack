using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AzurePublishHelpers
{
    public class AzurePubXmlHelper
    {
        public PublishProfile GetPublishProfile(string azurePubXmlFile)
        {
            if (!File.Exists(azurePubXmlFile))
            {
                return null;
            }
            var doc = XDocument.Load(azurePubXmlFile);
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            var props = doc.Element(ns + "Project").Element(ns + "PropertyGroup");
            return new PublishProfile()
            {
                ConnectionName = props.Element(ns + "AzureCredentials").Value,
                HostedServiceName = props.Element(ns + "AzureHostedServiceName").Value,
                HostedServiceLabel = props.Element(ns + "AzureHostedServiceLabel").Value,
                DeploymentSlot = props.Element(ns + "AzureSlot").Value,
                EnableIntelliTrace = props.Element(ns + "AzureEnableIntelliTrace").Value == "True" ? true : false,
                EnableProfiling = props.Element(ns + "AzureEnableProfiling").Value == "True" ? true : false,
                EnableWebDeploy = props.Element(ns + "AzureEnableWebDeploy").Value == "True" ? true : false,
                StorageAccountName = props.Element(ns + "AzureStorageAccountName").Value,
                StorageAccountLabel = props.Element(ns + "AzureStorageAccountLabel").Value,
                DeploymentLabel = props.Element(ns + "AzureDeploymentLabel").Value,
                SolutionConfiguration = props.Element(ns + "AzureSolutionConfiguration").Value,
                ServiceConfiguration = props.Element(ns + "AzureServiceConfiguration").Value,
                AppendTimestampToDeploymentLabel = props.Element(ns + "AzureAppendTimestampToDeploymentLabel").Value == "True" ? true : false,
                AllowUpgrade = props.Element(ns + "AzureDeploymentReplacementMethod").Value == "AutomaticUpgrade" ? true : false,
                EnableRemoteDesktop = props.Element(ns + "AzureEnableRemoteDesktop").Value == "True" ? true : false,
            };
        }
    }
}
