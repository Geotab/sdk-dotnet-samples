using Geotab.Checkmate.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel.Exceptions;
using Geotab.SDK.ImportGroupsR;
using Xunit;
using Geotab.Drawing;
using Xunit.Abstractions;
using System.Threading.Tasks;

namespace ImportGroupsR.Test
{
    public class GroupImportTest : ApiTest
    {
        static readonly string LocalTimeZoneId = Geotab.Checkmate.ObjectModel.TimeZoneInfo.MachineToOlson(System.TimeZoneInfo.Local.Id);

        enum GroupColor { Blue = 255, Green = 32768, Purple = 8388736, Cyan = 11393254, Red = 16729344, Orange = 16753920, Yellow = 16776960 }

        const string FirstLineParentName = "First Line Parent Name";
        const string FirstLineParentSreference = "First Line Parent Sreference";

        bool isVerboseMode = true;
        TestValueHelper testValueHelper;

        Dictionary<string, Group> lookupSreferenceToGroupFromDbForVerification;
        Dictionary<Id, Group> lookupIdToGroupFromDbForVerification;
        IDictionary<string, IList<Group>> groupsInDBWithNonUniqueReferenceLookup; // Key -sReference
        IDictionary<Id, IList<Group>> groupsInDBWithNonUniqueIdLookup;
        int numberOfLinesParsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupImportTest" /> class.
        /// </summary>
        public GroupImportTest(ITestOutputHelper log)
             : base(log)
        {
            testValueHelper = new TestValueHelper();
        }

        /// <summary>
        /// Dummies the test.
        /// </summary>
        [Fact]
        public async Task ImportThreeGroups_Test()
        {
            await SetupAsync();
            await InitializeDatabaseWithThreeGroupsAsync(api);
            await Verify_ImportThreeGroupsAsync(api);
        }

