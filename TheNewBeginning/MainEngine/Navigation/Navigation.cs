using System.Collections.Generic;

namespace MainEngine.Navigation
{
    public class Navigation
    {
        public Dictionary<int, NavNode> Nodes { get; set; } = new();
        public Dictionary<int, List<Highway>> Highways { get; set; } = new();
    }
}