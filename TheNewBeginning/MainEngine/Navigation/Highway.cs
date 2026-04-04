using System;
using Microsoft.Xna.Framework;

namespace MainEngine.Navigation
{
    public class Highway
    {
        public int Id { get; set; }
        public int FromId { get; set; }
        public int ToId { get; set; }
        public float Length { get; set; }
        public float SpeedLimit { get; set; } = 1f;
        public float ExtraCost { get; set; } = 0f;
        public bool IsBlocked { get; set; } = false;

        public float Cost
        {
            get
            {
                if (IsBlocked)
                    return float.PositiveInfinity;
                
                float safeSpeed = MathF.Max(0.001f, SpeedLimit);
                return (Length / safeSpeed) + ExtraCost;
            }
        }

        public static Highway Create(
            int id,
            int fromId,
            int toId,
            Vector2 fromPosition,
            Vector2 toPosition,
            float speedLimit = 1f,
            float extraCost = 0f)
        {
            return new Highway
            {
                Id = id,
                FromId = fromId,
                ToId = toId,
                Length = Vector2.Distance(fromPosition, toPosition),
                SpeedLimit = speedLimit,
                ExtraCost = extraCost
            };
        }
    }
}