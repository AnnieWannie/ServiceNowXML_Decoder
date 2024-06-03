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
                        GenerateHtmlFromXml(file);
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

        #region Main Process
        static void GenerateHtmlFromXml(string xmlFilePath)
        {
            try
            {
                string sOutputHTMLFolderRoot = ConfigModel.Config["OutputFolder"];
                string sOutputSubFolderYear = "";
                string sOutputSubFolderMonth = "";
                //string sOutputSubFolderDay = "";
                string outputLoc = "";
                string outputFile = "";

                Dictionary<string, string> monthsDict = new Dictionary<string, string> {
                {"01","January"},
                {"02","February"},
                {"03","March"},
                {"04","April"},
                {"05","May"},
                {"06","June"},
                {"07","July"},
                {"08","August"},
                {"09","September"},
                {"10","October"},
                {"11","November"},
                {"12","December"}
            };

                int creationCount = 0;

                string html = "";

                XDocument xmlDoc = new XDocument();
                xmlDoc = XDocument.Load(xmlFilePath);

                IncidentDetails fd = new IncidentDetails();

                var unloadNode = xmlDoc.Element("unload");

                foreach (var node in unloadNode.Elements())
                {
                    if (node.Name == "incident")
                    {
                        if (fd.Incident is not null)
                        {
                            html = @"
                                    <html>
                                    <head>
                                    <style>
                                    div {
                                        width: 100%;
                                        word-break: break-all;
                                            }
                                    </style>
                                    </head>
                                    <body>
                                    <div>
                                    ";
                            html += fd.Incident + fd.SysJournal + fd.SysAttachment + fd.SysAttachmentDocument + fd.ChildIncident;
                            html += @"</div></body></html>";

                            sOutputSubFolderYear = fd.SysCreatedDate.Substring(0, 4);
                            sOutputSubFolderMonth = monthsDict[fd.SysCreatedDate.Substring(5, 2)];
                            //sOutputSubFolderDay = fd.SysCreatedDate.Substring(8, 2);

                            Directory.CreateDirectory(sOutputHTMLFolderRoot + $"\\{sOutputSubFolderYear}" + $"\\{sOutputSubFolderMonth}\\" + fd.SysCreatedDate + "_" + fd.IncNo);
                            outputLoc = sOutputHTMLFolderRoot + $"\\{sOutputSubFolderYear}" + $"\\{sOutputSubFolderMonth}\\" + fd.SysCreatedDate + "_" + fd.IncNo;
                            outputFile = outputLoc + "\\" + fd.SysCreatedDate + "_" + fd.IncNo + ".html";
                            File.WriteAllText(outputFile, html);

                            if (fd.SysAttachmentByteData is not null)
                            {
                                foreach (var attachFile in fd.SysAttachmentByteData)
                                {
                                    byte[] decodedBytes = attachFile.Value;
                                    DecompressAndWriteToFile(attachFile.Value, outputLoc, attachFile.Key);
                                }
                            }

                            creationCount++;
                            Log.Information("HTML for {0} generated. Total generated: {1}", fd.IncNo, creationCount);

                            fd = new IncidentDetails();
                        }

                        fd.Incident += ProcessIncidentNode(node, fd);
                    }
                    else if (node.Name == "sys_journal_field")
                    {
                        fd.SysJournal += ProcessSysJournal(node, fd);
                    }
                    else if (node.Name == "sys_attachment")
                    {
                        fd.SysAttachment += ProcessSysAttachment(node, fd);
                    }
                    else if (node.Name == "sys_attachment_doc")
                    {
                        ProcessSysAttachmentDocument(node, fd);
                    }
                    else
                    {
                        throw new Exception();
                    }

                }
                html = @"
                        <html>
                        <head>
                        <style>
                        div {
                            width: 100%;
                            word-break: break-all;
                                }
                        </style>
                        </head>
                        <body>
                        <div>
                            ";
                html += fd.Incident + fd.SysJournal + fd.SysAttachment + fd.SysAttachmentDocument + fd.ChildIncident;
                html += @"</div></body></html>";

                sOutputSubFolderYear = fd.SysCreatedDate.Substring(0, 4);
                sOutputSubFolderMonth = monthsDict[fd.SysCreatedDate.Substring(5, 2)];
                //sOutputSubFolderDay = fd.SysCreatedDate.Substring(8, 2);

                Directory.CreateDirectory(sOutputHTMLFolderRoot + $"\\{sOutputSubFolderYear}" + $"\\{sOutputSubFolderMonth}\\" + fd.SysCreatedDate + "_" + fd.IncNo);
                outputLoc = sOutputHTMLFolderRoot + $"\\{sOutputSubFolderYear}" + $"\\{sOutputSubFolderMonth}\\" + fd.SysCreatedDate + "_" + fd.IncNo;
                outputFile = outputLoc + "\\" + fd.SysCreatedDate + "_" + fd.IncNo + ".html";
                File.WriteAllText(outputFile, html);

                if (fd.SysAttachmentByteData is not null)
                {
                    foreach (var attachFile in fd.SysAttachmentByteData)
                    {
                        byte[] decodedBytes = attachFile.Value;
                        DecompressAndWriteToFile(attachFile.Value, outputLoc, attachFile.Key);
                    }
                }

                // **************************** FOR TESTING - parsing attachments xml dump ************************* 
                //sOutputSubFolderYear = "2028";
                //sOutputSubFolderMonth = "June";
                //Directory.CreateDirectory(sOutputHTMLFolderRoot + $"\\{sOutputSubFolderYear}" + $"\\{sOutputSubFolderMonth}");
                //outputLoc = sOutputHTMLFolderRoot + $"\\{sOutputSubFolderYear}" + $"\\{sOutputSubFolderMonth}";
                //outputFile = outputLoc + "\\" + fd.attachmentHash + ".html";

                //File.WriteAllText(outputFile, html);

                creationCount++;
                Log.Information("HTML for {0} generated. Total generated: {1}", fd.IncNo, creationCount);
            }
            catch (Exception ex)
            {
                Log.Information("An error has occured while parsing {0} please ensure XML is formatted correctly", xmlFilePath);
                throw;
            }
        }
        #endregion

        #region Incident
        static string ProcessIncidentNode(XElement node, IncidentDetails fd)
        {
            try
            {
                string processedIncident = "";
                processedIncident += $"<h1>" + node.Element("number").Value + "</h1>";
                processedIncident += $"<p>";


                fd.IncNo = node.Element("number").Value;

                foreach (XElement child in node.Elements())
                {
                    if (child.Value != "")
                    {
                        if (child.Name == "sys_created_on" && fd.SysCreatedDate is null)
                        {
                            fd.SysCreatedDate = child.Value.Substring(0, 10);
                            processedIncident += $"<b>{child.Name}</b>: {child.Value}<br>";
                        }
                        else if (child.Name == "child_incidents")
                        {
                            fd.ChildIncident = $"<h2><b><u>{child.Name}</u></b>: {child.Value.Replace("\n", "<br>")}</h2>";
                        }
                        else
                        {
                            if (child.Attribute("display_value") is not null)
                                processedIncident += $"<b>{child.Name}</b>: {child.Attribute("display_value").Value} <br>";
                            else
                                processedIncident += $"<b>{child.Name}</b>: {child.Value.Replace("\n", "<br>")}<br>";
                        }
                    }
                }
                processedIncident += "</p>";
                return processedIncident;
            }
            catch (Exception ex)
            {
                Log.Information("An error has occured generating HTML for {0}", fd.IncNo);
                throw;
            }
        }
        #endregion

        #region Sys_Journal
        static string ProcessSysJournal(XElement node, IncidentDetails fd)
        {
            try
            {
                string processedSysJournal = $"";
                processedSysJournal += $"<h2><strong><u>{node.Element("element").Value}</u></strong></h2>";
                processedSysJournal += $"<p>";

                foreach (XElement child in node.Elements())
                {
                    if (child.Value != "")
                    {
                        if (child.Name == "value")
                        {
                            processedSysJournal += $@"<b>{child.Name}</b>: {(child.Value).Replace("\n", "<br>")}<br>";
                        }
                        else
                        {
                            processedSysJournal += $"<b>{child.Name}</b>: {(child.Value).Replace("\n", "<br>")}<br>";
                        }

                    }
                }

                processedSysJournal += $"</p>";
                return processedSysJournal;
            }
            catch (Exception ex)
            {
                Log.Information("An error has occured generating HTML for {0}", fd.IncNo);
                throw;
            }
        }
        #endregion

        #region Sys_Attachment
        static string ProcessSysAttachment(XElement node, IncidentDetails fd)
        {
            fd.attachmentHash = node.Element("hash").Value;
            try
            {
                string processedSysAttachment = $"<h2><strong><u>Attachments</u></strong></h2>";
                processedSysAttachment += $"<p>";

                foreach (XElement child in node.Elements())
                {
                    if (child.Value != "")
                    {
                        processedSysAttachment += $"<b>{child.Name}</b>: {(child.Value).Replace("\n", "<br>")}<br>";
                    }
                }

                processedSysAttachment += "</p>";
                return processedSysAttachment;
            }
            catch (Exception ex)
            {
                Log.Information("An error has occured generating HTML for {0}", fd.IncNo);
                throw;
            }
        }
        #endregion

        #region Sys_Attachment_Document
        static void ProcessSysAttachmentDocument(XElement node, IncidentDetails fd)
        {
            try
            {
                string attachmentName = node.Element("sys_attachment").Attribute("display_value").Value;
                byte[] attachmentData = Convert.FromBase64String(node.Element("data").Value);
                

                if (fd.SysAttachmentByteData is null) {
                    fd.SysAttachmentByteData = new Dictionary<string, byte[]> { { attachmentName, attachmentData } };
                }
                else
                {
                    if (fd.SysAttachmentByteData.ContainsKey(attachmentName))
                    {
                        byte[] tempBytes = new byte[(fd.SysAttachmentByteData[attachmentName].Length + attachmentData.Length)];
                        Buffer.BlockCopy(fd.SysAttachmentByteData[attachmentName], 0, tempBytes, 0, fd.SysAttachmentByteData[attachmentName].Length);
                        Buffer.BlockCopy(attachmentData, 0, tempBytes, fd.SysAttachmentByteData[attachmentName].Length, attachmentData.Length);
                        fd.SysAttachmentByteData[attachmentName] = tempBytes;
                    }
                    else
                    {
                        fd.SysAttachmentByteData.Add(attachmentName, attachmentData);
                    }                  
                }

                // *********** Appends child content of sys_Attach_doc node to html may or may not be needed (info overload for user) *********************

                //string processedSysAttachmentDoc = $"<h3><strong><u>Attachment Document</u></strong></h3>";
                //processedSysAttachmentDoc += $"<p>";
                
                
                //foreach (XElement child in node.Elements())
                //{
                //    if (child.Value != "")
                //    {
                //        if (child.Name == "sys_attachment")
                //        {

                //            processedSysAttachmentDoc += $"<b>{child.Name}</b>: {child.Attribute("display_value").Value}<br>";
                //        }
                //        else if (child.Name == "data")
                //        {
                //            continue;
                //        }
                //        else
                //        {
                //            processedSysAttachmentDoc += $"<b>{child.Name}</b>: {(child.Value).Replace("\n", "<br>")}<br>";
                //        }
                //    }
                //}

                //processedSysAttachmentDoc += "</p>";
                
            }
            catch (Exception ex)
            {
                Log.Information("An error has occured generating HTML for {0}", fd.IncNo);
                throw;
            }
        }
        #endregion
        static void DecompressAndWriteToFile(byte[] compressedBytes, string targetDirectory, string fileName)
        {
            Directory.CreateDirectory(targetDirectory);

            string filePath = Path.Combine(targetDirectory, fileName);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                using (MemoryStream compressedMemoryStream = new MemoryStream(compressedBytes))
                using (GZipStream decompressionStream = new GZipStream(compressedMemoryStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(fileStream);
                }
            }
        }
    }

}


