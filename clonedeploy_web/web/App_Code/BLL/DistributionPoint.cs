﻿using System.Collections.Generic;
using System.Linq;
using DAL;
using Helpers;

namespace BLL
{
    public class DistributionPoint
    {

        public static Models.ValidationResult AddDistributionPoint(Models.DistributionPoint distributionPoint)
        {
            using (var uow = new DAL.UnitOfWork())
            {
                var validationResult = ValidateDistributionPoint(distributionPoint, true);
                if (validationResult.IsValid)
                {
                    uow.DistributionPointRepository.Insert(distributionPoint);
                    validationResult.IsValid = uow.Save();
                }

                return validationResult;
            }
        }

        public static string TotalCount()
        {
            using (var uow = new DAL.UnitOfWork())
            {
                return uow.DistributionPointRepository.Count();
            }
        }

        public static bool DeleteDistributionPoint(int distributionPointId)
        {
            using (var uow = new DAL.UnitOfWork())
            {
                uow.DistributionPointRepository.Delete(distributionPointId);
                return uow.Save();
            }
        }

        public static Models.DistributionPoint GetDistributionPoint(int distributionPointId)
        {
            using (var uow = new DAL.UnitOfWork())
            {
                return uow.DistributionPointRepository.GetById(distributionPointId);
            }
        }

        public static List<Models.DistributionPoint> SearchDistributionPoints(string searchString)
        {
            using (var uow = new DAL.UnitOfWork())
            {
                return uow.DistributionPointRepository.Get(d => d.DisplayName.Contains(searchString),
                    orderBy: (q => q.OrderBy(d => d.DisplayName)));
            }
        }

        public static Models.ValidationResult UpdateDistributionPoint(Models.DistributionPoint distributionPoint)
        {
            using (var uow = new DAL.UnitOfWork())
            {
                var validationResult = ValidateDistributionPoint(distributionPoint, false);
                if (validationResult.IsValid)
                {
                    uow.DistributionPointRepository.Update(distributionPoint, distributionPoint.Id);
                    validationResult.IsValid = uow.Save();
                }

                return validationResult;
            }
        }

        public static Models.ValidationResult ValidateDistributionPoint(Models.DistributionPoint distributionPoint, bool isNewDistributionPoint)
        {
            var validationResult = new Models.ValidationResult();

            if (string.IsNullOrEmpty(distributionPoint.DisplayName) || distributionPoint.DisplayName.Contains(" "))
            {
                validationResult.IsValid = false;
                validationResult.Message = "Distribution Point Name Is Not Valid";
                return validationResult;
            }

            if (isNewDistributionPoint)
            {
                using (var uow = new DAL.UnitOfWork())
                {
                    if (uow.DistributionPointRepository.Exists(h => h.DisplayName == distributionPoint.DisplayName))
                    {
                        validationResult.IsValid = false;
                        validationResult.Message = "This Distribution Point Already Exists";
                        return validationResult;
                    }
                }
            }
            else
            {
                using (var uow = new DAL.UnitOfWork())
                {
                    var originalDistributionPoint = uow.DistributionPointRepository.GetById(distributionPoint.Id);
                    if (originalDistributionPoint.DisplayName != distributionPoint.DisplayName)
                    {
                        if (uow.DistributionPointRepository.Exists(h => h.DisplayName == distributionPoint.DisplayName))
                        {
                            validationResult.IsValid = false;
                            validationResult.Message = "This Distribution Point Already Exists";
                            return validationResult;
                        }
                    }
                }
            }

            return validationResult;
        }
      
    }
}