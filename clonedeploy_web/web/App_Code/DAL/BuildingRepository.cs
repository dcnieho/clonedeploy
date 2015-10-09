﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Helpers;

namespace DAL
{
    public class BuildingRepository : GenericRepository<Models.Building>
    {
        private CloneDeployDbContext _context;

        public BuildingRepository(CloneDeployDbContext context)
            : base(context)
        {
            _context = context;
        }

        public void Import()
        {
            throw new Exception("Not Implemented");
        }

        public List<Models.Building> Get(string searchString)
        {
            return (from s in _context.Buildings
                    join d in _context.DistributionPoints on s.DistributionPointId equals d.Id into joined
                    from j in joined.DefaultIfEmpty()
                    where s.Name.Contains(searchString)
                    orderby s.Name
                    select new
                    {
                        id = s.Id,
                        name = s.Name,
                        distributionPoint = j
                    }).AsEnumerable().Select(x => new Models.Building()
                    {
                        Id = x.id,
                        Name = x.name,
                        DistributionPoint = x.distributionPoint
                    }).ToList();

        }
    }
}