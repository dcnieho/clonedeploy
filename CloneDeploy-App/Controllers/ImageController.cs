﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Web.Http;
using CloneDeploy_App.Controllers.Authorization;
using CloneDeploy_Entities;
using CloneDeploy_Entities.DTOs;
using CloneDeploy_Entities.DTOs.ImageSchemaBE;
using CloneDeploy_Services;


namespace CloneDeploy_App.Controllers
{
    public class ImageController: ApiController
    {
        private readonly ImageServices _imageServices;

        public ImageController()
        {
            _imageServices = new ImageServices();
        }

        [CustomAuth(Permission = "ImageSearch")]
        public IEnumerable<ImageEntity> GetAll(string searchstring = "")
        {
            var identity = (ClaimsPrincipal)Thread.CurrentPrincipal;
            var userId = identity.Claims.Where(c => c.Type == "user_id")
                             .Select(c => c.Value).SingleOrDefault();
            return string.IsNullOrEmpty(searchstring)
                ? _imageServices.SearchImagesForUser(Convert.ToInt32(userId))
                : _imageServices.SearchImagesForUser(Convert.ToInt32(userId),searchstring);
        }

        [CustomAuth(Permission = "ImageSearch")]
        public IEnumerable<ImageEntity> Search(string searchstring = "")
        {
            return string.IsNullOrEmpty(searchstring)
                ? _imageServices.SearchImages()
                : _imageServices.SearchImages(searchstring);
        }

        [CustomAuth(Permission = "ImageSearch")]
        public ApiStringResponseDTO GetCount()
        {
            var identity = (ClaimsPrincipal)Thread.CurrentPrincipal;
            var userId = identity.Claims.Where(c => c.Type == "user_id")
                             .Select(c => c.Value).SingleOrDefault();

            return new ApiStringResponseDTO() {Value = _imageServices.ImageCountUser(Convert.ToInt32(userId))};

        }

        [CustomAuth(Permission = "ImageRead")]
        public ImageEntity Get(int id)
        {
            var result = _imageServices.GetImage(id);
            if (result == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            return result;
        }

      

        [Authorize]
        [HttpGet]
        public ApiBoolResponseDTO SendImageApprovedMail(int id)
        {
            _imageServices.SendImageApprovedEmail(id);
            return new ApiBoolResponseDTO(){Value = true};
        }

        [CustomAuth(Permission = "ImageCreate")]
        public ActionResultDTO Post(ImageEntity image)
        {
            var result = _imageServices.AddImage(image);
            if (result == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            return result;
        }

        [CustomAuth(Permission = "ImageUpdate")]
        public ActionResultDTO Put(int id, ImageEntity image)
        {
            image.Id = id;
            var result = _imageServices.UpdateImage(image);
            if (result == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            return result;
        }

        [CustomAuth(Permission = "ImageDelete")]
        public ActionResultDTO Delete(int id)
        {
            var result = _imageServices.DeleteImage(id);
            if (result == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            return result;
        }

        [CustomAuth(Permission = "ImageRead")]
        [HttpGet]
        public ApiBoolResponseDTO Export(string path)
        {
            _imageServices.ExportCsv(path);
            return new ApiBoolResponseDTO() { Value = true };
        }

        [CustomAuth(Permission = "ImageRead")]
        public IEnumerable<ImageProfileEntity> GetImageProfiles(int id)
        {
            return _imageServices.SearchProfiles(id);
        }

        [HttpGet]
        [CustomAuth(Permission = "ImageCreate")]
        public ImageProfileEntity SeedDefaultProfile(int id)
        {
            return _imageServices.SeedDefaultImageProfile(id);
        }

        [CustomAuth(Permission = "ImageRead")]
        public IEnumerable<ImageFileInfo> GetPartitionFileInfo(int id, string selectedHd, string selectedPartition)
        {

            return _imageServices.GetPartitionImageFileInfoForGridView(id, selectedHd, selectedPartition);

        }

        [CustomAuth(Permission = "ImageSearch")]
        public ApiStringResponseDTO GetImageSizeOnServer(string imageName, string hdNumber)
        {

            return new ApiStringResponseDTO() {Value = _imageServices.ImageSizeOnServerForGridView(imageName, hdNumber)};
        }
    }
}