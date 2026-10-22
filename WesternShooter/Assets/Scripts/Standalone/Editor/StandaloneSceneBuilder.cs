using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using WesternShooter.Audio;
using WesternShooter.Player.Input;
using WesternShooter.Player.Movement;
using WesternShooter.Player.Presentation;
using WesternShooter.Player.StateMachine;

namespace WesternShooter.Standalone.Editor
{
    public static class StandaloneSceneBuilder
    {
        private const string Root = "Assets/StandaloneGenerated";
        private const string ScenePath = Root + "/StandaloneDemo.unity";

        [MenuItem("WesternShooter/Create standalone smoke demo")]
        public static void Create()
        {
            if (Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                Generate();
        }

        // Batch entry point: -executeMethod WesternShooter.Standalone.Editor.StandaloneSceneBuilder.Generate
        public static void Generate()
        {
            // Never silently overwrite an authored scene or its generated resources.
            if (Directory.Exists(Root)) throw new InvalidOperationException("Move or delete " + Root + " before regenerating.");
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/Documentation/GameAudioMixer.mixer");
            if (mixer == null) throw new InvalidOperationException("Tracked GameAudioMixer is required.");
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Camera").AddComponent<UnityEngine.Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 9, -12);
            camera.transform.LookAt(new Vector3(0, 0, 5));
            camera.gameObject.AddComponent<AudioListener>();
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            Box("Ground", new Vector3(0, -0.5f, 3), new Vector3(30, 1, 30));
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Primitive player";
            player.transform.position = new Vector3(0, 1, 0);
            UnityEngine.Object.DestroyImmediate(player.GetComponent<Collider>());
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerAnimationController>();
            player.AddComponent<PlayerFootstepAudio>();
            var movement = player.AddComponent<PlayerMovementStateMachine>();
            for (int i = 0; i < 3; i++)
                Box("Target", new Vector3((i - 1) * 3, 1, 7), Vector3.one * 2).AddComponent<StandaloneTarget>();
            var audioObject = new GameObject("Existing audio system");
            var manager = audioObject.AddComponent<GameAudioManager>();
            var config = new SerializedObject(manager);
            config.FindProperty("audioMixer").objectReferenceValue = mixer;
            string[] fields = { "peacefulSnapshot", "wantedSnapshot", "characterVoiceSnapshot" };
            string[] snapshots = { "PeacefulTown", "Wanted", "CharacterVoice" };
            for (int i = 0; i < fields.Length; i++)
            {
                var snapshot = mixer.FindSnapshot(snapshots[i]);
                if (snapshot == null) throw new InvalidOperationException("Missing snapshot: " + snapshots[i]);
                config.FindProperty(fields[i]).objectReferenceValue = snapshot;
            }
            var groups = mixer.FindMatchingGroups("Music");
            if (groups.Length == 0) throw new InvalidOperationException("Missing Music mixer group.");
            string[] sources = { "peacefulMusicSource", "wantedMusicSource", "victoryMusicSource", "defeatMusicSource" };
            for (int i = 0; i < sources.Length; i++)
            {
                var path = Root + "/Tone" + i + ".wav";
                WriteTone(path, 220 + i * 110);
                AssetDatabase.ImportAsset(path);
                var source = audioObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = i < 2;
                source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                source.outputAudioMixerGroup = groups[0];
                config.FindProperty(sources[i]).objectReferenceValue = source;
            }
            config.FindProperty("applyPeacefulOnGameplaySceneLoad").boolValue = false;
            config.ApplyModifiedPropertiesWithoutUndo();
            var demo = new GameObject("Smoke demo controls").AddComponent<StandaloneDemo>();
            demo.player = movement;
            demo.audioManager = manager;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Standalone scene generated. Play Mode and build validation are still required.");
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            return box;
        }

        private static void WriteTone(string path, int frequency)
        {
            const int rate = 44100;
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + rate * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(rate * 2);
                for (int i = 0; i < rate; i++)
                {
                    var envelope = Math.Min(1.0, Math.Min(i / 441.0, (rate - 1 - i) / 441.0));
                    writer.Write((short)(Math.Sin(2 * Math.PI * frequency * i / rate) * 2000 * envelope));
                }
            }
        }
    }
}
