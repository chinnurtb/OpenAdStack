using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzurePublishHelpers
{
    public class PublishProfile
    {
        public string ConnectionName { get; set; }
        public string HostedServiceName { get; set; }
        public string HostedServiceLabel { get; set; }
        public string DeploymentSlot { get; set; }
        public bool EnableIntelliTrace { get; set; }
        public bool EnableProfiling { get; set; }
        public bool EnableWebDeploy { get; set; }
        public string StorageAccountName { get; set; }
        public string StorageAccountLabel { get; set; }
        public string DeploymentLabel { get; set; }
        public string SolutionConfiguration { get; set; }
        public string ServiceConfiguration { get; set; }
        public bool AppendTimestampToDeploymentLabel { get; set; }
        public bool AllowUpgrade { get; set; }
        public bool EnableRemoteDesktop { get; set; }
    }
}

