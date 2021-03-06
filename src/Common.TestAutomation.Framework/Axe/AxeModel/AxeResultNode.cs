﻿using System.Collections.Generic;

namespace Common.TestAutomation.Framework.Axe.AxeModel
{
    public class AxeResultNode
    {
        public List<string> Target { get; set; }
        public string Html { get; set; }
        public string Impact { get; set; }
        public List<object> Any { get; set; }
        public List<object> All { get; set; }
        public List<object> None { get; set; }
    }
}
