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

    public class Exercise4POST : IApiProviderFor<QER.CompositionApi.Portal.PortalApiProject>,IApiProvider
    {
        public void Build(IApiBuilder builder)
        {
            builder.AddMethod(Method.Define("pworequest")
          .Handle<PostedPWO>("POST", async (posted, qr, ct) =>
          {
              string AADUsername = posted.Aadusername;
              string AADGroupUID = posted.Aadgroupuid;
              string personUID = "";
              string AADXObjectKey = "";
              string AADAccProduct = "";
              string AADUIDOrg = "";
              var uidpersoninserted = qr.Session.User().Uid;  // Gets the UID of the current user



              
              
              //query to find uid_accproduct of aad group with given uid
              var queryFindAADAccProduct = Query.From("AADGroup")
                                                .Select("UID_AccProduct")
                                                .Where(string.Format("UID_AADGroup = '{0}'", AADGroupUID));
              var tryGetAADAccProduct = await qr.Session.Source()
                                  .TryGetAsync(queryFindAADAccProduct, EntityLoadType.DelayedLogic, ct)
                                  .ConfigureAwait(false);

              if (tryGetAADAccProduct.Success)
              {
                  AADAccProduct = tryGetAADAccProduct.Result.GetValue<string>("UID_AccProduct");

                  //query to find uid_itshoporg of service item corresponding to given aad group
                  var queryFindAADUIDOrg = Query.From("ITShopOrg")
                                                .Select("UID_ITShopOrg")
                                                .Where(string.Format("UID_AccProduct = '{0}'", AADAccProduct));
                  // Attempt to retrieve the entity from the database asynchronously
                  var tryGetAADUIDOrg = await qr.Session.Source()
                                      .TryGetAsync(queryFindAADUIDOrg, EntityLoadType.DelayedLogic, ct)
                                      .ConfigureAwait(false);
                  if (tryGetAADUIDOrg.Success)
                  {
                      //store the uid_itshop org 
                      AADUIDOrg = tryGetAADUIDOrg.Result.GetValue<string>("UID_ITShopOrg");
                  }

                  //query to find xobject key of aad group with given uid
                  var queryFindAADXObjectKey = Query.From("AADGroup")
                  .Select("XObjectKey")
                  .Where(string.Format("UID_AADGroup = '{0}'", AADGroupUID));
                  // Attempt to retrieve the entity from the database asynchronously
                  var tryGetAADXObjectKey = await qr.Session.Source()
                                      .TryGetAsync(queryFindAADXObjectKey, EntityLoadType.DelayedLogic, ct)
                                      .ConfigureAwait(false);
                  // store the aad group's XObjectKey
                  if (tryGetAADXObjectKey.Success)
                  {
                      AADXObjectKey = tryGetAADXObjectKey.Result.GetValue<string>("XObjectKey");

                  }
                  

                  //query to find person from given aad user account
                  var queryFindPersonFromAADUser = Query.From("AADUser")
                                                  .Select("UID_Person")
                                                  .Where(string.Format("UserPrincipalName = '{0}'", AADUsername));


                  // Attempt to retrieve the entity from the database asynchronously
                  var tryGetAADUserPerson = await qr.Session.Source()
                                      .TryGetAsync(queryFindPersonFromAADUser, EntityLoadType.DelayedLogic, ct)
                                      .ConfigureAwait(false);
                  if (tryGetAADUserPerson.Success)
                  {
                      personUID = tryGetAADUserPerson.Result.GetValue<string>("UID_Person");
                      // Create a new 'PersonWantsOrg' entity
                      var newPWO = await qr.Session.Source().CreateNewAsync("PersonWantsOrg",
                          new EntityParameters
                          {
                              CreationType = EntityCreationType.DelayedLogic
                          }, ct).ConfigureAwait(false);
                      // Set the values for the new 'PersonWantsOrg' entity
                      await newPWO.PutValueAsync("UID_Org", AADUIDOrg, ct).ConfigureAwait(false);
                      await newPWO.PutValueAsync("UID_PersonOrdered", personUID, ct).ConfigureAwait(false);
                      await newPWO.PutValueAsync("UID_PersonInserted", uidpersoninserted, ct).ConfigureAwait(false);
                      await newPWO.PutValueAsync("ObjectKeyOrdered", AADXObjectKey, ct).ConfigureAwait(false);
                      // Start Unit of Work to save the new entity to the database
                      using (var uu = qr.Session.StartUnitOfWork())
                      {
                          await uu.PutAsync(newPWO, ct).ConfigureAwait(false);  // Add the new entity to the unit of work
                          await uu.CommitAsync(ct).ConfigureAwait(false);  // Commit the transaction to persist changes
                      }
                  }
                  else //if user with given username was not found
                  {
                      throw new HttpException(681, "Provided AAD Username is not valid");
                  }



              }
              else //if aad group with given uid was not found
              {
                  throw new HttpException(681, "Provided AAD UID is not valid");
              }





          }));
        }

        //request body for post request
        public class PostedPWO
        {
            public string Aadusername { get; set; }
            public string Aadgroupuid { get; set; }
        }
    }
}