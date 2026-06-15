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
                "Move WASD · Sprint Shift · Shoot Space · Pass E · Check F · Drop gloves G · Switch Tab",
                _small);

            // Energy bar for the skater you're controlling.
            var human = mm.Skaters.Find(s => s != null && s.IsHumanControlled);
            if (human != null)
            {
                float bw = 200f, bh = 14f, bx = 16f, by = Screen.height - 28f;
                var prevC = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.5f);
                GUI.DrawTexture(new Rect(bx - 2f, by - 2f, bw + 4f, bh + 4f), Texture2D.whiteTexture);
                GUI.color = Color.Lerp(new Color(0.85f, 0.2f, 0.15f), new Color(0.3f, 0.85f, 0.3f), human.Stamina);
                GUI.DrawTexture(new Rect(bx, by, bw * Mathf.Clamp01(human.Stamina), bh), Texture2D.whiteTexture);
                GUI.color = prevC;
                GUI.Label(new Rect(bx, by - 17f, bw, 16f), "ENERGY", _small);
            }

            // Drop-the-gloves brawl overlay.
            var fight = mm.ActiveFight;
            if (mm.Phase == MatchPhase.Fight && fight != null)
            {
                float fw = 440f, fx = (Screen.width - fw) * 0.5f, fy = Screen.height * 0.40f;
                GUI.Box(new Rect(fx - 12f, fy - 30f, fw + 24f, 170f), "FIGHT!");
                GUI.Label(new Rect(fx, fy - 6f, 200f, 18f), fight.NameA, _small);
                GUI.Label(new Rect(fx + fw - 200f, fy - 6f, 200f, 18f), fight.NameB, _small);
                DrawBar(fx, fy + 14f, 200f, 16f, fight.HpA, new Color(0.85f, 0.2f, 0.2f));
                DrawBar(fx, fy + 34f, 200f, 9f, fight.StA, new Color(0.9f, 0.8f, 0.2f));
                DrawBar(fx + fw - 200f, fy + 14f, 200f, 16f, fight.HpB, new Color(0.85f, 0.2f, 0.2f));
                DrawBar(fx + fw - 200f, fy + 34f, 200f, 9f, fight.StB, new Color(0.9f, 0.8f, 0.2f));
                GUI.Label(new Rect(fx, fy + 58f, fw, 26f), fight.Message, _bug);
                GUI.Label(new Rect(fx, fy + 96f, fw, 36f), "Jab Space · Haymaker F · Block hold Shift · Dodge Tab", _small);
            }

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

        static void DrawBar(float x, float y, float w, float h, float t, Color c)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(t), h), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
