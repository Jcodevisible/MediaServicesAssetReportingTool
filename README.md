# Media-Services-Asset-Reporting-Tool
The Azure Media Services Asset Reporting Tool is a tool that extracts data from the Azure Media Services account using the Azure.ResourceManager.Media SDK library of the V3 API. It then reports information about assets to CSV, TXT, or JSON formats.

Project Name: Azure Media Services Reporting Tool  

Language:  Csharp  

Products:  Azure Media Services  

Authors: Jameela Esa, Dania Chavez, David Bristol 

  

  

# Overview 

This project extracts the assets & related data from the Azure Media Services account of the subscription using the Azure.ResourceManager.Media sdk library of the V3 API.  It enumerates through the accounts and pulls the assets and related data and outputs it onto a series of TXT files. 

  

  

## Prerequisites 

-Have an Azure account with an active subscription 

-Have an Azure Media Services resource  

-Visual Studio (2019 and above) 

-.NET core 7.0 

-Log in to your Azure account from the command line using the Az login command 

  

## Code structure 

  

- Entry point for running the program - ./Program.cs 

- Output location of files - \MediaServicesReportingTool\MediaServicesReportingTool\bin\Debug\net7.0  

  

## Before running the sample for the first time 

  

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent program and navigate to the directory that you'd like to clone the sample to. 

2. `git clone <add URL> 

  

### Locally configuring and running the application 

  

Open the `MediaServicesReportingTool.sln` solution in Visual Studio. 

Locate and open the appsettings.json file to configure the following settings: 

(Note: The values can be obtained from the API Access page for your Media Services account in the portal.) 

For the application code, locate and open the `program.cs` file. 

Build and Run the project 

You will be prompted to choose which output type (CSV, TXT, JSON) you want to output the report to.  There may be additional prompts depending on the format. 

Once the program has completed, navigate to the bin/Debug/net7.0/AMSReports directory to retrieve the output files. 

  

## Output 

The code will export a report in three file formats : JSON, TXT, and CSV.   

  

  

## JSON Language Files 

JSON output: 

While the JSON is being processed, the asset name asset container and the storage account where it is being stored is being output to the console.  

 

Example: 

AssetName1 containerName1 storageAccount1 

AssetName2 containerName2 storageAccount1 

AssetName3 containerName3 storageAccount2 

 

After all the data has been processed a JSON string will be output to the console, and you will want to copy it.  You can use a JSON formatter online to format it.  

 

Example: 

 

 

  

  

  

The exported data is stored in  the BIN folder there is an AMSreports subfolder. 

File names correspond to locales, e.g., `reportname.txt`, `reportname.csv`. 

  

  

## Feedback 

We appreciate your feedback and energy in helping us improve our services. Please let us know if you are satisfied with this tool. 

 
