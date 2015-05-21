using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChronoZoom.OrientImporter.Adlib
{
    public class adlibXML
    {
        [XmlArrayItem("record")]
        public record[] recordList;

        public diagnostic diagnostic { get; set; }
    }

    public class record
    {
        [XmlAttribute]
        public int priref { get; set; }

        public string title { get; set; }

        [XmlElement("reproduction")]
        public reproduction[] reproductions;

        [XmlElement("production.date.start")]
        public string productiondateBegin { get; set; }

        [XmlElement("production.date.end")]
        public string productiondateEnd { get; set; }
    }

    public class diagnostic
    {
        public int hits { get; set; }
        public int limit { get; set; }
    }

    public class reproduction
    {
        [XmlElement("reproduction.identifier_URL")]
        public string identifier { get; set; }

        [XmlElement("reproduction.reference")]
        public string reference { get; set; }
    }
}
