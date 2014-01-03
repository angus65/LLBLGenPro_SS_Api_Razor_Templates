﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SD.LLBLGen.Pro.ORMSupportClasses;
using ServiceStack.Text;
using Northwind.Data;
using Northwind.Data.Dtos;
using Northwind.Data.Dtos.TypedListDtos;
using Northwind.Data.EntityClasses;
using Northwind.Data.FactoryClasses;
using Northwind.Data.Helpers;
using Northwind.Data.HelperClasses;
using Northwind.Data.ServiceInterfaces;
using Northwind.Data.ServiceInterfaces.TypedListServiceInterfaces;
using Northwind.Data.Services;
using Northwind.Data.Services.TypedListServices;
using Northwind.Data.TypedListClasses;
	// __LLBLGENPRO_USER_CODE_REGION_START SsSvcAdditionalNamespaces 
	// __LLBLGENPRO_USER_CODE_REGION_END 

namespace Northwind.Data.ServiceRepositories.TypedListServiceRepositories
{ 
    public partial class EmployeesByRegionAndTerritoryTypedListServiceRepository : TypedListServiceRepositoryBase<EmployeesByRegionAndTerritory>, IEmployeesByRegionAndTerritoryTypedListServiceRepository
	// __LLBLGENPRO_USER_CODE_REGION_START SsSvcAdditionalInterfaces 
	// __LLBLGENPRO_USER_CODE_REGION_END 
    {
        #region Class Extensibility Methods
        partial void OnCreateRepository();
        partial void OnBeforeFetchEmployeesByRegionAndTerritoryQueryCollectionRequest(IDataAccessAdapter adapter, EmployeesByRegionAndTerritoryQueryCollectionRequest request, SortExpression sortExpression, string[] includedFieldNames, IRelationPredicateBucket predicateBucket, int pageNumber, int pageSize, int limit);
        partial void OnAfterFetchEmployeesByRegionAndTerritoryQueryCollectionRequest(IDataAccessAdapter adapter, EmployeesByRegionAndTerritoryQueryCollectionRequest request, EmployeesByRegionAndTerritoryTypedList typedList, SortExpression sortExpression, string[] includedFieldNames, IRelationPredicateBucket predicateBucket, int pageNumber, int pageSize, int limit, int totalItemCount);
        #endregion
        
        public override IDataAccessAdapterFactory DataAccessAdapterFactory { get; set; }
        
        protected override TypedListType TypedListType
        {
            get { return TypedListType.EmployeesByRegionAndTerritoryTypedList; }
        }
    
        public EmployeesByRegionAndTerritoryTypedListServiceRepository()
        {
            OnCreateRepository();
        }

        // Description for parameters: http://datatables.net/usage/server-side
        public DataTableResponse GetDataTableResponse(EmployeesByRegionAndTerritoryDataTableRequest request)
        {
            //UrlDecode Request Properties
            request.sSearch = System.Web.HttpUtility.UrlDecode(request.sSearch);
            request.Sort = System.Web.HttpUtility.UrlDecode(request.Sort);
            request.Filter = System.Web.HttpUtility.UrlDecode(request.Filter);
            request.Select = System.Web.HttpUtility.UrlDecode(request.Select);

            //Paging
            var iDisplayStart = request.iDisplayStart + 1; // this is because it passes in the 0 instead of 1, 10 instead of 11, etc...
            iDisplayStart = iDisplayStart <= 0 ? (1+((request.PageNumber-1)*request.PageSize)): iDisplayStart;
            var iDisplayLength = request.iDisplayLength <= 0 ? request.PageSize: request.iDisplayLength;
            var pageNumber = Math.Ceiling(iDisplayStart*1.0/iDisplayLength);
            var pageSize = iDisplayLength;
            //Sorting
            var sort = request.Sort;
            if (request.iSortingCols > 0 && request.iSortCol_0 >= 0)
            {
                sort = string.Format("{0}:{1}", FieldMap.Keys.ElementAt(Convert.ToInt32(request.iSortCol_0)), request.sSortDir_0);
            }
            //Search
            var filter = request.Filter;
            var searchStr = string.Empty;
            if (!string.IsNullOrEmpty(request.sSearch))
            {
                // process int field searches
                var n = 0;
                var searchStrAsInt = -1;
                if (int.TryParse(request.sSearch, out searchStrAsInt))
                {
                    foreach (var fm in FieldMap)
                    {
                        if (fm.Value.DataType.IsNumericType())
                        {
                            n++;
                            searchStr += string.Format("({0}:eq:{1})", fm.Key, searchStrAsInt);
                        }
                    }
                }
                // process string field searches
                foreach (var fm in FieldMap)
                {
                    if (fm.Value.DataType == typeof (string)/* && fm.Value.MaxLength < 2000*/)
                    {
                        n++;
                        searchStr += string.Format("({0}:lk:*{1}*)", fm.Key, request.sSearch);
                    }
                }
                searchStr = n > 1 ? "(|" + searchStr + ")": searchStr.Trim('(', ')');

                filter = string.IsNullOrEmpty(filter) ? searchStr
                    : string.Format("(^{0}{1})", 
                    filter.StartsWith("(") ? filter: "(" + filter + ")",
                    searchStr.StartsWith("(") ? searchStr : "(" + searchStr + ")");
            }

            var entities = Fetch(new EmployeesByRegionAndTerritoryQueryCollectionRequest
                {
                    Filter = filter, 
                    PageNumber = Convert.ToInt32(pageNumber),
                    PageSize = pageSize,
                    Sort = sort,
                    Select = request.Select,
                });
            var response = new DataTableResponse();
            foreach (var item in entities.Result)
            {
                response.aaData.Add(new string[]
                    {
                        item.RegionId.ToString(),
                        item.RegionDescription,
                        item.TerritoryId,
                        item.TerritoryDescription,
                        item.EmployeeId.ToString(),
                        item.EmployeeFirstName,
                        item.EmployeeLastName,
                        item.EmployeeCity,
                        item.EmployeeCountry,
                        item.EmployeeRegion

                    });
            }
            response.sEcho = request.sEcho;
            // total records in the database before datatables search
            response.iTotalRecords = entities.Paging.TotalCount;
            // total records in the database after datatables search
            response.iTotalDisplayRecords = entities.Paging.TotalCount;
            return response;
        }
    
        public EmployeesByRegionAndTerritoryCollectionResponse Fetch(EmployeesByRegionAndTerritoryQueryCollectionRequest request)
        {
            base.FixupLimitAndPagingOnRequest(request);

            var typedList = new EmployeesByRegionAndTerritoryTypedList();
            
            var totalItemCount = 0;
            var sortExpression = RepositoryHelper.ConvertStringToSortExpression(TypedListType, request.Sort);
            var includedFieldNames = RepositoryHelper.ConvertStringToExcludedIncludedFields(request.Select);
            var predicateBucket = RepositoryHelper.ConvertStringToRelationPredicateBucket(TypedListType, typedList.GetRelationInfo(), request.Filter);

            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                OnBeforeFetchEmployeesByRegionAndTerritoryQueryCollectionRequest(adapter, request, sortExpression, includedFieldNames, predicateBucket,
                    request.PageNumber, request.PageSize, request.Limit);
                totalItemCount = (int)adapter.GetDbCount(typedList.GetFieldsInfo(), predicateBucket, null, false);
                adapter.FetchTypedList(typedList.GetFieldsInfo(), typedList, predicateBucket, request.Limit, sortExpression, true, null, request.PageNumber, request.PageSize);
                OnAfterFetchEmployeesByRegionAndTerritoryQueryCollectionRequest(adapter, request, typedList, sortExpression, includedFieldNames, predicateBucket,
                    request.PageNumber, request.PageSize, request.Limit, totalItemCount);
            }

            var dtos = new EmployeesByRegionAndTerritoryCollection();
            var enumerator = typedList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                dtos.Add(Map(enumerator.Current, includedFieldNames));
            }

