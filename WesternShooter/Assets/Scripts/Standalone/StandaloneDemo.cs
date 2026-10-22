using UnityEngine;
using UnityEngine.InputSystem;
using WesternShooter.Audio;
using WesternShooter.Player.StateMachine;

namespace WesternShooter.Standalone
{
    // Deliberately separate from the original GameFlow and its unavailable scene wiring.
    public sealed class StandaloneDemo : MonoBehaviour
    {
        public PlayerMovementStateMachine player;
        public GameAudioManager audioManager;
        private bool paused;
        private bool voice;
        private float volume = 0.5f;
        private int hits;

        private void Start()
        {
            audioManager.SetPeacefulSnapshot();
            audioManager.SetVolume(AudioBusType.Master, volume);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
                audioManager.SetNonMusicMuted(paused);
            }
            if (paused) return;
            if (keyboard.digit1Key.wasPressedThisFrame) audioManager.SetPeacefulSnapshot();
            if (keyboard.digit2Key.wasPressedThisFrame) audioManager.SetWantedSnapshot();
            if (keyboard.vKey.wasPressedThisFrame)
            {
                voice = !voice;
                if (voice) audioManager.OnCharacterVoicePlaybackStarted();
                else audioManager.OnCharacterVoicePlaybackEnded();
            }
            if (keyboard.digit3Key.wasPressedThisFrame) audioManager.PlayVictoryMusic();
            if (keyboard.digit4Key.wasPressedThisFrame) audioManager.PlayDefeatMusic();
            if (keyboard.hKey.wasPressedThisFrame) player.TakeDamage(10f);
            if (Mouse.current != null && Mouse.current.rightButton.isPressed && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var camera = UnityEngine.Camera.main;
                if (Physics.Raycast(camera.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out var hit, 100f))
                {
                    var target = hit.collider.GetComponent<StandaloneTarget>();
                    if (target != null && target.Damage()) hits++;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(15, 15, 570, 250), GUI.skin.box);
            GUILayout.Label("Standalone smoke demo - original scenes are unavailable");
            GUILayout.Label("WASD / Shift: move | RMB + LMB: hit cubes | H: damage player");
            GUILayout.Label("1: peaceful | 2: wanted | 3: victory | 4: defeat");
            GUILayout.Label("V: simulated voice request on/off | Esc: pause | F: player death");
            GUILayout.Label($"Health: {player.CurrentHealth} | Targets destroyed: {hits}/3 | Voice request: {voice}");
            GUILayout.Label("Master volume (generated test tones)");
            var next = GUILayout.HorizontalSlider(volume, 0f, 1f);
            if (!Mathf.Approximately(next, volume))
            {
                volume = next;
                audioManager.SetVolume(AudioBusType.Master, volume);
            }
            GUILayout.Label(paused ? "Paused" : "Static camera; targets require four hits each. No AI/NavMesh test.");
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (voice && audioManager != null) audioManager.OnCharacterVoicePlaybackEnded();
        }
    }
}
