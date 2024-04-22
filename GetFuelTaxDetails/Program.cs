using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Fuel;

/***************************************************************
 * DISCLAIMER: This code example is provided for demonstration *
 * purposes only. Depending on the frequency at which it is   *
 * executed, it may be subject to rate limits imposed by APIs *
 * or other services it interacts with. It is recommended to   *
 * review and adjust the code as necessary to handle rate      *
 * limits or any other constraints relevant to your use case.  *
 ***************************************************************/


namespace Geotab.SDK.GetFuelTaxDetails
{
    static class Program
    {
        /// <summary>
        /// The amount of ticks in an hour.
        /// </summary>
        const long HourTicks = TimeSpan.TicksPerHour;

        static async Task Main(string[] args)
        {
            try
            {
                // Validate the command line.
                if (args.Length != 3)
                {
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("dotnet run <database> <username> <password>");
                    Console.WriteLine();
                    Console.WriteLine("  database - Database name (e.g. g560)");
                    Console.WriteLine("  username - Geotab user name");
                    Console.WriteLine("  password - Geotab password");
                    return;
                }

                // Retrieve the database name and credentials from the command line.
                string database = args[0];
                string username = args[1];
                string password = args[2];

                // Fuel tax details support the following attributes: jurisdiction, toll road, and authority. To forego toll road identification, authority identification, or fuel usage calculation, set the corresponding option(s) to <c>false</c>.
                dynamic options = new
                {
                    DriverIdentification = true,
                    TollRoadIdentification = true,
                    AuthorityIdentification = true,
                    FuelUsage = true
                };

                // Set the beginning of the time interval. It will be extended to the nearest hour. For example, 4:20:00 will become 4:00:00.
                DateTime fromDate = new DateTime(2023, 8, 1, 5, 0, 0, DateTimeKind.Utc);

                // Set the end of the time interval. It will be extended to the nearest hour. For example, 3:45:00 will become 4:00:00.
                DateTime toDate = new DateTime(2023, 8, 2, 5, 0, 0, DateTimeKind.Utc);

                // Create the Geotab API object.
                // A database can be moved to another server without notice.
                Console.WriteLine();
                Console.WriteLine("Creating API...");
                const string server = "my.geotab.com";
                API api = new API(username, password, null, database, server);

                // The example code will retrieve fuel tax details for one device at a time. For smaller fleets, it is feasible to retrieve the details for all devices by removing the device search from the search object below.
                Console.WriteLine("Retrieving devices...");
                Id groupVehicleId = KnownId.GroupVehicleId;
                IList<Device> devices = await api.CallAsync<IList<Device>>("Get", typeof(Device), new { search = new DeviceSearch { Groups = new List<GroupSearch> { new GroupSearch(groupVehicleId) }} } );

                // Get the fuel tax details restricted to the time interval, grouped by device, and sorted by enter time.
                Console.WriteLine("Retrieving fuel tax details...");
                List<FuelTaxDetail> details = new List<FuelTaxDetail>();
                var fuelUsageByDevice = new Dictionary<Device, Dictionary<string, Dictionary<FuelType, FuelUsage>>>();
                foreach (var device in devices)
                {
                    Search fuelTaxDetailSearch = new FuelTaxDetailSearch
                    {
                        DeviceSearch = new DeviceSearch(device.Id),
                        FromDate = fromDate,
                        ToDate = toDate,
                        IncludeHourlyData = false,
                        IncludeBoundaries = false
                    };
                    List<FuelTaxDetail> deviceDetails = (await api.CallAsync<IList<FuelTaxDetail>>("Get", typeof(FuelTaxDetail), new { search = fuelTaxDetailSearch })).ToList();
                    if (deviceDetails.Count > 0)
                    {
                        // Group successive details depending on the options.
                        List<List<FuelTaxDetail>> groups = Group(deviceDetails, options);
                        List<FuelTaxDetail> mergedDetails = Merge(groups);
                        details.AddRange(deviceDetails);
                        if (options.FuelUsage)
                        {
                            if (device is GoDevice)
                            {
                                var deviceFuelUsage = await GetFuelUsageByJurisdictionAsync(api, device as GoDevice, deviceDetails);
                                fuelUsageByDevice[device] = deviceFuelUsage;
                            }
                        }
                    }
                }
                Console.WriteLine($"{details.Count} fuel tax details ready.");
                if (options.FuelUsage)
                {
                    Console.WriteLine($"Fuel usage for {fuelUsageByDevice.Count} devices ready.");
                }
                // Print fuel tax detail results in terminal.
                foreach(var detail in details){
                    Console.WriteLine($"Device: {detail.Device} | Driver: {detail.Driver} | EnterTime: {detail.EnterTime} | EnterOdometer: {detail.EnterOdometer} | ExitTime: {detail.ExitTime} | ExitOdometer: {detail.ExitOdometer}");
                }
                // Write and save the results in csv file based on the user's answer.
                Console.WriteLine("Do you want to download .csv file? (yes/no)");
                string name = Console.ReadLine();
                if (name.ToLower() == "yes")
                {
                    string fileName = "sample.csv";
                    string downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                    string downloadFilePath = Path.Combine(downloadFolderPath, Path.GetFileName(fileName));
                    try
                    {
                        WriteDataToCsv(downloadFilePath, details);
                        Console.WriteLine($"CSV file created and downloaded to: {downloadFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while downloading the file: {ex.Message}");
                    }
                }
            }
            catch (InvalidUserException)
            {
                // Invalid credentials.
                Console.WriteLine("Incorrect database name, user name or password");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception: {exception.Message}\n\n{exception.StackTrace}");
            }
            finally
            {
                Console.WriteLine();
                Console.Write("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Groups successive <see cref="FuelTaxDetail"/> elements by their attributes, depending on the <see cref="options"/>.
        /// </summary>
        /// <param name="details">The details.</param>
        /// <param name="options">The options.</param>
        /// <returns>A list of detail groups.</returns>
        static List<List<FuelTaxDetail>> Group(IList<FuelTaxDetail> details, dynamic options)
        {
            List<List<FuelTaxDetail>> groups = new List<List<FuelTaxDetail>> { new List<FuelTaxDetail>() };
            List<FuelTaxDetail> group = groups[0];
            FuelTaxDetail previousDetail = null;
            foreach (var detail in details)
            {
                if (previousDetail == null || AreAttributesEqual(detail, previousDetail, options))
                {
                    group.Add(detail);
                }
                else
                {
                    group = new List<FuelTaxDetail> { detail };
                    groups.Add(group);
                }
                previousDetail = detail;
            }
            return groups;
        }

        /// <summary>
        /// Compares two <see cref="FuelTaxDetail"/> elements based on their attributes.
        /// </summary>
        /// <param name="detail1">The first detail.</param>
        /// <param name="detail2">The second detail.</param>
        /// <param name="options">The grouping options.</param>
        /// <returns><c>true</c> iff both details share the same attributes, depending on the <see cref="options"/>.</returns>
        static bool AreAttributesEqual(FuelTaxDetail detail1, FuelTaxDetail detail2, dynamic options)
        {
            return detail1.Jurisdiction == detail2.Jurisdiction
                && (!options.DriverIdentification || detail1.Driver.Equals(detail2.Driver))
                && (!options.TollRoadIdentification || detail1.TollRoad == detail2.TollRoad)
                && (!options.AuthorityIdentification || detail1.Authority == detail2.Authority);
        }

        /// <summary>
        /// Merges successive <see cref="FuelTaxDetail"/> elements that share the same attributes into one detail.
        /// </summary>
        /// <param name="groups">Groups of details that share the same attributes.</param>
        /// <returns>The resulting merged details.</returns>
        static List<FuelTaxDetail> Merge(List<List<FuelTaxDetail>> groups)
        {
            var groupedDetails = new List<FuelTaxDetail>();
            foreach (var group in groups)
            {
                var detail = group[0];
                var detailCount = group.Count;
                if (detailCount > 1)
                {
                    var lastDetail = group[detailCount - 1];
                    detail.ExitTime = lastDetail.ExitTime;
                    detail.ExitOdometer = lastDetail.ExitOdometer;
                    detail.ExitGpsOdometer = lastDetail.ExitGpsOdometer;
                    detail.ExitLatitude = lastDetail.ExitLatitude;
                    detail.ExitLongitude = lastDetail.ExitLongitude;
                    detail.IsExitOdometerInterpolated = lastDetail.IsExitOdometerInterpolated;
                    for (var detailIndex = 1; detailIndex < detailCount; detailIndex++)
                    {
                        var nextDetail = group[detailIndex];
                        if (nextDetail.EnterTime.Ticks % TimeSpan.TicksPerHour == 0)
                        {
                            detail.HourlyOdometer.Add(nextDetail.EnterOdometer);
                            detail.HourlyGpsOdometer.Add(nextDetail.EnterGpsOdometer);
                            detail.HourlyLatitude.Add(nextDetail.EnterLatitude);
                            detail.HourlyLongitude.Add(nextDetail.EnterLongitude);
                            detail.HourlyIsOdometerInterpolated.Add(nextDetail.IsEnterOdometerInterpolated);
                        }
                        for (int hourIndex = 0; hourIndex < nextDetail.HourlyOdometer.Count; hourIndex++)
                        {
                            detail.HourlyOdometer.Add(nextDetail.HourlyOdometer[hourIndex]);
                            detail.HourlyGpsOdometer.Add(nextDetail.HourlyGpsOdometer[hourIndex]);
                            detail.HourlyLatitude.Add(nextDetail.HourlyLatitude[hourIndex]);
                            detail.HourlyLongitude.Add(nextDetail.HourlyLongitude[hourIndex]);
                            detail.HourlyIsOdometerInterpolated.Add(nextDetail.HourlyIsOdometerInterpolated[hourIndex]);
                        }
                    }
                }
                groupedDetails.Add(detail);
            }
            return groupedDetails;
        }

        /// <summary>
        /// Calculates fuel usage for a collection of fuel tax details, classified by jurisdiction and fuel type.
        /// </summary>
        /// <param name="api">The Geotab API.</param>
        /// <param name="device">The device.</param>
        /// <param name="details">The fuel tax details.</param>
        /// <returns>A list of fuel tax data objects.</returns>
        static async Task<Dictionary<string, Dictionary<FuelType, FuelUsage>>> GetFuelUsageByJurisdictionAsync(API api, GoDevice device, IList<FuelTaxDetail> details)
        {
            var fuelUsageByJurisdiction = new Dictionary<string, Dictionary<FuelType, FuelUsage>>();

            // Get the details' time interval.
            DateTime fromDate = details[0].EnterTime;
            DateTime toDate = details[details.Count - 1].ExitTime;

            // Get the fuel transactions within the details' time interval.
            Search fuelTransactionSearch = new FuelTransactionSearch
            {
                VehicleIdentificationNumber = device.VehicleIdentificationNumber,
                FromDate = fromDate,
                ToDate = toDate
            };
            List<FuelTransaction> fuelTransactions = (await api.CallAsync<IList<FuelTransaction>>("Get", typeof(FuelTransaction), new { search = fuelTransactionSearch })).ToList();
            fuelTransactions.Sort((transaction1, transaction2) => DateTime.Compare(transaction1.DateTime.Value, transaction2.DateTime.Value));

            // Calculate total purchased fuel by fuel type and jurisdiction.
            Dictionary<FuelType, double> fuelPurchasedByType = new Dictionary<FuelType, double>();
            double totalDistance = 0;
            foreach (var fuelTransaction in fuelTransactions)
            {
                // Locate the fuel transaction within a detail.
                FuelTaxDetail detail = details.Last(x => x.EnterTime <= fuelTransaction.DateTime);

                // Update jurisdiction fuel usage.
                string jurisdiction = detail.Jurisdiction;
                if (jurisdiction != null)
                {
                    if (!fuelUsageByJurisdiction.TryGetValue(jurisdiction, out Dictionary<FuelType, FuelUsage> fuelUsageByType))
                    {
                        fuelUsageByType = new Dictionary<FuelType, FuelUsage>();
                        fuelUsageByJurisdiction.Add(jurisdiction, fuelUsageByType);
                    }
                    FuelTransactionProductType? productType = fuelTransaction.ProductType;
                    FuelType fuelType = productType == null ? FuelType.None : GetFuelType(productType.Value);
                    if (!fuelUsageByType.TryGetValue(fuelType, out FuelUsage fuelUsage))
                    {
                        fuelUsage = new FuelUsage();
                        fuelUsageByType.Add(fuelType, fuelUsage);
                    }
                    double volume = fuelTransaction.Volume.Value;
                    fuelUsage.FuelPurchased += volume;
                    if (!fuelPurchasedByType.ContainsKey(fuelType))
                    {
                        fuelPurchasedByType.Add(fuelType, 0);
                    }
                    fuelPurchasedByType[fuelType] += volume;
                }
            }

            // Resolve fuel type None into the fuel type with the largest purchased volume.
            if (fuelPurchasedByType.TryGetValue(FuelType.None, out double _))
            {
                FuelType topFuelType = FuelType.None;
                double topVolume = 0;
                foreach (var fuelTypePurchased in fuelPurchasedByType)
                {
                    FuelType fuelType = fuelTypePurchased.Key;
                    if (fuelType != FuelType.None)
                    {
                        double volume = fuelTypePurchased.Value;
                        if (volume > topVolume)
                        {
                            topVolume = volume;
                            topFuelType = fuelType;
                        }
                    }
                }
                if (topFuelType != FuelType.None)
                {
                    fuelPurchasedByType[topFuelType] += fuelPurchasedByType[FuelType.None];
                    fuelPurchasedByType.Remove(FuelType.None);
                }
                foreach (var fuelUsageByType in fuelUsageByJurisdiction.Values)
                {
                    if (fuelUsageByType.TryGetValue(FuelType.None, out FuelUsage typeNoneFuelUsage))
                    {
                        fuelUsageByType[topFuelType].FuelPurchased += typeNoneFuelUsage.FuelPurchased;
                        fuelUsageByType.Remove(FuelType.None);
                    }
                }
            }

            // Create fuel usage stumps for jurisdictions where no fuel transactions occurred.
            foreach (var detail in details)
            {
                string jurisdiction = detail.Jurisdiction;
                if (jurisdiction != null)
                {
                    if (!fuelUsageByJurisdiction.TryGetValue(jurisdiction, out Dictionary<FuelType, FuelUsage> fuelUsageByType))
                    {
                        fuelUsageByType = new Dictionary<FuelType, FuelUsage>();
                        fuelUsageByJurisdiction.Add(jurisdiction, fuelUsageByType);
                    }
                    double detailDistance = detail.ExitOdometer - detail.EnterOdometer;
                    foreach (var fuelType in fuelPurchasedByType.Keys)
                    {
                        if (!fuelUsageByType.TryGetValue(fuelType, out FuelUsage fuelUsage))
                        {
                            fuelUsage = new FuelUsage();
                            fuelUsageByType.Add(fuelType, fuelUsage);
                        }
                        fuelUsage.Distance += detailDistance;
                    }
                    totalDistance += detailDistance;
                }
            }

            // Calculate fuel economy for each fuel type. Fill in fuel usage per jurisdiction and fuel type.
            foreach (var typeVolume in fuelPurchasedByType)
            {
                FuelType fuelType = typeVolume.Key;
                double totalVolume = typeVolume.Value;
                double volumePerKm = totalVolume / totalDistance;
                foreach (var fuelUsageByType in fuelUsageByJurisdiction.Values)
                {
                    FuelUsage fuelUsage = fuelUsageByType[fuelType];
                    fuelUsage.FuelUsed = volumePerKm * fuelUsage.Distance;
                    fuelUsage.FuelEconomy = volumePerKm * 100;
                }
            }
            return fuelUsageByJurisdiction;
        }

        /// <summary>
        /// Write fuel details data to csv file.
        /// </summary>
        /// <param name="downloadFilePath">The download file path.</param>
        /// <param name="details">The list of FuelTaxDetail objects.</param>
        static void WriteDataToCsv(string downloadFilePath, List<FuelTaxDetail> details)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(downloadFilePath))
                {
                    writer.WriteLine("Driver, Device, EnterTime, EnterOdometer, ExitTime, ExitOdometer");
                    foreach (FuelTaxDetail detail in details)
                    {
                        writer.WriteLine($"{detail.Driver}, {detail.Device}, {detail.EnterTime}, {detail.EnterOdometer}, {detail.ExitTime}, {detail.ExitOdometer}");
                    }
                }

                Console.WriteLine("Data written to CSV file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while writing to the file: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts fuel product type to fuel tax fuel type.
        /// </summary>
        /// <param name="productType">The product type.</param>
        /// <returns>The fuel tax fuel type.</returns>
        static FuelType GetFuelType(FuelTransactionProductType productType)
        {
            switch (productType)
            {
                case FuelTransactionProductType.NonFuel:
                    return FuelType.NonFuel;
                case FuelTransactionProductType.Regular:
                case FuelTransactionProductType.Midgrade:
                case FuelTransactionProductType.Premium:
                case FuelTransactionProductType.Super:
                    return FuelType.Gasoline;
                case FuelTransactionProductType.Diesel:
                    return FuelType.Diesel;
                case FuelTransactionProductType.E85:
                    return FuelType.Ethanol;
                case FuelTransactionProductType.CNG:
                    return FuelType.CNG;
                case FuelTransactionProductType.LPG:
                    return FuelType.LPG;
                default:
                    return FuelType.None;
            }
        }

        enum FuelType
        {
            None,
            NonFuel,
            Gasoline,
            Diesel,
            Ethanol,
            CNG,
            LPG
        }

        class FuelUsage
        {
            public double FuelPurchased { get; set; }
            public double FuelUsed { get; set; }
            public double NetTaxableFuel => FuelUsed - FuelPurchased;
            public double FuelEconomy { get; set; }
            public double Distance { get; set; }
        }
    }
}