            var response = new EmployeesByRegionAndTerritoryCollectionResponse(dtos, request.PageNumber,
                                                          request.PageSize, totalItemCount);
            return response;       
        }

        private EmployeesByRegionAndTerritory Map(EmployeesByRegionAndTerritoryRow row, string[] fieldNames)
        {
            var hasFn = fieldNames != null && fieldNames.Any();
            var item = new EmployeesByRegionAndTerritory();
            if (!hasFn || fieldNames.Contains("RegionId", StringComparer.OrdinalIgnoreCase))
                item.RegionId = row.RegionId;
            if (!hasFn || fieldNames.Contains("RegionDescription", StringComparer.OrdinalIgnoreCase))
                item.RegionDescription = row.RegionDescription;
            if (!hasFn || fieldNames.Contains("TerritoryId", StringComparer.OrdinalIgnoreCase))
                item.TerritoryId = row.TerritoryId;
            if (!hasFn || fieldNames.Contains("TerritoryDescription", StringComparer.OrdinalIgnoreCase))
                item.TerritoryDescription = row.TerritoryDescription;
            if (!hasFn || fieldNames.Contains("EmployeeId", StringComparer.OrdinalIgnoreCase))
                item.EmployeeId = row.EmployeeId;
            if (!hasFn || fieldNames.Contains("EmployeeFirstName", StringComparer.OrdinalIgnoreCase))
                item.EmployeeFirstName = row.EmployeeFirstName;
            if (!hasFn || fieldNames.Contains("EmployeeLastName", StringComparer.OrdinalIgnoreCase))
                item.EmployeeLastName = row.EmployeeLastName;
            if (!hasFn || fieldNames.Contains("EmployeeCity", StringComparer.OrdinalIgnoreCase))
                item.EmployeeCity = row.EmployeeCity;
            if (!hasFn || fieldNames.Contains("EmployeeCountry", StringComparer.OrdinalIgnoreCase))
                item.EmployeeCountry = row.EmployeeCountry;
            if (!hasFn || fieldNames.Contains("EmployeeRegion", StringComparer.OrdinalIgnoreCase))
                item.EmployeeRegion = row.EmployeeRegion;

            return item;
        }
    
	// __LLBLGENPRO_USER_CODE_REGION_START SsSvcAdditionalMethods 
	// __LLBLGENPRO_USER_CODE_REGION_END 

    }
}
