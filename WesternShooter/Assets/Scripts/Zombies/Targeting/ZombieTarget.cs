using UnityEngine;
using WesternShooter.Player.StateMachine;

namespace WesternShooter.Zombies.Targeting
{
    public class ZombieTarget
    {
        private Transform target;

        public ZombieTarget(Transform initialTarget)
        {
            target = initialTarget;
        }

        public bool HasTarget => target != null;
        public Transform Target => target;
        public Vector3 Position => target != null ? target.position : Vector3.zero;

        public void SetTarget(Transform newTarget) => target = newTarget;

        public void ClearTarget() => target = null;

        public float DistanceFrom(Vector3 origin)
        {
            if (target == null)
                return float.PositiveInfinity;

            return Vector3.Distance(origin, target.position);
        }

        public bool TryResolveFallbackTarget()
        {
            if (target != null)
                return true;

            var player = Object.FindFirstObjectByType<PlayerMovementStateMachine>();
            if (player == null)
                return false;

            target = player.transform;
            return true;
        }
    }
}

