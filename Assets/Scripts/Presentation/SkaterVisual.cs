using UnityEngine;
using Hockey.Gameplay;

namespace Hockey.Presentation
{
    /// <summary>
    /// Attaches a generated character model to a skater: loads a prefab from
    /// Resources/Characters/&lt;resourceName&gt;, parents it under the skater, hides the placeholder
    /// capsule (keeping its collider for physics) and drives the model's Animator locomotion from the
    /// skater's speed. If no model prefab exists yet it does nothing, so the capsule remains and the
    /// game still runs — build the prefabs via the "Shoresy ▸ Build Character Prefabs &amp; Anims" menu.
    /// </summary>
    [RequireComponent(typeof(SkaterController))]
    public class SkaterVisual : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Tooltip("Prefab name under Resources/Characters (no extension).")]
        public string resourceName;

        [Tooltip("Yaw offset if the model faces the wrong way (try 180).")]
        public float modelYaw = 0f;

        SkaterController _skater;
        Animator _animator;

        void Start()
        {
            _skater = GetComponent<SkaterController>();

            GameObject prefab = string.IsNullOrEmpty(resourceName)
                ? null
                : Resources.Load<GameObject>("Characters/" + resourceName);
            if (prefab == null) return; // keep the capsule as a fallback

            var model = Instantiate(prefab, transform);
            model.transform.localPosition = new Vector3(0f, -SkaterController.StandHeight, 0f);
            model.transform.localRotation = Quaternion.Euler(0f, modelYaw, 0f);

            // Hide the capsule mesh + the facing nose; the CapsuleCollider stays for physics.
            var capsule = GetComponent<MeshRenderer>();
            if (capsule != null) capsule.enabled = false;
            var nose = transform.Find("Facing");
            if (nose != null) nose.gameObject.SetActive(false);

            _animator = model.GetComponentInChildren<Animator>();
            if (_animator != null) _animator.applyRootMotion = false;
        }

        void Update()
        {
            if (_animator == null) return;
            float speed = _skater.PlanarVelocity.magnitude;
            _animator.SetFloat(SpeedHash, speed);
            // No idle clip yet: freeze the locomotion pose when essentially stopped.
            _animator.speed = speed < 0.2f ? 0f : 1f;
        }
    }
}
