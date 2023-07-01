// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Media;
using Azure.ResourceManager.Media.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;
using System.Threading;

using System;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using amsTool;
using Azure.Storage.Blobs;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Azure;
using System.Runtime.CompilerServices;

namespace MediaServicesReportingTool
{
    class Program
    {
        private string bearerToken;
        private outputJson outputObj = new outputJson();
        List<assetData> assetlist = new List<assetData>();
        private int storageCount;
        private Dictionary<string, string> storageAccounts = new Dictionary<string, string>();
        private AuthenticationConfig config;
        private static string timestamp = DateTime.UtcNow.ToString("MMddyyyymmss");
        private static AppDomain root = null;
        static async Task Main(string[] args)
        {
            //defines  Application root that is used in Log method to find current folder
            root = AppDomain.CurrentDomain;
            timestamp = DateTime.UtcNow.ToString("yyyyMMddhhmm");

            Console.WriteLine("What format would you like to output your report? Please input CSV, TXT, JSON ");
            string outputtype = Console.ReadLine();
            outputtype = outputtype.ToUpper();

            if (outputtype == "CSV") {
                await CSVMain();
            } 
            else if (outputtype == "JSON") {
                await JSONMain();
            } else if (outputtype == "TXT") {
                await TXTMain();
             } else {
                //add error message here "input must be "TXT", "CSV", or "JSON"
                Console.Error.WriteLine("Error: Your values don't match TXT,CSV,or JSON.  Please enter only TXT,CSV,or JSON");

                outputtype = Console.ReadLine();
                outputtype = outputtype.ToUpper();
                Console.WriteLine($"Results will be written to {root.BaseDirectory}AMSreports");

                if (outputtype == "CSV")
                {
                    await CSVMain();
                }
                else if (outputtype == "JSON")
                {
                    await JSONMain();
                }
                else if (outputtype == "TXT")
                {
                    await TXTMain();
                }
                else
                {
                    
                    Console.Error.WriteLine("Error: You've entered the incorrect value twice. Program will end. Press any key to exit.");
                    Console.ReadKey();
                    
                }
               
                Console.WriteLine($"Results will be written to {root.BaseDirectory}AMSreports.  Press any key to close the program.");
                Console.ReadKey();
            }
                

        }
        #region CSV TXT Methods 
        static async Task CSVMain()
        {
            // Loading the settings from the appsettings.json file or from the command line parameters
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                //.AddCommandLine(args)
                .Build();
            root = AppDomain.CurrentDomain;


            if (!Options.TryGetOptions(configuration, out var options))
            {
                return;
            }


            // TableOfContents();

            Console.WriteLine($"Subscription ID:             {options.subscription}");
            Console.WriteLine($"Resource group name:         {options.resourceGroup}");
            Console.WriteLine($"Media Services account name: {options.mediaServiceAccount}");
            Console.WriteLine();

            // First we construct the ArmClient using DefaultAzureCredential

            //var client = new ArmClient(new DefaultAzureCredential());
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var client = new ArmClient(credential);

            SubscriptionCollection subscriptions = client.GetSubscriptions();
            SubscriptionResource subscription = subscriptions.Get(options.subscription.ToString());
            Console.WriteLine($"Got subscription: {subscription.Data.DisplayName}");

            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(options.resourceGroup);

            // Get all the media accounts in as resource group
            MediaServicesAccountCollection mediaServices = resourceGroup.GetMediaServicesAccounts();


            //Get Media Account details 
            foreach (MediaServicesAccountResource myAccount in mediaServices)
            {
                Console.WriteLine($"name= {myAccount.Data.Name}");
                Console.WriteLine($"location= {myAccount.Data.Location}");

                //// CSV output ////
                GetMediaAssetsInfoCSV(myAccount);
                GetStreamingEndpointsInfoCSV(myAccount);
                GetMediaTransformInfoCSV(myAccount);
                GetMediaLiveEventsInfoCSV(myAccount);

                

            }

            Console.WriteLine($"Results will be written to {root.BaseDirectory}AMSreports.  Press any key to close the program.");
            Console.ReadKey();

        }

      

