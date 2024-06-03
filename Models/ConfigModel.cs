using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceNowXMLToHTML.Models
{
    public static class ConfigModel
    {
        public static IConfiguration Config { get; set; }
        
        static ConfigModel()
        {
            Config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();              
        }
    }
}
