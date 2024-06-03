using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceNowXMLToHTML.Models
{
    public class IncidentDetails
    {
        public string SysCreatedDate { get; set; }
        public string IncNo { get; set; }
        public string Incident { get; set; }
        public string SysJournal { get; set; }
        public string SysAttachment { get; set; }
        public string SysAttachmentDocument { get; set; }
        public string ChildIncident { get; set; }
        public string attachmentHash { get; set; }
        public Dictionary<string, byte[]> SysAttachmentByteData { get; set; }
    }
}
