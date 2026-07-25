using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central, editable catalogue for the microplastic combo achievements.
/// A collected material is never consumed, so one cleanup can contribute to
/// every matching recipe and the player is free to collect materials in any order.
/// </summary>
public sealed class MicroplasticComboTracker : MonoBehaviour
{
    public enum MaterialType
    {
        Fiber,
        Fragment,
        Pellet,
        Film,
        Foam,
        SeaWater,
        GroundWater,
        UsedPlasticBottle,
        SeaFish,
        VehicleTire,
        TeaBag,
        LungOrOrgan,
        MicrowaveContainer,
        SeaSalt,
        ShipOrWallPaint,
        PlasticStraw
    }

    [Serializable]
    public sealed class MaterialRequirement
    {
        public MaterialType material;
        [Min(1)] public int amount;

        public MaterialRequirement(MaterialType material, int amount)
        {
            this.material = material;
            this.amount = amount;
        }
    }

    [Serializable]
    public sealed class ComboDefinition
    {
        public string id;
        public string title;
        [TextArea(2, 4)] public string requirement;
        [TextArea(4, 8)] public string funFact;
        public MaterialRequirement[] materials;

        public ComboDefinition(string id, string title, string requirement, string funFact, params MaterialRequirement[] materials)
        {
            this.id = id;
            this.title = title;
            this.requirement = requirement;
            this.funFact = funFact;
            this.materials = materials;
        }
    }

