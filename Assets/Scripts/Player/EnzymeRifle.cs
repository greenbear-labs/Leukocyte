using UnityEngine;

namespace Leukocyte
{
    /// <summary>Hitscan primary weapon: fires enzyme bolts with ammo, reload, spread and recoil.</summary>
    public class EnzymeRifle : MonoBehaviour
    {
        public float damage = 12f;
        public float fireRate = 10f;
        public float range = 200f;
        public float spreadDegrees = 0.6f;
        public float recoilDegrees = 0.35f;
        public int magazineSize = 30;
        public float reloadTime = 1.6f;

        public int Ammo { get; private set; }
        public bool Reloading { get; private set; }
        public float ReloadProgress => Reloading ? 1f - reloadTimer / reloadTime : 1f;

        static readonly Color BoltColor = new Color(0.4f, 0.95f, 1f);

        PlayerController owner;
        Camera cam;
        Transform model;
        Transform muzzle;
        Light flash;
        Vector3 modelRest;
        float nextFireTime;
        float reloadTimer;
        float kick;

        public void Init(PlayerController player, Camera camera)
        {
            owner = player;
            cam = camera;
            Ammo = magazineSize;
            BuildModel();
        }

        void BuildModel()
        {
            model = new GameObject("EnzymeRifle").transform;
            model.SetParent(cam.transform, false);
            modelRest = new Vector3(0.28f, -0.26f, 0.55f);
            model.localPosition = modelRest;

            var armor = new Color(0.85f, 0.92f, 1f);
            Greybox.Prim(PrimitiveType.Cube, "Body", model, new Vector3(0, 0, 0), new Vector3(0.12f, 0.14f, 0.5f), armor, ColliderMode.None);
            Greybox.Prim(PrimitiveType.Cube, "Barrel", model, new Vector3(0, 0.02f, 0.35f), new Vector3(0.06f, 0.06f, 0.3f), new Color(0.6f, 0.7f, 0.8f), ColliderMode.None);
            Greybox.Prim(PrimitiveType.Cube, "Grip", model, new Vector3(0, -0.12f, -0.1f), new Vector3(0.07f, 0.16f, 0.08f), new Color(0.4f, 0.5f, 0.6f), ColliderMode.None);
            var canister = Greybox.Prim(PrimitiveType.Cylinder, "EnzymeCanister", model, new Vector3(0, 0.1f, -0.05f),
                new Vector3(0.08f, 0.12f, 0.08f), BoltColor, ColliderMode.None, 2f);
            canister.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(model, false);
            muzzle.localPosition = new Vector3(0f, 0.02f, 0.52f);

            flash = muzzle.gameObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 6f;
            flash.color = BoltColor;
            flash.intensity = 0f;
        }

        public void Refill()
        {
            Ammo = magazineSize;
            Reloading = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var gm = GameManager.I;
            bool canAct = gm && gm.State == GameState.Playing && !owner.Hp.IsDead && owner.InputEnabled;

            if (Reloading)
            {
                reloadTimer -= dt;
                if (reloadTimer <= 0f) Refill();
            }
            else if (canAct)
            {
                if (Input.GetKeyDown(KeyCode.R) && Ammo < magazineSize) StartReload();
                else if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
                {
                    if (Ammo > 0) Fire();
                    else StartReload();
                }
            }

            // Viewmodel: recoil kick, reload dip and sprint tilt.
            kick = Mathf.MoveTowards(kick, 0f, 10f * dt);
            Vector3 target = modelRest + new Vector3(0f, 0f, -0.06f * kick);
            Quaternion rot = Quaternion.Euler(-6f * kick, 0f, 0f);
            if (Reloading)
            {
                target += new Vector3(0f, -0.15f * Mathf.Sin(ReloadProgress * Mathf.PI), 0f);
                rot = Quaternion.Euler(25f * Mathf.Sin(ReloadProgress * Mathf.PI), 0f, 0f);
            }
            else if (owner.Sprinting)
            {
                rot = Quaternion.Euler(10f, -25f, 10f);
            }
            model.localPosition = Vector3.Lerp(model.localPosition, target, 20f * dt);
            model.localRotation = Quaternion.Slerp(model.localRotation, rot, 15f * dt);

            flash.intensity = Mathf.MoveTowards(flash.intensity, 0f, 80f * dt);
        }

        void StartReload()
        {
            Reloading = true;
            reloadTimer = reloadTime;
        }

        void Fire()
        {
            nextFireTime = Time.time + 1f / fireRate;
            Ammo--;

            var spread = Quaternion.Euler(Random.Range(-spreadDegrees, spreadDegrees), Random.Range(-spreadDegrees, spreadDegrees), 0f);
            Vector3 origin = cam.transform.position;
            Vector3 dir = cam.transform.rotation * spread * Vector3.forward;
            Vector3 end = origin + dir * range;

            if (Physics.Raycast(origin, dir, out var hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var victim = hit.collider.GetComponentInParent<Combatant>();
                if (victim && victim.team != Team.Immune && victim.Alive)
                {
                    bool kill = victim.Hp.Current <= damage;
                    victim.Hp.Damage(damage);
                    var gm = GameManager.I;
                    if (gm)
                    {
                        if (kill) gm.Kills++;
                        if (gm.Hud) gm.Hud.ShowHitMarker(kill);
                    }
                    var germ = victim.GetComponent<Germ>();
                    Fader.Burst(hit.point, germ ? germ.BloodColor : Color.white, 2, 0.12f, 3f);
                }
                else
                {
                    Fader.Burst(hit.point, BoltColor, 2, 0.08f, 2f);
                }
            }

            Fader.Tracer(muzzle.position, end, BoltColor);
            flash.intensity = 4f;
            kick = 1f;
            owner.AddRecoil(recoilDegrees);
        }
    }
}
