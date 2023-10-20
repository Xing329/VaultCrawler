using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace VaultDataCrawlerNF
{
    // Container class to wrap the list of File objects
    [XmlRoot("FileList")]
    public class FileListContainer
    {
        [XmlElement("File")]
        public List<Autodesk.Connectivity.WebServices.File> Files { get; set; }

        public FileListContainer()
        {
            Files = new List<Autodesk.Connectivity.WebServices.File>();
        }
    }
    internal class XmlHelper
    {
        public void ExportToXml(List<Autodesk.Connectivity.WebServices.File> fileList, string filePath)
        {
            // Wrap the list of File objects in a container class
            FileListContainer fileListContainer = new FileListContainer { Files = fileList };

            XmlSerializer serializer = new XmlSerializer(typeof(FileListContainer));

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                // Serialize the container class to XML
                serializer.Serialize(fs, fileListContainer);
            }
        }

    }
}
