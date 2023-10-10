using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Exception = System.Exception;

namespace Geotab.SDK.GetCount
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {
        /// <summary>
        /// This is a Geotab API console example to count active vehicles, trailers, and zones in your database.
        ///
        /// Steps:
        /// 1) Create API from command line arguments.
        /// 2) Authenticate the user.
        /// 3) Get the count of active vehicles, trailers, and zones.
        ///
        /// A complete Geotab API object and method reference is available on the Geotab Developer page.
        /// </summary>
        /// <param name="args">The command line arguments passed to the application.</param>
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" Geotab SDK");
                Console.ForegroundColor = ConsoleColor.Gray;

                if (args.Length != 4)
                {
                    Console.WriteLine();
                    Console.WriteLine(" Command line parameters:");
                    Console.WriteLine(" dotnet run <server> <database> <username> <password>");
                    Console.WriteLine();
                    Console.WriteLine(" Example: dotnet run server database username password");
                    Console.WriteLine();
                    Console.WriteLine(" server   - Server host name (Example: my.geotab.com)");
                    Console.WriteLine(" database - Database name (Example: G560)");
                    Console.WriteLine(" username - Geotab user name");
                    Console.WriteLine(" password - Geotab password");

                    return;
                }

                // Command line argument variables
                var server = args[0];
                var database = args[1];
                var username = args[2];
                var password = args[3];

                Console.WriteLine();
                Console.WriteLine(" Creating API...");

                // Create Geotab API object.
                // It is important to create this object with the base "Federation" server (my.geotab.com) NOT the specific server (my3.geotab.com).
                // A database can be moved to another server without notice.
                var api = new API(username, password, null, database, server);

                Console.WriteLine(" Authenticating...");

                // Authenticates the user and stores their credentials for further requests to the server.
                // It is not strictly necessary to call Authenticate. You can call api.Call and the user will be auto-authenticated.
                // However, there is a couple of reasons you may want call Authenticate before hand:
                //
                //  1) Authenticating can take some time. When we authenticate against the generic "Federation" server, the server
                //     hosting the database needs to be located and this could take a few seconds.
                //  2) To error trap in the case that the users credentials are invalid, erroneous etc. and prompt the
                //     user to re-enter their login credentials.
                try
                {
                    await api.AuthenticateAsync();
                }
                catch (InvalidUserException)
                {
                    // Here you can display the error and prompt for user to re-enter credentials
                    Console.WriteLine(" User name or password incorrect");
                    return;
                }
                catch (DbUnavailableException)
                {
                    // Here you can display the error and prompt for user to re-enter database
                    Console.WriteLine(" Database not found");
                    return;
                }

                // Make a call through the Geotab API for the count of zones. GetCountOf is a Generic method, meaning it can be called
                // against many different object types. So we specify the type we want to get the count of as well as the method name.
                var zoneCount = (await api.CallAsync<int?>("GetCountOf",  typeof(Zone))).Value;

                //Create a DeviceSearch object for active devices and filtering only active assets assigned to the Vehicle group
                DeviceSearch deviceSearch = new DeviceSearch
                {
                    FromDate = DateTime.UtcNow,
                    Groups = new List<GroupSearch>
                    {
                        new GroupSearch
                        {
                            Id = Id.Create("GroupVehicleId")
                        }
                    }
                };


                var vehicleCount = (await api.CallAsync<int?>("GetCountOf",  typeof(Device), new {search = deviceSearch })).Value;

                //Create a DeviceSearch object for devices and filtering only assets assigned to the Trailer group
                deviceSearch = new DeviceSearch
                {
                    Groups = new List<GroupSearch>
                    {
                        new GroupSearch
                        {
                            Id = Id.Create("GroupTrailerId")
                        }
                    }
                };

                var trailerCount = (await api.CallAsync<int?>("GetCountOf",  typeof(Device), new {search = deviceSearch })).Value;
                

                Console.WriteLine();
                Console.WriteLine($" Total Active Vehicles : {vehicleCount}");
                Console.WriteLine($" Total Trailers : {trailerCount}");
                Console.WriteLine($" Total Zoness : {zoneCount}");
            }catch(InvalidPermissionsException)
            {
                Console.WriteLine(" User does not have valid permissions");
                return;
            }catch (Exception exception)
            {
                Console.WriteLine($" Exception: {exception.Message}\n\n{exception.StackTrace}");
            }
            finally
            {
                Console.WriteLine();
                Console.Write(" Press any key to close...");
                Console.ReadKey(true);
            }
        }
    }
}
