using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class TestClassInfo
    {
        public string Name { get; }
        public string Code { get; }

        public TestClassInfo(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
