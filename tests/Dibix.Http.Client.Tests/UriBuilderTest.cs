using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Client.Tests
{
    [TestClass]
    public sealed class UriBuilderTest
    {
        [TestMethod]
        public void CrossCheck()
        {
            Uri uri = UriBuilder.Create("some/kind/{of}/uri", UriKind.Relative)
                                .AddQueryParam("name", "luke")
                                .AddQueryParam("id", (int?)null)
                                .AddQueryParam("omitnull", (int?)null, null)
                                .AddQueryParam("omitprimitive", 3, 3)
                                .AddQueryParam("bool", true)
                                .AddQueryParam("array", "first")
                                .AddQueryParam("array", "second")
                                .AddQueryParam("anotherarray", "one", "two")
                                .AddQueryParam("andanotherone", new int?[] { 1, null, 3 })
                                .Build();
            Assert.AreEqual("some/kind/{of}/uri?name=luke&id=&bool=true&array[]=first&array[]=second&anotherarray[]=one&anotherarray[]=two&andanotherone[]=1&andanotherone[]=&andanotherone[]=3", uri.ToString());
        }
    }
}