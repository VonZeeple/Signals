using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace signals.src.transmission
{
    class SignalNetworkRenderer : IRenderer
    {

        SignalNetworkMod mod;
        ICoreClientAPI capi;
        IShaderProgram prog;

        //List<HangingWireRenderer>

        public SignalNetworkRenderer(ICoreClientAPI capi, SignalNetworkMod mod)
        {
            this.capi = capi;
            this.mod = mod;

            //capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "mechnetwork");
            //capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowFar, "mechnetwork");
            //capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowNear, "mechnetwork");

            //capi.Event.ReloadShader += LoadShader;
            //LoadShader();


        }

        #region IRenderer implementation
        public double RenderOrder => 0.5;

        public int RenderRange => 100;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            throw new NotImplementedException();
        }
        
        #endregion
    }
}
