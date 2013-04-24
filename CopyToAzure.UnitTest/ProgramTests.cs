using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CopyToAzure.UnitTest
{
    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public void FindLatestPublishVersion()
        {
            var retval =
                CopyToAzure.Program.GetVersionToPublish(
                    @"C:\dev\MMSD\DARC-WPF\Applications\WPF\DarcWpfClient\DarcWpfClient\publish\");

            Assert.IsTrue(retval != null);
        }
    }
}
