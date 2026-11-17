using UnityEngine;

namespace Leukocyte
{
    public enum TrooperClass { Neutrophil, Macrophage, TCell }

    public enum Ability { CytokineBurst, AntibodyTag }

    /// <summary>Weapon stats for a class's primary.</summary>
    public class WeaponProfile
    {
        public string name;
        public float damage;
        public int pellets = 1;
        public float fireRate;
        public bool automatic = true;
        public int magazineSize;
        public float reloadTime;
        public float spreadDegrees;
        public float recoilDegrees;
        public float zoomFov;          // 0 = no zoom
        public Color boltColor;
        public float range = 220f;
        public Vector3 modelScale = Vector3.one;
    }

    /// <summary>Per-class stats. Classes follow real white blood cell roles.</summary>
    public class ClassProfile
    {
        public TrooperClass id;
        public string title;
        public string role;
        public string blurb;
        public float maxHealth;
        public float speedMultiplier;
        public Ability ability;
        public int abilityCharges;
        public float abilityRecharge;
        public float grabHeal;
        public float engulfThreshold;
        public Color armor;
        public WeaponProfile weapon;

        /// <summary>The class the player picked on the menu or respawn screen. Persists between matches.</summary>
        public static TrooperClass Selected = TrooperClass.Neutrophil;

        public static ClassProfile Get(TrooperClass c) => c == TrooperClass.Macrophage ? Macrophage : c == TrooperClass.TCell ? TCell : Neutrophil;

        public static readonly ClassProfile Neutrophil = new ClassProfile
        {
            id = TrooperClass.Neutrophil,
            title = "NEUTROPHIL",
            role = "Assault",
            blurb = "Fast first responder. Enzyme Rifle, 3 Cytokine Bursts.",
            maxHealth = 100f,
            speedMultiplier = 1.05f,
            ability = Ability.CytokineBurst,
            abilityCharges = 3,
            abilityRecharge = 10f,
            grabHeal = 30f,
            engulfThreshold = 0.4f,
            armor = new Color(0.85f, 0.92f, 1f),
            weapon = new WeaponProfile
            {
                name = "ENZYME RIFLE",
                damage = 12f, fireRate = 10f, automatic = true, magazineSize = 30, reloadTime = 1.6f,
                spreadDegrees = 0.6f, recoilDegrees = 0.35f, boltColor = new Color(0.4f, 0.95f, 1f),
            },
        };

        public static readonly ClassProfile Macrophage = new ClassProfile
        {
            id = TrooperClass.Macrophage,
            title = "MACROPHAGE",
            role = "Heavy",
            blurb = "Slow, tough eater. Lysosome Scattergun, big heals from Phagocyte Grab.",
            maxHealth = 170f,
            speedMultiplier = 0.85f,
            ability = Ability.CytokineBurst,
            abilityCharges = 2,
            abilityRecharge = 14f,
            grabHeal = 55f,
            engulfThreshold = 0.6f,
            armor = new Color(0.95f, 0.85f, 0.7f),
            weapon = new WeaponProfile
            {
                name = "LYSOSOME SCATTERGUN",
                damage = 10f, pellets = 9, fireRate = 1.5f, automatic = false, magazineSize = 6, reloadTime = 2.3f,
                spreadDegrees = 5f, recoilDegrees = 3f, boltColor = new Color(1f, 0.7f, 0.3f), range = 60f,
                modelScale = new Vector3(1.5f, 1.4f, 0.9f),
            },
        };

        public static readonly ClassProfile TCell = new ClassProfile
        {
            id = TrooperClass.TCell,
            title = "T-CELL",
            role = "Marksman",
            blurb = "Precision killer. Perforin Lance with zoom (RMB). Antibody Tag marks enemies for +50% damage.",
            maxHealth = 85f,
            speedMultiplier = 1.1f,
            ability = Ability.AntibodyTag,
            abilityCharges = 2,
            abilityRecharge = 15f,
            grabHeal = 25f,
            engulfThreshold = 0.35f,
            armor = new Color(0.8f, 0.75f, 1f),
            weapon = new WeaponProfile
            {
                name = "PERFORIN LANCE",
                damage = 55f, fireRate = 1.6f, automatic = false, magazineSize = 8, reloadTime = 2f,
                spreadDegrees = 0f, recoilDegrees = 2f, zoomFov = 28f, boltColor = new Color(0.85f, 0.5f, 1f), range = 300f,
                modelScale = new Vector3(0.8f, 0.8f, 1.6f),
            },
        };
    }
}
