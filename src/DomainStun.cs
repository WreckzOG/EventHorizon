using System.Reflection;
using HarmonyLib;
using RoR2;
namespace EventHorizon
{
    internal static class DomainStun
    {
        private static readonly FieldInfo controllers=AccessTools.Field(typeof(SetStateOnHurt),"buffDurationControllers");
        internal static void Apply(SetStateOnHurt hurt,float duration)
        {
            // The installed game fills this array in Start. A growing domain can reach a
            // newly spawned enemy before Start, when native GetModifiedDuration dereferences null.
            // Use precisely the same components as native Start; retain modifiers and native stun/RPC.
            if(controllers!=null && controllers.GetValue(hurt)==null)controllers.SetValue(hurt,hurt.GetComponents<AlterBuffDuration>());
            hurt.SetStun(duration);
        }
    }
}
