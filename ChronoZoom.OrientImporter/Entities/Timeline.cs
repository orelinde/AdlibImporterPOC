using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orient.Client;

namespace ChronoZoom.OrientImporter.Entities
{
    public class Timeline
    {
        public ORID Id { get; set; }
        public decimal BeginDate { get; set; }
        public decimal EndDate { get; set; }
        public string Title { get; set; }
        public List<ContentItem> ContentItems { get; set; } 
    }
}
