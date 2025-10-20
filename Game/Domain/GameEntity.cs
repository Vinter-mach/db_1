using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes; 

namespace Game.Domain
{
    public class GameEntity
    {
        [BsonElement("players")] 
        private readonly List<Player> players;

        public GameEntity(int turnsCount)
            : this(Guid.Empty, GameStatus.WaitingToStart, turnsCount, 0, new List<Player>())
        {
        }

        [BsonConstructor] 
        public GameEntity(Guid id, GameStatus status, int turnsCount, int currentTurnIndex, List<Player> players)
        {
            Id = id;
            Status = status;
            TurnsCount = turnsCount;
            CurrentTurnIndex = currentTurnIndex;
            this.players = players ?? new List<Player>(); 
        }

        [BsonId]
        public Guid Id
        {
            get;
            private set;
        }

        public IReadOnlyList<Player> Players => players.AsReadOnly();

        public int TurnsCount { get; }

        public int CurrentTurnIndex { get; private set; }

        public GameStatus Status { get; private set; }

        public void AddPlayer(UserEntity user)
        {
            if (Status != GameStatus.WaitingToStart)
                throw new ArgumentException(Status.ToString());
            
            players.Add(new Player(user.Id, user.Login)); 
            
            if (Players.Count == 2)
                Status = GameStatus.Playing;
        }

        public bool IsFinished()
        {
            return CurrentTurnIndex >= TurnsCount
                   || Status == GameStatus.Finished
                   || Status == GameStatus.Canceled;
        }

        public void Cancel()
        {
            if (!IsFinished())
                Status = GameStatus.Canceled;
        }

        public bool HaveDecisionOfEveryPlayer => Players.All(p => p.Decision.HasValue);

        public void SetPlayerDecision(Guid userId, PlayerDecision decision)
        {
            if (Status != GameStatus.Playing)
                throw new InvalidOperationException(Status.ToString());
            
            var player = Players.SingleOrDefault(p => p.UserId == userId);
            
            if (player == null)
                throw new InvalidOperationException($"Player with ID {userId} not found in game.");

            if (player.Decision.HasValue)
                throw new InvalidOperationException(player.Decision.ToString());
            
            player.Decision = decision;
        }

        public GameTurnEntity FinishTurn()
        {
            if (Status != GameStatus.Playing)
                throw new InvalidOperationException($"Cannot finish turn in status: {Status}");
            if (Players.Count != 2)
                throw new InvalidOperationException("Game must have exactly two players to finish a turn.");
            if (!HaveDecisionOfEveryPlayer)
                throw new InvalidOperationException("Not all players have made a decision.");

            var player1 = Players[0];
            var player2 = Players[1];

            var decision1 = player1.Decision!.Value;
            var decision2 = player2.Decision!.Value;

            var winnerId = Guid.Empty;

            if (decision1.Beats(decision2))
            {
                player1.Score++;
                winnerId = player1.UserId;
            }
            else if (decision2.Beats(decision1))
            {
                player2.Score++;
                winnerId = player2.UserId;
            }

            var result = new GameTurnEntity
            {
                TurnIndex = CurrentTurnIndex,
                WinnerId = winnerId
            };
            
            player1.Decision = null;
            player2.Decision = null;
            CurrentTurnIndex++;
            
            if (CurrentTurnIndex == TurnsCount)
                Status = GameStatus.Finished;
            
            return result;
        }
    }
}