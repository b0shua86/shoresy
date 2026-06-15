using UnityEngine;
using UnityEngine.Rendering;
using Hockey.Core;
using Hockey.Gameplay;
using Hockey.AI;
using Hockey.Presentation;
using Hockey.UI;

namespace Hockey.Boot
{
    /// <summary>
    /// Builds a fully playable match from code on entering play mode, so no manual scene wiring is
    /// needed: lighting, rink, puck, goals, two teams (1 human + AI), camera, HUD and pause menu.
    /// Idempotent — runs once. The actual front-end menus replace this auto-boot later.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (MatchManager.Instance != null) return;
            if (Object.FindFirstObjectByType<MatchManager>() != null) return;
            BuildWorld(GameConfig.CreateDefault());
        }

        public static MatchManager BuildWorld(GameConfig config)
        {
            var root = new GameObject("Game");

            // Lighting
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root.transform);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(55f, 30f, 0f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);

            // Rink
            RinkLayout rink = RinkBuilder.Build(root.transform);

            // Match hub
            var mm = new GameObject("MatchManager").AddComponent<MatchManager>();
            mm.transform.SetParent(root.transform);

            // Puck
            mm.Puck = CreatePuck(root.transform, config.puck);

            // Goals (visual + scoring trigger) at both ends
            CreateGoal(root.transform, rink, TeamSide.Home);
            CreateGoal(root.transform, rink, TeamSide.Away);

            // Teams
            SpawnTeam(root.transform, mm, config, TeamSide.Home);
            SpawnTeam(root.transform, mm, config, TeamSide.Away);

            // Start the match (also places everyone for the opening faceoff)
            mm.Init(config, rink);

            // Human controls the Home centre to start
            var human = mm.Skaters.Find(s => s.Team == TeamSide.Home && s.Role == PlayerRole.Center);
            var driver = new GameObject("PlayerInput").AddComponent<PlayerInputDriver>();
            driver.transform.SetParent(root.transform);
            driver.Controlled = human;

            // Camera
            var camGO = new GameObject("BroadcastCamera") { tag = "MainCamera" };
            camGO.transform.SetParent(root.transform);
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<BroadcastCamera>();
            camGO.transform.position = new Vector3(0f, 13f, -18f);

            // HUD + pause/difficulty menu
            var ui = new GameObject("UI");
            ui.transform.SetParent(root.transform);
            ui.AddComponent<HUD>();
            ui.AddComponent<PauseMenu>();

            return mm;
        }

        static PuckController CreatePuck(Transform parent, PuckTuning tuning)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Puck";
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(0.23f, 0.025f, 0.23f);
            go.transform.position = new Vector3(0f, PuckController.RestHeight, 0f);
            go.GetComponent<Renderer>().sharedMaterial = SolidMat(new Color(0.05f, 0.05f, 0.05f));
            go.AddComponent<Rigidbody>();
            var puck = go.AddComponent<PuckController>();
            puck.Configure(tuning);
            return puck;
        }

        static void SpawnTeam(Transform parent, MatchManager mm, GameConfig config, TeamSide side)
        {
            // Fallback capsule colours (used until the generated models are bound): royal blue / orange.
            Color col = side == TeamSide.Home ? new Color(0.10f, 0.22f, 0.65f) : new Color(0.85f, 0.35f, 0.06f);
            for (int i = 0; i < config.playersPerSide; i++)
                SpawnSkater(parent, mm, config, side, FormationUtil.RoleForIndex(i, config.playersPerSide), col, 10 + i, i);
            if (config.useGoalies)
                SpawnSkater(parent, mm, config, side, PlayerRole.Goalie, col, 1, 0);
        }

        static void SpawnSkater(Transform parent, MatchManager mm, GameConfig config, TeamSide side, PlayerRole role, Color col, int number, int index)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{side}_{role}_{number}";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, SkaterController.StandHeight, 0f);
            go.GetComponent<Renderer>().sharedMaterial = SolidMat(col);
            go.AddComponent<Rigidbody>();

            var skater = go.AddComponent<SkaterController>();
            skater.Team = side; skater.Role = role; skater.JerseyNumber = number;

            var skill = side == TeamSide.Home
                ? AISkillProfile.ForTeammate(config.difficulty)
                : AISkillProfile.ForOpponent(config.difficulty);
            skater.Configure(config.skating, skill.speedScale);

            go.AddComponent<AIController>().Skill = skill;
            mm.Register(skater);

            // Bind the generated character model (no-op fallback to the capsule if it isn't built yet).
            go.AddComponent<SkaterVisual>().resourceName = CharacterRoster.Resolve(side, role, index);

            // small white nose so the capsule's facing is readable
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            nose.transform.SetParent(go.transform);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            nose.transform.localPosition = new Vector3(0f, 0.2f, 0.5f);
            Object.Destroy(nose.GetComponent<Collider>());
            nose.GetComponent<Renderer>().sharedMaterial = SolidMat(Color.white);
        }

        static void CreateGoal(Transform parent, RinkLayout rink, TeamSide defending)
        {
            Vector3 center = rink.OwnGoalCenter(defending);
            float dir = defending.AttackDirZ(); // net opens toward centre ice

            var goalRoot = new GameObject($"Goal_{defending}");
            goalRoot.transform.SetParent(parent);
            goalRoot.transform.position = center;

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "NetFrame";
            frame.transform.SetParent(goalRoot.transform);
            frame.transform.localScale = new Vector3(1.8f, 1.2f, 0.1f);
            frame.transform.localPosition = new Vector3(0f, 0.6f, -dir * 0.5f);
            frame.GetComponent<Renderer>().sharedMaterial = SolidMat(new Color(0.85f, 0.2f, 0.2f));
            Object.Destroy(frame.GetComponent<Collider>());

            var trig = new GameObject("GoalTrigger");
            trig.transform.SetParent(goalRoot.transform);
            trig.transform.localPosition = new Vector3(0f, 0.6f, -dir * 0.3f);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.6f, 1.1f, 0.5f);
            trig.AddComponent<GoalTrigger>().DefendingSide = defending;
        }

        static Material SolidMat(Color c)
        {
            // Pick the shader matching the ACTIVE pipeline; URP/Lit renders magenta under built-in.
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader s = srp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            return new Material(s) { color = c };
        }
    }
}
