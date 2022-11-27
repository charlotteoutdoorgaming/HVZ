using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using HVZ.Models;
using HVZ.Persistence.MongoDB.Serializers;
using NodaTime;
namespace HVZ.Persistence.MongoDB.Repos;
public class OrgRepo : IOrgRepo
{
    private const string CollectionName = "Orgs";
    public readonly IMongoCollection<Organization> Collection;
    public readonly IUserRepo _userRepo;
    public readonly IGameRepo _gameRepo;
    private readonly IClock _clock;

    static OrgRepo()
    {
        BsonClassMap.RegisterClassMap<Organization>(cm =>
        {
            cm.MapIdProperty(o => o.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(o => o.Name);
            cm.MapProperty(o => o.OwnerId);
            cm.MapProperty(o => o.Moderators);
            cm.MapProperty(o => o.Administrators);
            cm.MapProperty(o => o.Games);
            cm.MapProperty(o => o.ActiveGameId);
            cm.MapProperty(o => o.CreatedAt);
        });
    }

    public OrgRepo(IMongoDatabase database, IClock clock, IUserRepo userRepo, IGameRepo gameRepo)
    {
        database.CreateCollection(CollectionName);
        Collection = database.GetCollection<Organization>(CollectionName);
        _userRepo = userRepo;
        _gameRepo = gameRepo;
        _clock = clock;
        InitIndexes();
    }

    public void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Id)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.OwnerId)),
            new CreateIndexModel<Organization>(Builders<Organization>.IndexKeys.Ascending(o => o.Name)),
        });
    }

    public async Task<Organization> CreateOrg(string name, string creatorUserId)
    {
        await _userRepo.GetUserById(creatorUserId);
        Organization org = new(
            id: string.Empty,
            name: name,
            ownerid: creatorUserId,
            moderators: new(),
            administrators: new() { creatorUserId },
            games: new(),
            activegameid: null,
            createdat: _clock.GetCurrentInstant()
        );
        await Collection.InsertOneAsync(org);

        return org;
    }

    public async Task<Organization?> FindOrgById(string orgId)
        => orgId == string.Empty ? null : await Collection.Find<Organization>(o => o.Id == orgId).FirstAsync();

    public async Task<Organization> GetOrgById(string orgId)
    {
        Organization? org = await FindOrgById(orgId);
        if (org == null)
            throw new ArgumentException($"Org with id {orgId} not found!");
        return (Organization)org;
    }

    public async Task<Organization?> FindOrgByName(string name)
        => name == string.Empty ? null : await Collection.Find<Organization>(o => o.Name == name).FirstAsync();

    public Task<Organization> GetOrgByName(string name)
    {
        throw new NotImplementedException();
    }

    public async Task<Organization> SetActiveGameOfOrg(string orgId, string gameId)
    {
        Organization org = await GetOrgById(orgId);
        if (org.ActiveGameId == gameId)
            return org;
        else
            return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.ActiveGameId, gameId),
            new() { ReturnDocument = ReturnDocument.After });
    }

    public async Task<Game?> FindActiveGameOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        return org.ActiveGameId == null ? null : await _gameRepo.FindGameById(org.ActiveGameId);
    }

    public async Task<HashSet<string>> GetAdminsOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        return org.Administrators;
    }

    public async Task<Organization> AddAdmin(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);
        await _userRepo.GetUserById(userId); //sanity check that the user exists

        org.Administrators.Add(userId);
        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Administrators, org.Administrators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<Organization> RemoveAdmin(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);
        if (org.OwnerId == userId)
            throw new ArgumentException($"User with ID {userId} is the owner of org with id {org.Id}, cannot remove them from this org's admins.");

        org.Administrators.Remove(userId);
        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Administrators, org.Administrators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<HashSet<string>> GetModsOfOrg(string orgId)
    {
        Organization org = await GetOrgById(orgId);
        return org.Moderators;
    }

    public async Task<Organization> AssignOwner(string orgId, string newOwnerId)
    {
        Organization org = await GetOrgById(orgId);
        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.OwnerId, newOwnerId),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<Organization> AddModerator(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);
        await _userRepo.GetUserById(userId); //sanity check that the user exists

        org.Moderators.Add(userId);
        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Moderators, org.Moderators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }

    public async Task<Organization> RemoveModerator(string orgId, string userId)
    {
        Organization org = await GetOrgById(orgId);

        org.Moderators.Remove(userId);
        return await Collection.FindOneAndUpdateAsync(o => o.Id == orgId,
            Builders<Organization>.Update.Set(o => o.Moderators, org.Moderators),
            new() { ReturnDocument = ReturnDocument.After }
            );
    }
}