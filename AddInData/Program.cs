using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Exception = System.Exception;

/***************************************************************
 * DISCLAIMER: This code example is provided for demonstration *
 * purposes only. Depending on the frequency at which it is   *
 * executed, it may be subject to rate limits imposed by APIs *
 * or other services it interacts with. It is recommended to   *
 * review and adjust the code as necessary to handle rate      *
 * limits or any other constraints relevant to your use case.  *
 ***************************************************************/

namespace Geotab.SDK.StorageApi
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                System.Console.WriteLine("Geotab SDK");
                System.Console.WriteLine("________________________________________________________________");
                System.Console.WriteLine("________________________________________________________________");
                System.Console.WriteLine("");
                System.Console.WriteLine("Sample Application to demonstrate MyGeotab Storage API");
                if (args.Length != 4)
                {
                    System.Console.WriteLine("ERROR: Arguments not provided");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("Command line parameters:");
                    System.Console.WriteLine("dotnet run <server> <database> <username> <password>");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("Example: dotnet run server database username password");
                    System.Console.WriteLine("server   - Server host name (Example: my.geotab.com)");
                    System.Console.WriteLine("database - Database name (Example: G560)");
                    System.Console.WriteLine("username - Geotab user name");
                    System.Console.WriteLine("password - Geotab password");
                }
                else
                {
                    API api = Helpers.InitializeArgs();

                    bool isAcceptingInput = true;
                    try
                    {
                        System.Console.WriteLine("Authenticating...");
                        await api.AuthenticateAsync();
                        System.Console.WriteLine("Done.");

                        while (isAcceptingInput)
                        {
                            System.Console.WriteLine("");
                            System.Console.WriteLine("Main Menu Options:");
                            System.Console.WriteLine("\t1. Create a new object");
                            System.Console.WriteLine("\t2. Modify an existing object");
                            System.Console.WriteLine("\t3. Remove an existing object");
                            System.Console.WriteLine("\t4. Display Retrieve AddInData example with select and where clauses");
                            System.Console.WriteLine("\t5. Exit");
                            System.Console.WriteLine("");
                            System.Console.WriteLine("Please input a number corresponding to the following options and press the enter key:");
                            var operation_choice = Console.ReadLine();
                            switch (operation_choice)
                            {
                                case "1":
                                    {
                                        await Helpers.CaseCreateAddInDataAsync(api);
                                        break;
                                    }
                                case "2":
                                    {
                                        await Helpers.CaseModifyAddInDataAsync(api);
                                        break;
                                    }
                                case "3":
                                    {
                                        await Helpers.CaseRemoveAddInDataAsync(api);
                                        break;
                                    }
                                case "4":
                                    {
                                        await Helpers.CaseRetrieveAddInExampleAsync(api);
                                        break;
                                    }
                                case "5":
                                    {
                                        isAcceptingInput = false;
                                        break;
                                    }
                                default:
                                    {
                                        Console.Clear();
                                        System.Console.WriteLine($"ERROR: \"{operation_choice}\" is not a valid option");
                                        break;
                                    }
                            }
                        }
                    }
                    catch (InvalidUserException)
                    {
                        Console.WriteLine(" User name or password incorrect");
                        return;
                    }
                    catch (DbUnavailableException)
                    {
                        Console.WriteLine(" Database not found");
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($" Exception: {exception.Message}\n\n{exception.StackTrace}");
            }
            finally
            {
                Console.WriteLine();
                Console.Write("Press any key to close...");
                Console.ReadKey(true);
            }
        }
    }
}