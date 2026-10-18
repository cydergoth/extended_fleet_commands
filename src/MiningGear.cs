using System.Collections.Generic;
using System.Linq;

namespace ExtendedFleetCommands
{
    /// <summary>
    /// The "Only use mining gear when mining" behaviour. A fleet ship with the Mining role and this
    /// setting on mines with its mining weapons and mining drones only; its other weapons and drones
    /// stay quiet while it works an asteroid. A ship with no mining gear doesn't mine at all, as if
    /// it found no asteroid.
    /// </summary>
    internal static class MiningGear
    {
        private const int MiningRole = 2;

        /// <summary>
        /// The setting is kept in <see cref="FleetMemberBehavior.repairTactic"/>, which the game saves
        /// with the ship but never reads, so it survives saving and loading without a file of its own.
        /// A high bit keeps clear of the field's documented values (0 and 1).
        /// </summary>
        private const int OnlyMiningGearFlag = 0x100;

        private static readonly List<ActiveEquipment> miningDrones = new List<ActiveEquipment>();

        public static bool IsOn(AIMercenaryCharacter character)
        {
            return (character.behavior.repairTactic & OnlyMiningGearFlag) != 0;
        }

        public static void Set(AIMercenaryCharacter character, bool on)
        {
            if (on)
            {
                character.behavior.repairTactic |= OnlyMiningGearFlag;
            }
            else
            {
                character.behavior.repairTactic &= ~OnlyMiningGearFlag;
            }
        }

        /// <summary>The ship is a miner with the setting on.</summary>
        public static bool Restricted(AIMercenary ship)
        {
            AIMercenaryCharacter character = ship.aiMercChar;
            return character != null && character.behavior.role == MiningRole && IsOn(character);
        }

        /// <summary>The ship is working an asteroid with the setting on.</summary>
        public static bool RestrictedNow(AIMercenary ship)
        {
            return ship.mining && Restricted(ship);
        }

        /// <summary>Mining weapons (fixed or turret-mounted) or mining drones.</summary>
        public static bool HasMiningGear(AIMercenary ship)
        {
            SpaceShip ss = ship.GetSS();
            return (ss.weapons != null && ss.weapons.Any(w => IsMiningWeapon(w.wRef)))
                || (ship.attackDronesAE != null && ship.attackDronesAE.Any(IsMiningDrone));
        }

        public static bool IsMiningWeapon(TWeapon weapon)
        {
            return weapon != null && weapon.damageType == DamageType.AsteroidBonus;
        }

        private static bool IsMiningDrone(ActiveEquipment ae)
        {
            return ae is AE_Drone drone && drone.IsMiningDrone;
        }

        /// <summary>Runs before AIMercenary.SearchForAsteroids. Returns false to stop it looking.</summary>
        public static bool AllowsMining(AIMercenary ship)
        {
            if (!Restricted(ship) || HasMiningGear(ship))
            {
                return true;
            }
            // The setting may have been switched on mid-job; let go of the asteroid.
            if (ship.mining && ship.target != null && ship.target.CompareTag("Asteroid"))
            {
                ship.ForgetTarget(checkTargetLastPosition: false);
            }
            ship.mining = false;
            return false;
        }

        /// <summary>
        /// Runs before AIControl.Update, which launches every drone bay at the ship's target. While
        /// mining with the setting on, it recalls combat drones and hands Update a list of the mining
        /// drone bays only. Returns the real list for <see cref="RestoreDrones"/>, or null.
        /// </summary>
        public static List<ActiveEquipment> LimitDrones(AIMercenary ship)
        {
            List<ActiveEquipment> all = ship.attackDronesAE;
            if (all == null || all.Count == 0 || !RestrictedNow(ship))
            {
                return null;
            }
            SpaceShip ss = ship.GetSS();
            miningDrones.Clear();
            foreach (ActiveEquipment ae in all)
            {
                if (IsMiningDrone(ae))
                {
                    miningDrones.Add(ae);
                }
                else if (ss.GetDroneStatus(2, ae))
                {
                    // Out (from a fight, say): call them back.
                    ss.DeployRecallDrones(2, selftarget: false, ae);
                }
            }
            ship.attackDronesAE = miningDrones;
            return all;
        }

        public static void RestoreDrones(AIMercenary ship, List<ActiveEquipment> all)
        {
            ship.attackDronesAE = all;
        }

        /// <summary>
        /// Weapon turrets pick their own targets, including the asteroid the ship is mining. A turret
        /// with a mining laser also goes after nearby asteroids by itself whatever the ship's role,
        /// and fires everything on its mount at them, so this applies to any ship with the setting on.
        /// </summary>
        public static bool TurretMayFire(WeaponTurret turret, TWeapon weapon)
        {
            if (IsMiningWeapon(weapon) || turret.target == null || !turret.target.CompareTag("Asteroid"))
            {
                return true;
            }
            return !(GameAccess.TurretOwner(turret) is AIMercenary ship && ship.aiMercChar != null && IsOn(ship.aiMercChar));
        }

        /// <summary>The panel's toggle was switched on: say so if the ship has nothing to mine with.</summary>
        public static void WarnIfNoGear(AIMercenary ship)
        {
            if (ship != null && ship.aiMercChar.behavior.role == MiningRole && !HasMiningGear(ship))
            {
                FleetOrders.Report(ship, Loc.MsgNoMiningGear, "");
            }
        }
    }
}
