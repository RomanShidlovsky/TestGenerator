using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp;

public class TestClass
{
    public class C 
    {
        private IDataReader reader;
        public C(IDataReader reader)
        {
            this.reader = reader;
        }
        public void Start(int a, int b, char k)
        {

        }

        public int Start()
        {
            return 0;
        }

        public void Start(int a)
        {

        }

        public int Stop(Object obj) { return 0; }
    }
}

public class R { }