        //Gets Assets Info CSV
        public static void GetMediaAssetsInfoCSV(MediaServicesAccountResource myAccount)
        {
            
            //Gets the assets for the media service account
            // csv Headers
            Log("AssetId,AssetName,Description,CreatedOn,StorageAccount,Container,ResourceType,StorageEncryptionFormat,StorageEncryptionScope", $"mediaassetslog_{timestamp}.csv");
            Log("AssetId,AssetName,LocatorId,LocatorName,StreamingPolicy,EndsOn,ContentKeyPolicy", $"streaminglocatorlog_{timestamp}.csv");
            Log("AssetId,AssetName,Filters", $"assetfilterslog_{timestamp}.csv");
           
            foreach (MediaAssetResource myAsset in myAccount.GetMediaAssets())
            {
                Log(
                   $"{myAsset.Data.AssetId}," +
                   $"{myAsset.Data.Name}," +
                   $"{myAsset.Data.Description}," +
                   $"{myAsset.Data.CreatedOn}," +
                   $"{myAsset.Data.StorageAccountName}," +
                   $"{myAsset.Data.Container}," +
                   $"{myAsset.Data.ResourceType.Type}," +
                   $"{myAsset.Data.StorageEncryptionFormat}," +
                   $"{myAsset.Data.EncryptionScope},"
                  , $"mediaassetslog_{timestamp}.csv");

               
                //Gets the streaminglocators for any assets that have one
                foreach (MediaAssetStreamingLocator myLocator in myAsset.GetStreamingLocators())
                {
                    
                    Log(
                          $"{myAsset.Data.AssetId}," +
                        $"{myAsset.Data.Name}," +
                     $"{myLocator.Name}," +
                     $"{myLocator.StreamingLocatorId}," +
                     $"{myLocator.StreamingPolicyName}," +
                     //$"    Starts On: {myLocator.StartOn}," +
                     $"{myLocator.EndOn}," +
                     $"{myLocator.DefaultContentKeyPolicyName}"
                     , $"streaminglocatorlog_{timestamp}.csv");


                }

                foreach (MediaAssetFilterResource myFilters in myAsset.GetMediaAssetFilters())
                {
                    Log(
                        $"{myAsset.Data.AssetId}," +
                         $"{myAsset.Data.Name}," +
                    $"{myFilters.Data.Name}," 
                   // $"{myFilters.Data.Tracks}," +
                  //  $"{myFilters.Data.FirstQualityBitrate}," +
                   // $"{myFilters.Data.PresentationTimeRange}," 
                    , $"assetfilterslog_{timestamp}.csv");

                }


                //Gets the SAS Storage URIs for the assets that have one
                //foreach (Uri myStorageURIs in myAsset.GetStorageContainerUris()  )
                //{

                //    Log(
                //     $"    URI: {myStorageURIs.AbsoluteUri}\r\n" +
                //     $"    Path: {myStorageURIs.AbsolutePath}\r\n", "mediaassetslog.txt");

                //}



            }

            

        }


        //Gets Streaming Endpoints Info CSV
        public static void GetStreamingEndpointsInfoCSV(MediaServicesAccountResource myAccount)
        {
            
            // csv Headers
            Log("SEName,HostName,CDNEnabled,CDNProfile,CDNProvider,ScaleUnits", $"streamingendpointslog_{timestamp}.csv");


            //Gets the Streaming Endpoints for the media service account
            foreach (StreamingEndpointResource myStreamingEndpoint in myAccount.GetStreamingEndpoints())
            {
                Log(
                    
                     $"{myStreamingEndpoint.Data.Name}," +
                     $"{myStreamingEndpoint.Data.HostName}," +
                     $"{myStreamingEndpoint.Data.IsCdnEnabled}," +
                     $"{myStreamingEndpoint.Data.CdnProfile}," +
                     $"{myStreamingEndpoint.Data.CdnProvider}," +
                     $"{myStreamingEndpoint.Data.ScaleUnits},"
                     , $"streamingendpointslog_{timestamp}.csv");
            }
           
        }

        

