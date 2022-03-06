using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace signals.src.signalNetwork
{
    class BEPressurePlate : BESwitch
    {
        long? listenerId;

        protected Dictionary<Entity, long> collidingEntities = new Dictionary<Entity, long>();
        public override void Initialize(ICoreAPI api)
        {
            BlockPressurePlate block = this.Block as BlockPressurePlate;
            state = false;
            //init the ticklistener depending on state
            //RegisterGameTickListener(OnTick, 50);
            //UnregisterGameTickListener
            base.Initialize(api);
        }

        
        internal bool OnEntityCollide(Entity entity)
        {
            if(Api.Side == EnumAppSide.Client) return false;
            state = true;
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            sw?.commute(state);

            collidingEntities[entity] = Api.World.ElapsedMilliseconds;
 
            listenerId = RegisterGameTickListener(OnServerTick, 50);
            return true;
        }

        
        private void OnServerTick(float dt)
        {
            HashSet<Entity> notColliding = new HashSet<Entity>();
            foreach(Entity entity in collidingEntities.Keys)
            {
                Block block = Api.World.CollisionTester.GetCollidingBlock(Api.World.BlockAccessor, entity.CollisionBox, entity.Pos.XYZ, true);
                if(block != this.Block)
                {
                    notColliding.Add(entity);
                }
                else
                {
                    //collidingEntities[entity] = Api.World.ElapsedMilliseconds;
                }
            }
            foreach(Entity entity in notColliding)
            {
                collidingEntities.Remove(entity);
            }
            if(collidingEntities.Count == 0)
            {
                state = false;
                if(listenerId.HasValue) UnregisterGameTickListener(listenerId.Value);
                BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
                sw?.commute(state);
            }
        }
    }
}