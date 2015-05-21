using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChronoZoom.OrientImporter.Entities;
using Orient.Client;
using Orient.Client.API;

namespace ChronoZoom.OrientImporter.Data
{
    public class OrientDao
    {
        private ODatabase db;
        private OServer server;

        public void Connect(string host, int port, string username, string password, string database)
        {
            OClient.CreateDatabasePool(host,port,database,ODatabaseType.Graph,username,password,10,database);
            db = new ODatabase(database);
        }

        public ORID AddTimeline(Timeline timeline)
        {
            var oDocument = db.Insert(timeline).Run();
            return oDocument.ORID;
        }

        public ORID AddContentItem(ContentItem contentItem)
        {
            var oDocument = db.Insert(contentItem).Run();
            return oDocument.ORID;
        }

        public void Disconnect()
        {
            
            db.Close();
        }

        public ODatabase GetContext()
        {
            return db;
        }

        /// <summary>
        /// Server connection
        /// </summary>
        public void Connect(string host, int port, string username, string password)
        {
            server = new OServer(host, port, username, password);
        }

        public bool CreateDatabase(String databasename)
        {
            if (server.DatabaseExist(databasename, OStorageType.PLocal)) return false;
            var created = server.CreateDatabase(databasename, ODatabaseType.Graph, OStorageType.PLocal);
            return created;
        }

        public void CreateClassesInDatabase()
        {
            db.Create.Class<Timeline>().Extends<OVertex>().CreateProperties().Run();
            db.Create.Class<ContentItem>().Extends<OVertex>().CreateProperties().Run();
            db.Create.Class<Contains>("Contains").Extends<OEdge>().CreateProperties().Run();
        }

        public void AddContentItemToTimeline(ORID timelineOrid, ORID ci)
        {
            var oEdge = db.Create.Edge("Contains").From(timelineOrid).To(ci).Run();
        }

        public void AddContentItemToContentItem(ORID lastOrid, ORID orid)
        {
            var oEdge = db.Create.Edge("Contains").From(lastOrid).To(orid).Run();
        }
    }
}