        /// <summary>
        /// ParseFileWithThreeGroups_GroupBreadthFirstIterator_Test
        /// </summary>
        [Fact]
        public async Task ParseFileWithThreeGroups_GroupBreadthFirstIterator_Test()
        {
            await SetupAsync();
            var parser = await InitializeParserWithThreeGroupsAsync(api);
            var groupIterator = new GroupBreadthFirstIterator(parser.FirstLineParentGroupParsed);
            var iterationCount = 0;
            foreach (var group in groupIterator)
            {
                if (iterationCount == 0)
                {
                    Assert.Equal(group.Name, FirstLineParentName);
                    Assert.Equal(group.Reference, FirstLineParentSreference);
                }
                else
                {
                    Assert.Equal(group.Name, GenerateName(iterationCount, 0));
                    Assert.Equal(group.Reference, GenerateSReference(iterationCount, 0));
                }
                iterationCount++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportDatabaseInitializationFile_Test()
        {
            await SetupAsync();
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization);
            await Verify_DatabaseInitializationAsync(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_Added_Test()
        {
            await SetupAsync();
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization);
            await SimulateParsingAndImportFromFileAsync(api, CreateInputFileForAdded, Parser_RowParsed, Importer_GroupImported);
            await Verify_AddedAsync(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_Updated_Test()
        {
            await SetupAsync();
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization);
            var importUpdated = new ImportUpdated(this);
            await SimulateParsingAndImportFromFileAsync(api, importUpdated.CreateInputFileForUpdated, Parser_RowParsed, Importer_GroupImported);
            await importUpdated.Verify_UpdatedAsync(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_Deleted_Test()
        {
            await SetupAsync();
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization);
            var importDeleted = new ImportDeleted(this);
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_Deleted(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithZones_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateZonesForDeletedAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithDevices_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateDevicesForDeletedAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithUsers_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateUsersForDeletedAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithDrivers_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateDriversForDeletedAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithRules_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateRulesForDeletedAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// Imports the groups with custom report schedule emails test.
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithCustomReportScheduleNormalReports_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateCustomReportScheduleForDeleted_NormalReportAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// Imports the groups with custom report schedule emails test.
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithCustomReportScheduleDashboards_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateCustomReportScheduleForDeleted_DashboardAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// Imports the groups with custom report schedule emails test.
        /// </summary>
        [Fact]
        public async Task ImportGroups_WithCustomReportScheduleEmails_Deleted_Test()
        {
            await SetupAsync();
            var importDeleted = new ImportDeleted(this);
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization, new List<Func<API, Task>> { importDeleted.CreateCustomReportScheduleForDeleted_EmailPdfAsync });
            await SimulateParsingAndImportFromFileAsync(api, importDeleted.CreateInputFileForDeleted, Parser_RowParsed, Importer_GroupImported, true);
            await importDeleted.Verify_WithOneAssociatedAssetClass_Deleted(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_MovedExisting_Test()
        {
            await SetupAsync();
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization);
            var importUpdated = new ImportMovedUpdated(this, null, null, 0);
            await SimulateParsingAndImportFromFileAsync(api, importUpdated.CreateInputFileForMovedUpdated, Parser_RowParsed, Importer_GroupImported);
            await importUpdated.Verify_MovedUpdated(api);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task ImportGroups_MovedUpdated_Test()
        {
            await SetupAsync();
            await InitializeDatabaseAsync(api, CreateInputFileForDatabaseInitialization);
            var importUpdated = new ImportMovedUpdated(this);
            await SimulateParsingAndImportFromFileAsync(api, importUpdated.CreateInputFileForMovedUpdated, Parser_RowParsed, Importer_GroupImported);
            await importUpdated.Verify_MovedUpdated(api);
        }

        async Task Verify_ImportThreeGroupsAsync(API api)
        {
            (lookupSreferenceToGroupFromDbForVerification, lookupIdToGroupFromDbForVerification, groupsInDBWithNonUniqueReferenceLookup, groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
            Assert.Equal(numberOfLinesParsed + 1, lookupSreferenceToGroupFromDbForVerification.Count);

            for (int i = 1; i < 3; i++)
            {
                var readBackGroup = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(i, 0)];
                Assert.Equal(GenerateName(i, 0), readBackGroup.Name);
                Assert.Equal(GenerateDescription(i, 0), readBackGroup.Comments);
                var expectedColor = GenerateColor(i, 0);
                Assert.Equal(expectedColor, readBackGroup.Color);
            }
        }

        async Task Verify_DatabaseInitializationAsync(API api)
        {
            (lookupSreferenceToGroupFromDbForVerification, lookupIdToGroupFromDbForVerification, groupsInDBWithNonUniqueReferenceLookup, groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
            Assert.Equal(numberOfLinesParsed + 1, lookupSreferenceToGroupFromDbForVerification.Count);
            for (int i = 1; i <= 3; i++)
            {
                switch (i)
                {
                    case 1:
                        Verify_DatabaseInitializationLevel1();
                        break;
                    case 2:
                        Verify_DatabaseInitializationLevel2();
                        break;
                    case 3:
                        Verify_DatabaseInitializationLevel3();
                        break;
                }
            }
        }

        void Verify_Group(int level, int groupInLevel, Group parent = null, string postfixName = null, string postfixDescription = null, int colorIncrement = 0)
        {
            var readBackGroup = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(level, groupInLevel)];
            Assert.Equal(GenerateName(level, groupInLevel, postfixName), readBackGroup.Name);
            Assert.Equal(GenerateDescription(level, groupInLevel, postfixDescription), readBackGroup.Comments);
            var expectedColor = GenerateColor(level, groupInLevel, colorIncrement);
            Assert.Equal(expectedColor, readBackGroup.Color);
            if (parent != null)
            {
                Assert.True(parent.Children.Any(group => group.Id == readBackGroup.Id), $"{nameof(Verify_Group)}: Group with sReference {parent.Reference} has no child with sReference {readBackGroup.Reference}");
            }
        }

        void Verify_DatabaseInitializationLevel1()
        {
            var readBackParent = lookupSreferenceToGroupFromDbForVerification[FirstLineParentSreference];
            for (int j = 1; j <= 3; j++)
            {
                Verify_Group(1, j, readBackParent);
            }
        }

        void Verify_DatabaseInitializationLevel2()
        {
            var readBackParent = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 1)];
            for (int j = 1; j <= 3; j++)
            {
                Verify_Group(2, j, readBackParent);
            }
            readBackParent = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 2)];
            Verify_Group(2, 4, readBackParent);
        }

        void Verify_DatabaseInitializationLevel3()
        {
            var readBackParent = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(2, 4)];
            for (int j = 1; j <= 2; j++)
            {
                Verify_Group(3, j, readBackParent);
            }
        }

        async Task Verify_AddedAsync(API api)
        {
            (lookupSreferenceToGroupFromDbForVerification, lookupIdToGroupFromDbForVerification, groupsInDBWithNonUniqueReferenceLookup, groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
            Assert.Equal(numberOfLinesParsed + 1, lookupSreferenceToGroupFromDbForVerification.Count);
            for (int i = 1; i <= 3; i++)
            {
                switch (i)
                {
                    case 1:
                        Verify_AddedLevel1();
                        break;
                    case 2:
                        Verify_AddedLevel2();
                        break;
                    case 3:
                        Verify_AddedLevel3();
                        break;
                }
            }
        }

        void Verify_AddedLevel1()
        {
            var readBackParent = lookupSreferenceToGroupFromDbForVerification[FirstLineParentSreference];
            for (int j = 1; j <= 4; j++)
            {
                Verify_Group(1, j, readBackParent);
            }
        }

        void Verify_AddedLevel2()
        {
            var readBackParent = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 1)];
            for (int j = 1; j <= 3; j++)
            {
                Verify_Group(2, j, readBackParent);
            }
            Verify_Group(2, 5, readBackParent);
            readBackParent = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 2)];
            Verify_Group(2, 4, readBackParent);
        }

