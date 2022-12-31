using NodaTime;
using Moq;
using HVZ.Models;
using HVZ.Persistence.MongoDB.Repos;
using MongoDB.Driver;
namespace HVZ.Persistence.MongoDB.Tests;

public class OrgRepotest : MongoTestBase
{
    private OrgRepo orgRepo = null!;
    private Mock<IUserRepo> userRepoMock = null!;
    private Mock<IGameRepo> gameRepoMock = null!;
    //public OrgRepo CreateOrgRepo() =>
    //        new OrgRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), Mock.Of<IUserRepo>(), Mock.Of<IGameRepo>());

    [SetUp]
    public void SetUp()
    {
        userRepoMock = new Mock<IUserRepo>();
        gameRepoMock = new Mock<IGameRepo>();
        orgRepo = new OrgRepo(CreateTemporaryDatabase(), Mock.Of<IClock>(), userRepoMock.Object, gameRepoMock.Object);
    }

    [Test]
    public async Task create_then_read_are_equal()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";


        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization foundOrg = await orgRepo.Collection.Find(o => o.OwnerId == userid).FirstAsync();

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg.Id, Is.Not.EqualTo(String.Empty));
        Assert.That(createdOrg.Id, Is.Not.EqualTo(String.Empty));
    }

    [Test]
    public async Task test_findorgbyid()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization? foundOrg = await orgRepo.FindOrgById(createdOrg.Id);
        Organization? notFoundOrg = await orgRepo.FindOrgById(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    [Test]
    public async Task test_getorgbyid()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization foundOrg = await orgRepo.GetOrgById(createdOrg.Id);

        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.GetOrgById("000000000000000000000000"));

    }

    [Test]
    public async Task test_findorgbyname()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization? foundOrg = await orgRepo.FindOrgByName(orgname);
        Organization? notFoundOrg = await orgRepo.FindOrgByName(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    [Test]
    public async Task test_findorgbyurl()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization? foundOrg = await orgRepo.FindOrgByUrl(orgurl);
        Organization? notFoundOrg = await orgRepo.FindOrgByUrl(string.Empty);

        Assert.That(createdOrg, Is.EqualTo(foundOrg));
        Assert.That(foundOrg, Is.Not.Null);
        Assert.That(notFoundOrg, Is.Null);
    }

    [Test]
    public async Task test_getorgbyurl()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";

        Organization createdOrg = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Organization foundOrg = await orgRepo.GetOrgByUrl(orgurl);

        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.GetOrgByUrl("none"));
    }

    [Test]
    public async Task test_setactivegameoforg()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string gameid = "1";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.ActiveGameId, Is.Null);

        org = await orgRepo.SetActiveGameOfOrg(org.Id, gameid);

        Assert.That(org.ActiveGameId, Is.EqualTo(gameid));
    }

    [Test]
    public async Task test_getactivegameoforg()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        string gameid = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Game newGame = new("test", gameid, userid, org.Id, Instant.MinValue, true, Player.gameRole.Human, new HashSet<Player>());
        gameRepoMock.Setup(repo => repo.GetGameById("1")).ReturnsAsync(newGame);
        await orgRepo.SetActiveGameOfOrg(org.Id, gameid);

        Game? foundGame = await orgRepo.FindActiveGameOfOrg(org.Id);

        Assert.That(foundGame, Is.Not.Null);
        Assert.That(foundGame, Is.EqualTo(newGame));
    }

    [Test]
    public async Task test_getorgadmins()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        var admins = await orgRepo.GetAdminsOfOrg(org.Id);

        Assert.That(admins.Contains(userid1), Is.True);
    }

    [Test]
    public async Task test_addremoveorgadmin()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        string userid2 = "2";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        org = await orgRepo.AddAdmin(org.Id, userid2);
        Assert.That(org.Administrators.Contains(userid2), Is.True);

        org = await orgRepo.RemoveAdmin(org.Id, userid2);
        Assert.That(org.Administrators.Contains(userid2), Is.False);
    }

    [Test]
    public async Task test_orgcreatorisadmin()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.That(org.Administrators.Contains(userid), Is.True);
    }

    [Test]
    public async Task test_removeOrgOwnerFromAdminThrowsException()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "0";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.RemoveAdmin(org.Id, userid));
    }

    [Test]
    public async Task test_getorgmods()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        var mods = await orgRepo.GetModsOfOrg(org.Id);

        Assert.That(mods.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task test_addremoveorgmod()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        org = await orgRepo.AddModerator(org.Id, userid1);
        Assert.That(org.Moderators.Contains(userid1), Is.True);

        org = await orgRepo.RemoveModerator(org.Id, userid1);
        Assert.That(org.Moderators.Contains(userid1), Is.False);
    }
    [Test]
    public async Task test_setorgowner()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid1 = "1";
        string userid2 = "2";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid1);

        //this should fail because userid2 is not an admin in the org
        Assert.ThrowsAsync<ArgumentException>(async () => await orgRepo.SetOwner(org.Id, userid2));

        await orgRepo.AddAdmin(org.Id, userid2);
        org = await orgRepo.SetOwner(org.Id, userid2);

        Assert.That(org.OwnerId, Is.EqualTo(userid2));
    }

    [Test]
    public async Task test_cant_create_new_game_while_active_game_exists()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "111111111111111111111111";
        string gameName = "testgame";
        string otherGameName = "othertestgame";

        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);

        Game game = new Game(
            name: "gamename",
            gameid: "1",
            creatorid: userid,
            orgid: org.Id,
            createdat: Instant.MinValue,
            isActive: true,
            defaultrole: Player.gameRole.Human,
            players: new HashSet<Player>()
        );

        gameRepoMock.Setup(repo => repo.CreateGame(gameName, userid, org.Id)).ReturnsAsync(game);
        gameRepoMock.Setup(repo => repo.GetGameById(game.Id)).ReturnsAsync(game);
        await orgRepo.CreateGame(gameName, userid, org.Id);
        org = await orgRepo.GetOrgById(org.Id);
        Assert.That(org.ActiveGameId, Is.Not.Null);
        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.CreateGame(otherGameName, userid, org.Id));
    }

    [Test]
    public async Task test_non_admin_cant_create_game()
    {
        string orgname = "test";
        string orgurl = "testurl";
        string userid = "1";
        string otherUserId = "2";
        string gameName = "testgame";
        Organization org = await orgRepo.CreateOrg(orgname, orgurl, userid);
        Game game = new Game(
            name: "gamename",
            gameid: "1",
            creatorid: userid,
            orgid: org.Id,
            createdat: Instant.MinValue,
            isActive: true,
            defaultrole: Player.gameRole.Human,
            players: new HashSet<Player>()
        );

        gameRepoMock.Setup(repo => repo.CreateGame(gameName, userid, org.Id)).ReturnsAsync(game);

        Assert.That(await orgRepo.CreateGame(gameName, userid, org.Id), Is.Not.Null);
        Assert.ThrowsAsync<ArgumentException>(() => orgRepo.CreateGame(gameName, otherUserId, org.Id));
    }
}