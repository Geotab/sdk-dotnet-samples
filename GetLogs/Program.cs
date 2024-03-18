using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;

/***************************************************************
 * DISCLAIMER: This code example is provided for demonstration *
 * purposes only. Depending on the frequency at which it is   *
 * executed, it may be subject to rate limits imposed by APIs *
 * or other services it interacts with. It is recommended to   *
 * review and adjust the code as necessary to handle rate      *
 * limits or any other constraints relevant to your use case.  *
 ***************************************************************/


namespace Geotab.SDK.GetLogs
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {
        /// <summary>
        /// This is a Geotab API  example of downloading a device's logs.
        ///
        /// Steps:
        /// 1) Authenticate a user via login, password, database and server using the Geotab API object.
        /// 2) Search for a device by its serial number.
        /// 3) Get logs associated with the device for a given time period.
        ///
        /// A complete Geotab API object and method reference is available at the Geotab Developer page.
        /// </summary>
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length != 5)
                {
                    Console.WriteLine();
                    Console.WriteLine("Command line parameters:");
                    Console.WriteLine("dotnet run <server> <database> <username> <password> <serialNumber>");
                    Console.WriteLine();
                    Console.WriteLine("Command line:        dotnet run server database username password serialNumber");
                    Console.WriteLine("server             - The server name (Example: my.geotab.com)");
                    Console.WriteLine("database           - The database name (Example: G560)");
                    Console.WriteLine("username           - The Geotab user name");
                    Console.WriteLine("password           - The Geotab password");
                    Console.WriteLine("serialNumber       - Serial number of the device.");
                    Console.WriteLine();
                    return;
                }

                // Process command line arguments
                string server = args[0];
                string database = args[1];
                string username = args[2];
                string password = args[3];
                string serialNumber = args[4];

                // Create the Geotab API object used to make calls to the server
                // Note: server name should be the generic 'Federation' server as databases can be moved without notice.
                // For example; use "my.geotab.com" rather than "my3.geotab.com".
                var api = new API(username, password, null, database, server);

                try
                {
                    // Authenticate user
                    await api.AuthenticateAsync();
                    Console.WriteLine("Successfully Authenticated");
                }
                catch (InvalidUserException ex)
                {
                    Console.WriteLine($"Invalid user: {ex}");
                    return;
                }
                catch (DbUnavailableException ex)
                {
                    Console.WriteLine($"Database unavailable: {ex}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to authenticate user: {ex}");
                    return;
                }

                // Get Device by serial number
                Device device = null;
                try
                {
                    DeviceSearch deviceSearch = new()
                    {
                        SerialNumber = serialNumber
                    };
                    IList<Device> devices = await api.CallAsync<IList<Device>>("Get", typeof(Device), new { search = deviceSearch });
                    if (devices.Count > 0)
                    {
                        Console.WriteLine("Device found");
                        device = devices[0];
                    }
                    else
                    {
                        Console.WriteLine("Device not found");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get device: {ex}");
                }

                // Get logs for the Device
                try
                {
                    var toDate = DateTime.UtcNow;
                    var fromDate = toDate.AddDays(-7);
                    LogRecordSearch logRecordSearch = new()
                    {
                        DeviceSearch = new DeviceSearch(device.Id),
                        FromDate = fromDate,
                        ToDate = toDate
                    };
                    IList<LogRecord> logs = await api.CallAsync<IList<LogRecord>>("Get", typeof(LogRecord), new { search = logRecordSearch });

                    // Use a string builder for the results and limit the amount of data entered into the text box.
                    StringBuilder stringBuilder = new(10000);
                    if (logs.Count == 0)
                    {
                        stringBuilder.Append("No Logs Found");
                    }
                    else
                    {
                        // We will display the Lat, Lon, and Date of each Log as a row.
                        for (int i = 0; i < logs.Count; i++)
                        {
                            LogRecord logRecord = logs[i];
                            stringBuilder.Append("Lat: ");
                            stringBuilder.Append(logRecord.Latitude);
                            stringBuilder.Append(" Lon: ");
                            stringBuilder.Append(logRecord.Longitude);
                            stringBuilder.Append(" Date: ");
                            stringBuilder.Append(logRecord.DateTime);
                            stringBuilder.Append(Environment.NewLine);
                        }
                    }

                    // Display results
                    Console.WriteLine(stringBuilder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get logs: {ex}");
                }
            }
            catch (Exception ex)
            {
                // Show miscellaneous exceptions
                Console.WriteLine($"Unhandled exception: {ex}");
            }
            finally
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }
    }
}
