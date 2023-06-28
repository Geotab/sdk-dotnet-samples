using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.AssetGroups;

namespace Geotab.SDK.ImportGroups
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Loads data from .csv file and creates a collection of <see cref="GroupRow"/> objects.
        /// </summary>
        /// <param name="fileName">The name of the file to load.</param>
        /// <returns>A collection of <see cref="GroupRow"/> objects.</returns>
        static async Task<List<GroupRow>> LoadGroupRowsFromCSV(string fileName)
        {
            List<GroupRow> groups = new List<GroupRow>();
            int count = 0;

            // Read file
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                // Create a GroupRow for each line of the file
                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {
                    // Consider lines starting with # to be comments
                    if (line.StartsWith("#", StringComparison.Ordinal))
                    {
                        count++;
                        continue;
                    }

                    try
                    {
                        // Create GroupRow from line columns
                        GroupRow groupRow = new GroupRow();
                        string[] columns = line.Split(',');
                        groupRow.ParentGroupName = columns[0];
                        groupRow.GroupName = columns[1];
                        groups.Add(groupRow);
                        count++;
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Invalid row: {count}\n{exception.Message}");
                    }
                }
            }
            return groups;
        }
        
        
        private static String timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        private static String fileNameLogs = $"logs_{timestamp}.csv";
        private static String filePathLogs = Path.Combine(Directory.GetCurrentDirectory(), fileNameLogs);
        private static StreamWriter writer = new StreamWriter(filePathLogs, true) { AutoFlush = true };
        /// <summary>
        /// This is a Geotab API console example of importing groups from a CSV file.
        /// 1) Process command line arguments: Server, Database, Username, Password, Input File and load CSV file.
        /// Note: the CSV file in this project is a sample, you may need to change entries (such as group names) for the example to work.
        /// 2) Create Geotab API object and Authenticate.
        /// 3) Import groups into database.
        /// A complete Geotab API object and method reference is available at the Geotab Developer page.
        /// </summary>
        /// <param name="args">The command line arguments for the application. Note: When debugging these can be added by: Right click the project &gt; Properties &gt; Debug Tab &gt; Start Options: Command line arguments.</param>
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length != 5)
                {
                    // ImportNodes
                    Console.WriteLine();
                    Console.WriteLine("Command line parameters:");
                    Console.WriteLine("dotnet run <server> <database> <username> <password> <inputfile>");
                    Console.WriteLine();
                    Console.WriteLine("Command line:     dotnet run server database login password inputfile");
                    Console.WriteLine("server          - The server name (Example: my.geotab.com)");
                    Console.WriteLine("database        - The database name (Example: G560)");
                    Console.WriteLine("username        - MyGeotab user name (Example: username@geotab.com)");
                    Console.WriteLine("password        - MyGeotab password");
                    Console.WriteLine("inputfile       - File name of the CSV file to import.");
                    Console.WriteLine();
                    return;
                }

                // Process command line arguments
                string server = args[0];
                string database = args[1];
                string username = args[2];
                string password = args[3];
                string fileName = args[4];

                // Load GroupRow collection from the given .csv file
                Console.WriteLine("Loading .csv file...");

                List<GroupRow> groupRows;
                try
                {
                    groupRows = await LoadGroupRowsFromCSV(fileName);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Could not load CSV file: {exception.Message}");
                    return;
                }

                // Creates a log file
                // string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                // string fileNameLogs = $"logs_{timestamp}.csv";
                // string filePathLogs = Path.Combine(Directory.GetCurrentDirectory(), fileNameLogs);

                // File.Create(filePathLogs).Close();

                // Create Geotab API object
                API api = new API(username, password, null, database, server);

                // Authenticate
                Console.WriteLine("Authenticating...");
                AddingLog("Authentication successful");
                await api.AuthenticateAsync();

                // Start import
                Console.WriteLine("Importing...");

                // When adding a Group you must include the Group's Parent. There is a base Group structure which is immutable which you can add child nodes to.
                // For example: The system will always have a RootGroup (ObjectModel.RootGroup) with an CompanyGroup (ObjectModel.CompanyGroup) as a child.

                // Load the all the nodes in the system this user has access to into this dictionary.
                Dictionary<string, Group> existingGroupDictionary = await PopulateGroupDictionaryAsync(api);

                // Start adding the new Groups we retrieved from the file.
                foreach (GroupRow row in groupRows)
                {
                    // Assigning the parent node. //

                    // When adding a node it, must have a parent.
                    Group parentGroup;
                    string parentGroupName = row.ParentGroupName;

                    // If there is no parent node name or if the parent node's name matches organization or entire organization create a new CompanyGroup object.
                    if (string.IsNullOrEmpty(parentGroupName) || parentGroupName.ToLowerInvariant() == "organization" || parentGroupName.ToLowerInvariant() == "entire organization")
                    {
                        parentGroup = new CompanyGroup();
                    }
                    else
                    {
                        // This will need re-loading when there is a node we previously added that we now want to assign children to.
                        if (!existingGroupDictionary.ContainsKey(parentGroupName.ToLowerInvariant()))
                        {
                            existingGroupDictionary = await PopulateGroupDictionaryAsync(api);
                        }

                        // Check for non-organization Group in the dictionary of nodes that exist in the system.
                        if (!existingGroupDictionary.TryGetValue(parentGroupName.ToLowerInvariant(), out parentGroup))
                        {
                            Console.WriteLine($"Non-existent parent Group: {parentGroupName}");
                            AddingLog($"Non-existent parent Group: {parentGroupName}");
                            continue;
                        }
                    }

                    // If the parent is null then we cannot add the Group. So we write to the console and try to add the next node.
                    if (parentGroup == null)
                    {
                        Console.WriteLine($"No parent for Group {row.GroupName}");
                        AddingLog($"No parent for Group {row.GroupName}");
                        continue;
                    }

                    // Adding the new node //

                    // If a node exists with this name we wont add it and try to add the next node.
                    if (existingGroupDictionary.ContainsKey(row.GroupName.ToLowerInvariant()))
                    {
                        Console.WriteLine($"A group with the name '{row.GroupName}' already exists, please change this group name.");
                        AddingLog($"A group with the name '{row.GroupName}' already exists, please change this group name.");
                        continue;
                    }

                    try
                    {     
                        // determines if the parentGroup is a build in group
                        if((parentGroup.Id.ToString().Contains("Id"))) 
                        {
                            var groupToAdd = new GroupWithGlobalReporting(null,name:row.GroupName, parent:Group.Get(parentGroup.Id));
                            groupToAdd.IsGlobalReportingGroup = true;
                            await api.CallAsync<Id>("Add", typeof(Group), new { entity = groupToAdd });
                            Console.WriteLine($"Successfully added: {row.GroupName}");
                            AddingLog($"Successfully added: {row.GroupName}");
                        }
                        else 
                        {
                            var groupToAdd = new Group(null,name:row.GroupName, parent:parentGroup);
                            await api.CallAsync<Id>("Add", typeof(Group), new { entity = groupToAdd });
                            Console.WriteLine($"Successfully added: {row.GroupName}");
                            AddingLog($"Successfully added: {row.GroupName}");
                        }
                    }
                    catch (Exception exception)
                    {
                        // Catch exceptions here so we can continue trying to add nodes.
                        Console.WriteLine($"Could not add {row.GroupName}: {exception.Message}");
                        AddingLog($"Could not add {row.GroupName}: {exception.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Show miscellaneous exceptions
                Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                AddingLog($"Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        ///saves a string to the created Log file
        /// </summary>
        public static void AddingLog(string input)
        {
            writer.WriteLine(input);
        }

        /// <summary>
        /// Populates the Group Dictionary
        /// </summary>
        /// <param name="api">The Geotab API object.</param>
        /// <returns>A dictionary populated with the <see cref="Group"/> name (Key) and <see cref="Group"/> (Value)</returns>
        static async Task<Dictionary<string, Group>> PopulateGroupDictionaryAsync(API api)
        {
            var dataStoreGroups = await api.CallAsync<IList<Group>>("Get", typeof(Group));

            var groupDictionary = new Dictionary<string, Group>(dataStoreGroups.Count);
            var nonUniqueGroups = new HashSet<string>();
            for (var i = 0; i < dataStoreGroups.Count; i++)
            {
                var group = dataStoreGroups[i];
                
                if (nonUniqueGroups.Contains(group.Name))
                {
                    continue;
                }
                try
                {
                    groupDictionary.Add(group.Name.ToLowerInvariant(), group);
                }
                catch (ArgumentException)
                {
                    // don't allow ambiguous names
                    groupDictionary.Remove(group.Name);
                    Console.WriteLine($"group name '{group.Name}' is not unique, cannot use it as parent for import.");
                    AddingLog($"group name '{group.Name}' is not unique, cannot use it as parent for import.");
                    nonUniqueGroups.Add(group.Name);
                }
            }
            return groupDictionary;
        }
    }
}
