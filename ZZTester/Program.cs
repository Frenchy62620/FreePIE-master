using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using System.IO;
using System.Xml;

namespace ZZTester
{
    public class PasswordSettings
    {
        public string customerRef { get; set; }
        public string node { get; set; }
        public string name { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public string fileType { get; set; }
    }
    class Program
    {
        public static List<PasswordSettings> Logins = new List<PasswordSettings>();

        static void Main(string[] args)
        {

            PasswordSettings settings = new PasswordSettings();
            settings.customerRef = "abc";
            settings.name = "test";
            Logins.Add(settings);
            settings = new PasswordSettings();
            settings.customerRef = "def";
            settings.name = "test5";
            Logins.Add(settings);

            foreach (var login in Logins)
            {
                if (!File.Exists("e:\\Test.xml"))
                {
                    XDocument doc =
                        new XDocument(
                            new XElement("PasswordSettings",
                                new XElement("Logins",
                                    new XElement("customerRef", login.customerRef),
                                    new XElement("name", login.name)
                                )
                            )
                        );

                    doc.Save("e:\\Test.xml");

                    /*
                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                    xmlWriterSettings.Indent = true;
                    xmlWriterSettings.NewLineOnAttributes = true;
                    using (XmlWriter xmlWriter = XmlWriter.Create("E:\\Test.xml", xmlWriterSettings))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement("PasswordSettings");

                        xmlWriter.WriteStartElement("Logins");
                        xmlWriter.WriteElementString("customerRef", login.customerRef);
                        xmlWriter.WriteElementString("name", login.name);
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndDocument();
                        xmlWriter.Flush();
                        xmlWriter.Close();
                    }
                    */
                }
                else
                {
                    XDocument xDocument = XDocument.Load("e:\\Test.xml");
                    XElement root = xDocument.Element("PasswordSettings");
                    IEnumerable<XElement> rows = root.Descendants("Logins");
                    XElement firstRow = rows.Last();
                    firstRow.AddAfterSelf(
                        new XElement("Logins",
                            new XElement("customerRef", login.customerRef),
                            new XElement("name", login.name)));
                    xDocument.Save("e:\\Test.xml");
                }
            }
        }
    }

}