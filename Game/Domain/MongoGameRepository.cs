using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoGameRepository : IGameRepository
    {
        public const string CollectionName = "games";
        private readonly IMongoCollection<GameEntity> gameCollection;

        public MongoGameRepository(IMongoDatabase db)
        {
            gameCollection = db.GetCollection<GameEntity>(CollectionName);
        }

        public GameEntity Insert(GameEntity game)
        {
            gameCollection.InsertOne(game);
            return game;
        }

        public GameEntity FindById(Guid gameId)
        {
            var filter = Builders<GameEntity>.Filter.Eq(g => g.Id, gameId);
            return gameCollection.Find(filter).FirstOrDefault();
        }

        public void Update(GameEntity game)
        {
            var filter = Builders<GameEntity>.Filter.Eq(g => g.Id, game.Id);
            var options = new ReplaceOptions { IsUpsert = false };
            gameCollection.ReplaceOne(filter, game, options);
        }

        // Возвращает не более чем limit игр со статусом GameStatus.WaitingToStart
        public IList<GameEntity> FindWaitingToStart(int limit)
        {
            var filter = Builders<GameEntity>.Filter.Eq(g => g.Status, GameStatus.WaitingToStart);
            return gameCollection
                .Find(filter)
                .Limit(limit)
                .ToList();
        }

        // Обновляет игру, если она находится в статусе GameStatus.WaitingToStart
        public bool TryUpdateWaitingToStart(GameEntity game)
        {
            var filter = Builders<GameEntity>.Filter.And(
                Builders<GameEntity>.Filter.Eq(g => g.Id, game.Id),
                Builders<GameEntity>.Filter.Eq(g => g.Status, GameStatus.WaitingToStart)
            );
            
            var result = gameCollection.ReplaceOne(filter, game);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
    }
}