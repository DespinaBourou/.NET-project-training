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


    
    public class Exercise3 : IApiProviderFor<PortalApiProject>
    {
        public void Build(IApiBuilder builder)
        {
            builder.AddMethod(Method.Define("aadgroup2/insertgroup")
                .Handle<PostedAADGroup>("POST", async (posted, qr, ct) =>
                {
                    //variables to hold data from the post request
                    Console.WriteLine("Hi");
                    string displayName = ""; //its necessary for the creation 
                    string mailNickName = ""; //alias //its necessary for the creation
                    string UID_AADOrganization = ""; //tenant //its necessary for the creation
                    string description = "";
                    string requiredAERole = "0d6c1a85-eac7-4c00-a580-c08cb8347351"; //uid of the required role
                    var uidperson = qr.Session.User().Uid;  // Gets the UID of the current user

                    //query to check if logged in identity has required ae role
                    var queryCheckIfHasAERole = Query.From("PersonInAERole")
                                                          .Select("*")
                                                          .Where(string.Format("UID_Person = '{0}' AND UID_AERole = '{1}'",
                                                          uidperson, requiredAERole));

                    // Attempt to retrieve the entity from the database asynchronously
                    var tryget = await qr.Session.Source()
                                        .TryGetAsync(queryCheckIfHasAERole, EntityLoadType.DelayedLogic, ct)
                                        .ConfigureAwait(false);
                    Console.WriteLine(tryget);
                    if (tryget.Success)
                    {
                        // Loop through each column in the posted data to extract values
                        foreach (var column in posted.columns)
                        {
                            // Check each column name and assign its value to the corresponding variable
                            if (column.column == "DisplayName")
                            {
                                displayName = column.value;
                            }

                            if (column.column == "MailNickName")
                            {
                                mailNickName = column.value;
                            }

                            if (column.column == "UID_AADOrganization")
                            {
                                UID_AADOrganization = column.value;
                            }
                            if (column.column == "Description")
                            {
                                description = column.value;
                            }
                        }

                            //check if display name starts w aad
                            if (displayName.StartsWith("aad"))
                        {

                            

                            // Create a new 'AADGroup' entity
                            var newAADG = await qr.Session.Source().CreateNewAsync("AADGroup",
                                new EntityParameters
                                {
                                    CreationType = EntityCreationType.DelayedLogic
                                }, ct).ConfigureAwait(false);

                            // Set the values for the new 'AADGroup' entity
                            await newAADG.PutValueAsync("DisplayName", displayName, ct).ConfigureAwait(false);
                            await newAADG.PutValueAsync("MailNickName", mailNickName, ct).ConfigureAwait(false);
                            await newAADG.PutValueAsync("UID_AADOrganization", UID_AADOrganization, ct).ConfigureAwait(false);
                            await newAADG.PutValueAsync("Description", description, ct).ConfigureAwait(false);


                            // Start Unit of Work to save the new entity to the database
                            using (var u = qr.Session.StartUnitOfWork())
                            {
                                await u.PutAsync(newAADG, ct).ConfigureAwait(false);  // Add the new entity to the unit of work
                                await u.CommitAsync(ct).ConfigureAwait(false);  // Commit the transaction to persist changes
                            }

                        }
                        else
                        {
                            //return "Error 681: bla bla";
                            throw new HttpException(681, string.Format("Invalid display name {0}, display name must start with aad",displayName));

                        }



                    }
                    else
                    {
                        throw new HttpException(681, "You dont have required permissions");
                    }

                }));


            builder.AddMethod(Method.Define("aadgroup3/updateobject")
                // Handle the POST request with input of type ChangedAADGroup
                .Handle<ChangedAADGroup>("POST", async (posted, qr, ct) =>
                {
                    // Variables to hold column data                    
                    var displayName = "";
                    var UID_AADOrganization = "";
                    var mailNickName = "";
                    var description = "";
                    //var UID_AccProduct = "";
                    var UIDAADGroupToChange = "";
                    string requiredAERole = "0d6c1a85-eac7-4c00-a580-c08cb8347351"; //uid of the required ae role
                    var uidperson = qr.Session.User().Uid;  // Gets the UID of the current user

                    //query to check if logged in identity has required ae role
                    var queryCheckIfHasAERole = Query.From("PersonInAERole")
                                                          .Select("*")
                                                          .Where(string.Format("UID_Person = '{0}' AND UID_AERole = '{1}'",
                                                          uidperson, requiredAERole));

                    // Attempt to retrieve the entity from the database asynchronously
                    var trygetaerole = await qr.Session.Source()
                                        .TryGetAsync(queryCheckIfHasAERole, EntityLoadType.DelayedLogic, ct)
                                        .ConfigureAwait(false);
                    if (trygetaerole.Success) //if identity has required ae role
                    {
                        // Extract the object key from the posted data
                        foreach (var column in posted.element)
                        {
                            if (column.column == "UID_AADGroup")
                            {
                                // Get the XObjectKey of the entity to be updated
                                UIDAADGroupToChange = column.value;
                            }
                        }
                        //query to find aad group with given uid
                        var queryCheckIfValidAADUID = Query.From("AADGroup")
                                                        .Select("*")
                                                        .Where(string.Format("UID_AADGroup='{0}'", UIDAADGroupToChange));
                        // Attempt to retrieve the entity from the database asynchronously
                        var trygetaadgroup = await qr.Session.Source()
                                            .TryGetAsync(queryCheckIfValidAADUID, EntityLoadType.DelayedLogic, ct)
                                            .ConfigureAwait(false);
                        if (trygetaadgroup.Success) //if provided uid corresponds to an aad group
                        {
                            // extract data from posted data and update the entity(the aad group) accordingly
                            // Loop through each column in the posted data to update the entity's properties
                            foreach (var column in posted.columns)
                            {
                                // Assign values based on column names and update the entity accordingly
                                if (column.column == "DisplayName")
                                {
                                    displayName = column.value;
                                    if (displayName.StartsWith("aad"))
                                    {
                                        await trygetaadgroup.Result.PutValueAsync("DisplayName", displayName, ct).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        throw new HttpException(681, "AAD group display name must start with aad");
                                    }

                                }
                                else if (column.column == "MailNickName")
                                {
                                    mailNickName = column.value;
                                    await trygetaadgroup.Result.PutValueAsync("MailNickName", mailNickName, ct).ConfigureAwait(false);
                                }
                                else if (column.column == "Description")
                                {
                                    description = column.value;
                                    await trygetaadgroup.Result.PutValueAsync("Description", description, ct).ConfigureAwait(false);
                                }
                                else if (column.column == "UID_AADOrganization")
                                {
                                    UID_AADOrganization = column.value;
                                    await trygetaadgroup.Result.PutValueAsync("UID_AADOrganization", UID_AADOrganization, ct).ConfigureAwait(false);
                                }
                            }
                                

                                // Start a unit of work to save changes to the database
                                using (var u = qr.Session.StartUnitOfWork())
                                {
                                    // Add the updated entity to the unit of work
                                    await u.PutAsync(trygetaadgroup.Result, ct).ConfigureAwait(false);

                                    // Commit the unit of work to persist changes
                                    await u.CommitAsync(ct).ConfigureAwait(false);
                                }                          

                        }
                        else //if provided uid does not correspond to an aad group
                        {
                            throw new HttpException(681, "Provided UID does not correspond to AAD Group");
                        }


                    }
                    else //if identity doesnt have required ae role
                    {
                        throw new HttpException(681, "Identity does not have required ae role");
                    }



                }));

            builder.AddMethod(Method.Define("AADGroupDelete")
               .Handle<DeleteAADGroup, DeleteReturnedClass>("DELETE", async (posted, qr, ct) =>
               {
                   string requiredAERole = "0d6c1a85-eac7-4c00-a580-c08cb8347351"; //uid of the required ae role
                   var uidperson = qr.Session.User().Uid;  // Gets the UID of the current user
                   string UIDAADGroupToDelete = "";

                   //query to check if logged in identity has required ae role
                   var queryCheckIfHasAERole = Query.From("PersonInAERole")
                                                         .Select("*")
                                                         .Where(string.Format("UID_Person = '{0}' AND UID_AERole = '{1}'", uidperson, requiredAERole));

                   // Attempt to retrieve the entity from the database asynchronously
                   var trygetaerole = await qr.Session.Source()
                                       .TryGetAsync(queryCheckIfHasAERole, EntityLoadType.DelayedLogic, ct)
                                       .ConfigureAwait(false);
                   if (trygetaerole.Success) //if identity has required ae role
                   {
                       UIDAADGroupToDelete = posted.AAD_UID_Delete;
                       //query to find aad group with given uid
                       var queryCheckIfValidAADUID = Query.From("AADGroup")
                                                       .Select("*")
                                                       .Where(string.Format("UID_AADGroup='{0}'", UIDAADGroupToDelete));
                       // Attempt to retrieve the entity from the database asynchronously
                       var tryGetAADGrouptoDelete = await qr.Session.Source()
                                           .TryGetAsync(queryCheckIfValidAADUID, EntityLoadType.DelayedLogic, ct)
                                           .ConfigureAwait(false);
                       if (tryGetAADGrouptoDelete.Success) //if provided uid corresponds to an aad group
                       {
                           // Start a unit of work for transactional database operations
                           using (var u = qr.Session.StartUnitOfWork())
                           {
                               // Get the entity to be deleted
                               var objecttodelete = tryGetAADGrouptoDelete.Result;

                               // Mark the entity for deletion
                               objecttodelete.MarkForDeletion();

                               // Save the changes to the unit of work
                               await u.PutAsync(objecttodelete, ct).ConfigureAwait(false);

                               // Commit the unit of work to persist changes to the database
                               await u.CommitAsync(ct).ConfigureAwait(false);
                           }
                       }
                       else //if provided uid does not respond to an aad group
                       {
                           // If the entity was not found, return an error with a custom message and error code
                           return await DeleteReturnedClass.Error(
                               string.Format("No aad group was found with uid '{0}'.", UIDAADGroupToDelete),
                               681
                           ).ConfigureAwait(false);
                       }
                       // Return a successful response by converting the entity to DeleteReturnedClass
                       return await DeleteReturnedClass.fromEntity(tryGetAADGrouptoDelete.Result, qr.Session).ConfigureAwait(false);
                   }
                   else //if identity does not have required role
                   {
                       return await DeleteReturnedClass.Error(
                                  "Identity does not have required ae role",
                                  681
                              ).ConfigureAwait(false);
                   }
               }));

        }


               }

        //class to represent the posted data structure,
        //which will be an object with one property named columns
        public class PostedAADGroup
        {
            public columnsarray[] columns { get; set; }  // Array named columns of columnsarray objects,
                                                         // where each columnsarray object
                                                         // has a column property and a value property

        }

    //alt to columnsarray
    //public string DisplayName { get; set; }
    //public string MailNickName { get; set; }
    //public string Description { get; set; }
    //public string UID_AADOrganization { get; set; }



    // Class to represent each "column" in the posted data (ie each object in the array)
    public class columnsarray
    {
        public string column { get; set; }  // Name of the column//
        public string value { get; set; }   // Value of the column
    }

    //class to represent the posted data structure,
    //which will be an object with a propery named element that will store the uid of the row to be updated
    //and a property named columns representing the new values
    public class ChangedAADGroup
        {

        // An array of columns representing the key(s) of the entity
        public columnsarray[] element { get; set; }

        // An array of columns representing the properties to update
        public columnsarray[] columns { get; set; }

    }
        //alt way for changedaadgroup
        //public string AAD_UID { get; set; }
        //public string DisplayName { get; set; }
        //public string MailNickName { get; set; }
        //public string Description { get; set; }
        //public string UID_AccProduct { get; set; }
        //public string UID_AADOrganization { get; set; }

        public class DeleteAADGroup
        {

            //property to hold the uid of the aad group to be deleted as a string
            public string AAD_UID_Delete { get; set; }
        }

    // Class representing the data structure of the returned class (output to the client)
    public class DeleteReturnedClass
    {
        // Property to hold the XObjectKey of the deleted entity
        public string UIDAAD { get; set; }

        // Property to hold any error message
        public string errormessage { get; set; }

        // Static method to create a ReturnedClass instance from an IEntity object
        public static async Task<DeleteReturnedClass> fromEntity(IEntity entity, ISession session)
        {
            // Instantiate a new ReturnedClass object
            var g = new DeleteReturnedClass
            {
                // Asynchronously get the XObjectKey value from the entity and assign it
                UIDAAD = await entity.GetValueAsync<string>("UID_AADGroup").ConfigureAwait(false)
            };

            // Return the populated ReturnedClass object
            return g;
        }

        // Static method to return a ReturnedClass instance containing an error message
        public static async Task<DeleteReturnedClass> ReturnObject(string data)
        {
            // Instantiate a new ReturnedClass object with the error message
            var x = new DeleteReturnedClass
            {
                errormessage = data
            };

            // Return the error-containing ReturnedClass object
            return x;
        }

        // Static method to throw an HTTP exception in case of an error
        // Parameters:
        // - mess: The error message to be displayed
        // - errorNumber: The HTTP error code corresponding to the error
        public static async Task<DeleteReturnedClass> Error(string mess, int errorNumber)
        {
            // Throw an HTTP exception with the provided error number and message
            throw new System.Web.HttpException(errorNumber, mess);
        }
    }



}



