using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ChronoZoom.OrientImporter.Entities;
using System.Net.Http;
using System.Threading;
using System.Xml.Serialization;
using ChronoZoom.OrientImporter.Adlib;
using ChronoZoom.OrientImporter.Comparators;
using ChronoZoom.OrientImporter.Data;
using Newtonsoft.Json;
using Orient.Client;

namespace ChronoZoom.OrientImporter
{
    class Program
    {
        private static string HOST = "http://www.kompili.com";
        private static int PORT = 2424;
        private static string USERNAME = "***";
        private static string PASSWORD = "*** ";
        private static string DATABASE = "chronozoom";
        //private static string APIURL = "http://amdata.adlibsoft.com/wwwopac.ashx?database=AMlibrary&search=all&limit=47668&output=json";
        private static string APIURL = "http://amdata.adlibsoft.com/wwwopac.ashx?database=AMcollect&search=all&limit=100";
        private static Stopwatch time = new Stopwatch();
        private static ORID timelineORID;
        static void Main(string[] args)
        {
            Console.WriteLine("Begin program");

            CreateDatabaseIfNotExists();
            string json = DownloadApiDataAsJson();

            adlibXML dataToAdlibObjects = ParseDataToAdlibObjects(json);
            var tuple = ParseAdlibDataToChronozoomObjects(dataToAdlibObjects);
            timelineORID = WriteTimelineToDatabase(tuple.Item1); //item 1 = timeline
            CreateTreeOfContentItemsAndAddToDb(tuple.Item2.ToList());
            Console.ReadKey();
        }

        private static void CreateDatabaseIfNotExists()
        {
            Console.WriteLine("Create Database");
            time.Restart();
            var db = new OrientDao();
            db.Connect(HOST, PORT, USERNAME, PASSWORD);
            var created = db.CreateDatabase("chronozoom");
            db.Connect(HOST, PORT, USERNAME, PASSWORD, DATABASE); // Connect to database
            if (created)
            {
                db.CreateClassesInDatabase();
            }
            time.Stop();
            Console.WriteLine(created ? "Create Database : Created and took: " + time.Elapsed.Duration() : "Create Database : Failed - Already exists and took: " + time.Elapsed.Duration());
            db.Disconnect();
        }

        private static ORID WriteTimelineToDatabase(Timeline timeline)
        {
            ORID orid = null;
            Console.WriteLine("Write timeline to database");
            time.Restart();
            var db = new OrientDao();
            db.Connect(HOST, PORT, USERNAME, PASSWORD, DATABASE);
            orid = db.AddTimeline(timeline);
            db.Disconnect();
            time.Stop();
            Console.WriteLine("Write timeline to database : Done and took " + time.Elapsed.Duration());
            return orid;
        }

        private static adlibXML ParseDataToAdlibObjects(string data)
        {
            Console.WriteLine("Begin parsing data");
            time.Reset(); time.Start();

            var xmlSerializer = new XmlSerializer(typeof(adlibXML));
            StringReader reader = new StringReader(data);
            var adlibData = (adlibXML)xmlSerializer.Deserialize(reader);
            Console.WriteLine("Begin parsing data : Done and took " + time.Elapsed.Duration());
            Console.WriteLine("Begin parsing data : Done and parsed " + adlibData.diagnostic.limit + " / " + adlibData.diagnostic.hits + " items");
            return adlibData;
        }

        private static Tuple<Timeline, ContentItem[]> ParseAdlibDataToChronozoomObjects(adlibXML adlibData)
        {
            Console.WriteLine("Parse adlib data to chronozoom objects : start");
            time.Restart();

            Timeline timeline = new Timeline();
            timeline.Title = "Test timeline";
            timeline.BeginDate = 250;
            timeline.EndDate = 2030;
            var contentItems = new ContentItem[adlibData.recordList.Count()];

            for (int i = 0; i < adlibData.recordList.Length; i++)
            {
                record record = adlibData.recordList[i];
                int beginDate = 0;
                int endDate = 0;

                if (record.productiondateBegin != null)
                {
                    int.TryParse(record.productiondateBegin, out beginDate);
                }

                if (record.productiondateEnd != null)
                {
                    int.TryParse(record.productiondateEnd, out endDate);
                }

                contentItems[i] = new ContentItem()
                {
                    BeginDate = beginDate,
                    EndDate = endDate,
                    Title = record.title,
                    Id = record.priref
                };
                if (record.reproductions == null) continue;
                if (record.reproductions.Any()) contentItems[i].Source = record.reproductions[0].identifier;
            }

            time.Stop();
            Console.WriteLine("Parse adlib data to chronozoom objects : end and took " + time.Elapsed.Duration());
            timeline.BeginDate = contentItems.Min(x => x.BeginDate);
            timeline.EndDate = contentItems.Max(x => x.EndDate);
            var tuple = new Tuple<Timeline, ContentItem[]>(timeline, contentItems);
            return tuple;
        }

