using Kitchen;
using KitchenMods;
using Unity.Entities;

namespace InfinitePatience
{
    public class InfinitePatienceSystem : GameSystemBase, IModSystem
    {
        private EntityQuery cheatQuery;

        protected override void Initialise()
        {
            base.Initialise();
            cheatQuery = GetEntityQuery(typeof(SCheatNoPatienceDecrease));
        }

        protected override void OnUpdate()
        {
            if (ModState.InfinitePatience)
            {
                if (cheatQuery.IsEmpty)
                    EntityManager.CreateEntity(typeof(SCheatNoPatienceDecrease));
            }
            else
            {
                if (!cheatQuery.IsEmpty)
                    EntityManager.DestroyEntity(cheatQuery);
            }
        }
    }
}
