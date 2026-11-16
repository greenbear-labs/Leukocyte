using UnityEngine;

namespace Leukocyte
{
    /// <summary>The player's primary weapon. Stats and look come from the current class's WeaponProfile:
    /// automatic rifle, pellet scattergun or zoomable marksman lance. All hitscan.</summary>
    public class EnzymeRifle : MonoBehaviour
    {
        public WeaponProfile Profile { get; private set; }
        public int magazineSize => Profile.magazineSize;
        public string WeaponName => Profile.name;
        public int Ammo { get; private set; }
        public bool Reloading { get; private set; }
        public float ReloadProgress => Reloading ? 1f - reloadTimer / Profile.reloadTime : 1f;
        public bool Zoomed { get; private set; }

        PlayerController owner;
        Camera cam;
        Transform model;
        Transform muzzle;
        Light flash;
        Vector3 modelRest;
        float nextFireTime;
        float reloadTimer;
        float kick;

        public void Init(PlayerController player, Camera camera, WeaponProfile profile)
        {
            owner = player;
            cam = camera;
            SetProfile(profile);
        }

        public void SetProfile(WeaponProfile profile)
        {
            Profile = profile;
            Ammo = profile.magazineSize;
            Reloading = false;
            Zoomed = false;
            if (model) Destroy(model.gameObject);
            BuildModel();
        }

        void BuildModel()
        {
            model = new GameObject("Weapon_" + Profile.name).transform;
            model.SetParent(cam.transform, false);
            modelRest = new Vector3(0.28f, -0.26f, 0.55f);
            model.localPosition = modelRest;

            var s = Profile.modelScale;
            var armor = new Color(0.85f, 0.92f, 1f);
            Greybox.Prim(PrimitiveType.Cube, "Body", model, Vector3.zero, Vector3.Scale(new Vector3(0.12f, 0.14f, 0.5f), s), armor, ColliderMode.None);
            Greybox.Prim(PrimitiveType.Cube, "Barrel", model, new Vector3(0, 0.02f, 0.35f * s.z), Vector3.Scale(new Vector3(0.06f, 0.06f, 0.3f), s), new Color(0.6f, 0.7f, 0.8f), ColliderMode.None);
            Greybox.Prim(PrimitiveType.Cube, "Grip", model, new Vector3(0, -0.12f, -0.1f), new Vector3(0.07f, 0.16f, 0.08f), new Color(0.4f, 0.5f, 0.6f), ColliderMode.None);
            var canister = Greybox.Prim(PrimitiveType.Cylinder, "Canister", model, new Vector3(0, 0.1f * s.y, -0.05f),
                new Vector3(0.08f, 0.12f, 0.08f), Profile.boltColor, ColliderMode.None, 2f);
            canister.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (Profile.zoomFov > 0f)
                Greybox.Prim(PrimitiveType.Cylinder, "Scope", model, new Vector3(0, 0.13f, 0.1f),
                    new Vector3(0.05f, 0.12f, 0.05f), new Color(0.3f, 0.3f, 0.4f), ColliderMode.None)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(model, false);
            muzzle.localPosition = new Vector3(0f, 0.02f, 0.52f * s.z);

            flash = muzzle.gameObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 6f;
            flash.color = Profile.boltColor;
            flash.intensity = 0f;
        }

        /// <summary>Viewmodel jab used by Phagocyte Grab.</summary>
        public void Punch() { kick = 2.5f; }

        public void Refill()
        {
            Ammo = Profile.magazineSize;
            Reloading = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var gm = GameManager.I;
            bool canAct = gm && gm.State == GameState.Playing && !owner.Hp.IsDead && owner.InputEnabled;

            Zoomed = canAct && Profile.zoomFov > 0f && !Reloading && !owner.Sprinting && GameInput.Zoom;

            if (Reloading)
            {
                reloadTimer -= dt;
                if (reloadTimer <= 0f) Refill();
            }
            else if (canAct)
            {
                bool trigger = Profile.automatic ? GameInput.FireHeld : GameInput.FirePressed;
                if (GameInput.Reload && Ammo < Profile.magazineSize) StartReload();
                else if (trigger && Time.time >= nextFireTime)
                {
                    if (Ammo > 0) Fire();
                    else StartReload();
                }
            }

            // Viewmodel: recoil kick, reload dip, sprint tilt, hidden while scoped.
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
            if (Zoomed) target = new Vector3(0f, -0.6f, 0.3f);
            model.localPosition = Vector3.Lerp(model.localPosition, target, 20f * dt);
            model.localRotation = Quaternion.Slerp(model.localRotation, rot, 15f * dt);

            flash.intensity = Mathf.MoveTowards(flash.intensity, 0f, 80f * dt);
        }

        void StartReload()
        {
            Reloading = true;
            reloadTimer = Profile.reloadTime;
        }

        void Fire()
        {
            nextFireTime = Time.time + 1f / Profile.fireRate;
            Ammo--;
            owner.MarkEngaged();

            Vector3 origin = cam.transform.position;
            float spread = Zoomed ? 0f : Profile.spreadDegrees * (owner.Sprinting ? 2f : 1f);
            bool anyHit = false, anyKill = false;

            for (int p = 0; p < Profile.pellets; p++)
            {
                var jitter = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f);
                Vector3 dir = cam.transform.rotation * jitter * Vector3.forward;
                Vector3 end = origin + dir * Profile.range;

                if (Physics.Raycast(origin, dir, out var hit, Profile.range, ~0, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    var victim = hit.collider.GetComponentInParent<Combatant>();
                    if (victim && victim.team != Team.Immune && victim.Alive)
                    {
                        float dmg = Profile.damage * victim.DamageTakenMultiplier;
                        bool kill = victim.Hp.Current <= dmg;
                        victim.Hp.Damage(dmg, origin);
                        if (kill) KillFeed.PlayerKill(victim, "Destroyed");
                        anyHit = true;
                        anyKill |= kill;
                        var germ = victim.GetComponent<Germ>();
                        Fader.Burst(hit.point, germ ? germ.BloodColor : Color.white, 2, 0.12f, 3f);
                    }
                    else
                    {
                        Fader.Burst(hit.point, Profile.boltColor, 1, 0.08f, 2f);
                    }
                }

                if (p < 4) Fader.Tracer(muzzle.position, end, Profile.boltColor);
            }

            var gm = GameManager.I;
            if (anyHit)
            {
                if (gm && gm.Hud) gm.Hud.ShowHitMarker(anyKill);
                Sfx.UI(() => anyKill ? Sfx.Squelch : Sfx.HitTick, anyKill ? 0.8f : 0.35f);
            }

            float loud = Profile.pellets > 1 ? 0.6f : Profile.damage > 30f ? 0.55f : 0.35f;
            float pitch = Profile.pellets > 1 ? 0.6f : Profile.damage > 30f ? 0.8f : 1f;
            Sfx.UI(() => Sfx.Shot, loud, 0.05f, pitch);
            flash.intensity = 4f;
            kick = Profile.pellets > 1 ? 2f : 1f;
            owner.AddRecoil(Profile.recoilDegrees * (Zoomed ? 0.5f : 1f));
            if (Profile.pellets > 1 || Profile.damage > 30f) owner.AddShake(0.12f);
        }
    }
}
