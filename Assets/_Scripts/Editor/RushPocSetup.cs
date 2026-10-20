using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

//One-click setup for the rush POC: wires CustomerManager/CartHealth/RushEconomy into the Game scene
//and adds the ShotDog to the Player prefab. Safe to run again: it reuses what already exists.
public static class RushPocSetup
{
    private const string GAME_SCENE_PATH = "Assets/Scenes/Game.unity";
    private const string PLAYER_PREFAB_PATH = "Assets/Prefabs/Player/Player.prefab";
    private const string RECIPE_LIST_PATH = "Assets/ScriptableObjects/RecipeSO/_RecipeListSO.asset";
    private const string MEAT_COOKED_PATH = "Assets/ScriptableObjects/KitchenObjectsSO/MeatPattyCooked.asset";
    private const string MEAT_BURNED_PATH = "Assets/ScriptableObjects/KitchenObjectsSO/MeatPattyBurned.asset";

    private const float SERVICE_POINT_DISTANCE = 2.5f;
    private const float SPAWN_RADIUS = 22f;

    [MenuItem("Murder King/Configurar POC do Rush")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
        Physics.SyncTransforms();

        DeliveryCounter deliveryCounter = Object.FindFirstObjectByType<DeliveryCounter>();
        if (deliveryCounter == null)
        {
            EditorUtility.DisplayDialog("Murder King", "Nenhum DeliveryCounter na cena Game. Coloque um antes de rodar o setup.", "OK");
            return;
        }

        //Customers come from the side of the kitchen where the DeliveryCounter is
        Vector3 kitchenCenter = Vector3.zero;
        BaseCounter[] counters = Object.FindObjectsByType<BaseCounter>(FindObjectsSortMode.None);
        foreach (BaseCounter counter in counters)
        {
            kitchenCenter += counter.transform.position;
        }
        kitchenCenter /= counters.Length;

        Vector3 outward = deliveryCounter.transform.position - kitchenCenter;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f)
        {
            outward = deliveryCounter.transform.forward;
            outward.y = 0f;
        }
        outward.Normalize();

        //---------------Points-----------------
        GameObject pointsRoot = FindOrCreate("RushPoints");
        Transform servicePoint = FindOrCreateChild(pointsRoot.transform, "CustomerServicePoint", out bool createdServicePoint);
        if (createdServicePoint)
        {
            servicePoint.position = Grounded(deliveryCounter.transform.position + outward * SERVICE_POINT_DISTANCE, deliveryCounter.transform.position.y);
            servicePoint.rotation = Quaternion.LookRotation(outward);
        }

        Transform spawnRoot = FindOrCreateChild(pointsRoot.transform, "CustomerSpawnPoints", out _);
        if (spawnRoot.childCount == 0)
        {
            float[] angles = { -70f, -25f, 25f, 70f };
            for (int i = 0; i < angles.Length; i++)
            {
                Transform spawnPoint = new GameObject("Spawn" + (i + 1)).transform;
                spawnPoint.SetParent(spawnRoot, false);
                Vector3 position = servicePoint.position + Quaternion.Euler(0f, angles[i], 0f) * outward * SPAWN_RADIUS;
                spawnPoint.position = Grounded(position, servicePoint.position.y);
            }
        }
        Transform[] spawnPoints = spawnRoot.Cast<Transform>().ToArray();

        //---------------Rush manager-----------------
        CustomerManager customerManager = Object.FindFirstObjectByType<CustomerManager>();
        GameObject rushManager = customerManager != null ? customerManager.gameObject : new GameObject("RushManager");
        GetOrAdd<NetworkObject>(rushManager);
        CartHealth cartHealth = GetOrAdd<CartHealth>(rushManager);
        GetOrAdd<RushEconomy>(rushManager);
        customerManager = GetOrAdd<CustomerManager>(rushManager);
        //The old runtime-built RushHUD was replaced by the UI scripts in _Scripts/UI/Rush
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(rushManager);
        rushManager.transform.position = servicePoint.position;

        List<RecipeSO> recipes = GetCookableRecipes(out bool usedFallback);

        SerializedObject serializedCustomerManager = new SerializedObject(customerManager);
        SetObjectList(serializedCustomerManager.FindProperty("recipeSOList"), recipes);
        SetObjectList(serializedCustomerManager.FindProperty("spawnPoints"), spawnPoints);
        serializedCustomerManager.FindProperty("servicePoint").objectReferenceValue = servicePoint;
        serializedCustomerManager.FindProperty("cartHealth").objectReferenceValue = cartHealth;
        serializedCustomerManager.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        SetupPlayerPrefab();

