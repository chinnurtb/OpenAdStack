using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AzurePublishHelpers;
using System.Security.Cryptography.X509Certificates;

namespace ImportPublishSettings
{
    class Program
    {
        static int Main(string[] args)
        {
            StoreLocation certLocation = StoreLocation.CurrentUser;

            if (args.Length < 1 || args.Length > 3)
            {
                Console.WriteLine("Usage: ImportPublishSettings <publishSettingsFilename> [certStoreLocation] [connectionsFile]");
                Console.WriteLine("\tcertStoreLocation defaults to CurrentUser");
                Console.WriteLine("\tconnectionsFileName defaults to Visual Studio's settings file for the current user.");
                return 1;
            }
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Could not find file: " + args[0]);
                return 1;
            }
            if (args.Length > 1)
            {
                string certLocationString = args[1].ToLowerInvariant();
                if (certLocationString != "localmachine" && certLocationString != "currentuser")
                {
                    Console.WriteLine("certStoreLocation must be either 'LocalMachine' or 'CurrentUser'");
                    return 1;
                }
                if (certLocationString == "localmachine")
                {
                    certLocation = StoreLocation.LocalMachine;
                }
            }
            
            try
            {
                var pubSettings = new PubSettingsHelper();
                var connections = pubSettings.ImportPubSettingsFile(args[0], true, certLocation);
                Console.WriteLine("Successfully imported certificate for connections in '{0}'", args[0]);
                var azureConnectionsHelper = new WindowsAzureConnectionsHelper();

                if (args.Length > 2)
                {
                    azureConnectionsHelper.ConnectionsFile = args[2];
                }

                foreach (var connection in connections)
                {
                    azureConnectionsHelper.SaveConnection(connection);
                    Console.WriteLine("Successfully saved connection '{0}' to {1}", connection.Name, azureConnectionsHelper.ConnectionsFile);
                }
                Console.WriteLine("Please remember to protect or delete your .publishsettings file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error importing file.");
                Console.WriteLine(ex.ToString());
                return 1;
            }
            return 0;
        }
    }
}