        void Verify_AddedLevel3()
        {
            var readBackParent = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(2, 1)];
            Verify_Group(3, 3);
            readBackParent = lookupSreferenceToGroupFromDbForVerification[GenerateSReference(2, 4)];
            for (int j = 1; j <= 2; j++)
            {
                Verify_Group(3, j, readBackParent);
            }
        }

        class ImportUpdated
        {
            readonly GroupImportTest testClass;
            const string UpdatePostfixName = "UN";
            const string UpdatePostfixDescription = "UD";
            const int UpdateColorIncrement = 2;

            public ImportUpdated(GroupImportTest testClass)
            {
                this.testClass = testClass;
            }

            public int CreateInputFileForUpdated(TextWriter textWriter)
            {
                var inputItems = new List<GroupForTest> {

                // level 1
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 1), GenerateSReference(1, 1), GenerateColorString(1, 1), GenerateDescription(1, 1)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 2, UpdatePostfixName), GenerateSReference(1, 2), GenerateColorString(1, 2, UpdateColorIncrement), GenerateDescription(1, 2, UpdatePostfixDescription)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 3, UpdatePostfixName), GenerateSReference(1, 3), GenerateColorString(1, 3), GenerateDescription(1, 3)),

                // level 2
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 1), GenerateSReference(2, 1), GenerateColorString(2, 1), GenerateDescription(2, 1, UpdatePostfixDescription)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 2), GenerateSReference(2, 2), GenerateColorString(2, 2), GenerateDescription(2, 2)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 3), GenerateSReference(2, 3), GenerateColorString(2, 3), GenerateDescription(2, 3)),
                new GroupForTest(GenerateName(1, 2), GenerateSReference(1, 2), GenerateName(2, 4), GenerateSReference(2, 4), GenerateColorString(2, 4), GenerateDescription(2, 4)),

                // level 3
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 1), GenerateSReference(3, 1), GenerateColorString(3, 1, UpdateColorIncrement), GenerateDescription(3, 1)),
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 2), GenerateSReference(3, 2), GenerateColorString(3, 2), GenerateDescription(3, 2)),
                };
                return CreateInputFile(inputItems, textWriter);
            }

            public async Task Verify_UpdatedAsync(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                Assert.Equal(testClass.numberOfLinesParsed + 1, testClass.lookupSreferenceToGroupFromDbForVerification.Count);
                for (int i = 1; i <= 3; i++)
                {
                    switch (i)
                    {
                        case 1:
                            Verify_UpdatedLevel1();
                            break;
                        case 2:
                            Verify_UpdatedLevel2();
                            break;
                        case 3:
                            Verify_UpdatedLevel3();
                            break;
                    }
                }
            }

            void Verify_UpdatedLevel1()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[FirstLineParentSreference];
                testClass.Verify_Group(1, 1, readBackParent);
                testClass.Verify_Group(1, 2, readBackParent, UpdatePostfixName, UpdatePostfixDescription, UpdateColorIncrement);
                testClass.Verify_Group(1, 3, readBackParent, UpdatePostfixName);
            }

            void Verify_UpdatedLevel2()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 1)];
                testClass.Verify_Group(2, 1, readBackParent, null, UpdatePostfixDescription);
                for (int j = 2; j <= 3; j++)
                {
                    testClass.Verify_Group(2, j, readBackParent);
                }
                readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 2)];
                testClass.Verify_Group(2, 4, readBackParent);
            }

            void Verify_UpdatedLevel3()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[GenerateSReference(2, 4)];
                testClass.Verify_Group(3, 1, readBackParent, null, null, UpdateColorIncrement);
                testClass.Verify_Group(3, 2, readBackParent);
            }
        }

        class ImportDeleted
        {
            readonly GroupImportTest testClass;
            const string UpdatePostfixName = "UN";
            const string UpdatePostfixDescription = "UD";
            const int UpdateColorIncrement = 2;

            public ImportDeleted(GroupImportTest testClass)
            {
                this.testClass = testClass;
            }

            public int CreateInputFileForDeleted(TextWriter textWriter)
            {
                var inputItems = new List<GroupForTest> {

                // level 1
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 1), GenerateSReference(1, 1), GenerateColorString(1, 1), GenerateDescription(1, 1)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 3), GenerateSReference(1, 3), GenerateColorString(1, 3), GenerateDescription(1, 3)),
                // level 2
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 1), GenerateSReference(2, 1), GenerateColorString(2, 1), GenerateDescription(2, 1)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 2), GenerateSReference(2, 2), GenerateColorString(2, 2), GenerateDescription(2, 2)),
                };
                return CreateInputFile(inputItems, textWriter);
            }

            public async Task CreateZonesForDeletedAsync(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                await CreateZonesForDeletedAfterGettingGroupsFromDBAsync(api);
            }

            public async Task CreateDevicesForDeletedAsync(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                await CreateDevicesForDeletedAfterGettingGroupsFromDAsyncB(api);
            }

            public async Task CreateUsersForDeletedAsync(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                await CreateUsersForDeletedAfterGettingGroupsFromDBAsync(api);
            }

            public async Task CreateDriversForDeletedAsync(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                await CreateDriversForDeletedAfterGettingGroupsFromDBAsync(api);
            }

            public async Task CreateRulesForDeletedAsync(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                await CreateRulesForDeletedAfterGettingGroupsFromDBAsync(api);
            }

            public async Task CreateCustomReportScheduleForDeleted_NormalReportAsync(API api)
            {
                await CreateCustomReportScheduleForDeletedAsync(api, ReportDestination.NormalReport);
            }

            public async Task CreateCustomReportScheduleForDeleted_DashboardAsync(API api)
            {
                await CreateCustomReportScheduleForDeletedAsync(api, ReportDestination.Dashboard);
            }

            public async Task CreateCustomReportScheduleForDeleted_EmailPdfAsync(API api)
            {
                await CreateCustomReportScheduleForDeletedAsync(api, ReportDestination.EmailPdf);
            }

            async Task CreateCustomReportScheduleForDeletedAsync(API api, ReportDestination reportDestination)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                await CreateCustomReportScheduleAfterGettingGroupsFromDBAsync(api, reportDestination);
            }

            async Task CreateZonesForDeletedAfterGettingGroupsFromDBAsync(API api)
            {
                await testClass.AddZoneToGroupAsync(api, GenerateSReference(2, 3));
                await testClass.AddZoneToGroupAsync(api, GenerateSReference(1, 2));
            }

            /// <summary>
            /// Add devices to DB and associate them with groups for testing
            /// </summary>
            /// <param name="api"></param>
            /// <returns>device number of the last device created</returns>
            async Task<int> CreateDevicesForDeletedAfterGettingGroupsFromDAsyncB(API api)
            {
                var sReference = GenerateSReference(2, 3);
                int deviceNumber = 1;
                deviceNumber = await testClass.AddDevicesToGroupAsync(api, sReference, deviceNumber, 2) + 1;
                sReference = GenerateSReference(1, 2);
                deviceNumber = await testClass.AddDevicesToGroupAsync(api, sReference, deviceNumber);
                return deviceNumber;
            }

            async Task<int> CreateUsersForDeletedAfterGettingGroupsFromDBAsync(API api)
            {
                var sReference = GenerateSReference(2, 3);
                int userNumber = 1;
                userNumber = await testClass.AddUsersToGroupAsync(api, sReference, userNumber, 2) + 1;
                sReference = GenerateSReference(1, 2);
                userNumber = await testClass.AddUsersToGroupAsync(api, sReference, userNumber);
                return userNumber;
            }

            async Task CreateDriversForDeletedAfterGettingGroupsFromDBAsync(API api)
            {
                var group = testClass.GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(GenerateSReference(2, 3));
                await testClass.AddDriversToGroupAsync(api, group);
                await testClass.AddDriversToGroupAsync(api, group);
                await testClass.AddDriversToGroupAsync(api, testClass.GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(GenerateSReference(1, 2)));
            }

            async Task<int> CreateRulesForDeletedAfterGettingGroupsFromDBAsync(API api)
            {
                var sReference = GenerateSReference(2, 3);
                int ruleNumber = 1;
                ruleNumber = await testClass.AddRulesToGroupAsync(api, sReference, ruleNumber, 2) + 1;
                sReference = GenerateSReference(1, 2);
                ruleNumber = await testClass.AddRulesToGroupAsync(api, sReference, ruleNumber);
                return ruleNumber;
            }

            async Task<int> CreateCustomReportScheduleAfterGettingGroupsFromDBAsync(API api, ReportDestination reportDestination)
            {
                var sReference = GenerateSReference(2, 3);
                int reportNumber = 1;
                reportNumber = await testClass.AddCustomReportScheduleToGroupAsync(api, sReference, reportDestination, ReportToGroupAssociation.IncludeAllChildrenGroups, reportNumber) + 1;
                reportNumber = await testClass.AddCustomReportScheduleToGroupAsync(api, sReference, reportDestination, ReportToGroupAssociation.IncludeDirectChildrenOnlyGroups, reportNumber) + 1;
                sReference = GenerateSReference(1, 2);
                reportNumber = await testClass.AddCustomReportScheduleToGroupAsync(api, sReference, reportDestination, ReportToGroupAssociation.ScopeGroups, reportNumber);
                return reportNumber;
            }

            public async Task Verify_WithZones_DeletedAsync(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                Assert.Equal(testClass.numberOfLinesParsed + 3, testClass.lookupSreferenceToGroupFromDbForVerification.Count);
                for (int i = 1; i <= 2; i++)
                {
                    switch (i)
                    {
                        case 1:
                            Verify_WithOneAssociatedAssetClass_DeletedLevel1();
                            break;
                        case 2:
                            Verify_WithOneAssociatedAssetClass_DeletedLevel2();
                            break;
                    }
                }
            }

            public async Task Verify_WithOneAssociatedAssetClass_Deleted(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                Assert.Equal(testClass.numberOfLinesParsed + 3, testClass.lookupSreferenceToGroupFromDbForVerification.Count);
                for (int i = 1; i <= 2; i++)
                {
                    switch (i)
                    {
                        case 1:
                            Verify_WithOneAssociatedAssetClass_DeletedLevel1();
                            break;
                        case 2:
                            Verify_WithOneAssociatedAssetClass_DeletedLevel2();
                            break;
                    }
                }
            }

            /// <summary>
            /// Applies to all tests where only one associated asset class, like only Zones, only Devices, only Users, etc. prevent Group Deletion,
            /// because all these tests have similar initial group structure, "desired after deletion" and "actual after deletion" group structure
            /// </summary>
            /// <param name="api">The API.</param>
            public async Task Verify_Deleted(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                Assert.Equal(testClass.numberOfLinesParsed + 1, testClass.lookupSreferenceToGroupFromDbForVerification.Count);
                for (int i = 1; i <= 2; i++)
                {
                    switch (i)
                    {
                        case 1:
                            Verify_DeletedLevel1();
                            break;
                        case 2:
                            Verify_DeletedLevel2();
                            break;
                    }
                }
            }

            void Verify_WithOneAssociatedAssetClass_DeletedLevel1()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[FirstLineParentSreference];
                for (int j = 1; j <= 3; j++)
                {
                    testClass.Verify_Group(1, j, readBackParent);
                }
            }

            void Verify_WithOneAssociatedAssetClass_DeletedLevel2()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 1)];
                for (int j = 1; j <= 3; j++)
                {
                    testClass.Verify_Group(2, j, readBackParent);
                }
            }

            void Verify_DeletedLevel1()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[FirstLineParentSreference];
                testClass.Verify_Group(1, 1, readBackParent);
                testClass.Verify_Group(1, 3, readBackParent);
            }

            void Verify_DeletedLevel2()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 1)];
                for (int j = 1; j <= 2; j++)
                {
                    testClass.Verify_Group(2, j, readBackParent);
                }
            }
        }

        class ImportMovedUpdated
        {
            readonly GroupImportTest testClass;
            readonly string updatePostfixName;
            readonly string updatePostfixDescription;
            readonly int updateColorIncrement;

            public ImportMovedUpdated(GroupImportTest testClass, string updatePostfixName = "UN", string updatePostfixDescription = "UD", int updateColorIncrement = 2)
            {
                this.testClass = testClass;
                this.updatePostfixName = updatePostfixName;
                this.updatePostfixDescription = updatePostfixDescription;
                this.updateColorIncrement = updateColorIncrement;
            }

            public int CreateInputFileForMovedUpdated(TextWriter textWriter)
            {
                var inputItems = new List<GroupForTest> {

                // level 1
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 1), GenerateSReference(1, 1), GenerateColorString(1, 1), GenerateDescription(1, 1)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 2), GenerateSReference(1, 2), GenerateColorString(1, 2), GenerateDescription(1, 2)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 3), GenerateSReference(1, 3), GenerateColorString(1, 3), GenerateDescription(1, 3)),

                // level 2
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 1), GenerateSReference(2, 1), GenerateColorString(2, 1), GenerateDescription(2, 1)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 2), GenerateSReference(2, 2), GenerateColorString(2, 2), GenerateDescription(2, 2)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 4, updatePostfixName), GenerateSReference(2, 4), GenerateColorString(2, 4), GenerateDescription(2, 4)),

                // level 3
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 1), GenerateSReference(3, 1), GenerateColorString(3, 1), GenerateDescription(3, 1)),
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 2), GenerateSReference(3, 2), GenerateColorString(3, 2), GenerateDescription(3, 2)),
                new GroupForTest(GenerateName(2, 4, updatePostfixName), GenerateSReference(2, 4), GenerateName(2, 3), GenerateSReference(2, 3), GenerateColorString(2, 3, updateColorIncrement), GenerateDescription(2, 3, updatePostfixDescription))
                };
                return CreateInputFile(inputItems, textWriter);
            }

            public async Task Verify_MovedUpdated(API api)
            {
                (testClass.lookupSreferenceToGroupFromDbForVerification, testClass.lookupIdToGroupFromDbForVerification, testClass.groupsInDBWithNonUniqueReferenceLookup, testClass.groupsInDBWithNonUniqueIdLookup) = await GetAllGroupsFromDBAsync(api);
                Assert.Equal(testClass.numberOfLinesParsed + 1, testClass.lookupSreferenceToGroupFromDbForVerification.Count);
                for (int i = 1; i <= 3; i++)
                {
                    switch (i)
                    {
                        case 1:
                            Verify_MovedUpdatedLevel1();
                            break;
                        case 2:
                            Verify_MovedUpdatedLevel2();
                            break;
                        case 3:
                            Verify_MovedUpdatedLevel3();
                            break;
                    }
                }
            }

            void Verify_MovedUpdatedLevel1()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[FirstLineParentSreference];
                for (int j = 1; j <= 3; j++)
                {
                    testClass.Verify_Group(1, j, readBackParent);
                }
            }

            void Verify_MovedUpdatedLevel2()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[GenerateSReference(1, 1)];
                for (int j = 1; j <= 2; j++)
                {
                    testClass.Verify_Group(2, j, readBackParent);
                }
                testClass.Verify_Group(2, 4, readBackParent, updatePostfixName);
            }

            void Verify_MovedUpdatedLevel3()
            {
                var readBackParent = testClass.lookupSreferenceToGroupFromDbForVerification[GenerateSReference(2, 4)];
                for (int j = 1; j <= 2; j++)
                {
                    testClass.Verify_Group(3, j, readBackParent);
                }
                testClass.Verify_Group(2, 3, readBackParent, null, updatePostfixDescription, updateColorIncrement);
            }
        }

        async Task<ImportGroupParser> InitializeParserWithThreeGroupsAsync(API api)
        {
            var firstLineParentGroup = await CreateFirstLineParentGroupAsync(api);
            return await SimulateParsingFromFileAsync(api, CreateInputFileForSimpleImport, Parser_RowParsed);
        }

        async Task InitializeDatabaseWithThreeGroupsAsync(API api)
        {
            var firstLineParentGroup = await CreateFirstLineParentGroupAsync(api);
            await SimulateParsingAndImportFromFileAsync(api, CreateInputFileForSimpleImport, Parser_RowParsed, Importer_GroupImported);
        }

        async Task InitializeDatabaseAsync(API api, CreateInputFileDelegate inputFileFiller, IList<Func<API, Task>> assetCreators = null)
        {
            Log.WriteLine("Database Initialization Start");
            var firstLineParentGroup = await CreateFirstLineParentGroupAsync(api);
            await SimulateParsingAndImportFromFileAsync(api, CreateInputFileForDatabaseInitialization, Parser_RowParsed, Importer_GroupImported);
            Log.WriteLine($"Database Initialization End{Environment.NewLine}");
            if (assetCreators != null)
            {
                foreach (var assetCreator in assetCreators)
                {
                    await assetCreator(api);
                }
            }
        }

        delegate int CreateInputFileDelegate(TextWriter textWriter);

        async Task<ImportGroupParser> SimulateParsingFromFileAsync(API api, CreateInputFileDelegate inputFileFiller, EventHandler<RowParsedEventArgs<Group>> rowParsedHandler = null)
        {
            var parser = new ImportGroupParser(api, FirstLineParentSreference);
            if (rowParsedHandler != null)
            {
                parser.RowParsed += rowParsedHandler;
            }
            using (var stream = new MemoryStream())
            {
                var numEntries = 0;
                using (var streamWriter = new StreamWriter(stream, Encoding.ASCII))
                {
                    numEntries = inputFileFiller(streamWriter);
                    stream.Position = 0;
                    var groupsParsedList = await parser.ParseAsync(stream);
                    numberOfLinesParsed = groupsParsedList.Count;
                }
                //Test Parser
                Assert.Equal(numEntries, numberOfLinesParsed);
            }
            return parser;
        }

        async Task SimulateParsingAndImportFromFileAsync(API api, CreateInputFileDelegate inputFileFiller, EventHandler<RowParsedEventArgs<Group>> rowParsedHandler = null, EventHandler<EntityImportedEventArgs<GroupImporter.GroupWithLoggingData>> itemImportedHandler = null, bool deleteEmptyGroups = false, bool moveAssetsUp = false)
        {
            var parser = await SimulateParsingFromFileAsync(api, inputFileFiller, rowParsedHandler);

            var importer = new GroupImporter(api, parser.FirstLineParentGroupParsed, parser.FirstLineParentGroupFromDB, parser.GroupsParsedList.Count, parser.GroupLookupParsed, parser.GroupLookupFromDB, deleteEmptyGroups, moveAssetsUp);
            if (itemImportedHandler != null)
            {
                importer.EntityImported += itemImportedHandler;
            }
            await importer.DetermineDispositionAndImportGroupsAsync();
        }

        int CreateInputFileForSimpleImport(TextWriter textWriter)
        {
            var inputItems = new List<GroupForTest> {
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 0), GenerateSReference(1, 0), GenerateColorString(1,0), GenerateDescription(1, 0)),
                new GroupForTest(GenerateName(1, 0), GenerateSReference(1, 0), GenerateName(2, 0), GenerateSReference(2, 0), GenerateColorString(2,0), GenerateDescription(2, 0)),
                new GroupForTest(GenerateName(2, 0), GenerateSReference(2, 0), GenerateName(3, 0), GenerateSReference(3, 0), GenerateColorString(3,0), GenerateDescription(3, 0))
            };
            return CreateInputFile(inputItems, textWriter);
        }

        int CreateInputFileForDatabaseInitialization(TextWriter textWriter)
        {
            var inputItems = new List<GroupForTest> {

                // level 1
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 1), GenerateSReference(1, 1), GenerateColorString(1, 1), GenerateDescription(1, 1)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 2), GenerateSReference(1, 2), GenerateColorString(1, 2), GenerateDescription(1, 2)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 3), GenerateSReference(1, 3), GenerateColorString(1, 3), GenerateDescription(1, 3)),

                // level 2
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 1), GenerateSReference(2, 1), GenerateColorString(2, 1), GenerateDescription(2, 1)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 2), GenerateSReference(2, 2), GenerateColorString(2, 2), GenerateDescription(2, 2)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 3), GenerateSReference(2, 3), GenerateColorString(2, 3), GenerateDescription(2, 3)),
                new GroupForTest(GenerateName(1, 2), GenerateSReference(1, 2), GenerateName(2, 4), GenerateSReference(2, 4), GenerateColorString(2, 4), GenerateDescription(2, 4)),

                // level 3
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 1), GenerateSReference(3, 1), GenerateColorString(3, 1), GenerateDescription(3, 1)),
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 2), GenerateSReference(3, 2), GenerateColorString(3, 2), GenerateDescription(3, 2)),
            };
            return CreateInputFile(inputItems, textWriter);
        }

        int CreateInputFileForAdded(TextWriter textWriter)
        {
            var inputItems = new List<GroupForTest> {

                // level 1
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 1), GenerateSReference(1, 1), GenerateColorString(1, 1), GenerateDescription(1, 1)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 2), GenerateSReference(1, 2), GenerateColorString(1, 2), GenerateDescription(1, 2)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 3), GenerateSReference(1, 3), GenerateColorString(1, 3), GenerateDescription(1, 3)),
                new GroupForTest(FirstLineParentName, FirstLineParentSreference, GenerateName(1, 4), GenerateSReference(1, 4), GenerateColorString(1, 4), GenerateDescription(1, 4)),
                // level 2
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 1), GenerateSReference(2, 1), GenerateColorString(2, 1), GenerateDescription(2, 1)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 2), GenerateSReference(2, 2), GenerateColorString(2, 2), GenerateDescription(2, 2)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 3), GenerateSReference(2, 3), GenerateColorString(2, 3), GenerateDescription(2, 3)),
                new GroupForTest(GenerateName(1, 1), GenerateSReference(1, 1), GenerateName(2, 5), GenerateSReference(2, 5), GenerateColorString(2, 5), GenerateDescription(2, 5)),
                new GroupForTest(GenerateName(1, 2), GenerateSReference(1, 2), GenerateName(2, 4), GenerateSReference(2, 4), GenerateColorString(2, 4), GenerateDescription(2, 4)),
                // level 3
                new GroupForTest(GenerateName(2, 1), GenerateSReference(2, 1), GenerateName(3, 3), GenerateSReference(3, 3), GenerateColorString(3, 3), GenerateDescription(3, 3)),
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 1), GenerateSReference(3, 1), GenerateColorString(3, 1), GenerateDescription(3, 1)),
                new GroupForTest(GenerateName(2, 4), GenerateSReference(2, 4), GenerateName(3, 2), GenerateSReference(3, 2), GenerateColorString(3, 2), GenerateDescription(3, 2)),
            };
            return CreateInputFile(inputItems, textWriter);
        }

        async Task AddZoneToGroupAsync(API api, string groupSreference)
        {
            var group = GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(groupSreference);
            var zone = testValueHelper.GetZone(new List<Group> { group });
            zone.Id = await api.CallAsync<Id>("Add", typeof(Zone), new { entity = zone });
        }

        /// <summary>
        /// Creates and adds devices to a group
        /// </summary>
        /// <param name="api"></param>
        /// <param name="groupSreference"></param>
        /// <param name="deviceNumber"></param>
        /// <param name="deviceCount"></param>
        /// <returns>number of the last added device</returns>
        async Task<int> AddDevicesToGroupAsync(API api, string groupSreference, int deviceNumber, int deviceCount = 1)
        {
            var group = GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(groupSreference);
            for (int i = 0; i < deviceCount; i++)
            {
                deviceNumber += i;
                var device = testValueHelper.GetGoDevice(deviceNumber, new List<Group> { group });
                device.Id = await api.CallAsync<Id>("Add", typeof(Device), new { entity = device });
                Assert.NotNull(device.Id);
            }
            return deviceNumber;
        }

        async Task<int> AddUsersToGroupAsync(API api, string groupSreference, int userNumber, int userCount = 1)
        {
            var group = GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(groupSreference);
            var organizationGroups = new List<Group> { group };
            List<Group> reportGroups = null;
            var securityGroups = new List<Group> { new EverythingSecurityGroup() };
            for (int i = 0; i < userCount; i++)
            {
                userNumber += i;
                var user = User.CreateBasicUser(null, null, $"User{userNumber}@geotab.com", $"Fname{userNumber}", $"Lname{userNumber}", $"Password{userNumber}", "", "", "", DateTime.MinValue, DateTime.MaxValue, organizationGroups, reportGroups, securityGroups, null);
                user.Id = await api.CallAsync<Id>("Add", typeof(User), new { entity = user });
                Assert.NotNull(user.Id);
            }
            return userNumber;
        }

        async Task AddDriversToGroupAsync(API api, Group group)
        {
            var companyGroups = new List<Group> { group };
            var driver = testValueHelper.GetDriver();
            driver.CompanyGroups = driver.DriverGroups = companyGroups;
            driver.Id = await api.CallAsync<Id>("Add", typeof(User), new { entity = driver });
        }

        async Task<int> AddRulesToGroupAsync(API api, string groupSreference, int ruleNumber, int ruleCount = 1)
        {
            var group = GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(groupSreference);
            for (int i = 0; i < ruleCount; i++)
            {
                ruleNumber += i;
                var rule = new Rule(null, null, $"Rule{ruleNumber}", new Color(255, 127, 80), "Comment", new List<Group> { group }, ExceptionRuleBaseType.Custom, DateTime.UtcNow, DateTime.MaxValue)
                {
                    Condition = new Condition { ConditionType = ConditionType.Aux1, Value = 1 }
                };
                rule.Id = await api.CallAsync<Id>("Add", typeof(Rule), new { entity = rule });
                Assert.NotNull(rule.Id);
            }
            return ruleNumber;
        }

        /// <summary>
        /// Desribes group to CustomReportSchedule association for Report View, Dashboard and Email reports
        /// </summary>
        enum ReportToGroupAssociation
        {
            /// <summary>
            /// The scope groups
            /// </summary>
            ScopeGroups,
            /// <summary>
            /// All children groups of the specified group a associated with report
            /// </summary>
            IncludeAllChildrenGroups,
            /// <summary>
            /// Only direct children only groups
            /// </summary>
            IncludeDirectChildrenOnlyGroups
        }

        async Task<int> AddCustomReportScheduleToGroupAsync(API api, string groupSreference, ReportDestination reportDestination, ReportToGroupAssociation groupToReportAssociation, int reportNumber, int reportCount = 1)
        {
            var group = GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(groupSreference);
            for (int i = 0; i < reportCount; i++)
            {
                reportNumber += i;
                IList<Group> scopeGroups = null, includeAllChildrenGroups = null, includeDirectChildrenOnlyGroups = null;

                switch (groupToReportAssociation)
                {
                    case ReportToGroupAssociation.ScopeGroups:
                        scopeGroups = new List<Group> { group };
                        break;
                    case ReportToGroupAssociation.IncludeAllChildrenGroups:
                        includeAllChildrenGroups = new List<Group> { group };
                        break;
                    case ReportToGroupAssociation.IncludeDirectChildrenOnlyGroups:
                        includeDirectChildrenOnlyGroups = new List<Group> { group };
                        break;
                    default:
                        Assert.True(false, $"Invalid GroupToReportAssociation: ${groupToReportAssociation} specified!");
                        break;
                }
                var report = testValueHelper.GetCustomReportSchedule($"Report{reportNumber}", reportDestination, scopeGroups, includeAllChildrenGroups, includeDirectChildrenOnlyGroups);
                report.Id = await api.CallAsync<Id>("Add", typeof(CustomReportSchedule), new { entity = report });
                Assert.NotNull(report.Id);
            }
            return reportNumber;
        }

        Group GetGroupFrom_LookupSreferenceToGroupFromDbForVerification(string groupSreference)
        {
            if (!lookupSreferenceToGroupFromDbForVerification.TryGetValue(groupSreference, out Group group))
            {
                Assert.False(true, $"In {nameof(AddDevicesToGroupAsync)}: Group with SReference {groupSreference} is not in the database.");
            }
            return group;
        }

        void Importer_GroupImported(object sender, EntityImportedEventArgs<GroupImporter.GroupWithLoggingData> e)
        {
            GroupImporter.GroupImportedHandler(sender, e, Log.WriteLine, isVerboseMode, false);
        }

        void Parser_RowParsed(object sender, RowParsedEventArgs<Group> e)
        {
            ImportGroupParser.RowParsedHandler(sender, e, Log.WriteLine, isVerboseMode, false);
        }

        static int CreateInputFile(IList<GroupForTest> inputItems, TextWriter textWriter)
        {
            foreach (var item in inputItems)
            {
                textWriter.WriteLine($"{item.ParentName},{item.ParentSreference},{item.ChildName},{item.ChildSreference},{item.ChildDescription},{item.ChildColor}");
            }
            textWriter.Flush();
            return inputItems.Count;
        }

        /// <summary>
        /// creates Group under CompanyGroup, calls 
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        async Task<Group> CreateFirstLineParentGroupAsync(API api)
        {
            var groups = await api.CallAsync<List<Group>>("Get", typeof(Group));
            foreach (var group in groups)
            {
                if (group.Reference.Equals(FirstLineParentSreference))
                {
                    return group;
                }
            }

            var parentGroup = new Group
            {
                Parent = new CompanyGroup(),
                Name = FirstLineParentName,
                Reference = FirstLineParentSreference
            };
            parentGroup.PopulateDefaults();

            parentGroup.Id = await api.CallAsync<Id>("Add", typeof(Group), new { entity = parentGroup });
            return parentGroup;
        }

        /// <summary>
        /// calls server
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="lookupSreferenceToGroup">The lookup sreference to group.</param>
        /// <param name="lookupIdToGroup">The lookup identifier to group.</param>
        /// <param name="groupsWithNonUniqueReferenceLookup">The lookup of groups with non-unique references</param>
        /// <param name="groupsWithNonUniqueIdLookup">The lookup of groups with non-unique Ids</param>
        static async Task<(Dictionary<string, Group>, Dictionary<Id, Group>, IDictionary<string, IList<Group>>, IDictionary<Id, IList<Group>>)> GetAllGroupsFromDBAsync(API api)
        {
            IList<Group> groupListFromDB = await api.CallAsync<List<Group>>("Get", typeof(Group));
            
            IDictionary<string, IList<Group>> groupsWithNonUniqueReferenceLookup;
            IDictionary<Id, IList<Group>> groupsWithNonUniqueIdLookup;
            Dictionary<string, Group> lookupSreferenceToGroup = RowParser<Group>.CreateDictionary(groupListFromDB, g => g.Reference, out groupsWithNonUniqueReferenceLookup);
            Dictionary<Id, Group> lookupIdToGroup = RowParser<Group>.CreateDictionary(groupListFromDB, g => g.Id, out groupsWithNonUniqueIdLookup);
            return (lookupSreferenceToGroup, lookupIdToGroup, groupsWithNonUniqueReferenceLookup, groupsWithNonUniqueIdLookup);
        }

        static string GenerateName(int level, int groupNumberInLevel, string postfix = null)
        {
            return $"Name Level {level} G{groupNumberInLevel}{postfix}";
        }

        // Do not use method below with postfix to update Sreference if postfix was not used during creation
        static string GenerateSReference(int level, int groupNumberInLevel, string postfix = null)
        {
            return $"SReference Level {level} G{groupNumberInLevel}{postfix}";
        }

        static string GenerateDescription(int level, int groupNumberInLevel, string postfix = null)
        {
            return $"Description Level {level} G{groupNumberInLevel}{postfix}";
        }

        static string GenerateColorString(int level, int groupNumberInLevel, int increment = 0)
        {
            return GenerateColorInt(level, groupNumberInLevel + increment).ToString();
        }

        static Color GenerateColor(int level, int groupNumberInLevel, int increment = 0)
        {
            return new Color(GenerateColorInt(level, groupNumberInLevel + increment), false);
        }

        static int GenerateColorInt(int level, int groupNumberInLevel)
        {
            var colors = Enum.GetValues(typeof(GroupColor));
            var index = (level * 10 + groupNumberInLevel * 100) % colors.Length;
            return (int)colors.GetValue(index);
        }

        abstract class RowParser<TItem>
        {
            protected readonly API checkmateApi;
            protected int row;

            protected RowParser(API checkmateApi)
            {
                this.checkmateApi = checkmateApi;
            }

            public delegate TKey GetKey<TKey, TValue>(TValue entity);

            public event EventHandler<RowParsedEventArgs<TItem>> RowParsed;

            /// <summary>
            /// Creates the dictionary. If an element with the existing key encountered again it is added to <paramref name="nonUniqueElementsLookup" />. Thus one instance of Value with non-unique Key will always be in the returned Dictionary and the rest of them will be in <paramref name="nonUniqueElementsLookup" />.
            /// </summary>
            /// <typeparam name="TKey">The type of the key.</typeparam>
            /// <typeparam name="TValue">The type of the value.</typeparam>
            /// <param name="collection">The collection.</param>
            /// <param name="getKey">The unique string.</param>
            /// <param name="nonUniqueElementsLookup">The non unique elements lookup.</param>
            /// <param name="comp"><see cref="IEqualityComparer{T}"/> implementation implementation</param>
            /// <returns></returns>
            public static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(ICollection<TValue> collection, GetKey<TKey, TValue> getKey, out IDictionary<TKey, IList<TValue>> nonUniqueElementsLookup, IEqualityComparer<TKey> comp = null)
            {
                Dictionary<TKey, TValue> lookup = comp == null ? new Dictionary<TKey, TValue>(collection.Count) : new Dictionary<TKey, TValue>(collection.Count, comp);
                nonUniqueElementsLookup = new Dictionary<TKey, IList<TValue>>();
                foreach (var item in collection)
                {
                    var key = getKey(item);
                    if (IsKeyValid(key))
                    {
                        if (!lookup.ContainsKey(key))
                        {
                            lookup.Add(key, item);
                        }
                        else
                        {
                            if (!nonUniqueElementsLookup.TryGetValue(key, out IList<TValue> nonUniqueElementsList))
                            {
                                nonUniqueElementsList = new List<TValue>();
                                nonUniqueElementsLookup.Add(key, nonUniqueElementsList);
                            }
                            nonUniqueElementsList.Add(item);
                        }
                    }
                }
                return lookup;
            }

            public virtual List<TItem> Parse(Stream stream)
            {
                List<TItem> items = new List<TItem>();
                row = 0;
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.StartsWith("'", StringComparison.Ordinal) || line.StartsWith("--", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal) || string.IsNullOrEmpty(line))
                        {
                            // Comment line, continue
                            continue;
                        }
                        row++;
                        try
                        {
                            TItem item = ParseLine(line, items);
                            OnRowParsed(new RowParsedEventArgs<TItem>(item, row, line));
                        }
                        catch (Exception exception)
                        {
                            OnRowParsed(new RowParsedEventArgs<TItem>(row, line, exception));
                        }
                    }
                }
                return items;
            }

            protected void OnRowParsed(RowParsedEventArgs<TItem> e)
            {
                EventHandler<RowParsedEventArgs<TItem>> eventHandler = RowParsed;
                eventHandler?.Invoke(this, e);
            }

            protected abstract TItem ParseLine(string line, IList<TItem> items);

            static bool IsKeyValid<TKey>(TKey key)
            {
                if (typeof(TKey) == typeof(string))
                {
                    return !string.IsNullOrEmpty(key as string);
                }
                if (typeof(TKey) == typeof(Id))
                {
                    return key != null;
                }
                throw new NotImplementedException($"IsKeyValid: invalid key type {typeof(Key)}");
            }

            static bool IsKeyValid(string key)
            {
                return !string.IsNullOrEmpty(key);
            }

            static bool IsKeyValid(Id key)
            {
                return key != null;
            }
        }
    }
}