        Selection.activeGameObject = pointsRoot;
        EditorUtility.DisplayDialog("Murder King",
            "POC do Rush configurada!\n\n" +
            $"Receitas dos clientes: {string.Join(", ", recipes.Select(recipe => recipe.recipeName))}" +
            (usedFallback ? " (nenhuma receita era 100% possível com os balcões da cena, usei todas)" : "") + "\n\n" +
            "Confira na cena o objeto RushPoints: mova o CustomerServicePoint para a janela de atendimento " +
            "(a seta azul aponta para a rua) e os CustomerSpawnPoints para as portas dos prédios.",
            "OK");
    }

    private static void SetupPlayerPrefab()
    {
        GameObject playerRoot = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);
        PlayerShotDog shotDog = GetOrAdd<PlayerShotDog>(playerRoot);

        SerializedObject serializedShotDog = new SerializedObject(shotDog);
        SerializedProperty ammoSourceList = serializedShotDog.FindProperty("ammoSourceList");
        if (ammoSourceList.arraySize == 0)
        {
            //Cooked meat = the dilemma (burger or ammo?); burned meat finally has a use
            AddAmmoSource(ammoSourceList, AssetDatabase.LoadAssetAtPath<KitchenObjectSO>(MEAT_COOKED_PATH), 3);
            AddAmmoSource(ammoSourceList, AssetDatabase.LoadAssetAtPath<KitchenObjectSO>(MEAT_BURNED_PATH), 2);
        }
        serializedShotDog.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(playerRoot, PLAYER_PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(playerRoot);
    }

    private static void AddAmmoSource(SerializedProperty ammoSourceList, KitchenObjectSO kitchenObjectSO, int shells)
    {
        int index = ammoSourceList.arraySize;
        ammoSourceList.InsertArrayElementAtIndex(index);
        SerializedProperty element = ammoSourceList.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("kitchenObjectSO").objectReferenceValue = kitchenObjectSO;
        element.FindPropertyRelative("shells").intValue = shells;
    }

    //Only orders the players can actually cook with the counters placed in the scene
    private static List<RecipeSO> GetCookableRecipes(out bool usedFallback)
    {
        HashSet<KitchenObjectSO> available = new HashSet<KitchenObjectSO>();
        foreach (ContainerCounter containerCounter in Object.FindObjectsByType<ContainerCounter>(FindObjectsSortMode.None))
        {
            Object kitchenObjectSO = new SerializedObject(containerCounter).FindProperty("kitchenObjectSO").objectReferenceValue;
            if (kitchenObjectSO != null)
            {
                available.Add((KitchenObjectSO)kitchenObjectSO);
            }
        }

        List<(KitchenObjectSO input, KitchenObjectSO output)> transforms = new List<(KitchenObjectSO, KitchenObjectSO)>();
        foreach (CuttingCounter cuttingCounter in Object.FindObjectsByType<CuttingCounter>(FindObjectsSortMode.None))
        {
            CollectTransforms(new SerializedObject(cuttingCounter).FindProperty("cuttingRecipeSOArray"), transforms);
        }
        foreach (StoveCounter stoveCounter in Object.FindObjectsByType<StoveCounter>(FindObjectsSortMode.None))
        {
            SerializedObject serializedStove = new SerializedObject(stoveCounter);
            CollectTransforms(serializedStove.FindProperty("fryingRecipeSOArray"), transforms);
            CollectTransforms(serializedStove.FindProperty("burningRecipeSOArray"), transforms);
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach ((KitchenObjectSO input, KitchenObjectSO output) in transforms)
            {
                if (input != null && output != null && available.Contains(input) && available.Add(output))
                {
                    changed = true;
                }
            }
        }

        List<RecipeSO> allRecipes = AssetDatabase.LoadAssetAtPath<RecipeListSO>(RECIPE_LIST_PATH).recipeSOList;
        List<RecipeSO> cookable = allRecipes.Where(recipe => recipe.kitchenObjectSOList.All(available.Contains)).ToList();

        usedFallback = cookable.Count == 0;
        return usedFallback ? allRecipes : cookable;
    }

    private static void CollectTransforms(SerializedProperty recipeArray, List<(KitchenObjectSO, KitchenObjectSO)> transforms)
    {
        for (int i = 0; i < recipeArray.arraySize; i++)
        {
            Object recipe = recipeArray.GetArrayElementAtIndex(i).objectReferenceValue;
            if (recipe == null)
            {
                continue;
            }
            SerializedObject serializedRecipe = new SerializedObject(recipe);
            transforms.Add(((KitchenObjectSO)serializedRecipe.FindProperty("input").objectReferenceValue,
                            (KitchenObjectSO)serializedRecipe.FindProperty("output").objectReferenceValue));
        }
    }

    //---------------Helpers-----------------
    private static Vector3 Grounded(Vector3 position, float fallbackY)
    {
        int mask = ~LayerMask.GetMask("Counters", "Players");
        Vector3 origin = new Vector3(position.x, fallbackY + 3f, position.z);
        position.y = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, mask) ? hit.point.y : fallbackY;
        return position;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
    }

    private static GameObject FindOrCreate(string name)
    {
        GameObject existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }

    private static Transform FindOrCreateChild(Transform parent, string name, out bool created)
    {
        Transform child = parent.Find(name);
        created = child == null;
        if (created)
        {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }
        return child;
    }

    private static void SetObjectList<T>(SerializedProperty listProperty, IList<T> values) where T : Object
    {
        listProperty.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            listProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
