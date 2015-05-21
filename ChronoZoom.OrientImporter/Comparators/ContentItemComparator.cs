using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChronoZoom.OrientImporter.Entities;

namespace ChronoZoom.OrientImporter.Comparators
{
    public class ContentItemComparator : IComparer<ContentItem>
    {


        public int Compare(ContentItem x, ContentItem y)
        {
            var totalX = 0;
            var totalY = 0;

            totalX = (int) (x.EndDate - x.BeginDate);
            totalY = (int) (y.EndDate - y.BeginDate);

            if (totalX > totalY) return 1;
            if (totalX < totalY) return -1;
            if (totalX == totalY)
            {
               // if (x.BeginDate < y.BeginDate) return -1;
                return 0;
            }
            //if (totalY < totalX) return -1;
            //if (x.BeginDate > y.BeginDate) return -1;
            //if (x.BeginDate < y.BeginDate) return +1;
            return  0;
            
        }
    }
}
