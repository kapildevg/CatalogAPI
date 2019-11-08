using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CatalogAPI.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace CatalogAPI.Helpers
{
    public class StorageAccountHelper
    {
        private string storageConnectionString;
        private string tableConnectionString;
        private CloudStorageAccount storageAccount;
        private CloudStorageAccount tableStorageAccount;
        private CloudBlobClient blobClient;
        private CloudTableClient tableClient;
        public string StorageConnectionString
        {
            get { return this.storageConnectionString; }
            set
            {
                this.storageConnectionString = value;
                storageAccount = CloudStorageAccount.Parse(this.storageConnectionString);

            }
        }
        public string TableConnectionString
        {
            get { return this.tableConnectionString; }
            set
            {
                this.tableConnectionString = value;
                tableStorageAccount = CloudStorageAccount.Parse(this.tableConnectionString);

            }
        }

        public async Task<string> UploadFileToBlobAsync(string imageFile, string containerName)
        {
            blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            

            await container.CreateIfNotExistsAsync();
            BlobContainerPermissions permissions = new BlobContainerPermissions()
            {
                PublicAccess = BlobContainerPublicAccessType.Container
            };
            await container.SetPermissionsAsync(permissions);
            var fileName = Path.GetFileName(imageFile);
            var blob = container.GetBlockBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
            await blob.UploadFromFileAsync(imageFile);
                return blob.Uri.AbsoluteUri;

        }

        public async Task<CatalogEntity> SaveToTableAsync(CatalogItem item)
        {
            CatalogEntity catalogEntity = new CatalogEntity(item.Name, item.Id)
            {
                ImageUrl = item.ImageUrl,
                ReorderLevel = item.ReorderLevel,
                Quantity = item.Quantity,
                Price = item.Price,
                ManufacturingDate = item.ManufacturingDate


            };
            tableClient = tableStorageAccount.CreateCloudTableClient();
            var catalogTable = tableClient.GetTableReference("catalog");
            await catalogTable.CreateIfNotExistsAsync();
            TableOperation operation = TableOperation.InsertOrMerge(catalogEntity);
            var tableResult = await catalogTable.ExecuteAsync(operation);
            return tableResult.Result as CatalogEntity;


        }
    }
}
