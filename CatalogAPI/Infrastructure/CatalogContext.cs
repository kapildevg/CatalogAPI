using CatalogAPI.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace CatalogAPI.Infrastructure
{
    public class CatalogContext
    {
        private IConfiguration configuration;
        private IMongoDatabase database;

        public CatalogContext(IConfiguration configuration)
        {
            this.configuration = configuration;
            var connectionString = configuration.GetValue<string>("MongoSettings:ConnectionString");
  //          var connectionString =
  //@"mongodb://cosmos14-mongo:dvmUeQ6NM4DsnL4vEGT1hAl5n66iSRC3nQIgxFi5rn7SCXQxmLCUxHNutFjGkHDEsEzVRc3JRx3s6Dkft9uheA==@cosmos14-mongo.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&maxIdleTimeMS=120000&appName=@cosmos14-mongo@";
            MongoClientSettings settings = MongoClientSettings.FromUrl(
              new MongoUrl(connectionString)
            );
            
            settings.SslSettings =
              new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            //var mongoClient = new MongoClient(settings);
            //MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
            MongoClient client = new MongoClient(settings);
            if (client != null)
            {
                this.database = client.GetDatabase(configuration.GetValue<string>("MongoSettings:Database"));
            }
        }

        public IMongoCollection<CatalogItem> Catalog
        {
            get 
            { 
                return this.database.GetCollection<CatalogItem>("products"); 
            }
        }
    }
}
