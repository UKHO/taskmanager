﻿using System;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Hosting;
using NCNEPortal.Helpers;
using NUnit.Framework;

namespace NCNEPortal.UnitTests
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
        public async Task Test_BuildId_set_to_123456_when_no_buildInfo_file_exists()
        {
            Assert.AreEqual("123456", _appVersionInfo.BuildId);
        }

        [Test]
        public async Task Test_BuildNumber_set_to_today_when_no_buildInfo_file_exists()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd") + ".0";
            Assert.AreEqual(today, _appVersionInfo.BuildNumber);
        }
    }
}
