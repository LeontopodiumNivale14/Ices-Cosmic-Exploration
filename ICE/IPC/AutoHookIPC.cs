using ECommons.EzIpcManager;
using ECommons.Reflection;
using System.Threading.Tasks;

namespace ICE.IPC
{
    public class AutoHookIPC
    {
        public const string Name = "AutoHook";
        public const string Repo = "https://github.com/PunishXIV/AutoHook";
        public AutoHookIPC() => EzIPC.Init(this, Name, SafeWrapper.AnyException);
        public bool Installed => Utils.HasPlugin(Name);
        public bool UpdatedPlugin()
        {
            // Really only need this for users, should probably add a way for dev plugin versions (ah) but :shrug:

            /*
            if (DalamudReflector.TryGetDalamudPlugin(Name, out var plogon, false, true))
            {
                if (plogon.GetType().Assembly.GetName().Version < new Version(6, 0, 0, 27))
                    return false;

                return true;
            }

            return false;
            */
            return true;
        }

        [EzIPC] private readonly Func<bool> GetPluginState;
        [EzIPC] private Action<bool> SetPluginState;

        [EzIPC] private readonly Func<bool> GetAutoStartFishing;
        [EzIPC] private Action<bool> SetAutoStartFishing;

        [EzIPC] public Action<bool> SetAutoGigState;
        [EzIPC] public Action<string> SetPreset;
        [EzIPC] public Action<string> SetPresetAutogig;
        [EzIPC] public Action<string> CreateAndSelectAnonymousPreset;
        [EzIPC] public Action<string> CreateAndSelectAnonymousFolder;
        [EzIPC] public Action<string> ImportAndSelectPreset;
        [EzIPC] public Action DeleteSelectedPreset;
        [EzIPC] public Action DeleteAllAnonymousPresets;
        [EzIPC] public Func<uint, Task<bool>> SwapBaitById;

        public void Ah_State(bool state)
        {
            bool stateEnabled = GetPluginState();

            if (EzThrottler.Throttle("Applying autohook states"))
            {
                if (state)
                {
                    if (!stateEnabled)
                        SetPluginState(true);
                }

                if (!state)
                {
                    if (stateEnabled)
                        SetPluginState(false);
                }
            }
        }
    }
}
