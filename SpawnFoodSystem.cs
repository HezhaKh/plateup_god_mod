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
    public class SpawnFoodSystem : GameSystemBase, IModSystem
    {
        private struct Binding
        {
            public KeyCode Key;
            public string NamePattern;
            public Binding(KeyCode k, string n) { Key = k; NamePattern = n; }
        }

        private static readonly Binding[] Bindings = new[]
        {
            new Binding(KeyCode.Alpha1, "Coffee"),
            new Binding(KeyCode.Alpha2, "Pizza"),
            new Binding(KeyCode.Alpha3, "Dumpling"),
            new Binding(KeyCode.Alpha4, "Hot Dog"),
        };

        private readonly Dictionary<string, int> idCache = new Dictionary<string, int>();
        private EntityQuery playerQuery;

        protected override void Initialise()
        {
            base.Initialise();
            playerQuery = GetEntityQuery(typeof(CPlayer));
        }

        protected override void OnUpdate()
        {
            if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                return;

            for (int i = 0; i < Bindings.Length; i++)
            {
                if (Input.GetKeyDown(Bindings[i].Key))
                    TrySpawn(Bindings[i].NamePattern);
            }
        }

        private void TrySpawn(string namePattern)
        {
            if (playerQuery.IsEmpty)
                return;

            Entity player;
            using (var arr = playerQuery.ToEntityArray(Allocator.Temp))
                player = arr[0];

            if (EntityManager.HasComponent<CItemHolder>(player))
            {
                Entity current = EntityManager.GetComponentData<CItemHolder>(player).HeldItem;
                if (current != default && EntityManager.Exists(current))
                    return;
            }

            if (!TryFindItemId(namePattern, out int itemId))
            {
                Debug.LogWarning("[SpawnFood] no item matched '" + namePattern + "'");
                return;
            }

            Entity req = EntityManager.CreateEntity();
            EntityManager.AddComponentData(req, new CCreateItem { ID = itemId, Holder = player });
        }

        private bool TryFindItemId(string pattern, out int id)
        {
            if (idCache.TryGetValue(pattern, out id))
                return true;

            GameData data = GameData.Main;
            if (data == null || data.Objects == null)
            {
                id = 0;
                return false;
            }

            Item exact = null;
            Item contains = null;
            foreach (var kv in data.Objects)
            {
                Item it = kv.Value as Item;
                if (it == null) continue;
                string n = it.name ?? string.Empty;
                if (string.Equals(n, pattern, StringComparison.OrdinalIgnoreCase))
                {
                    exact = it;
                    break;
                }
                if (contains == null && n.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    contains = it;
            }

            Item chosen = exact ?? contains;
            if (chosen == null)
            {
                id = 0;
                return false;
            }
            id = chosen.ID;
            idCache[pattern] = id;
            return true;
        }
    }
}