        //Gets Transform Information CSV
        public static void GetMediaTransformInfoCSV(MediaServicesAccountResource myAccount)
        {
           
            //Gets the transforms for the media service account
            // csv Headers
            Log("TransformName,Description,CreatedOn", $"mediatransformlog_{timestamp}.csv");
            Log("JobId,JobName,Description,CreatedOn,Priority,Input", $"mediajobslog_{timestamp}.csv");
            Log("JobId,JobName,JobOutput,JobOutputLabel", $"mediajoboutputslog_{timestamp}.csv");

            foreach (MediaTransformResource myTransforms in myAccount.GetMediaTransforms())
            {
                Log(
                     $"{myTransforms.Data.Name}," +
                     $"{myTransforms.Data.Description}," +
                     $"{myTransforms.Data.CreatedOn}"
                     , $"mediatransformlog_{timestamp}.csv");

                foreach (MediaJobResource myJobs in myTransforms.GetMediaJobs())
                {
                    String logData;

                    logData = $"{myJobs.Data.Id}," +
                $"{myJobs.Data.Name}," +
                $"{myJobs.Data.Description}," +
                $"{myJobs.Data.CreatedOn}," +
                $"{myJobs.Data.Priority},";

                    string jobInput = String.Empty;




                    if (myJobs != null && myJobs.Data != null && myJobs.Data.Input != null)
                    {
                        switch (myJobs.Data.Input.GetType().ToString())
                        {
                            case "Azure.ResourceManager.Media.Models.MediaJobInputAsset":
                                {

                                    jobInput = $"{(myJobs.Data.Input as MediaJobInputAsset).AssetName}";

                                    break;
                                };
                            case "Azure.ResourceManager.Media.Models.MediaJobInputClip":
                                {
                                    break;
                                }

                            case "Azure.ResourceManager.Media.Models.MediaJobInputHttp":
                                {

                                    jobInput = $"{(myJobs.Data.Input as MediaJobInputHttp).BaseUri}";

                                    break;
                                }
                            case "Azure.ResourceManager.Media.Models.MediaJobInputSequence":
                                {
                                    break;
                                }
                            case "Azure.ResourceManager.Media.Models.MediaJobInputs":
                                {
                                    break;
                                }
                            default: { break; }
                        }
                    }

                    logData = logData + jobInput;

                    Log(logData, $"mediajobslog_{timestamp}.csv");

                    foreach (MediaJobOutputAsset myJobOutput in myJobs.Data.Outputs)
                    {
                        Log(
                             $"{myJobs.Data.Id}," +
                            $"{myJobs.Data.Name}," +
                            $"{myJobOutput.AssetName}," +
                            $"{myJobOutput.Label},"
                            , $"mediajoboutputslog_{timestamp}.csv");
                    }

                }

            }

            
        }
        
        

        //Gets Live Events Info CSV
        public static void GetMediaLiveEventsInfoCSV(MediaServicesAccountResource myAccount)
        {
            
            //Gets the live events for the media service account
            // csv Headers
            Log("LiveEventName,LiveEventURL,Description,CreatedOn,Encoding", $"liveeventslog_{timestamp}.csv");
           

            foreach (MediaLiveEventResource myLiveEvents in myAccount.GetMediaLiveEvents())
            {
                Log(
                     $"{myLiveEvents.Data.Name}," +
                     $"{myLiveEvents.Data.Id}," +
                     $"{myLiveEvents.Data.Description}," +
                     $"{myLiveEvents.Data.CreatedOn}," +
                     $"{myLiveEvents.Data.Encoding.PresetName},"
                     , $"liveeventslog_{timestamp}.csv");


            }

           
        }


       
        #endregion


