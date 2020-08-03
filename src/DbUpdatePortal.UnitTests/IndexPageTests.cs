using Common.Helpers;
using Common.Helpers.Auth;
using DbUpdatePortal.Auth;
using DbUpdatePortal.Configuration;
using DbUpdatePortal.Pages;
using DbUpdateWorkflowDatabase.EF;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DbUpdatePortal.UnitTests
{
    [TestFixture]
    public class IndexPageTests
    {


        private IndexModel _indexModel;
        private IAdDirectoryService _fakeAdDirectoryService;
        private IDbUpdateUserDbService _fakencneUserDbService;
        private DbUpdateWorkflowDbContext _dbContext;
        private ILogger<IndexModel> _fakeLogger;
        private ICarisProjectHelper _fakecarisProjectHelper;
        private IOptions<GeneralConfig> _fakegeneralConfig;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new DbUpdateWorkflowDbContext(dbContextOptions);

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakencneUserDbService = A.Fake<IDbUpdateUserDbService>();

            _fakeLogger = A.Fake<ILogger<IndexModel>>();

            _fakecarisProjectHelper = A.Fake<ICarisProjectHelper>();
            _fakegeneralConfig = A.Fake<IOptions<GeneralConfig>>();

            _indexModel = new IndexModel(_fakencneUserDbService, _dbContext, _fakeLogger, _fakeAdDirectoryService, _fakecarisProjectHelper, _fakegeneralConfig);
        }

        [Test]
        public async Task OnGetAsync_sets_UserFullName_from_userIdentityService()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("The User's Full Name", "user email"));

            await _indexModel.OnGetAsync();

            Assert.AreEqual("The User's Full Name", _indexModel.CurrentUser.DisplayName);
        }
    }
}
