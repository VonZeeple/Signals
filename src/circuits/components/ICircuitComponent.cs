using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.circuits.components
{
    interface ICircuitComponent
    {

        ItemStack ItemStack { get; set; }

        bool DoesInstantUpdate();

        bool IsSource();
        Vec3i[] GetOutputPositions();
        Vec3i[] GetInputPositions();
        byte[] GetOutputs();

        void SetInputs(byte[] inputs);

        void Update(float dt);

        ItemStack GetItemStackOnRemove();
        Cuboidf GetSelectionBox();

        //Used to detect collisions with other components of the circuit
        Cuboidf GetCollisionBox();

        //Smaller selection box might be conveninent for placement sometimes, as the mouse cursor tend to be on top of the SB.
        Cuboidf GetSelectionBoxForPlacement();


        MeshData getMesh(ICoreClientAPI inCapi);


        void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving);


        void ToTreeAttributes(ITreeAttribute tree);


    }
}
