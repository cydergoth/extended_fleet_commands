using System.Collections.Generic;
using UnityEngine;

namespace ExtendedFleetCommands
{
    /// <summary>Decides what a fleet ship can tow, load or defend.</summary>
    internal static class OrderTargets
    {
        /// <summary>
        /// Drifting wrecks, containers and small asteroids: anything the tractor beam can latch on to
        /// (it needs an <see cref="Entity"/>) that isn't a ship, station, the player or a big asteroid.
        /// </summary>
        public static bool CanTow(Transform target, AIMercenary ship)
        {
            if (target == null || target == ship.transform || target.GetComponent<Rigidbody>() == null)
            {
                return false;
            }
            Entity entity = target.GetComponent<Entity>();
            return entity != null && !entity.IsAI && !entity.IsStation && !entity.IsPlayer && !entity.IsBigAsteroid;
        }

        /// <summary>The loot on <paramref name="target"/>, or null if it isn't loot a ship can pick up.</summary>
        public static Collectible AsLoadable(Transform target)
        {
            Collectible collectible = target != null ? target.GetComponent<Collectible>() : null;
            if (collectible == null || (collectible.hidden && !collectible.detected))
            {
                return null;
            }
            // Credits go straight to the player when they drop, so there are none to load.
            if (collectible.itemType == 3 && collectible.itemID == 0)
            {
                return null;
            }
            return collectible;
        }

        /// <summary>Same rule as CargoSystem.CollectItem: a wreck must be a smaller class than the ship.</summary>
        public static bool FitsInHold(Collectible collectible, AIMercenary ship)
        {
            return collectible.itemType != 4 || (int)ShipDB.GetModel(collectible.itemID).shipClass < ship.GetSS().shipClass;
        }

        public static Collectible FindNearestLoadable(AIMercenary ship)
        {
            return FindNearestLoadable(ship, ship.transform.position, Plugin.LoadSearchRange.Value, null);
        }

        /// <summary>The nearest loot within <paramref name="radius"/> of <paramref name="center"/> the ship can carry.</summary>
        public static Collectible FindNearestLoadable(AIMercenary ship, Vector3 center, float radius, ICollection<int> skip)
        {
            Collectible nearest = null;
            float nearestDistance = radius;
            foreach (Collectible collectible in Object.FindObjectsOfType<Collectible>())
            {
                if (AsLoadable(collectible.transform) == null || collectible.beingTowed || !FitsInHold(collectible, ship)
                    || (skip != null && skip.Contains(collectible.GetInstanceID())))
                {
                    continue;
                }
                float distance = Vector3.Distance(center, collectible.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = collectible;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        /// <summary>Ships (including other fleet ships) and stations that aren't hostile to the player.</summary>
        public static bool CanDefend(Transform target, AIMercenary ship)
        {
            if (target == null || target == ship.transform)
            {
                return false;
            }
            Entity entity = target.GetComponent<Entity>();
            if (entity == null || entity.IsPlayer || !(entity.IsAI || entity.IsStation) || entity.IsDestroyedOrWreckedStation)
            {
                return false;
            }
            return !IsHostile(ship, entity);
        }

        /// <summary>Other ships (including fleet ships) that aren't hostile to the player; not stations.</summary>
        public static bool CanEscort(Transform target, AIMercenary ship)
        {
            if (target == null || target == ship.transform)
            {
                return false;
            }
            Entity entity = target.GetComponent<Entity>();
            return entity != null && entity.IsAI && !IsHostile(ship, entity);
        }

        /// <summary>Fleet ships share the player's friendly-fire system, so this is "hostile to the player".</summary>
        public static bool IsHostile(AIMercenary ship, Entity entity)
        {
            FriendlyFireSystem ffSys = ship.GetSS().ffSys;
            return ffSys != null && ffSys.TargetIsEnemy(entity.ffSys);
        }

        public static string DisplayName(Transform target)
        {
            Collectible collectible = target.GetComponent<Collectible>();
            return collectible != null ? collectible.ItemName : target.name;
        }

        /// <summary>Rough radius of an object on the flight plane, from its solid colliders.</summary>
        public static float Radius(Transform target)
        {
            bool found = false;
            Bounds bounds = default;
            foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            {
                // Skip triggers: a station's docking area is far bigger than the station.
                if (collider.isTrigger)
                {
                    continue;
                }
                if (found)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    found = true;
                }
            }
            return found ? Mathf.Max(bounds.extents.x, bounds.extents.z) : target.localScale.x * 5f;
        }
    }
}
