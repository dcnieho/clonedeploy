﻿using System.Collections.Generic;
using CloneDeploy_Entities;
using CloneDeploy_Entities.DTOs;
using RestSharp;

namespace CloneDeploy_ApiCalls
{
    public class ClusterGroupAPI : BaseAPI
    {
        private readonly ApiRequest _apiRequest;

        public ClusterGroupAPI(string resource) : base(resource)
        {
            _apiRequest = new ApiRequest();
        }

        public ActionResultDTO Delete(int id)
        {
            Request.Method = Method.DELETE;
            Request.Resource = string.Format("api/{0}/Delete/{1}", Resource, id);
            var response = _apiRequest.Execute<ActionResultDTO>(Request);
            if (response.Id == 0)
                response.Success = false;
            return response;
        }

        public ClusterGroupEntity Get(int id)
        {
            Request.Method = Method.GET;
            Request.Resource = string.Format("api/{0}/Get/{1}", Resource, id);
            return _apiRequest.Execute<ClusterGroupEntity>(Request);
        }

        public List<ClusterGroupEntity> GetAll(int limit, string searchstring)
        {
            Request.Method = Method.GET;
            Request.Resource = string.Format("api/{0}/GetAll", Resource);
            Request.AddParameter("limit", limit);
            Request.AddParameter("searchstring", searchstring);
            return _apiRequest.Execute<List<ClusterGroupEntity>>(Request);
        }

        public IEnumerable<ClusterGroupDistributionPointEntity> GetClusterDistributionPoints(int id)
        {
            Request.Method = Method.GET;
            Request.Resource = string.Format("api/{0}/GetClusterDistributionPoints/{1}", Resource, id);
            return _apiRequest.Execute<List<ClusterGroupDistributionPointEntity>>(Request);
        }

        public IEnumerable<ClusterGroupServerEntity> GetClusterServers(int id)
        {
            Request.Method = Method.GET;
            Request.Resource = string.Format("api/{0}/GetClusterServers/{1}", Resource, id);
            return _apiRequest.Execute<List<ClusterGroupServerEntity>>(Request);
        }

        public string GetCount()
        {
            Request.Method = Method.GET;
            Request.Resource = string.Format("api/{0}/GetCount", Resource);
            var responseData = _apiRequest.Execute<ApiStringResponseDTO>(Request);
            return responseData != null ? responseData.Value : string.Empty;
        }

        public ActionResultDTO Post(ClusterGroupEntity tObject)
        {
            Request.Method = Method.POST;
            Request.AddJsonBody(tObject);
            Request.Resource = string.Format("api/{0}/Post/", Resource);
            var response = _apiRequest.Execute<ActionResultDTO>(Request);
            if (response.Id == 0)
                response.Success = false;
            return response;
        }

        public ActionResultDTO Put(int id, ClusterGroupEntity tObject)
        {
            Request.Method = Method.PUT;
            Request.AddJsonBody(tObject);
            Request.Resource = string.Format("api/{0}/Put/{1}", Resource, id);
            var response = _apiRequest.Execute<ActionResultDTO>(Request);
            if (response.Id == 0)
                response.Success = false;
            return response;
        }
    }
}