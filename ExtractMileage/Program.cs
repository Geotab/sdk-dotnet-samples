using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;

/***************************************************************
 * DISCLAIMER: This code example is provided for demonstration *
 * purposes only. Depending on the frequency at which it is   *
 * executed, it may be subject to rate limits imposed by APIs *
 * or other services it interacts with. It is recommended to   *
 * review and adjust the code as necessary to handle rate      *
 * limits or any other constraints relevant to your use case.  *
 ***************************************************************/


namespace Geotab.SDK.ExtractMileage
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {
        /// <summary>
        /// This is a Geotab API console example to download vehicle mileage to a CSV or XML file.
        ///
        /// Steps:
        /// 1) Create Geotab API object from supplied arguments and authenticate.
        /// 2) Get the odometer readings of each device and a VehicleWithMileage object.
        /// 3) Output the information to a CSV or XML file.
        ///
        /// A complete Geotab API object and method reference is available at the Geotab Developer page.
        /// </summary>
        /// <param name="args">The command line arguments for the application. Note: When debugging these can be added by: Right click the project > Properties > Debug Tab > Start Options: Command line arguments.</param>
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" Geotab SDK");
                Console.ForegroundColor = ConsoleColor.Gray;

                if (args.Length != 5)
                {
                    Console.WriteLine();
                    Console.WriteLine(" Command line parameters:");
                    Console.WriteLine(" dotnet run <server> <database> <username> <password> <filename.csv or filename.xml>");
                    Console.WriteLine();
                    Console.WriteLine(" Example: dotnet.run server database username password filename");
                    Console.WriteLine();
                    Console.WriteLine(" server     - Sever host name (Example: my.geotab.com)");
                    Console.WriteLine(" database   - Database name (Example: G560)");
                    Console.WriteLine(" username   - Geotab user name");
                    Console.WriteLine(" password   - Geotab password");
                    Console.WriteLine(" outputfile - File name of the output file (.csv or .xml)");

                    return;
                }

                // Command line argument variables
                var server = args[0];
                var database = args[1];
                var username = args[2];
                var password = args[3];
                var fileName = args[4];

                var utcNow = DateTime.UtcNow;

                Console.WriteLine();
                Console.WriteLine(" Creating API...");

                // Create Geotab API object.
                // It is important to create this object with the base "Federation" server (my.geotab.com) NOT the specific server (my3.geotab.com).
                // A database can be moved to another server without notice.
                var api = new API(username, password, null, database, server);

                // Make API call for all devices
                Console.WriteLine(" Retrieving devices...");
                var devices = await api.CallAsync<IList<Device>>("Get", typeof(Device));

                // The list of all vehicle vehicle readings
                var odometerReadings = new List<VehicleWithMileage>(devices.Count);

                Console.Write(" ");
                Console.ForegroundColor = ConsoleColor.White;

                for (int i = 0; i < devices.Count; i++)
                {
                    Device device = devices[i];

                    // Search for status data based on the current device and the odometer reading
                    var statusDataSearch = new StatusDataSearch
                    {
                        DeviceSearch = new DeviceSearch(device.Id),
                        DiagnosticSearch = new DiagnosticSearch(KnownId.DiagnosticOdometerAdjustmentId),
                        FromDate = DateTime.MaxValue
                    };

                    // Retrieve the odometer status data
                    IList<StatusData> statusData = await api.CallAsync<IList<StatusData>>("Get", typeof(StatusData), new { search = statusDataSearch });

                    var odometerReading = statusData[0].Data ?? 0;
                    odometerReadings.Add(new VehicleWithMileage(device, odometerReading));

                    Console.Write(".");
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();

                // Write the results to an XML or CSV file
                if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    WriteXml(odometerReadings, fileName, utcNow);
                }
                else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    WriteCsv(odometerReadings, fileName, utcNow);
                }
                else 
                {
                    Console.WriteLine(" Unknown file type");
                }

                Console.WriteLine();
                Console.WriteLine(" Extract complete");
            }
            catch (Exception exception)
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

        static string ToFriendlyDate(DateTime? value) => value.HasValue ? value.Value.ToLocalTime().ToString("yyyyMMdd HH:mm:ss") : "";

        // Writes a CSV file
        static void WriteCsv(IEnumerable<VehicleWithMileage> odometerReadings, string fileName, DateTime utcDate)
        {
            using (var writer = new StreamWriter(fileName))
            {
                foreach (var odometerReading in odometerReadings)
                {
                    writer.WriteLine(
                        $"{odometerReading.Vehicle.SerialNumber},{odometerReading.Vehicle.Name},{Math.Round(RegionInfo.CurrentRegion.IsMetric ? odometerReading.Mileage : Distance.ToImperial(odometerReading.Mileage / 1000), 0)},{ToFriendlyDate(utcDate.ToLocalTime())}");
                }
            }
        }

        // Writes a XML file
        static void WriteXml(IEnumerable<VehicleWithMileage> odometerReadings, string fileName, DateTime utcDate)
        {
            using (var writer = new XmlTextWriter(fileName, Encoding.Unicode))
            {
                var isMetric = RegionInfo.CurrentRegion.IsMetric;

                writer.WriteStartDocument();
                writer.WriteStartElement("MileageExtract");

                foreach (var odometerReading in odometerReadings)
                {
                    writer.WriteStartElement("Vehicle");
                    writer.WriteStartElement("SerialNumber");
                    writer.WriteString(odometerReading.Vehicle.SerialNumber);
                    writer.WriteEndElement();
                    writer.WriteStartElement("Description");
                    writer.WriteString(odometerReading.Vehicle.Name);
                    writer.WriteEndElement();
                    writer.WriteStartElement("Odometer");
                    writer.WriteString(Math.Round(isMetric ? odometerReading.Mileage : Distance.ToImperial(odometerReading.Mileage / 1000), 0).ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                    writer.WriteStartElement("ExtractDate");
                    writer.WriteString(ToFriendlyDate(utcDate.ToLocalTime()));
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }
    }
}
