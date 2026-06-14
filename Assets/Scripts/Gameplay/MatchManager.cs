using System;
using System.Collections.Generic;
using UnityEngine;
using Hockey.Core;

namespace Hockey.Gameplay
{
    /// <summary>
    /// Runtime hub for a match: owns the config, puck, roster, score, clock and phase. Created by the
    /// bootstrap; other systems find it via <see cref="Instance"/>. Also handles free-puck capture and
    /// goal/period flow at a level the AI and UI can read from.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public RinkLayout Rink { get; private set; }
        public PuckController Puck { get; set; }

        public readonly List<SkaterController> Skaters = new List<SkaterController>();

        public readonly int[] Score = new int[2];
        public int Period = 1;
        public float TimeRemaining;
        public MatchPhase Phase = MatchPhase.PreGame;
        public Difficulty Difficulty => Config != null ? Config.difficulty : Difficulty.Pro;

        public event Action<TeamSide> GoalScored;
        public event Action PhaseChanged;

        float _faceoffAt;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Init(GameConfig config, RinkLayout rink)
        {
            Instance = this;
            Config = config;
            Rink = rink;
            TimeRemaining = config.periodSeconds;
            Period = 1;
            Array.Clear(Score, 0, Score.Length);
            SetPhase(MatchPhase.Faceoff);
            _faceoffAt = Time.time + 1.0f;
        }

        public void Register(SkaterController s)
        {
            if (s != null && !Skaters.Contains(s)) Skaters.Add(s);
        }

        void SetPhase(MatchPhase p)
        {
            Phase = p;
            PhaseChanged?.Invoke();
        }

        void Update()
        {
            if (Config == null) return;

            if (Phase == MatchPhase.Faceoff && Time.time >= _faceoffAt)
                SetPhase(MatchPhase.Play);

            if (Phase == MatchPhase.Play)
            {
                TimeRemaining -= Time.deltaTime;
                if (TimeRemaining <= 0f) { TimeRemaining = 0f; EndPeriod(); return; }
                TryCapturePuck();
            }
        }

        void TryCapturePuck()
        {
            if (Puck == null || !Puck.CanBeCaptured) return;
            SkaterController best = null;
            float bestD = Config.puck.maxControlDistance;
            Vector3 pp = Puck.transform.position;
            foreach (var s in Skaters)
            {
                if (s == null || s.IsStunned) continue;
                float d = Vector3.Distance(s.StickTip, pp);
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best != null) Puck.SetCarrier(best);
        }

        public void OnGoal(TeamSide scoringSide)
        {
            if (Phase != MatchPhase.Play) return;
            Score[(int)scoringSide]++;
            GoalScored?.Invoke(scoringSide);
            SetPhase(MatchPhase.Faceoff);
            _faceoffAt = Time.time + 2.0f;
            ResetForFaceoff();
        }

        void EndPeriod()
        {
            if (Period >= Config.periods) { SetPhase(MatchPhase.GameOver); return; }
            Period++;
            TimeRemaining = Config.periodSeconds;
            SetPhase(MatchPhase.Faceoff);
            _faceoffAt = Time.time + 2.0f;
            ResetForFaceoff();
        }

        public void ResetForFaceoff()
        {
            if (Puck != null)
            {
                Puck.SetCarrier(null);
                var rb = Puck.Body != null ? Puck.Body : Puck.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                Puck.transform.position = Rink.CenterFaceoff + Vector3.up * PuckController.RestHeight;
            }
            FormationUtil.PlaceForFaceoff(this);
        }

        public void SetDifficulty(Difficulty d)
        {
            if (Config != null) Config.difficulty = d;
        }
    }
}