        #region TXT Methods 
        static async Task TXTMain()
        {
            // Loading the settings from the appsettings.json file or from the command line parameters
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                //.AddCommandLine(args)
                .Build();
            root = AppDomain.CurrentDomain;

            

            if (!Options.TryGetOptions(configuration, out var options))
            {
                return;
            }


            // TableOfContents();

            Console.WriteLine($"Subscription ID:             {options.subscription}");
            Console.WriteLine($"Resource group name:         {options.resourceGroup}");
            Console.WriteLine($"Media Services account name: {options.mediaServiceAccount}");
            Console.WriteLine();

            // First we construct the ArmClient using DefaultAzureCredential

            //var client = new ArmClient(new DefaultAzureCredential());
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var client = new ArmClient(credential);

            SubscriptionCollection subscriptions = client.GetSubscriptions();
            SubscriptionResource subscription = subscriptions.Get(options.subscription.ToString());
            Console.WriteLine($"Got subscription: {subscription.Data.DisplayName}");

            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(options.resourceGroup);

            // Get all the media accounts in as resource group
            MediaServicesAccountCollection mediaServices = resourceGroup.GetMediaServicesAccounts();


            //Get Media Account details 
            foreach (MediaServicesAccountResource myAccount in mediaServices)
            {
                Console.WriteLine($"name= {myAccount.Data.Name}");
                Console.WriteLine($"location= {myAccount.Data.Location}");

                

                //// Text Output ////
                GetMediaAssetsInfo(myAccount);
                GetStreamingEndpointsInfo(myAccount);
                GetMediaTransformInfo(myAccount);
                GetMediaLiveEventsInfo(myAccount);
                //GetContentKeyPoliciesInfo(myAccount);
                //GetMediaServicesPrivateEndpointConnectionsInfo(myAccount);
                //GetStreamingPoliciesInfo(myAccount);

            }

            Console.WriteLine($"Results will be written to {root.BaseDirectory}AMSreports.  Press any key to close the program.");
            Console.ReadKey();

        }

