using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Nichan
{
    public class Res
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
        public string UserId { get; set; }
        public DateTime? Date { get; set; }
        public XElement Text { get; set; }
    }
    public class Thread
    {
        public Uri Uri { get; set; }
        public string Title { get; set; }
        public int ResCount { get; set; }
        public List<Res> Res { get; set; } = new List<Res>();
    }
    public class Board
    {
        public Uri Uri { get; set; }
        public string Title { get; set; }
        public List<Thread> Threads { get; set; } = new List<Thread>();
    }
}
