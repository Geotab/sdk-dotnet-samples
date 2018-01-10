namespace ImportGroupsR.Test
{
    class GroupForTest
    {
        public GroupForTest(string parentName, string parentSreference, string childName, string childSreference, string childDescription, string childColor)
        {
            ParentName = parentName;
            ChildName = childName;
            ParentSreference = parentSreference;
            ChildSreference = childSreference;
            ChildDescription = childDescription;
            ChildColor = childColor;
        }

        public string ParentName { get; set; }
        public string ChildName { get; set; }
        public string ParentSreference { get; set; }
        public string ChildSreference { get; set; }
        public string ChildDescription { get; set; }
        public string ChildColor { get; set; }
    }

}