        //Gets Assets Info TXT
        public static void GetMediaAssetsInfo(MediaServicesAccountResource myAccount)
        {
            Console.WriteLine($"Media Services Account Resource Name= {myAccount.Data.Name}");
            Console.WriteLine($"location= {myAccount.Data.Location}");
            Log("******* Report run " + DateTime.UtcNow.ToString() + " UTC *******", $"mediaassetslog_{timestamp}.txt");
            //Header("Media Assets Information");
            Log(
                $"Media Services Account Resource Name= {myAccount.Data.Name}\r\n" +
                $"Location= {myAccount.Data.Location}\r\n"
                , $"mediaassetslog_{timestamp}.txt");
           
         
            //Gets the assets for the media service account

            foreach (MediaAssetResource myAsset in myAccount.GetMediaAssets())
            {


                // outputs  text file
                Log(
                   $"Asset Id: {myAsset.Data.AssetId}\r\n" +
                   $"Asset Name: {myAsset.Data.Name}\r\n" +
                   $"Description: {myAsset.Data.Description}\r\n" +
                   $"Created On: {myAsset.Data.CreatedOn}\r\n" +
                   $"Storage Account: {myAsset.Data.StorageAccountName}\r\n" +
                   $"Container: {myAsset.Data.Container}\r\n" +
                   $"Resource Type: {myAsset.Data.ResourceType.Type}\r\n" +
                   $"Storage Encryption Format: {myAsset.Data.StorageEncryptionFormat}\r\n" +
                   $"Storage Encryption Scope: {myAsset.Data.EncryptionScope}\r\n"
                  , $"mediaassetslog_{timestamp}.txt");

                //Gets the streaminglocators for any assets that have one
                foreach (MediaAssetStreamingLocator myLocator in myAsset.GetStreamingLocators())
                {
                    Log(
                     $"    Locator Name: {myLocator.Name}\r\n" +
                     $"    Locator Id: {myLocator.StreamingLocatorId}\r\n" +
                     $"    Streaming Policy: {myLocator.StreamingPolicyName}\r\n" +
                     //$"    Starts On: {myLocator.StartOn}\r\n" +
                     $"    Ends On: {myLocator.EndOn}\r\n" +
                     $"    Content Key Policy: {myLocator.DefaultContentKeyPolicyName}"
                     , $"mediaassetslog_{timestamp}.txt");


                }

                foreach (MediaAssetFilterResource myFilters in myAsset.GetMediaAssetFilters())
                {
                    Log(
                    $"    Filter: {myFilters.Data.Name}\r\n"
                    //$"    Tracks: {myFilters.Data.Tracks}\r\n" +
                    //$"    First Quality Bitrate: {myFilters.Data.FirstQualityBitrate}\r\n" +
                    //$"    Presentation TimeRange: {myFilters.Data.PresentationTimeRange}\r\n" 
                    , $"mediaassetslog.txt_{timestamp}");

                }


                //Gets the SAS Storage URIs for the assets that have one
                //foreach (Uri myStorageURIs in myAsset.GetStorageContainerUris()  )
                //{

                //    Log(
                //     $"    URI: {myStorageURIs.AbsoluteUri}\r\n" +
                //     $"    Path: {myStorageURIs.AbsolutePath}\r\n", "mediaassetslog.txt");

                //}



            }
           
          
        }

        
        public static void GetStreamingEndpointsInfo(MediaServicesAccountResource myAccount)
        {
           
            Log("*******Report run " + DateTime.UtcNow.ToString() + " UTC *******", "streamingendpointslog.txt");

            Log(
               $"Media Services Account Resource Name= {myAccount.Data.Name}\r\n" +
               $"Location= {myAccount.Data.Location}\r\n", "streamingendpointslog.txt");


            foreach (StreamingEndpointResource myStreamingEndpoint in myAccount.GetStreamingEndpoints())
            {
                Log(
                     $"    SE Name: {myStreamingEndpoint.Data.Name}\r\n" +
                     $"    Host Name: {myStreamingEndpoint.Data.HostName}\r\n" +
                     $"    CDN Enabled: {myStreamingEndpoint.Data.IsCdnEnabled}\r\n" +
                     $"    CDN Profile: {myStreamingEndpoint.Data.CdnProfile}\r\n" +
                     $"    CDN Provider: {myStreamingEndpoint.Data.CdnProvider}\r\n" +
                     $"    Scale Units: {myStreamingEndpoint.Data.ScaleUnits}\r\n"
                     , "streamingendpointslog.txt");
            }
            
          
        }

       

        //Gets Transform Information TXT
        public static void GetMediaTransformInfo(MediaServicesAccountResource myAccount)
        {
            
            Log("*******Report run " + DateTime.UtcNow.ToString() + " UTC *******", "mediatransformlog.txt");

            Log(
               $"Media Services Account Resource Name= {myAccount.Data.Name}\r\n" +
               $"Location= {myAccount.Data.Location}\r\n", "mediatransformlog.txt");


            foreach (MediaTransformResource myTransforms in myAccount.GetMediaTransforms())
            {
                Log(
                     $"Transform Name: {myTransforms.Data.Name}\r\n" +
                     $"Description: {myTransforms.Data.Description}\r\n" +
                     $"Created On Date: {myTransforms.Data.CreatedOn}\r\n" +
                     $"Outputs: {myTransforms.GetMediaJobs()}\r\n"  //how to we get the collections from this??
                     , "mediatransformlog.txt");

                foreach (MediaJobResource myJobs in myTransforms.GetMediaJobs())
                {
                    Log(
                $"         Job Id: {myJobs.Data.Id}\r\n" +
                $"         Job Name: {myJobs.Data.Name}"
                , "mediatransformlog.txt");
                    Log(

                    $"         Description: {myJobs.Data.Description}\r\n" +
                    $"         Created Date: {myJobs.Data.CreatedOn}\r\n" +
                    $"         Priority: {myJobs.Data.Priority}"
                    , "mediatransformlog.txt");
                    if (myJobs != null && myJobs.Data != null && myJobs.Data.Input != null)
                    {
                        switch (myJobs.Data.Input.GetType().ToString())
                        {
                            case "Azure.ResourceManager.Media.Models.MediaJobInputAsset":
                                {
                                    Log(
                                   $"         Input Asset: {(myJobs.Data.Input as MediaJobInputAsset).AssetName}"
                                   , "mediatransformlog.txt");
                                    break;
                                };
                            case "Azure.ResourceManager.Media.Models.MediaJobInputClip":
                                {
                                    break;
                                }

                            case "Azure.ResourceManager.Media.Models.MediaJobInputHttp":
                                {
                                    Log(
                                   $"         Input URI: {(myJobs.Data.Input as MediaJobInputHttp).BaseUri}"
                                   , "mediatransformlog.txt");
                                    break;
                                }
                            case "Azure.ResourceManager.Media.Models.MediaJobInputSequence":
                                {
                                    break;
                                }
                            case "Azure.ResourceManager.Media.Models.MediaJobInputs":
                                {
                                    break;
                                }
                            default: { break; }
                        }
                    }

                    foreach (MediaJobOutputAsset myJobOutput in myJobs.Data.Outputs)
                    {
                        Log(
                            $"         Output: {myJobOutput.AssetName}\r\n" +
                            $"         Label: {myJobOutput.Label}\r\n"
                            , "mediatransformlog.txt");
                    }







                }




            }
           
        }

        

