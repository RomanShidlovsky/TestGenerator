using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Moq;
using TestApp;
using System.Data;

namespace TestApp.Tests
{
    [TestFixture]
    public class CTests
    {
        private TestClass.C _c;
        private Mock<IDataReader> _reader;
        [SetUp]
        public void SetUp()
        {
            _reader = new Mock<IDataReader>();
            _c = new TestClass.C(_reader.Object);
        }

        [Test]
        public void StartTest()
        {
            int a = default;
            int b = default;
            char k = default;
            _c.Start(a, b, k);
            Assert.Fail("autogenerated");
        }

        [Test]
        public void StartTest1()
        {
            var actual = _c.Start();
            int expected = default;
            Assert.That(actual, Is.EqualTo(expected));
            Assert.Fail("autogenerated");
        }

        [Test]
        public void StartTest2()
        {
            int a = default;
            _c.Start(a);
            Assert.Fail("autogenerated");
        }

        [Test]
        public void StopTest()
        {
            Object obj = default;
            var actual = _c.Stop(obj);
            int expected = default;
            Assert.That(actual, Is.EqualTo(expected));
            Assert.Fail("autogenerated");
        }
    }
}