using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ImportGroupsR.Test
{
    class TestValueHelper
    {
        static Random random = new Random(0);

        public CustomReportSchedule GetCustomReportSchedule(string templateName, ReportDestination reportDestination, IList<Group> scopeGroups, IList<Group> includeAllChildrenGroups, IList<Group> includeDirectChildrenOnlyGroups)
        {
            var argument = new
            {
                devices = new List<Device>(),
                reportArgumentType = "DeviceActivityDetail"
            };
            return new CustomReportSchedule(null, true, ReportFrequency.Daily, ReportPeriod.PreviousDay, GetReportTemplate(templateName), reportDestination, argument, GetRandomDateTime(true), GetUser(), Id.Create(Guid.NewGuid()), scopeGroups, null, includeAllChildrenGroups, includeDirectChildrenOnlyGroups);
        }

        public ReportTemplate GetReportTemplate(string templateName)
        {
            byte[] binary = new byte[100];
            for (int i = 0; i < 100; i++)
            {
                binary[i] = GetRandomByte();
            }

            return new ReportTemplate(null, templateName, ReportDataSource.Device, binary, ReportTemplateType.Custom, true, null);
        }

        public Driver GetDriver()
        {
            var userEmail = GetRandomString(15);
            var driver = new Driver
            {
                Name = userEmail,
                FirstName = GetRandomString(15),
                LastName = GetRandomString(15),
                CompanyGroups = new List<Group>() { new CompanyGroup() },
                SecurityGroups = new List<Group>() { new EverythingSecurityGroup() },
                Password = GetRandomString(15)
            };
            driver.DriverGroups = driver.CompanyGroups;
            return driver;
        }

        public User GetUser()
        {
            var userEmail = GetRandomString(15);
            var user = new User
            {
                Name = userEmail,
                FirstName = GetRandomString(15),
                LastName = GetRandomString(15),
                CompanyGroups = new List<Group>() { new CompanyGroup() },
                SecurityGroups = new List<Group>() { new EverythingSecurityGroup() },
                Password = GetRandomString(15)
            };
            user.PopulateDefaults();
            return user;
        }

        public string GetRandomString(int length)
        {
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(Convert.ToChar((int)Math.Floor(26 * GetRandomDouble() + 65)));
            }
            return sb.ToString();
        }

        public double GetRandomDouble()
        {
            lock (random)
            {
                return random.NextDouble();
            }
        }

        public T GetRandomEnum<T>(List<T> list = null)
        {
            if (typeof(T).GetTypeInfo().BaseType != typeof(Enum))
            {
                throw new InvalidOperationException();
            }
            if (list == null)
            {
                Array values = Enum.GetValues(typeof(T));
                return (T)values.GetValue(GetRandomInt(values.Length));
            }
            return list[GetRandomInt(list.Count - 1)];
        }

        public int GetRandomInt(int maximumValue)
        {
            lock (random)
            {
                return random.Next(maximumValue);
            }
        }

        public int GetRandomInt(int minValue, int maximumValue)
        {
            lock (random)
            {
                return random.Next(minValue, maximumValue);
            }
        }

        public byte GetRandomByte()
        {
            return (byte)GetRandomInt(0, 256);
        }

        public DateTime GetRandomDateTime(bool maxDateTimeNow = true)
        {
            DateTime max = maxDateTimeNow ? DateTime.UtcNow : new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return GetRandomDateTime(SupportedDateTime.MinDateTime, max);
        }

        public DateTime GetRandomDateTime(DateTime minDateTime, DateTime maxDateTime)
        {
            // Spread random instants with even probability between min and max date boundaries.
            if (maxDateTime == DateTime.MaxValue)
            {
                maxDateTime = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            long maxMillis = (long)(maxDateTime - minDateTime).TotalMilliseconds;

            // Spread randomly with even probability between 0 and max boundaries.
            if (maxMillis < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minDateTime), "Negative duration.");
            }
            bool isLargerThenMax = maxMillis.CompareTo(int.MaxValue * (long)int.MaxValue) > 0;
            if (isLargerThenMax)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDateTime), "Duration too large.");
            }
            int maxIntMillis = (int)Math.Sqrt(maxMillis);
            return minDateTime.AddMilliseconds(GetRandomInt(0, maxIntMillis) * (long)GetRandomInt(0, maxIntMillis));
        }

        public Zone GetZone(List<Group> groups)
        {
            var ep = new List<ISimpleCoordinate>();
            for (int i = 0; i < 4; i++)
            {
                ep.Add(new Coordinate(GetRandomInt(-180, 180), GetRandomInt(-90, 90)));
            }

            // Close polygon;
            ep.Add(ep[0]);

            var zone = new Zone
            {
                Name = GetRandomString(16),
                Groups = groups,
                Points = ep
            };
            zone.PopulateDefaults();

            return zone;
        }
    }
}
