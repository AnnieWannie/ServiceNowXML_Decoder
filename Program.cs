using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ServiceNowXMLToHTML.Models;
using Serilog;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.Design;
using System.IO.Compression;
using System.Net.Mail;

namespace ServiceNowXMLToHTML
{
    class Program
    {
        static void Main()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(ConfigModel.Config["LogOutput"], rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                string sInputXMLFolder = ConfigModel.Config["InputFolder"];

                DirectoryInfo InputXMLFldir = new DirectoryInfo(sInputXMLFolder);

                Log.Information("******* ServiceNowXMLToHTML has begun *******");

                if (InputXMLFldir.GetFiles("*.xml").Length > 0)
                {
                    Log.Information("XML files detected now generating html files...");

                    string[] files = Directory.GetFiles(sInputXMLFolder, "*.xml", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        ServiceNowXMLtoHTML.GenerateHtmlFromXml(file);
                    }                 
                }
                Log.Information("******* ServiceNowXMLToHTML has completed successfully *******");
            }
            catch (Exception ex)
            {
                Log.Information("!!! An error has occured run incomplete: {0}", ex.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

       
    }

}


