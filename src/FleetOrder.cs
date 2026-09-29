using System.Collections.Generic;
using UnityEngine;

namespace ExtendedFleetCommands
{
    internal enum OrderKind
    {
        Tow,
        Load,
        Defend,
        Escort,
    }

    /// <summary>
    /// The extended order a fleet ship is following, attached to the ship's GameObject so it goes
    /// away with the ship. Orders aren't saved: warping, docking or reloading clears them.
    /// </summary>
    internal sealed class FleetOrder : MonoBehaviour
    {
        public bool Active;
        public OrderKind Kind;
        public Transform Target;
        public string TargetName;

        /// <summary>Tow and load (and escort loot pickup): how far from the object's centre to stop.</summary>
        public float StandOff;

        /// <summary>Tow: the tractor beam has hold of the target.</summary>
        public bool Attached;

        /// <summary>
        /// Defend: where to wait, relative to the defended ship or station.
        /// Escort: where to follow, relative to the escorted ship's position and heading.
        /// </summary>
        public Vector3 GuardOffset;

        /// <summary>Escort: the loot being picked up near the escorted ship, if any.</summary>
        public Transform Loot;

        /// <summary>Escort: loot the ship tried and couldn't take (loot filter), by instance ID.</summary>
        public readonly HashSet<int> SkippedLoot = new HashSet<int>();

        /// <summary>Escort: the hold filled up, so it stops collecting until given a new order.</summary>
        public bool HoldFull;

        /// <summary>The ship's active order, or null.</summary>
        public static FleetOrder Of(Component ship)
        {
            FleetOrder order = ship.GetComponent<FleetOrder>();
            return order != null && order.Active ? order : null;
        }

        public static FleetOrder GetOrAdd(Component ship)
        {
            FleetOrder order = ship.GetComponent<FleetOrder>();
            return order != null ? order : ship.gameObject.AddComponent<FleetOrder>();
        }

        public string Describe()
        {
            switch (Kind)
            {
                case OrderKind.Tow:
                    return Loc.Get(Loc.OrderTowing, TargetName);
                case OrderKind.Load:
                    return Loc.Get(Loc.OrderLoading, TargetName);
                case OrderKind.Escort:
                    return Loc.Get(Loc.OrderEscorting, TargetName);
                default:
                    return Loc.Get(Loc.OrderDefending, TargetName);
            }
        }
    }
}
