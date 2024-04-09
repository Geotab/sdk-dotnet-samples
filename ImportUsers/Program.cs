using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;

namespace Geotab.SDK.ImportUsers
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Searches a list of organization groups for matches based on name.
        /// </summary>
        /// <param name="names">The name of the group to search for. This is from the CSV - group node.</param>
        /// <param name="organizationGroups">The group collection to search.</param>
        /// <returns>A list of organization groups.</returns>
        static IList<Group> GetOrganizationGroups(IEnumerable<string> names, IList<Group> organizationGroups)
        {
            IList<Group> groups = [];

            foreach (string groupName in names)
            {
                string name = groupName.Trim().ToLowerInvariant();
                if (name.Equals("organization") || name.Equals("entire organization") || name.Equals("company"))
                {
                    name = "Company Group";
                }
                Group group = organizationGroups.FirstOrDefault(aGroup => aGroup.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (group != null)
                {
                    groups.Add(group);
                }
                else
                {
                    Console.WriteLine($"** WARNING: The group '{groupName}' could not be located. The user will be added without association to this group. Please verify the group name or create it if necessary.");
                }
            }

            return groups;
        }

        /// <summary>
        /// Searches a list of security groups for matches based on name.
        /// </summary>
        /// <param name="name">The security group name.</param>
        /// <param name="securityGroups">The groups collection to search. This is from the CSV Security Clearance</param>
        /// <returns>A list of security groups.</returns>
        static IList<Group> GetSecurityGroups(string name, IList<Group> securityGroups)
        {
            IList<Group> groups = [];
            name = name.Trim().ToLowerInvariant();
            if (name.Equals("administrator") || name.Equals("admin"))
            {
                name = "**EverythingSecurity**";
            }
            else if (name.Equals("superviser") || name.Equals("supervisor"))
            {
                name = "**SupervisorSecurity**";
            }
            else if (name.Equals("view only") || name.Equals("viewonly"))
            {
                name = "**ViewOnlySecurity**";
            }
            else if (name.Equals("nothing"))
            {
                name = "**NothingSecurity**";
            }
            foreach (Group aSecurityGroup in securityGroups)
            {
                if (aSecurityGroup.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    groups.Add(aSecurityGroup);
                    break;
                }
            }
            return groups;
        }

        /// <summary>
        /// Loads rows from a CSV file into a collection of UserDetails objects.
        /// </summary>
        /// <param name="filename">The filename of the CSV file.</param>
        /// <returns>A collection of UserDetails objects.</returns>
        static IList<UserDetails> LoadUsersFromCSV(string filename)
        {
            List<UserDetails> userDetails = [];
            int count = 0;

            using (StreamReader streamReader = new(filename))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {
                        count++;

                        // Consider lines starting with # to be comments or empty lines
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        // Create UserDetails from line columns
                        string[] columns = line.Split(',');

                        string userName = columns[0].Trim();
                        string password = columns[1].Trim();
                        string groups = columns[2].Trim();
                        string securityClearance = columns[3].Trim();
                        string firstName = columns[4].Trim();
                        string lastName = columns[5].Trim();

                        DateTime maxValue = DateTime.MaxValue;
                        DateTime minValue = DateTime.MinValue;
                        var user = User.CreateBasicUser(null, null, userName, firstName, lastName, password, null, null, null, minValue, maxValue, null, null, null, null);
                        userDetails.Add(new UserDetails(user, password, groups, securityClearance, firstName, lastName));
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Invalid row: {count} {exception.Message}");
                    }
                }
            }
            return userDetails;
        }

        /// <summary>
        /// This is a console example of importing users from a .csv file.
        /// 1) Process command line arguments: Server, Database, Username, Password, Options and Load .csv file.
        /// Note: the .csv file in this project is a sample, you may need to change entries (such as group names) for the example to work.
        /// 2) Create users.
        /// 3) Add organization and security nodes to the users.
        /// 4) Create Geotab API object and Authenticate.
        /// 5) Import users into database.
        /// A complete Geotab API object and method reference is available at the Geotab Developer page.
        /// </summary>
        /// <param name="args">The command line arguments for the application. Note: When debugging these can be added by: Right click the project &gt; Properties &gt; Debug Tab &gt; Start Options: Command line arguments.</param>
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length != 5)
                {
                    Console.WriteLine();
                    Console.WriteLine("Command line parameters:");
                    Console.WriteLine("dotnet run <server> <database> <username> <password> <inputFile>");
                    Console.WriteLine();
                    Console.WriteLine("Command line:        dotnet run server database username password inputFile");
                    Console.WriteLine("server             - The server name (Example: my.geotab.com)");
                    Console.WriteLine("database           - The database name (Example: G560)");
                    Console.WriteLine("username           - The Geotab user name");
                    Console.WriteLine("password           - The Geotab password");
                    Console.WriteLine("inputFile          - File name of the CSV file to import.");
                    Console.WriteLine();
                    return;
                }

                // Process command line arguments
                string server = args[0];
                string database = args[1];
                string username = args[2];
                string password = args[3];
                string filename = args[4];

                // Load user info from .csv file into UserDetails collection.
                Console.WriteLine("Loading CSV file...");
                IList<UserDetails> userDetails;
                try
                {
                    userDetails = LoadUsersFromCSV(filename);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Could not load CSV file: {exception.Message}");
                    return;
                }

                // Create Geotab API object and authenticate
                Console.WriteLine("Authenticating...");
                API api = new(username, password, null, database, server);
                await api.AuthenticateAsync();

                // Retrieve existing users and groups from the database
                IList<User> existingUsers = await api.CallAsync<List<User>>("Get", typeof(User)) ?? [];
                IList<Group> allGroups = await api.CallAsync<List<Group>>("Get", typeof(Group)) ?? [];
                IList<Group> securityGroups = await api.CallAsync<List<Group>>("Get", typeof(Group), new { search = new GroupSearch(new SecurityGroup().Id) }) ?? [];

                // Import users
                Console.WriteLine("Importing users...");
                foreach (UserDetails userDetail in userDetails)
                {
                    User user = userDetail.UserNode;
                    user.CompanyGroups = GetOrganizationGroups(userDetail.GroupsNode.Split('|'), allGroups);
                    user.SecurityGroups = GetSecurityGroups(userDetail.SecurityClearanceNode, securityGroups);

                    if (ValidateUser(user, existingUsers))
                    {
                        try
                        {
                            // Add the user
                            await api.CallAsync<Id>("Add", typeof(User), new { entity = user });
                            Console.WriteLine($"User: '{user.Name}' added");
                            existingUsers.Add(user);
                        }
                        catch (Exception exception)
                        {
                            // Catch and display any errors that occur when adding the user
                            Console.WriteLine($"Error adding user: '{user.Name}'\n{exception.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Show miscellaneous exceptions
                Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Checks if a user exists by searching for the user name in a collection of existing users.
        /// </summary>
        /// <param name="name">The user name to search for.</param>
        /// <param name="existingUsers">The collection of existing users.</param>
        /// <returns>True if a duplicate is found, otherwise false.</returns>
        static bool UserExists(string name, IList<User> existingUsers)
        {
            foreach (User user in existingUsers)
            {
                if (user.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Validate that a user has groups assigned and does not exist.
        /// </summary>
        /// <param name="user">The user to validate.</param>
        /// <param name="existingUsers">The list of existing users.</param>
        /// <returns>True if the user is valid, otherwise False.</returns>
        static bool ValidateUser(User user, IList<User> existingUsers)
        {
            if (user.CompanyGroups == null || user.CompanyGroups.Count == 0)
            {
                Console.WriteLine($"Invalid user: {user.Name}. Must have organization nodes.");
                return false;
            }
            if (user.SecurityGroups == null || user.SecurityGroups.Count == 0)
            {
                Console.WriteLine($"Invalid user: {user.Name}. Must have security nodes.");
                return false;
            }
            if (UserExists(user.Name, existingUsers))
            {
                Console.WriteLine($"Invalid user: {user.Name}. Duplicate User.");
                return false;
            }
            return true;
        }
    }
}
