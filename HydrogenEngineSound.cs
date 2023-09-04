using System.Reflection;
using System.Collections.Generic;
using Sandbox;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Audio;
using VRage.Plugins;
using HarmonyLib;

namespace EngineSound
{
    public class HydrogenEngineSound : IPlugin
    {
        public void Init(object gameinstance = null)
        {
            var harmony = new Harmony("EngineSound");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }
    }

    //[HarmonyPatch(typeof(MyHydrogenEngine), "UpdateAfterSimulation100")]
    [HarmonyPatch(typeof(MyGasFueledPowerProducer), "UpdateBeforeSimulation")]
    public class MyHydrogenEngineUpdatePatch
    {
        public static void Postfix(MyHydrogenEngine __instance)
        {
            __instance.UpdateSoundState();
        }
    }

    //[HarmonyPatch(typeof(MyHydrogenEngine), "OnStartWorking")]
    [HarmonyPatch(typeof(MyHydrogenEngine), "UpdateAfterSimulation100")]
    public class MyHydrogenEngineUpdateBeforeSimulation100Patch
    {
        public static void Postfix(MyHydrogenEngine __instance)
        {
            if (__instance.m_soundEmitter != null && Sandbox.Game.World.MySector.MainCamera != null)
            {
                __instance.MarkForUpdate();
            }
        }
    }



    public static class MyHydrogenEngineExtensions
    {
        private static Dictionary<MyHydrogenEngine, float> _m_lastOutput = new Dictionary<MyHydrogenEngine, float>();
        private static MethodInfo _markforupdate = AccessTools.DeclaredMethod(typeof(MyGasFueledPowerProducer), "MarkForUpdate");


        // When testing, this was run from MyGasFueledPowerProducer.UpdateBeforeSimulation. I'm guessing we'll want it in MyHydrogenEngine.
        public static void UpdateSoundState(this MyHydrogenEngine _this)
        {
            if (!MySandboxGame.IsGameReady || _this.m_soundEmitter == null || !_this.IsWorking)
            {
                return;
            }

            if (_this.m_soundEmitter.Sound != null && _this.m_soundEmitter.Sound.IsPlaying)
            {
                if (!_m_lastOutput.ContainsKey(_this))
                    _m_lastOutput.Add(_this, 0f);
                float m_lastOutput = _m_lastOutput[_this]; // This can be removed when m_lastOutput is an instance field

                float usePercentage = _this.SourceComp.CurrentOutput / _this.SourceComp.MaxOutput;
                if (usePercentage != m_lastOutput)
                {
                    float mul = (usePercentage > m_lastOutput ? .1f : .02f);

                    float rpm = m_lastOutput + (usePercentage - m_lastOutput) * mul; // These two lines can be merged by assigning directly to m_lastOutput
                    _m_lastOutput[_this] = rpm;

                    float semitones = 8f * rpm - 6f;
                    _this.m_soundEmitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);

                    _this.m_soundEmitter.Sound.VolumeMultiplier = .8f + rpm;

                    _this.MarkForUpdate();
                }
            }
        }

        // Wrapper for private method
        public static void MarkForUpdate(this MyGasFueledPowerProducer _this)
        {
            _markforupdate.Invoke(_this, null);
        }
    }
}
