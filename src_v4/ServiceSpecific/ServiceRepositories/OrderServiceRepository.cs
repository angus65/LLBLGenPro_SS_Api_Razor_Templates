﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SD.LLBLGen.Pro.ORMSupportClasses;
using ServiceStack.Text;
using Northwind.Data;
using Northwind.Data.Dtos;
using Northwind.Data.EntityClasses;
using Northwind.Data.FactoryClasses;
using Northwind.Data.Helpers;
using Northwind.Data.HelperClasses;
using Northwind.Data.ServiceInterfaces;
using Northwind.Data.Services;
	// __LLBLGENPRO_USER_CODE_REGION_START SsSvcAdditionalNamespaces 
	// __LLBLGENPRO_USER_CODE_REGION_END 

namespace Northwind.Data.ServiceRepositories
{ 
    public partial class OrderServiceRepository : EntityServiceRepositoryBase<Order, OrderEntity, OrderEntityFactory>, IOrderServiceRepository
	// __LLBLGENPRO_USER_CODE_REGION_START SsSvcAdditionalInterfaces 
	// __LLBLGENPRO_USER_CODE_REGION_END 
    {
        #region Class Extensibility Methods
        partial void OnCreateRepository();
        partial void OnBeforeFetchOrderPkRequest(IDataAccessAdapter adapter, OrderPkRequest request, OrderEntity entity, IPrefetchPath2 prefetchPath, ExcludeIncludeFieldsList excludedIncludedFields);
        partial void OnAfterFetchOrderPkRequest(IDataAccessAdapter adapter, OrderPkRequest request, OrderEntity entity, IPrefetchPath2 prefetchPath, ExcludeIncludeFieldsList excludedIncludedFields);

        partial void OnBeforeFetchOrderQueryCollectionRequest(IDataAccessAdapter adapter, OrderQueryCollectionRequest request, SortExpression sortExpression, ExcludeIncludeFieldsList excludedIncludedFields, IPrefetchPath2 prefetchPath, IRelationPredicateBucket predicateBucket, int pageNumber, int pageSize, int limit);
        partial void OnAfterFetchOrderQueryCollectionRequest(IDataAccessAdapter adapter, OrderQueryCollectionRequest request, EntityCollection<OrderEntity> entities, SortExpression sortExpression, ExcludeIncludeFieldsList excludedIncludedFields, IPrefetchPath2 prefetchPath, IRelationPredicateBucket predicateBucket, int pageNumber, int pageSize, int limit, int totalItemCount);

        partial void OnBeforeOrderDeleteRequest(IDataAccessAdapter adapter, OrderDeleteRequest request, OrderEntity entity);
        partial void OnAfterOrderDeleteRequest(IDataAccessAdapter adapter, OrderDeleteRequest request, OrderEntity entity, ref bool deleted);
        partial void OnBeforeOrderUpdateRequest(IDataAccessAdapter adapter, OrderUpdateRequest request);
        partial void OnAfterOrderUpdateRequest(IDataAccessAdapter adapter, OrderUpdateRequest request);
        partial void OnBeforeOrderAddRequest(IDataAccessAdapter adapter, OrderAddRequest request);
        partial void OnAfterOrderAddRequest(IDataAccessAdapter adapter, OrderAddRequest request);

        #endregion
        
        public override IDataAccessAdapterFactory DataAccessAdapterFactory { get; set; }
        
        protected override EntityType EntityType
        {
            get { return EntityType.OrderEntity; }
        }
    
        public OrderServiceRepository()
        {
            OnCreateRepository();
        }

        // Description for parameters: http://datatables.net/usage/server-side
        public DataTableResponse GetDataTableResponse(OrderDataTableRequest request)
        {
            //UrlDecode Request Properties
            request.sSearch = System.Web.HttpUtility.UrlDecode(request.sSearch);
            request.Sort = System.Web.HttpUtility.UrlDecode(request.Sort);
            request.Include = System.Web.HttpUtility.UrlDecode(request.Include);
            request.Filter = System.Web.HttpUtility.UrlDecode(request.Filter);
            request.Relations = System.Web.HttpUtility.UrlDecode(request.Relations);
            request.Select = System.Web.HttpUtility.UrlDecode(request.Select);
            
            //Selection
            var iSelectColumns = request.iSelectColumns;

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

            var columnFieldIndexNames = Enum.GetNames(typeof(OrderFieldIndex));
            if(iSelectColumns != null && iSelectColumns.Length > 0){
                try { request.Select = string.Join(",", iSelectColumns.Select(c => columnFieldIndexNames[c]).ToArray()); } catch {}
            }
                
            var entities = Fetch(new OrderQueryCollectionRequest
                {
                    Filter = filter, 
                    PageNumber = Convert.ToInt32(pageNumber),
                    PageSize = pageSize,
                    Sort = sort,
                    Include = request.Include,
                    Relations = request.Relations,
                    Select = request.Select,
                    RCache = request.RCache
                });
            var response = new DataTableResponse();
            var includeCustomer = ((request.Include ?? "").IndexOf("customer", StringComparison.InvariantCultureIgnoreCase)) >= 0;
            var includeEmployee = ((request.Include ?? "").IndexOf("employee", StringComparison.InvariantCultureIgnoreCase)) >= 0;
            var includeOrderDetails = ((request.Include ?? "").IndexOf("orderdetails", StringComparison.InvariantCultureIgnoreCase)) >= 0;
            var includeShipper = ((request.Include ?? "").IndexOf("shipper", StringComparison.InvariantCultureIgnoreCase)) >= 0;

            foreach (var item in entities.Result)
            {
                var relatedDivs = new List<string>();
                relatedDivs.Add(string.Format(@"<div style=""display:block;""><span class=""badge badge-info"">{0}</span><a href=""/customers?filter=customerid:eq:{2}"">{1} Customer</a></div>",
                            includeCustomer ? (item.Customer==null?"0":"1"): "", "",
                            item.CustomerId
                        ));
                relatedDivs.Add(string.Format(@"<div style=""display:block;""><span class=""badge badge-info"">{0}</span><a href=""/employees?filter=employeeid:eq:{2}"">{1} Employee</a></div>",
                            includeEmployee ? (item.Employee==null?"0":"1"): "", "",
                            item.EmployeeId
                        ));
                relatedDivs.Add(string.Format(@"<div style=""display:block;""><span class=""badge badge-info"">{0}</span><a href=""/orderdetails?filter=orderid:eq:{2}"">{1} Order Details</a></div>",
                            includeOrderDetails ? item.OrderDetails.Count.ToString(CultureInfo.InvariantCulture): "", "",
                            item.OrderId
                        ));
                relatedDivs.Add(string.Format(@"<div style=""display:block;""><span class=""badge badge-info"">{0}</span><a href=""/shippers?filter=shipperid:eq:{2}"">{1} Shipper</a></div>",
                            includeShipper ? (item.Shipper==null?"0":"1"): "", "",
                            item.ShipVia
                        ));

                response.aaData.Add(new string[]
                    {
                        item.OrderId.ToString(),
                        item.CustomerId,
                        item.EmployeeId.ToString(),
                        item.OrderDate.ToString(),
                        item.RequiredDate.ToString(),
                        item.ShippedDate.ToString(),
                        item.ShipVia.ToString(),
                        item.Freight.ToString(),
                        item.ShipName,
                        item.ShipAddress,
                        item.ShipCity,
                        item.ShipRegion,
                        item.ShipPostalCode,
                        item.ShipCountry,

                        string.Join("", relatedDivs.ToArray())
                    });
            }
            response.sEcho = request.sEcho;
            // total records in the database before datatables search
            response.iTotalRecords = entities.Paging.TotalCount;
            // total records in the database after datatables search
            response.iTotalDisplayRecords = entities.Paging.TotalCount;
            return response;
        }
    
        public OrderCollectionResponse Fetch(OrderQueryCollectionRequest request)
        {
            base.FixupLimitAndPagingOnRequest(request);

            var totalItemCount = 0;
            var sortExpression = RepositoryHelper.ConvertStringToSortExpression(EntityType, request.Sort);
            var includeFields = RepositoryHelper.ConvertStringToExcludedIncludedFields(EntityType, request.Select);
            var prefetchPath = RepositoryHelper.ConvertStringToPrefetchPath(EntityType, request.Include, request.Select);
            var predicateBucket = RepositoryHelper.ConvertStringToRelationPredicateBucket(EntityType, request.Filter, request.Relations);

            EntityCollection<OrderEntity> entities;
            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                OnBeforeFetchOrderQueryCollectionRequest(adapter, request, sortExpression, includeFields, prefetchPath, predicateBucket,
                    request.PageNumber, request.PageSize, request.Limit);
                entities = base.Fetch(adapter, sortExpression, includeFields, prefetchPath, predicateBucket,
                    request.PageNumber, request.PageSize, request.Limit, request.RCache, out totalItemCount);
                OnAfterFetchOrderQueryCollectionRequest(adapter, request, entities, sortExpression, includeFields, prefetchPath, predicateBucket,
                    request.PageNumber, request.PageSize, request.Limit, totalItemCount);
            }
            var response = new OrderCollectionResponse(entities.ToDtoCollection(), request.PageNumber,
                                                          request.PageSize, totalItemCount);
            return response;            
        }


