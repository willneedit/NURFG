#if TOOLS
using Godot;

namespace NURFG
{
    [Tool]
    public partial class Plugin : EditorPlugin
    {
        private EditorDock _dock = null;

        private Control _dockContent;

        public override void _EnterTree()
        {
            _dock = new()
            {
                Title = "Test Runner",
                DefaultSlot = EditorDock.DockSlot.RightBl
            };

            _dockContent = (Control)GD.Load<PackedScene>("addons/NURFG/EditorWidget/TestRunnerDock.tscn").Instantiate();
            _dock.AddChild(_dockContent);
            AddDock(_dock);
        }

        public override void _ExitTree()
        {
            // Clean-up of the plugin goes here.
            // Remove the dock.

            // Erase the control from the memory.
            RemoveDock(_dock);
            _dock?.QueueFree();
            _dock = null;
        }
    }
}
#endif