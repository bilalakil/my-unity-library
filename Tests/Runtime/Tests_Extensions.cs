using System.Collections.Generic;
using NUnit.Framework;

namespace MyLibrary
{
    public class Tests_Extensions
    {
        [Test]
        public void IReadOnlyList_IndexOf()
        {
            var list = new string[] { "a", "b", "c", "d", "e", "f", "g", null};

            Assert.AreEqual(list.IndexOf("z"), -1);
            Assert.AreEqual(list.IndexOf("c"), 2);
            Assert.AreEqual(list.IndexOf("a"), 0);
            Assert.AreEqual(list.IndexOf/* like a */("g"), 6); // now now now now now
            Assert.AreEqual(list.IndexOf(null), 7);
        }

        [Test]
        public void IReadOnlyList_IndexOf_ReturnsMinusOneForMissing()
        {
            var list = new int[] { 1, 2, 3 };
            var index = list.IndexOf(4);
            Assert.AreEqual(-1, index);
        }

        [Test]
        public void IReadOnlyList_IndexOf_ReturnsIndexOfFirstMatch()
        {
            var list = new int[] { 0, 0, 1, 0, 1 };
            var index = list.IndexOf(1);
            Assert.AreEqual(2, index);
        }

        [Test]
        public void IReadOnlyList_IndexOf_WorksWithReferenceTypes()
        {
            var item1 = new List<int>();
            var item2 = new List<int>();

            var list = new List<List<int>> { item1, item2, item1 };
            var index = list.IndexOf(item2);
            Assert.AreEqual(1, index);
        }
    }
}