using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CatalogAPI.Helpers;
using CatalogAPI.Infrastructure;
using CatalogAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace CatalogAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private CatalogContext db;
        private IConfiguration configuration;
        public CatalogController(CatalogContext db,IConfiguration config)
        {
            this.db = db;
            this.configuration = config;
        }

        [AllowAnonymous]
        [HttpGet("", Name ="GetProducts")]
        public async Task<ActionResult<List<CatalogItem>>> GetProducts()
        {
            var result= await this.db.Catalog.FindAsync<CatalogItem>(FilterDefinition<CatalogItem>.Empty);
            return result.ToList();
        }

        [AllowAnonymous]
        [HttpGet("{id}", Name = "FindById")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<CatalogItem>> FindProductById(string id)
        {
            var builder = Builders<CatalogItem>.Filter;
            var filter = builder.Eq("Id", id);
            var result = await db.Catalog.FindAsync(filter);
            var item = result.FirstOrDefault();
            if (item == null)
            {
                return NotFound(); //Not found , Status code 404
            }
            else
            {
                return Ok(item); //Found , status code 200
            }
        }

        //[Authorize(Roles ="admin")]
        [HttpPost("", Name ="AddProduct")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<CatalogItem> AddProduct(CatalogItem item)
        {
            TryValidateModel(item);
            if (ModelState.IsValid)
            {
                this.db.Catalog.InsertOne(item);
                return Created("", item); // status code 201
            }
            else
            {
                return BadRequest(ModelState); //status code 400
            }
        }

        //[Authorize(Roles ="admin")]
        [HttpPost("product")]
        public ActionResult<CatalogItem> AddProduct()
        {
            //var imageName = SaveImageToLocal(Request.Form.Files[0]);
            var imageName = SaveImageToCloudAsync(Request.Form.Files[0]).GetAwaiter().GetResult();
            var catalogItem = new CatalogItem()
            {
                Name=Request.Form["name"],
                Price =Double.Parse( Request.Form["price"]),
                Quantity = Int32.Parse(Request.Form["quantity"]),
                ReorderLevel = Int32.Parse(Request.Form["reorderLevel"]),
                ManufacturingDate = DateTime.Parse(Request.Form["manufacturingDate"]),
                Vendors = new List<Vendor>(),
                ImageUrl=imageName
            };
            db.Catalog.InsertOne(catalogItem);
            BackupToTableAsync(catalogItem).GetAwaiter().GetResult();
            return catalogItem;          
        }

        [NonAction]
        private string SaveImageToLocal(IFormFile image)
        {
            var imageName = $"{Guid.NewGuid()}_{image.FileName}";
            var dirName = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            var filePath = Path.Combine(dirName, imageName);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(fs);
            }
            return $"/Images/{imageName}";
        }

        [NonAction]
        private async Task<string> SaveImageToCloudAsync(IFormFile file)
        {
            var imageName = $"{Guid.NewGuid()}_{file.FileName}";
            var tempFile = Path.GetTempFileName();
            using (FileStream fs = new FileStream(tempFile, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }
            var imageFile = Path.Combine(Path.GetDirectoryName(tempFile), imageName);
            System.IO.File.Move(tempFile, imageFile);
            StorageAccountHelper helper = new StorageAccountHelper();
            helper.StorageConnectionString = configuration.GetConnectionString("StorageConnection");
            var fileUri = await helper.UploadFileToBlobAsync(imageFile, "eshopimages");
            return fileUri;
        }

        [NonAction]
        async Task<CatalogEntity>BackupToTableAsync(CatalogItem item)
        {
            StorageAccountHelper helper = new StorageAccountHelper();
            helper.TableConnectionString = configuration.GetConnectionString("TableConnection");
            return await helper.SaveToTableAsync(item);
        }
    }
}