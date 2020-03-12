using System;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Configuration;
using Portal.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class CarisProjectNameGeneratorTests
    {
        private ICarisProjectNameGenerator _carisProjectNameGenerator;
        private IOptions<GeneralConfig> _generalConfig;


        [SetUp]
        public async Task Setup()
        {
            _generalConfig = A.Fake<IOptions<GeneralConfig>>();
            _generalConfig.Value.CarisProjectNameCharacterLimit = 50;
            _carisProjectNameGenerator = new CarisProjectNameGenerator(_generalConfig);

        }

        [Test]
        public async Task Test_GenerateProjectName_Shorter_Than_Character_Limit()
        {
            var processId = 123;
            var rsdraNumber = "1234567";
            var sourceDocumentName = "qwertyuiop";

            Assert.AreEqual("123_1234567_qwertyuiop", _carisProjectNameGenerator.Generate(processId, rsdraNumber, sourceDocumentName));
        }

        [Test]
        public async Task Test_GenerateProjectName_Too_Long_Trims_To_Character_Limit()
        {
            var processId = 123;
            var rsdraNumber = "1234567";
            var sourceDocumentName = "qwertyuiopqwertyuiopqwertyuiopqwertyui_trim_this";

            Assert.AreEqual("123_1234567_qwertyuiopqwertyuiopqwertyuiopqwertyui", _carisProjectNameGenerator.Generate(processId, rsdraNumber, sourceDocumentName));
        }

        [Test]
        public async Task Test_GenerateProjectName_Too_Long_Trims_End_Whitespace_To_Final_Character()
        {
            var processId = 123;
            var rsdraNumber = "1234567";
            var sourceDocumentName = "qwertyuiopqwertyuiopqwertyuiop            ";

            Assert.AreEqual("123_1234567_qwertyuiopqwertyuiopqwertyuiop", _carisProjectNameGenerator.Generate(processId, rsdraNumber, sourceDocumentName));
        }

    }
}