        private static void CreateTreeOfContentItemsAndAddToDb(List<ContentItem> contentItems)
        {
            // Console.WriteLine("Sort items by date");
            // time.Restart();
            // var sorted = contentItems.OrderBy(x => x.BeginDate).ThenByDescending(x => x.EndDate);
            // time.Stop();
            // Console.WriteLine("Sort items by date : ended and took " + time.Elapsed.Duration());
            //Create tree
            var db = new OrientDao();
            db.Connect(HOST, PORT, USERNAME, PASSWORD, DATABASE);

            // ORID lastOrid = null;
            // ContentItem lastItem = null;

            Console.WriteLine("Start adding items to DB ");
            time.Restart();

            for (int i = 0; i <= 5; i++)
            {
                Console.WriteLine("Processing : depth {0}", i);
                AddContentItems(contentItems, db);
                GroupContentItems(db.GetContext(), i);
            }
            //foreach (var contentItem in contentItems)
            //{
            //    var addContentItem = db.AddContentItem(contentItem);
            //    var id = addContentItem.RID;
            //}
            time.Stop();
            Console.WriteLine("Start adding items to DB : ended and took " + time.Elapsed.Duration());
        }

        private static void AddContentItems(List<ContentItem> contentItems, OrientDao db)
        {
            foreach (var contentItem in contentItems)
            {
                db.AddContentItem(contentItem);
            }
        }

        private static void GroupContentItems(ODatabase db, int depth)
        {
            List<ContentItem> possibleParentContentItems = db.Select().From("ContentItem").Where("Depth").Equals(depth).ToList<ContentItem>();
            possibleParentContentItems.OrderByDescending(r=> r.EndDate - r.BeginDate);

            List<ContentItem> parents = new List<ContentItem>();
            foreach (var item in possibleParentContentItems)
            {
                if (parents.Count() == 0 || item.EndDate < parents.Min(x => x.BeginDate) || item.BeginDate > parents.Max(x => x.EndDate)
                || parents.All(x => item.EndDate < x.BeginDate || item.BeginDate > x.EndDate))
                {
                    parents.Add(item);
                }
            }

            //context.Configuration.AutoDetectChangesEnabled = true;
            int[] parentIDs = parents.Select(x => x.Id).ToArray();

            foreach (var parent in parents)
            {
                var contentItems =
                    db.Select()
                        .From("ContentItem")
                        .Where("Depth")
                        .Equals(depth)
                        .And("BeginDate")
                        .GreaterEqual(parent.BeginDate)
                        .And("EndDate")
                        .LesserEqual(parent.EndDate).ToList<ContentItem>();
                //var contentItems = context.
                //ContentItems.Where(
                //w => w.Depth == depth &&
                //w.BeginDate >= parent.BeginDate && w.EndDate <= parent.EndDate && !parentIDs.Contains(w.ID));
                if (contentItems.Any())
                {
                    foreach (var contentItem in contentItems)
                    {
                        if (!parentIDs.Contains(contentItem.Id))
                        {
                         //   contentItem.Depth = depth + 1;
                          //  contentItem.ParentID = parent.Id;
                            db.Update(contentItem).Where("Id").Equals(contentItem.Id).Run();
                        }
                       
                    }
                    parent.HasChildren = true;
                }
                db.Update(parent).Where("Id").Equals(parent.Id).Run();
            }
        }

        private static ORID InsertItemToContentItem(OrientDao dao, ORID lastOrid, ContentItem contentItem)
        {
            var orid = dao.AddContentItem(contentItem);
            dao.AddContentItemToContentItem(lastOrid, orid);
            return orid;
        }

        private static ORID InsertContentItemToTimeline(OrientDao dao, ContentItem ci)
        {
            var orid = dao.AddContentItem(ci);
            dao.AddContentItemToTimeline(timelineORID, orid);
            return orid;
        }

        private static string DownloadApiDataAsJson()
        {
            string json = "";
            Console.WriteLine("Begin downloading json");
            time.Start();
            using (var client = new HttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                json = client.GetStringAsync(new Uri(APIURL)).Result;
            }
            time.Stop();
            Console.WriteLine("Begin downloading json : Done and took: " + time.Elapsed.Duration());
            return json;
        }


    }
}

/*          foreach (var contentItem in sorted)
            {
                if(contentItem.BeginDate == 0 && contentItem.EndDate == 0) continue;;
                if (lastItem == null)
                {
                    lastOrid = InsertContentItemToTimeline(db, contentItem);
                    lastItem = contentItem;
                }
                else
                {
                    if (lastItem.BeginDate < contentItem.BeginDate && lastItem.EndDate < contentItem.EndDate)
                    {
                        lastOrid = InsertItemToContentItem(db, lastOrid, contentItem);
                        lastItem = contentItem;
                    }
                    else
                    {
                        lastOrid = InsertContentItemToTimeline(db, contentItem);
                        lastItem = contentItem;
                    }
                }
            }

*/