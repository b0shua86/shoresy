using UnityEngine;
using Hockey.Core;
using Hockey.Gameplay;

namespace Hockey.UI
{
    /// <summary>
    /// Minimal broadcast-style scorebug using IMGUI (no UI assets needed yet): score, period, clock,
    /// difficulty, a controls hint, and FACE-OFF / GOAL! / result stings. The polished uGUI/TMP
    /// scorebug, animated GOAL sting and replays arrive in the presentation milestone.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        GUIStyle _bug, _big, _small;
        float _goalFlashUntil;
        TeamSide _lastScorer;

        void Start()
        {
            var mm = MatchManager.Instance;
            if (mm != null)
                mm.GoalScored += s => { _goalFlashUntil = Time.unscaledTime + 2.2f; _lastScorer = s; };
        }

        void EnsureStyles()
        {
            if (_bug != null) return;
            _bug = new GUIStyle(GUI.skin.box) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _big = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.UpperLeft };
        }

        void OnGUI()
        {
            var mm = MatchManager.Instance;
            if (mm == null || mm.Config == null) return;
            EnsureStyles();

            float w = 380f, h = 50f, x = (Screen.width - w) * 0.5f;
            GUI.Box(new Rect(x, 8f, w, h), "");
            int mins = Mathf.FloorToInt(Mathf.Max(0f, mm.TimeRemaining) / 60f);
            int secs = Mathf.FloorToInt(Mathf.Max(0f, mm.TimeRemaining) % 60f);
            GUI.Label(new Rect(x, 14f, w, h - 12f),
                $"HOME  {mm.Score[(int)TeamSide.Home]}    |    P{mm.Period}   {mins:00}:{secs:00}    |    {mm.Score[(int)TeamSide.Away]}  AWAY",
                _bug);

            GUI.Label(new Rect(12f, 8f, 460f, 120f),
                $"Difficulty: {mm.Config.difficulty}   (Esc / Start = pause + difficulty)\n" +
                "Move WASD / L-Stick · Sprint Shift · Shoot Space · Pass E · Switch Tab",
                _small);

            if (Time.unscaledTime < _goalFlashUntil)
            {
                var prev = GUI.color;
                GUI.color = _lastScorer == TeamSide.Home ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.45f, 0.45f);
                GUI.Label(new Rect(0f, Screen.height * 0.30f, Screen.width, 100f), "GOAL!", _big);
                GUI.color = prev;
            }

            if (mm.Phase == MatchPhase.Faceoff)
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 60f), "FACE-OFF", _big);

            if (mm.Phase == MatchPhase.GameOver)
            {
                string result = mm.Score[0] == mm.Score[1] ? "TIE GAME"
                    : (mm.Score[0] > mm.Score[1] ? "HOME WINS" : "AWAY WINS");
                GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, 100f), result, _big);
            }
        }
    }
}
