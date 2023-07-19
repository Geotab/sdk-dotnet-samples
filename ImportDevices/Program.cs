using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Collections;

namespace Geotab.SDK.ImportDevices
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Searches for and returns a group from a flat list of groups.
        /// </summary>
        /// <param name="id">The group id to search for.</param>
        /// <param name="groups">The group collection to search in.</param>
        /// <returns>The found group or null if not found.</returns>
        static Group GetGroup(string id, IList<Group> groups)
        {
            return groups.FirstOrDefault(group => group.Id.ToString() == id);
        }

        /// <summary>
        /// Loads a csv file and processes rows into a collection of DeviceRow
        /// </summary>
        /// <param name="fileName">The csv file name</param>
        /// <returns>A collection of DeviceRow</returns>
        static List<DeviceRow> LoadDevicesFromCSV(string fileName)
        {
            List<DeviceRow> deviceRows = new List<DeviceRow>();
            int count = 0;
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    // Consider lines starting with # to be comments
                    if (line.StartsWith("#", StringComparison.Ordinal))
                    {
                        count++;
                        continue;
                    }

                    try
                    {
                        // Create DeviceRow from line columns
                        DeviceRow device = new DeviceRow();
                        string[] columns = line.Split(',');
                        device.Description = columns[0];
                        device.SerialNumber = columns[1];
                        device.NodeId = columns.Length > 2 ? columns[2] : "";
                        device.Vin = columns.Length > 3 ? columns[3] : "";
                        deviceRows.Add(device);
                        count++;
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Invalid row: {count} {exception.Message}");
                    }
                }
            }
            return deviceRows;
        }

        /// <summary>
        /// This is a console example of importing devices from a .csv file.
        /// 1) Process command line arguments: Server, Database, Username, Password, Options and Load .csv file.
        /// Note: the .csv file in this project is a sample, you may need to change entries (such as group names) for the example to work.
        /// 2) Create Geotab API object and Authenticate.
        /// 3) Import devices into database.
        /// A complete Geotab API object and method reference is available at the Geotab Developer page.
        /// </summary>
        /// <param name="args">The command line arguments for the application. Note: When debugging these can be added by: Right click the project &gt; Properties &gt; Debug Tab &gt; Start Options: Command line arguments.</param>
        static async Task Main(string[] args)
        {
            try
            {
                // 1. Process command line arguments
                if (args.Length != 5)
                {
                    Console.WriteLine();
                    Console.WriteLine("Command line parameters:");
                    Console.WriteLine("dotnet run <server> <database> <username> <password> <inputfile>");
                    Console.WriteLine();
                    Console.WriteLine("Command line:      dotnet run server database username password inputfile");
                    Console.WriteLine("server           - The server name (Example: my.geotab.com)");
                    Console.WriteLine("database         - The database name (Example: G560)");
                    Console.WriteLine("username         - The MyGeotab user name.");
                    Console.WriteLine("password         - The MyGeotab password.");
                    Console.WriteLine("inputfile        - File name of the CSV file to import.");
                    Console.WriteLine();
                    return;
                }

                // Process command line arguments
                string server = args[0];
                string database = args[1];
                string username = args[2];
                string password = args[3];
                string fileName = args[4];

                // 5. Load DeviceRow collection from the given .csv file
                Console.WriteLine("Loading .csv file...");
                List<DeviceRow> deviceRows;
                try
                {
                    deviceRows = LoadDevicesFromCSV(fileName);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Could not load CSV file: {exception.Message}");
                    return;
                }

                // 2. Create Geotab API object
                API api = new API(username, password, null, database, server);

                // 3. Authenticate
                Console.WriteLine("Authenticating...");
                try
                {
                    await api.AuthenticateAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not authenticate: \n{ex.Message}");
                }


                // 6. Start import
                Console.WriteLine("Importing...");

                IList<Group> groups = await api.CallAsync<IList<Group>>("Get", typeof(Group));
                List<Group> assetTypeGroups = GetAllChildrenOfGroup("GroupAssetTypeId", groups);

                // 7. Iterate through each row, validate the entries, and add them if the validation passes.
                foreach (DeviceRow device in deviceRows)
                {
                    bool deviceRejected = false;
                    IList<Group> newDeviceGroups = new List<Group>();

                    // A devices and nodes have a many to many relationship.
                    // In the .csv file if a device belongs to multiple nodes we separate with a pipe character.
                    string[] groupIds = (device.NodeId ?? "").Split('|');

                    // Verify that there is one asset type is assigned to the device.
                    // Assets are restricted to one asset type (although the API call does not enforce this).
                    // If multiple asset types are detected, the user will be prompted in the database UI to remove all except one.
                    if (AssetTypeCount(groupIds, assetTypeGroups) < 1)
                    {
                        Console.WriteLine($"Rejected: '{device.Description}'. Does not have asset type.");
                        deviceRejected = true;
                    }
                    else if (AssetTypeCount(groupIds, assetTypeGroups) > 1)
                    {
                        Console.WriteLine($"Rejected: '{device.Description}'. Has too many asset types.");
                        deviceRejected = true;
                    }
                    else
                    {

                        // Iterate through the group names and try to assign each group to the device looking it up from the allNodes collection.
                        foreach (string groupId in groupIds)
                        {
                            // No need to add GroupCompanyId. Asset Types are children of GroupCompanyId and will cause an error if added
                            if (groupId != "GroupCompanyId")
                            {
                                // Verify that the groups specified in the CSV file exist in the database and that the current user has access to them.
                                Group group = GetGroup(groupId.Trim(), groups);
                                if (group == null)
                                {
                                    Console.WriteLine($"Rejected: '{device.Description}'. Group: '{groupId}' does not exist.");
                                    deviceRejected = true;
                                    break;
                                }
                                // Add group to device nodes collection.
                                newDeviceGroups.Add(group);
                            }
                        }
                    }

                    // If the device is rejected, move on the the next device row.
                    if (deviceRejected)
                    {
                        continue;
                    }
                    try
                    {

                        // Create the device object.
                        if (!string.IsNullOrEmpty(device.SerialNumber))
                        {
                            if (device.SerialNumber.StartsWith("G"))
                            {

                                // Use GoDevice instead of Device because CustomVehicleDevice includes the VehicleIdentificationNumber attribute, which is not present in the Device.
                                GoDevice newDevice = (GoDevice)Device.FromSerialNumber(device.SerialNumber);
                                newDevice.PopulateDefaults();
                                newDevice.Name = device.Description;
                                newDevice.Groups = (newDeviceGroups.Count > 0) ? newDeviceGroups : null;
                                newDevice.VehicleIdentificationNumber = device.Vin ?? null;
                                await api.CallAsync<Id>("Add", typeof(Device), new { entity = newDevice });
                            }
                            else
                            {

                                // Use CustomVehicleDevice instead of CustomDevice because CustomVehicleDevice includes the VehicleIdentificationNumber attribute, which is not present in the CustomDevice.
                                CustomVehicleDevice newDevice = (CustomVehicleDevice)Device.FromSerialNumber(device.SerialNumber);
                                newDevice.PopulateDefaults();
                                newDevice.Name = device.Description;
                                newDevice.Groups = (newDeviceGroups.Count > 0) ? newDeviceGroups : null;
                                newDevice.VehicleIdentificationNumber = device.Vin ?? null;
                                await api.CallAsync<Id>("Add", typeof(Device), new { entity = newDevice });
                            }
                        }
                        else
                        {

                            // Use UntrackedAsset instead of Device because CustomVehicleDevice includes the VehicleIdentificationNumber attribute, which is not present in the Device.
                            UntrackedAsset newDevice = new UntrackedAsset
                            {
                                Name = device.Description,
                                SerialNumber = "",
                                Groups = (newDeviceGroups.Count > 0) ? newDeviceGroups : null,
                                VehicleIdentificationNumber = device.Vin ?? null
                            };
                            newDevice.PopulateDefaults();

                            // Add the device.
                            await api.CallAsync<Id>("Add", typeof(Device), new { entity = newDevice });
                        }
                        Console.WriteLine($"Added: '{device.Description}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: '{device.Description}'. {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Recursively retrieves all groups under a specified group.
        /// </summary>
        /// <param name="groupId">The group id to search for.</param>
        /// <param name="groupList">The group collection to search in.</param>
        /// <returns>A list of all child groups found, including the starting group. Returns null if the starting group is not found.</returns>
        static List<Group> GetAllChildrenOfGroup(string groupId, IList<Group> groupList)
        {
            foreach (Group group in groupList)
            {
                if (group.Id.ToString() == groupId)
                {
                    List<Group> list = new List<Group>();
                    list.Add(group);
                    foreach (Group child in group.Children)
                    {
                        list.AddRange(GetAllChildrenOfGroup(child.Id.ToString(), groupList));
                    }
                    return list;
                }
            }
            return null;
        }

        /// <summary>
        /// Counts the number of asset types in a given list of group IDs.
        /// </summary>
        /// <param name="groupIds">The array of group IDs to search for.</param>
        /// <param name="allAssetTypes">The list of all asset types.</param>
        /// <returns>The count of asset types present in the given group IDs.</returns>
        static int AssetTypeCount(string[] groupIds, IList<Group> allAssetTypes)
        {
            return groupIds.Count(item => allAssetTypes.Any(group => group.Id.ToString() == item));
        }
    }
}