        public OrderResponse Fetch(OrderPkRequest request)
        {
            var entity = new OrderEntity();
            entity.OrderId = request.OrderId;

            var excludedIncludedFields = RepositoryHelper.ConvertStringToExcludedIncludedFields(EntityType, request.Select);
            var prefetchPath = RepositoryHelper.ConvertStringToPrefetchPath(EntityType, request.Include, request.Select);

            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                OnBeforeFetchOrderPkRequest(adapter, request, entity, prefetchPath, excludedIncludedFields);

                var pkPredicate = adapter.CreatePrimaryKeyFilter(entity.Fields.PrimaryKeyFields);
                entity = base.Fetch(adapter, pkPredicate, prefetchPath, excludedIncludedFields, request.RCache);
                if (entity != null)
                {
                    OnAfterFetchOrderPkRequest(adapter, request, entity, prefetchPath, excludedIncludedFields);
                    return new OrderResponse(entity.ToDto());
                }
            }
            return new OrderResponse(null);
        }

        public OrderResponse Create(OrderAddRequest request)
        {
            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                OnBeforeOrderAddRequest(adapter, request);
                
                var entity = request.FromDto();
                entity.IsNew = true;
            
                if (adapter.SaveEntity(entity, true))
                {
                    OnAfterOrderAddRequest(adapter, request);
                    return new OrderResponse(entity.ToDto());
                }
            }

            throw new InvalidOperationException();
        }

        public OrderResponse Update(OrderUpdateRequest request)
        {
            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                OnBeforeOrderUpdateRequest(adapter, request);
                
                var entity = request.FromDto();
                entity.IsNew = false;
                entity.IsDirty = true;
            
                if (adapter.SaveEntity(entity, true))
                {
                    OnAfterOrderUpdateRequest(adapter, request);
                    return new OrderResponse(entity.ToDto());
                }
            }

            throw new InvalidOperationException();
        }
        
        public SimpleResponse<bool> Delete(OrderDeleteRequest request)
        {
            var entity = new OrderEntity();
            entity.OrderId = request.OrderId;


            var deleted = false;
            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                OnBeforeOrderDeleteRequest(adapter, request, entity);
                deleted = adapter.DeleteEntity(entity);
                OnAfterOrderDeleteRequest(adapter, request, entity, ref deleted);
            }
            return new SimpleResponse<bool> { Result = deleted };
        }

        private const string UcMapCacheKey = "order-uc-map";
        internal override IDictionary< string, IEntityField2[] > UniqueConstraintMap
        {
            get 
            { 
                var map = CacheClient.Get<IDictionary< string, IEntityField2[] >>(UcMapCacheKey);
                if (map == null)
                {
                    map = new Dictionary< string, IEntityField2[] >();
                    CacheClient.Set(UcMapCacheKey, map);
                }
                return map;
            }
            set { }
        }
    
	// __LLBLGENPRO_USER_CODE_REGION_START SsSvcAdditionalMethods 
	// __LLBLGENPRO_USER_CODE_REGION_END 

    }

    internal static partial class OrderEntityDtoMapperExtensions
    {
        static partial void OnBeforeEntityToDto(OrderEntity entity, Hashtable seenObjects, Hashtable parents);
        static partial void OnAfterEntityToDto(OrderEntity entity, Hashtable seenObjects, Hashtable parents, Order dto);
        static partial void OnBeforeEntityCollectionToDtoCollection(EntityCollection<OrderEntity> entities);
        static partial void OnAfterEntityCollectionToDtoCollection(EntityCollection<OrderEntity> entities, OrderCollection dtos);
        static partial void OnBeforeDtoToEntity(Order dto);
        static partial void OnAfterDtoToEntity(Order dto, OrderEntity entity);
        
        public static Order ToDto(this OrderEntity entity)
        {
            return entity.ToDto(new Hashtable(), new Hashtable());
        }

        public static Order ToDto(this OrderEntity entity, Hashtable seenObjects, Hashtable parents)
        {
            OnBeforeEntityToDto(entity, seenObjects, parents);
            var dto = new Order();
            if (entity != null)
            {
                if (seenObjects == null)
                    seenObjects = new Hashtable();
                seenObjects[entity] = dto;

                parents = new Hashtable(parents) { { entity, null } };

                // Map dto properties
                dto.OrderId = entity.OrderId;
                dto.CustomerId = entity.CustomerId;
                dto.EmployeeId = entity.EmployeeId;
                dto.OrderDate = entity.OrderDate;
                dto.RequiredDate = entity.RequiredDate;
                dto.ShippedDate = entity.ShippedDate;
                dto.ShipVia = entity.ShipVia;
                dto.Freight = entity.Freight;
                dto.ShipName = entity.ShipName;
                dto.ShipAddress = entity.ShipAddress;
                dto.ShipCity = entity.ShipCity;
                dto.ShipRegion = entity.ShipRegion;
                dto.ShipPostalCode = entity.ShipPostalCode;
                dto.ShipCountry = entity.ShipCountry;


                // Map dto associations
                // n:1 Customer association
                if (entity.Customer != null)
                {
                  dto.Customer = entity.Customer.RelatedObject(seenObjects, parents);
                }
                // n:1 Employee association
                if (entity.Employee != null)
                {
                  dto.Employee = entity.Employee.RelatedObject(seenObjects, parents);
                }
                // 1:n OrderDetails association of OrderDetail entities
                if (entity.OrderDetails != null && entity.OrderDetails.Any())
                {
                  dto.OrderDetails = new OrderDetailCollection(entity.OrderDetails.RelatedArray(seenObjects, parents));
                }
                // n:1 Shipper association
                if (entity.Shipper != null)
                {
                  dto.Shipper = entity.Shipper.RelatedObject(seenObjects, parents);
                }

            }
            
            OnAfterEntityToDto(entity, seenObjects, parents, dto);
            return dto;
        }
        
        public static OrderCollection ToDtoCollection(this EntityCollection<OrderEntity> entities)
        {
            OnBeforeEntityCollectionToDtoCollection(entities);
            var seenObjects = new Hashtable();
            var collection = new OrderCollection();
            foreach (var entity in entities)
            {
                collection.Add(entity.ToDto(seenObjects, new Hashtable()));
            }
            OnAfterEntityCollectionToDtoCollection(entities, collection);
            return collection;
        }

        public static OrderEntity FromDto(this Order dto)
        {
            OnBeforeDtoToEntity(dto);
            var entity = new OrderEntity();

            // Map entity properties
            entity.CustomerId = dto.CustomerId;
            entity.EmployeeId = dto.EmployeeId;
            entity.OrderDate = dto.OrderDate;
            entity.RequiredDate = dto.RequiredDate;
            entity.ShippedDate = dto.ShippedDate;
            entity.ShipVia = dto.ShipVia;
            entity.Freight = dto.Freight;
            entity.ShipName = dto.ShipName;
            entity.ShipAddress = dto.ShipAddress;
            entity.ShipCity = dto.ShipCity;
            entity.ShipRegion = dto.ShipRegion;
            entity.ShipPostalCode = dto.ShipPostalCode;
            entity.ShipCountry = dto.ShipCountry;


            // Map entity associations
            // n:1 Customer association
            if (dto.Customer != null)
            {
                entity.Customer = dto.Customer.FromDto();
            }
            // n:1 Employee association
            if (dto.Employee != null)
            {
                entity.Employee = dto.Employee.FromDto();
            }
            // 1:n OrderDetails association
            if (dto.OrderDetails != null && dto.OrderDetails.Any())
            {
                foreach (var relatedDto in dto.OrderDetails)
                    entity.OrderDetails.Add(relatedDto.FromDto());
            }
            // n:1 Shipper association
            if (dto.Shipper != null)
            {
                entity.Shipper = dto.Shipper.FromDto();
            }

            OnAfterDtoToEntity(dto, entity);
            return entity;
        }

        public static Order[] RelatedArray(this EntityCollection<OrderEntity> entities, Hashtable seenObjects, Hashtable parents)
        {
            if (null == entities)
            {
                return null;
            }

            var arr = new Order[entities.Count];
            var i = 0;

            foreach (var entity in entities)
            {
                if (parents.Contains(entity))
                {
                    // - avoid all cyclic references and return null
                    // - another option is to 'continue' and just disregard this one entity; however,
                    // if this is a collection this would lead the client app to believe that other
                    // items are part of the collection and not the parent item, which is misleading and false
                    // - it is therefore better to just return null, indicating nothing is being retrieved
                    // for the property all-together
                    return null;
                }
            }

            foreach (var entity in entities)
            {
                if (seenObjects.Contains(entity))
                {
                    arr[i++] = seenObjects[entity] as Order;
                }
                else
                {
                    arr[i++] = entity.ToDto(seenObjects, parents);
                }
            }
            return arr;
        }

        public static Order RelatedObject(this OrderEntity entity, Hashtable seenObjects, Hashtable parents)
        {
            if (null == entity)
            {
                return null;
            }

            if (seenObjects.Contains(entity))
            {
                if (parents.Contains(entity))
                {
                    // avoid cyclic references
                    return null;
                }
                else
                {
                    return seenObjects[entity] as Order;
                }
            }

            return entity.ToDto(seenObjects, parents);
        }
    }
}
