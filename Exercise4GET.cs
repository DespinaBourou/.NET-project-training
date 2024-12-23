using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QBM.CompositionApi.ApiManager;
using QBM.CompositionApi.Definition;
using QBM.CompositionApi.PlugIns;
using VI.Base;
using QER.CompositionApi.Portal;
using QBM.CompositionApi.Crud;
using VI.DB.Entities; //needed for exercise 3
using VI.DB;
using QBM.CompositionApi.Handling;
using System.Web;
using NLog.Targets;
using static QBM.CompositionApi.MyExercisesPlugin;

namespace QBM.CompositionApi
{

    public class Exercise4GET : IApiProviderFor<PortalApiProject>
    {
        public void Build(IApiBuilder builder)
        {
            builder.AddMethod(Method.Define("aadgroupfilter")
                .Handle<FilterAADGroup, ArrayList>("POST", async (posted, qr, ct) =>
                {
                    string whereClause = "1=1"; //1=1 for if i dont include any filters
                    var returnedfilterl = new ArrayList(); //will return an arraylist consisting of objects representing the returned aad groups
                    if (posted != null) //to check if there are filters , ie if there is a post body
                    {
                        string UID_AADOrg = posted.UID_AADOrganization;
                        string xUserInserted = posted.xUserInserted;

                        //change the where clause depending on the filters
                        if (UID_AADOrg != null && xUserInserted != null)
                        {
                            whereClause = string.Format("UID_AADOrganization = '{0}' AND XUserInserted= '{1}'",
                                UID_AADOrg, xUserInserted);

                        }
                        else if (UID_AADOrg != null)
                        {
                            whereClause = string.Format("UID_AADOrganization = '{0}'", UID_AADOrg);
                        }
                        else if (xUserInserted != null)
                        {
                            whereClause = string.Format("XUserInserted = '{0}'", xUserInserted);

                        }
                    }

                    //build the query
                    var queryWithFilters = Query.From("AADGroup")
                                             .Select("*")
                                             .Where(whereClause);
                    //attempt to retrieve collection of entities matching the query asynchronously
                    var tryGetwFilter = await qr.Session
                                                .Source()
                                                .GetCollectionAsync(queryWithFilters, EntityCollectionLoadType.Default, ct)
                                                .ConfigureAwait(false);

                    //convert each entity included in the returned collection to a FilterResponse object and return it
                    foreach (var filtergroup in tryGetwFilter)
                    {
                        returnedfilterl.Add(await FilterResponse.fromEntity(filtergroup, qr.Session));
                    }
                    return returnedfilterl;

                })
            );
        }

        //request body
        public class FilterAADGroup
        {
            public string UID_AADOrganization { get; set; }
            public string xUserInserted { get; set; }
        }
        public class FilterResponse
        {
            //properties to hold the columns of aad group
            public string DisplayName { get; set; }
            public string UID_AADOrganization { get; set; }
            public string MailNickName { get; set; }
            public string Description { get; set; }

            //static method to create a filterresponse instance from an IEntity object
            public static async Task<FilterResponse> fromEntity(IEntity entity, ISession session)
            {
                // Instantiate a new FilterResponse object and populate it with data from the entity
                var g = new FilterResponse
                {
                    // Asynchronously get the DisplayName value from the entity
                    DisplayName = await entity.GetValueAsync<string>("DisplayName").ConfigureAwait(false),

                    // Asynchronously get the UID_AADOrganization value from the entity
                    UID_AADOrganization = await entity.GetValueAsync<string>("UID_AADOrganization").ConfigureAwait(false),

                    // Asynchronously get the MailNickName value from the entity
                    MailNickName = await entity.GetValueAsync<string>("MailNickName").ConfigureAwait(false),

                    // Asynchronously get the Description value from the entity
                    Description = await entity.GetValueAsync<string>("Description").ConfigureAwait(false),


                };

                // Return the populated ReturnedName object
                return g;
            }


        }
    }
}
