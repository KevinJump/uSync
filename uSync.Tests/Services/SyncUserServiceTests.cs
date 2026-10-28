using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Services;

[TestFixture]
public class SyncUserServiceTests
{
    private uSyncSettings _settings = null!;
    private IUser? _currentUser;
    private Mock<IUserService> _userService = null!;
    private SyncUserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new uSyncSettings();
        _currentUser = null;

        var configService = new Mock<ISyncConfigService>();
        configService.SetupGet(x => x.Settings).Returns(() => _settings);

        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(x => x.CurrentUser).Returns(() => _currentUser);

        var securityAccessor = new Mock<IBackOfficeSecurityAccessor>();
        securityAccessor.SetupGet(x => x.BackOfficeSecurity).Returns(() => _currentUser is null ? null : security.Object);

        _userService = new Mock<IUserService>();

        _service = new SyncUserService(NullLogger<SyncUserService>.Instance, configService.Object, securityAccessor.Object, _userService.Object);
    }

    private static IUser User(int id)
        => Mock.Of<IUser>(x => x.Id == id);

    [Test]
    public async Task RequestedUser_IsUsed()
    {
        _currentUser = User(5);
        Assert.That(await _service.GetImportUserIdAsync(12), Is.EqualTo(12));
    }

    [Test]
    public async Task RequestedSuperUser_IsUsed()
    {
        _settings.BackgroundUser = Guid.NewGuid();
        Assert.That(await _service.GetImportUserIdAsync(UmbConstants.Security.SuperUserId), Is.EqualTo(UmbConstants.Security.SuperUserId));
    }

    [Test]
    public async Task NoRequestedUser_UsesCurrentBackofficeUser()
    {
        _currentUser = User(5);
        _settings.BackgroundUser = Guid.NewGuid();
        Assert.That(await _service.GetImportUserIdAsync(UmbConstants.Security.UnknownUserId), Is.EqualTo(5));
    }

    [Test]
    public async Task NoUser_UsesBackgroundUser()
    {
        var key = Guid.NewGuid();
        _settings.BackgroundUser = key;
        _userService.Setup(x => x.GetAsync(key)).ReturnsAsync(User(7));

        Assert.That(await _service.GetImportUserIdAsync(UmbConstants.Security.UnknownUserId), Is.EqualTo(7));
    }

    [Test]
    public async Task MissingBackgroundUser_UsesSuperUser()
    {
        _settings.BackgroundUser = Guid.NewGuid();
        _userService.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync((IUser?)null);

        Assert.That(await _service.GetImportUserIdAsync(UmbConstants.Security.UnknownUserId), Is.EqualTo(UmbConstants.Security.SuperUserId));
    }

    [Test]
    public async Task NoUserAndNoBackgroundUser_UsesSuperUser()
    {
        Assert.That(await _service.GetImportUserIdAsync(UmbConstants.Security.UnknownUserId), Is.EqualTo(UmbConstants.Security.SuperUserId));
    }
}
