using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using TestApp;

namespace TestApp.Tests
{
    [TestFixture]
    public class RTests
    {
        private R _r;
        [SetUp]
        public void SetUp()
        {
            _r = new R();
        }
    }
}