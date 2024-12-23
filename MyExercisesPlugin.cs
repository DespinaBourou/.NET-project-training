using System;
using System.Linq;
using System.Text;
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


namespace QBM.CompositionApi
{
    public class MyExercisesPlugin : IApiProviderFor<PortalApiProject>
    {
        public void Build(IApiBuilder builder)
        {
            Console.WriteLine("Hello World");
            builder.AddMethod(Method.Define("helloworld")
                .HandleGet(qr => new DataObject { Message = "Hello World!" }));

            //{
            //    return "Hello World";
            //}



            builder.AddMethod(Method.Define("helloworld/post")
            // Handle the POST request with input of type PostedMessage and output of type DataObject
            .Handle<PostedMessage, DataObject>("POST",
            (posted, qr) => new DataObject
            {
                Message = "Hello " + posted.Input
            }));


            //exercise 3
            builder.AddMethod(Method.Define("aadgroup/{uidgroup}")
                   //.AllowUnauthenticated()
                   .WithParameter("uidgroup", typeof(string), isInQuery: false)
                   .HandleGet(async (qr, ct) =>
                        {
                            var uidgroup = qr.Parameters.Get<string>("uidgroup");

                            Console.WriteLine(uidgroup);

                            //build a query to select columns from AADGroup table where UID_AADGroup
                            //matches the uid provided as a path parameter
                            var queryAADGroup = Query.From("AADGroup")
                            .Select("DisplayName", "UID_AADOrganization", "Description", "UID_AccProduct", "MailNickName")
                            .Where(string.Format("UID_AADGroup='{0}'", uidgroup));
                            Console.WriteLine(queryAADGroup);

                            // Attempt to retrieve the entity matching the query asynchronously
                            var tryGet = await qr.Session
                                                .Source()
                                                .TryGetAsync(queryAADGroup, EntityLoadType.DelayedLogic).ConfigureAwait(false);
                            //var tryGet = await qr.Session
                            //    .Source()
                            //.GetCollectionAsync(queryAADGroup, EntityCollectionLoadType.Default, ct).ConfigureAwait(false);
                            //tryGet[0]

                            Console.WriteLine(tryGet);

                            // Convert the retrieved entity to a ReturnedName object and return it
                            return await ReturnedName.fromEntity(tryGet.Result, qr.Session)
                                        .ConfigureAwait(false);
                        }));
        }

            //method to insert new aad group
            /*builder.AddMethod(Method.Define("aadgroup/insertgroup")
                .Handle<PostedAADGroup>("POST", async (posted, qr, ct) =>
                    {
                        //variables to hold data from the post request
                        string displayName = ""; //its necessary for the creation 
                        string mailNickName = ""; //alias //its necessary for the creation
                        string UID_AADOrganization = ""; //tenant //its necessary for the creation
                        string description = "";
                        string requiredAERole = "0d6c1a85-eac7-4c00-a580-c08cb8347351"; //uid of the required role
                        var uidperson = qr.Session.User().Uid;  // Gets the UID of the current user

                        //query to check if logged in identity has required ae role
                        var queryCheckIfHasAERole = Query.From("PersonInAERole")
                                                              .Select("*")
                                                              .Where(string.Format("UID_Person = '{0}' AND UID_AERole = '{1}'", uidperson, requiredAERole));

                        // Attempt to retrieve the entity from the database asynchronously
                        var tryget = await qr.Session.Source()
                                            .TryGetAsync(queryCheckIfHasAERole, EntityLoadType.DelayedLogic, ct)
                                            .ConfigureAwait(false);
                        Console.WriteLine(tryget);
                        if (tryget.Success)
                        {
                            //check if display name starts w aad
                            if (posted.DisplayName.StartsWith("aad"))
                            {

                                //extract the values from the posted data and assign them to the vars
                                displayName = posted.DisplayName;
                                mailNickName = posted.MailNickName;
                                UID_AADOrganization = posted.UID_AADOrganization;
                                description = posted.Description;

                                // Create a new 'AADGroup' entity
                                var newID = await qr.Session.Source().CreateNewAsync("AADGroup",
                                    new EntityParameters
                                    {
                                        CreationType = EntityCreationType.DelayedLogic
                                    }, ct).ConfigureAwait(false);

                                // Set the values for the new 'Person' entity
                                await newID.PutValueAsync("DisplayName", displayName, ct).ConfigureAwait(false);
                                await newID.PutValueAsync("MailNickName", mailNickName, ct).ConfigureAwait(false);
                                await newID.PutValueAsync("UID_AADOrganization", UID_AADOrganization, ct).ConfigureAwait(false);
                                await newID.PutValueAsync("Description", description, ct).ConfigureAwait(false);


                                // Start Unit of Work to save the new entity to the database
                                using (var u = qr.Session.StartUnitOfWork())
                                {
                                    await u.PutAsync(newID, ct).ConfigureAwait(false);  // Add the new entity to the unit of work
                                    await u.CommitAsync(ct).ConfigureAwait(false);  // Commit the transaction to persist changes
                                }
                            }
                            else
                            {

                                throw new HttpException(681, "Invalid display name, display name must start with aad");
                            }

                            

                        }
                        else
                        {
                            throw new HttpException(681, "You dont have required permissions");
                        }




                    }));
        }*/

        //This class defines the data that will be sent from the client
        //to the server for the creation of the new aadgroup
        /*public class PostedAADGroup
        {
            public string DisplayName { get; set; }
            public string MailNickName { get; set; }
            public string Description { get; set; }
            public string UID_AADOrganization { get; set; }



        }*/


        //This class defines the type of data object that will be sent to the client (the response)
        public class DataObject
        {
            public string Message { get; set; }
        }

        //This class defines the type of data object that will be sent from the client to the server (request body)
        public class PostedMessage
        {
            public string Input { get; set; }
        }

        public class ReturnedName
        {
            // Properties to hold the details of the aadgroup
            public string DisplayName { get; set; }
            public string UID_AADOrganization { get; set; }
            public string MailNickName { get; set; }
            public string Description { get; set; }
            public string UID_AccProduct { get; set; }

            // Static method to create a ReturnedName instance from an IEntity object
            public static async Task<ReturnedName> fromEntity(IEntity entity, ISession session)
            {
                // Instantiate a new ReturnedName object and populate it with data from the entity
                var g = new ReturnedName
                {
                    // Asynchronously get the DisplayName value from the entity
                    DisplayName = await entity.GetValueAsync<string>("DisplayName").ConfigureAwait(false),

                    // Asynchronously get the UID_AADOrganization value from the entity
                    UID_AADOrganization = await entity.GetValueAsync<string>("UID_AADOrganization").ConfigureAwait(false),

                    // Asynchronously get the MailNickName value from the entity
                    MailNickName = await entity.GetValueAsync<string>("MailNickName").ConfigureAwait(false),

                    // Asynchronously get the Description value from the entity
                    Description = await entity.GetValueAsync<string>("Description").ConfigureAwait(false),

                    // Asynchronously get the UID_AccProduct value from the entity
                    UID_AccProduct = await entity.GetValueAsync<string>("UID_AccProduct").ConfigureAwait(false),
                };

                // Return the populated ReturnedName object
                return g;
            }
        }
    }
}

// This attribute will automatically assign all methods defined by this DLL
// to the CCC module.
//[assembly: Module("CCC")]







