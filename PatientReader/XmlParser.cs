using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace PatientReader;

internal class XmlParser
{
    XmlDocument xmlDoc;
    XmlElement root;
    Dictionary<string, string> attributes;

    public XmlParser(string xmlString)
    {
        try
        {
            this.xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement == null)
                throw new ArgumentException();

            this.root = xmlDoc.DocumentElement;
            this.attributes = new Dictionary<string, string>();

        }
        catch (ArgumentException ex)
        {
            Console.WriteLine("xmlDoc is null. Exception message: {0} ", ex.Message);
            throw;
        }

    }

    public Dictionary<string, string> getData()
    {
        return extractDataFromXml();
    }
    private Dictionary<string, string> extractDataFromXml()
    {
        try
        {
            XmlNode date = root;
            XmlNode patientID = root.SelectSingleNode("id");
            XmlNode name = root.SelectSingleNode("name");

            if (name == null || patientID == null)
                throw new Exception("Wrong value! An Xml object is null");


            attributes.Add("patientID", patientID.InnerText);
            attributes.Add("name", name.InnerText);

            return attributes;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Some object appears to be null. Error message: {0}", ex.Message);
            throw;
        }
    }
}