    private const string UnlockKeyPrefix = "Microbit.ComboUnlocked.";
    private const string AchievementResetVersionKey = "Microbit.AchievementResetVersion";
    private const int AchievementResetVersion = 1;
    private static readonly List<ComboDefinition> Definitions = new()
    {
        new ComboDefinition("microfiber-clutter", "Microfiber Clutter", "3x Fiber",
            "Washing polyester or nylon clothing can release more than 700,000 microfibers into wastewater in a single load.",
            new MaterialRequirement(MaterialType.Fiber, 3)),
        new ComboDefinition("shattered-bottle", "Shattered Bottle", "2x Fragment + 1x Used Plastic Bottle",
            "A whole plastic bottle can be cracked by sunlight and waves until it becomes millions of sharp plastic fragments.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.UsedPlasticBottle, 1)),
        new ComboDefinition("microbead-scrub", "Microbead Scrub", "3x Pellet (Nurdle)",
            "Tiny round microbeads used in scrub products are so small that they can pass through wastewater systems and head straight for rivers.",
            new MaterialRequirement(MaterialType.Pellet, 3)),
        new ComboDefinition("degraded-plastic-sheet", "Degraded Plastic Sheet", "2x Film + 1x Seawater",
            "Plastic bags can persist for up to 500 years. In seawater they soften into thin film fragments that turtles can mistake for food.",
            new MaterialRequirement(MaterialType.Film, 2), new MaterialRequirement(MaterialType.SeaWater, 1)),
        new ComboDefinition("styrofoam-dust", "Styrofoam Dust", "3x Foam (Styrofoam)",
            "Expanded polystyrene is brittle. When it breaks apart, its lightweight foam particles can travel far on the wind and water.",
            new MaterialRequirement(MaterialType.Foam, 3)),
        new ComboDefinition("contaminated-fish", "Contaminated Fish", "2x Pellet + 1x Sea Fish",
            "Fish can mistake plastic pellets for eggs or plankton. Once eaten, the particles can move through the food web.",
            new MaterialRequirement(MaterialType.Pellet, 2), new MaterialRequirement(MaterialType.SeaFish, 1)),
        new ComboDefinition("tire-rubber-dust", "Tire Rubber Dust", "2x Fragment + 1x Vehicle Tire",
            "Every trip wears a little rubber from vehicle tires. Rain can wash those tiny particles from roads into drains, rivers, and seas.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.VehicleTire, 1)),
        new ComboDefinition("synthetic-tea-bag", "Synthetic Tea Bag", "2x Film + 1x Tea Bag",
            "Some tea bags contain synthetic plastic. In hot water, they can release billions of tiny particles into a single cup.",
            new MaterialRequirement(MaterialType.Film, 2), new MaterialRequirement(MaterialType.TeaBag, 1)),
        new ComboDefinition("microplastic-soup", "Microplastic Soup", "1x Pellet + 1x Fragment + 1x Fiber",
            "Different microplastics mix together in the ocean. Estimates suggest people may consume about 5 grams a week: roughly the mass of a credit card.",
            new MaterialRequirement(MaterialType.Pellet, 1), new MaterialRequirement(MaterialType.Fragment, 1), new MaterialRequirement(MaterialType.Fiber, 1)),
        new ComboDefinition("inhaled-fiber", "Inhaled Fiber", "2x Fiber + 1x Lung / Organ",
            "Synthetic fibers from clothing and carpets can become airborne, small enough to be inhaled and settle in lung tissue.",
            new MaterialRequirement(MaterialType.Fiber, 2), new MaterialRequirement(MaterialType.LungOrOrgan, 1)),
        new ComboDefinition("leached-plastic", "Leached Plastic", "2x Fragment + 1x Microwave Plastic Container",
            "Heating some plastic containers can create microscopic cracks and release plastic fragments into food.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.MicrowaveContainer, 1)),
        new ComboDefinition("plastified-salt", "Plastified Salt", "2x Pellet + 1x Sea Salt",
            "Studies have found traces of microplastics in sea salt from many parts of the world because salt is made from polluted water.",
            new MaterialRequirement(MaterialType.Pellet, 2), new MaterialRequirement(MaterialType.SeaSalt, 1)),
        new ComboDefinition("paint-flakes", "Paint Flakes", "2x Fragment + 1x Ship or Wall Paint",
            "Peeling wall paint and protective ship coatings can become hidden microplastic fragments that drift through the ocean.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.ShipOrWallPaint, 1)),
        new ComboDefinition("brittle-trash", "Brittle Trash", "2x Foam + 1x Plastic Straw",
            "Sunlight makes hard plastics brittle. Straws and food foam can then shatter into sharp pieces that are easy for wildlife to swallow.",
            new MaterialRequirement(MaterialType.Foam, 2), new MaterialRequirement(MaterialType.PlasticStraw, 1)),
        new ComboDefinition("toxic-leachate", "Toxic Leachate", "1x Film + 1x Fragment + 1x Groundwater",
            "Rainwater moving through landfill can carry film and fragment microplastics underground, threatening nearby well water.",
            new MaterialRequirement(MaterialType.Film, 1), new MaterialRequirement(MaterialType.Fragment, 1), new MaterialRequirement(MaterialType.GroundWater, 1))
    };

    private static MicroplasticComboTracker instance;
    private readonly Dictionary<MaterialType, int> collectedMaterials = new();

    public static event Action<ComboDefinition> ComboUnlocked;
    public static IReadOnlyList<ComboDefinition> AllDefinitions => Definitions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeTracker()
    {
        if (instance != null)
        {
            return;
        }

        GameObject trackerObject = new("Microplastic Combo Tracker");
        instance = trackerObject.AddComponent<MicroplasticComboTracker>();
        UnityEngine.Object.DontDestroyOnLoad(trackerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        ResetLegacyUnlocksOnce();
    }

    /// <summary>Called only when a trash object has actually been cleaned.</summary>
    public static void RecordTrashCollected(string trashObjectName)
    {
        if (instance == null)
        {
            CreateRuntimeTracker();
        }

        if (!TryClassifyTrash(trashObjectName, out MaterialType material))
        {
            return;
        }

        instance.Record(material);
    }

    public static bool IsUnlocked(string id)
    {
        ResetLegacyUnlocksOnce();
        return !string.IsNullOrWhiteSpace(id) && PlayerPrefs.GetInt(UnlockKeyPrefix + id, 0) == 1;
    }

    public static int GetCollectedCount(MaterialType material)
    {
        return instance != null && instance.collectedMaterials.TryGetValue(material, out int count) ? count : 0;
    }

    [ContextMenu("Reset Current Run Collection")]
    public void ResetCurrentRunCollection()
    {
        collectedMaterials.Clear();
    }

    [ContextMenu("Reset All Combo Unlocks")]
    public void ResetAllComboUnlocks()
    {
        foreach (ComboDefinition definition in Definitions)
        {
            PlayerPrefs.DeleteKey(UnlockKeyPrefix + definition.id);
        }

        PlayerPrefs.Save();
    }

    private static void ResetLegacyUnlocksOnce()
    {
        if (PlayerPrefs.GetInt(AchievementResetVersionKey, 0) >= AchievementResetVersion)
        {
            return;
        }

        foreach (ComboDefinition definition in Definitions)
        {
            PlayerPrefs.DeleteKey(UnlockKeyPrefix + definition.id);
        }

        PlayerPrefs.SetInt(AchievementResetVersionKey, AchievementResetVersion);
        PlayerPrefs.Save();
    }

    private void Record(MaterialType material)
    {
        collectedMaterials.TryGetValue(material, out int amount);
        collectedMaterials[material] = amount + 1;

        foreach (ComboDefinition definition in Definitions)
        {
            if (IsUnlocked(definition.id) || !RequirementsMet(definition))
            {
                continue;
            }

            PlayerPrefs.SetInt(UnlockKeyPrefix + definition.id, 1);
            PlayerPrefs.Save();
            ComboUnlocked?.Invoke(definition);
            Debug.Log($"Unlocked microplastic combo: {definition.title}");
        }
    }

    private bool RequirementsMet(ComboDefinition definition)
    {
        foreach (MaterialRequirement requirement in definition.materials)
        {
            if (!collectedMaterials.TryGetValue(requirement.material, out int amount) || amount < requirement.amount)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryClassifyTrash(string objectName, out MaterialType material)
    {
        string key = objectName.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("(clone)", string.Empty);

        if (key.Contains("fiber") || key.Contains("serat")) { material = MaterialType.Fiber; return true; }
        if (key.Contains("pellet") || key.Contains("nurdle") || key.Contains("microbead")) { material = MaterialType.Pellet; return true; }
        if (key.Contains("film")) { material = MaterialType.Film; return true; }
        if (key.Contains("foam") || key.Contains("styro") || key.Contains("gabus")) { material = MaterialType.Foam; return true; }
        if (key.Contains("airlaut") || key.Contains("seawater")) { material = MaterialType.SeaWater; return true; }
        if (key.Contains("airtanah") || key.Contains("groundwater")) { material = MaterialType.GroundWater; return true; }
        if (key.Contains("botolplastik") || key.Contains("plasticbottle") || key.Contains("bottle")) { material = MaterialType.UsedPlasticBottle; return true; }
        if (key.Contains("ikanlaut") || key.Contains("ikanmati") || key.Contains("seafish")) { material = MaterialType.SeaFish; return true; }
        if (key.Contains("bankendaraan") || key.Contains("rubbertire") || key.Contains("tire")) { material = MaterialType.VehicleTire; return true; }
        if (key.Contains("kantongteh") || key.Contains("teabag")) { material = MaterialType.TeaBag; return true; }
        if (key.Contains("paru") || key.Contains("lung") || key.Contains("organ")) { material = MaterialType.LungOrOrgan; return true; }
        if (key.Contains("wadahplastikmicrowave") || key.Contains("microwave")) { material = MaterialType.MicrowaveContainer; return true; }
        if (key.Contains("garamlaut") || key.Contains("seasalt")) { material = MaterialType.SeaSalt; return true; }
        if (key.Contains("catkapal") || key.Contains("cepatkapal") || key.Contains("paint")) { material = MaterialType.ShipOrWallPaint; return true; }
        if (key.Contains("sedotan") || key.Contains("straw")) { material = MaterialType.PlasticStraw; return true; }
        if (key.Contains("fragment") || key.Contains("serpihan")) { material = MaterialType.Fragment; return true; }

        material = default;
        return false;
    }
}