        //Gets Live Events Info TXT
        public static void GetMediaLiveEventsInfo(MediaServicesAccountResource myAccount)
        {
            
            Log("*******Report run " + DateTime.UtcNow.ToString() + " UTC *******", "liveeventslog.txt");

            Log(
               $"Media Services Account Resource Name= {myAccount.Data.Name}\r\n" +
               $"Location= {myAccount.Data.Location}\r\n", "liveeventslog.txt");


            foreach (MediaLiveEventResource myLiveEvents in myAccount.GetMediaLiveEvents())
            {


                Log(
                     $"    Live Event Name: {myLiveEvents.Data.Name}\r\n" +
                     $"    Live Event URL: {myLiveEvents.Data.Id}\r\n" +
                     $"    Description: {myLiveEvents.Data.Description}\r\n" +
                     $"    Created Date: {myLiveEvents.Data.CreatedOn}\r\n" +
                     $"    Encoding: {myLiveEvents.Data.Encoding.PresetName}\r\n"
                     , "liveeventslog.txt");


            }
            
            
        }


      


        #endregion




        #region JSON Methods
        static async Task JSONMain()
        {

            try
            {
                Program obj = new Program();
                obj.config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

                Console.WriteLine("How many storage accounts do you have configured to your AMS account?");
                obj.storageCount = Int32.Parse(Console.ReadLine());

                int count = 0;

                for (int i = 0; i < obj.storageCount; i++)
                {
                    count++;
                    Console.WriteLine("Enter storage account #" + count + ":");
                    string storageName = Console.ReadLine();

                    Console.WriteLine("Enter connection string for storage account #" + count + ":");
                    string storageConnectionString = Console.ReadLine();

                    obj.storageAccounts.Add(storageName, storageConnectionString);

                    Console.WriteLine();
                }

                var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
                var client = new ArmClient(credential);

                SubscriptionCollection subscriptions = client.GetSubscriptions();
                SubscriptionResource subscription = subscriptions.Get(obj.config.subscription);

                ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
                ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(obj.config.resourceGroup);

                // Get all the media accounts in as resource group
                MediaServicesAccountCollection mediaServices = resourceGroup.GetMediaServicesAccounts();


                //Get Media Account details 
                foreach (MediaServicesAccountResource myAccount in mediaServices)
                {
                    if(myAccount.Data.Name.Equals(obj.config.mediaServiceAccount))
                    {
                        //get Assets
                        await obj.getAssets(myAccount);

                        obj.outputObj.value = obj.assetlist;
                        Console.WriteLine();
                        Console.WriteLine(obj.outputObj); //output json 


                       
                        //Header("Media Assets Information");
                        Log("******* Report run " + DateTime.UtcNow.ToString() + " UTC *******\r\n", "mediaassetslog.json");

                        Log(
                            obj.outputObj.ToString()
                            , $"mediaassetslog_{timestamp}.json");

                        


                    }

                }

                

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine($"Results will be written to {root.BaseDirectory}AMSreports.  Press any key to close the program.");
            Console.ReadKey();
        }


        public async Task getAssets(MediaServicesAccountResource myAccount)
        {
           
            foreach (MediaAssetResource myAsset in myAccount.GetMediaAssets())
            {
                Console.WriteLine(myAsset.Data.Name + "  " + myAsset.Data.Container + "  " + myAsset.Data.StorageAccountName+ "  "+ myAsset.Data.EncryptionScope);
                

                List<streamingLocators> locatorslist = new List<streamingLocators>();

                locatorslist = await getStreamingLocators(myAsset, myAccount); //get streaming locator details 
                bool isencoded = await isEncoded(myAsset.Data.Container, myAsset.Data.StorageAccountName);
                assetlist.Add(new assetData
                {
                    assetName = myAsset.Data.Name,
                    assetContainerName = myAsset.Data.Container,
                    assetDescription = myAsset.Data.Description,  //handles null property values
                    assetCreationDate = myAsset.Data.CreatedOn.ToString(),
                    assetStorageEncryptionFormat = myAsset.Data.StorageEncryptionFormat.ToString(),
                    assetStorageAccount = myAsset.Data.StorageAccountName,
                    encoded = isencoded,
                    streamingLocators = locatorslist
                    

                });
            }

        }

        public async Task<List<streamingLocators>> getStreamingLocators(MediaAssetResource myAsset, MediaServicesAccountResource myAccount)
        {
            List<streamingLocators> locatorslist = new List<streamingLocators>();

            foreach (MediaAssetStreamingLocator myLocator in myAsset.GetStreamingLocators())
            {
                locatorslist.Add(new streamingLocators { name = myLocator.Name, ID = myLocator.StreamingLocatorId.ToString(), streamingPolicy = myLocator.StreamingPolicyName });
            }

            return locatorslist;

        }

        public async Task<bool> isEncoded(string containerName, string storageAccountName)
        {
            BlobServiceClient blobServiceClient;

            string connectionString;
            storageAccounts.TryGetValue(storageAccountName, out connectionString);
            blobServiceClient = new BlobServiceClient(connectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = containerClient.GetBlobs();
            bool isencoded = false;

            foreach (var blob in blobs)
            {
                if (blob.Name.Contains(".ism") && !blob.Name.Contains(".ismc"))
                {

                    isencoded = true;
                }

            }

            return isencoded;
        }


        #endregion


        //Sets up the log file output
        public static void Log(string msg, string filename)
        {

            Console.WriteLine(msg);

            DirectoryInfo filepath  = new DirectoryInfo(root.BaseDirectory + $"//AMSreports//");
            if (!filepath.Exists)
                filepath.Create();

            StreamWriter sw = File.AppendText(root.BaseDirectory + $"//AMSreports//{filename}");//filepath for output
            try
            {
                sw.WriteLine(msg);
            }
            finally
            {
                sw.Close();
            }

        }

    }

    /// Class to manage the settings which come from appsettings.json or command line parameters.
    internal class Options
    {
        [Required]
        public Guid? subscription { get; set; }

        [Required]
        public string? resourceGroup { get; set; }

        [Required]
        public string? mediaServiceAccount { get; set; }


        static public bool TryGetOptions(IConfiguration configuration, [NotNullWhen(returnValue: true)] out Options? options)
        {
            try
            {
                options = configuration.Get<Options>() ?? throw new Exception("No configuration found. Configuration can be set in appsettings.json or using command line options.");
                Validator.ValidateObject(options, new ValidationContext(options), true);
                return true;
            }
            catch (Exception ex)
            {
                options = null;
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }


}
















//// Get a specific media account 
//MediaServicesAccountResource mediaService = await resourceGroup.GetMediaServicesAccountAsync(options.mediaServiceAccount);

//Console.WriteLine($"Got media service : {mediaService.Data.Name}");



