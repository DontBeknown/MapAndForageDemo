using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapForageDemo.Gameplay.Level
{
    public sealed class LevelInstance : MonoBehaviour
    {
        [SerializeField] private Area[] _areas;

        public IReadOnlyList<Area> Areas => _areas;

        public Area FindArea(Vector2 position) => Array.Find(_areas, area => area.WorldBounds.Contains(position));

        public Vector2 Constrain(Vector2 position)
        {
            if (FindArea(position) != null)
                return position;

            Vector2 nearest = position;
            float bestDistance = float.PositiveInfinity;
            foreach (var area in _areas)
            {
                Rect bounds = area.WorldBounds;
                var candidate = new Vector2(
                    Mathf.Clamp(position.x, bounds.xMin + 0.01f, bounds.xMax - 0.01f),
                    Mathf.Clamp(position.y, bounds.yMin + 0.01f, bounds.yMax - 0.01f));
                float distance = (candidate - position).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                nearest = candidate;
                bestDistance = distance;
            }

            return nearest;
        }
    }
}
