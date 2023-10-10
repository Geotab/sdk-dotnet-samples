using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.AssetGroups;

namespace Geotab.SDK.ImportDevices
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {

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
                string server = args[0];
                string database = args[1];
                string username = args[2];
                string password = args[3];
                string fileName = args[4];

                // 2. Load DeviceRow collection from the given .csv file
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

                // 3. Create Geotab API object
                API api = new API(username, password, null, database, server);

                // 4. Authenticate
                Console.WriteLine("Authenticating...");
                await api.AuthenticateAsync();

                // 5. Start importing
                Console.WriteLine("Importing...");

                IList<Group> allGroups = await api.CallAsync<IList<Group>>("Get", typeof(Group));
                List<Group> assetTypes = GetGroupDescendants("GroupAssetTypeId", allGroups);
                IList<Group> nonAssetTypes = allGroups.Except(assetTypes).ToList(); ;

                // Iterate through each row, validate the entries, and add them if the validation passes.
                foreach (DeviceRow device in deviceRows)
                {
                    bool deviceRejected = false;
                    string descriptionInput = device.Description.Trim();
                    string serialNumberInput = device.SerialNumber.Trim();
                    string groupNamesInput = device.GroupNames.Trim();
                    string assetTypeInput = device.AssetType.Trim();
                    string vinInput = device.Vin.Trim();

                    // check description
                    if (string.IsNullOrEmpty(descriptionInput))
                    {
                        Console.WriteLine($"Rejected. Does not have a name.");
                        deviceRejected = true;
                    }

                    // check group name
                    IList<Group> newDeviceGroups = new List<Group>();

                    // In the .csv file if a device belongs to multiple nodes we separate with a pipe character.
                    string[] groupNames = (groupNamesInput ?? "").Split('|');
                    if (groupNames.Length == 0 || groupNamesInput == "")
                    {
                        Console.WriteLine($"Rejected: '{descriptionInput}'. Does not have any groups.");
                        deviceRejected = true;
                    }
                    else
                    {
                        // Iterate through the group names and try to assign each group to the device looking it up from the allNodes collection.
                        foreach (string groupName in groupNames)
                        {

                            // Adding GroupCompanyId is unnecessary
                            if (groupName == "Company Group" || groupName == "Company")
                            {
                                continue;
                            }

                            // Verify that the groups specified in the CSV file exist in the database and that the current user has access to them.
                            Group group = GetGroup(groupName.Trim(), nonAssetTypes);
                            if (group == null)
                            {
                                Console.WriteLine($"Warning: '{descriptionInput}'. Group: '{groupName}' does not exist.");
                            }
                            else
                            {
                                // Add group to device nodes collection.
                                newDeviceGroups.Add(group);
                            }
                        }
                        // 
                        if (newDeviceGroups.Count == 0 && !(groupNames.Contains("Company Group") || groupNames.Contains("Company")))
                        {
                            Console.WriteLine($"Rejected: '{descriptionInput}'. Has no valid groups.");
                            deviceRejected = true;
                        }
                        // check asset type is given, if not default to Vehicle, else use the given asset type
                        if (string.IsNullOrEmpty(assetTypeInput))
                        {
                            Console.WriteLine($"'{descriptionInput} - No Asset Type provided. Defaulting to 'Vehicle' Asset Type.");
                            newDeviceGroups.Add(new VehicleGroup());
                        }
                        else
                        {
                            Group group = GetGroup(assetTypeInput.Trim(), assetTypes);
                            if (group == null)
                            {
                                Console.WriteLine($"'{descriptionInput} - Could not find Asset Type '{assetTypeInput}'. Defaulting to 'Vehicle' Asset Type.");
                                newDeviceGroups.Add(new VehicleGroup());
                            }
                            else
                            {
                                newDeviceGroups.Add(group);
                            }
                        }
                    }
                    // If the device is rejected, move on the the next device row.
                    if (deviceRejected)
                    {
                        continue;
                    }
                    // Create a new device object (GoDevice, CustomVehicleDevice, or UntrackedAsset), then add.
                    try
                    {

                        // Create the device object.
                        if (!string.IsNullOrEmpty(serialNumberInput))
                        {
                            if (serialNumberInput.StartsWith("G"))
                            {
                                // Use GoDevice instead of Device because GoDevice includes the VehicleIdentificationNumber property, which is not present in the Device.
                                var newDevice = (GoDevice)Device.FromSerialNumber(serialNumberInput);
                                newDevice.PopulateDefaults();
                                newDevice.Comment = "Imported from dotnet sample: ImportDevices";
                                newDevice.Name = descriptionInput;
                                newDevice.Groups = (newDeviceGroups.Count > 0) ? newDeviceGroups : null;
                                newDevice.VehicleIdentificationNumber = vinInput ?? null;
                                await api.CallAsync<Id>("Add", typeof(Device), new { entity = newDevice });
                            }
                            else
                            {
                                // Use CustomVehicleDevice instead of CustomDevice because CustomVehicleDevice includes the VehicleIdentificationNumber property, which is not present in the CustomDevice.
                                var newDevice = (CustomVehicleDevice)Device.FromSerialNumber(device.SerialNumber);
                                newDevice.PopulateDefaults();
                                newDevice.Comment = "Imported from dotnet sample: ImportDevices";
                                newDevice.Name = descriptionInput;
                                newDevice.Groups = (newDeviceGroups.Count > 0) ? newDeviceGroups : null;
                                newDevice.VehicleIdentificationNumber = vinInput ?? null;
                                await api.CallAsync<Id>("Add", typeof(Device), new { entity = newDevice });
                            }
                        }
                        else
                        {
                            // Use UntrackedAsset instead of Device because UntrackedAsset includes the VehicleIdentificationNumber property, which is not present in the Device.
                            var newDevice = new UntrackedAsset
                            {
                                Name = descriptionInput,
                                Groups = (newDeviceGroups.Count > 0) ? newDeviceGroups : null,
                                VehicleIdentificationNumber = vinInput ?? null
                            };
                            newDevice.PopulateDefaults();
                            newDevice.Comment = "Imported from dotnet sample: ImportDevices";
                            await api.CallAsync<Id>("Add", typeof(Device), new { entity = newDevice });
                        }

                        Console.WriteLine($"Added: '{descriptionInput}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: '{descriptionInput}'. {ex.Message}");
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
        /// Searches for and returns a group from a flat list of groups.
        /// </summary>
        /// <param name="id">The group id to search for.</param>
        /// <param name="groups">The group collection to search in.</param>
        /// <returns>The found group or null if not found.</returns>
        static Group GetGroup(string name, IList<Group> groups)
        {
            return groups.FirstOrDefault(group => group.Name.ToString() == name);
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
                        device.SerialNumber = columns.Length > 1 ? columns[1] : "";
                        device.GroupNames = columns.Length > 2 ? columns[2] : "";
                        device.AssetType = columns.Length > 3 ? columns[3] : "";
                        device.Vin = columns.Length > 4 ? columns[4] : "";

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
        /// Recursively retrieves all groups under a specified group.
        /// </summary>
        /// <param name="groupName">The group id to search for.</param>
        /// <param name="groupList">The group collection to search in.</param>
        /// <returns>A list of all child groups found, including the starting group. Returns null if the starting group is not found.</returns>
        static List<Group> GetGroupDescendants(string groupName, IList<Group> groupList)
        {
            foreach (Group group in groupList)
            {
                if (group.Id.ToString() == groupName)
                {
                    List<Group> list = new List<Group>();
                    list.Add(group);
                    foreach (Group child in group.Children)
                    {
                        list.AddRange(GetGroupDescendants(child.Id.ToString(), groupList));
                    }
                    return list;
                }
            }
            return null;
        }
    }
}
