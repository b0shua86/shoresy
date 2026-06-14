using UnityEngine;
using Hockey.Core;

namespace Hockey.Gameplay
{
    /// <summary>Dimensions and key positions of the rink in world space (centre ice at the origin).</summary>
    public struct RinkLayout
    {
        public float length;   // along Z
        public float width;    // along X
        public float goalLineZ;          // |Z| of each goal line
        public Vector3 HomeGoalCenter;   // net the Home team defends (-Z end)
        public Vector3 AwayGoalCenter;   // net the Away team defends (+Z end)
        public Vector3 CenterFaceoff;

        public Vector3 OwnGoalCenter(TeamSide side) => side == TeamSide.Home ? HomeGoalCenter : AwayGoalCenter;
        public Vector3 AttackGoalCenter(TeamSide side) => side == TeamSide.Home ? AwayGoalCenter : HomeGoalCenter;
    }

    /// <summary>Builds a simple rectangular rink (ice + boards) from primitives at runtime.</summary>
    public static class RinkBuilder
    {
        public static RinkLayout Build(Transform parent, float length = 58f, float width = 26f)
        {
            var layout = new RinkLayout
            {
                length = length,
                width = width,
                goalLineZ = length * 0.5f - 4f,
                CenterFaceoff = Vector3.zero,
            };
            layout.HomeGoalCenter = new Vector3(0f, 0f, -layout.goalLineZ);
            layout.AwayGoalCenter = new Vector3(0f, 0f, layout.goalLineZ);

            // Ice surface (a thin slab with a slick physics material).
            var ice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ice.name = "Ice";
            ice.transform.SetParent(parent, false);
            ice.transform.localScale = new Vector3(width, 0.2f, length);
            ice.transform.position = new Vector3(0f, -0.1f, 0f);
            ice.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.92f, 0.95f, 1f));
            var slick = new PhysicsMaterial("Ice")
            {
                dynamicFriction = 0.05f,
                staticFriction = 0.05f,
                bounciness = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
            };
            ice.GetComponent<Collider>().material = slick;

            // Perimeter boards.
            const float h = 1.2f, t = 0.3f;
            var boardCol = new Color(0.85f, 0.88f, 0.92f);
            CreateBoard(parent, "Board+X", new Vector3(width * 0.5f + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, length), boardCol);
            CreateBoard(parent, "Board-X", new Vector3(-width * 0.5f - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, length), boardCol);
            CreateBoard(parent, "Board+Z", new Vector3(0f, h * 0.5f, length * 0.5f + t * 0.5f), new Vector3(width + t * 2f, h, t), boardCol);
            CreateBoard(parent, "Board-Z", new Vector3(0f, h * 0.5f, -length * 0.5f - t * 0.5f), new Vector3(width + t * 2f, h, t), boardCol);

            return layout;
        }

        static void CreateBoard(Transform parent, string name, Vector3 pos, Vector3 scale, Color col)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.SetParent(parent, false);
            b.transform.position = pos;
            b.transform.localScale = scale;
            b.GetComponent<Renderer>().sharedMaterial = MakeMat(col);
        }

        static Material MakeMat(Color c)
        {
            // URP/Lit if URP is active, otherwise the built-in Standard shader.
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return new Material(s) { color = c };
        }
    }
}
