using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Domain
{
    /// <summary>
    /// Состояние игрока в рамках какой-то запущенной или планируемой игры.
    /// </summary>
    public class Player
    {
        [BsonConstructor]
        public Player(Guid userId, string name)
        {
            UserId = userId;
            Name = name;
        }

        [BsonElement("userId")]
        public Guid UserId { get; }

        /// <summary>
        /// Снэпшот имени игрока на момент старта игры. Считайте, что это такое требование к игре.
        /// </summary>
        [BsonElement("name")] // 👈 Добавляем BsonElement
        public string Name { get; }
        
        /// <summary>
        /// Ход, который выбрал игрок
        /// </summary>
        public PlayerDecision? Decision { get; set; }
        
        /// <summary>
        /// Текущие очки в игре. Сколько туров выиграл этот игрок.
        /// </summary>
        public int Score { get; set; }
    }
}