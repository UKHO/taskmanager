using System;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Portal.Helpers;

namespace Portal.UnitTests
{
    [TestFixture]
    public class AppVersionInfoTests
    {
        public AppVersionInfo _appVersionInfo { get; set; }
        public IHostEnvironment _hostEnvironment { get; set; }

        [SetUp]
        public void Setup()
        {
            _hostEnvironment = A.Fake<IHostEnvironment>();
            _appVersionInfo = new AppVersionInfo(_hostEnvironment);
        }

        [Test]
        public async Task Test_GitHash_set_To_LOCALBUILD_when_no_InformationalVersion_value_exists()
        {
            Assert.AreEqual("LOCALBUILD",_appVersionInfo.GitHash);
        }

        [Test]
        public async Task Test_ShortGitHash_set_to_LBUILD_when_no_InformationalVersion_value_exists()
        {
            Assert.AreEqual("LBUILD", _appVersionInfo.ShortGitHash);
        }

        [Test]
        public async Task Test_BuildNumber_set_to_today()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            StringAssert.Contains(today, _appVersionInfo.BuildNumber);
        }
    }
}
