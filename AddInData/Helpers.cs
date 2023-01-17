using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using System.Linq;
using Geotab.SDK.StorageApi;
class Helpers
{
    public static string addInId = "ajZhMTNmOGQtYjUwYy1mMmE";
    public static string exampleAddInId = "aGY4MWEzZDEtNTVlZi1kM2E";
    public static API InitializeArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        string server = "";
        string database = "";
        string username = "";
        string password = "";
        API api = null;
        try
        {
            server = args[1];
            database = args[2];
            username = args[3];
            password = args[4];
            api = new API(username, password, null, database, server);
            return api;
        }
        catch (System.Exception)
        {
            System.Console.WriteLine(@"Please provide the following arguments: ""<server>"" ""<database>"" ""<username>"" ""<password>""");
        }
        return null;
    }

    public static async Task CaseCreateAddInDataAsync(API api)
    {
        Console.Clear();
        System.Console.WriteLine($"1. Create a new object");
        System.Console.WriteLine("______________________________\n");
        System.Console.WriteLine($"Note: A pre-defined AddInId \"{addInId}\" will be used for all examples in this project\n\n");

        var details = new Dictionary<string, object>();
        StartDetailsEntry(details);

        dynamic expDetails = details.Aggregate(new ExpandoObject() as IDictionary<string, Object>,
         (a, p) => { a.Add(p.Key, p.Value); return a; });

        var addInObjId = await CreateAddInDataAsync(addInId, api, expDetails);
        Console.Clear();
        System.Console.WriteLine($"An AddInData Object has been created.\n");
        System.Console.WriteLine($"AddInId: \"{addInId}\" ");
        var updatedDetails = await GetAddInDataAsync(api, addInId, Convert.ToString(addInObjId));
        var jsonUpdatedDetails = FormatJsonString(updatedDetails[0].Details);
        System.Console.WriteLine($"AddInData Object:");
        System.Console.WriteLine($"{jsonUpdatedDetails}\n");
        System.Console.WriteLine($"Press Enter to continue...");
        Console.ReadLine();
        Console.Clear();
    }

    public static void StartDetailsEntry(Dictionary<string, object> details)
    {
        while (true)
        {
            System.Console.WriteLine($"Please input data in the format    key:value    and press the enter key.");
            System.Console.WriteLine($"Press Esc to finish entering data...");
            var userInput = ReadLineOrEscape()?.Replace(" ", "") ?? "Esc";
            if (userInput == "Esc")
            {
                Console.Clear();
                break;
            }
            var keyValuePair = userInput.Split(":");
            try
            {
                if (details.ContainsKey(keyValuePair[0]))
                {
                    details[keyValuePair[0]] = keyValuePair[1];
                    System.Console.WriteLine($"\n... Updated key value pair {{ {keyValuePair[0]} : {keyValuePair[1]} }}\n");
                }
                else
                {
                    details.Add(keyValuePair[0], keyValuePair[1]);
                    System.Console.WriteLine($"\n... Added key value pair {{ {keyValuePair[0]} : {keyValuePair[1]} }}\n");
                }
            }
            catch
            {
                System.Console.WriteLine($"\nERROR: \"{userInput}\" is not a valid input\n");
            }

        }
    }

    public static async Task<Id> CreateAddInDataAsync(string addInId, API api, object details)
    {
        var addInDataObject_id = await api.CallAsync<Id>("Add", typeof(AddInData), new
        {
            entity = new
            {
                addInId = addInId,
                details = details,
                groups = new[] { new CompanyGroup() }
            }
        });
        return addInDataObject_id;
    }

    public static async Task CaseModifyAddInDataAsync(API api)
    {
        Console.Clear();
        System.Console.WriteLine($"2. Modify an existing object");
        System.Console.WriteLine("______________________________\n");
        System.Console.WriteLine($"Note: A default AddInId \"{addInId}\" is being used for this example ");
        System.Console.WriteLine($"Note: The entityId of the AddInData object needs to be selected to modify the corresponding AddInData object\n");
        var details = new Dictionary<string, object>();
        var entId_OldDetails = await GetExistingAddInDataObjsAsync(api);
        if (entId_OldDetails.Item1 != null)
        {
            var jsonOldDetails = FormatJsonString(entId_OldDetails.Item2);

            StartDetailsEntry(details);

            var modifyResult = await ModifyAddInDataAsync(addInId, api, entId_OldDetails.Item1, details);
            var updatedDetails = await GetAddInDataAsync(api, addInId, entId_OldDetails.Item1);
            var jsonUpdatedDetails = FormatJsonString(updatedDetails[0].Details);
            System.Console.WriteLine($"The AddInData Object (Id:\"{entId_OldDetails.Item1}\") has been modified\n");
            System.Console.WriteLine($"Details before modification:");
            System.Console.WriteLine($"{jsonOldDetails}");
            System.Console.WriteLine();
            System.Console.WriteLine($"Details after modification:");
            System.Console.WriteLine($"{jsonUpdatedDetails}");
            System.Console.WriteLine();
            System.Console.WriteLine($"Press Enter to continue...");
            Console.ReadLine();
            Console.Clear();
        }
        else
        {
            Console.Clear();
        }
    }

    public static async Task<Id> ModifyAddInDataAsync(string addInId, API api, string recordId, object details)
    {
        var modifyResult = await api.CallAsync<Id>("Set", typeof(AddInData), new
        {
            entity = new
            {
                addInId = addInId,
                id = recordId,
                details = details
            }
        });
        return modifyResult;
    }

    public static async Task<(string?, string?)> GetExistingAddInDataObjsAsync(API api)
    {

        var addInDataObjects = await GetAddInDataAsync(api, addInId);
        if (addInDataObjects.Count > 0)
        {
            bool isAcceptingInput = true;
            while (isAcceptingInput)
            {
                System.Console.WriteLine($"AddInData Object Options:");
                var addInDict = new Dictionary<int, Id>();
                int i = 1;
                addInDataObjects?.ForEach(item =>
                {
                    addInDict.Add(i, item.Id);
                    System.Console.WriteLine($"\t{i}. -> {item.Details}");
                    i++;
                });
                System.Console.WriteLine($"\nSelect the number corresponding to the AddInData object displayed:");
                var choice = Console.ReadLine().Replace(".", "").Replace("\"", "");
                int variable = -1;
                int.TryParse(choice, out variable);
                if (variable == -1 || variable < 1 || variable > i - 1)
                {
                    System.Console.WriteLine($"ERROR: \"{choice}\" is not a valid option\n");
                }
                else
                {
                    isAcceptingInput = false;
                    var entityId = addInDict[Convert.ToInt32(choice)].ToString();
                    var selectedObj = from addInDataObj in addInDataObjects
                                      where addInDataObj.Id.ToString().Contains(entityId)
                                      select addInDataObj;
                    var existingDetails = selectedObj.First().Details;
                    Console.Clear();
                    System.Console.WriteLine($"\nDetails of the current object: {existingDetails}\n");
                    return (entityId, existingDetails);
                }
            }
            return (null, null);
        }
        else
        {
            System.Console.WriteLine($"There are no AddInData objects for the AddInId \"{addInId}\"");
            System.Console.WriteLine($"Please create an AddInData object first\n");
            System.Console.WriteLine($"Press Enter to continue...");
            Console.ReadLine();
            return (null, null);
        }
    }

    public static async Task<List<AddInData>> GetAddInDataAsync(API api, string _addInId, string? entityId = null, string? selectClause = null, string? whereClause = null)
    {
        var addInSearch = new Dictionary<string, Object>();
        addInSearch.Add("addInId", _addInId);
        if (entityId != null) addInSearch.Add("id", entityId);
        if (selectClause != null) addInSearch.Add("selectClause", selectClause);
        if (whereClause != null) addInSearch.Add("whereClause", whereClause);

        dynamic expAddInSearch = addInSearch.Aggregate(new ExpandoObject() as IDictionary<string, Object>,
         (a, p) => { a.Add(p.Key, p.Value); return a; });

        var addinDataObjects = await api.CallAsync<List<AddInData>>("Get", typeof(AddInData), new
        {
            search = expAddInSearch
        });

        return addinDataObjects;
    }

    public static async Task CaseRetrieveAddInExampleAsync(API api)
    {
        Console.Clear();
        System.Console.WriteLine($"4. Display Retrieve AddInData example with select and where clauses");
        System.Console.WriteLine("______________________________\n");
        var exampleAddInDataObj = await GetAddInDataAsync(api, exampleAddInId);
        var detailsJson = "";
        if (exampleAddInDataObj.Count == 0)
        {

            var details = new
            {
                items = new[]
                {
                    new
                    {
                        name="bottles",
                        price=12
                    },
                    new
                    {
                        name="caps",
                        price=20
                    },
                },
                customer = new
                {
                    name = "joesmith",
                    email = "joe@smith.com"
                }
            };
            await CreateAddInDataAsync(exampleAddInId, api, details);
            var exampleObj = await GetAddInDataAsync(api, exampleAddInId);
            detailsJson = FormatJsonString(exampleObj[0].Details);
        }
        else
        {
            detailsJson = FormatJsonString(exampleAddInDataObj[0].Details);
        }

        System.Console.WriteLine($"Example AddInData contents:");
        System.Console.WriteLine($"{detailsJson}\n");
        System.Console.WriteLine($"Filtered result after adding the below select and where clauses\n");
        System.Console.WriteLine($"\t\"selectClause\": \"customer.email\",");
        System.Console.WriteLine($"\t\"whereClause\": \"items.[].price < 15\"");

        var filteredAddInDataObj = await GetAddInDataAsync(api, exampleAddInId, null, "customer.email", "items.[].price < 15");
        System.Console.WriteLine(FormatJsonString("\nResult:"));
        System.Console.WriteLine(FormatJsonString($"\t{filteredAddInDataObj[0].Details}"));
        System.Console.WriteLine($"\nPress Enter to continue...");
        Console.ReadLine();
        Console.Clear();
    }

    public static async Task CaseRemoveAddInDataAsync(API api)
    {
        Console.Clear();
        System.Console.WriteLine($"3. Remove an existing object");
        System.Console.WriteLine("______________________________\n");
        var entityId = await GetExistingAddInDataObjsAsync(api);
        if (entityId.Item1 != null)
        {
            Console.Clear();
            await RemoveAddInDataAsync(api, entityId.Item1);
            System.Console.WriteLine($"The AddInData object with id {entityId.Item1} has been removed");
            System.Console.WriteLine($"\nPress Enter to continue...");
            Console.ReadLine();
            Console.Clear();
        }
        else
        {
            Console.Clear();
        }
    }

    public static async Task<object> RemoveAddInDataAsync(API api, string entityId)
    {
        var removeResult = await api.CallAsync<Id>("Remove", typeof(AddInData), new
        {
            entity = new
            {
                Id = Id.Create(entityId)
            }
        });
        return removeResult;
    }

    public static string ReadLineOrEscape()
    {
        ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
        StringBuilder stringInput = new StringBuilder();
        int index = 0;
        while (keyInfo.Key != ConsoleKey.Enter)
        {
            keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Escape) return null;
            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (index > 0)
                {
                    Console.CursorLeft = index - 1;
                    stringInput.Remove(index - 1, 1);
                    Console.Write(" \b");
                    index--;
                }
            }
            if (keyInfo.KeyChar > 31 && keyInfo.KeyChar < 127)
            {
                index++;
                Console.Write(keyInfo.KeyChar);
                stringInput.Append(keyInfo.KeyChar);
            }
        }
        return stringInput.ToString();
    }

    public static string FormatJsonString(string json)
    {
        try
        {
            var parsedJson = JsonSerializer.Deserialize<ExpandoObject>(json);
            var options = new JsonSerializerOptions() { WriteIndented = true };
            return JsonSerializer.Serialize(parsedJson, options);
        }
        catch (System.Exception)
        {
            return json;
        }
    }
}
