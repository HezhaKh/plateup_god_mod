using System;
using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace InfinitePatience
{
    public class ModInitializer : IModInitializer
    {
        public void PostActivate(Mod mod) { }

        public void PreInject() { }

        public void PostInject()
        {
            if (UnityEngine.Object.FindObjectOfType<SpawnMenuBehaviour>() != null)
                return;
            GameObject host = new GameObject("InfinitePatience.SpawnMenu");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<SpawnMenuBehaviour>();
        }
    }

    public class SpawnMenuBehaviour : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.BackQuote;

        private bool visible;
        private Vector2 scroll;
        private string search = string.Empty;
        private int tab;
        private static readonly string[] Tabs = { "Dishes", "All items", "Appliances" };

        private List<Item> dishes;
        private List<Item> allItems;
        private List<Appliance> appliances;
        private int cachedObjectCount;

        private static readonly float[] SpeedPresets = { 0.25f, 0.5f, 1f, 2f, 4f, 8f };
        private static readonly string[] SpeedLabels = { "0.25x", "0.5x", "1x", "2x", "4x", "8x" };

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            EnsureLists();

            GUI.Box(new Rect(10, 10, 380, 520), "PlateUp dev menu (`)");
            GUILayout.BeginArea(new Rect(20, 35, 360, 490));

            ModState.InfinitePatience = GUILayout.Toggle(ModState.InfinitePatience, " Infinite patience");

            DrawSpeedRow();

            GUILayout.Space(4);
            tab = GUILayout.Toolbar(tab, Tabs);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            search = GUILayout.TextField(search ?? string.Empty);
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(340), GUILayout.Height(400));

            string filter = (search ?? string.Empty).Trim();
            switch (tab)
            {
                case 0: DrawItemList(dishes, filter, isAppliance: false); break;
                case 1: DrawItemList(allItems, filter, isAppliance: false); break;
                case 2: DrawApplianceList(appliances, filter); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawItemList(List<Item> list, string filter, bool isAppliance)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Item it = list[i];
                string n = it.name ?? string.Empty;
                if (filter.Length > 0 && n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (GUILayout.Button(n))
                    SpawnItem(it.ID);
            }
        }

        private void DrawApplianceList(List<Appliance> list, string filter)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Appliance a = list[i];
                string n = a.name ?? string.Empty;
                if (filter.Length > 0 && n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (GUILayout.Button(n))
                    SpawnAppliance(a.ID);
            }
        }

        private void EnsureLists()
        {
            GameData data = GameData.Main;
            if (data == null || data.Objects == null)
            {
                dishes ??= new List<Item>();
                allItems ??= new List<Item>();
                appliances ??= new List<Appliance>();
                return;
            }
            if (dishes != null && allItems != null && appliances != null && cachedObjectCount == data.Objects.Count)
                return;

            dishes = new List<Item>();
            allItems = new List<Item>();
            appliances = new List<Appliance>();
            HashSet<int> dishItemIds = new HashSet<int>();
            var resultingMenuItemsField = typeof(Dish).GetField(
                "ResultingMenuItems",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var kv in data.Objects)
            {
                GameDataObject gdo = kv.Value;
                if (gdo is Item it)
                {
                    allItems.Add(it);
                }
                else if (gdo is Appliance ap)
                {
                    appliances.Add(ap);
                }
                else if (gdo is Dish d)
                {
                    List<Dish.MenuItem> list = d.UnlocksMenuItems;
                    if (list == null || list.Count == 0)
                        list = resultingMenuItemsField?.GetValue(d) as List<Dish.MenuItem>;
                    if (list != null)
                    {
                        foreach (var mi in list)
                            if (mi.Item != null)
                                dishItemIds.Add(mi.Item.ID);
                    }
                }
            }

            foreach (Item it in allItems)
            {
                if (dishItemIds.Contains(it.ID) || it.IsConsumedByCustomer)
                    dishes.Add(it);
            }
            Comparison<Item> itemByName = (a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            Comparison<Appliance> appByName = (a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            dishes.Sort(itemByName);
            allItems.Sort(itemByName);
            appliances.Sort(appByName);
            cachedObjectCount = data.Objects.Count;
        }

        private void DrawSpeedRow()
        {
            float current = ReadGameSpeed();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", GUILayout.Width(60));
            for (int i = 0; i < SpeedPresets.Length; i++)
            {
                bool active = Mathf.Approximately(current, SpeedPresets[i]);
                GUIStyle style = active ? GUI.skin.box : GUI.skin.button;
                if (GUILayout.Button(SpeedLabels[i], style))
                    WriteGameSpeed(SpeedPresets[i]);
            }
            GUILayout.EndHorizontal();
        }

        private static float ReadGameSpeed()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return 1f;
            EntityManager em = world.EntityManager;
            EntityQuery q = em.CreateEntityQuery(typeof(SGameTime));
            using (var arr = q.ToEntityArray(Allocator.Temp))
            {
                if (arr.Length == 0) return 1f;
                return em.GetComponentData<SGameTime>(arr[0]).GameSpeed;
            }
        }

        private static void WriteGameSpeed(float speed)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            EntityManager em = world.EntityManager;
            EntityQuery q = em.CreateEntityQuery(typeof(SGameTime));
            using (var arr = q.ToEntityArray(Allocator.Temp))
            {
                if (arr.Length == 0) return;
                SGameTime data = em.GetComponentData<SGameTime>(arr[0]);
                data.GameSpeed = speed;
                em.SetComponentData(arr[0], data);
            }
        }

        private static bool TryGetPlayer(EntityManager em, out Entity player, out CPosition playerPos)
        {
            player = default;
            playerPos = default;
            EntityQuery q = em.CreateEntityQuery(typeof(CPlayer), typeof(CPosition));
            if (q.IsEmpty)
                return false;
            using (var arr = q.ToEntityArray(Allocator.Temp))
                player = arr[0];
            playerPos = em.GetComponentData<CPosition>(player);
            return true;
        }

        private void SpawnItem(int itemId)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            EntityManager em = world.EntityManager;
            if (!TryGetPlayer(em, out Entity player, out _))
                return;
            if (em.HasComponent<CItemHolder>(player))
            {
                Entity current = em.GetComponentData<CItemHolder>(player).HeldItem;
                if (current != default && em.Exists(current))
                    return;
            }
            Entity req = em.CreateEntity();
            em.AddComponentData(req, new CCreateItem { ID = itemId, Holder = player });
        }

        private void SpawnAppliance(int applianceId)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            EntityManager em = world.EntityManager;
            if (!TryGetPlayer(em, out _, out CPosition playerPos))
                return;

            Vector3 target = playerPos.ForwardPosition;
            CPosition spawnPos = CPosition.Rounded(target);

            Entity req = em.CreateEntity();
            em.AddComponentData(req, new CCreateAppliance { ID = applianceId, ForceLayer = OccupancyLayer.Default });
            em.AddComponentData(req, spawnPos);
        }
    }
}
