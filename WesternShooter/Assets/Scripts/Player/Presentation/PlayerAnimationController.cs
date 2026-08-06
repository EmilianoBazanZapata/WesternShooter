using UnityEngine;

namespace WesternShooter.Player.Presentation
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int DieTriggerHash = Animator.StringToHash("DieTrigger");

        private Animator characterAnimator;

        public void Configure(Animator resolvedAnimator) => characterAnimator = resolvedAnimator;

        public void SyncGameplayAnimationState(bool isDead, bool isMovePressed, bool isRunPressed, bool isAimPressed, bool isShootPressed)
        {
            if (characterAnimator == null)
                return;

            var isWalking = !isDead && isMovePressed && !isRunPressed && !isAimPressed;
            var isRunning = !isDead && isMovePressed && isRunPressed && !isAimPressed;
            var isAiming = !isDead && isAimPressed;
            var isShooting = !isDead && (isAimPressed || isShootPressed);

            characterAnimator.SetBool(IsWalkingHash, isWalking);
            characterAnimator.SetBool(IsRunningHash, isRunning);
            characterAnimator.SetBool(IsAimingHash, isAiming);
            characterAnimator.SetBool(IsShootingHash, isShooting);
            characterAnimator.SetBool(IsDeadHash, isDead);
        }

        public void PlayDeath()
        {
            if (characterAnimator == null)
                return;

            characterAnimator.SetTrigger(DieTriggerHash);
        }
    }

}

