using System;
using System.Linq;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserEntity> userCollection;
        public const string CollectionName = "users";

        public MongoUserRepository(IMongoDatabase database)
        {
            userCollection = database.GetCollection<UserEntity>(CollectionName);
            var indexKeysDefinition = Builders<UserEntity>.IndexKeys.Ascending(u => u.Login);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<UserEntity>(indexKeysDefinition, indexOptions);
            userCollection.Indexes.CreateOne(indexModel);
        }

        public UserEntity Insert(UserEntity user)
        {
            userCollection.InsertOne(user);
            return user;
        }

        public UserEntity FindById(Guid id)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, id);
            return userCollection.Find(filter).FirstOrDefault();
        }

        public UserEntity GetOrCreateByLogin(string login)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Login, login);
            var existingUser = userCollection.Find(filter).FirstOrDefault();
            
            if (existingUser != null)
                return existingUser;

            try
            {
                var newUser = new UserEntity(Guid.NewGuid(), login, null, null, 0, null);
                userCollection.InsertOne(newUser);
                return newUser;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return userCollection.Find(filter).FirstOrDefault();
            }
        }

        public void Update(UserEntity user)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, user.Id);
            userCollection.ReplaceOne(filter, user);
        }

        public void Delete(Guid id)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, id);
            userCollection.DeleteOne(filter);
        }

        public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
                
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

            var totalCount = (int)userCollection.CountDocuments(FilterDefinition<UserEntity>.Empty);
            var users = userCollection
                .Find(FilterDefinition<UserEntity>.Empty)
                .SortBy(u => u.Login)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            return new PageList<UserEntity>(users, totalCount, pageNumber, pageSize);
        }

        // Не нужно реализовывать этот метод
        public void UpdateOrInsert(UserEntity user, out bool isInserted)
        {
            throw new NotImplementedException();
        }
    }
}