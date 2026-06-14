using UnityEngine;
using UnityEngine.InputSystem;
using Hockey.Core;
using Hockey.Gameplay;

namespace Hockey.UI
{
    /// <summary>
    /// In-game pause menu (Esc / Start). Lets you change difficulty live — AI controllers re-read the
    /// match difficulty automatically — fulfilling "adjust skill from a menu during gameplay".
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        bool _paused;
        GUIStyle _title, _btn;

        void Update()
        {
            bool toggle = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                       || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (toggle) SetPaused(!_paused);
        }

        void SetPaused(bool p)
        {
            _paused = p;
            Time.timeScale = p ? 0f : 1f;
        }

        void OnGUI()
        {
            if (!_paused) return;
            var mm = MatchManager.Instance;
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _btn = new GUIStyle(GUI.skin.button) { fontSize = 19 };
            }

            float w = 360f, h = 430f, x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;
            GUI.Box(new Rect(x, y, w, h), "");
            GUI.Label(new Rect(x, y + 16f, w, 50f), "PAUSED", _title);
            GUI.Label(new Rect(x + 24f, y + 78f, w - 48f, 24f), "Difficulty");

            string[] names = System.Enum.GetNames(typeof(Difficulty));
            for (int i = 0; i < names.Length; i++)
            {
                var d = (Difficulty)i;
                bool current = mm != null && mm.Config != null && mm.Config.difficulty == d;
                var prev = GUI.color;
                if (current) GUI.color = Color.cyan;
                if (GUI.Button(new Rect(x + 24f, y + 106f + i * 52f, w - 48f, 44f), names[i], _btn) && mm != null)
                    mm.SetDifficulty(d);
                GUI.color = prev;
            }

            if (GUI.Button(new Rect(x + 24f, y + 106f + names.Length * 52f + 10f, w - 48f, 48f), "Resume", _btn))
                SetPaused(false);
        }
    }
}
